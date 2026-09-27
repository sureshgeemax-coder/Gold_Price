using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoldPriceDashboard.Models;

namespace GoldPriceDashboard.Services
{
    public interface IGoldRateService
    {
        Task<GoldDashboardViewModel> RefreshDashboardAsync();
        Task<List<GoldRate>> GetHistoryAsync(string filter, int? purity = null, string source = null);
    }
}
