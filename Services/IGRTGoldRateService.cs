using System.Threading.Tasks;
using GoldPriceDashboard.Models;

namespace GoldPriceDashboard.Services
{
    public interface IGRTGoldRateService
    {
        Task<GoldRateResult> GetGoldRatesAsync();
    }
}