using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using static epicro.Wc3.Wc3Globals;
using static epicro.Wc3.Memory.Message;

namespace epicro.Wc3.Worker
{
    public sealed class ChatHotkey
    {
        public string ChatMessage { get; set; }
        public Keys   Hotkey      { get; set; }
        public bool   IsRegisted  { get; set; }

        internal ChatHotkey()
        {
            ChatMessage = string.Empty;
            Hotkey      = 0;
            IsRegisted  = false;
        }
    }

    public sealed class ChatHotkeyList : List<ChatHotkey>
    {
        internal ChatHotkeyList()
        {
            Read();
        }

        public void Save()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Count; i++)
            {
                if (i > 0) builder.Append("∫");
                builder.AppendFormat("{0}∫{1}∫{2}", this[i].ChatMessage, (int)this[i].Hotkey, this[i].IsRegisted);
            }
            Settings.HotkeyChat = builder.ToString();
        }

        private void Read()
        {
            string[] Text = Settings.HotkeyChat.Split(new string[] { "∫" }, StringSplitOptions.None);
            for (int i = 0; i < 10; i++)
            {
                Add(new ChatHotkey());
                this[i].ChatMessage = (i * 3     < Text.Length) ? Text[i * 3]     : string.Empty;
                this[i].Hotkey      = (i * 3 + 1 < Text.Length && int.TryParse(Text[i * 3 + 1], out int k))  ? (Keys)k : 0;
                this[i].IsRegisted  = (i * 3 + 2 < Text.Length && bool.TryParse(Text[i * 3 + 2], out bool b)) && b;
            }
        }

        public void RestoreRegistrations()
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i].IsRegisted && this[i].Hotkey != 0)
                {
                    if (!hotkeyList.IsRegistered(this[i].Hotkey))
                    {
                        int idx = i;
                        hotkeyList.Register(this[idx].Hotkey, vk => SendMsg(false, this[idx].ChatMessage), this[idx].Hotkey, false, true);
                    }
                }
            }
        }

        public bool IsKeyRegisted(int index)
            => this[index].Hotkey != 0;

        public bool Register(int index)
        {
            if (hotkeyList.IsRegistered(this[index].Hotkey)) return false;
            int idx = index;   // capture
            hotkeyList.Register(this[idx].Hotkey, vk => SendMsg(false, this[idx].ChatMessage), this[idx].Hotkey, false, true);
            this[index].IsRegisted = true;
            return true;
        }

        public bool UnRegister(int index)
        {
            if (!hotkeyList.IsRegistered(this[index].Hotkey)) return false;
            hotkeyList.UnRegister(this[index].Hotkey);
            this[index].IsRegisted = false;
            return true;
        }
    }
}
