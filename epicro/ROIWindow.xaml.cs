using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Graphics.Imaging;
using System.Windows.Controls;
using epicro.Utilites;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using epicro.Helpers;
using epicro.Models;
using System.Linq;
using System.Threading.Tasks;

namespace epicro
{
    public partial class ROIWindow : Window
    {
        private readonly string[] roiNames;
        private List<Int32Rect> RoiAreas = new List<Int32Rect>();
        private Point startPoint;
        private int currentIndex = 0;
        private string settingsPrefix;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public ROIWindow(BitmapSource bitmapSource, string[] roiNames, string settingsPrefix)
        {
            InitializeComponent();
            this.roiNames = roiNames;
            this.settingsPrefix = settingsPrefix;
            CapturedImage.Source = bitmapSource;
            RoiInstruction.Text = $"ROI 영역 드래그 - {roiNames[currentIndex]}";
            PositionWindowToTarget();
        }

        private void PositionWindowToTarget()
        {
            var hwnd = MainWindow.TargetWindow?.Handle ?? IntPtr.Zero;
            if (hwnd == IntPtr.Zero) return;

            if (GetWindowRect(hwnd, out RECT rect))
            {
                // GetWindowRect은 물리 픽셀 반환 → WPF DIP로 변환
                double dpiScale;
                using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                    dpiScale = g.DpiX / 96.0;

                this.Left = rect.Left / dpiScale;
                this.Top  = rect.Top  / dpiScale;
            }
        }

        private void RoiCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(RoiCanvas);
            RoiRectangle.Visibility = Visibility.Visible;
            Canvas.SetLeft(RoiRectangle, startPoint.X);
            Canvas.SetTop(RoiRectangle, startPoint.Y);
            RoiRectangle.Width = 0;
            RoiRectangle.Height = 0;
        }

        private void RoiCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(RoiCanvas);
                var x = Math.Min(pos.X, startPoint.X);
                var y = Math.Min(pos.Y, startPoint.Y);
                var w = Math.Abs(pos.X - startPoint.X);
                var h = Math.Abs(pos.Y - startPoint.Y);
                Canvas.SetLeft(RoiRectangle, x);
                Canvas.SetTop(RoiRectangle, y);
                RoiRectangle.Width = w;
                RoiRectangle.Height = h;
            }
        }

        private void RoiCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Canvas는 WPF 논리 픽셀 좌표를 반환하지만,
            // 캡처 비트맵은 물리 픽셀 기준이므로 비율로 변환해야 함.
            // scaleX = 비트맵 물리 픽셀 너비 / 캔버스 논리 픽셀 너비
            var bitmapSource = CapturedImage.Source as BitmapSource;
            double scaleX = (bitmapSource != null && RoiCanvas.ActualWidth > 0)
                ? bitmapSource.PixelWidth / RoiCanvas.ActualWidth : 1.0;
            double scaleY = (bitmapSource != null && RoiCanvas.ActualHeight > 0)
                ? bitmapSource.PixelHeight / RoiCanvas.ActualHeight : 1.0;

            var rect = new Int32Rect(
                (int)(Canvas.GetLeft(RoiRectangle) * scaleX),
                (int)(Canvas.GetTop(RoiRectangle) * scaleY),
                (int)(RoiRectangle.Width * scaleX),
                (int)(RoiRectangle.Height * scaleY));

            RoiAreas.Add(rect);
            RoiRectangle.Visibility = Visibility.Collapsed;

            MessageBox.Show($"{roiNames[currentIndex]} ROI 지정 완료");
            Debug.WriteLine($"ROI {roiNames[currentIndex]} 저장: {rect.X}, {rect.Y}, {rect.Width}, {rect.Height}");

            currentIndex++;
            if (currentIndex >= roiNames.Length)
            {
                SaveRoiAreas();
                // SaveRoiImages(CapturedImage.Source as BitmapSource);
                //MessageBox.Show("모든 ROI 설정 완료");
                DialogResult = true;
                Close();
            }
            else
            {
                RoiInstruction.Text = $"ROI 영역 드래그 - {roiNames[currentIndex]}";
            }
        }
        private void SaveRoiAreas()
        {
            for (int i = 0; i < roiNames.Length; i++)
            {
                var rect = RoiAreas[i];
                SettingsManager.Current[$"{settingsPrefix}_{roiNames[i]}"] = $"{rect.X},{rect.Y},{rect.Width},{rect.Height}";
            }

            SettingsManager.Save();
            SettingsManager.Reload();
        }
        public void SaveRoiImages(BitmapSource fullBitmap)
        {
            for (int i = 0; i < roiNames.Length; i++)
            {
                string roiValue = SettingsManager.Current[$"{settingsPrefix}_{roiNames[i]}"];
                if (string.IsNullOrEmpty(roiValue)) continue;

                var parts = roiValue.Split(',');
                var rect = new Int32Rect(
                    int.Parse(parts[0]),
                    int.Parse(parts[1]),
                    int.Parse(parts[2]),
                    int.Parse(parts[3]));
                Debug.WriteLine($"이미지 크기: {fullBitmap.PixelWidth} x {fullBitmap.PixelHeight}");
                Debug.WriteLine($"ROI {roiNames[i]}: {rect.X}, {rect.Y}, {rect.Width}, {rect.Height}");
                // 잘라내기
                var cropped = new CroppedBitmap(fullBitmap, rect);

                // 저장 경로
                string fileName = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"{settingsPrefix}_{roiNames[i]}.png");

                // PNG로 저장
                using (var stream = new FileStream(fileName, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(cropped));
                    encoder.Save(stream);
                }

                //MessageBox.Show($"{roiNames[i]} 영역 이미지 저장 완료:\n{fileName}");
            }
            MessageBox.Show("모든 이미지 저장 완료");
        }

        public async Task CaptureAndSaveBossImagesAsync(string selectedZone)
        {
            var bossConfigs = BossConfigManager.GetBossConfigs(); // Dictionary<string, BossConfig>

            var bossesInZone = bossConfigs
                .Where(b => b.Value.Zone == selectedZone)
                .ToList(); // KeyValuePair 그대로

            if (bossesInZone.Count == 0)
            {
                MessageBox.Show("선택된 보스존의 정보를 찾을 수 없습니다.");
                return;
            }

            string imageFolderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            if (!Directory.Exists(imageFolderPath))
            {
                Directory.CreateDirectory(imageFolderPath);
            }

            foreach (var bossPair in bossesInZone)
            {
                string bossName = bossPair.Key;             // → 보스 이름 (현무, 백호, 청룡 등)
                var bossConfig = bossPair.Value;            // → BossConfig 구조체

                var rect = bossConfig.Roi;
                if (rect.Width == 0 || rect.Height == 0)
                {
                    Debug.WriteLine($"[WARN] {bossName} ROI 정보가 없습니다.");
                    continue;
                }

                var bitmap = await SoftwareBitmapCopy.CaptureSingleFrameAsync(MainWindow.TargetWindow.Handle);
                if (bitmap != null)
                {
                    var cropped = new CroppedBitmap(bitmap, rect);

                    string filePath = System.IO.Path.Combine(imageFolderPath, bossConfig.ImagePath);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(cropped));
                        encoder.Save(stream);
                    }

                    Debug.WriteLine($"[INFO] {bossName} 이미지 저장 완료: {filePath}");
                }
            }

            MessageBox.Show("보스 이미지 캡처 및 저장 완료!");
        }
    }
}