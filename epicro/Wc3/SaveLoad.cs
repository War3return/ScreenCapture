using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace epicro.Wc3
{
    /// <summary>
    /// 간단한 JSON 파일 직렬화/역직렬화 유틸리티입니다.
    /// 파일은 epicro 실행 파일과 같은 디렉토리에 저장됩니다.
    /// </summary>
    internal static class SaveLoad
    {
        private static string BasePath => AppDomain.CurrentDomain.BaseDirectory;

        public static void Save<T>(string key, T data)
        {
            try
            {
                string path = Path.Combine(BasePath, key + ".json");
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SaveLoad.Save] {key}: {ex.Message}"); }
        }

        public static T Load<T>(string key)
        {
            try
            {
                string path = Path.Combine(BasePath, key + ".json");
                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (FileNotFoundException) { /* 파일 없음 — 정상 */ }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SaveLoad.Load] {key}: {ex.Message}"); }
            return default(T);
        }
    }
}
