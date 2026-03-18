using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace epicro.Helpers
{
    public static class ParseRoiHelper
    {
        public static Int32Rect ParseRectFromSettings(string roiSetting)
        {
            if (string.IsNullOrWhiteSpace(roiSetting))
                return Int32Rect.Empty;

            var parts = roiSetting.Split(',');
            if (parts.Length < 4)
                return Int32Rect.Empty;

            if (!int.TryParse(parts[0], out int x) ||
                !int.TryParse(parts[1], out int y) ||
                !int.TryParse(parts[2], out int w) ||
                !int.TryParse(parts[3], out int h))
                return Int32Rect.Empty;

            return new Int32Rect(x, y, w, h);
        }

    }
}
