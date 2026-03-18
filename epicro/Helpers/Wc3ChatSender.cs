using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace epicro.Helpers
{
    /// <summary>
    /// WC3 채팅 버퍼에 직접 메모리 쓰기 방식으로 텍스트를 전송합니다.
    /// Cirnix의 GameModule.GetOffset() + Message.MessageCut() 방식을 그대로 이식.
    /// </summary>
    public static class Wc3ChatSender
    {
        // === P/Invoke ===
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        [DllImport("user32", SetLastError = true)]
        private static extern bool PostMessage(
            IntPtr hWnd, uint Msg, uint wParam, uint lParam);

        // Cirnix: Message.MessageSearchPattern
        private static readonly byte[] MessageSearchPattern = { 0x94, 0x28, 0x49, 0x65, 0x94 };

        private static IntPtr _processHandle     = IntPtr.Zero;
        private static IntPtr _mainWindowHandle  = IntPtr.Zero;
        private static Process _process          = null;

        // Cirnix 변수명 그대로 유지
        private static IntPtr StormDllOffset = IntPtr.Zero;
        private static IntPtr CEditBoxOffset = IntPtr.Zero;
        private static IntPtr MessageOffset  = IntPtr.Zero;

        /// <summary>마지막 오류 메시지 (성공 시 빈 문자열)</summary>
        public static string LastError { get; private set; } = "";

        // === Public API ===

        public static void Initialize(Process process, IntPtr mainWindowHandle)
        {
            _process          = process;
            _processHandle    = process.Handle;
            _mainWindowHandle = mainWindowHandle;
            StormDllOffset    = IntPtr.Zero;
            CEditBoxOffset    = IntPtr.Zero;
            MessageOffset     = IntPtr.Zero;
            LastError         = "";
        }

        public static void Reset()
        {
            _process          = null;
            _processHandle    = IntPtr.Zero;
            _mainWindowHandle = IntPtr.Zero;
            StormDllOffset    = IntPtr.Zero;
            CEditBoxOffset    = IntPtr.Zero;
            MessageOffset     = IntPtr.Zero;
            LastError         = "";
        }

        /// <summary>
        /// WC3 채팅 버퍼에 텍스트를 직접 쓰고 Enter를 눌러 전송합니다.
        /// </summary>
        public static bool SendChatMessage(string text)
        {
            if (_processHandle == IntPtr.Zero)
            {
                LastError = "[Wc3ChatSender] 프로세스 핸들 없음 — 창이 선택됐는지 확인";
                return false;
            }
            if (string.IsNullOrEmpty(text))
            {
                LastError = "[Wc3ChatSender] 전송할 텍스트가 비어있음";
                return false;
            }

            // Cirnix MessageCut: CEditBoxOffset 없으면 탐색
            if (CEditBoxOffset == IntPtr.Zero)
            {
                if (!GetOffset())
                    return false; // LastError는 GetOffset에서 설정
            }

            // Cirnix MessageCut: UTF-8로 채팅 버퍼에 직접 기록
            byte[] bytes  = Encoding.UTF8.GetBytes(text);
            byte[] buffer = new byte[bytes.Length + 1]; // null terminator
            Array.Copy(bytes, buffer, bytes.Length);

            if (!WriteProcessMemory(_processHandle, MessageOffset, buffer, buffer.Length, out _))
            {
                int err = Marshal.GetLastWin32Error();
                LastError      = $"[Wc3ChatSender] WriteProcessMemory 실패 — Win32: {err}, 오프셋: 0x{MessageOffset.ToInt64():X8}";
                CEditBoxOffset = IntPtr.Zero; // 다음 호출 시 재탐색
                return false;
            }

            // Cirnix ApplyChat(TryHide=false) 방식:
            // Sleep 없이 Enter 두 번을 연속으로 PostMessage 큐에 쌓아두면
            // WC3가 한 프레임 내에 open→send를 처리해서 채팅창이 화면에 보이지 않음
            PostMessage(_mainWindowHandle, 0x100, 13, 0); // WM_KEYDOWN VK_RETURN (open)
            PostMessage(_mainWindowHandle, 0x101, 13, 0); // WM_KEYUP   VK_RETURN
            PostMessage(_mainWindowHandle, 0x100, 13, 0); // WM_KEYDOWN VK_RETURN (send)
            PostMessage(_mainWindowHandle, 0x101, 13, 0); // WM_KEYUP   VK_RETURN
            Thread.Sleep(50);

            LastError = "";
            return true;
        }

        // === Private: Cirnix의 GameModule.GetOffset() + Message.GetOffset() ===

        /// <summary>
        /// Cirnix: GameModule.GetOffset() → storm.dll 베이스 주소 획득
        ///         Message.GetOffset()    → FollowPointer로 CEditBoxOffset 탐색
        /// </summary>
        private static bool GetOffset()
        {
            // 1단계: Process.Modules에서 storm.dll 베이스 주소 획득
            StormDllOffset = IntPtr.Zero;
            try
            {
                foreach (ProcessModule module in _process.Modules)
                {
                    if (module.ModuleName.Equals("Storm.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        StormDllOffset = module.BaseAddress;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = $"[Wc3ChatSender] Process.Modules 열거 실패: {ex.Message}";
                return false;
            }

            if (StormDllOffset == IntPtr.Zero)
            {
                LastError = "[Wc3ChatSender] storm.dll을 찾지 못함 — WC3가 완전히 로드됐는지 확인";
                return false;
            }

            // 2단계: FollowPointer로 CEditBoxOffset 탐색
            //        Cirnix: FollowPointer(StormDllOffset + 0x58280, MessageSearchPattern)
            CEditBoxOffset = FollowPointer(StormDllOffset + 0x58280, MessageSearchPattern);
            if (CEditBoxOffset == IntPtr.Zero)
            {
                LastError = $"[Wc3ChatSender] 포인터 체인 추적 실패 — StormDll: 0x{StormDllOffset.ToInt64():X8}";
                return false;
            }

            // 3단계: 채팅 버퍼 주소 = CEditBoxOffset + 0x88
            MessageOffset = CEditBoxOffset + 0x88;
            return true;
        }

        // === Private: Cirnix의 FollowPointer / Bring 그대로 이식 ===

        /// <summary>
        /// Cirnix Component.FollowPointer — 연결 리스트를 따라가며 시그니처를 탐색합니다.
        /// 각 노드 구조: [4바이트 다음 포인터][시그니처 바이트들...]
        /// </summary>
        private static IntPtr FollowPointer(IntPtr offset, byte[] signature)
        {
            const int MaxIterations = 1000;

            // StormDllOffset + 0x58280에서 첫 번째 포인터 읽기
            byte[] buf = Bring(offset, 4);
            if (buf == null) return IntPtr.Zero;

            // 32비트 WC3 주소이므로 uint로 읽어 sign-extension 방지
            offset = new IntPtr((long)(uint)BitConverter.ToInt32(buf, 0));

            for (int i = 0; i < MaxIterations; i++)
            {
                buf = Bring(offset, 4 + signature.Length);
                if (buf == null) return IntPtr.Zero;

                // bytes[4..4+len] 이 시그니처와 일치하면 이 노드가 CEditBox
                bool match = true;
                for (int j = 0; j < signature.Length; j++)
                {
                    if (buf[4 + j] != signature[j]) { match = false; break; }
                }
                if (match) return offset;

                // 다음 노드로 이동
                int nextRaw = BitConverter.ToInt32(buf, 0);
                if (nextRaw == 0) return IntPtr.Zero;
                offset = new IntPtr((long)(uint)nextRaw);
            }

            LastError = "[Wc3ChatSender] FollowPointer 최대 반복 횟수 초과 — 포인터 체인이 비정상적입니다";
            return IntPtr.Zero;
        }

        /// <summary>
        /// Cirnix Component.Bring — 프로세스 메모리에서 size바이트를 읽습니다.
        /// </summary>
        private static byte[] Bring(IntPtr offset, int size)
        {
            byte[] buffer = new byte[size];
            if (!ReadProcessMemory(_processHandle, offset, buffer, size, out int read) || read != size)
                return null;
            return buffer;
        }
    }
}
