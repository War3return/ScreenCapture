using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using epicro.Helpers;
using epicro.Models;
using epicro.Wc3;
using epicro.Wc3.Worker;
using epicro.Wc3.KeyHook;
using epicro.Wc3.Memory;
using K = System.Windows.Forms.Keys;

namespace epicro
{
    public partial class CirnixSettingWindow : Window
    {
        private readonly Action<string> _appendLog;
        private readonly Action<string> _updateAutoRGStatus;
        private readonly Action<string> _updateAutoStartStatus;
        private readonly Action        _onForceMemRefresh;

        private bool _uiReady = false;

        // ── AutoMouse용 키 목록 ──────────────────────────────────────────────
        private static readonly System.Windows.Forms.Keys[] _keyList = new[]
        {
            System.Windows.Forms.Keys.None,
            System.Windows.Forms.Keys.F1,  System.Windows.Forms.Keys.F2,  System.Windows.Forms.Keys.F3,
            System.Windows.Forms.Keys.F4,  System.Windows.Forms.Keys.F5,  System.Windows.Forms.Keys.F6,
            System.Windows.Forms.Keys.F7,  System.Windows.Forms.Keys.F8,  System.Windows.Forms.Keys.F9,
            System.Windows.Forms.Keys.F10, System.Windows.Forms.Keys.F11, System.Windows.Forms.Keys.F12,
            System.Windows.Forms.Keys.Q,   System.Windows.Forms.Keys.W,   System.Windows.Forms.Keys.E,
            System.Windows.Forms.Keys.R,   System.Windows.Forms.Keys.T,   System.Windows.Forms.Keys.Y,
            System.Windows.Forms.Keys.A,   System.Windows.Forms.Keys.S,   System.Windows.Forms.Keys.D,
            System.Windows.Forms.Keys.Z,   System.Windows.Forms.Keys.X,   System.Windows.Forms.Keys.C,
            System.Windows.Forms.Keys.D1,  System.Windows.Forms.Keys.D2,  System.Windows.Forms.Keys.D3,
            System.Windows.Forms.Keys.D4,  System.Windows.Forms.Keys.D5,
            System.Windows.Forms.Keys.Insert,  System.Windows.Forms.Keys.Home, System.Windows.Forms.Keys.End,
            System.Windows.Forms.Keys.Prior,   System.Windows.Forms.Keys.Next,
            System.Windows.Forms.Keys.NumPad0, System.Windows.Forms.Keys.NumPad1,
            System.Windows.Forms.Keys.NumPad2, System.Windows.Forms.Keys.NumPad3,
            System.Windows.Forms.Keys.NumPad4, System.Windows.Forms.Keys.NumPad5,
            System.Windows.Forms.Keys.NumPad6, System.Windows.Forms.Keys.NumPad7,
            System.Windows.Forms.Keys.NumPad8, System.Windows.Forms.Keys.NumPad9,
        };

        // ── 키 캡처 공통 상태 ────────────────────────────────────────────────
        // 채팅 핫키: _capturingIndex >= 0
        // AutoMouse:  _capturingIndex == -1, _capturingAction != null
        private int    _capturingIndex  = -1;
        private Button _capturingButton = null;
        private Action<System.Windows.Forms.Keys> _capturingAction = null;   // AutoMouse용
        private readonly List<Button> _hotkeyButtons = new List<Button>();

        public CirnixSettingWindow(
            Action<string> appendLog,
            Action<string> updateAutoRGStatus,
            Action<string> updateAutoStartStatus,
            Action         onForceMemRefresh = null)
        {
            InitializeComponent();
            _appendLog             = appendLog;
            _updateAutoRGStatus    = updateAutoRGStatus;
            _updateAutoStartStatus = updateAutoStartStatus;
            _onForceMemRefresh     = onForceMemRefresh;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            BuildCmdList();
            Save_LoadRpgList();
            Save_UpdateDesignationLabel();
        }

        // ── 설정 로드 ────────────────────────────────────────────────────────
        private void LoadSettings()
        {
            // 게임 패치
            chk_HPView.IsChecked = SettingsManager.Current.GamePatch_HPView;
            txt_CamDist.Text           = SettingsManager.Current.GamePatch_CameraDistance.ToString();
            txt_CamAngleX.Text         = SettingsManager.Current.GamePatch_CameraAngleX.ToString();
            txt_CamAngleY.Text         = SettingsManager.Current.GamePatch_CameraAngleY.ToString();

            // 자동 기능
            txt_AutoRG_Count.Text         = SettingsManager.Current.AutoRG_Count.ToString();
            txt_AutoStart_MinPlayers.Text = SettingsManager.Current.AutoStart_MinPlayers.ToString();
            txt_MaxRoom_Count.Text        = SettingsManager.Current.MaxRoom_Count.ToString();
            txt_MinRoom_Count.Text        = SettingsManager.Current.MinRoom_Count.ToString();

            // 명령어 프리셋
            txt_GlobalDelay.Text = SettingsManager.Current.GlobalDelay.ToString();
            txt_Preset1.Text     = SettingsManager.Current.CommandPreset1;
            txt_Preset2.Text     = SettingsManager.Current.CommandPreset2;
            txt_Preset3.Text     = SettingsManager.Current.CommandPreset3;
            switch (SettingsManager.Current.SelectedCommand)
            {
                case 2:  rdo_Preset2.IsChecked = true; break;
                case 3:  rdo_Preset3.IsChecked = true; break;
                default: rdo_Preset1.IsChecked = true; break;
            }

            // 리플레이 자동 저장
            chk_AutoReplay.IsChecked    = SettingsManager.Current.IsAutoReplay;
            chk_NoSavedReplay.IsChecked = SettingsManager.Current.NoSavedReplaySave;

            // 기타 설정 — WC3 실행
            txt_Wc3ExePath.Text    = SettingsManager.Current.Wc3ExePath;
            txt_Wc3LaunchArgs.Text = SettingsManager.Current.Wc3LaunchArgs;

            // 기타 설정
            chk_CommandHide.IsChecked  = SettingsManager.Current.IsCommandHide;
            chk_MemOptimize.IsChecked       = SettingsManager.Current.IsMemoryOptimize;
            chk_OptimizeAfterGame.IsChecked = SettingsManager.Current.IsOptimizeAfterEndGame;
            txt_MemOptCooldown.Text         = SettingsManager.Current.MemoryOptimizeCoolDown.ToString();

            // 자동 마우스
            chk_AutoMouse_Enable.IsChecked = AutoMouse.Enabled;
            btn_AutoMouse_Left.Content  = KeyDisplayName(AutoMouse.LeftStartKey);
            btn_AutoMouse_Right.Content = KeyDisplayName(AutoMouse.RightStartKey);
            btn_AutoMouse_End.Content   = KeyDisplayName(AutoMouse.EndKey);
            txt_AutoMouse_Interval.Text = AutoMouse.Interval.ToString();

            // 채팅 핫키 행 생성
            BuildChatHotkeyRows();

            _uiReady = true;
        }


        private System.Windows.Forms.Keys GetKey(ComboBox cb)
        {
            int idx = cb.SelectedIndex;
            return (idx >= 0 && idx < _keyList.Length) ? _keyList[idx] : System.Windows.Forms.Keys.None;
        }

        private void BuildChatHotkeyRows()
        {
            pnl_ChatHotkeys.Children.Clear();
            _hotkeyButtons.Clear();
            var list = Wc3Globals.chatHotkeyList;
            for (int i = 0; i < list.Count; i++)
            {
                int idx = i;
                var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });

                // 키 캡처 버튼
                var btnKey = new Button
                {
                    Content  = KeyDisplayName(list[idx].Hotkey),
                    Margin   = new Thickness(0, 0, 6, 0),
                    FontSize = 10,
                    Padding  = new Thickness(4, 2, 4, 2),
                    ToolTip  = "클릭 후 원하는 키를 누르세요\nDelete/Backspace: 해제"
                };
                btnKey.Click += (s, e) => StartHotkeyCapture(idx, (Button)s);
                Grid.SetColumn(btnKey, 0);
                _hotkeyButtons.Add(btnKey);

                // 메시지 텍스트박스
                var tbMsg = new System.Windows.Controls.TextBox
                {
                    Text   = list[idx].ChatMessage,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                tbMsg.LostFocus += (s, e) =>
                {
                    list[idx].ChatMessage = ((System.Windows.Controls.TextBox)s).Text;
                    list.Save();
                };
                Grid.SetColumn(tbMsg, 1);

                // 등록 체크박스
                var chkReg = new CheckBox
                {
                    Content             = "등록",
                    IsChecked           = list[idx].IsRegisted,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center
                };
                chkReg.Click += (s, e) =>
                {
                    if (chkReg.IsChecked == true) { if (!list.Register(idx)) chkReg.IsChecked = false; }
                    else list.UnRegister(idx);
                    list.Save();
                };
                Grid.SetColumn(chkReg, 2);

                row.Children.Add(btnKey);
                row.Children.Add(tbMsg);
                row.Children.Add(chkReg);
                pnl_ChatHotkeys.Children.Add(row);
            }
        }

        // 채팅 핫키 캡처 시작
        private void StartHotkeyCapture(int index, Button btn)
        {
            CancelCapture();
            _capturingIndex  = index;
            _capturingButton = btn;
            btn.Content = "▶ 키 입력...";
            this.Focus();
        }

        // AutoMouse 캡처 시작
        private void StartActionCapture(Button btn, Action<System.Windows.Forms.Keys> applyKey)
        {
            CancelCapture();
            _capturingIndex  = -2;   // AutoMouse 전용 마커
            _capturingButton = btn;
            _capturingAction = applyKey;
            btn.Content = "▶ 키 입력...";
            this.Focus();
        }

        private void CancelCapture()
        {
            if (_capturingButton == null) return;
            if (_capturingIndex >= 0)
                _capturingButton.Content = KeyDisplayName(Wc3Globals.chatHotkeyList[_capturingIndex].Hotkey);
            // AutoMouse(_capturingIndex == -2): 현재 버튼 텍스트는 이미 현재 키 이름이므로 그대로 둠
            _capturingIndex  = -1;
            _capturingButton = null;
            _capturingAction = null;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_capturingButton == null) return;

            var wpfKey = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

            // 수식키 단독 → 무시
            switch (wpfKey)
            {
                case System.Windows.Input.Key.LeftShift:
                case System.Windows.Input.Key.RightShift:
                case System.Windows.Input.Key.LeftCtrl:
                case System.Windows.Input.Key.RightCtrl:
                case System.Windows.Input.Key.LeftAlt:
                case System.Windows.Input.Key.RightAlt:
                case System.Windows.Input.Key.LWin:
                case System.Windows.Input.Key.RWin:
                    return;
            }

            // Escape → 취소
            if (wpfKey == System.Windows.Input.Key.Escape)
            {
                CancelCapture();
                e.Handled = true;
                return;
            }

            var formsKey = (wpfKey == System.Windows.Input.Key.Delete || wpfKey == System.Windows.Input.Key.Back)
                ? System.Windows.Forms.Keys.None
                : (System.Windows.Forms.Keys)System.Windows.Input.KeyInterop.VirtualKeyFromKey(wpfKey);

            if (_capturingIndex >= 0)
            {
                // 채팅 핫키
                var list = Wc3Globals.chatHotkeyList;
                list[_capturingIndex].Hotkey = formsKey;
                list.Save();
            }
            else
            {
                // AutoMouse
                _capturingAction?.Invoke(formsKey);
            }

            _capturingButton.Content = KeyDisplayName(formsKey);
            _capturingIndex  = -1;
            _capturingButton = null;
            _capturingAction = null;
            e.Handled = true;
        }

        private static string KeyDisplayName(System.Windows.Forms.Keys key)
        {
            switch (key)
            {
                // ── 없음 ──────────────────────────────
                case K.None:            return "(없음)";

                // ── 숫자열 ────────────────────────────
                case K.D0:              return "0";
                case K.D1:              return "1";
                case K.D2:              return "2";
                case K.D3:              return "3";
                case K.D4:              return "4";
                case K.D5:              return "5";
                case K.D6:              return "6";
                case K.D7:              return "7";
                case K.D8:              return "8";
                case K.D9:              return "9";

                // ── 숫자패드 ──────────────────────────
                case K.NumPad0:         return "Num0";
                case K.NumPad1:         return "Num1";
                case K.NumPad2:         return "Num2";
                case K.NumPad3:         return "Num3";
                case K.NumPad4:         return "Num4";
                case K.NumPad5:         return "Num5";
                case K.NumPad6:         return "Num6";
                case K.NumPad7:         return "Num7";
                case K.NumPad8:         return "Num8";
                case K.NumPad9:         return "Num9";
                case K.Multiply:        return "Num*";
                case K.Add:             return "Num+";
                case K.Subtract:        return "Num-";
                case K.Divide:          return "Num/";
                case K.Decimal:         return "Num.";

                // ── OEM 특수문자 ──────────────────────
                case K.Oemtilde:        return "`";   // `  ~
                case K.OemMinus:        return "-";   // -  _
                case K.Oemplus:         return "=";   // =  +
                case K.OemOpenBrackets: return "[";   // [  {
                case K.Oem6:            return "]";   // ]  }
                case K.OemSemicolon:    return ";";   // ;  :
                case K.OemQuotes:       return "'";   // '  "
                case K.Oemcomma:        return ",";   // ,  <
                case K.OemPeriod:       return ".";   // .  >
                case K.OemQuestion:     return "/";   // /  ?
                case K.OemPipe:         return "\\";  // \  |
                case K.OemBackslash:    return "\\";  // \  (102번 키보드)

                // ── 탐색/편집 ─────────────────────────
                case K.Prior:           return "PageUp";
                case K.Next:            return "PageDown";
                case K.Home:            return "Home";
                case K.End:             return "End";
                case K.Insert:          return "Insert";
                case K.Delete:          return "Delete";

                // ── 기타 ─────────────────────────────
                case K.Back:            return "Backspace";
                case K.Tab:             return "Tab";
                case K.Return:          return "Enter";
                case K.Escape:          return "Esc";
                case K.Space:           return "Space";
                case K.Capital:         return "CapsLock";
                case K.Scroll:          return "ScrollLock";
                case K.Pause:           return "Pause";
                case K.PrintScreen:     return "PrtSc";

                // ── 기능키 ────────────────────────────
                case K.F1:              return "F1";
                case K.F2:              return "F2";
                case K.F3:              return "F3";
                case K.F4:              return "F4";
                case K.F5:              return "F5";
                case K.F6:              return "F6";
                case K.F7:              return "F7";
                case K.F8:              return "F8";
                case K.F9:              return "F9";
                case K.F10:             return "F10";
                case K.F11:             return "F11";
                case K.F12:             return "F12";

                // ── 방향키 ────────────────────────────
                case K.Up:              return "↑";
                case K.Down:            return "↓";
                case K.Left:            return "←";
                case K.Right:           return "→";

                default:                return key.ToString();
            }
        }

        // ── 게임 패치 ────────────────────────────────────────────────────────
        private void chk_HPView_Click(object sender, RoutedEventArgs e)
        {
            bool on = chk_HPView.IsChecked == true;
            SettingsManager.Current.GamePatch_HPView = on;
            if (epicro.Wc3.Component.Warcraft3Info.Process != null) GameDll.HPView = on;
            SettingsManager.Save();
        }

        private void btn_CamDist_Click(object sender, RoutedEventArgs e)
        {
            if (!float.TryParse(txt_CamDist.Text, out float val)) return;
            SettingsManager.Current.GamePatch_CameraDistance = val;
            if (epicro.Wc3.Component.Warcraft3Info.Process != null) GameDll.CameraDistance = val;
            SettingsManager.Save();
        }

        private void btn_CamAngleX_Click(object sender, RoutedEventArgs e)
        {
            if (!float.TryParse(txt_CamAngleX.Text, out float val)) return;
            SettingsManager.Current.GamePatch_CameraAngleX = val;
            if (epicro.Wc3.Component.Warcraft3Info.Process != null) GameDll.CameraAngleX = val;
            SettingsManager.Save();
        }

        private void btn_CamAngleY_Click(object sender, RoutedEventArgs e)
        {
            if (!float.TryParse(txt_CamAngleY.Text, out float val)) return;
            SettingsManager.Current.GamePatch_CameraAngleY = val;
            if (epicro.Wc3.Component.Warcraft3Info.Process != null) GameDll.CameraAngleY = val;
            SettingsManager.Save();
        }

        private void btn_CamReset_Click(object sender, RoutedEventArgs e)
        {
            if (epicro.Wc3.Component.Warcraft3Info.Process != null)
            {
                GameDll.CameraDistance = 0f;
                GameDll.CameraAngleX   = 0f;
                GameDll.CameraAngleY   = 0f;
            }
            SettingsManager.Current.GamePatch_CameraDistance = 0f;
            SettingsManager.Current.GamePatch_CameraAngleX   = 0f;
            SettingsManager.Current.GamePatch_CameraAngleY   = 0f;
            txt_CamDist.Text = txt_CamAngleX.Text = txt_CamAngleY.Text = "0";
            SettingsManager.Save();
        }

        // ── 자동 RG ──────────────────────────────────────────────────────────
        private void btn_AutoRG_Start_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txt_AutoRG_Count.Text, out int count)) count = 0;
            SettingsManager.Current.AutoRG_Count = count;
            SettingsManager.Save();
            AutoRG.RunWorkerAsync(count);
            SetAutoRGStatus("실행 중");
        }

        private void btn_AutoRG_Stop_Click(object sender, RoutedEventArgs e)
        {
            AutoRG.CancelAsync();
            SetAutoRGStatus("대기 중");
        }

        private void SetAutoRGStatus(string status)
        {
            lbl_AutoRG_Status.Text = status;
            _updateAutoRGStatus?.Invoke($"자동 RG: {status}");
        }

        // ── 자동 시작 ─────────────────────────────────────────────────────────
        private void btn_AutoStart_Start_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txt_AutoStart_MinPlayers.Text, out int count)) count = 4;
            SettingsManager.Current.AutoStart_MinPlayers = count;
            SettingsManager.Save();
            AutoStarter.RunWorkerAsync(count);
            SetAutoStartStatus("실행 중");
        }

        private void btn_AutoStart_Stop_Click(object sender, RoutedEventArgs e)
        {
            AutoStarter.CancelAsync();
            SetAutoStartStatus("대기 중");
        }

        private void SetAutoStartStatus(string status)
        {
            lbl_AutoStart_Status.Text = status;
            _updateAutoStartStatus?.Invoke($"자동 시작: {status}");
        }

        // ── 인원 알림 ─────────────────────────────────────────────────────────
        private void btn_MaxRoom_Start_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txt_MaxRoom_Count.Text, out int count)) count = 0;
            SettingsManager.Current.MaxRoom_Count = count;
            SettingsManager.Save();
            MaxRoom.RunWorkerAsync(count);
        }
        private void btn_MaxRoom_Stop_Click(object sender, RoutedEventArgs e) => MaxRoom.CancelAsync();

        private void btn_MinRoom_Start_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txt_MinRoom_Count.Text, out int count)) count = 0;
            SettingsManager.Current.MinRoom_Count = count;
            SettingsManager.Save();
            MinRoom.RunWorkerAsync(count);
        }
        private void btn_MinRoom_Stop_Click(object sender, RoutedEventArgs e) => MinRoom.CancelAsync();

        // ── 방 관리 ──────────────────────────────────────────────────────────
        private void btn_RoomJoin_Click(object sender, RoutedEventArgs e)
        {
            string name = txt_RoomName.Text.Trim();
            if (!string.IsNullOrEmpty(name)) epicro.Wc3.Memory.Join.RoomJoin(name);
        }

        private void btn_RoomCreate_Click(object sender, RoutedEventArgs e)
        {
            string name = txt_RoomName.Text.Trim();
            if (!string.IsNullOrEmpty(name)) epicro.Wc3.Memory.Join.RoomCreate(name);
        }

        // ── 채널 채팅 ─────────────────────────────────────────────────────────
        private void btn_ChannelChatOpen_Click(object sender, RoutedEventArgs e)
            => _appendLog?.Invoke("채널 채팅 창은 아직 구현 중입니다.");

        private void btn_ChannelRefresh_Click(object sender, RoutedEventArgs e)
            => _appendLog?.Invoke("채널 채팅 새로고침");

        // ── 자동 마우스 ──────────────────────────────────────────────────────
        private void chk_AutoMouse_Enable_Click(object sender, RoutedEventArgs e)
            => AutoMouse.Enabled = chk_AutoMouse_Enable.IsChecked == true;

        private void btn_AutoMouse_Left_Click(object sender, RoutedEventArgs e)
            => StartActionCapture(btn_AutoMouse_Left, k => AutoMouse.LeftStartKey = k);

        private void btn_AutoMouse_Right_Click(object sender, RoutedEventArgs e)
            => StartActionCapture(btn_AutoMouse_Right, k => AutoMouse.RightStartKey = k);

        private void btn_AutoMouse_End_Click(object sender, RoutedEventArgs e)
            => StartActionCapture(btn_AutoMouse_End, k => AutoMouse.EndKey = k);

        private void txt_AutoMouse_Interval_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txt_AutoMouse_Interval.Text, out int val) && val > 0)
                AutoMouse.Interval = val;
        }

        // ── 명령어 프리셋 ─────────────────────────────────────────────────────
        private void btn_GlobalDelay_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txt_GlobalDelay.Text, out int val) || val < 0) return;
            SettingsManager.Current.GlobalDelay = val;
            SettingsManager.Save();
        }

        private void rdo_Preset_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReady) return;
            int sel = rdo_Preset1.IsChecked == true ? 1
                    : rdo_Preset2.IsChecked == true ? 2 : 3;
            SettingsManager.Current.SelectedCommand = sel;
            SettingsManager.Save();
        }

        private void btn_SavePreset1_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.CommandPreset1 = txt_Preset1.Text;
            SettingsManager.Save();
            _appendLog?.Invoke("프리셋 1 저장 완료");
        }

        private void btn_SavePreset2_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.CommandPreset2 = txt_Preset2.Text;
            SettingsManager.Save();
            _appendLog?.Invoke("프리셋 2 저장 완료");
        }

        private void btn_SavePreset3_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.CommandPreset3 = txt_Preset3.Text;
            SettingsManager.Save();
            _appendLog?.Invoke("프리셋 3 저장 완료");
        }

        private void btn_RunPreset1_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.CommandPreset1 = txt_Preset1.Text;
            SettingsManager.Save();
            epicro.Wc3.Worker.Actions.LoadCommands(new[] { "cmd", "1" });
        }

        private void btn_RunPreset2_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.CommandPreset2 = txt_Preset2.Text;
            SettingsManager.Save();
            epicro.Wc3.Worker.Actions.LoadCommands(new[] { "cmd", "2" });
        }

        private void btn_RunPreset3_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.CommandPreset3 = txt_Preset3.Text;
            SettingsManager.Save();
            epicro.Wc3.Worker.Actions.LoadCommands(new[] { "cmd", "3" });
        }

        // ── WC3 실행 ──────────────────────────────────────────────────────────
        private void txt_Wc3LaunchArgs_LostFocus(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.Wc3LaunchArgs = txt_Wc3LaunchArgs.Text.Trim();
            SettingsManager.Save();
        }

        private void btn_Wc3PathBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new System.Windows.Forms.OpenFileDialog
            {
                Title  = "WC3 실행 파일을 선택하세요",
                Filter = "실행 파일 (*.exe)|*.exe",
                FileName = txt_Wc3ExePath.Text
            })
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txt_Wc3ExePath.Text            = dlg.FileName;
                    SettingsManager.Current.Wc3ExePath     = dlg.FileName;
                    // Rework 명령용 폴더 경로도 함께 갱신
                    SettingsManager.Current.Wc3InstallPath = System.IO.Path.GetDirectoryName(dlg.FileName);
                    SettingsManager.Save();
                }
            }
        }

        private async void btn_LaunchWc3_Click(object sender, RoutedEventArgs e)
        {
            string exePath = SettingsManager.Current.Wc3ExePath?.Trim();
            if (string.IsNullOrEmpty(exePath) || !System.IO.File.Exists(exePath))
            {
                MessageBox.Show("실행 파일을 찾을 수 없습니다.\n[찾기] 버튼으로 war3.exe를 지정해주세요.",
                                "WC3 실행 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string args = SettingsManager.Current.Wc3LaunchArgs ?? string.Empty;
            btn_LaunchWc3.IsEnabled = false;
            await Wc3.GameModule.LaunchWarcraft3(exePath, args);
            btn_LaunchWc3.IsEnabled = true;
        }

        // ── 리플레이 자동 저장 ────────────────────────────────────────────────
        private void chk_AutoReplay_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.IsAutoReplay = chk_AutoReplay.IsChecked == true;
            SettingsManager.Save();
        }

        private void chk_NoSavedReplay_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.NoSavedReplaySave = chk_NoSavedReplay.IsChecked == true;
            SettingsManager.Save();
        }

        // ── 기타 설정 ─────────────────────────────────────────────────────────
        private void chk_CommandHide_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.IsCommandHide = chk_CommandHide.IsChecked == true;
            SettingsManager.Save();
        }

        private void chk_MemOptimize_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.IsMemoryOptimize = chk_MemOptimize.IsChecked == true;
            SettingsManager.Save();
        }

        private void chk_OptimizeAfterGame_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.IsOptimizeAfterEndGame = chk_OptimizeAfterGame.IsChecked == true;
            SettingsManager.Save();
        }

        private void txt_MemOptCooldown_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txt_MemOptCooldown.Text, out int val) && val > 0)
            {
                SettingsManager.Current.MemoryOptimizeCoolDown = val;
                SettingsManager.Save();
            }
        }

        private async void btn_MemOptimize_Click(object sender, RoutedEventArgs e)
        {
            if (epicro.Wc3.Component.Warcraft3Info.Process == null) return;
            _appendLog?.Invoke("메모리 최적화 실행 중...");
            bool ok = await epicro.Wc3.CProcess.TrimProcessMemory(0);
            if (ok)
            {
                _appendLog?.Invoke("메모리 최적화 완료");
                _onForceMemRefresh?.Invoke(); // 우측상단 메모리 라벨 즉시 갱신
            }
            else
            {
                _appendLog?.Invoke("메모리 최적화 실패: WC3 프로세스를 찾을 수 없습니다.");
            }
        }

        // ── 명령어 목록 ─────────────────────────────────────────────────────────
        private void BuildCmdList()
        {
            pnl_CmdList.Children.Clear();

            // 헤더
            AddCmdHeader();
            AddSeparator();

            // 세이브 코드
            AddCmdSection("세이브 코드");
            AddCmdRow("!lc",    "ㅣㅊ",      "RPG 세이브 코드 로드");
            AddCmdRow("!tlc",   "싳",         "텍스트 세이브 로드 (TWRPG 등)");
            AddCmdRow("!olc",   "ㅐㅣㅊ",    "기타 세이브 로드");
            AddCmdRow("!set",   "ㄴㄷㅅ",    "세이브 분류 설정  (!set 이름)");
            AddCmdRow("!mset",  "ㅡㄴㄷㅅ",  "맵 설정  (!mset 맵이름)");
            AddCmdRow("-save",  "",            "RPG 인게임 세이브 감지");

            // 게임 패치
            AddCmdSection("게임 패치");
            AddCmdRow("!hp",    "ㅗㅔ",      "HP 최대값 표시 토글");
            AddCmdRow("!dr",    "ㅇㄱ",      "게임 딜레이 설정  (!dr 0~550)");
            AddCmdRow("!ss",    "ㄴㄴ",      "시작 딜레이 설정  (!ss 0~6)");
            AddCmdRow("!cam",   "시야",       "카메라 거리  (!cam 0~6000)");
            AddCmdRow("!camx",  "ㅊ믙",      "카메라 X각도  (!camx 0~360)");
            AddCmdRow("!camy",  "ㅊ므ㅛ",    "카메라 Y각도  (!camy 0~360)");
            AddCmdRow("!chk",   "체크",       "로드된 맵 치트 여부 검사");
            AddCmdRow("!map",   "맵",         "현재 로드된 맵 경로 출력");

            // 자동 기능
            AddCmdSection("자동 기능");
            AddCmdRow("!rg",    "ㄱㅎ",      "자동 RG  (!rg 횟수, 0=무한)");
            AddCmdRow("!as",    "ㅁㄴ",      "자동 시작  (!as 최소인원)");
            AddCmdRow("!max",   "ㅡㅁㅌ",    "최대 인원 알림  (!max 인원)");
            AddCmdRow("!min",   "ㅡㅑㅜ",    "최소 인원 알림  (!min 인원)");

            // 방 관리
            AddCmdSection("방 관리");
            AddCmdRow("!j",     "ㅓ",         "방 입장  (!j 방이름)");
            AddCmdRow("!c",     "ㅊ",         "방 생성  (!c 방이름)");
            AddCmdRow("!wa",    "ㅈㅁ",       "밴리스트 확인");
            AddCmdRow("!va",    "ㅍㅁ",       "IP 확인");

            // 명령어 프리셋
            AddCmdSection("명령어 프리셋");
            AddCmdRow("!cmd",   "층",         "기본 프리셋 실행");
            AddCmdRow("!cmd 1", "",           "프리셋 1 실행");
            AddCmdRow("!cmd 2", "",           "프리셋 2 실행");
            AddCmdRow("!cmd 3", "",           "프리셋 3 실행");

            // 기타
            AddCmdSection("기타");
            AddCmdRow("!dice",   "주사위",    "주사위  (!dice 최대값)");
            AddCmdRow("!mo",     "ㅡㅐ",      "메모리 최적화");
            AddCmdRow("!rework", "ㄱㄷ재가",  "WC3 재실행 (로비에서만)");
            AddCmdRow("!dbg",    "윻",         "단축키 후킹 재설정");
            AddCmdRow("!exit",   "종료",       "프로그램 종료");
        }

        private void AddCmdHeader()
        {
            var g = MakeCmdGrid();
            AddCmdCell(g, 0, "명령어",    isBold: true, isSecondary: true);
            AddCmdCell(g, 1, "한글 단축", isBold: true, isSecondary: true);
            AddCmdCell(g, 2, "설명",      isBold: true, isSecondary: true);
            pnl_CmdList.Children.Add(g);
        }

        private void AddSeparator()
        {
            pnl_CmdList.Children.Add(new System.Windows.Controls.Separator
            {
                Margin = new Thickness(0, 2, 0, 4),
                Background = (System.Windows.Media.Brush)FindResource("BorderBrush_")
            });
        }

        private void AddCmdSection(string title)
        {
            pnl_CmdList.Children.Add(new TextBlock
            {
                Text       = $"── {title}",
                FontSize   = 9,
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
                Margin     = new Thickness(0, 8, 0, 3)
            });
        }

        private void AddCmdRow(string cmd, string shortcut, string desc)
        {
            var g = MakeCmdGrid();
            AddCmdCell(g, 0, cmd,      isMono: true);
            AddCmdCell(g, 1, shortcut, isMono: true);
            AddCmdCell(g, 2, desc);
            pnl_CmdList.Children.Add(g);
        }

        private Grid MakeCmdGrid()
        {
            var g = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            return g;
        }

        private void AddCmdCell(Grid g, int col, string text,
                                 bool isBold = false, bool isMono = false, bool isSecondary = false)
        {
            var tb = new TextBlock
            {
                Text       = text,
                FontSize   = 11,
                FontWeight = isBold ? FontWeights.SemiBold : FontWeights.Normal,
                FontFamily = isMono
                    ? new System.Windows.Media.FontFamily("Consolas")
                    : new System.Windows.Media.FontFamily("Segoe UI"),
                Foreground = isSecondary
                    ? (System.Windows.Media.Brush)FindResource("TextSecondaryBrush")
                    : (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming      = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(tb, col);
            g.Children.Add(tb);
        }

        // ── 닫기 ──────────────────────────────────────────────────────────────
        private void btn_Close_Click(object sender, RoutedEventArgs e) => Close();

        // ══════════════════════════════════════════════════════════════════════
        // 세이브 탭
        // ══════════════════════════════════════════════════════════════════════

        private static readonly SavePath _saveNewEntry = new SavePath("", "(새로 만들기)", "(새로 만들기)");
        private string SaveCustomMapDataPath => Path.Combine(Wc3Globals.DocumentPath, "CustomMapData");

        private void Save_UpdateDesignationLabel()
        {
            string mapEN    = Wc3Globals.Category[0];
            string heroType = Wc3Globals.Category[1];
            if (string.IsNullOrEmpty(mapEN))
            {
                save_lbl_CurrentDesignation.Text = "(지정 없음)";
                return;
            }
            string display = Wc3Globals.saveFilePath.ConvertName(mapEN);
            save_lbl_CurrentDesignation.Text =
                $"{display}  >  {(string.IsNullOrEmpty(heroType) ? "(분류 없음)" : heroType)}";
        }

        // 폴더의 서브폴더가 또 서브폴더를 가지면 컨테이너(묶음 폴더)로 판단
        private bool Save_IsContainerFolder(string dirPath)
        {
            foreach (string sub in Directory.GetDirectories(dirPath))
                if (Directory.GetDirectories(sub).Length > 0)
                    return true;
            return false;
        }

        private SavePath Save_FindRegistered(string relativePath)
            => Wc3Globals.saveFilePath.Find(
                x => string.Equals(x.path.TrimEnd('\\'), relativePath.TrimEnd('\\'),
                                   StringComparison.OrdinalIgnoreCase));

        private void Save_AddToList(string relativePath, string folderName)
        {
            var registered = Save_FindRegistered(relativePath);
            save_lst_RpgList.Items.Add(registered ?? new SavePath(relativePath, folderName, ""));
        }

        private void Save_LoadRpgList()
        {
            save_lst_RpgList.Items.Clear();
            save_lst_RpgList.Items.Add(_saveNewEntry);

            if (Directory.Exists(SaveCustomMapDataPath))
            {
                foreach (string dir in Directory.GetDirectories(SaveCustomMapDataPath))
                {
                    string folderName = Path.GetFileName(dir);
                    string relPath    = "\\" + folderName;

                    if (Save_IsContainerFolder(dir))
                    {
                        foreach (string sub in Directory.GetDirectories(dir))
                        {
                            string subName    = Path.GetFileName(sub);
                            string subRelPath = relPath + "\\" + subName;
                            Save_AddToList(subRelPath, subName);
                        }
                    }
                    else
                    {
                        Save_AddToList(relPath, folderName);
                    }
                }
            }

            save_lst_CategoryList.Items.Clear();
            save_txt_RpgNameKR.Clear();
            save_txt_RpgNameEN.Clear();
            save_txt_RpgPath.Clear();
        }

        private bool Save_IsRegistered(SavePath item)
            => Wc3Globals.saveFilePath.Contains(item);

        private string Save_GetFullPath(SavePath item)
            => SaveCustomMapDataPath + item.path;

        private void Save_LoadCategories(SavePath rpg)
        {
            string fullPath = Save_GetFullPath(rpg);
            if (!Directory.Exists(fullPath)) return;
            foreach (string dir in Directory.GetDirectories(fullPath))
                save_lst_CategoryList.Items.Add(Path.GetFileName(dir));
        }

        private void save_lst_RpgList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = save_lst_RpgList.SelectedItem as SavePath;
            if (selected == null) return;

            bool isNew = ReferenceEquals(selected, _saveNewEntry);
            save_txt_RpgNameKR.Text = isNew ? "" : selected.nameKR;
            save_txt_RpgNameEN.Text = isNew ? "" : selected.nameEN;
            save_txt_RpgPath.Text   = isNew ? "" : selected.path;

            save_lst_CategoryList.Items.Clear();
            save_txt_CategoryName.Clear();
            if (!isNew) Save_LoadCategories(selected);
        }

        private void save_lst_CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            save_txt_CategoryName.Text = save_lst_CategoryList.SelectedItem as string ?? "";
        }

        private void save_btn_RpgRefresh_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Reload();
            Save_LoadRpgList();
        }

        private void save_btn_BrowsePath_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.SelectedPath = Directory.Exists(SaveCustomMapDataPath)
                    ? SaveCustomMapDataPath
                    : Wc3Globals.DocumentPath;

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string sel = dlg.SelectedPath;
                    if (sel.StartsWith(SaveCustomMapDataPath, StringComparison.OrdinalIgnoreCase))
                        save_txt_RpgPath.Text = sel.Substring(SaveCustomMapDataPath.Length);
                    else
                        MessageBox.Show("WC3 CustomMapData 폴더 내 경로를 선택하세요.", "경고");
                }
            }
        }

        private void save_btn_RpgAdd_Click(object sender, RoutedEventArgs e)
        {
            string nameKR = save_txt_RpgNameKR.Text.Trim();
            string nameEN = save_txt_RpgNameEN.Text.Trim();
            string path   = save_txt_RpgPath.Text.Trim();

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("경로를 입력하세요.", "오류");
                return;
            }
            if (string.IsNullOrEmpty(nameEN))
                nameEN = path.TrimStart('\\');

            var selected = save_lst_RpgList.SelectedItem as SavePath;
            bool isNew   = selected == null || ReferenceEquals(selected, _saveNewEntry);

            if (!isNew && Save_IsRegistered(selected))
            {
                selected.nameKR = nameKR;
                selected.nameEN = nameEN;
                selected.path   = path;
                Wc3Globals.saveFilePath.Save();
            }
            else if (!isNew && !Save_IsRegistered(selected))
            {
                selected.nameKR = nameKR;
                selected.nameEN = nameEN;
                Wc3Globals.saveFilePath.Add(selected);
                Wc3Globals.saveFilePath.Save();
            }
            else
            {
                string fullPath = SaveCustomMapDataPath + path;
                Directory.CreateDirectory(fullPath);
                Wc3Globals.saveFilePath.Add(new SavePath(path, nameEN, nameKR));
                Wc3Globals.saveFilePath.Save();
            }

            SettingsManager.Save();
            Save_LoadRpgList();
        }

        private void save_btn_RpgRemove_Click(object sender, RoutedEventArgs e)
        {
            var selected = save_lst_RpgList.SelectedItem as SavePath;
            if (selected == null || ReferenceEquals(selected, _saveNewEntry)) return;

            if (!Save_IsRegistered(selected))
            {
                MessageBox.Show("등록되지 않은 항목입니다.", "알림");
                return;
            }

            string displayName = selected.DisplayName;
            if (MessageBox.Show(
                    $"'{displayName}'을(를) 목록에서 제거하시겠습니까?\n(폴더는 삭제되지 않습니다)",
                    "제거", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Wc3Globals.saveFilePath.RemovePath(selected.nameEN);
                SettingsManager.Save();
                Save_LoadRpgList();
            }
        }

        private void save_btn_RpgOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var selected = save_lst_RpgList.SelectedItem as SavePath;
            if (selected == null || ReferenceEquals(selected, _saveNewEntry)) return;

            string path = Save_GetFullPath(selected);
            if (Directory.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", path);
        }

        private void save_btn_CategoryAdd_Click(object sender, RoutedEventArgs e)
        {
            var rpg = save_lst_RpgList.SelectedItem as SavePath;
            if (rpg == null || ReferenceEquals(rpg, _saveNewEntry)) return;

            string name = save_txt_CategoryName.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            Directory.CreateDirectory(Path.Combine(Save_GetFullPath(rpg), name));
            Save_LoadCategories(rpg);
        }

        private void save_btn_CategoryDelete_Click(object sender, RoutedEventArgs e)
        {
            var rpg = save_lst_RpgList.SelectedItem as SavePath;
            if (rpg == null || ReferenceEquals(rpg, _saveNewEntry)) return;

            string catName = save_lst_CategoryList.SelectedItem as string;
            if (string.IsNullOrEmpty(catName)) return;

            string fullPath = Path.Combine(Save_GetFullPath(rpg), catName);
            if (MessageBox.Show(
                    $"'{catName}' 폴더를 삭제하시겠습니까?\n(폴더 안의 파일도 모두 삭제됩니다)",
                    "삭제", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (Directory.Exists(fullPath))
                    Directory.Delete(fullPath, true);
                Save_LoadCategories(rpg);
            }
        }

        private void save_btn_CategoryOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var rpg = save_lst_RpgList.SelectedItem as SavePath;
            if (rpg == null || ReferenceEquals(rpg, _saveNewEntry)) return;

            string catName = save_lst_CategoryList.SelectedItem as string;
            if (string.IsNullOrEmpty(catName)) return;

            string fullPath = Path.Combine(Save_GetFullPath(rpg), catName);
            if (Directory.Exists(fullPath))
                System.Diagnostics.Process.Start("explorer.exe", fullPath);
        }

        private void save_btn_SetCurrent_Click(object sender, RoutedEventArgs e)
        {
            var rpg = save_lst_RpgList.SelectedItem as SavePath;
            if (rpg == null || ReferenceEquals(rpg, _saveNewEntry))
            {
                MessageBox.Show("RPG를 선택해주세요.", "알림");
                return;
            }
            if (!Save_IsRegistered(rpg))
            {
                MessageBox.Show("등록되지 않은 RPG입니다. 먼저 [추가] 버튼으로 등록해주세요.", "알림");
                return;
            }
            string category = save_lst_CategoryList.SelectedItem as string;
            if (string.IsNullOrEmpty(category))
            {
                MessageBox.Show("저장 분류를 선택해주세요.", "알림");
                return;
            }

            Settings.MapType  = Wc3Globals.Category[0] = rpg.nameEN;
            Settings.HeroType = Wc3Globals.Category[1] = category;

            Save_UpdateDesignationLabel();
        }
    }
}
