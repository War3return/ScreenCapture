using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace epicro.Helpers
{
    /// <summary>
    /// WC3 프로세스의 메모리 사용량을 주기적으로 읽어 라벨에 표시합니다.
    /// 실제 메모리 정리는 MemoryOptimizeChecker(HangWatchdog)가 담당합니다.
    /// </summary>
    public class ProcessMemoryWatcher
    {
        private readonly Process _targetProcess;
        private CancellationTokenSource _cts;
        private readonly Action<string> _onUpdateLabel;

        // 라벨 갱신 주기 (30초)
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

        public ProcessMemoryWatcher(Process targetProcess, Action<string> onUpdateLabel)
        {
            _targetProcess = targetProcess;
            _onUpdateLabel = onUpdateLabel;
        }

        public void Start()
        {
            Stop(); // 중복 방지
            _cts = new CancellationTokenSource();
            Task.Run(() => MonitorLoop(_cts.Token));
        }

        public void Stop()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                _cts.Cancel();
        }

        /// <summary>
        /// 현재 메모리 사용량을 즉시 읽어 라벨을 갱신합니다.
        /// </summary>
        public void ForceRefresh()
        {
            try
            {
                if (_targetProcess == null || _targetProcess.HasExited) return;
                _targetProcess.Refresh();
                long mem = _targetProcess.WorkingSet64;
                _onUpdateLabel?.Invoke($"WC3 메모리: {FormatBytes(mem)}");
            }
            catch { }
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_targetProcess.HasExited)
                        break;

                    _targetProcess.Refresh();
                    long mem = _targetProcess.WorkingSet64;
                    _onUpdateLabel?.Invoke($"WC3 메모리: {FormatBytes(mem)}");

                    await Task.Delay(RefreshInterval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[오류] ProcessMemoryWatcher: {ex.Message}");
                    break;
                }
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024):F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }
    }
}
