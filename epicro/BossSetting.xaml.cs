using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using epicro.Utilites;
using epicro.Helpers;

namespace epicro
{
    /// <summary>
    /// BossSetting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BossSetting : Window
    {
        public BossSetting()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (ComboBoxItem item in cbb_BossZone.Items)
            {
                if (item.Content.ToString() == SettingsManager.Current.BossZone)
                {
                    cbb_BossZone.SelectedItem = item;
                    break;
                }
            }

            // 자원 인식 방식 라디오버튼 복원
            if (SettingsManager.Current.ResourceDetectionMode == "Memory")
                rb_ModeMemory.IsChecked = true;
            else
                rb_ModeOCR.IsChecked = true;

            UpdateOcrPanelVisibility();
        }

        private void rb_Mode_Checked(object sender, RoutedEventArgs e)
        {
            UpdateOcrPanelVisibility();
        }

        private void UpdateOcrPanelVisibility()
        {
            if (pnl_OcrOnly == null) return;
            bool isOcr = rb_ModeOCR.IsChecked == true;
            pnl_OcrOnly.Visibility = isOcr ? Visibility.Visible : Visibility.Collapsed;
            // OCR 전용 좌표 버튼도 OCR 모드에서만 활성화
            btn_GoldTree.IsEnabled = isOcr;
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            if (cbb_BossZone.SelectedItem is ComboBoxItem selected)
                SettingsManager.Current.BossZone = selected.Content.ToString();

            SettingsManager.Current.ResourceDetectionMode =
                rb_ModeMemory.IsChecked == true ? "Memory" : "OCR";

            SettingsManager.Save();
            this.Close();
        }

        private void btn_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // 물리 픽셀 → WPF DIP 변환용 DPI 배율 반환 (이 창이 실제로 있는 모니터 기준)
        private (double x, double y) GetDpiScale()
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            return (dpi.DpiScaleX, dpi.DpiScaleY);
        }

        // 비트맵 크기를 화면 WorkArea에 맞게 제한한 ROIWindow 크기 반환
        private (double w, double h) GetRoiWindowSize(BitmapSource bitmap)
        {
            var (dpiX, dpiY) = GetDpiScale();
            double w = bitmap.PixelWidth  / dpiX;
            double h = bitmap.PixelHeight / dpiY;

            // 모니터 화면을 벗어나지 않도록 WorkArea 기준으로 최대 제한
            double maxW = SystemParameters.WorkArea.Width;
            double maxH = SystemParameters.WorkArea.Height;
            return (Math.Min(w, maxW), Math.Min(h, maxH));
        }

        private async void btn_BossROI_Click(object sender, RoutedEventArgs e)
        {
            var TargetWindow = MainWindow.TargetWindow;
            if (!(Application.Current.MainWindow is MainWindow) || TargetWindow == null)
            {
                MessageBox.Show("먼저 인식 대상을 선택하세요.");
                return;
            }

            var bitmap = await SoftwareBitmapCopy.CaptureSingleFrameAsync(TargetWindow.Handle);
            if (bitmap == null) return;

            var (rw, rh) = GetRoiWindowSize(bitmap);
            var roiWindow = new ROIWindow(bitmap, new[] { "Q", "W", "E", "R", "A" }, "Roi")
            {
                Width = rw, Height = rh
            };
            roiWindow.ShowDialog();
        }

        private async void btn_GoldTree_Click(object sender, RoutedEventArgs e)
        {
            var TargetWindow = MainWindow.TargetWindow;
            if (TargetWindow == null)
            {
                MessageBox.Show("먼저 캡처할 창을 선택하세요.");
                return;
            }

            var bitmap = await SoftwareBitmapCopy.CaptureSingleFrameAsync(TargetWindow.Handle);
            if (bitmap == null) return;

            var (rw, rh) = GetRoiWindowSize(bitmap);
            var roiWindow = new ROIWindow(bitmap, new[] { "Gold", "Tree" }, "Roi")
            {
                Width = rw, Height = rh
            };
            roiWindow.ShowDialog();
        }

        private async void btn_AutoCapture_Click(object sender, RoutedEventArgs e)
        {
            var TargetWindow = MainWindow.TargetWindow;
            if (cbb_BossZone.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedZone = selectedItem.Content.ToString();
                await BossImageHelper.CaptureAndSaveBossImagesAsync(selectedZone, TargetWindow.Handle);
            }
            else
            {
                MessageBox.Show("보스존을 선택해주세요.");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ocrEngine == null)
            {
                MessageBox.Show(
                    "OCR 엔진이 초기화되지 않았습니다.\n" +
                    "tesseract/tessdata 폴더가 프로그램 경로에 있는지 확인하세요.",
                    "OCR 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var ocrsetttingwindow = new OcrSettingWindow();
                ocrsetttingwindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OCR 설정 창을 열 수 없습니다:\n{ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
