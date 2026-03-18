//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using CaptureSampleCore;
using Composition.WindowsRuntimeHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.UI.Composition;
using epicro.Utilites;
using System.Windows.Threading;
using epicro.Helpers;
using Tesseract;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using epicro.Logic;
using epicro.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reflection;
using epicro.Wc3;
using epicro.Wc3.Worker;
using epicro.Wc3.KeyHook;
using epicro.Wc3.Memory;


namespace epicro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr hwnd;
        private Compositor compositor;
        private CompositionTarget target;
        private ContainerVisual root;

        private BasicSampleApplication sample;
        private ObservableCollection<WindowInfo> processes;
        private BossSummonerWpf summoner;
        private ProcessMemoryWatcher processWatcher;

        public static BasicCapture backgroundCapture;
        private OcrService ocrService;
        private BeltMacro beltMacro;
        public static WindowInfo TargetWindow { get; private set; }
        private string _lastWindowTitle;

        private System.Timers.Timer ocrTimer;
        public static TesseractEngine ocrEngine;
        private TelegramBotService _telegramBotService;
        private CancellationTokenSource _reattachCts;

        public ObservableCollection<BossStats> AllBossStats { get; set; } = new ObservableCollection<BossStats>();
        public ObservableCollection<BossStats> FilteredBossStatsList { get; set; } = new ObservableCollection<BossStats>();

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool AdjustWindowRect(ref RECT lpRect, uint dwStyle, bool bMenu);

        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        const int GWL_STYLE = -16;

        public MainWindow()
        {
            InitializeComponent();
            var picker = new GraphicsCapturePicker();
            try
            {
                string tessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tesseract", "tessdata");
                ocrEngine = new TesseractEngine(tessPath, "eng", EngineMode.Default);
            }
            catch (Exception ex)
            {
                ocrEngine = null;
                Debug.WriteLine($"[OCR] Tesseract 초기화 실패: {ex.Message}");
            }
            InitBossSummoner();
            InitBossStats();
            this.DataContext = this;
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var shortVer = $"{version.Major}.{version.Minor}";
            this.Title = $"epicro v{shortVer}";
#if DEBUG

#endif
        }
        private void InitBossSummoner()
        {
            summoner = new BossSummonerWpf(AppendLog, FilteredBossStatsList, UpdateWoodStatus);
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int hero = SettingsManager.Current.HeroNum;
            int bag = SettingsManager.Current.BagNum;
            string beltNum = SettingsManager.Current.BeltNum;
            double beltSpeed = SettingsManager.Current.BeltSpeed;

            // 🔹 보스존 복원
            foreach (ComboBoxItem item in cbb_BossZone.Items)
            {
                if (item.Content.ToString() == SettingsManager.Current.BossZone)
                {
                    cbb_BossZone.SelectedItem = item;
                    break;
                }
            }

            // 🔹 인식방법 복원
            string roi = SettingsManager.Current.SelectedROI;
            if (roi == "gold") rb_Gold.IsChecked = true;
            else if (roi == "tree") rb_Tree.IsChecked = true;

            // 🔹 소환순서 복원
            txt_BossOrder.Text = SettingsManager.Current.BossOrder;

            // 필터 설정을 SettingsManager에서 가져옴
            var textColors = new List<Tuple<System.Drawing.Color, int>>();

            // 각 설정 값이 비어있지 않으면 리스트에 추가
            if (!string.IsNullOrEmpty(SettingsManager.Current.TextColor1))
                textColors.Add(new Tuple<System.Drawing.Color, int>(ColorTranslator.FromHtml(SettingsManager.Current.TextColor1), SettingsManager.Current.TextRange1));
            if (!string.IsNullOrEmpty(SettingsManager.Current.TextColor2))
                textColors.Add(new Tuple<System.Drawing.Color, int>(ColorTranslator.FromHtml(SettingsManager.Current.TextColor2), SettingsManager.Current.TextRange2));
            if (!string.IsNullOrEmpty(SettingsManager.Current.TextColor3))
                textColors.Add(new Tuple<System.Drawing.Color, int>(ColorTranslator.FromHtml(SettingsManager.Current.TextColor3), SettingsManager.Current.TextRange3));

            // 배경 색상도 설정
            var backgroundColor = string.IsNullOrEmpty(SettingsManager.Current.BackgroundColor)
                ? null
                : new Tuple<System.Drawing.Color, int>(ColorTranslator.FromHtml(SettingsManager.Current.BackgroundColor), SettingsManager.Current.BackgroundRange);

            AppendLog("에피크로 로딩 완료");
            //Debug.WriteLine($"불러온 설정값 - 영웅: {hero}, 창고: {bag}, 벨트번호: {beltNum}, 속도: {beltSpeed}");

            LoadRoiAreas();
            InitWc3UI();
            /*
            var interopWindow = new WindowInteropHelper(this);
            hwnd = interopWindow.Handle;

            var presentationSource = PresentationSource.FromVisual(this);
            double dpiX = 1.0;
            double dpiY = 1.0;
            if (presentationSource != null)
            {
                dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }
            var controlsWidth = (float)(ControlsGrid.ActualWidth * dpiX);

            InitComposition(controlsWidth);
            InitWindowList();
            */
            // 기존 실시간 송출 제거 → 대신 BackgroundCapture만 시작

            // 텔레그램 봇 서비스 초기화
            var chatIds = SettingsManager.Current.TelegramChatIds;
            _telegramBotService = new TelegramBotService(chatIds);
            _telegramBotService.IsEnabled = SettingsManager.Current.TelegramEnabled;

            // 매크로 단축키 버튼 초기값 (WC3 연결 전이므로 설정에서 직접 읽기)
            {
                var parts = SettingsManager.Current.MacroHotkeys.Split('∫');
                var beltKey = (parts.Length > 0 && int.TryParse(parts[0], out int b) && b != 0)
                    ? (System.Windows.Forms.Keys)b : System.Windows.Forms.Keys.None;
                var bossKey = (parts.Length > 1 && int.TryParse(parts[1], out int s) && s != 0)
                    ? (System.Windows.Forms.Keys)s : System.Windows.Forms.Keys.None;
                btn_Macro_Belt.Content = MacroKeyDisplayName(beltKey);
                btn_Macro_Boss.Content = MacroKeyDisplayName(bossKey);
            }

            // Cirnix 활성화 초기값
            chk_Main_CirnixEnabled.IsChecked = SettingsManager.Current.IsCirnixEnabled;

            // 자동 업데이트 확인 (네트워크 오류 시 무음 처리)
            await UpdateHelper.CheckAndPromptUpdateAsync();
        }

        private string GetStatusText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"선택된 창: {TargetWindow?.Title ?? "없음"}");
            sb.AppendLine($"벨트 매크로: {(beltMacro != null ? "실행 중" : "정지")}");
            sb.AppendLine($"보스 소환기: {(summoner?.IsRunning == true ? "실행 중" : "정지")}");
            return sb.ToString().TrimEnd();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 🔸 보스존 저장
            if (cbb_BossZone.SelectedItem is ComboBoxItem selectedZone)
            {
                SettingsManager.Current.BossZone = selectedZone.Content.ToString();
            }

            // 🔸 인식방법 저장 (라디오버튼)
            if (rb_Gold.IsChecked == true)
                SettingsManager.Current.SelectedROI = "gold";
            else if (rb_Tree.IsChecked == true)
                SettingsManager.Current.SelectedROI = "tree";

            // 🔸 소환순서 저장
            SettingsManager.Current.BossOrder = txt_BossOrder.Text;

            // WC3 정리
            try
            {
                KeyboardHooker.HookEnd();
                AutoRG.CancelAsync();
                AutoStarter.CancelAsync();
                AutoMouse.Enabled = false;
                MaxRoom.CancelAsync();
                MinRoom.CancelAsync();
                Commands.StopDetect();
            }
            catch (Exception ex) { Debug.WriteLine($"[Window_Closing] WC3 정리 중 오류: {ex.Message}"); }

            SettingsManager.Save(); // 저장!

            // 보스소환기가 실행 중이면 중지
            if (summoner != null)
            {
                summoner.Stop();  // 내부적으로 isRunning = false, CancellationToken.Cancel()
            }
            if (beltMacro != null)
            {
                beltMacro.StopMacro(); // 매크로 중지
                beltMacro = null; // BeltMacro 객체 해제
            }

            if (backgroundCapture != null)
            {
                backgroundCapture.StopCapture();
                backgroundCapture.Dispose(); // 기존 캡처 정리
                backgroundCapture = null;
                Debug.WriteLine("이전 백그라운드 캡처 해제 완료");
            }

            if (TargetWindow != null)
            {
                TargetWindow.ProcessExited -= OnTargetWindowExited;
                TargetWindow.StopMonitoring();
            }

            processWatcher?.Stop();

            _reattachCts?.Cancel();
            _reattachCts?.Dispose();
            _reattachCts = null;

            _telegramBotService?.Dispose();

            // ocrEngine, FileSystemWatcher 등은 Environment.Exit(0)이 OS 수준에서
            // 전부 회수하므로 별도 Dispose 불필요 (백그라운드 스레드와의 경쟁 방지)

            // 백그라운드 스레드가 남아 프로세스가 종료 안 되는 경우 방지
            Environment.Exit(0);
        }
        private void UpdateWoodStatus(int totalWood, double woodPerHour)
        {
            txtTotalWood.Text = $"{totalWood:N0}";
            txtWoodPerHour.Text = $"{woodPerHour:N0}";
        }

        private void InitBossStats()
        {
            var bossConfigs = BossConfigManager.GetBossConfigs();

            foreach (var kvp in bossConfigs)
            {
                string bossName = kvp.Key;

                // 중복 방지
                if (!AllBossStats.Any(x => x.Name == bossName))
                {
                    AllBossStats.Add(new BossStats { Name = bossName });
                }
            }
        }
        public void UpdateFilteredStats(string selectedZone)
        {
            var bossConfigs = BossConfigManager.GetBossConfigs();

            FilteredBossStatsList.Clear();

            foreach (var stat in AllBossStats)
            {
                if (bossConfigs.TryGetValue(stat.Name, out var config) && config.Zone == selectedZone)
                {
                    FilteredBossStatsList.Add(stat);
                }
            }

            if (txtTotalKills != null)
                txtTotalKills.Text = AllBossStats.Sum(b => b.KillCount).ToString("N0");
        }

        public void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txt_log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                txt_log.ScrollToEnd(); // 항상 최신 로그 보기
            });
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            //StopCapture();
            WindowComboBox.SelectedIndex = -1;
        }

        private void WindowComboBox_DropDownOpened(object sender, EventArgs e)
        {
            InitWindowList();
        }

        private void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var process = (WindowInfo)comboBox.SelectedItem;
            if (process == null) return;

            // 사용자가 수동 선택 → 재연결 감시 취소
            _reattachCts?.Cancel();
            _reattachCts?.Dispose();
            _reattachCts = null;
            AttachToWindow(process);
        }

        private void AttachToWindow(WindowInfo process)
        {
            // 이전 TargetWindow 이벤트 구독 해제
            if (TargetWindow != null)
            {
                TargetWindow.ProcessExited -= OnTargetWindowExited;
                TargetWindow.StopMonitoring();
            }

            TargetWindow = process;
            _lastWindowTitle = process.Title;

            if (backgroundCapture != null)
            {
                backgroundCapture.StopCapture();
                backgroundCapture.Dispose();
                backgroundCapture = null;
            }

            var hwnd = process.Handle;
            try
            {
                var d3dDevice = Direct3D11Helper.CreateDevice();
                var item = CaptureHelper.CreateItemForWindow(TargetWindow.Handle);
                backgroundCapture = new BasicCapture(d3dDevice, item);
                backgroundCapture.StartCapture();
                AppendLog($"창 선택됨: {process.ToString()}");

                if (processWatcher != null)
                {
                    processWatcher.Stop();
                    processWatcher = null;
                }

                var wc3Process = Process.GetProcessById(process.ProcessId);
                processWatcher = new ProcessMemoryWatcher(wc3Process, UpdateMemoryLabel);
                processWatcher.Start();

                Wc3ChatSender.Initialize(wc3Process, TargetWindow.Handle);

                try
                {
                    epicro.Wc3.Component.Warcraft3Info.Process = wc3Process;
                    GameModule.GetOffset();

                    // 매크로 단축키 델리게이트 – InitFunction.Init() 전에 할당
                    MacroHotkey.ToggleBelt = () => Dispatcher.Invoke(ToggleBeltMacro);
                    MacroHotkey.ToggleBoss = () => Dispatcher.Invoke(ToggleBossSummoner);

                    InitFunction.Init();
                    btn_Macro_Belt.Content = MacroKeyDisplayName(MacroHotkey.BeltKey);
                    btn_Macro_Boss.Content = MacroKeyDisplayName(MacroHotkey.BossKey);
                    epicro.Wc3.Wc3Globals.chatHotkeyList.RestoreRegistrations();
                    Commands.StartDetect();
                    KeyboardHooker.HookStart();
                    AppendLog("WC3 메모리 초기화 완료");
                    UpdateWc3StatusLabel(true);
                }
                catch (Exception wc3Ex)
                {
                    AppendLog($"WC3 초기화 실패: {wc3Ex.Message}");
                }

                TargetWindow.ProcessExited += OnTargetWindowExited;
                TargetWindow.StartExitAndRestartMonitoring();
            }
            catch (Exception)
            {
                Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                processes?.Remove(process);
            }
        }

        private void StartReattachWatcher(string targetTitle)
        {
            _reattachCts?.Cancel();
            _reattachCts?.Dispose();
            _reattachCts = new CancellationTokenSource();
            var token = _reattachCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try { await Task.Delay(1000, token); }
                    catch (TaskCanceledException) { break; }

                    var found = System.Diagnostics.Process.GetProcesses()
                        .Where(p => p.MainWindowHandle != IntPtr.Zero
                                 && p.MainWindowTitle == targetTitle
                                 && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle))
                        .Select(p => new WindowInfo { Handle = p.MainWindowHandle, ProcessId = p.Id, Title = p.MainWindowTitle })
                        .FirstOrDefault();

                    if (found != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AppendLog($"'{targetTitle}' 창이 다시 감지되었습니다. 자동 연결합니다.");
                            AttachToWindow(found);
                        });
                        break;
                    }
                }
            }, token);
        }

        private void OnTargetWindowExited(object sender, EventArgs e)
        {
            var windowTitle = TargetWindow?.Title ?? "알 수 없는 창";
            _telegramBotService?.BroadcastAsync($"⚠️ 워크래프트 창이 종료되었습니다.\n창 이름: {windowTitle}\n시각: {DateTime.Now:HH:mm:ss}");

            Dispatcher.Invoke(() =>
            {
                AppendLog("워크래프트 창이 종료되었습니다. 실행 중인 매크로를 중지합니다.");

                if (beltMacro != null)
                {
                    beltMacro.StopMacro();
                    beltMacro = null;
                    AppendLog("벨트 매크로 중지됨");
                }

                summoner?.Stop();

                if (backgroundCapture != null)
                {
                    backgroundCapture.StopCapture();
                    backgroundCapture.Dispose();
                    backgroundCapture = null;
                }

                processWatcher?.Stop();
                processWatcher = null;

                Wc3ChatSender.Reset();

                // WC3 자동 기능 중지
                try
                {
                    AutoRG.CancelAsync();
                    AutoStarter.CancelAsync();
                    AutoMouse.CheckOff();
                    MaxRoom.CancelAsync();
                    MinRoom.CancelAsync();
                    Commands.StopDetect();
                }
                catch (Exception ex) { Debug.WriteLine($"[OnTargetWindowExited] WC3 중지 중 오류: {ex.Message}"); }

                UpdateWc3StatusLabel(false);

                TargetWindow = null;
                memoryLabel.Content = "";

                // 같은 이름의 창이 다시 열리면 자동 재연결 (매크로는 멈춘 상태 유지)
                if (!string.IsNullOrEmpty(_lastWindowTitle))
                {
                    AppendLog($"'{_lastWindowTitle}' 창 재연결 대기 중...");
                    StartReattachWatcher(_lastWindowTitle);
                }
            });
        }

        private void InitComposition(float controlsWidth)
        {
            // Create the compositor.
            compositor = new Compositor();

            // Create a target for the window.
            target = compositor.CreateDesktopWindowTarget(hwnd, true);

            // Attach the root visual.
            root = compositor.CreateContainerVisual();
            root.RelativeSizeAdjustment = Vector2.One;
            root.Size = new Vector2(-controlsWidth, 0);
            root.Offset = new Vector3(controlsWidth, 0, 0);
            target.Root = root;

            // Setup the rest of the sample application.
            sample = new BasicSampleApplication(compositor);
            root.Children.InsertAtTop(sample.Visual);
        }

        private List<Int32Rect> LoadRoiAreas()
        {
            var list = new List<Int32Rect>();

            string[] keys = { "Roi_Q", "Roi_W", "Roi_E", "Roi_R", "Roi_A" };
            foreach (var key in keys)
            {
                string value = SettingsManager.Current[key];
                if (!string.IsNullOrEmpty(value))
                {
                    var parts = value.Split(',');
                    var rect = new Int32Rect(
                        int.Parse(parts[0]),
                        int.Parse(parts[1]),
                        int.Parse(parts[2]),
                        int.Parse(parts[3]));
                    list.Add(rect);
                }
            }
            return list;
        }
        private string GetRoiImagePath(int index)
        {
            string[] roiNames = { "Q", "W", "E", "R", "A" };
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"ROI_{roiNames[index]}.png");
        }


        private void InitWindowList()
        {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var processesWithWindows = from p in Process.GetProcesses()
                                           where !string.IsNullOrWhiteSpace(p.MainWindowTitle)
                                           && p.MainWindowTitle.ToLower().Contains("warcraft")
                                           && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                           select new WindowInfo
                                           {
                                               Handle = p.MainWindowHandle,
                                               ProcessId = p.Id,
                                               Title = p.MainWindowTitle
                                           };
                processes = new ObservableCollection<WindowInfo>(processesWithWindows);
                WindowComboBox.ItemsSource = processes;
            }
            else
            {
                WindowComboBox.IsEnabled = false;
            }
        }

        private void StartHwndCapture(IntPtr hwnd)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                //sample.StartCaptureFromItem(item);
            }
        }

        private void StopCapture()
        {
            //sample.StopCapture();
        }

        private async void RoiButton_Click(object sender, RoutedEventArgs e)
        {
            if (TargetWindow == null)
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            var bitmap = await SoftwareBitmapCopy.CaptureSingleFrameAsync(TargetWindow.Handle);

            if (bitmap != null)
            {
                // 비트맵은 Windows.Graphics.Capture가 반환한 물리 픽셀 기준이고,
                // WPF 창 Width/Height는 논리 픽셀 기준이므로 DPI 배율로 나눠야 함.
                // 이렇게 하면 ROI 창이 게임 창과 동일한 화면 크기로 열린다.
                var dpiSource = PresentationSource.FromVisual(this);
                double dpiX = dpiSource?.CompositionTarget.TransformToDevice.M11 ?? 1.0;
                double dpiY = dpiSource?.CompositionTarget.TransformToDevice.M22 ?? 1.0;

                var roiWindow = new ROIWindow(bitmap, new[] { "Q", "W", "E", "R", "A" }, "Roi")
                {
                    Width = bitmap.PixelWidth / dpiX,
                    Height = bitmap.PixelHeight / dpiY
                };

                if (roiWindow.ShowDialog() == true)
                {

                    // 이후 ROI 정보를 저장하거나 비교용으로 사용하면 됩니다.
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_BossSetting_Click(object sender, RoutedEventArgs e)
        {
            if (TargetWindow == null)
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            var bossSetting = new BossSetting();
            bossSetting.ShowDialog();
        }

        private void StopOcrTimer()
        {
            //ocrService?.Stop();
        }

        private void btn_BossStart_Click(object sender, RoutedEventArgs e)
        {
            LoadRoiAreas();
            SettingsManager.Reload();
            if (TargetWindow == null)
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            //StartOcrTimer();
            if (cbb_BossZone.SelectedItem is ComboBoxItem selectedItem)
            {
                summoner.BossZone = selectedItem.Content.ToString();
            }

            summoner.RefreshOcrSettings();

            summoner.BossOrder = txt_BossOrder.Text;

            if (rb_Gold.IsChecked == true)
                SettingsManager.Current.SelectedROI = "gold";
            else if (rb_Tree.IsChecked == true)
                SettingsManager.Current.SelectedROI = "tree";

            summoner.Start();
        }

        private void btn_BossStop_Click(object sender, RoutedEventArgs e)
        {
            if (TargetWindow == null)
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            summoner?.Stop();
        }

        private void cbb_BossZone_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbb_BossZone.SelectedItem is ComboBoxItem item)
            {
                string selectedZone = item.Content.ToString();
                UpdateFilteredStats(selectedZone);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void btnBeltSet_Click(object sender, RoutedEventArgs e)
        {
            if (TargetWindow == null)
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            var beltSetting = new BeltSetting();
            beltSetting.ShowDialog();
        }

        private void btnBeltStart_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Reload(); // 디스크에서 최신 설정 불러오기

            if (TargetWindow == null)
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            if (beltMacro != null)
            {
                AppendLog("벨트 매크로가 이미 실행 중입니다.");
                return;
            }

            beltMacro = new BeltMacro(AppendLog, TargetWindow.Handle);
            beltMacro.StartMacro();
            AppendLog("벨트 매크로 시작");
        }

        private void btnBeltStop_Click(object sender, RoutedEventArgs e)
        {
            if (beltMacro != null)
            {
                beltMacro.StopMacro();
                beltMacro = null;
                AppendLog("벨트 매크로 중지됨");
            }
            else
            {
                AppendLog("벨트 매크로가 실행 중이 아닙니다.");
            }
        }

        // ── 매크로 단축키 키 캡처 ──────────────────────────────────────────────
        private Button _capturingMacroBtn;
        private Action<System.Windows.Forms.Keys> _capturingMacroAction;

        private void StartMacroKeyCapture(Button btn, Action<System.Windows.Forms.Keys> apply)
        {
            _capturingMacroBtn    = btn;
            _capturingMacroAction = apply;
            btn.Content = "▶ 키 입력...";
            this.Focus();
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (_capturingMacroBtn == null) { base.OnPreviewKeyDown(e); return; }

            var wpfKey = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
            var formsKey = (System.Windows.Forms.Keys)
                System.Windows.Input.KeyInterop.VirtualKeyFromKey(wpfKey);

            if (formsKey == System.Windows.Forms.Keys.Delete ||
                formsKey == System.Windows.Forms.Keys.Back)
                formsKey = System.Windows.Forms.Keys.None;

            _capturingMacroAction?.Invoke(formsKey);
            _capturingMacroBtn.Content = MacroKeyDisplayName(formsKey);
            _capturingMacroBtn    = null;
            _capturingMacroAction = null;
            e.Handled = true;
        }

        private static string MacroKeyDisplayName(System.Windows.Forms.Keys key)
        {
            if (key == System.Windows.Forms.Keys.None) return "단축키 (없음)";
            return $"단축키: {key}";
        }

        private void btn_Macro_Belt_Click(object sender, RoutedEventArgs e)
            => StartMacroKeyCapture(btn_Macro_Belt, k => MacroHotkey.BeltKey = k);

        private void btn_Macro_Boss_Click(object sender, RoutedEventArgs e)
            => StartMacroKeyCapture(btn_Macro_Boss, k => MacroHotkey.BossKey = k);

        // 단축키에서 호출 – 실행 중이면 중지, 아니면 시작
        private void ToggleBeltMacro()
        {
            if (beltMacro != null)
            {
                beltMacro.StopMacro();
                beltMacro = null;
                AppendLog("벨트 매크로 중지됨 (단축키)");
            }
            else
            {
                if (TargetWindow == null) return;
                SettingsManager.Reload();
                beltMacro = new BeltMacro(AppendLog, TargetWindow.Handle);
                beltMacro.StartMacro();
                AppendLog("벨트 매크로 시작 (단축키)");
            }
        }

        private void ToggleBossSummoner()
        {
            if (summoner?.IsRunning == true)
            {
                summoner.Stop();
                AppendLog("보스 소환 중지됨 (단축키)");
            }
            else
            {
                if (TargetWindow == null) return;
                LoadRoiAreas();
                SettingsManager.Reload();

                // 버튼 시작과 동일하게 BossZone, BossOrder, OCR 설정 적용
                if (cbb_BossZone.SelectedItem is ComboBoxItem selectedItem)
                    summoner.BossZone = selectedItem.Content.ToString();

                summoner.BossOrder = txt_BossOrder.Text;
                summoner.RefreshOcrSettings();

                if (rb_Gold.IsChecked == true)
                    SettingsManager.Current.SelectedROI = "gold";
                else if (rb_Tree.IsChecked == true)
                    SettingsManager.Current.SelectedROI = "tree";

                summoner?.Start();
                AppendLog("보스 소환 시작 (단축키)");
            }
        }

        private void btnItemMix_Click(object sender, RoutedEventArgs e)
        {
            var mixWindow = new ItemMixWindow();
            mixWindow.Owner = this; // 부모 창 지정 (선택 사항)
            mixWindow.Show();       // 또는 ShowDialog(); 로 모달창으로 열기 가능
        }

        private void btnTelegramSetting_Click(object sender, RoutedEventArgs e)
        {
            var win = new TelegramSettingWindow(_telegramBotService);
            win.Owner = this;
            win.ShowDialog();
        }

        private async void btnSendLock_Click(object sender, RoutedEventArgs e)
        {
            if (TargetWindow == null)
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            var lockChecks = new[]
            {
                (cb: cb_lock1, cmd: "-잠금1"),
                (cb: cb_lock2, cmd: "-잠금2"),
                (cb: cb_lock3, cmd: "-잠금3"),
                (cb: cb_lock4, cmd: "-잠금4"),
                (cb: cb_lock5, cmd: "-잠금5"),
                (cb: cb_lock6, cmd: "-잠금6"),
            };

            foreach (var item in lockChecks)
            {
                if (item.cb.IsChecked == true)
                {
                    bool ok = Wc3ChatSender.SendChatMessage(item.cmd);
                    //AppendLog(ok ? $"{item.cmd} 전송 완료" : Wc3ChatSender.LastError);
                    await Task.Delay(200);
                }
            }
            AppendLog($"잠금 완료");
        }

        private void btnUpgradeCheck_Click(object sender, RoutedEventArgs e)
        {
            if (TargetWindow == null)
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            // 강화 수치 검증
            string levelText = txt_UpgradeLevel.Text.Trim();
            if (!int.TryParse(levelText, out _))
            {
                MessageBox.Show("강화 수치에 숫자를 입력해주세요.");
                return;
            }

            // 부위별 명령어 매핑
            var cmdMap = new[]
            {
                "-UpW",  // 무기    (index 0)
                "-UpA",  // 방어구  (index 1)
                "-UpG",  // 장갑    (index 2)
                "-UpN",  // 악세    (index 3)
                "-UpH",  // 히든    (index 4)
            };

            int idx = cbb_UpgradeType.SelectedIndex;
            if (idx < 0 || idx >= cmdMap.Length) return;

            string cmd = $"{cmdMap[idx]} {levelText}";
            bool ok = Wc3ChatSender.SendChatMessage(cmd);
            AppendLog(ok ? $"강화확인 전송: {cmd}" : Wc3ChatSender.LastError);
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            string targetTitle = TargetWindow?.Title ?? _lastWindowTitle;

            if (string.IsNullOrEmpty(targetTitle))
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            InitWindowList();

            var matchingWindow = processes?.FirstOrDefault(w => w.Title == targetTitle);
            if (matchingWindow != null)
            {
                WindowComboBox.SelectedItem = matchingWindow;
            }
            else
            {
                AppendLog($"같은 이름의 창을 찾을 수 없습니다: {targetTitle}");
                MessageBox.Show($"'{targetTitle}' 창을 찾을 수 없습니다.");
            }
        }

        private void btnGoldTest_Click(object sender, RoutedEventArgs e)
        {
            if (TargetWindow == null)
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            var res = GoldMemoryReader.ReadResources(TargetWindow.ProcessId, out string err);

            if (res != null)
            {
                txtPlayerIndex.Text  = $"P{res.PlayerIndex + 1}";
                txtGoldResult.Text   = $"{res.Gold:N1}";
                txtLumberResult.Text = $"{res.Lumber:N1}";
                AppendLog($"[P{res.PlayerIndex + 1}] 골드: {res.Gold:N1} / 목재: {res.Lumber:N1}");
                AppendLog(res.DebugInfo);
            }
            else
            {
                txtPlayerIndex.Text  = "—";
                txtGoldResult.Text   = "실패";
                txtLumberResult.Text = "실패";
                AppendLog($"자원 읽기 실패: {err}");
            }
        }

        public void UpdateMemoryLabel(string text)
        {
            Dispatcher.Invoke(() =>
            {
                memoryLabel.Content = text;
            });
        }

        private void InitWc3UI()
        {
            // 사이드바 상태 레이블 초기화만 수행
            // 실제 설정은 CirnixSettingWindow에서 관리
        }

        // ── Cirnix 활성화 핸들러 ─────────────────────────────────────────────
        private void chk_Main_CirnixEnabled_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.IsCirnixEnabled = chk_Main_CirnixEnabled.IsChecked == true;
            SettingsManager.Save();
        }

        // ── WC3 실행 버튼 (사이드바) ──────────────────────────────────────────
        private async void btn_LaunchWc3_Main_Click(object sender, RoutedEventArgs e)
        {
            string exePath = SettingsManager.Current.Wc3ExePath?.Trim();
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                MessageBox.Show(
                    "WC3 실행 파일이 설정되지 않았습니다.\nCirnix 설정 > 기타 설정 탭에서 [찾기]로 지정해주세요.",
                    "WC3 실행 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string args = SettingsManager.Current.Wc3LaunchArgs ?? string.Empty;
            btn_LaunchWc3_Main.IsEnabled = false;
            await Wc3.GameModule.LaunchWarcraft3(exePath, args);
            btn_LaunchWc3_Main.IsEnabled = true;
        }

        // ── Cirnix 설정 창 ────────────────────────────────────────────────────
        private CirnixSettingWindow _cirnixWindow;

        private void btn_CirnixSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_cirnixWindow != null && _cirnixWindow.IsLoaded)
            {
                _cirnixWindow.Activate();
                return;
            }
            _cirnixWindow = new CirnixSettingWindow(
                appendLog:             AppendLog,
                updateAutoRGStatus:    null,
                updateAutoStartStatus: null,
                onForceMemRefresh:     () => processWatcher?.ForceRefresh()
            );
            _cirnixWindow.Owner = this;
            _cirnixWindow.Closed += (s, _) =>
            {
                chk_Main_CirnixEnabled.IsChecked = SettingsManager.Current.IsCirnixEnabled;
            };
            _cirnixWindow.Show();
        }

        // ── 캐릭터변경 ──────────────────────────────────────────────────────────
        private void btn_OpenCharChange_Click(object sender, RoutedEventArgs e)
        {
            var w = new CharChangeWindow { Owner = this };
            w.Show();
        }

        private void UpdateWc3StatusLabel(bool connected)
        {
            dot_Wc3Connected.Fill = new System.Windows.Media.SolidColorBrush(
                connected
                    ? System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50)   // 초록
                    : System.Windows.Media.Color.FromRgb(0x60, 0x60, 0x60)); // 회색
            lbl_Wc3Status.Text = connected ? "WC3 연결됨" : "WC3 미연결";
        }
    }
}