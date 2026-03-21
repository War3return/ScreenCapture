using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using epicro.Helpers;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace epicro.Logic
{
    public class BeltMacro
    {
        private volatile bool running = false;
        private volatile bool stopRequested = false;
        private IntPtr targetWindow;
        private DateTime lastHeroKeyPressTime;
        private Thread macroThread;
        private readonly Action<string> log;

        // Windows API
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern void PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP   = 0x0101;
        private const int WM_CHAR    = 0x0102;
        private const int VK_RETURN  = 0x0D;

        // 숫자 키 매핑 (0~9 키보드 숫자)
        private static readonly Dictionary<int, int> NUMKEY_CODE = new Dictionary<int, int>
        {
            { 0, 0x30 }, { 1, 0x31 }, { 2, 0x32 }, { 3, 0x33 }, { 4, 0x34 },
            { 5, 0x35 }, { 6, 0x36 }, { 7, 0x37 }, { 8, 0x38 }, { 9, 0x39 }
        };

        // 넘버패드 키 매핑
        private static readonly Dictionary<string, int> NUMPAD_CODES = new Dictionary<string, int>()
        {
            { "넘버패드1", (int)Keys.NumPad1 },
            { "넘버패드2", (int)Keys.NumPad2 },
            { "넘버패드3", (int)Keys.NumPad3 },
            { "넘버패드4", (int)Keys.NumPad4 },
            { "넘버패드5", (int)Keys.NumPad5 },
            { "넘버패드6", (int)Keys.NumPad6 },
            { "넘버패드7", (int)Keys.NumPad7 },
            { "넘버패드8", (int)Keys.NumPad8 },
            { "넘버패드9", (int)Keys.NumPad9 },
            { "넘버패드0", (int)Keys.NumPad0 }
        };

        public BeltMacro(Action<string> log, IntPtr hwnd)
        {
            this.log = log;
            targetWindow = hwnd;
        }

        public void StartMacro()
        {
            if (running) return;
            if (targetWindow == IntPtr.Zero) return;

            running = true;
            stopRequested = false;
            lastHeroKeyPressTime = DateTime.Now;

            macroThread = new Thread(RunMacro);
            macroThread.IsBackground = true;
            macroThread.Start();
        }

        public void StopMacro()
        {
            stopRequested = true;
            running = false;

            if (macroThread != null && macroThread.IsAlive)
            {
                macroThread.Join(TimeSpan.FromSeconds(5)); // 최대 5초 대기 후 자동 해제
            }
        }

        private void RunMacro()
        {
            DateTime lastHourlyTaskTime = DateTime.Now;

            while (running && !stopRequested)
            {
                try
                {
                    if (targetWindow == IntPtr.Zero)
                    {
                        log("타겟 윈도우가 종료됨, 매크로 자동 중지");
                        StopMacro();
                        return;
                    }

                    // ▶ 매 루프마다 최신 설정을 메모리에서 직접 읽음 (디스크 접근 없음)
                    double currentBeltSpeed  = SettingsManager.Current.BeltSpeed;
                    bool   heroSelectEnabled = SettingsManager.Current.HeroSelectEnabled;
                    int    heroNum           = SettingsManager.Current.HeroNum;
                    int    currentBeltKey    = NUMPAD_CODES.TryGetValue(SettingsManager.Current.BeltNum, out int k)
                                                   ? k : (int)Keys.NumPad2;

                    DateTime currentTime = DateTime.Now;

                    // 1시간마다 수행 (범줍 + 세이브)
                    if ((currentTime - lastHourlyTaskTime).TotalSeconds >= 3600)
                    {
                        PerformHourlyTasks();
                        lastHourlyTaskTime = DateTime.Now;
                    }

                    // 10초마다 영웅 재선택
                    if (heroSelectEnabled && (currentTime - lastHeroKeyPressTime).TotalSeconds >= 10)
                    {
                        if (heroNum == 0)
                        {
                            SendKey(targetWindow, (int)Keys.F1);
                            lastHeroKeyPressTime = DateTime.Now;
                        }
                        else if (NUMKEY_CODE.TryGetValue(heroNum, out int heroKey))
                        {
                            SendKey(targetWindow, heroKey);
                            lastHeroKeyPressTime = DateTime.Now;
                        }
                    }

                    // 채팅창 열림 감지 → 엔터로 닫기
                    if (SettingsManager.Current.PreventChatboxEnter
                        && epicro.Wc3.Memory.States.IsChatBoxOpen)
                    {
                        SendKey(targetWindow, VK_RETURN);
                        Thread.Sleep(100);
                    }

                    // 벨트 넘버패드 키 입력 (SendMessage — 채팅창 비활성 보장)
                    if (currentBeltKey != 0)
                    {
                        SendKey(targetWindow, currentBeltKey);
                    }

                    Thread.Sleep((int)(currentBeltSpeed * 1000));
                }
                catch (ThreadAbortException)
                {
                    // StopMacro() 등에서 스레드가 중단될 때 — 정상 종료
                    return;
                }
                catch (Exception ex)
                {
                    // 예외 발생 시 루프를 종료하지 않고 1초 대기 후 재시작
                    log($"[벨트매크로] 오류 발생, 재시작: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// 1시간마다 수행되는 작업 (범줍, 세이브)
        /// — 실행 시점에 SettingsManager.Current를 읽어 항상 최신 설정 반영
        /// </summary>
        private void PerformHourlyTasks()
        {
            if (targetWindow == IntPtr.Zero) return;

            // ▶ 실행 시점의 최신 설정 읽기
            bool bumEnabled  = SettingsManager.Current.PickupEnabled;
            bool saveEnabled = SettingsManager.Current.SaveEnabled;
            int  bagNum      = SettingsManager.Current.BagNum;
            int  heroNum     = SettingsManager.Current.HeroNum;

            // 범줍
            if (bumEnabled)
            {
                Thread.Sleep(1000);

                if (bagNum != 0 && NUMKEY_CODE.TryGetValue(bagNum, out int bagKey))
                    SendKey(targetWindow, bagKey);
                else if (bagNum == 0)
                    SendKey(targetWindow, (int)Keys.F8);

                Thread.Sleep(200);
                SendChar(targetWindow, 'q');
                Thread.Sleep(200);
                SendChar(targetWindow, 'g');
                Thread.Sleep(200);

                if (heroNum == 0)
                    SendKey(targetWindow, (int)Keys.F1);
                else if (NUMKEY_CODE.TryGetValue(heroNum, out int heroKey))
                    SendKey(targetWindow, heroKey);

                //log("창고 범줍 완료!");
            }

            Thread.Sleep(500);

            // 세이브
            if (saveEnabled)
            {
                epicro.Wc3.Worker.Actions.SetSaveReady(); // 파일 감시자 사전 활성화 (async void 우회)
                bool ok = Wc3ChatSender.SendChatMessage("-save");
                log(ok ? "세이브 완료!" : Wc3ChatSender.LastError);
                Thread.Sleep(500);
            }
        }

        private void PressEscAfterDelay()
        {
            Thread.Sleep(5000);
            SendKey(targetWindow, (int)Keys.Escape);
        }

        // SendMessage (동기) — 게임 커맨드용, 채팅창 비활성 보장
        private void SendKey(IntPtr hwnd, int keyCode)
        {
            SendMessage(hwnd, WM_KEYDOWN, keyCode, 0);
            Thread.Sleep(50);
            SendMessage(hwnd, WM_KEYUP, keyCode, 0);
        }

        // PostMessage (비동기) — 필요 시 사용
        private void SendKeyPost(IntPtr hwnd, int keyCode)
        {
            PostMessage(hwnd, WM_KEYDOWN, keyCode, 0);
            Thread.Sleep(50);
            PostMessage(hwnd, WM_KEYUP, keyCode, 0);
        }

        // 단일 문자 입력 (VkKeyScan → SendMessage)
        private void SendChar(IntPtr hwnd, char ch)
        {
            short keyInfo = VkKeyScan(ch);
            int vk = keyInfo & 0xFF;

            SendMessage(hwnd, WM_KEYDOWN, vk, 0);
            Thread.Sleep(50);
            SendMessage(hwnd, WM_KEYUP, vk, 0);
        }

        // 문자열 입력 (WM_CHAR — 채팅 커맨드용)
        private void SendString(IntPtr hwnd, string text)
        {
            foreach (char ch in text)
            {
                PostMessage(hwnd, WM_CHAR, ch, 0);
                Thread.Sleep(50);
            }
        }

        private void SendKeyVK_Post(IntPtr hwnd, int vk)
        {
            PostMessage(hwnd, WM_KEYDOWN, vk, 0);
            Thread.Sleep(10);
            PostMessage(hwnd, WM_KEYUP, vk, 0);
        }

        private void SendStringVK(IntPtr hwnd, string text)
        {
            foreach (char ch in text)
            {
                short vkCombo = VkKeyScan(ch);
                int vk = vkCombo & 0xFF;
                int shift = (vkCombo >> 8) & 0xFF;

                if ((shift & 1) != 0)
                {
                    PostMessage(hwnd, WM_KEYDOWN, (int)Keys.ShiftKey, 0);
                    Thread.Sleep(5);
                }

                PostMessage(hwnd, WM_KEYDOWN, vk, 0);
                Thread.Sleep(10);
                PostMessage(hwnd, WM_KEYUP, vk, 0);

                if ((shift & 1) != 0)
                {
                    Thread.Sleep(5);
                    PostMessage(hwnd, WM_KEYUP, (int)Keys.ShiftKey, 0);
                }
            }
        }
    }
}
