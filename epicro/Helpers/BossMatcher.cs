using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using System.Windows;
using epicro.Models;
using OpenCvSharp.Extensions;
using Composition.WindowsRuntimeHelpers;

namespace epicro.Helpers
{
    public static class BossMatcher
    {
        public static bool MatchBossByRoi(Mat fullMat, BossConfig config, string imageFolderPath, double threshold = 0.99)
        {
            Mat roiMat = null;
            Mat template = null;
            Mat result = null;

            try
            {
                var roi = new OpenCvSharp.Rect(config.Roi.X, config.Roi.Y, config.Roi.Width, config.Roi.Height);
                roiMat = new Mat(fullMat, roi); // ROI 자르기

                var templatePath = Path.Combine(imageFolderPath, config.ImagePath);
                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"[ERROR] 보스 템플릿 이미지 없음: {templatePath}");
                    return false;
                }

                template = Cv2.ImRead(templatePath, ImreadModes.Color);
                result = new Mat();

                Cv2.MatchTemplate(roiMat, template, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);

                //Console.WriteLine($"[DEBUG] {config.ImagePath} 매칭 점수: {maxVal}");
                return maxVal >= threshold;
            }
            finally
            {
                roiMat?.Dispose();
                template?.Dispose();
                result?.Dispose();
            }
        }
        public static Mat Convert(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            var mat = BitmapConverter.ToMat(bitmap);

            if (mat.Type() != MatType.CV_8UC3)
            {
                var converted = new Mat();
                Cv2.CvtColor(mat, converted, ColorConversionCodes.BGRA2BGR);
                mat.Dispose(); // 원본 해제
                return converted;
            }

            return mat; // 여기서는 별도 Clone 없이 반환하고, 반드시 using으로 감싸서 쓰기
        }
    }
}
