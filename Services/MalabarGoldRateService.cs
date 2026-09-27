using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GoldPriceDashboard.Models;
using Serilog;

namespace GoldPriceDashboard.Services
{
    public class MalabarGoldRateService : IMalabarGoldRateService
    {
        private readonly ILogger _logger;
        private const string URL_KEY = "MalabarGoldUrl";
        private const int TIMEOUT_SECONDS = 30;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) };

        public MalabarGoldRateService()
        {
            _logger = Log.ForContext<MalabarGoldRateService>();
        }

        public async Task<GoldRateResult> GetGoldRatesAsync()
        {
            var result = new GoldRateResult { Source = "Malabar", Success = false };
            try
            {
                var url = System.Configuration.ConfigurationManager.AppSettings[URL_KEY] ?? "https://www.malabargoldanddiamonds.com/ae/stores/singapore";
                _logger.Information("Fetching Malabar gold rates from {Url}", url);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                request.Headers.Add("Accept-Language", "en-US,en;q=0.5");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();

                var rate916 = ExtractSingaporeRate(html, "price22kt_85");
                var rate999 = ExtractSingaporeRate(html, "price24kt_85");
                var lastUpdated = ExtractLastUpdated(html);

                if (rate916.HasValue && rate999.HasValue)
                {
                    result.Rate916 = rate916.Value;
                    result.Rate999 = rate999.Value;
                    result.LastUpdated = lastUpdated ?? DateTime.Now;
                    result.Currency = "SGD";
                    result.SourceUrl = url;
                    result.Success = true;
                    _logger.Information("Malabar 916 = {R916}, 999 = {R999}", rate916.Value, rate999.Value);
                }
                else
                {
                    result.ErrorMessage = "Unable to parse Singapore gold rates from Malabar page.";
                    _logger.Warning("Failed to parse Malabar Singapore rates. 916={R916}, 999={R999}", rate916, rate999);
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Failed to retrieve Malabar rates: {ex.Message}";
                _logger.Error(ex, "Error fetching Malabar gold rates");
            }
            return result;
        }

        private decimal? ExtractSingaporeRate(string html, string cellId)
        {
            try
            {
                var pattern = @"<td\b(?=[^>]*\bid\s*=\s*[\""']" + Regex.Escape(cellId) + @"[\""'])[^>]*>(.*?)</td>";
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (!match.Success) return null;

                var valueMatch = Regex.Match(match.Groups[1].Value, @"([\d,]+(?:\.\d+)?)");
                if (valueMatch.Success && decimal.TryParse(valueMatch.Groups[1].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) && rate > 0 && rate < 10000)
                {
                    return rate;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error extracting Singapore rate from cell {CellId}", cellId);
            }
            return null;
        }

        private DateTime? ExtractLastUpdated(string html)
        {
            try
            {
                var pattern = @"<[^>]*\bid\s*=\s*[\""']updatedtime_85[\""'][^>]*>(.*?)</[^>]+>";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var match = regex.Match(html);
                if (match.Success)
                {
                    var text = Regex.Replace(match.Groups[1].Value, @"<[^>]+>", "").Trim();
                    var formats = new[] { "dd/MM/yyyy h:mm tt", "dd/MM/yyyy hh:mm tt" };
                    if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                    {
                        return dt;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error extracting last updated timestamp");
            }
            return null;
        }
    }
}
