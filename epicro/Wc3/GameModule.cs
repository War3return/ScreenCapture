using System;
using System.Diagnostics;
using System.Threading.Tasks;

using static epicro.Wc3.Component;
using static epicro.Wc3.NativeMethods;

namespace epicro.Wc3
{
    /// <summary>
    /// WC3 프로세스의 Game.dll / Storm.dll 베이스 주소를 탐색하고 관리합니다.
    /// </summary>
    public static class GameModule
    {
        private const uint TH32CS_SNAPMODULE   = 0x00000008;
        private const uint TH32CS_SNAPMODULE32 = 0x00000010;

        /// <summary>
        /// 현재 연결된 WC3 프로세스에서 Game.dll, Storm.dll 베이스 주소를 찾습니다.
        /// </summary>
        public static bool GetOffset()
        {
            if (Warcraft3Info.Process == null) return false;

            uint pid = Warcraft3Info.ID;
            IntPtr snap = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, pid);
            if (snap == IntPtr.Zero) return false;

            try
            {
                MODULEENTRY32 me = new MODULEENTRY32 { dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MODULEENTRY32)) };

                if (!Module32First(snap, ref me)) return false;

                GameDllOffset = IntPtr.Zero;
                StormDllOffset = IntPtr.Zero;

                do
                {
                    string name = me.szModule;
                    if (name.IndexOf("game", StringComparison.OrdinalIgnoreCase) != -1)
                        GameDllOffset = me.modBaseAddr;
                    else if (name.IndexOf("storm", StringComparison.OrdinalIgnoreCase) != -1)
                        StormDllOffset = me.modBaseAddr;
                }
                while (Module32Next(snap, ref me));

                return GameDllOffset != IntPtr.Zero && StormDllOffset != IntPtr.Zero;
            }
            finally
            {
                CloseHandle(snap);
            }
        }

        /// <summary>
        /// WC3 프로세스가 살아 있는지 확인합니다.
        /// </summary>
        public static bool WarcraftCheck() => !Warcraft3Info.HasExited;

        /// <summary>
        /// 에피크로에서는 사용자가 직접 창을 선택하므로, 이 메서드는 현재 상태만 확인합니다.
        /// </summary>
        public static WarcraftState InitWarcraft3Info()
        {
            if (Warcraft3Info.Process == null) return WarcraftState.Closed;
            if (Warcraft3Info.HasExited) return WarcraftState.Closed;
            return WarcraftState.OK;
        }

        /// <summary>
        /// 지정된 exe 경로와 인수로 WC3를 실행합니다.
        /// </summary>
        public static async Task LaunchWarcraft3(string exePath, string args)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName         = exePath,
                    Arguments        = args ?? string.Empty,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(exePath)
                });
                await Task.Delay(1000);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[GameModule.LaunchWc3] {ex.Message}"); }
        }

        /// <summary>
        /// WC3를 재시작합니다.
        /// </summary>
        public static async Task StartWarcraft3(string installPath, int windowState)
        {
            try
            {
                string args;
                switch (windowState)
                {
                    case 0: args = "-windows"; break;
                    case 2: args = "-nativefullscr"; break;
                    case 3: args = "-opengl"; break;
                    default: args = string.Empty; break;
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = System.IO.Path.Combine(installPath, "war3.exe"),
                    Arguments = args,
                    WorkingDirectory = installPath
                });
                await Task.Delay(1000);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[GameModule.StartWar3] {ex.Message}"); }
        }
    }
}
