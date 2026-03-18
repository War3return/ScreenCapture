using System.Media;
using System.Threading;

using static epicro.Wc3.Memory.Message;
using static epicro.Wc3.Memory.States;

namespace epicro.Wc3.Worker
{
    internal static class MinRoom
    {
        private static readonly System.Threading.Timer Timer;
        private static readonly HangWatchdog Worker;
        private static int MinCount;
        internal static bool IsRunning { get; private set; } = false;

        static MinRoom()
        {
            Worker = new HangWatchdog(0, 0, 0);
            Worker.Condition = () => IsRunning && MinCount >= PlayerCount;
            Worker.Actions += DoActions;

            Timer = new System.Threading.Timer(state => Worker.Check());
        }

        internal static void RunWorkerAsync(int count)
        {
            if (IsRunning || count == 0) return;
            Timer.Change(0, 500);
            IsRunning = true;
            MinCount = count;
            Worker.Check();
        }

        internal static void CancelAsync()
        {
            if (!IsRunning) return;
            Worker.Reset();
            Timer.Change(Timeout.Infinite, Timeout.Infinite);
            IsRunning = false;
            MinCount = 0;
        }

        private static void DoActions()
        {
            try
            {
                CancelAsync();
                SendMsg(true, $"'{MinCount}'명 이하가 되었습니다.");
                System.Media.SystemSounds.Exclamation.Play();
            }
            catch
            {
                SendMsg(true, "실행 도중 문제가 발생했습니다.");
            }
        }
    }
}
