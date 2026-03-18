using System;
using System.Runtime.InteropServices;

namespace epicro.Helpers
{
    public class PlayerResources
    {
        public int    PlayerIndex { get; set; }  // 0-based jidx (표시용: +1)
        public double Gold        { get; set; }
        public double Lumber      { get; set; }
        public string DebugInfo   { get; set; }  // 중간 포인터 값 로그
    }

    public static class GoldMemoryReader
    {
        // ── Windows API ──────────────────────────────────────────────
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        private static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll")]
        private static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct MODULEENTRY32
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

        private const uint PROCESS_VM_READ           = 0x0010;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint TH32CS_SNAPMODULE         = 0x00000008;
        private const uint TH32CS_SNAPMODULE32       = 0x00000010;

        // ── 오프셋 상수 ──────────────────────────────────────────────
        // 1. gs         = *(game_dll + 0xD305E0)
        // 2. jidx       = *(gs + 0x28)               [uint16]
        // 3. cpi        = *(gs + jidx*4 + 0x58)      [CPlayer_info 포인터]
        // 4. handle     = *(cpi + 0x58)               [실제 Jass 핸들]
        // 5. ht         = *(*(game_dll + 0xD30448) + 0x0C)
        // 6. player_obj = *(ht + handle*8 + 4)
        // 7. gold/lumber = player_obj + 0x78 / 0xF8  ÷ 10
        private const long GAME_STATE_OFFSET  = 0xD305E0;
        private const long OBJ_MANAGER_OFFSET = 0xD30448;

        // ── 공개 API ─────────────────────────────────────────────────

        /// <summary>
        /// 내 플레이어 번호(jidx), 골드, 목재를 한 번에 반환합니다.
        /// 실패 시 null, 오류 메시지는 errorMsg로 전달됩니다.
        /// </summary>
        public static PlayerResources ReadResources(int processId, out string errorMsg)
        {
            errorMsg = null;

            IntPtr hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                errorMsg = "프로세스 열기 실패 (권한 부족)";
                return null;
            }

            try
            {
                // Step 1. Game.dll 베이스 주소 찾기 (32비트 WoW64 모듈 대응)
                IntPtr gameDllBase = GetModuleBase(processId, "Game.dll");
                if (gameDllBase == IntPtr.Zero)
                {
                    errorMsg = "Game.dll 모듈을 찾을 수 없습니다. 게임이 실행 중인지 확인하세요.";
                    return null;
                }

                long baseAddr = gameDllBase.ToInt64();

                // Step 2. jidx (내 플레이어 인덱스, uint16)
                uint gameState = ReadUInt32(hProcess, new IntPtr(baseAddr + GAME_STATE_OFFSET));
                if (gameState == 0) { errorMsg = "game_state 포인터가 null입니다."; return null; }

                int jidx = ReadUInt16(hProcess, new IntPtr((long)gameState + 0x28));

                // Step 3. CPlayer_info 포인터 → 실제 Jass 핸들
                uint cpi = ReadUInt32(hProcess, new IntPtr((long)gameState + jidx * 4 + 0x58));
                if (cpi == 0) { errorMsg = $"cpi가 null입니다. (jidx={jidx})"; return null; }

                uint handle = ReadUInt32(hProcess, new IntPtr((long)cpi + 0x58));
                if (handle == 0) { errorMsg = $"Jass 핸들이 0입니다. (cpi=0x{cpi:X})"; return null; }

                // Step 4. Handle Table → Player Object
                uint objManager = ReadUInt32(hProcess, new IntPtr(baseAddr + OBJ_MANAGER_OFFSET));
                if (objManager == 0) { errorMsg = "obj_manager 포인터가 null입니다."; return null; }

                uint handleTable = ReadUInt32(hProcess, new IntPtr((long)objManager + 0x0C));
                if (handleTable == 0) { errorMsg = "handle_table 포인터가 null입니다."; return null; }

                uint playerObj = ReadUInt32(hProcess, new IntPtr((long)handleTable + handle * 8 + 4));
                if (playerObj == 0) { errorMsg = $"player_obj가 null입니다. (handle={handle})"; return null; }

                // Step 5. 골드 / 목재 읽기 (raw ÷ 10 = 실제값)
                int goldRaw   = ReadInt32(hProcess, new IntPtr((long)playerObj + 0x78));
                int lumberRaw = ReadInt32(hProcess, new IntPtr((long)playerObj + 0xF8));

                string debug =
                    $"[DBG] gameDllBase=0x{baseAddr:X}\r\n" +
                    $"[DBG] gameState  =0x{gameState:X}\r\n" +
                    $"[DBG] jidx       ={jidx}\r\n" +
                    $"[DBG] cpi        =0x{cpi:X}\r\n" +
                    $"[DBG] handle     ={handle}  (0x{handle:X})\r\n" +
                    $"[DBG] objManager =0x{objManager:X}\r\n" +
                    $"[DBG] handleTable=0x{handleTable:X}\r\n" +
                    $"[DBG] playerObj  =0x{playerObj:X}\r\n" +
                    $"[DBG] goldRaw={goldRaw}  lumberRaw={lumberRaw}";

                return new PlayerResources
                {
                    PlayerIndex = jidx,
                    Gold        = goldRaw   / 10.0,
                    Lumber      = lumberRaw / 10.0,
                    DebugInfo   = debug
                };
            }
            catch (Exception ex)
            {
                errorMsg = $"예외 발생: {ex.Message}";
                return null;
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        // ── 메모리 읽기 헬퍼 ─────────────────────────────────────────

        private static uint ReadUInt32(IntPtr hProcess, IntPtr addr)
        {
            byte[] buf = new byte[4];
            ReadProcessMemory(hProcess, addr, buf, 4, out _);
            return BitConverter.ToUInt32(buf, 0);
        }

        private static int ReadInt32(IntPtr hProcess, IntPtr addr)
        {
            byte[] buf = new byte[4];
            ReadProcessMemory(hProcess, addr, buf, 4, out _);
            return BitConverter.ToInt32(buf, 0);
        }

        private static int ReadUInt16(IntPtr hProcess, IntPtr addr)
        {
            byte[] buf = new byte[2];
            ReadProcessMemory(hProcess, addr, buf, 2, out _);
            return BitConverter.ToUInt16(buf, 0);
        }

        // ── 모듈 베이스 탐색 ─────────────────────────────────────────

        /// <summary>32비트 WoW64 프로세스의 모듈 베이스 주소를 반환합니다.</summary>
        private static IntPtr GetModuleBase(int processId, string moduleName)
        {
            IntPtr hSnap = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, (uint)processId);
            if (hSnap == new IntPtr(-1)) return IntPtr.Zero;
            try
            {
                MODULEENTRY32 me = new MODULEENTRY32 { dwSize = (uint)Marshal.SizeOf(typeof(MODULEENTRY32)) };
                if (Module32First(hSnap, ref me))
                {
                    do
                    {
                        if (me.szModule.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                            return me.modBaseAddr;
                    }
                    while (Module32Next(hSnap, ref me));
                }
            }
            finally
            {
                CloseHandle(hSnap);
            }
            return IntPtr.Zero;
        }
    }
}
