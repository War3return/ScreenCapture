using System;
using System.ComponentModel;
using System.Threading.Tasks;

using static epicro.Wc3.Wc3Globals;
using static epicro.Wc3.Memory.Message;

namespace epicro.Wc3.Worker
{
    internal static class Commands
    {
        private static BackgroundWorker Worker;

        static Commands()
        {
            Worker = new BackgroundWorker();
            Worker.DoWork              += new DoWorkEventHandler(Worker_DoWork);
            Worker.RunWorkerCompleted  += new RunWorkerCompletedEventHandler(Worker_RunWorkerCompleted);
        }

        private static async void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!Settings.IsCirnixEnabled) return;
                if (await Actions.ProcessCheck()) return;

                string prefix = GetMessage();
                if (string.IsNullOrEmpty(prefix)) return;

                switch (prefix[0])
                {
                    case '!':
                        UserState = CommandTag.Default;
                        return;
                    case '-':
                        UserState = CommandTag.Chat;
                        return;
                }

                if (UserState == CommandTag.None) return;

                if (prefix[0] == '\0')
                {
                    string[] args;
                    try
                    {
                        args = prefix.Substring(1, prefix.Length - 1)
                                     .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    catch
                    {
                        UserState = CommandTag.None;
                        return;
                    }
                    if (args.Length > 0)
                    {
                        string command = args[0];
                        commandList.Find(item => item.Tag == UserState && item.CompareCommand(command))
                                   ?.Function(args);
                    }
                }
            }
            catch (Exception)
            {
                // 예외 무시
            }
            UserState = CommandTag.None;
        }

        private static async void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            await Task.Delay(200);
            if (_stopRequested)
            {
                _stopRequested = false;
                _loopActive    = false;
                return;
            }
            // IsBusy 체크: 리셋 등으로 이미 새 루프가 시작된 경우 중복 실행 방지
            if (!Worker.IsBusy)
                Worker.RunWorkerAsync();
        }

        internal static void StartDetect()
        {
            // 이미 루프가 실행 중이면 중복 시작 방지
            if (_loopActive) return;
            _loopActive = true;
            if (!Worker.IsBusy) Worker.RunWorkerAsync();
        }

        internal static void StopDetect()
        {
            _stopRequested = true;
        }

        private static volatile bool _stopRequested = false;
        private static volatile bool _loopActive    = false;
    }
}
