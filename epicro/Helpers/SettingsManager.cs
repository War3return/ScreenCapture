using System;
using System.IO;
using epicro.Models;
using Newtonsoft.Json;

namespace epicro.Helpers
{
    public static class SettingsManager
    {
        private static readonly string FilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Current { get; private set; } = Load();

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath, System.Text.Encoding.UTF8);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                        return settings;
                }
            }
            catch
            {
                // 파일이 손상됐거나 읽기 실패 시 기본값 사용
            }
            return new AppSettings();
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string json = JsonConvert.SerializeObject(Current, Formatting.Indented);
                File.WriteAllText(FilePath, json, System.Text.Encoding.UTF8);
            }
            catch
            {
                // 저장 실패 시 무시 (읽기 전용 환경 등 예외 상황 대비)
            }
        }

        public static void Reload()
        {
            Current = Load();
        }
    }
}
