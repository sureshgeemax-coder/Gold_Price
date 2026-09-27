using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Threading.Tasks;
using GoldPriceDashboard.Models;
using GoldPriceDashboard.Repository;
using GoldPriceDashboard.Services;
using Serilog;
using System.Web.Mvc;

namespace GoldPriceDashboard.Controllers
{
    public class GoldRateApiController : ApiController
    {
        private static IGoldRateService _goldRateService;
        private static IExcelGoldRateRepository _excelRepository;
        private static readonly object _lock = new object();
        private readonly ILogger _logger;

        static GoldRateApiController()
        {
            var mustafa = new MustafaGoldRateService();
            var malabar = new MalabarGoldRateService();
            var grt = new GRTGoldRateService();
            var joyalukkas = new JoyalukkasGoldRateService();
            var excel = new ExcelGoldRateRepository();
            excel.InitializeAsync().Wait();
            _goldRateService = new GoldRateService(mustafa, malabar, grt, joyalukkas, excel);
            _excelRepository = excel;
        }

        public GoldRateApiController()
        {
            _logger = Log.ForContext<GoldRateApiController>();
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/goldrates")]
        public async Task<IHttpActionResult> GetGoldRates()
        {
            try
            {
                var vm = await _goldRateService.RefreshDashboardAsync();
                var response = new
                {
                    lastUpdated = vm.LastRefresh,
                    mustafa = new { rate916 = vm.MustafaRate916, rate999 = vm.MustafaRate999 },
                    malabar = new { rate916 = vm.MalabarRate916, rate999 = vm.MalabarRate999 },
                    grt = new { rate916 = vm.GRTGoldRate916, rate999 = vm.GRTGoldRate999 },
                    joyalukkas = new { rate916 = vm.JoyalukkasRate916, rate999 = vm.JoyalukkasRate999 }
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "API error fetching gold rates");
                return InternalServerError(ex);
            }
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/goldrates/history")]
        public async Task<IHttpActionResult> GetHistory(string fromDate = null, string toDate = null, int? purity = null, string source = null, string filter = null)
        {
            try
            {
                DateTime? from = null;
                DateTime? to = null;
                if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var fd)) from = fd;
                if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var td)) to = td;

                var history = await _excelRepository.GetHistoricalRatesAsync(from, to, purity, source);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "API error fetching history");
                return InternalServerError(ex);
            }
        }
    }
}
