using System;
using System.IO;

using epicro.Helpers;
using epicro.Models;

namespace epicro.Wc3
{
    // ── CommandTag (Cirnix.Global.CommandTag 대체) ────────────────────────
    public enum CommandTag
    {
        None    = 0,
        Default = 1,
        Chat    = 2,
        Cheat   = 3
    }

    // ── Theme (메시지 색상/제목) ──────────────────────────────────────────
    internal static class Theme
    {
        // WC3 \x1 메시지는 |C 대문자 필수, |r 불필요
        public static string MsgTitle      = "[EPICRO]";
        public static string MsgTitleColor = "|CFF00CCFF";  // 하늘색
        public static string MsgColor      = "|CFFFFE066";  // 노란색
    }

    // ── SavePath / SavePathList ────────────────────────────────────────────
    public sealed class SavePath
    {
        public string path        { get; set; }
        public string nameEN      { get; set; }
        public string nameKR      { get; set; }
        public string DisplayName => !string.IsNullOrEmpty(nameKR) ? nameKR : nameEN;
        public SavePath() { }
        public SavePath(string path, string nameEN, string nameKR = "")
        { this.path = path; this.nameEN = nameEN; this.nameKR = nameKR; }
    }

    public sealed class SavePathList : System.Collections.Generic.List<SavePath>
    {
        public void Read()
        {
            string[] data = Settings.SaveFilePath.Split(new string[] { "∫" }, StringSplitOptions.None);
            if (data.Length <= 1) return;
            int count = (data.Length / 3) * 3; // 3의 배수만큼만 처리 (불완전한 항목 제외)
            for (int i = 0; i < count; i++)
                switch (i % 3)
                {
                    case 0: Add(new SavePath()); this[i / 3].path   = data[i]; break;
                    case 1:                      this[i / 3].nameEN = data[i]; break;
                    case 2:                      this[i / 3].nameKR = data[i]; break;
                }
        }

        public void Save()
        {
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < Count; i++)
            {
                if (i > 0) builder.Append("∫");
                builder.AppendFormat("{0}∫{1}∫{2}", this[i].path, this[i].nameEN, this[i].nameKR);
            }
            Settings.SaveFilePath = builder.ToString();
        }

        public void AddPath(string path, string nameEN, string nameKR = "")
        {
            int index;
            if ((index = path.IndexOf("CustomMapData")) == -1)
                throw new Exception("경로에 CustomMapData가 존재하지 않습니다.");
            Add(new SavePath(path.Substring(index + 13), nameEN, nameKR));
            Save();
        }

        public string GetFullPath(int index)
        {
            try { return $"{Wc3Globals.DocumentPath}\\CustomMapData{this[index].path}"; }
            catch { return null; }
        }

        public string GetFullPath(string name)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].nameEN == name || this[i].nameKR == name)
                    return $"{Wc3Globals.DocumentPath}\\CustomMapData{this[i].path}";
            return null;
        }

        public string GetPath(string name)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].nameEN == name || this[i].nameKR == name)
                    return this[i].path;
            return null;
        }

        public string ConvertName(string nameEN)
        {
            var item = Find(x => x.nameEN == nameEN);
            if (item == null) return nameEN;
            return string.IsNullOrEmpty(item.nameKR) ? item.nameEN : item.nameKR;
        }

        public bool RemovePath(string name)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].nameEN == name || this[i].nameKR == name)
                {
                    RemoveAt(i);
                    Save();
                    return true;
                }
            return false;
        }
    }

    // ── Wc3Globals (Cirnix.Global.Globals 대체) ──────────────────────────
    internal static class Wc3Globals
    {
        public static HotkeyList            hotkeyList    = new HotkeyList();
        public static CommandList           commandList   = new CommandList();
        public static Worker.ChatHotkeyList chatHotkeyList = new Worker.ChatHotkeyList();
        public static CommandTag            UserState     = CommandTag.None;
        public static SavePathList          saveFilePath  = new SavePathList();

        // 세이브 파일 관련
        public  const  int           MaxCodeSlots   = 24;
        public static string[]       Category       = new string[3] { "", "", "" };
        public static string[]       Code           = new string[MaxCodeSlots];
        public static Action<int>    ListUpdate     = _ => { };

        /// <summary>
        /// 워크래프트 III 문서 경로 (세이브 파일 기본 디렉토리)
        /// </summary>
        public static string DocumentPath
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Warcraft III");

        /// <summary>
        /// 세이브 파일 경로를 depth 기준으로 반환합니다.
        /// depth=0: 맵 루트, depth=1: 영웅 폴더, depth=2: 파일
        /// </summary>
        public static string GetCurrentPath(int depth)
        {
            switch (depth)
            {
                case 0: return saveFilePath.GetFullPath(Category[0]);
                case 1: return $"{saveFilePath.GetFullPath(Category[0])}\\{Category[1]}";
                case 2: return $"{saveFilePath.GetFullPath(Category[0])}\\{Category[1]}\\{Category[2]}";
                default: throw new Exception("Unspecified Value.");
            }
        }

        /// <summary>
        /// 지정된 디렉토리에서 가장 최근에 수정된 파일 경로를 반환합니다.
        /// </summary>
        public static string GetLastest(string directory)
        {
            try
            {
                System.IO.FileInfo fileInv = null;
                if (Directory.Exists(directory))
                {
                    var di = new System.IO.DirectoryInfo(directory);
                    int result = 0;
                    foreach (var item in di.GetFiles())
                        if (result == 0) { fileInv = item; result = 1; }
                        else
                        {
                            result = DateTime.Compare(fileInv.LastWriteTime, item.LastWriteTime);
                            if (result < 0) fileInv = item;
                            result = 1;
                        }
                }
                return fileInv?.FullName;
            }
            catch { return null; }
        }

        public static string GetFileTime(string path)
        {
            try { return new System.IO.FileInfo(path).LastWriteTime.ToString("yyyy-MM-dd HH.mm.ss"); }
            catch { return DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss"); }
        }

        public static string GetDirectorySafeName(string name)
            => name.Replace('\\', ' ').Replace('/', ' ').Replace(':', ' ').Replace('*', ' ')
                   .Replace('?', ' ').Replace('\"', ' ').Replace('<', ' ').Replace('>', ' ').Replace('|', ' ');

        /// <summary>
        /// 한국어 받침에 따라 조사를 결정합니다.
        /// </summary>
        public static string IsKoreanBlock(string text, string withBlock, string withoutBlock)
        {
            if (string.IsNullOrEmpty(text)) return withoutBlock;
            char last = text[text.Length - 1];
            if (last >= 0xAC00 && last <= 0xD7A3)
            {
                int jongseong = (last - 0xAC00) % 28;
                return jongseong != 0 ? text + withBlock : text + withoutBlock;
            }
            return text + withoutBlock;
        }

        public static string ConvertSize(long bytes)
        {
            bool neg = bytes < 0;
            if (neg) bytes = -bytes;
            string result;
            if      (bytes >= 1000000) result = $"{Math.Round(bytes / 1048576.0, 1)} MB";
            else if (bytes >= 1000)    result = $"{Math.Round(bytes / 1024.0, 1)} KB";
            else                       result = $"{Math.Round((double)bytes)} bytes";
            return neg ? '-' + result : result;
        }

        /// <summary>
        /// CirnoLib.MPQ 없이 동작하는 치트맵 검사 스텁.
        /// CirnoLib DLL을 프로젝트에 추가하면 실제 로직을 연결할 수 있습니다.
        /// </summary>
        public static bool IsCheatMap(string path) => false;

        // Globals.GetCodes / GetCodes2 / GetCodes3 — 그대로 이식
        public static void GetCodes()
        {
            if (string.IsNullOrEmpty(Category[0]) || string.IsNullOrEmpty(Category[1]) || string.IsNullOrEmpty(Category[2]))
            {
                for (int i = 0; i < MaxCodeSlots; i++) Code[i] = string.Empty;
                return;
            }
            var buffer = new System.Collections.Generic.List<string>();
            string[] lines = File.ReadAllLines(GetCurrentPath(2));
            bool isFound = false;
            for (int i = 0; i < lines.Length; i++)
            {
                int index;
                if ((index = lines[i].IndexOf("\"Code")) == -1 && (index = lines[i].IndexOf("\"저장코드")) == -1)
                { if (isFound) break; continue; }
                isFound = true;
                index += 8;
                int endIdx = lines[i].LastIndexOf(" \" )");
                if (endIdx > index) buffer.Add(lines[i].Substring(index, endIdx - index).Trim());
            }
            for (int i = 0; i < MaxCodeSlots; i++) Code[i] = i < buffer.Count ? buffer[i] : string.Empty;
            NormalizeCode();
        }

        public static void GetCodes2()
        {
            if (string.IsNullOrEmpty(Category[0]) || string.IsNullOrEmpty(Category[1]) || string.IsNullOrEmpty(Category[2]))
            {
                for (int i = 0; i < MaxCodeSlots; i++) Code[i] = string.Empty;
                return;
            }
            var buffer = new System.Collections.Generic.List<string>();
            string[] lines = File.ReadAllLines(GetCurrentPath(2));
            bool isFound = false;
            for (int i = 0; i < lines.Length; i++)
            {
                int index = lines[i].IndexOf("\"로드 코드");
                if (index == -1) { if (isFound) break; continue; }
                isFound = true; index += 10;
                int endIdx = lines[i].LastIndexOf("\" )");
                if (endIdx > index) buffer.Add(lines[i].Substring(index, endIdx - index).Trim());
            }
            for (int i = 0; i < MaxCodeSlots; i++) Code[i] = i < buffer.Count ? buffer[i] : string.Empty;
            NormalizeCode2();
        }

        public static void GetCodes3()
        {
            if (string.IsNullOrEmpty(Category[0]) || string.IsNullOrEmpty(Category[1]) || string.IsNullOrEmpty(Category[2]))
            {
                for (int i = 0; i < MaxCodeSlots; i++) Code[i] = string.Empty;
                return;
            }
            var buffer = new System.Collections.Generic.List<string>();
            string[] lines = File.ReadAllLines(GetCurrentPath(2));
            bool isFound = false;
            for (int i = 0; i < lines.Length; i++)
            {
                int index = lines[i].IndexOf("\"-");
                if (index == -1) { if (isFound) break; continue; }
                isFound = true; index += 1;
                int endIdx = lines[i].LastIndexOf("\" )");
                if (endIdx > index) buffer.Add(lines[i].Substring(index, endIdx - index).Trim());
            }
            for (int i = 0; i < MaxCodeSlots; i++) Code[i] = i < buffer.Count ? buffer[i] : string.Empty;
            NormalizeCode2();
        }

        // allowExclamation=true: GetCodes (lc)  prefix '/' or '!'
        // allowExclamation=false: GetCodes2/3 (tlc/olc) prefix '/' only
        private static void NormalizeCodeCore(bool allowExclamation)
        {
            for (int guard = 0; guard < 1000; guard++)
            {
                bool isBreak = true;
                for (int i = 0; i < MaxCodeSlots; i++)
                {
                    if (string.IsNullOrEmpty(Code[i])) break;
                    if (Code[i][0] == '/' || (allowExclamation && Code[i][0] == '!'))
                    {
                        isBreak = false;
                        for (int j = i; j < MaxCodeSlots; j++)
                        {
                            int k = j - 1;
                            if (string.IsNullOrEmpty(Code[j])) break;
                            Code[j] = Code[k][Code[k].Length - 1] + Code[j];
                            Code[k] = Code[k].Substring(0, Code[k].Length - 1);
                        }
                    }
                }
                if (isBreak) return;
            }
        }

        private static void NormalizeCode()  => NormalizeCodeCore(allowExclamation: true);
        private static void NormalizeCode2() => NormalizeCodeCore(allowExclamation: false);

        public static bool IsGrabitiSaveText(string[] lines)
        {
            try
            {
                bool isFound = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    int index;
                    if ((index = lines[i].IndexOf("\"Code")) == -1 && (index = lines[i].IndexOf("\"저장코드")) == -1)
                    { if (isFound) break; continue; }
                    isFound = true;
                }
                return isFound;
            }
            catch { return false; }
        }

        public static bool IsTwrSaveText(string[] lines)
        {
            try
            {
                bool isFound = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    int index = lines[i].IndexOf("\"로드 코드");
                    if (index == -1) { if (isFound) break; continue; }
                    isFound = true;
                }
                return isFound;
            }
            catch { return false; }
        }
    }

    // ── Settings bridge (Cirnix.Global.Settings 대체) ────────────────────
    internal static class Settings
    {
        static AppSettings Cfg => SettingsManager.Current;
        static void Save() => SettingsManager.Save();

        // 게임 패치
        public static bool  GamePatch_HPView           { get => Cfg.GamePatch_HPView;           set { Cfg.GamePatch_HPView           = value; Save(); } }
        public static bool  GamePatch_ColorfulChat      { get => Cfg.GamePatch_ColorfulChat;     set { Cfg.GamePatch_ColorfulChat     = value; Save(); } }
        public static float StartSpeed                  { get => Cfg.GamePatch_StartDelay;       set { Cfg.GamePatch_StartDelay       = value; Save(); } }
        public static float CameraDistance              { get => Cfg.GamePatch_CameraDistance;   set { Cfg.GamePatch_CameraDistance   = value; Save(); } }
        public static float CameraAngleX                { get => Cfg.GamePatch_CameraAngleX;     set { Cfg.GamePatch_CameraAngleX     = value; Save(); } }
        public static float CameraAngleY                { get => Cfg.GamePatch_CameraAngleY;     set { Cfg.GamePatch_CameraAngleY     = value; Save(); } }
        public static int   GameDelay                   { get => Cfg.GamePatch_GameDelay;        set { Cfg.GamePatch_GameDelay        = value; Save(); } }

        // 메시지 설정
        public static bool  IsCommandHide               { get => Cfg.IsCommandHide;              set { Cfg.IsCommandHide              = value; Save(); } }
        public static bool  IsAutoFrequency             { get => Cfg.IsAutoFrequency;            set { Cfg.IsAutoFrequency            = value; Save(); } }
        public static int   ChatFrequency               { get => Cfg.ChatFrequency;              set { Cfg.ChatFrequency              = value; Save(); } }

        // 자동 기능
        public static int   AutoRG_Count                { get => Cfg.AutoRG_Count;               set { Cfg.AutoRG_Count               = value; Save(); } }
        public static int   AutoStart_MinPlayers        { get => Cfg.AutoStart_MinPlayers;       set { Cfg.AutoStart_MinPlayers       = value; Save(); } }
        public static int   MaxRoom_Count               { get => Cfg.MaxRoom_Count;              set { Cfg.MaxRoom_Count              = value; Save(); } }
        public static int   MinRoom_Count               { get => Cfg.MinRoom_Count;              set { Cfg.MinRoom_Count              = value; Save(); } }

        // 자동 마우스 (직렬화된 문자열)
        public static string AutoMouse                  { get => Cfg.AutoMouse_Settings;         set { Cfg.AutoMouse_Settings         = value; Save(); } }

        // 매크로 단축키 (직렬화된 문자열)
        public static string MacroHotkeys               { get => Cfg.MacroHotkeys;               set { Cfg.MacroHotkeys               = value; Save(); } }

        // 채팅 핫키 (직렬화된 문자열)
        public static string HotkeyChat                 { get => Cfg.ChatHotkeys;               set { Cfg.ChatHotkeys               = value; Save(); } }

        // RPG 세이브 선택 경로
        public static string MapType                    { get => Cfg.MapType;                    set { Cfg.MapType                    = value; Save(); } }
        public static string HeroType                   { get => Cfg.HeroType;                   set { Cfg.HeroType                   = value; Save(); } }
        public static string SaveFilePath               { get => Cfg.SaveFilePath;               set { Cfg.SaveFilePath               = value; Save(); } }
        public static bool   IsGrabitiSaveAutoAdd       { get => Cfg.IsGrabitiSaveAutoAdd;       set { Cfg.IsGrabitiSaveAutoAdd       = value; Save(); } }

        // 명령어 프리셋
        public static string CommandPreset1             { get => Cfg.CommandPreset1;             set { Cfg.CommandPreset1             = value; Save(); } }
        public static string CommandPreset2             { get => Cfg.CommandPreset2;             set { Cfg.CommandPreset2             = value; Save(); } }
        public static string CommandPreset3             { get => Cfg.CommandPreset3;             set { Cfg.CommandPreset3             = value; Save(); } }
        public static int    SelectedCommand            { get => Cfg.SelectedCommand;            set { Cfg.SelectedCommand            = value; Save(); } }
        public static int    GlobalDelay                { get => Cfg.GlobalDelay;               set { Cfg.GlobalDelay               = value; Save(); } }

        // 메모리 최적화
        public static bool  IsMemoryOptimize            { get => Cfg.IsMemoryOptimize;           set { Cfg.IsMemoryOptimize           = value; Save(); } }
        public static int   MemoryOptimizeCoolDown      { get => Cfg.MemoryOptimizeCoolDown;     set { Cfg.MemoryOptimizeCoolDown     = value; Save(); } }
        public static bool  IsOptimizeAfterEndGame      { get => Cfg.IsOptimizeAfterEndGame;     set { Cfg.IsOptimizeAfterEndGame     = value; Save(); } }

        // 채널채팅 배경색
        public static int   ChannelChatBGColor          { get => Cfg.ChannelChatBGColor;         set { Cfg.ChannelChatBGColor         = value; Save(); } }

        // WC3 실행
        public static string InstallPath               { get => Cfg.Wc3InstallPath;             set { Cfg.Wc3InstallPath             = value; Save(); } }
        public static string ExePath                   { get => Cfg.Wc3ExePath;                 set { Cfg.Wc3ExePath                 = value; Save(); } }
        public static string LaunchArgs                { get => Cfg.Wc3LaunchArgs;              set { Cfg.Wc3LaunchArgs              = value; Save(); } }

        // Cirnix 전체 활성화
        public static bool IsCirnixEnabled   { get => Cfg.IsCirnixEnabled;   set { Cfg.IsCirnixEnabled   = value; Save(); } }

        // 리플레이 자동 저장
        public static bool IsAutoReplay      { get => Cfg.IsAutoReplay;      set { Cfg.IsAutoReplay      = value; Save(); } }
        public static bool NoSavedReplaySave { get => Cfg.NoSavedReplaySave; set { Cfg.NoSavedReplaySave = value; Save(); } }

        // 사용 안 함 (Cirnix 서버 전용) — 컴파일 오류 방지용 스텁
        public static bool IsAutoHp           { get => false; set { } }
        public static bool IsAutoLoad         { get => false; set { } }
        public static bool IsCheatMapCheck    { get => false; set { } }
        public static string ConvertExtention { get => "png"; set { } }
        public static bool IsOriginalRemove   { get => false; set { } }
        public static int SmartKeyFlag        { get => 0;     set { } }  // SmartKey 제거됨
    }

    // ── HotkeyList / HotkeyComponent / CommandList / CommandComponent ─────
    // (Cirnix.Global.InputLibrary에서 이식)

    public sealed class HotkeyComponent
    {
        public System.Windows.Forms.Keys vk { get; private set; }
        public bool recall { get; private set; }
        public bool onlyInGame { get; private set; }
        public Action<System.Windows.Forms.Keys> function { get; private set; }
        public System.Windows.Forms.Keys fk { get; private set; }
        internal int id { get; private set; }
        internal bool paused { get; set; } = false;

        internal HotkeyComponent(System.Windows.Forms.Keys vk, bool recall, bool onlyInGame,
            Action<System.Windows.Forms.Keys> function, System.Windows.Forms.Keys fk, int id)
        {
            this.vk         = vk;
            this.recall     = recall;
            this.onlyInGame = onlyInGame;
            this.function   = function;
            this.fk         = fk;
            this.id         = id;
        }
    }

    public sealed class HotkeyList : System.Collections.Generic.List<HotkeyComponent>
    {
        private int seq = 0;

        public void Register(System.Windows.Forms.Keys vk,
            Action<System.Windows.Forms.Keys> function,
            System.Windows.Forms.Keys fk,
            bool recall = false, bool onlyInGame = true)
        {
            Add(new HotkeyComponent(vk, recall, onlyInGame, function, fk, seq++));
        }

        public bool IsRegistered(System.Windows.Forms.Keys vk)
            => FindIndex(item => item.vk == vk) != -1;

        public bool UnRegister(System.Windows.Forms.Keys vk)
        {
            bool ret = false;
            foreach (var item in FindAll(item => item.vk == vk))
            {
                Remove(item);
                ret = true;
            }
            return ret;
        }
    }

    public sealed class CommandComponent
    {
        public string CommandEng      { get; private set; }
        public string CommandKor      { get; private set; }
        public CommandTag Tag         { get; private set; }
        public Action<string[]> Function { get; private set; }
        public string Name        { get; internal set; }
        public string Params      { get; internal set; }
        public string Description { get; internal set; }

        internal CommandComponent(string eng, string kor, CommandTag tag, Action<string[]> func)
        {
            CommandEng = eng;
            CommandKor = kor;
            Tag        = tag;
            Function   = func;
        }

        public bool CompareCommand(string text)
            => CommandEng.Equals(text, StringComparison.OrdinalIgnoreCase)
            || CommandKor.Equals(text, StringComparison.OrdinalIgnoreCase);
    }

    public sealed class CommandList : System.Collections.Generic.List<CommandComponent>
    {
        public void Register(string eng, string kor, Action<string[]> func, CommandTag tag = CommandTag.Default)
        {
            if (string.IsNullOrEmpty(eng)) eng = "EpicroxNullCmd";
            if (string.IsNullOrEmpty(kor)) kor = "EpicroxNullCmd";
            Add(new CommandComponent(eng, kor, tag, func));
        }

        public bool Unregister(string command)
            => RemoveAll(item => item.CommandEng == command || item.CommandKor == command) > 0;
    }
}
