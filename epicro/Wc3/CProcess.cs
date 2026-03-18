using System;
using System.Threading.Tasks;

using static epicro.Wc3.Component;
using static epicro.Wc3.NativeMethods;

namespace epicro.Wc3
{
    /// <summary>
    /// WC3 프로세스 메모리 최적화 (EmptyWorkingSet) 유틸리티입니다.
    /// </summary>
    public static class CProcess
    {
        /// <summary>
        /// 최적화 전/후 메모리 크기(bytes). [0]=전, [1]=중간(미사용), [2]=후
        /// </summary>
        public static long[] MemoryValue { get; private set; } = new long[3];

        /// <summary>
        /// WC3 프로세스의 워킹셋을 비워 메모리를 줄입니다.
        /// </summary>
        /// <param name="resultDelay">최적화 후 결과를 확인하기 위한 대기 초. 0=즉시</param>
        public static async Task<bool> TrimProcessMemory(int resultDelay = 5)
        {
            try
            {
                if (Warcraft3Info.Process == null || Warcraft3Info.HasExited) return false;

                Warcraft3Info.Process.Refresh();
                MemoryValue[0] = Warcraft3Info.Process.WorkingSet64;

                // Process.Handle은 전체 권한(PROCESS_SET_QUOTA 포함)으로 열림.
                // Warcraft3Info.Handle은 OpenProcess(0x38)으로 열려 PROCESS_SET_QUOTA(0x100)가
                // 없으므로 EmptyWorkingSet이 항상 실패함.
                if (!EmptyWorkingSet(Warcraft3Info.Process.Handle))
                    return false;

                if (resultDelay > 0)
                    await Task.Delay(resultDelay * 1000);

                Warcraft3Info.Process.Refresh();
                MemoryValue[2] = Warcraft3Info.Process.WorkingSet64;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// overload(bool) — 최적화만 실행하고 바로 반환합니다.
        /// </summary>
        public static async Task<bool> TrimProcessMemory(bool silent)
        {
            return await TrimProcessMemory(silent ? 0 : 5);
        }
    }
}
