using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using epicro.Helpers;

namespace epicro.Models
{
    public class BossConfig
    {
        public string ImagePath { get; set; }
        public string Key { get; set; }
        public string Zone { get; set; }
        public Int32Rect Roi { get; set; }
        public int Tree { get; set; }

        public BossConfig(string imagePath, string key, string zone, Int32Rect roi, int tree)
        {
            ImagePath = imagePath;
            Key = key;
            Zone = zone;
            Roi = roi;
            Tree = tree;
        }
    }

    public class BossConfigManager
    {
        public static Dictionary<string, BossConfig> GetBossConfigs()
        {
            var bossConfigs = new Dictionary<string, BossConfig>
            {
                // 유적지 보스
                { "유적지Q", new BossConfig("b1q.png", "Q", "유적지", GetROI("Roi_Q"), 150) },
                { "유적지W", new BossConfig("b1w.png", "W", "유적지", GetROI("Roi_W"), 200) },
                { "유적지E", new BossConfig("b1e.png", "E", "유적지", GetROI("Roi_E"), 300) },
                { "유적지R", new BossConfig("b1r.png", "R", "유적지", GetROI("Roi_R"), 350) },
                { "유적지A", new BossConfig("b1a.png", "A", "유적지", GetROI("Roi_A"), 400) },

                // 해역 보스
                { "해역Q", new BossConfig("b2q.png", "Q", "해역", GetROI("Roi_Q"), 900) },
                { "해역W", new BossConfig("b2w.png", "W", "해역", GetROI("Roi_W"), 900) },

                // 태엽 보스
                { "태엽Q", new BossConfig("b3q.png", "Q", "태엽", GetROI("Roi_Q"), 1200) },
                { "태엽W", new BossConfig("b3w.png", "W", "태엽", GetROI("Roi_W"), 1200) },

                // 키사메 보스
                { "자부자", new BossConfig("zabuza.png", "Q", "키사메", GetROI("Roi_Q"), 1500) },
                { "하쿠", new BossConfig("haku.png", "W", "키사메", GetROI("Roi_W"), 1500) },
                { "키사메", new BossConfig("kisame.png", "E", "키사메", GetROI("Roi_E"), 10000) },
                { "키사메이지", new BossConfig("kisameeasy.png", "R", "키사메", GetROI("Roi_R"), 10000) },

                // 키미 보스
                { "키미", new BossConfig("kimi.png", "Q", "키미", GetROI("Roi_Q"), 15000) },
                { "오로치", new BossConfig("orochi.png", "W", "키미", GetROI("Roi_W"), 70000) },
                { "키미이지", new BossConfig("kimieasy.png", "E", "키미", GetROI("Roi_E"), 15000) },

                // 데달 보스
                { "가아라", new BossConfig("gaara.png", "Q", "데달", GetROI("Roi_Q"), 1500) },
                { "사소리", new BossConfig("sasori.png", "W", "데달", GetROI("Roi_W"), 1500) },
                { "데이다라", new BossConfig("deidara.png", "E", "데달", GetROI("Roi_E"), 20000) },
                { "사완변", new BossConfig("sasoriR.png", "R", "데달", GetROI("Roi_R"), 30000) },
                { "데이다라이지", new BossConfig("deidaraeasy.png", "A", "데달", GetROI("Roi_A"), 20000) },

                // 유기토 보스
                { "히단", new BossConfig("hidan.png", "Q", "유기토", GetROI("Roi_Q"), 8000) },
                { "카쿠즈", new BossConfig("kakuzu.png", "W", "유기토", GetROI("Roi_W"), 30000) },
                { "유기토", new BossConfig("yugito.png", "E", "유기토", GetROI("Roi_E"), 50000) },
                { "히단이지", new BossConfig("hidaneasy.png", "R", "유기토", GetROI("Roi_R"), 8000) },

                // 우타 보스
                { "우타", new BossConfig("uta.png", "Q", "우타", GetROI("Roi_Q"), 70000) },

                // 사신수 보스
                { "현무", new BossConfig("sasinQ.png", "Q", "사성수", GetROI("Roi_Q"), 120000) },
                { "백호", new BossConfig("sasinW.png", "W", "사성수", GetROI("Roi_W"), 160000) },
                { "청룡", new BossConfig("sasinE.png", "E", "사성수", GetROI("Roi_E"), 200000) },
                { "주작", new BossConfig("sasinR.png", "R", "사성수", GetROI("Roi_R"), 240000) },
            };
            return bossConfigs;
        }

        public static Dictionary<string, List<string>> GetZonesToBosses()
        {
            return new Dictionary<string, List<string>>
            {
                { "유적지", new List<string> { "유적지Q", "유적지W", "유적지E", "유적지R", "유적지A" } },
                { "해역", new List<string> { "해역Q", "해역W" } },
                { "태엽", new List<string> { "태엽Q", "태엽W" } },
                { "키사메", new List<string> { "자부자", "하쿠", "키사메", "키사메이지" } },
                { "키미", new List<string> { "키미", "오로치", "키미이지" } },
                { "데달", new List<string> { "가아라", "사소리", "데이다라", "사완변", "데이다라이지" } },
                { "유기토", new List<string> { "히단", "카쿠즈", "유기토", "히단이지" } },
                { "우타", new List<string> { "우타" } },
                { "사성수", new List<string> { "현무", "백호", "청룡", "주작" } },
            };
        }

        private static Int32Rect GetROI(string roiKey)
        {
            string roiValue = SettingsManager.Current[roiKey];
            if (!string.IsNullOrEmpty(roiValue))
            {
                string[] parts = roiValue.Split(',');
                if (parts.Length == 4 &&
                    int.TryParse(parts[0], out int x) &&
                    int.TryParse(parts[1], out int y) &&
                    int.TryParse(parts[2], out int width) &&
                    int.TryParse(parts[3], out int height))
                {
                    return new Int32Rect(x, y, width, height);
                }
            }
            return new Int32Rect(0, 0, 0, 0);
        }
    }
}
