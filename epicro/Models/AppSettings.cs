using System;

namespace epicro.Models
{
    public class AppSettings
    {
        // ── 기존 ROI 좌표 ────────────────────────────────────────────────────
        public string Roi_Q    { get; set; } = "";
        public string Roi_W    { get; set; } = "";
        public string Roi_E    { get; set; } = "";
        public string Roi_R    { get; set; } = "";
        public string Roi_A    { get; set; } = "";
        public string Roi_Gold { get; set; } = "";
        public string Roi_Tree { get; set; } = "";

        // 보스 설정
        public string BossZone              { get; set; } = "";
        public string SelectedROI           { get; set; } = "";
        public string BossOrder             { get; set; } = "";
        public string ResourceDetectionMode { get; set; } = "OCR";

        // 벨트/캐릭터 설정
        public int    HeroNum   { get; set; } = 0;
        public int    BagNum    { get; set; } = 0;
        public string BeltNum   { get; set; } = "";
        public double BeltSpeed { get; set; } = 0.4;

        // 체크박스 상태
        public bool SaveEnabled       { get; set; } = false;
        public bool PickupEnabled     { get; set; } = false;
        public bool HeroSelectEnabled { get; set; } = false;

        // OCR 색상 필터
        public string TextColor1      { get; set; } = "";
        public string TextColor2      { get; set; } = "";
        public string TextColor3      { get; set; } = "";
        public int    TextRange1      { get; set; } = 0;
        public int    TextRange2      { get; set; } = 0;
        public int    TextRange3      { get; set; } = 0;
        public string BackgroundColor { get; set; } = "";
        public int    BackgroundRange { get; set; } = 0;

        // 텔레그램
        public string TelegramBotToken { get; set; } = "";
        public string TelegramChatIds  { get; set; } = "";
        public bool   TelegramEnabled  { get; set; } = true;

        // ── WC3 / Cirnix 이식 설정 ──────────────────────────────────────────

        // 게임 패치
        public bool  GamePatch_HPView         { get; set; } = false;
        public bool  GamePatch_ColorfulChat   { get; set; } = false;
        public float GamePatch_StartDelay     { get; set; } = 0f;   // 0 = 즉시(0.01f 적용)
        public float GamePatch_CameraDistance { get; set; } = 0f;   // 0 = 변경 안 함
        public float GamePatch_CameraAngleX   { get; set; } = 0f;   // 0 = 변경 안 함
        public float GamePatch_CameraAngleY   { get; set; } = 0f;   // 0 = 변경 안 함
        public int   GamePatch_GameDelay      { get; set; } = 0;    // !dr 명령어용 (UI 미노출)

        // 메시지/채팅 설정
        public bool IsCommandHide   { get; set; } = false;
        public bool IsAutoFrequency { get; set; } = true;
        public int  ChatFrequency   { get; set; } = 0;

        // 명령어 프리셋
        public string CommandPreset1  { get; set; } = "";
        public string CommandPreset2  { get; set; } = "";
        public string CommandPreset3  { get; set; } = "";
        public int    SelectedCommand { get; set; } = 0;
        public int    GlobalDelay     { get; set; } = 50;

        // 자동 기능
        public int AutoRG_Count         { get; set; } = 0;
        public int AutoStart_MinPlayers { get; set; } = 4;
        public int MaxRoom_Count        { get; set; } = 0;
        public int MinRoom_Count        { get; set; } = 0;

        // 자동 마우스 (직렬화 문자열)
        public string AutoMouse_Settings { get; set; } = "100∫0∫0∫0∫False";

        // 매크로 단축키 (벨트∫보스, Keys 정수값)
        public string MacroHotkeys { get; set; } = "0∫0";

        // 채팅 핫키 (10슬롯, 직렬화 문자열)
        public string ChatHotkeys { get; set; } =
            "∫0∫False∫0∫False∫0∫False∫0∫False∫0∫False∫0∫False∫0∫False∫0∫False∫0∫False∫0∫False";

        // RPG 세이브 경로
        public string MapType      { get; set; } = "";
        public string HeroType     { get; set; } = "";
        public string SaveFilePath { get; set; } = "";
        public bool   IsGrabitiSaveAutoAdd { get; set; } = true;

        // Cirnix 전체 활성화
        public bool IsCirnixEnabled   { get; set; } = true;

        // 리플레이 자동 저장
        public bool IsAutoReplay      { get; set; } = false;
        public bool NoSavedReplaySave { get; set; } = false;

        // 메모리 최적화
        public bool PreventChatboxEnter    { get; set; } = false;
        public bool IsMemoryOptimize       { get; set; } = false;
        public int  MemoryOptimizeCoolDown { get; set; } = 5;
        public bool IsOptimizeAfterEndGame { get; set; } = true;

        // 채널채팅 배경색 (ARGB int)
        public int ChannelChatBGColor { get; set; } = unchecked((int)0xFF000000);

        // WC3 실행
        public string Wc3InstallPath { get; set; } = "";   // Rework 명령용 (폴더)
        public string Wc3ExePath     { get; set; } = "";   // 실행 버튼용 (exe 전체 경로)
        public string Wc3LaunchArgs  { get; set; } = "-window -opengl";

        // ── 동적 인덱서 (Properties.Settings 패턴 대응) ─────────────────────
        public string this[string key]
        {
            get
            {
                switch (key)
                {
                    case "Roi_Q":       return Roi_Q;
                    case "Roi_W":       return Roi_W;
                    case "Roi_E":       return Roi_E;
                    case "Roi_R":       return Roi_R;
                    case "Roi_A":       return Roi_A;
                    case "Roi_Gold":    return Roi_Gold;
                    case "Roi_Tree":    return Roi_Tree;
                    case "SelectedROI": return SelectedROI;
                    default:            return null;
                }
            }
            set
            {
                switch (key)
                {
                    case "Roi_Q":       Roi_Q       = value ?? ""; break;
                    case "Roi_W":       Roi_W       = value ?? ""; break;
                    case "Roi_E":       Roi_E       = value ?? ""; break;
                    case "Roi_R":       Roi_R       = value ?? ""; break;
                    case "Roi_A":       Roi_A       = value ?? ""; break;
                    case "Roi_Gold":    Roi_Gold    = value ?? ""; break;
                    case "Roi_Tree":    Roi_Tree    = value ?? ""; break;
                    case "SelectedROI": SelectedROI = value ?? ""; break;
                }
            }
        }
    }
}
