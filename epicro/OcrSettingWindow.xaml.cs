using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using epicro.Helpers;
using OpenCvSharp;
using Windows.UI.Xaml.Controls;

namespace epicro
{
    /// <summary>
    /// OcrSettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OcrSettingWindow : System.Windows.Window
    {
        private OcrService ocrService;
        private System.Windows.Media.Color? textColor1 = null;
        private System.Windows.Media.Color? textColor2 = null;
        private System.Windows.Media.Color? textColor3 = null;
        private System.Windows.Media.Color? backgroundColor = null;

        private enum ColorTarget
        {
            None,
            Text1,
            Text2,
            Text3,
            Background
        }

        private ColorTarget currentTarget = ColorTarget.None;

        private System.Threading.CancellationTokenSource _filterCts;

        public OcrSettingWindow()
        {
            InitializeComponent();
            this.ocrService = new OcrService(() => MainWindow.backgroundCapture?.GetSafeTextureCopy(), MainWindow.ocrEngine);
            Loaded += async (sender, e) => await LoadRoiImageAsync();
        }

        // GPU 텍스처 복사 + 비트맵 추출을 백그라운드에서 실행 (UI 스레드 블로킹 방지)
        private async Task LoadRoiImageAsync()
        {
            Bitmap bmp = null;
            try { bmp = await Task.Run(() => ocrService.GetProcessedRoiBitmap("Roi_Gold")); }
            catch { }
            if (bmp == null) return;
            try { SetImageSource(OriginalImage, bmp); }
            finally { bmp.Dispose(); }
        }

        // BitmapImage를 Freeze()해서 UI 스레드에 안전하게 전달
        private static BitmapImage ToBitmapImage(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                var img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = ms;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();
                return img;
            }
        }

        private static void SetImageSource(System.Windows.Controls.Image target, Bitmap bmp)
        {
            var img = ToBitmapImage(bmp);
            target.Source = img;
        }
        private void OriginalImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var bitmap = OriginalImage.Source as BitmapSource;
            if (bitmap == null) return;

            var pos = e.GetPosition(OriginalImage);
            int x = (int)(pos.X * bitmap.PixelWidth / OriginalImage.ActualWidth);
            int y = (int)(pos.Y * bitmap.PixelHeight / OriginalImage.ActualHeight);

            byte[] pixels = new byte[4];
            bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);
            var selectedColor = System.Windows.Media.Color.FromRgb(pixels[2], pixels[1], pixels[0]);

            switch (currentTarget)
            {
                case ColorTarget.Text1:
                    textColor1 = selectedColor;
                    Rectangle1.Fill = new SolidColorBrush(selectedColor);
                    break;
                case ColorTarget.Text2:
                    textColor2 = selectedColor;
                    Rectangle2.Fill = new SolidColorBrush(selectedColor);
                    break;
                case ColorTarget.Text3:
                    textColor3 = selectedColor;
                    Rectangle3.Fill = new SolidColorBrush(selectedColor);
                    break;
                case ColorTarget.Background:
                    backgroundColor = selectedColor;
                    RectangleBG.Fill = new SolidColorBrush(selectedColor);
                    break;
            }

            ApplyFilterToImage();
        }

        private void ApplyFilterToImage()
        {
            // UI 값을 미리 캡처 (백그라운드 스레드에서 UI 접근 불가)
            bool c1 = CheckBox1.IsChecked == true,  c2 = CheckBox2.IsChecked == true,
                 c3 = CheckBox3.IsChecked == true,  cBg = CheckBoxBG.IsChecked == true;
            int  r1 = (int)Slider1.Value,            r2 = (int)Slider2.Value,
                 r3 = (int)Slider3.Value,            rBg = (int)SliderBG.Value;
            var  tc1 = textColor1; var tc2 = textColor2; var tc3 = textColor3;
            var  bgc = backgroundColor;

            // 픽셀 루프 + GPU 텍스처 복사 모두 백그라운드에서 실행
            _filterCts?.Cancel();
            _filterCts = new System.Threading.CancellationTokenSource();
            var token = _filterCts.Token;

            Task.Run(() =>
            {
                if (token.IsCancellationRequested) return;
                var bmp = ocrService.GetProcessedRoiBitmap();
                if (bmp == null) return;

                BitmapImage result = null;
                using (var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp))
                {
                    bmp.Dispose();
                    if (token.IsCancellationRequested) return;

                    // mat.At<Vec3b> 대신 indexer 사용 (더 빠름)
                    var indexer = mat.GetGenericIndexer<Vec3b>();
                    for (int y = 0; y < mat.Rows; y++)
                    {
                        if (token.IsCancellationRequested) return;
                        for (int x = 0; x < mat.Cols; x++)
                        {
                            var color = indexer[y, x];
                            var bgr = System.Windows.Media.Color.FromRgb(color.Item2, color.Item1, color.Item0);

                            bool isText =
                                (c1 && tc1 != null && IsWithinRange(bgr, tc1.Value, r1)) ||
                                (c2 && tc2 != null && IsWithinRange(bgr, tc2.Value, r2)) ||
                                (c3 && tc3 != null && IsWithinRange(bgr, tc3.Value, r3));

                            bool isBg = cBg && bgc != null && IsWithinRange(bgr, bgc.Value, rBg);

                            if (isText)          indexer[y, x] = new Vec3b(0, 0, 0);
                            else if (isBg)       indexer[y, x] = new Vec3b(255, 255, 255);
                            else                 indexer[y, x] = new Vec3b(127, 127, 127);
                        }
                    }

                    using (var filteredBmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat))
                        result = ToBitmapImage(filteredBmp);
                }

                if (token.IsCancellationRequested || result == null) return;
                Dispatcher.Invoke(() =>
                {
                    if (!token.IsCancellationRequested)
                        FilteredImage.Source = result;
                });
            }, token);
        }
        private Bitmap BitmapSourceToBitmap(BitmapSource bitmapSource)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(outStream);
                var bmp = new Bitmap(outStream);
                return bmp;
            }
        }
        // System.Windows.Media.Color → "#RRGGBB" 형식 (ColorTranslator.FromHtml 호환)
        private static string ToHtmlColor(System.Windows.Media.Color c)
            => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private void SaveColorSettings()
        {
            // 문자 색상 1
            if (textColor1 != null)
                SettingsManager.Current.TextColor1 = ToHtmlColor(textColor1.Value);
            SettingsManager.Current.TextRange1 = (int)Slider1.Value;

            // 문자 색상 2
            if (textColor2 != null)
                SettingsManager.Current.TextColor2 = ToHtmlColor(textColor2.Value);
            SettingsManager.Current.TextRange2 = (int)Slider2.Value;

            // 문자 색상 3
            if (textColor3 != null)
                SettingsManager.Current.TextColor3 = ToHtmlColor(textColor3.Value);
            SettingsManager.Current.TextRange3 = (int)Slider3.Value;

            // 배경 색상
            if (backgroundColor != null)
                SettingsManager.Current.BackgroundColor = ToHtmlColor(backgroundColor.Value);
            SettingsManager.Current.BackgroundRange = (int)SliderBG.Value;

            ocrService.RefreshFilterSettings();
            SettingsManager.Save(); // 저장!
        }
        private bool IsWithinRange(System.Windows.Media.Color target, System.Windows.Media.Color baseColor, int range)
        {
            return Math.Abs(target.R - baseColor.R) <= range &&
                   Math.Abs(target.G - baseColor.G) <= range &&
                   Math.Abs(target.B - baseColor.B) <= range;
        }
        private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ApplyFilterToImage 내부에서 _filterCts 취소/재생성을 처리함
            ApplyFilterToImage();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double zoom = e.NewValue;
            ZoomTransform.ScaleX = zoom;
            ZoomTransform.ScaleY = zoom;
        }

        private void FillteredZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double zoom = e.NewValue;
            FillterdZoomTransform.ScaleX = zoom;
            FillterdZoomTransform.ScaleY = zoom;
        }

        private void CheckBox1_Click(object sender, RoutedEventArgs e)
        {
            currentTarget = ColorTarget.Text1;
        }
        private void CheckBox2_Click(object sender, RoutedEventArgs e)
        {
            currentTarget = ColorTarget.Text2;
        }
        private void CheckBox3_Click(object sender, RoutedEventArgs e)
        {
            currentTarget = ColorTarget.Text3;
        }
        private void CheckBoxBG_Click(object sender, RoutedEventArgs e)
        {
            currentTarget = ColorTarget.Background;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveColorSettings();
            this.Close();
        }

        private void OcrTestButton_Click(object sender, RoutedEventArgs e)
        {
            // 현재 UI에서 선택한 색상을 ocrService에 반영한 뒤 테스트
            // (색상 클릭만으로는 ocrService.textColors가 갱신되지 않으므로 반드시 먼저 적용)
            SaveColorSettings();

            string roiKey = "Roi_Gold";
            int ocrResult = ocrService.ReadCurrentValue(roiKey);

            if (ocrResult != -1)
            {
                OcrResultBox.Text = ocrResult.ToString();
            }
            else
            {
                OcrResultBox.Text = "OCR 실패. 결과를 가져올 수 없습니다.";
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}