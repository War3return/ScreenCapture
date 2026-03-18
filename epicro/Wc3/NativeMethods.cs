using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace epicro.Wc3
{
    [Flags]
    internal enum KeyModifiers : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X, Y;
    }

    internal static class NativeMethods
    {
        // ── Memory / Process ──────────────────────────────────────────────
        [DllImport("psapi", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EmptyWorkingSet([In] IntPtr hWnd);

        [DllImport("user32")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32", SetLastError = true)]
        internal static extern IntPtr GetWindowRect([In] IntPtr hWnd, [Out] out RECT rect);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadProcessMemory(
            [In] IntPtr hProcess,
            [In] IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            [In] int dwSize,
            [Out] out int lpNumberOfBytesRead);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VirtualProtectEx(
            [In] IntPtr hProcess,
            [In] IntPtr lpAddress,
            [In] int dwSize,
            [In] uint flNewProtect,
            [Out] out uint lpflOldProtect);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WriteProcessMemory(
            [In] IntPtr hProcess,
            [In] IntPtr lpBaseAddress,
            [In] byte[] lpBuffer,
            [In] int nSize,
            [Out] out int lpNumberOfBytesWritten);

        [DllImport("kernel32", SetLastError = true)]
        internal static extern IntPtr OpenProcess(
            [In] uint dwDesiredAccess,
            [In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            [In] uint dwProcessId);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle([In] IntPtr hObject);

        [DllImport("ntdll.dll")]
        internal static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref Component.PROCESS_BASIC_INFORMATION ProcessInformation,
            uint processInformationLength,
            IntPtr returnLength);

        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern IntPtr CommandLineToArgvW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine,
            out int pNumArgs);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualQueryEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            out Component.MEMORY_BASIC_INFORMATION lpBuffer,
            int dwLength);

        // ── Module Enumeration (32-bit in 64-bit host) ─────────────────────
        [DllImport("kernel32.dll")]
        internal static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        internal static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll")]
        internal static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct MODULEENTRY32
        {
            public uint   dwSize;
            public uint   th32ModuleID;
            public uint   th32ProcessID;
            public uint   GlblcntUsage;
            public uint   ProccntUsage;
            public IntPtr modBaseAddr;
            public uint   modBaseSize;
            public IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }

        // ── Input ─────────────────────────────────────────────────────────
        [DllImport("user32")]
        internal static extern void mouse_event(
            [In] uint dwFlags,
            [In] uint dx,
            [In] uint dy,
            [In] uint dwData,
            [In] uint dwExtraInfo);

        [DllImport("user32")]
        internal static extern void keybd_event(
            [In] byte bVk,
            [In] byte bScan,
            [In] uint dwFlags,
            [In] uint dwExtraInfo);

        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostMessage(
            [In] IntPtr hWnd,
            [In] uint Msg,
            [In] uint wParam,
            [In] uint lParam);

        [DllImport("user32")]
        internal static extern int GetCursorPos([Out] out POINT point);

        [DllImport("user32")]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifiers fsModifiers, Keys vk);

        [DllImport("user32")]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // ── Keyboard Hook ─────────────────────────────────────────────────
        internal delegate IntPtr LowLevelKeyboardProc(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct KBDLLHOOKSTRUCT
        {
            public Keys vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
