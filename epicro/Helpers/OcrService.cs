using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;
using System.IO;
using System.Drawing;
using Composition.WindowsRuntimeHelpers;
using SharpDX.Direct3D11;
using System.Diagnostics;
using System.Timers;
using System.Drawing.Imaging;
using epicro.Helpers;
using OpenCvSharp.Extensions;
using OpenCvSharp;

namespace epicro.Helpers
{
    public class OcrService
    {
        private readonly TesseractEngine ocrEngine;
        private readonly Func<Texture2D> getTextureFunc;

        public event Action<string> OnOcrResult;

        private List<Tuple<System.Drawing.Color, int>> textColors;
        private Tuple<System.Drawing.Color, int> backgroundColor;


        public OcrService(Func<Texture2D> textureProvider, TesseractEngine engine)
        {
            getTextureFunc = textureProvider;
            ocrEngine = engine;
            ocrEngine.SetVariable("classify_bln_numeric_mode", "1");
            ocrEngine.SetVariable("tessedit_char_whitelist", "0123456789");
            LoadFilterSettings();
        }
        public int ReadCurrentValue(string roiSettingKey = "Roi_Gold")
        {
            using (var texture = getTextureFunc())
            {
                if (texture == null)
                {
                    Debug.WriteLine("텍스처가 null입니다.");
                    return -1;
                }

                using (var rawBitmap = Direct3D11Helper.ExtractBitmapFromTexture(texture))
                {
                    var roiStr = SettingsManager.Current[roiSettingKey];
                    if (string.IsNullOrWhiteSpace(roiStr))
                    {
                        Debug.WriteLine($"ROI 설정이 존재하지 않음: {roiSettingKey}");
                        return -1;
                    }

                    var roiParse = ParseRoiHelper.ParseRectFromSettings(roiStr);
                    var roi = new Rectangle(roiParse.X, roiParse.Y, roiParse.Width, roiParse.Height);

                    // ROI 유효성 사전 검증 (Clone 전에 체크해야 예외 방지)
                    if (roi.Width <= 0 || roi.Height <= 0)
                    {
                        Debug.WriteLine("ROI 크기가 유효하지 않습니다 (0이하).");
                        return -1;
                    }
                    if (roi.X < 0 || roi.Y < 0 || roi.Right > rawBitmap.Width || roi.Bottom > rawBitmap.Height)
                    {
                        Debug.WriteLine($"ROI가 비트맵 범위를 초과합니다. ROI={roi}, Bitmap={rawBitmap.Width}x{rawBitmap.Height}");
                        return -1;
                    }

                    try
                    {
                        using (var roiBitmap = rawBitmap.Clone(roi, PixelFormat.Format32bppArgb))
                        using (var bgraMat = BitmapConverter.ToMat(roiBitmap))
                        using (var mat = new Mat())
                        {
                            // BGRA → BGR: 채널 수가 바뀌므로 별도 dst Mat 사용 (in-place 불안정)
                            Cv2.CvtColor(bgraMat, mat, ColorConversionCodes.BGRA2BGR);

                            for (int y = 0; y < mat.Rows; y++)
                            {
                                for (int x = 0; x < mat.Cols; x++)
                                {
                                    var color = mat.At<Vec3b>(y, x); // BGR
                                    var bgr = System.Drawing.Color.FromArgb(color.Item2, color.Item1, color.Item0);

                                    bool isText = false;
                                    foreach (var t in textColors)
                                    {
                                        if (IsWithinRange(bgr, t.Item1, t.Item2))
                                        {
                                            isText = true;
                                            break;
                                        }
                                    }

                                    bool isBackground = backgroundColor != null &&
                                                        IsWithinRange(bgr, backgroundColor.Item1, backgroundColor.Item2);

                                    if (isText)
                                        mat.Set(y, x, new Vec3b(0, 0, 0));       // 검정
                                    else if (isBackground)
                                        mat.Set(y, x, new Vec3b(255, 255, 255)); // 흰색
                                    else
                                        mat.Set(y, x, new Vec3b(127, 127, 127)); // 중간 회색
                                }
                            }

                            using (var processedBitmap = BitmapConverter.ToBitmap(mat))
                            using (var pix = PixConverter.ToPix(processedBitmap))
                            using (var page = ocrEngine.Process(pix, PageSegMode.SingleLine))
                            {
                                string result = page.GetText().Trim();
                                string digits = new string(result.Where(char.IsDigit).ToArray());

                                if (string.IsNullOrWhiteSpace(digits) && result.Contains("O"))
                                    digits = "0";

                                return int.TryParse(digits, out int value) ? value : -1;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("OCR 처리 중 오류: " + ex.Message);
                        return -1;
                    }
                }
            }
        }

        public Bitmap GetProcessedRoiBitmap(string roiSettingKey = "Roi_Gold")
        {
            using (var texture = getTextureFunc())
            {
                if (texture == null)
                    return null;

                using (var rawBitmap = Direct3D11Helper.ExtractBitmapFromTexture(texture))
                {
                    var roiStr = SettingsManager.Current[roiSettingKey];
                    if (string.IsNullOrWhiteSpace(roiStr))
                        return null;

                    var roiParse = ParseRoiHelper.ParseRectFromSettings(roiStr);
                    var roi = new Rectangle(roiParse.X, roiParse.Y, roiParse.Width, roiParse.Height);

                    if (roi.Width <= 0 || roi.Height <= 0) return null;
                    if (roi.X < 0 || roi.Y < 0 || roi.Right > rawBitmap.Width || roi.Bottom > rawBitmap.Height) return null;

                    try { return rawBitmap.Clone(roi, PixelFormat.Format32bppArgb); }
                    catch { return null; }
                }
            }
        }

        private bool IsWithinRange(System.Drawing.Color target, System.Drawing.Color baseColor, int range)
        {
            return Math.Abs(target.R - baseColor.R) <= range &&
                   Math.Abs(target.G - baseColor.G) <= range &&
                   Math.Abs(target.B - baseColor.B) <= range;
        }

        public void LoadFilterSettings()
        {
            textColors = new List<Tuple<System.Drawing.Color, int>>();

            AddTextColor(SettingsManager.Current.TextColor1, SettingsManager.Current.TextRange1);
            AddTextColor(SettingsManager.Current.TextColor2, SettingsManager.Current.TextRange2);
            AddTextColor(SettingsManager.Current.TextColor3, SettingsManager.Current.TextRange3);

            try
            {
                var bg = SettingsManager.Current.BackgroundColor;
                if (!string.IsNullOrWhiteSpace(bg))
                {
                    var bgColor = ColorTranslator.FromHtml(bg);
                    backgroundColor = new Tuple<System.Drawing.Color, int>(bgColor, SettingsManager.Current.BackgroundRange);
                }
            }
            catch
            {
                backgroundColor = null;
            }
        }

        public void RefreshFilterSettings()
        {
            LoadFilterSettings();
        }

        private void AddTextColor(string hex, int range)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(hex) && range > 0)
                {
                    // 구버전 호환: System.Windows.Media.Color.ToString()이 "#FFRRGGBB"로 저장했던 경우
                    // ColorTranslator.FromHtml()은 "#RRGGBB"(6자리)만 지원하므로 알파 제거
                    if (hex.Length == 9 && hex.StartsWith("#"))
                        hex = "#" + hex.Substring(3);

                    var color = ColorTranslator.FromHtml(hex);
                    textColors.Add(new Tuple<System.Drawing.Color, int>(color, range));
                }
            }
            catch
            {
                // 무시
            }
        }
    }
}
