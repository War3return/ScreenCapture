using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace epicro.Helpers
{
    // Telegram API에 직접 알림 전송 (등록된 Chat ID 목록 사용)
    public class TelegramBotService : IDisposable
    {
        private string _botToken;
        private readonly HashSet<long> _chatIds = new HashSet<long>();
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        public bool IsEnabled { get; set; } = true;
        public int RegisteredCount => _chatIds.Count;

        public TelegramBotService(string savedChatIds)
        {
            _botToken = TelegramConfig.BotToken;

            if (!string.IsNullOrEmpty(savedChatIds))
            {
                foreach (var part in savedChatIds.Split(','))
                {
                    if (long.TryParse(part.Trim(), out long id))
                        _chatIds.Add(id);
                }
            }
        }

        public async Task BroadcastAsync(string message)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(_botToken) || _chatIds.Count == 0) return;
            var tasks = _chatIds.ToList().Select(id => SendAsync(id, message));
            await Task.WhenAll(tasks);
        }

        public async Task SendAsync(long chatId, string message)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("chat_id", chatId.ToString()),
                    new KeyValuePair<string, string>("text", message)
                });
                await _http.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Telegram] Send error: {ex.Message}");
            }
        }

        public void UpdateBotToken(string token)
        {
            _botToken = token ?? "";
        }

        public void UpdateChatIds(string commaSeparated)
        {
            _chatIds.Clear();
            if (!string.IsNullOrEmpty(commaSeparated))
            {
                foreach (var part in commaSeparated.Split(','))
                {
                    if (long.TryParse(part.Trim(), out long id))
                        _chatIds.Add(id);
                }
            }
        }

        public void Dispose() { }
    }
}
