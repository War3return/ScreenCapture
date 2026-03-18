using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using epicro.Helpers;

namespace epicro
{
    public partial class CharChangeWindow : Window
    {
        private CharacterInfo _info;
        private string        _fileContent;

        public CharChangeWindow()
        {
            InitializeComponent();
        }

        // ── 파일 선택 ────────────────────────────────────────────────────────────
        private void btn_CharFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "세이브 파일 선택",
                Filter = "텍스트 파일 (*.txt)|*.txt",
            };

            string defaultDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Warcraft III", "CustomMapData");
            if (Directory.Exists(defaultDir))
                dlg.InitialDirectory = defaultDir;

            if (dlg.ShowDialog() != true) return;

            try
            {
                _info        = SaveFileParser.Parse(dlg.FileName);
                _fileContent = File.ReadAllText(dlg.FileName, Encoding.UTF8);

                lbl_CharFileName.Text = Path.GetFileName(dlg.FileName);
                lbl_CharId.Text       = _info.Id;
                lbl_CharName.Text     = _info.CharName;
                lbl_CharLv.Text       = _info.Level.ToString("N0");
                lbl_Str.Text          = _info.Str.ToString("N0");
                lbl_Agi.Text          = _info.Agi.ToString("N0");
                lbl_Int.Text          = _info.Int.ToString("N0");
                lbl_SubAttr.Text      = _info.SubAttr;

                var items = new[] { lbl_Item1, lbl_Item2, lbl_Item3,
                                    lbl_Item4, lbl_Item5, lbl_Item6 };
                for (int i = 0; i < items.Length; i++)
                    items[i].Text = string.IsNullOrEmpty(_info.HeroItems[i])
                        ? "-"
                        : $"{i + 1}. {_info.HeroItems[i]}";

                // 슬롯별 현재 아이템 표시 + 교환 가능 아이템 목록 로드
                var cmbList = new[] { cmb_Item1, cmb_Item2, cmb_Item3, cmb_Item4, cmb_Item5, cmb_Item6 };
                var lblList = new[] { lbl_Slot1, lbl_Slot2, lbl_Slot3, lbl_Slot4, lbl_Slot5, lbl_Slot6 };
                for (int i = 0; i < 6; i++)
                {
                    lblList[i].Text = string.IsNullOrEmpty(_info.HeroItems[i])
                        ? $"{i + 1}. (없음)"
                        : $"{i + 1}. {_info.HeroItems[i]}";
                    cmbList[i].Items.Clear();
                    cmbList[i].Items.Add("(변경 없음)");
                    var swappable = CharChangeItemData.GetSwappableItems(
                        _info.HeroItems[i], _info.HeroItemCategories[i]);
                    foreach (var item in swappable)
                        cmbList[i].Items.Add(item);
                    cmbList[i].SelectedIndex = 0;
                }

                txt_NewSubAttr.Text = "그대로";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 파싱 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── 변경권 종류 변경 시 티켓 전용 필드 토글 ─────────────────────────────
        private void cmb_TicketType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pnl_TicketExtra == null) return;
            var selected = (cmb_TicketType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            bool isTicket = selected.Contains("헬퍼") || selected.Contains("티켓");
            pnl_TicketExtra.Visibility = isTicket ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── 양식 생성 → 클립보드 복사 ───────────────────────────────────────────
        private void btn_CopyForm_Click(object sender, RoutedEventArgs e)
        {
            if (_info == null)
            {
                MessageBox.Show("먼저 세이브 파일을 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var ticketType = (cmb_TicketType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            bool isTicket  = ticketType.Contains("헬퍼") || ticketType.Contains("티켓");
            // 사용자가 "-load b" 처럼 입력한 경우 "-load " 접두사 제거
            var newLoadId  = txt_NewLoadId.Text.Trim();
            if (newLoadId.StartsWith("-load ", StringComparison.OrdinalIgnoreCase))
                newLoadId = newLoadId.Substring(6).Trim();

            var charChange = txt_CharChange.Text.Trim();
            var subAttr    = txt_NewSubAttr.Text.Trim();
            var statStr    = txt_StatStr.Text.Trim();
            var statAgi    = txt_StatAgi.Text.Trim();
            var statInt    = txt_StatInt.Text.Trim();

            var sb = new StringBuilder();

            // 변경권 종류
            sb.AppendLine("캐릭터 변경권 종 류");
            sb.AppendLine($"└ {ticketType}");
            if (isTicket)
            {
                sb.AppendLine();
                sb.AppendLine("캐릭터 변경권 종 류 ( 분기별 캐변권은 작성 X )");
                sb.AppendLine($"└ 티켓습득 닉네임 : {txt_TicketNick.Text.Trim()}");
                sb.AppendLine($"└ 링크 첨부 : {txt_TicketLink.Text.Trim()}");
            }

            sb.AppendLine();
            sb.AppendLine($"닉 네 임 : {_info.Id}");
            sb.AppendLine();

            sb.AppendLine("현재로드명 적어주세요(현재 캐릭터 로드명 누락금지)");
            sb.AppendLine($"-load {_info.LoadName}");
            sb.AppendLine();

            sb.AppendLine("로 드 명 ( 변경할 로드명을 작성해주세요. 누락금지 )");
            sb.AppendLine($"-load {newLoadId}");
            sb.AppendLine();

            sb.AppendLine("스 텟 변 경 ( 변경 사항이 없을 경우 미 작성, 양식변경 금지 )");
            sb.AppendLine($"힘 > {statStr}");
            sb.AppendLine($"민 > {statAgi}");
            sb.AppendLine($"지 > {statInt}");
            sb.AppendLine();

            sb.AppendLine("캐릭터 \" 영웅 \" 에 장착된 아이템 \" 6 부위 \" ( 벨트는 선인 벨트 부터 변경 가능합니다.. )");
            sb.AppendLine();

            // 아이템 변경 (선택된 항목만 출력, 동일 아이템 제외)
            var cmbItems = new[] { cmb_Item1, cmb_Item2, cmb_Item3, cmb_Item4, cmb_Item5, cmb_Item6 };
            for (int i = 0; i < 6; i++)
            {
                var newItem = cmbItems[i].SelectedItem?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(newItem) || newItem == "(변경 없음)") continue;
                var currentItem = string.IsNullOrEmpty(_info.HeroItems[i]) ? "(없음)" : _info.HeroItems[i];
                if (newItem == currentItem) continue;   // 같은 아이템 선택 시 제외
                sb.AppendLine($"{currentItem} > {newItem}");
            }
            sb.AppendLine();

            sb.AppendLine("변경 캐릭터");
            sb.AppendLine(charChange);
            sb.AppendLine();
            sb.AppendLine("부속성");
            sb.AppendLine(subAttr);
            sb.AppendLine();

            sb.AppendLine("마지막으로 저장한 메모장 ( 첨부하지 않을 경우 누락 될 수 있습니다. )");
            sb.AppendLine(_fileContent);

            if (ClipboardHelper.SetText(sb.ToString()))
                MessageBox.Show("클립보드에 복사되었습니다!", "완료",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("클립보드 복사에 실패했습니다.\n다른 프로그램이 클립보드를 점유 중입니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void btn_Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
