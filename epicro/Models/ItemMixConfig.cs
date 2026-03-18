using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace epicro.Models
{
    public class ItemMixConfig
    {
        public List<string> Materials { get; }
        public string Category { get; }
        public ItemMixConfig(string category, params string[] materials)
        {
            Category = category;
            Materials = materials.ToList();
        }

    }

    public class ItemMixConfigManager
    {
        public static Dictionary<string, ItemMixConfig> GetItemMixConfigs()
        {
            var ItemMixconfigs = new Dictionary<string, ItemMixConfig>
            {
                // 무기
                { "향와리", new ItemMixConfig("무기", "비전인술", "5미차", "적비기", "카쿠즈반지", "사소리반지") },
                { "향풍마", new ItemMixConfig("무기","데달반지", "5미차", "적비기", "카쿠즈반지", "사소리반지") },
                { "향선지", new ItemMixConfig("무기", "자분", "5미차", "적비기", "카쿠즈반지", "사소리반지") },
                { "향사메", new ItemMixConfig("무기", "비전인술", "비둔", "속심", "카쿠즈반지", "사소리반지") },
                { "향번송", new ItemMixConfig("무기", "데달반지", "비둔", "속심", "카쿠즈반지", "사소리반지") },
                { "향파초", new ItemMixConfig("무기", "자분", "비둔", "속심", "카쿠즈반지", "사소리반지") },
                { "축와리", new ItemMixConfig("무기", "호증", "포말", "우문", "6미차") },
                { "축풍마", new ItemMixConfig("무기", "호증", "포말", "우문", "6미차") },
                { "축선지", new ItemMixConfig("무기", "호증", "포말", "우문", "6미차") },
                { "축사메", new ItemMixConfig("무기", "호증", "포말", "우문", "6미차") },
                { "축번송", new ItemMixConfig("무기", "호증", "포말", "우문", "6미차") },
                { "축파초", new ItemMixConfig("무기", "호증", "포말", "우문", "6미차") },
                { "신와리", new ItemMixConfig("무기", "미후네", "호증", "칠날", "7미차", "스사") },
                { "신풍마", new ItemMixConfig("무기", "미후네", "호증", "칠날", "7미차", "스사") },
                { "신선지", new ItemMixConfig("무기", "미후네", "호증", "칠날", "7미차", "스사") },
                { "신사메", new ItemMixConfig("무기", "미후네", "호증", "칠날", "7미차", "스사") },
                { "신번송", new ItemMixConfig("무기", "미후네", "호증", "칠날", "7미차", "스사") },
                { "신파초", new ItemMixConfig("무기", "미후네", "호증", "칠날", "7미차", "스사") },

                // 방어구
                { "향불인갑", new ItemMixConfig("방어구", "비둔", "서클릿", "토비가면", "적비기", "카쿠즈반지") },
                { "향암갑", new ItemMixConfig("방어구", "비둔", "서클릿", "토비가면", "적비기", "카쿠즈반지") },
                { "향구미도포", new ItemMixConfig("방어구", "비둔", "서클릿", "토비가면", "적비기", "카쿠즈반지") },
                { "향불무갑", new ItemMixConfig("방어구", "비둔", "서클릿", "토비가면", "속심", "사소리반지") },
                { "향선인도포", new ItemMixConfig("방어구", "비둔", "서클릿", "토비가면", "속심", "사소리반지") },
                { "향차갑", new ItemMixConfig("방어구", "비둔", "서클릿", "토비가면", "속심", "사소리반지") },
                { "축불인갑", new ItemMixConfig("방어구", "호증", "포말", "6미인주력", "장대") },
                { "축암갑", new ItemMixConfig("방어구", "호증", "포말", "6미인주력", "장대") },
                { "축구미도포", new ItemMixConfig("방어구", "호증", "포말", "6미인주력", "장대") },
                { "축불무갑", new ItemMixConfig("방어구", "호증", "포말", "6미인주력", "장대") },
                { "축선인도포", new ItemMixConfig("방어구", "호증", "포말", "6미인주력", "장대") },
                { "축차갑", new ItemMixConfig("방어구", "호증", "포말", "6미인주력", "장대") },
                { "신불인갑", new ItemMixConfig("방어구", "백사비늘", "스사", "호증", "7미차", "7날") },
                { "신암갑", new ItemMixConfig("방어구", "백사비늘", "스사", "호증", "7미차", "7날") },
                { "신구미도포", new ItemMixConfig("방어구", "백사비늘", "스사", "호증", "7미차", "7날") },
                { "신불무갑", new ItemMixConfig("방어구", "백사비늘", "스사", "호증", "7미차", "7날") },
                { "신선인도포", new ItemMixConfig("방어구", "백사비늘", "스사", "호증", "7미차", "7날") },
                { "신차갑", new ItemMixConfig("방어구", "백사비늘", "스사", "호증", "7미차", "7날") },
 
                // 장갑
                { "향완력장갑", new ItemMixConfig("장갑", "서클릿", "토비가면", "백사세포", "금술서") },
                { "향신속장갑", new ItemMixConfig("장갑", "서클릿", "토비가면", "백사세포", "전생술") },
                { "향지혜장갑", new ItemMixConfig("장갑", "서클릿", "토비가면", "전생술", "금술서") },
                { "향생보", new ItemMixConfig("장갑", "전생술", "금술서", "백사세포", "포말", "6미차") },
                { "향차보", new ItemMixConfig("장갑", "전생술", "금술서", "백사세포", "포말", "6미차") },
                { "향호투", new ItemMixConfig("장갑", "전생술", "금술서", "백사세포", "포말", "6미차") },
                { "축완력장갑", new ItemMixConfig("장갑", "호증", "포말", "6미차", "라카창", "라카방") },
                { "축신속장갑", new ItemMixConfig("장갑", "호증", "포말", "6미차", "라카창", "라카방") },
                { "축지헤장갑", new ItemMixConfig("장갑", "호증", "포말", "6미차", "라카창", "라카방") },
                { "축생보", new ItemMixConfig("장갑", "호증", "포말", "6미차", "라카창", "라카방") },
                { "축차보", new ItemMixConfig("장갑", "호증", "포말", "6미차", "라카창", "라카방") },
                { "축호투", new ItemMixConfig("장갑", "호증", "포말", "6미차", "라카창", "라카방") },
                { "신완력장갑", new ItemMixConfig("장갑", "화심", "백사비늘", "스사", "7날", "7미차") },
                { "신신속장갑", new ItemMixConfig("장갑", "화심", "백사비늘", "스사", "7날", "7미차") },
                { "신지헤장갑", new ItemMixConfig("장갑", "화심", "백사비늘", "스사", "7날", "7미차") },
                { "신생보", new ItemMixConfig("장갑", "화심", "백사비늘", "스사", "7날", "7미차") },
                { "신차보", new ItemMixConfig("장갑", "화심", "백사비늘", "스사", "7날", "7미차") },
                { "신호투", new ItemMixConfig("장갑", "화심", "백사비늘", "스사", "7날", "7미차") },
 
                // 악세
                { "향완력목걸", new ItemMixConfig("악세", "전생술", "금술서", "백사세포", "6미인주력", "장대") },
                { "향신속목걸", new ItemMixConfig("악세", "전생술", "금술서", "백사세포", "6미인주력", "장대") },
                { "향지헤목걸", new ItemMixConfig("악세", "전생술", "금술서", "백사세포", "6미인주력", "장대") },
                { "향반지(힘)", new ItemMixConfig("악세", "전생술", "금술서", "백사세포", "6미인주력", "장대") },
                { "향반지(민)", new ItemMixConfig("악세", "전생술", "금술서", "백사세포", "6미인주력", "장대") },
                { "향반지(지)", new ItemMixConfig("악세", "전생술", "금술서", "백사세포", "6미인주력", "장대") },
                { "축완력목걸", new ItemMixConfig("악세", "호증", "라카창", "라카방", "달열") },
                { "축신속목걸", new ItemMixConfig("악세", "호증", "라카창", "라카방", "달열") },
                { "축지혜목걸", new ItemMixConfig("악세", "호증", "라카창", "라카방", "달열") },
                { "축반지(힘)", new ItemMixConfig("악세", "호증", "라카창", "라카방", "달열") },
                { "축반지(민)", new ItemMixConfig("악세", "호증", "라카창", "라카방", "달열") },
                { "축반지(지)", new ItemMixConfig("악세", "호증", "라카창", "라카방", "달열") },
                { "신완력목걸", new ItemMixConfig("악세", "스사", "대심", "화심", "백사비늘") },
                { "신신속목걸", new ItemMixConfig("악세", "스사", "대심", "화심", "백사비늘") },
                { "신지혜목걸", new ItemMixConfig("악세", "스사", "대심", "화심", "백사비늘") },
                { "신반지(힘)", new ItemMixConfig("악세", "스사", "대심", "화심", "백사비늘") },
                { "신반지(민)", new ItemMixConfig("악세", "스사", "대심", "화심", "백사비늘") },
                { "신반지(지)", new ItemMixConfig("악세", "스사", "대심", "화심", "백사비늘") },
 
                // 히든
                { "강성", new ItemMixConfig("히든", "유기토히든", "토비히든", "달열", "우문") },
                { "향레바", new ItemMixConfig("히든", "6미차", "포말", "우문", "호타루") },
                { "향금술", new ItemMixConfig("히든", "오로치히든", "포말", "우문", "호타루") },
                { "향선문", new ItemMixConfig("히든", "우문", "토비가면", "서클릿", "오로치히든") },
                { "향생막", new ItemMixConfig("히든", "전생술", "금술서", "백사세포") },
                { "향차막", new ItemMixConfig("히든", "전생술", "금술서", "백사세포") },
                { "빛성", new ItemMixConfig("히든", "달열", "호증", "7날", "7미차") },
                { "축레바", new ItemMixConfig("히든", "7미차", "7날", "호증", "미후네") },
                { "축선문", new ItemMixConfig("히든", "7미차", "7날", "호증", "미후네") },
                { "축금술", new ItemMixConfig("히든", "달열", "호증", "미후네", "7날") },
                { "축생막", new ItemMixConfig("히든", "미후네", "호증", "7날", "7미차") },
                { "축차막", new ItemMixConfig("히든", "미후네", "호증", "7날", "7미차") },
                { "강빛성", new ItemMixConfig("히든", "육문", "대심", "정수", "베히창") },
                { "신금술", new ItemMixConfig("히든", "육문", "대심", "화심", "베히창") },
                { "신레바", new ItemMixConfig("히든", "육문", "대심", "정수", "베히창", "화심") },
                { "신선문", new ItemMixConfig("히든", "왕관", "대심") },
                { "신생막", new ItemMixConfig("히든", "왕관", "대심") },
                { "신차막", new ItemMixConfig("히든", "왕관", "대심") },

                // 벨트
                { "재괴벨", new ItemMixConfig("벨트", "우문", "토비히든") },
                { "재사벨", new ItemMixConfig("벨트", "우문", "유기토히든") },
                { "심벨전투", new ItemMixConfig("벨트", "라카창", "라카방") },
                { "심벨경험", new ItemMixConfig("벨트", "라카창", "라카방") },
                { "재심벨전투", new ItemMixConfig("벨트", "달열", "미후네", "호증") },
                { "재심벨경험", new ItemMixConfig("벨트", "달열", "미후네", "호증") },
                { "재선벨", new ItemMixConfig("벨트", "육문", "호증") },
                { "공벨", new ItemMixConfig("벨트", "백사비늘", "스사") },
                { "재공벨", new ItemMixConfig("벨트", "화심", "스사", "백사비늘") },
                { "재지벨", new ItemMixConfig("벨트", "베히창", "정수","육문") },

                // 보구함
                { "보구함 1강", new ItemMixConfig("보구함", "스사", "백사비늘") },
                { "보구함 2강", new ItemMixConfig("보구함", "스사", "백사비늘") },
                { "보구함 3강", new ItemMixConfig("보구함", "스사", "백사비늘") },
                { "보구함 4강", new ItemMixConfig("보구함", "스사", "백사비늘") },
                { "보구함 5강", new ItemMixConfig("보구함", "스사", "백사비늘") },
                { "보구함 6강", new ItemMixConfig("보구함", "육문") },
                { "보구함 7강", new ItemMixConfig("보구함", "육문") },
                { "보구함 8강", new ItemMixConfig("보구함", "대심", "화심") },
                { "보구함 9강", new ItemMixConfig("보구함", "대심", "화심") },
                { "보구함 10강", new ItemMixConfig("보구함", "베히창", "정수") },
                { "보구함 11강", new ItemMixConfig("보구함", "베히창", "정수") },

            };
            return ItemMixconfigs;
        }
    }
}

