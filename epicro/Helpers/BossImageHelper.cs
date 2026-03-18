using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using epicro.Models;
using epicro.Utilites;

namespace epicro.Helpers
{
    public static class BossImageHelper
    {
        public static async Task CaptureAndSaveBossImagesAsync(string selectedZone, IntPtr targetHwnd)
        {
            var bossConfigs = BossConfigManager.GetBossConfigs();

            var bossesInZone = bossConfigs
                .Where(b => b.Value.Zone == selectedZone)
                .ToList();

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

            foreach (var boss in bossesInZone)
            {
                var rect = boss.Value.Roi;
                if (rect.Width == 0 || rect.Height == 0)
                {
                    Debug.WriteLine($"[WARN] {boss.Key} ROI 정보가 없습니다.");
                    continue;
                }

                var bitmap = await SoftwareBitmapCopy.CaptureSingleFrameAsync(targetHwnd);
                if (bitmap != null)
                {
                    var cropped = new CroppedBitmap(bitmap, rect);
                    string filePath = System.IO.Path.Combine(imageFolderPath, boss.Value.ImagePath);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(cropped));
                        encoder.Save(stream);
                    }

                    Debug.WriteLine($"[INFO] {boss.Key} 이미지 저장 완료: {filePath}");
                }
            }

            MessageBox.Show("보스 이미지 캡처 및 저장 완료!");
        }
    }
}
