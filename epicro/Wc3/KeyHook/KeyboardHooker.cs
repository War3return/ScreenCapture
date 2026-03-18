using System;
using System.Diagnostics;
using System.Windows.Forms;

using static epicro.Wc3.Wc3Globals;
using static epicro.Wc3.NativeMethods;
using static epicro.Wc3.Memory.States;

namespace epicro.Wc3.KeyHook
{
    public static class KeyboardHooker
    {
        private static LowLevelKeyboardProc _proc   = HookCallback;
        private static IntPtr               _HookID = IntPtr.Zero;
        private static Stopwatch            Timer   = Stopwatch.StartNew();
        private static Keys                 LastDownKey;

        private const int
            WM_KEYDOWN   = 0x100,
            WM_KEYUP     = 0x101,
            WM_SYSKEYDOWN = 0x104,
            WM_SYSKEYUP  = 0x105;

        private static bool ForegroundWar3()
            => GetForegroundWindow() == Component.Warcraft3Info.MainWindowHandle;

        private static IntPtr HookCallback(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (nCode >= 0 && ForegroundWar3() && Settings.IsCirnixEnabled)
            {
                bool isChatBoxOpen = IsChatBoxOpen;

                switch (wParam)
                {
                    case WM_KEYDOWN:   goto KEYDOWN;
                    case WM_KEYUP:
                    case WM_SYSKEYDOWN:
                    case WM_SYSKEYUP:  goto RETURN;
                }

            KEYDOWN:
                if (isChatBoxOpen)
                {
                    if (Settings.IsCommandHide)
                    {
                        try
                        {
                            if (lParam.vkCode == Keys.Enter)
                            {
                                string msg = Memory.Message.GetMessage();
                                if (msg != null && msg.Length >= 0 && msg[0] == '!')
                                    Memory.Message.SetEmpty();
                            }
                        }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[KeyboardHooker] 명령 숨김 처리 오류: {ex.Message}"); }
                    }
                    goto RETURN;
                }
                else
                {
                    Keys vkCode = lParam.vkCode;
                    var hotkey = hotkeyList.Find(item => item.vk == vkCode);
                    if (hotkey != null && !(hotkey.onlyInGame && !IsInGame))
                    {
                        if (Timer.ElapsedMilliseconds >= 65 || vkCode != LastDownKey)
                        {
                            Timer.Restart();
                            LastDownKey = vkCode;
                            hotkey.function(hotkey.fk);
                            if (!hotkey.recall)
                                return (IntPtr)1;
                        }
                    }
                    goto RETURN;
                }

            }
        RETURN:
            return CallNextHookEx(_HookID, nCode, wParam, ref lParam);
        }

        public static void HookStart()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
                _HookID = SetWindowsHookEx(0xD, _proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        public static void HookEnd()
        {
            UnhookWindowsHookEx(_HookID);
        }
    }
}
