namespace epicro.Helpers
{
    internal static class TelegramConfig
    {
        // 봇 토큰은 텔레그램 연동 설정 창에서 입력 → AppSettings에 저장
        public static string BotToken => SettingsManager.Current.TelegramBotToken;
    }
}
