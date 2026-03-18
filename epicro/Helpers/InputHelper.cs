using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace epicro.Helpers
{
    public static class InputHelper
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion U;
            public static int Size => Marshal.SizeOf(typeof(INPUT));
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;

        public static void SendRealKey(ushort keyCode)
        {
            INPUT[] inputs = new INPUT[2];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = keyCode;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = keyCode;
            inputs[1].U.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput((uint)inputs.Length, inputs, INPUT.Size);
        }

        public static void realKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!KeyCodes.TryGetValue(key.ToUpper(), out int keyCode)) return;
            var hwnd = MainWindow.TargetWindow.Handle;
            if (hwnd == IntPtr.Zero) return;
            SendRealKey((ushort)keyCode);
        }

        public static void SendKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!keyMap.TryGetValue(key.ToUpper(), out int keyCode)) return;

            var hwnd = MainWindow.TargetWindow.Handle;
            if (hwnd == IntPtr.Zero) return;

            SendMessage(hwnd, WM_KEYDOWN, keyCode, 0);
            Thread.Sleep(50);
            SendMessage(hwnd, WM_KEYUP, keyCode, 0);
        }

        public static void SendChar(char ch)
        {
            var hwnd = MainWindow.TargetWindow.Handle;
            if (hwnd == IntPtr.Zero) return;

            PostMessage(hwnd, WM_CHAR, ch, 0);
            Thread.Sleep(50);
        }

        private static readonly Dictionary<string, int> keyMap = new Dictionary<string, int>
        {
            { "Q", 0x51 },
            { "W", 0x57 },
            { "E", 0x45 },
            { "R", 0x52 },
            { "A", 0x41 },
            { "S", 0x53 },
            { "D", 0x44 },
            { "F", 0x46 }
            // 필요한 키코드 추가 가능
        };

        public static readonly Dictionary<string, int> KeyCodes = new Dictionary<string, int>
        {
            // A ~ Z
            { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 }, { "E", 0x45 },
            { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 }, { "I", 0x49 }, { "J", 0x4A },
            { "K", 0x4B }, { "L", 0x4C }, { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F },
            { "P", 0x50 }, { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 },
            { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 }, { "Y", 0x59 }, { "Z", 0x5A },

            // 0 ~ 9 (상단 숫자키)
            { "0", 0x30 }, { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 }, { "4", 0x34 },
            { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 }, { "8", 0x38 }, { "9", 0x39 },

            // F1 ~ F10
            { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 }, { "F5", 0x74 },
            { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 }, { "F9", 0x78 }, { "F10", 0x79 },

            // NumPad 0 ~ 9
            { "NumPad0", 0x60 }, { "NumPad1", 0x61 }, { "NumPad2", 0x62 }, { "NumPad3", 0x63 },
            { "NumPad4", 0x64 }, { "NumPad5", 0x65 }, { "NumPad6", 0x66 }, { "NumPad7", 0x67 },
            { "NumPad8", 0x68 }, { "NumPad9", 0x69 }
        };
    }
}
