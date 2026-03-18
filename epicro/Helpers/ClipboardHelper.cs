using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace epicro.Helpers
{
    public class ClipboardHelper
    {
        private const int  WM_PASTE        = 0x0302;
        private const uint CF_UNICODETEXT  = 13;
        private const uint GMEM_MOVEABLE   = 0x0002;

        [DllImport("user32.dll")]  static extern bool   OpenClipboard(IntPtr hWnd);
        [DllImport("user32.dll")]  static extern bool   CloseClipboard();
        [DllImport("user32.dll")]  static extern bool   EmptyClipboard();
        [DllImport("user32.dll")]  static extern IntPtr SetClipboardData(uint format, IntPtr hMem);
        [DllImport("user32.dll")]  static extern bool   IsClipboardFormatAvailable(uint format);
        [DllImport("user32.dll")]  static extern IntPtr GetClipboardData(uint format);
        [DllImport("user32.dll")]  static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")] static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
        [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")] static extern bool   GlobalUnlock(IntPtr hMem);
        [DllImport("kernel32.dll")] static extern IntPtr GlobalFree(IntPtr hMem);

        /// <summary>
        /// Win32 API로 클립보드에 유니코드 텍스트를 씁니다.
        /// WPF Clipboard.SetText의 CLIPBRD_E_CANT_OPEN 오류를 우회합니다.
        /// </summary>
        public static bool SetText(string text)
        {
            byte[] bytes  = Encoding.Unicode.GetBytes(text + "\0");
            IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);
            if (hGlobal == IntPtr.Zero) return false;

            IntPtr ptr = GlobalLock(hGlobal);
            if (ptr == IntPtr.Zero) { GlobalFree(hGlobal); return false; }
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            GlobalUnlock(hGlobal);

            for (int i = 0; i < 15; i++)
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    EmptyClipboard();
                    SetClipboardData(CF_UNICODETEXT, hGlobal);
                    CloseClipboard();
                    return true;   // OS가 hGlobal 소유권 인수 → 해제 불필요
                }
                Thread.Sleep(20);
            }

            GlobalFree(hGlobal);
            return false;
        }

        public static void SendClipboardTextToWindow(IntPtr hwnd)
        {
            SendMessage(hwnd, WM_PASTE, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
