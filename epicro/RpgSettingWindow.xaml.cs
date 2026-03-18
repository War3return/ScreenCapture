using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

using epicro.Helpers;
using epicro.Wc3;

namespace epicro
{
    public partial class RpgSettingWindow : Window
    {
        private static readonly SavePath _newEntry = new SavePath("", "(새로 만들기)", "(새로 만들기)");

        private string CustomMapDataPath => Path.Combine(Wc3Globals.DocumentPath, "CustomMapData");

        public RpgSettingWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRpgList();
            UpdateDesignationLabel();
        }

        private void UpdateDesignationLabel()
        {
            string mapEN   = Wc3Globals.Category[0];
            string heroType = Wc3Globals.Category[1];
            if (string.IsNullOrEmpty(mapEN))
            {
                lbl_CurrentDesignation.Text = "(지정 없음)";
                return;
            }
            string display = Wc3Globals.saveFilePath.ConvertName(mapEN);
            lbl_CurrentDesignation.Text =
                $"{display}  >  {(string.IsNullOrEmpty(heroType) ? "(분류 없음)" : heroType)}";
        }

        // ── RPG 리스트 ────────────────────────────────────────────────────────

        // 폴더의 서브폴더가 또 서브폴더를 가지면 컨테이너(묶음 폴더)로 판단
        private bool IsContainerFolder(string dirPath)
        {
            foreach (string sub in Directory.GetDirectories(dirPath))
                if (Directory.GetDirectories(sub).Length > 0)
                    return true;
            return false;
        }

        private SavePath FindRegistered(string relativePath)
            => Wc3Globals.saveFilePath.Find(
                x => string.Equals(x.path.TrimEnd('\\'), relativePath.TrimEnd('\\'),
                                   System.StringComparison.OrdinalIgnoreCase));

        private void AddToList(string relativePath, string folderName)
        {
            var registered = FindRegistered(relativePath);
            lst_RpgList.Items.Add(registered ?? new SavePath(relativePath, folderName, ""));
        }

        private void LoadRpgList()
        {
            lst_RpgList.Items.Clear();
            lst_RpgList.Items.Add(_newEntry);

            if (Directory.Exists(CustomMapDataPath))
            {
                foreach (string dir in Directory.GetDirectories(CustomMapDataPath))
                {
                    string folderName = Path.GetFileName(dir);
                    string relPath    = "\\" + folderName;

                    if (IsContainerFolder(dir))
                    {
                        // Grabiti's RPG Creator 같은 묶음 폴더 → 안의 서브폴더를 RPG로 등록
                        foreach (string sub in Directory.GetDirectories(dir))
                        {
                            string subName    = Path.GetFileName(sub);
                            string subRelPath = relPath + "\\" + subName;
                            AddToList(subRelPath, subName);
                        }
                    }
                    else
                    {
                        AddToList(relPath, folderName);
                    }
                }
            }

            lst_CategoryList.Items.Clear();
            txt_RpgNameKR.Clear();
            txt_RpgNameEN.Clear();
            txt_RpgPath.Clear();
        }

        private void lst_RpgList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = lst_RpgList.SelectedItem as SavePath;
            if (selected == null) return;

            bool isNew = ReferenceEquals(selected, _newEntry);
            txt_RpgNameKR.Text = isNew ? "" : selected.nameKR;
            txt_RpgNameEN.Text = isNew ? "" : selected.nameEN;
            txt_RpgPath.Text   = isNew ? "" : selected.path;

            lst_CategoryList.Items.Clear();
            txt_CategoryName.Clear();
            if (!isNew) LoadCategories(selected);
        }

        // saveFilePath에 등록된 항목인지 확인
        private bool IsRegistered(SavePath item)
            => Wc3Globals.saveFilePath.Contains(item);

        // 등록 여부에 관계없이 실제 폴더 전체 경로 반환
        private string GetFullPath(SavePath item)
            => CustomMapDataPath + item.path;

        private void LoadCategories(SavePath rpg)
        {
            string fullPath = GetFullPath(rpg);
            if (!Directory.Exists(fullPath)) return;
            foreach (string dir in Directory.GetDirectories(fullPath))
                lst_CategoryList.Items.Add(Path.GetFileName(dir));
        }

        private void btn_RpgRefresh_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Reload();
            LoadRpgList();
        }

        private void btn_BrowsePath_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.SelectedPath = Directory.Exists(CustomMapDataPath) ? CustomMapDataPath : Wc3Globals.DocumentPath;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string sel = dlg.SelectedPath;
                    if (sel.StartsWith(CustomMapDataPath, System.StringComparison.OrdinalIgnoreCase))
                        txt_RpgPath.Text = sel.Substring(CustomMapDataPath.Length);
                    else
                        System.Windows.MessageBox.Show("WC3 CustomMapData 폴더 내 경로를 선택하세요.", "경고");
                }
            }
        }

        private void btn_RpgAdd_Click(object sender, RoutedEventArgs e)
        {
            string nameKR = txt_RpgNameKR.Text.Trim();
            string nameEN = txt_RpgNameEN.Text.Trim();
            string path   = txt_RpgPath.Text.Trim();

            if (string.IsNullOrEmpty(path))
            {
                System.Windows.MessageBox.Show("경로를 입력하세요.", "오류");
                return;
            }
            // 영문 이름 없으면 폴더명으로 자동 설정
            if (string.IsNullOrEmpty(nameEN))
                nameEN = path.TrimStart('\\');

            var selected = lst_RpgList.SelectedItem as SavePath;
            bool isNew = selected == null || ReferenceEquals(selected, _newEntry);

            if (!isNew && IsRegistered(selected))
            {
                // 등록된 항목 수정
                selected.nameKR = nameKR;
                selected.nameEN = nameEN;
                selected.path   = path;
                Wc3Globals.saveFilePath.Save();
            }
            else if (!isNew && !IsRegistered(selected))
            {
                // 스캔된 미등록 항목 → 등록
                selected.nameKR = nameKR;
                selected.nameEN = nameEN;
                Wc3Globals.saveFilePath.Add(selected);
                Wc3Globals.saveFilePath.Save();
            }
            else
            {
                // 새 항목 생성
                string fullPath = CustomMapDataPath + path;
                Directory.CreateDirectory(fullPath);
                Wc3Globals.saveFilePath.Add(new SavePath(path, nameEN, nameKR));
                Wc3Globals.saveFilePath.Save();
            }

            SettingsManager.Save();
            LoadRpgList();
        }

        private void btn_RpgRemove_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_RpgList.SelectedItem as SavePath;
            if (selected == null || ReferenceEquals(selected, _newEntry)) return;

            if (!IsRegistered(selected))
            {
                System.Windows.MessageBox.Show("등록되지 않은 항목입니다.", "알림");
                return;
            }

            string displayName = selected.DisplayName;
            if (System.Windows.MessageBox.Show(
                    $"'{displayName}'을(를) 목록에서 제거하시겠습니까?\n(폴더는 삭제되지 않습니다)",
                    "제거", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Wc3Globals.saveFilePath.RemovePath(selected.nameEN);
                SettingsManager.Save();
                LoadRpgList();
            }
        }

        private void btn_RpgOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var selected = lst_RpgList.SelectedItem as SavePath;
            if (selected == null || ReferenceEquals(selected, _newEntry)) return;

            string path = GetFullPath(selected);
            if (Directory.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", path);
        }

        // ── 저장 분류 ─────────────────────────────────────────────────────────

        private void lst_CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txt_CategoryName.Text = lst_CategoryList.SelectedItem as string ?? "";
        }

        private void btn_CategoryAdd_Click(object sender, RoutedEventArgs e)
        {
            var rpg = lst_RpgList.SelectedItem as SavePath;
            if (rpg == null || ReferenceEquals(rpg, _newEntry)) return;

            string name = txt_CategoryName.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            Directory.CreateDirectory(Path.Combine(GetFullPath(rpg), name));
            LoadCategories(rpg);
        }

        private void btn_CategoryDelete_Click(object sender, RoutedEventArgs e)
        {
            var rpg = lst_RpgList.SelectedItem as SavePath;
            if (rpg == null || ReferenceEquals(rpg, _newEntry)) return;

            string catName = lst_CategoryList.SelectedItem as string;
            if (string.IsNullOrEmpty(catName)) return;

            string fullPath = Path.Combine(GetFullPath(rpg), catName);
            if (System.Windows.MessageBox.Show(
                    $"'{catName}' 폴더를 삭제하시겠습니까?\n(폴더 안의 파일도 모두 삭제됩니다)",
                    "삭제", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (Directory.Exists(fullPath))
                    Directory.Delete(fullPath, true);
                LoadCategories(rpg);
            }
        }

        private void btn_CategoryOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var rpg = lst_RpgList.SelectedItem as SavePath;
            if (rpg == null || ReferenceEquals(rpg, _newEntry)) return;

            string catName = lst_CategoryList.SelectedItem as string;
            if (string.IsNullOrEmpty(catName)) return;

            string fullPath = Path.Combine(GetFullPath(rpg), catName);
            if (Directory.Exists(fullPath))
                System.Diagnostics.Process.Start("explorer.exe", fullPath);
        }

        // ── 지정 버튼 ────────────────────────────────────────────────────────

        private void btn_SetCurrent_Click(object sender, RoutedEventArgs e)
        {
            var rpg = lst_RpgList.SelectedItem as SavePath;
            if (rpg == null || ReferenceEquals(rpg, _newEntry))
            {
                System.Windows.MessageBox.Show("RPG를 선택해주세요.", "알림");
                return;
            }
            if (!IsRegistered(rpg))
            {
                System.Windows.MessageBox.Show("등록되지 않은 RPG입니다. 먼저 [추가] 버튼으로 등록해주세요.", "알림");
                return;
            }
            string category = lst_CategoryList.SelectedItem as string;
            if (string.IsNullOrEmpty(category))
            {
                System.Windows.MessageBox.Show("저장 분류를 선택해주세요.", "알림");
                return;
            }

            Settings.MapType  = Wc3Globals.Category[0] = rpg.nameEN;
            Settings.HeroType = Wc3Globals.Category[1] = category;

            UpdateDesignationLabel();
        }
    }
}
