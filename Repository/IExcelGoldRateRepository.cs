using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoldPriceDashboard.Models;

namespace GoldPriceDashboard.Repository
{
    public interface IExcelGoldRateRepository
    {
        Task InitializeAsync();
        Task AppendRatesAsync(List<GoldRate> rates);
        Task<List<GoldRate>> GetHistoricalRatesAsync(DateTime? fromDate = null, DateTime? toDate = null, int? purity = null, string source = null);
        Task<GoldRate> GetLatestRateAsync(string source, int purity);
        Task<bool> IsDuplicateAsync(string source, int purity, decimal rate, DateTime date, DateTime lastUpdated);
    }
}
