using System;

using static epicro.Wc3.Component;
using static epicro.Wc3.NativeMethods;

namespace epicro.Wc3.Memory
{
    public static class Replay
    {
        private static byte[] SearchPattern = new byte[] { 0x78, 0x60, 0x34, 0x2F, 0x78 };
        internal static IntPtr Offset = IntPtr.Zero;

        private static void GetOffset()
        {
            Offset = SearchAddress(SearchPattern, 0x7FFFFFFF, 4);
            if (Offset != IntPtr.Zero) Offset += 0x25B0;
        }

        public static int PlaySpeed
        {
            get
            {
                int CurrentDelay = -1;
                GetOffset();
                if (Offset != IntPtr.Zero)
                {
                    int num;
                    byte[] buffer = new byte[4];
                    ReadProcessMemory(Warcraft3Info.Handle, Offset, buffer, 4, out num);
                    uint val = BitConverter.ToUInt32(buffer, 0);
                    if (val <= 0x230)
                        CurrentDelay = (int)val;
                }
                return CurrentDelay;
            }
            set
            {
                GetOffset();
                if (Offset == IntPtr.Zero) return;
                int written;
                byte[] bytes = BitConverter.GetBytes(value);
                WriteProcessMemory(Warcraft3Info.Handle, Offset,         bytes, 4, out written);
                WriteProcessMemory(Warcraft3Info.Handle, Offset + 4,     bytes, 4, out written);
                WriteProcessMemory(Warcraft3Info.Handle, Offset + 0x220, bytes, 4, out written);
                WriteProcessMemory(Warcraft3Info.Handle, Offset + 0x224, bytes, 4, out written);
                WriteProcessMemory(Warcraft3Info.Handle, Offset + 0x440, bytes, 4, out written);
                WriteProcessMemory(Warcraft3Info.Handle, Offset + 0x444, bytes, 4, out written);
            }
        }
    }
}
