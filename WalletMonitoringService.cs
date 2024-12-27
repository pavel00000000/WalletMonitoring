using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Numerics;
using System.Threading.Tasks;
using System.Web;

namespace UnifiedMonitoring.Services
{
    public interface IWalletMonitoringService
    {
        Task MonitorWalletsAsync();
        Task MonitorVolumesAsync();
    }

    public class WalletMonitoringService : IWalletMonitoringService
    {
        private static readonly Dictionary<string, decimal> LastBalances = new();

        private static readonly string ContractAddress = "0xB299751B088336E165dA313c33e3195B8c6663A6";
        private static readonly string WalletApiKey = "7TB1R1YDJMNHKAIF16ZT29UJFSP5ZSDF3H";
        private static readonly string VolumeApiKey = "3d2bc0e4-b7f7-4801-9de4-6a82db021acc";
        private static readonly List<string> Addresses = new()
{
    "0xa7ef98012A47622117C14FE29337104B0B84685C",
    "0xAEadC652657f603144A7F687C50f057720EE49B1",
    "0x8735dC4Dd38E005919f5E04409cA120406D1209E",
    "0x42Ba5701A97DB9d16B3ddC94172045423121b76c"
};

        private readonly ITelegramService _telegramService;
        private static Timer _timer;

        public WalletMonitoringService(ITelegramService telegramService)
        {
            _telegramService = telegramService;
        }

        public async Task MonitorWalletsAsync()
        {
            // Создаем таймер для выполнения мониторинга каждые 10 минут
            _timer = new Timer(async _ =>
            {
                await CheckWalletBalancesAsync();
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        }

        private async Task CheckWalletBalancesAsync()
        {
            var client = new RestClient("https://api.etherscan.io/v2/api?chainid=1");

            // Перебираем все адреса
            foreach (var address in Addresses)
            {
                Console.WriteLine($"Проверяем баланс для адреса: {address}");

                // Создаем запрос
                var request = new RestRequest()
                    .AddQueryParameter("module", "account")
                    .AddQueryParameter("action", "tokenbalance")
                    .AddQueryParameter("contractaddress", ContractAddress)
                    .AddQueryParameter("address", address)
                    .AddQueryParameter("tag", "latest")
                    .AddQueryParameter("apikey", WalletApiKey);

                // Выполняем запрос
                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    Console.WriteLine($"Ошибка при запросе баланса для адреса {address}: {response.ErrorMessage}");
                    Console.WriteLine($"Ответ от сервера: {response.Content}");
                    continue; // Переходим к следующему адресу в случае ошибки
                }

                // Проверяем, успешен ли запрос
                if (!response.IsSuccessful)
                {
                    Console.WriteLine($"Ошибка при запросе баланса для адреса {address}: {response.ErrorMessage}");
                    continue; // Переходим к следующему адресу в случае ошибки
                }


                try
                {
                    // Десериализуем полученный ответ
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response.Content);

                    // Проверяем, что ответ имеет нужные данные
                    if (apiResponse?.Result != null)
                    {
                        // Преобразуем результат в BigInteger и конвертируем в десятичный баланс
                        BigInteger balanceBigInt = BigInteger.Parse(apiResponse.Result);
                        decimal balance = (decimal)balanceBigInt / (decimal)Math.Pow(10, 18);

                        // Проверяем, изменился ли баланс
                        if (LastBalances.ContainsKey(address) && LastBalances[address] != balance)
                        {
                            string changeType = balance > LastBalances[address] ? "пополнение" : "снятие";
                            decimal difference = Math.Abs(balance - LastBalances[address]);

                            // Отправляем уведомление в Telegram
                            await _telegramService.SendMessageAsync($"Баланс изменился для адреса {address}:\nТип: {changeType}\nИзменение: {difference:N8}\nТекущий баланс: {balance:N8}");

                            // Обновляем последний баланс для адреса
                            LastBalances[address] = balance;
                        }
                        else if (!LastBalances.ContainsKey(address))
                        {
                            // Если баланс для этого адреса еще не был записан, добавляем его
                            LastBalances[address] = balance;
                        }
                        else
                        {
                            // Если баланс не изменился, просто отправляем актуальный баланс
                            await _telegramService.SendMessageAsync($"Текущий баланс для адреса {address}: {balance:N8}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Ответ не содержит данных для адреса {address}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка обработки данных для адреса {address}: {ex.Message}");
                }
            }
        }

        public async Task MonitorVolumesAsync()
        {
            var volume = await GetVeloVolumeAsync();
            await _telegramService.SendMessageAsync(volume);

            var price = await GetStarTokenPriceAsync();
            await _telegramService.SendMessageAsync(price);
        }

        private async Task<string> GetStarTokenPriceAsync()
        {
            var url = new UriBuilder("https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest");

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["start"] = "1";
            queryString["limit"] = "5000";
            queryString["convert"] = "USD";
            url.Query = queryString.ToString();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", VolumeApiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await client.GetStringAsync(url.ToString());
            var jsonResponse = JObject.Parse(response);

            var data = jsonResponse["data"];
            if (data == null)
                throw new Exception("Нет данных о криптовалютах в ответе.");

            foreach (var crypto in data)
            {
                if (crypto["symbol"]?.ToString().ToUpper() == "STAR")
                {
                    var priceUsd = crypto["quote"]?["USD"]?["price"];
                    return $"Текущая цена Star: {priceUsd:C2} USD";
                }
            }

            return "Токен Star не найден.";
        }

        private async Task<string> GetVeloVolumeAsync()
        {
            var url = new UriBuilder("https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest");

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["start"] = "1";
            queryString["limit"] = "5000";
            queryString["convert"] = "USD";
            url.Query = queryString.ToString();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-CMC_PRO_API_KEY", VolumeApiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await client.GetStringAsync(url.ToString());
            var jsonResponse = JObject.Parse(response);

            var data = jsonResponse["data"];
            if (data == null)
                throw new Exception("Нет данных о криптовалютах в ответе.");

            foreach (var crypto in data)
            {
                if (crypto["symbol"]?.ToString().ToUpper() == "STAR")
                {
                    var volumeUsd = crypto["quote"]?["USD"]?["volume_24h"];
                    return $"24h объем Star в USD: {volumeUsd}";
                }
            }

            return "Токен Star не найден.";
        }

        private class ApiResponse
        {
            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("result")]
            public string Result { get; set; }
        }
    }
}
