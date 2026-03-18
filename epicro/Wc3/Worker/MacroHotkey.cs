using System;
using System.Windows.Forms;

using static epicro.Wc3.Wc3Globals;

namespace epicro.Wc3.Worker
{
    /// <summary>
    /// 벨트 매크로 / 보스 소환 토글 단축키 관리
    /// 조건: WC3 최상단 활성화 + 게임 중 (onlyInGame = true)
    /// </summary>
    public static class MacroHotkey
    {
        // MainWindow에서 할당 – UI 스레드 Dispatcher.Invoke 포함
        public static Action ToggleBelt;
        public static Action ToggleBoss;

        private static Keys _beltKey, _bossKey;

        public static Keys BeltKey
        {
            get => _beltKey;
            set
            {
                if (_beltKey != 0 && hotkeyList.IsRegistered(_beltKey))
                    hotkeyList.UnRegister(_beltKey);
                _beltKey = value;
                if (_beltKey != 0)
                    hotkeyList.Register(_beltKey, _ => ToggleBelt?.Invoke(), _beltKey, false, true);
                Save();
            }
        }

        public static Keys BossKey
        {
            get => _bossKey;
            set
            {
                if (_bossKey != 0 && hotkeyList.IsRegistered(_bossKey))
                    hotkeyList.UnRegister(_bossKey);
                _bossKey = value;
                if (_bossKey != 0)
                    hotkeyList.Register(_bossKey, _ => ToggleBoss?.Invoke(), _bossKey, false, true);
                Save();
            }
        }

        public static void Init()
        {
            Read();
            if (_beltKey != 0) hotkeyList.Register(_beltKey, _ => ToggleBelt?.Invoke(), _beltKey, false, true);
            if (_bossKey != 0) hotkeyList.Register(_bossKey, _ => ToggleBoss?.Invoke(), _bossKey, false, true);
        }

        private static void Read()
        {
            string[] parts = Settings.MacroHotkeys.Split('∫');
            _beltKey = (parts.Length > 0 && int.TryParse(parts[0], out int b)) ? (Keys)b : 0;
            _bossKey = (parts.Length > 1 && int.TryParse(parts[1], out int s)) ? (Keys)s : 0;
        }

        private static void Save()
        {
            Settings.MacroHotkeys = $"{(int)_beltKey}∫{(int)_bossKey}";
        }
    }
}
