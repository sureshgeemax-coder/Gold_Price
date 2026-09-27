using System;
using System.Threading.Tasks;
using GoldPriceDashboard.Models;

namespace GoldPriceDashboard.Services
{
    public interface IMalabarGoldRateService
    {
        Task<GoldRateResult> GetGoldRatesAsync();
    }
}
