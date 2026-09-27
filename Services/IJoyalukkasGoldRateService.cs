using System.Threading.Tasks;
using GoldPriceDashboard.Models;

namespace GoldPriceDashboard.Services
{
    public interface IJoyalukkasGoldRateService
    {
        Task<GoldRateResult> GetGoldRatesAsync();
    }
}