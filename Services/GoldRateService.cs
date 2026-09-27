using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoldPriceDashboard.Models;
using GoldPriceDashboard.Repository;
using Serilog;

namespace GoldPriceDashboard.Services
{
    public class GoldRateService : IGoldRateService
    {
        private readonly IMustafaGoldRateService _mustafaService;
        private readonly IMalabarGoldRateService _malabarService;
        private readonly IGRTGoldRateService _grtService;
        private readonly IJoyalukkasGoldRateService _joyalukkasService;
        private readonly IExcelGoldRateRepository _excelRepository;
        private readonly ILogger _logger;

        public GoldRateService(IMustafaGoldRateService mustafaService, IMalabarGoldRateService malabarService, IGRTGoldRateService grtService, IJoyalukkasGoldRateService joyalukkasService, IExcelGoldRateRepository excelRepository)
        {
            _mustafaService = mustafaService;
            _malabarService = malabarService;
            _grtService = grtService;
            _joyalukkasService = joyalukkasService;
            _excelRepository = excelRepository;
            _logger = Log.ForContext<GoldRateService>();
        }

        public async Task<GoldDashboardViewModel> RefreshDashboardAsync()
        {
            var vm = new GoldDashboardViewModel();
            _logger.Information("Gold rate refresh started");

            var results = await Task.WhenAll(
                _mustafaService.GetGoldRatesAsync(),
                _malabarService.GetGoldRatesAsync(),
                _grtService.GetGoldRatesAsync(),
                _joyalukkasService.GetGoldRatesAsync());
            var mustafaResult = results[0];
            var malabarResult = results[1];
            var grtResult = results[2];
            var joyalukkasResult = results[3];

            await ApplyLastAvailableRatesAsync(mustafaResult);
            await ApplyLastAvailableRatesAsync(malabarResult);
            await ApplyLastAvailableRatesAsync(grtResult);
            await ApplyLastAvailableRatesAsync(joyalukkasResult);

            vm.MustafaRate916 = mustafaResult.Rate916;
            vm.MustafaRate999 = mustafaResult.Rate999;
            vm.MustafaUpdated = mustafaResult.LastUpdated;
            vm.MustafaStatus = GetSourceStatus(mustafaResult);
            vm.MustafaErrorMessage = mustafaResult.ErrorMessage;

            vm.MalabarRate916 = malabarResult.Rate916;
            vm.MalabarRate999 = malabarResult.Rate999;
            vm.MalabarUpdated = malabarResult.LastUpdated;
            vm.MalabarStatus = GetSourceStatus(malabarResult);
            vm.MalabarErrorMessage = malabarResult.ErrorMessage;

            vm.GRTGoldRate916 = grtResult.Rate916;
            vm.GRTGoldRate999 = grtResult.Rate999;
            vm.GRTGoldUpdated = grtResult.LastUpdated;
            vm.GRTGoldStatus = GetSourceStatus(grtResult);
            vm.GRTGoldErrorMessage = grtResult.ErrorMessage;

            vm.JoyalukkasRate916 = joyalukkasResult.Rate916;
            vm.JoyalukkasRate999 = joyalukkasResult.Rate999;
            vm.JoyalukkasUpdated = joyalukkasResult.LastUpdated;
            vm.JoyalukkasStatus = GetSourceStatus(joyalukkasResult);
            vm.JoyalukkasErrorMessage = joyalukkasResult.ErrorMessage;

            if (mustafaResult.Success && mustafaResult.Rate916.HasValue && mustafaResult.Rate999.HasValue)
            {
                var mustafaRates = new List<GoldRate>();
                mustafaRates.Add(CreateGoldRate(mustafaResult, 916, mustafaResult.Rate916.Value));
                mustafaRates.Add(CreateGoldRate(mustafaResult, 999, mustafaResult.Rate999.Value));
                foreach (var r in mustafaRates)
                {
                    if (!await _excelRepository.IsDuplicateAsync(r.Source, r.Purity, r.RatePerGram, r.Date, r.LastUpdated))
                    {
                        await _excelRepository.AppendRatesAsync(new List<GoldRate> { r });
                    }
                }
            }

            if (malabarResult.Success && malabarResult.Rate916.HasValue && malabarResult.Rate999.HasValue)
            {
                var malabarRates = new List<GoldRate>();
                malabarRates.Add(CreateGoldRate(malabarResult, 916, malabarResult.Rate916.Value));
                malabarRates.Add(CreateGoldRate(malabarResult, 999, malabarResult.Rate999.Value));
                foreach (var r in malabarRates)
                {
                    if (!await _excelRepository.IsDuplicateAsync(r.Source, r.Purity, r.RatePerGram, r.Date, r.LastUpdated))
                    {
                        await _excelRepository.AppendRatesAsync(new List<GoldRate> { r });
                    }
                }
            }

            if (grtResult.Success && grtResult.Rate916.HasValue && grtResult.Rate999.HasValue)
            {
                var grtRates = new List<GoldRate>
                {
                    CreateGoldRate(grtResult, 916, grtResult.Rate916.Value),
                    CreateGoldRate(grtResult, 999, grtResult.Rate999.Value)
                };
                foreach (var rate in grtRates)
                {
                    if (!await _excelRepository.IsDuplicateAsync(rate.Source, rate.Purity, rate.RatePerGram, rate.Date, rate.LastUpdated))
                    {
                        await _excelRepository.AppendRatesAsync(new List<GoldRate> { rate });
                    }
                }
            }

            if (joyalukkasResult.Success && joyalukkasResult.Rate916.HasValue && joyalukkasResult.Rate999.HasValue)
            {
                var joyalukkasRates = new List<GoldRate>
                {
                    CreateGoldRate(joyalukkasResult, 916, joyalukkasResult.Rate916.Value),
                    CreateGoldRate(joyalukkasResult, 999, joyalukkasResult.Rate999.Value)
                };
                foreach (var rate in joyalukkasRates)
                {
                    if (!await _excelRepository.IsDuplicateAsync(rate.Source, rate.Purity, rate.RatePerGram, rate.Date, rate.LastUpdated))
                    {
                        await _excelRepository.AppendRatesAsync(new List<GoldRate> { rate });
                    }
                }
            }

            var rates916 = new[]
            {
                new KeyValuePair<string, decimal?>("Mustafa", vm.MustafaRate916),
                new KeyValuePair<string, decimal?>("Malabar", vm.MalabarRate916),
                new KeyValuePair<string, decimal?>("GRT Jewellers", vm.GRTGoldRate916),
                new KeyValuePair<string, decimal?>("Joyalukkas", vm.JoyalukkasRate916)
            }.Where(rate => rate.Value.HasValue).OrderBy(rate => rate.Value.Value).ToList();
            if (rates916.Count > 1)
            {
                vm.Difference916 = rates916[rates916.Count - 1].Value.Value - rates916[0].Value.Value;
                vm.PercentageDifference916 = rates916[0].Value.Value == 0 ? (decimal?)null : vm.Difference916.Value / rates916[0].Value.Value * 100;
                vm.HigherRate916 = rates916[rates916.Count - 1].Key;
                vm.LowerRate916 = rates916[0].Key;
            }

            var rates999 = new[]
            {
                new KeyValuePair<string, decimal?>("Mustafa", vm.MustafaRate999),
                new KeyValuePair<string, decimal?>("Malabar", vm.MalabarRate999),
                new KeyValuePair<string, decimal?>("GRT Jewellers", vm.GRTGoldRate999),
                new KeyValuePair<string, decimal?>("Joyalukkas", vm.JoyalukkasRate999)
            }.Where(rate => rate.Value.HasValue).OrderBy(rate => rate.Value.Value).ToList();
            if (rates999.Count > 1)
            {
                vm.Difference999 = rates999[rates999.Count - 1].Value.Value - rates999[0].Value.Value;
                vm.PercentageDifference999 = rates999[0].Value.Value == 0 ? (decimal?)null : vm.Difference999.Value / rates999[0].Value.Value * 100;
                vm.HigherRate999 = rates999[rates999.Count - 1].Key;
                vm.LowerRate999 = rates999[0].Key;
            }

            if (mustafaResult.Success)
            {
                vm.MustafaPrevious916 = (await GetPriorRateAsync("Mustafa", 916, mustafaResult.LastUpdated))?.RatePerGram;
                vm.MustafaPrevious999 = (await GetPriorRateAsync("Mustafa", 999, mustafaResult.LastUpdated))?.RatePerGram;
                vm.MustafaMovement916 = CalculateMovement(vm.MustafaRate916, vm.MustafaPrevious916);
                vm.MustafaMovement999 = CalculateMovement(vm.MustafaRate999, vm.MustafaPrevious999);
            }
            if (malabarResult.Success)
            {
                vm.MalabarPrevious916 = (await GetPriorRateAsync("Malabar", 916, malabarResult.LastUpdated))?.RatePerGram;
                vm.MalabarPrevious999 = (await GetPriorRateAsync("Malabar", 999, malabarResult.LastUpdated))?.RatePerGram;
                vm.MalabarMovement916 = CalculateMovement(vm.MalabarRate916, vm.MalabarPrevious916);
                vm.MalabarMovement999 = CalculateMovement(vm.MalabarRate999, vm.MalabarPrevious999);
            }
            if (grtResult.Success)
            {
                vm.GRTGoldMovement916 = CalculateMovement(vm.GRTGoldRate916, (await GetPriorRateAsync("GRT Jewellers", 916, grtResult.LastUpdated))?.RatePerGram);
                vm.GRTGoldMovement999 = CalculateMovement(vm.GRTGoldRate999, (await GetPriorRateAsync("GRT Jewellers", 999, grtResult.LastUpdated))?.RatePerGram);
            }
            if (joyalukkasResult.Success)
            {
                vm.JoyalukkasMovement916 = CalculateMovement(vm.JoyalukkasRate916, (await GetPriorRateAsync("Joyalukkas", 916, joyalukkasResult.LastUpdated))?.RatePerGram);
                vm.JoyalukkasMovement999 = CalculateMovement(vm.JoyalukkasRate999, (await GetPriorRateAsync("Joyalukkas", 999, joyalukkasResult.LastUpdated))?.RatePerGram);
            }

            var history = await _excelRepository.GetHistoricalRatesAsync();
            vm.HistoricalRates = history.OrderByDescending(r => r.LastUpdated).Take(100).ToList();
            vm.LastRefresh = DateTime.Now;

            _logger.Information("Refresh completed. Mustafa 916={M916}, 999={M999}, Malabar 916={Ma916}, 999={Ma999}, GRT 916={G916}, 999={G999}, Joyalukkas 916={J916}, 999={J999}",
                vm.MustafaRate916, vm.MustafaRate999, vm.MalabarRate916, vm.MalabarRate999, vm.GRTGoldRate916, vm.GRTGoldRate999, vm.JoyalukkasRate916, vm.JoyalukkasRate999);
            return vm;
        }

        private GoldRate CreateGoldRate(GoldRateResult result, int purity, decimal rate)
        {
            return new GoldRate
            {
                Date = DateTime.Now.Date,
                Time = DateTime.Now.TimeOfDay,
                Source = result.Source,
                GoldType = "Jewellery",
                Purity = purity,
                RatePerGram = rate,
                Currency = result.Currency ?? "SGD",
                SourceUrl = result.SourceUrl,
                LastUpdated = result.LastUpdated ?? DateTime.Now,
                Status = result.Success ? "Success" : "Failed",
                ErrorMessage = result.ErrorMessage
            };
        }

        private async Task ApplyLastAvailableRatesAsync(GoldRateResult result)
        {
            if (result.Success) return;

            var latest = await Task.WhenAll(
                _excelRepository.GetLatestRateAsync(result.Source, 916),
                _excelRepository.GetLatestRateAsync(result.Source, 999));
            if (latest[0] != null)
            {
                result.Rate916 = latest[0].RatePerGram;
                result.SourceUrl = latest[0].SourceUrl;
            }
            if (latest[1] != null)
            {
                result.Rate999 = latest[1].RatePerGram;
                result.SourceUrl = result.SourceUrl ?? latest[1].SourceUrl;
            }
            var timestamps = latest.Where(rate => rate != null).Select(rate => rate.LastUpdated).ToList();
            if (timestamps.Count > 0) result.LastUpdated = timestamps.Max();
        }

        private string GetSourceStatus(GoldRateResult result)
        {
            if (result.Success) return "Available";
            return result.Rate916.HasValue || result.Rate999.HasValue ? "Last available rate" : "Failed to retrieve";
        }

        private string CalculateMovement(decimal? current, decimal? previous)
        {
            if (!current.HasValue || !previous.HasValue) return null;
            var diff = current.Value - previous.Value;
            if (diff == 0) return "→ No Change";
            var pct = previous.Value == 0 ? 0 : (diff / previous.Value) * 100;
            var sign = diff > 0 ? "+" : "";
            return diff > 0 ? $"▲ S${diff:F2} ({sign}{pct:F2}%)" : $"▼ S${Math.Abs(diff):F2} ({sign}{pct:F2}%)";
        }

        private async Task<GoldRate> GetPriorRateAsync(string source, int purity, DateTime? currentUpdated)
        {
            var history = await _excelRepository.GetHistoricalRatesAsync(source: source);
            return history
                .Where(rate => rate.Purity == purity && rate.Status == "Success" && (!currentUpdated.HasValue || rate.LastUpdated < currentUpdated.Value))
                .OrderByDescending(rate => rate.LastUpdated)
                .FirstOrDefault();
        }

        public async Task<List<GoldRate>> GetHistoryAsync(string filter, int? purity = null, string source = null)
        {
            DateTime? fromDate = null;
            DateTime? toDate = DateTime.Now;
            switch (filter)
            {
                case "today":
                    fromDate = DateTime.Now.Date;
                    break;
                case "7":
                    fromDate = DateTime.Now.AddDays(-7);
                    break;
                case "30":
                    fromDate = DateTime.Now.AddDays(-30);
                    break;
                case "90":
                    fromDate = DateTime.Now.AddDays(-90);
                    break;
                case "180":
                    fromDate = DateTime.Now.AddDays(-180);
                    break;
                case "365":
                    fromDate = DateTime.Now.AddDays(-365);
                    break;
            }
            return await _excelRepository.GetHistoricalRatesAsync(fromDate, toDate, purity, source);
        }
    }
}
