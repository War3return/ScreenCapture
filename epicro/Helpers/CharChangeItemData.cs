using System;
using System.Collections.Generic;
using System.Linq;

namespace epicro.Helpers
{
    /// <summary>
    /// 캐릭터변경 신청 시 각 슬롯에서 교환 가능한 아이템 목록을 제공합니다.
    /// 규칙: 같은 등급(신성한/축복받은/향상된) + 같은 서브카테고리 내에서만 교환 가능.
    /// 벨트: 재련된 여부 + 타입(심연/지옥/공허/선인)이 같은 것끼리만 교환 가능.
    /// </summary>
    public static class CharChangeItemData
    {
        private static readonly string[] Tiers = { "신성한", "축복받은", "향상된" };

        // ── 무기 ─────────────────────────────────────────────────────────────
        private static readonly (string sub, string[] bases)[] WeaponGroups =
        {
            ("보스", new[] { "파초선", "번개도 송곳니", "사메하다" }),
            ("사냥", new[] { "선인 지팡이", "풍마 수리검", "카부토와리" }),
        };

        // ── 갑옷(방어구) ──────────────────────────────────────────────────────
        private static readonly (string sub, string[] bases)[] ArmorGroups =
        {
            ("보스", new[] { "불의 인장", "암부 갑옷", "구미 도포" }),
            ("사냥", new[] { "차크라 갑옷", "선인 도포", "무사 갑옷" }),
        };

        // ── 장갑 ─────────────────────────────────────────────────────────────
        private static readonly (string sub, string[] bases)[] GloveGroups =
        {
            ("보호구", new[] { "전사의 생명 보호구", "마술사의 마력 보호구", "호카게 투구" }),
            ("장갑",   new[] { "지혜의 장갑", "신속의 장갑", "완력의 장갑" }),
        };

        // ── 악세서리 ──────────────────────────────────────────────────────────
        private static readonly (string sub, string[] bases)[] AccessoryGroups =
        {
            ("반지",   new[] { "선인 반지(지)", "선인 반지(민)", "선인 반지(힘)" }),
            ("목걸이", new[] { "지혜의 목걸이", "신속의 목걸이", "완력의 목걸이" }),
        };

        // ── 히든 ─────────────────────────────────────────────────────────────
        private static readonly (string sub, string[] bases)[] HiddenGroups =
        {
            ("금술",   new[] { "불의 금술", "천둥의 금술", "물의 금술" }),
            ("문장",   new[] { "선인의 문장(힘)", "선인의 문장(민)", "선인의 문장(지)" }),
            ("보호막", new[] { "생명의 보호막", "마력의 보호막" }),
        };

        // ── 벨트 (등급 없음, 재련 여부 + 타입 기준) ─────────────────────────
        private static readonly string[][] BeltGroups =
        {
            new[] { "재련된 심연 벨트(힘)", "재련된 심연 벨트(민)", "재련된 심연 벨트(지)" },
            new[] { "심연 벨트(힘)",         "심연 벨트(민)",         "심연 벨트(지)"         },
            new[] { "재련된 지옥 벨트(힘)", "재련된 지옥 벨트(민)", "재련된 지옥 벨트(지)" },
            new[] { "지옥 벨트(힘)",         "지옥 벨트(민)",         "지옥 벨트(지)"         },
            new[] { "재련된 공허 벨트(힘)", "재련된 공허 벨트(민)", "재련된 공허 벨트(지)" },
            new[] { "공허 벨트(힘)",         "공허 벨트(민)",         "공허 벨트(지)"         },
            new[] { "재련된 선인 벨트(힘)", "재련된 선인 벨트(민)", "재련된 선인 벨트(지)" },
            new[] { "선인 벨트(힘)",         "선인 벨트(민)",         "선인 벨트(지)"         },
        };

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// 현재 장착 아이템명과 슬롯 카테고리를 받아 교환 가능한 아이템 목록을 반환합니다.
        /// 카테고리 태그가 잘못 분류된 경우 이름 기반으로 전체 그룹에서 재검색합니다.
        /// </summary>
        public static List<string> GetSwappableItems(string itemName, string category)
        {
            if (string.IsNullOrWhiteSpace(itemName)) return new List<string>();

            // 1차: 주어진 카테고리로 검색
            var result = SearchByCategory(itemName, category);
            if (result.Count > 0) return result;

            // 2차: 세이브 파일 태그 오분류 대응 — 모든 카테고리 전체 검색
            foreach (var cat in new[] { "무기", "방어구", "장갑", "악세", "히든", "벨트" })
            {
                result = SearchByCategory(itemName, cat);
                if (result.Count > 0) return result;
            }

            return new List<string>();
        }

        private static List<string> SearchByCategory(string itemName, string category)
        {
            switch (category)
            {
                case "무기":   return GetTieredItems(itemName, WeaponGroups);
                case "방어구": return GetTieredItems(itemName, ArmorGroups);
                case "장갑":   return GetTieredItems(itemName, GloveGroups);
                case "악세":   return GetTieredItems(itemName, AccessoryGroups);
                case "히든":   return GetTieredItems(itemName, HiddenGroups);
                case "벨트":   return GetBeltItems(itemName);
                default:       return new List<string>();
            }
        }

        // ── 등급 + 서브카테고리 기반 검색 ────────────────────────────────────
        private static List<string> GetTieredItems(string itemName, (string sub, string[] bases)[] groups)
        {
            // 1. 등급 감지
            string tier     = "";
            string baseName = itemName.Trim();   // 혹시 모를 공백 제거
            foreach (var t in Tiers)
            {
                if (baseName.StartsWith(t + " ", StringComparison.OrdinalIgnoreCase))
                {
                    tier     = t;
                    baseName = baseName.Substring(t.Length + 1).Trim();
                    break;
                }
            }

            // 2. 서브카테고리 찾기
            foreach (var (_, bases) in groups)
            {
                foreach (var b in bases)
                {
                    if (baseName.Equals(b, StringComparison.OrdinalIgnoreCase)
                        || baseName.StartsWith(b, StringComparison.OrdinalIgnoreCase))
                    {
                        // 같은 등급 + 같은 서브의 전체 아이템 반환
                        return bases
                            .Select(x => string.IsNullOrEmpty(tier) ? x : $"{tier} {x}")
                            .ToList();
                    }
                }
            }

            return new List<string>();
        }

        // ── 벨트 그룹 검색 ───────────────────────────────────────────────────
        private static List<string> GetBeltItems(string itemName)
        {
            // 1. 완전 일치
            foreach (var group in BeltGroups)
            {
                if (group.Any(item => item.Equals(itemName, StringComparison.OrdinalIgnoreCase)))
                    return group.ToList();
            }

            // 2. 파싱 시 [힘]/[민]/[지] 대괄호 접미사가 잘린 경우 — prefix로 폴백 매칭
            //    "심연 벨트(힘)" → prefix "심연 벨트"  vs  파싱된 itemName "심연 벨트"
            foreach (var group in BeltGroups)
            {
                if (group.Any(item =>
                {
                    // "(힘)", "(민)", "(지)" 제거한 prefix
                    var prefix = System.Text.RegularExpressions.Regex
                        .Replace(item, @"\([힘민지]\)$", "").Trim();
                    return itemName.Equals(prefix, StringComparison.OrdinalIgnoreCase);
                }))
                    return group.ToList();
            }

            return new List<string>();
        }
    }
}
