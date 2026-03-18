using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace epicro.Helpers
{
    public class CharacterInfo
    {
        public string   Id               { get; set; } = "";
        public string   LoadName         { get; set; } = "";   // 영혼석 바로 위 줄 (현재 로드명)
        public string   CharName         { get; set; } = "";
        public int      Level            { get; set; }
        public int      Str              { get; set; }
        public int      Agi              { get; set; }
        public int      Int              { get; set; }
        public string   SubAttr          { get; set; } = "";
        public string[] HeroItems        { get; set; } = new string[6];
        // ItemMixConfig의 카테고리 키 (무기/방어구/장갑/악세/히든/벨트)
        public string[] HeroItemCategories { get; set; } = new string[6];
        public string   FilePath         { get; set; } = "";
    }

    public static class SaveFileParser
    {
        // call Preload( "내용" )
        private static readonly Regex _preload = new Regex(@"call Preload\( ""(.*)"" \)");

        // |cffRRGGBB 또는 |cffAARRGGBB (8자리) 형태의 색상 코드 + |r
        private static readonly Regex _colorAll  = new Regex(@"\|c[0-9a-fA-F]{8}|\|r");
        private static readonly Regex _colorText = new Regex(@"\|c[0-9a-fA-F]{8}(.*?)\|r");

        // 아이템 뒤에 붙는 등급/속성/강화 기호 제거: [SR], [지]◆◆, ★★★ 등
        private static readonly Regex _trailSuffix = new Regex(@"(\[.*?\])+[★◆]*$|[★◆]+$");

        // [에픽/무기], [벨트], [해방/조합] 등 아이템 앞 카테고리 괄호
        private static readonly Regex _slotPrefix = new Regex(@"^\s*'\[([^\]]+)\]");

        public static CharacterInfo Parse(string filePath)
        {
            var info = new CharacterInfo
            {
                FilePath  = filePath,
                HeroItems = new string[6],
            };

            string prevContent = "";
            foreach (var line in File.ReadLines(filePath, Encoding.UTF8))
            {
                var m = _preload.Match(line);
                if (!m.Success) continue;

                var content = m.Groups[1].Value;

                // 영혼석 바로 위 줄 = 현재 로드명
                if (content.StartsWith("영혼석 :"))
                {
                    info.LoadName = prevContent.Trim();
                }

                prevContent = content;

                if      (content.StartsWith("ID: "))
                    info.Id = content.Substring(4).Trim();
                else if (content.StartsWith("Char: "))
                    info.CharName = StripColorCodes(content.Substring(6));
                else if (content.StartsWith("Lv: "))
                { if (int.TryParse(content.Substring(4).Trim(), out int lv))  info.Level = lv; }
                else if (content.StartsWith("Str: "))
                { if (int.TryParse(content.Substring(5).Trim(), out int str)) info.Str   = str; }
                else if (content.StartsWith("Agi: "))
                { if (int.TryParse(content.Substring(5).Trim(), out int agi)) info.Agi   = agi; }
                else if (content.StartsWith("Int: "))
                { if (int.TryParse(content.Substring(5).Trim(), out int iit)) info.Int   = iit; }
                else if (content.StartsWith("부속성 : "))
                    info.SubAttr = content.Substring("부속성 : ".Length).Trim();
                else if (content.StartsWith("HeroItem"))
                {
                    var im = Regex.Match(content, @"HeroItem(\d): (.+)");
                    if (im.Success
                        && int.TryParse(im.Groups[1].Value, out int idx)
                        && idx >= 1 && idx <= 6)
                    {
                        var raw = im.Groups[2].Value;
                        info.HeroItems[idx - 1]         = ExtractItemName(raw);
                        info.HeroItemCategories[idx - 1] = ExtractSlotCategory(raw);
                    }
                }
            }

            return info;
        }

        // 색상 코드 전체 제거
        private static string StripColorCodes(string text)
            => _colorAll.Replace(text, "").Trim();

        // [에픽/무기] → "무기", [해방/조합] → "히든", [벨트] → "벨트" 등으로 변환
        private static string ExtractSlotCategory(string raw)
        {
            var m = _slotPrefix.Match(raw);
            if (!m.Success) return "";
            var prefix = m.Groups[1].Value; // "에픽/무기", "벨트", "해방/조합", "히든" 등

            if (prefix.Contains("무기"))   return "무기";
            if (prefix.Contains("갑옷") || prefix.Contains("보조구")) return "방어구";
            if (prefix.Contains("장갑") || prefix.Contains("보호구") || prefix.Contains("투구") || prefix.Contains("갑주")) return "장갑";
            if (prefix.Contains("악세"))   return "악세";
            if (prefix.Contains("벨트"))   return "벨트";
            if (prefix.Contains("조합") || prefix.Contains("히든")) return "히든";
            return "";
        }

        // |cffXXXXXX이름[SR]|r 에서 순수 아이템 이름만 추출
        private static string ExtractItemName(string raw)
        {
            var m = _colorText.Match(raw);
            if (!m.Success) return raw.Trim();

            var name = m.Groups[1].Value;
            // 끝에 붙은 등급([SR],[SSS],[지] 등), 강화(★◆) 제거
            name = _trailSuffix.Replace(name, "").Trim();
            return name;
        }
    }
}
