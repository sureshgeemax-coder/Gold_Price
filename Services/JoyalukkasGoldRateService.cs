using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using GoldPriceDashboard.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace GoldPriceDashboard.Services
{
    public class JoyalukkasGoldRateService : IJoyalukkasGoldRateService
    {
        private const string URL_KEY = "JoyalukkasGoldUrl";
        private const int TIMEOUT_SECONDS = 30;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) };
        private readonly ILogger _logger;

        public JoyalukkasGoldRateService()
        {
            _logger = Log.ForContext<JoyalukkasGoldRateService>();
        }

        public async Task<GoldRateResult> GetGoldRatesAsync()
        {
            var result = new GoldRateResult { Source = "Joyalukkas", Success = false };
            try
            {
                var url = System.Configuration.ConfigurationManager.AppSettings[URL_KEY] ?? "https://www.joyalukkas.com/graphql";
                const string query = "query getgoldrates{getgoldrates{Status metal_rate_time Data{GOLD_22KT_RATE GOLD_24KT_RATE}}}";
                var requestUrl = url + "?query=" + Uri.EscapeDataString(query) + "&operationName=getgoldrates&variables=%7B%7D";
                _logger.Information("Fetching Joyalukkas gold rates from {Url}", url);

                using (var request = new HttpRequestMessage(HttpMethod.Get, requestUrl))
                {
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("Store", "sg");
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var payload = JObject.Parse(json);
                    var goldRates = payload["data"]?["getgoldrates"];
                    var rates = goldRates?["Data"]?[0];
                    var rate916 = ParseRate(rates?["GOLD_22KT_RATE"]?.Value<string>());
                    var rate999 = ParseRate(rates?["GOLD_24KT_RATE"]?.Value<string>());
                    var updated = ParseUpdatedDate(goldRates?["metal_rate_time"]?.Value<string>());

                    if (rate916.HasValue && rate999.HasValue)
                    {
                        result.Rate916 = rate916.Value;
                        result.Rate999 = rate999.Value;
                        result.LastUpdated = updated ?? DateTime.Now;
                        result.Currency = "SGD";
                        result.SourceUrl = "https://www.joyalukkas.com/sg/goldrate";
                        result.Success = true;
                    }
                    else
                    {
                        result.ErrorMessage = "Unable to parse Singapore 22KT and 24KT rates from Joyalukkas.";
                        _logger.Warning("Failed to parse Joyalukkas rates. 22KT={R916}, 24KT={R999}", rate916, rate999);
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Failed to retrieve Joyalukkas rates: {ex.Message}";
                _logger.Error(ex, "Error fetching Joyalukkas gold rates");
            }

            return result;
        }

        private decimal? ParseRate(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) && rate > 0 && rate < 10000)
            {
                return rate;
            }

            return null;
        }

        private DateTime? ParseUpdatedDate(string value)
        {
            if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var updated))
            {
                return updated;
            }
            return null;
        }
    }
}