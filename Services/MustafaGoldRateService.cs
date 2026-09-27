using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GoldPriceDashboard.Models;
using Serilog;

namespace GoldPriceDashboard.Services
{
    public class MustafaGoldRateService : IMustafaGoldRateService
    {
        private readonly ILogger _logger;
        private const string URL_KEY = "MustafaGoldUrl";
        private const int TIMEOUT_SECONDS = 30;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) };

        public MustafaGoldRateService()
        {
            _logger = Log.ForContext<MustafaGoldRateService>();
        }

        public async Task<GoldRateResult> GetGoldRatesAsync()
        {
            var result = new GoldRateResult { Source = "Mustafa", Success = false };
            try
            {
                var url = System.Configuration.ConfigurationManager.AppSettings[URL_KEY] ?? "https://mustafajewellery.com/";
                _logger.Information("Fetching Mustafa gold rates from {Url}", url);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.Headers.Add("Accept-Language", "en-US,en;q=0.5");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();

                var rate916 = ExtractRate(html, @"22k[-\s]?916\s*(?:Jewellery)?", @"(\d{2,3}\.\d{2})");
                var rate999 = ExtractRate(html, @"24k[-\s]?999\s*(?:Jewellery)?", @"(\d{2,3}\.\d{2})");
                var lastUpdated = ExtractLastUpdated(html);

                if (rate916.HasValue && rate999.HasValue)
                {
                    result.Rate916 = rate916.Value;
                    result.Rate999 = rate999.Value;
                    result.LastUpdated = lastUpdated ?? DateTime.Now;
                    result.Currency = "SGD";
                    result.SourceUrl = url;
                    result.Success = true;
                    _logger.Information("Mustafa 916 = {R916}, 999 = {R999}", rate916.Value, rate999.Value);
                }
                else
                {
                    result.ErrorMessage = "Unable to parse gold rates from Mustafa page.";
                    _logger.Warning("Failed to parse Mustafa rates. 916={R916}, 999={R999}", rate916, rate999);
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Failed to retrieve Mustafa rates: {ex.Message}";
                _logger.Error(ex, "Error fetching Mustafa gold rates");
            }
            return result;
        }

        private decimal? ExtractRate(string html, string pattern, string valuePattern)
        {
            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var match = regex.Match(html);
                if (match.Success)
                {
                    var startIdx = Math.Min(match.Index + match.Length + 1, html.Length - 1);
                    var context = html.Substring(startIdx, Math.Min(200, html.Length - startIdx));
                    var valueRegex = new Regex(valuePattern);
                    var valueMatch = valueRegex.Match(context);
                    if (valueMatch.Success && decimal.TryParse(valueMatch.Groups[1].Value, out var rate))
                    {
                        if (rate > 0 && rate < 10000) return rate;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error extracting rate with pattern {Pattern}", pattern);
            }
            return null;
        }

        private DateTime? ExtractLastUpdated(string html)
        {
            try
            {
                var pattern = @"Last Updated on[:\s]*(\d{2}-\d{2}-\d{4}\s+\d{2}:\d{2}:\d{2}\s*(?:AM|PM)?)\s*(?:\(SGT\))?";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var match = regex.Match(html);
                if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var dt))
                {
                    return dt;
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
