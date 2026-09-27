using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GoldPriceDashboard.Models;
using Serilog;

namespace GoldPriceDashboard.Services
{
    public class GRTGoldRateService : IGRTGoldRateService
    {
        private const string URL_KEY = "GRTGoldUrl";
        private const int TIMEOUT_SECONDS = 30;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) };
        private readonly ILogger _logger;

        public GRTGoldRateService()
        {
            _logger = Log.ForContext<GRTGoldRateService>();
        }

        public async Task<GoldRateResult> GetGoldRatesAsync()
        {
            var result = new GoldRateResult { Source = "GRT Jewellers", Success = false };
            try
            {
                var url = System.Configuration.ConfigurationManager.AppSettings[URL_KEY] ?? "https://www.grtjewels.com/asia/";
                _logger.Information("Fetching GRT gold rates from {Url}", url);

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var html = await response.Content.ReadAsStringAsync();
                    var text = WebUtility.HtmlDecode(Regex.Replace(html, @"<[^>]+>", " "));

                    var rate916 = ExtractRate(text, @"GOLD\s*-\s*22\s*KT\s*-\s*1\s*\.?\s*g\s*-\s*SGD\s*\$?\s*([\d,]+(?:\.\d{1,2})?)");
                    var rate999 = ExtractRate(text, @"GOLD\s*-\s*24\s*KT\s*-\s*1\s*\.?\s*g\s*-\s*SGD\s*\$?\s*([\d,]+(?:\.\d{1,2})?)");

                    if (rate916.HasValue && rate999.HasValue)
                    {
                        result.Rate916 = rate916.Value;
                        result.Rate999 = rate999.Value;
                        result.LastUpdated = DateTime.Now;
                        result.Currency = "SGD";
                        result.SourceUrl = url;
                        result.Success = true;
                    }
                    else
                    {
                        result.ErrorMessage = "Unable to parse 22KT and 24KT rates from the GRT Today's Rate menu.";
                        _logger.Warning("Failed to parse GRT rates. 22KT={R916}, 24KT={R999}", rate916, rate999);
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Failed to retrieve GRT rates: {ex.Message}";
                _logger.Error(ex, "Error fetching GRT gold rates");
            }

            return result;
        }

        private decimal? ExtractRate(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) && rate > 0 && rate < 10000)
            {
                return rate;
            }

            return null;
        }
    }
}