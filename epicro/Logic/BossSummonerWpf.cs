using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tesseract;
using Windows.Media.Ocr;
using epicro.Helpers;
using epicro.Models;
using System.Windows.Forms;
using SharpDX.Direct3D11;
using Composition.WindowsRuntimeHelpers;
using OpenCvSharp;
using Windows.Media.Capture;
using System.Collections.ObjectModel;
using Windows.Networking;

namespace epicro.Logic
{
    public class BossSummonerWpf
    {
        private CancellationTokenSource cts;
        private readonly Action<string> log;
        private OcrService ocrService;
        private volatile bool isRunning;
        private int totalWood = 0;
        private DateTime startTime;
        private Dictionary<string, BossConfig> _cachedBossConfigs;

        private ObservableCollection<BossStats> _bossStatsList;
        private readonly Action<int, double> updateWoodCallback;
        private System.Timers.Timer elapsedTimer;

        public string BossOrder { get; set; }
        public string BossZone { get; set; }
        public bool IsRunning => isRunning;

        public BossSummonerWpf(Action<string> log, ObservableCollection<BossStats> bossStatsList, Action<int, double> updateWoodCallback)
        {
            SettingsManager.Reload();
            isRunning = false;
            this.log = log;
            //this.bossConfigs = BossConfigManager.GetBossConfigs();
            this.ocrService = new OcrService(() =>
            {
                var hwnd = MainWindow.TargetWindow?.Handle ?? IntPtr.Zero;

                if (WindowEnumerationHelper.IsWindowMinimized(hwnd))
                {
                    //log("[INFO] 대상 창이 최소화됨 → 자동 복원 시도");
                    WindowEnumerationHelper.RestoreWindow(hwnd);
                    Thread.Sleep(500); // 복원 후 안정 대기
                }

                return MainWindow.backgroundCapture?.GetSafeTextureCopy();
            }, MainWindow.ocrEngine);
            ocrService.RefreshFilterSettings();
            this._bossStatsList = bossStatsList;
            this.updateWoodCallback = updateWoodCallback; // 콜백 저장
        }

        public void Start()
        {
            if (isRunning)
            {
                log("[보스소환] 이미 실행 중입니다");
                return;
            }

            // cts가 남아있는 경우(Run 조기 종료로 인한 stuck 상태) 정리 후 재시작
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            cts = new CancellationTokenSource();
            Task.Run(() => Run(cts.Token));
            log("[보스소환] 자동 소환 시작됨");
        }

        public void Stop()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();   // Dispose 누락 수정
                cts = null;
            }

            isRunning = false;
            log("[보스소환] 자동 소환 중지됨");

            if (elapsedTimer != null)
            {
                elapsedTimer.Stop();
                elapsedTimer.Dispose();
                elapsedTimer = null;
            }
            _cachedBossConfigs = null;
        }

        private void Run(CancellationToken token)
        {
            //----------------------------------------------------초기화
            totalWood = 0;

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                updateWoodCallback?.Invoke(totalWood, 0);
            });

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                foreach (var boss in _bossStatsList)
                {
                    boss.ResetStats(); // 통계만 초기화 (이름은 유지)
                }
            });

            startTime = DateTime.Now;
            _cachedBossConfigs = BossConfigManager.GetBossConfigs();
            StartElapsedTimer();
            isRunning = true;
            var zonesToBosses = BossConfigManager.GetZonesToBosses();
            //----------------------------------------------------------

            var order = BossOrder?.ToUpper().ToCharArray();
            if (order == null || order.Length == 0)
            {
                log("[보스소환] 소환 순서가 비어있습니다");
                elapsedTimer?.Stop();
                elapsedTimer?.Dispose();
                elapsedTimer = null;
                isRunning = false;
                return;
            }

            if (!zonesToBosses.ContainsKey(BossZone))
            {
                log($"[보스소환] '{BossZone}' 구역을 찾을 수 없습니다.");
                elapsedTimer?.Stop();
                elapsedTimer?.Dispose();
                elapsedTimer = null;
                isRunning = false;
                return;
            }
            List<string> bossesInZone = zonesToBosses[BossZone];
            List<string> orderedBosses = new List<string>();
            foreach (char bossKey in order)
            {
                // 2단계: 해당 보스 키를 가진 보스를 찾아서 그 보스를 순서대로 매핑
                foreach (var boss in bossesInZone)
                {
                    if (_cachedBossConfigs.ContainsKey(boss) && _cachedBossConfigs[boss].Key == bossKey.ToString())
                    {
                        orderedBosses.Add(boss);  // 순서대로 보스를 추가
                        break;  // 한 번에 하나의 보스를 찾으면 그 보스만 추가하고 빠져나옴
                    }
                }
            }
            string selectedROI = SettingsManager.Current["SelectedROI"];
            bool watchGold = (selectedROI == "gold");
            string checkRoi = watchGold ? "Roi_Gold" : "Roi_Tree";

            // ── 자원 인식 방식 결정 ──────────────────────────────────
            bool isMemoryMode = SettingsManager.Current.ResourceDetectionMode == "Memory";
            int processId = MainWindow.TargetWindow?.ProcessId ?? 0;

            // 자원 값 읽기 함수 (OCR / 메모리 공통 인터페이스)
            Func<int> readValue = isMemoryMode
                ? (Func<int>)(() =>
                {
                    if (processId == 0) return -1;
                    var res = GoldMemoryReader.ReadResources(processId, out _);
                    if (res == null) return -1;
                    return watchGold ? (int)res.Gold : (int)res.Lumber;
                })
                : (Func<int>)(() => ocrService.ReadCurrentValue(checkRoi));

            log(isMemoryMode ? "[보스소환] 자원 인식: 메모리 모드" : "[보스소환] 자원 인식: OCR 모드");

            // 초기 값 설정
            int previousValue = readValue();
            if (previousValue == -1)
            {
                log("[ERROR] 숫자 초기 값을 가져올 수 없습니다.");
                isRunning = false;
                elapsedTimer?.Stop();
                elapsedTimer?.Dispose();
                elapsedTimer = null;
                return;
            }
            while (isRunning)
            {
                try
                {
                    foreach (var bossName in orderedBosses)
                    {
                        if (token.IsCancellationRequested) return;

                        if (_cachedBossConfigs.ContainsKey(bossName) && IsBossMatched(_cachedBossConfigs[bossName]))
                        {
                            SummonBoss(bossName);

                            int startValue = previousValue;
                            DateTime bossStartTime = DateTime.Now;
                            bool valueChanged = false;

                            while ((DateTime.Now - bossStartTime).TotalSeconds < 300)
                            {
                                if (token.IsCancellationRequested) return;
                                int currentValue = readValue();
                                if (currentValue == -1) { Thread.Sleep(500); continue; }

                                if (currentValue != startValue)
                                {
                                    //log($"[INFO] 자원 변동 감지 ({(watchGold ? "골드" : "목재")}): {startValue} → {currentValue}");
                                    previousValue = currentValue;
                                    valueChanged = true;

                                    var killTime = DateTime.Now - bossStartTime;
                                    OnBossKilled(bossName, killTime);
                                    break;
                                }
                                Thread.Sleep(500);
                            }
                            if (!valueChanged)
                            {
                                if (token.IsCancellationRequested) return;
                                log("[WARNING] 5분 동안 자원 변동이 없어 다음 보스를 찾습니다");
                            }
                            break;
                        }
                    }
                    Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    log($"[ERROR] 보스 소환 중 예외 발생: {ex.Message}");
                    log($"[ERROR] 스택 트레이스: {ex.StackTrace}");
                    break;
                }
            }

            // while 루프 종료 후 정리 (break 또는 isRunning=false로 탈출)
            isRunning = false;
            elapsedTimer?.Stop();
            elapsedTimer?.Dispose();
            elapsedTimer = null;
            _cachedBossConfigs = null;
        }
        private void SummonBoss(string bossKey)
        {
            Keys key = GetBossKey(bossKey, _cachedBossConfigs);
            //Console.WriteLine($"[INFO] {bossKey} 소환 - {key}");

            InputHelper.SendKey(key.ToString());
        }
        private bool IsBossMatched(BossConfig config)
        {
            try
            {
                //log("이미지매치 실행");
                // 1. GPU 프레임 → Texture2D 복사
                using (var texture = MainWindow.backgroundCapture.GetSafeTextureCopy())
                {
                    if (texture == null)
                    {
                        //log("[WARNING] Texture 복사 실패 (LatestFrameTexture가 null)");
                        return false;
                    }

                    // 2. Texture2D → Bitmap
                    using (var bitmap = Direct3D11Helper.ExtractBitmapFromTexture(texture))
                    {
                        // 3. Bitmap → Mat
                        using (var mat = BossMatcher.Convert(bitmap))
                        {
                            string imageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                            return BossMatcher.MatchBossByRoi(mat, config, imageFolder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log($"[ERROR] 보스 매칭 중 예외 발생: {ex.Message}");
                return false;
            }
        }
        // 보스 키 매핑
        private Keys GetBossKey(string bossName, Dictionary<string, BossConfig> bossConfigs)
        {

            if (bossConfigs.ContainsKey(bossName)
             && Enum.TryParse(bossConfigs[bossName].Key, out Keys key))
                return key;
            return Keys.None;
        }

        void OnBossKilled(string bossName, TimeSpan killTime)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                var boss = _bossStatsList.FirstOrDefault(b => b.Name == bossName);
                if (boss == null)
                {
                    boss = new BossStats { Name = bossName };
                    _bossStatsList.Add(boss);
                }
                boss.AddKill(killTime);

                // 총 처치수 카드 즉시 갱신
                var mainWindow = System.Windows.Application.Current?.MainWindow as MainWindow;
                if (mainWindow?.txtTotalKills != null)
                    mainWindow.txtTotalKills.Text = mainWindow.AllBossStats.Sum(b => b.KillCount).ToString("N0");
            });

            if (_cachedBossConfigs != null && _cachedBossConfigs.TryGetValue(bossName, out var config))
            {
                totalWood += config.Tree;
                var elapsed = (DateTime.Now - startTime).TotalHours;
                var woodPerHour = elapsed > 0 ? totalWood / elapsed : 0;

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    updateWoodCallback?.Invoke(totalWood, woodPerHour);
                });
            }
        }
        private void StartElapsedTimer()
        {
            elapsedTimer = new System.Timers.Timer(1000); // 1초마다
            elapsedTimer.Elapsed += (s, e) =>
            {
                var elapsed = DateTime.Now - startTime;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                    if (mainWindow?.txtElapsedTime == null) return;
                    int days = (int)elapsed.TotalDays;
                    mainWindow.txtElapsedTime.Text = days > 0
                        ? $"{days}:{elapsed.Hours:D2}:{elapsed.Minutes:D2}"
                        : $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}";
                });
            };
            elapsedTimer.Start();
        }

        public void RefreshOcrSettings()
        {
            ocrService?.RefreshFilterSettings();
        }
        private Rectangle ParseROI(string roiValue)
        {
            string[] parts = roiValue.Split(',');
            if (parts.Length == 4 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y) && int.TryParse(parts[2], out int width) && int.TryParse(parts[3], out int height))
            {
                return new Rectangle(x, y, width, height);
            }
            return new Rectangle(0, 0, 0, 0);
        }
    }
}