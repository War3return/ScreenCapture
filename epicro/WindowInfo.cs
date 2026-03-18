using System;
using System.Diagnostics;

namespace epicro
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public int ProcessId { get; set; }
        public string Title { get; set; }
        public string ClassName { get; set; } // 필요 시

        // 이벤트: 프로세스 종료 시
        public event EventHandler ProcessExited;

        private Process _processMonitor;

        public override string ToString()
        {
            return $"{Title} (PID: {ProcessId})";
        }

        /// <summary>
        /// 현재 설정된 ProcessId 프로세스를 PID 기반으로 모니터링.
        /// 종료되면 ProcessExited 이벤트를 발생시킵니다.
        /// </summary>
        public void StartExitAndRestartMonitoring()
        {
            StopMonitoring();

            try
            {
                _processMonitor = Process.GetProcessById(ProcessId);
                Handle = _processMonitor.MainWindowHandle;

                _processMonitor.EnableRaisingEvents = true;
                _processMonitor.Exited += OnProcessExited;
            }
            catch (ArgumentException)
            {
                // 이미 종료된 상태라면 즉시 종료 처리
                OnProcessExited(this, EventArgs.Empty);
            }
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            // 1) 종료 알림
            ProcessExited?.Invoke(this, EventArgs.Empty);

            // 2) 기존 모니터링 정리
            if (_processMonitor != null)
            {
                _processMonitor.Exited -= OnProcessExited;
                _processMonitor.Dispose();
                _processMonitor = null;
            }
        }

        /// <summary>
        /// 모니터링 중단 및 리소스 정리
        /// </summary>
        public void StopMonitoring()
        {
            if (_processMonitor != null)
            {
                _processMonitor.Exited -= OnProcessExited;
                _processMonitor.Dispose();
                _processMonitor = null;
            }
        }
    }
}
