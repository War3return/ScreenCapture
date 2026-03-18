using System.Text;
using System.Threading.Tasks;

using static epicro.Wc3.Component;
using static epicro.Wc3.NativeMethods;

namespace epicro.Wc3.Memory
{
    public static class Join
    {
        public static async void RoomJoin(string roomname)
        {
            if (roomname.Length == 0) return;
            PostMessage(Warcraft3Info.MainWindowHandle, 0x100, 18, 0);
            PostMessage(Warcraft3Info.MainWindowHandle, 0x100, 71, 0);
            PostMessage(Warcraft3Info.MainWindowHandle, 0x101, 71, 0);
            PostMessage(Warcraft3Info.MainWindowHandle, 0x101, 18, 0);
            await Task.Delay(3000);

            if (Message.GetOffset())
            {
                byte[] buffer = Encoding.UTF8.GetBytes(roomname.Trim());
                WriteProcessMemory(Warcraft3Info.Handle, Message.CEditBoxOffset + 0x6E8, buffer, buffer.Length + 1, out _);
                PostMessage(Warcraft3Info.MainWindowHandle, 0x100, 13, 0);
                PostMessage(Warcraft3Info.MainWindowHandle, 0x101, 13, 0);
            }
        }

        public static async void RoomCreate(string roomname)
        {
            if (roomname.Length == 0) return;
            PostMessage(Warcraft3Info.MainWindowHandle, 0x100, 18, 0);
            PostMessage(Warcraft3Info.MainWindowHandle, 0x100, 71, 0);
            PostMessage(Warcraft3Info.MainWindowHandle, 0x101, 71, 0);
            PostMessage(Warcraft3Info.MainWindowHandle, 0x101, 18, 0);
            await Task.Delay(3000);
            PostMessage(Warcraft3Info.MainWindowHandle, 0x100, 18, 0);
            PostMessage(Warcraft3Info.MainWindowHandle, 0x100, 67, 0);
            PostMessage(Warcraft3Info.MainWindowHandle, 0x101, 67, 0);
            PostMessage(Warcraft3Info.MainWindowHandle, 0x101, 18, 0);
            await Task.Delay(1000);

            if (Message.GetOffset())
            {
                byte[] buffer = Encoding.UTF8.GetBytes(roomname.Trim());
                WriteProcessMemory(Warcraft3Info.Handle, Message.CEditBoxOffset + 0x6E8, buffer, buffer.Length + 1, out _);
                await Task.Delay(3000);
                PostMessage(Warcraft3Info.MainWindowHandle, 0x100, 18, 0);
                PostMessage(Warcraft3Info.MainWindowHandle, 0x100, 67, 0);
                PostMessage(Warcraft3Info.MainWindowHandle, 0x101, 67, 0);
                PostMessage(Warcraft3Info.MainWindowHandle, 0x101, 18, 0);
            }
        }
    }
}
