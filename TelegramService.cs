using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace UnifiedMonitoring.Services
{


    public interface ITelegramService
    {
        Task SendMessageAsync(string message);
    }

    public class TelegramService : ITelegramService
    {
        private static readonly string TelegramBotToken = "7994007891:AAGpWidV5nMzpIPBhNEfx-xaR0cY1qwQRtc";
        private static readonly string TelegramChatId = "-1002322975978";

        public async Task SendMessageAsync(string message)
        {
            using var client = new HttpClient();
            var url = $"https://api.telegram.org/bot{TelegramBotToken}/sendMessage";
            var parameters = new Dictionary<string, string>
            {
                { "chat_id", TelegramChatId },
                { "text", message }
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Сообщение отправлено в Telegram");
            }
            else
            {
                Console.WriteLine($"Ошибка отправки сообщения в Telegram: {response.StatusCode}");
            }
        }
    
    }
}
