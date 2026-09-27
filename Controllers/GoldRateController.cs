using System;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Configuration;
using GoldPriceDashboard.Models;
using GoldPriceDashboard.Services;
using Serilog;

namespace GoldPriceDashboard.Controllers
{
    public class GoldRateController : Controller
    {
        private readonly IGoldRateService _goldRateService;
        private readonly ILogger _logger;

        public GoldRateController()
        {
            _goldRateService = DependencyResolver.Current.GetService<IGoldRateService>();
            _logger = Log.ForContext<GoldRateController>();
        }

        public async Task<ActionResult> Index()
        {
            try
            {
                var vm = await _goldRateService.RefreshDashboardAsync();
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading dashboard");
                var vm = new GoldDashboardViewModel
                {
                    MustafaStatus = "Error",
                    MustafaErrorMessage = ex.Message,
                    MalabarStatus = "Error",
                    MalabarErrorMessage = ex.Message,
                    GRTGoldStatus = "Error",
                    GRTGoldErrorMessage = ex.Message,
                    JoyalukkasStatus = "Error",
                    JoyalukkasErrorMessage = ex.Message
                };
                return View(vm);
            }
        }

        public ActionResult About()
        {
            return View();
        }

        public async Task<ActionResult> Shop(string id)
        {
            var shops = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "malabar", "Malabar" },
                { "mustafa", "Mustafa" },
                { "grt", "GRT Jewellers" },
                { "joyalukkas", "Joyalukkas" }
            };
            if (string.IsNullOrWhiteSpace(id) || !shops.TryGetValue(id, out var source)) return HttpNotFound();

            try
            {
                var vm = await _goldRateService.RefreshDashboardAsync();
                ViewBag.ShopKey = id.ToLowerInvariant();
                ViewBag.ShopName = source;
                ViewBag.ShopHistoryUrl = Url.Action("GetHistory", "GoldRate", new { source = source });
                ViewBag.TimestampLabel = id == "grt" ? "Fetched; source does not publish an update time" : "Last updated by source";
                var sourceKey = id == "grt" ? "GRTGoldUrl" : id == "joyalukkas" ? "JoyalukkasGoldUrl" : source + "GoldUrl";
                ViewBag.SourceUrl = id == "joyalukkas" ? "https://www.joyalukkas.com/sg/goldrate" : ConfigurationManager.AppSettings[sourceKey];
                return View("Shop", vm);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading {Shop} details", source);
                return HttpNotFound();
            }
        }

        [HttpPost]
        public async Task<ActionResult> Refresh()
        {
            try
            {
                var vm = await _goldRateService.RefreshDashboardAsync();
                return Json(new { success = true, data = vm });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during manual refresh");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetHistory(string filter = "7", string source = null)
        {
            try
            {
            var history = await _goldRateService.GetHistoryAsync(filter, source: source);
                return Json(history, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching history");
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
