using System;
using System.Threading.Tasks;
using GoldPriceDashboard.Models;

namespace GoldPriceDashboard.Services
{
    public interface IMustafaGoldRateService
    {
        Task<GoldRateResult> GetGoldRatesAsync();
    }
}
