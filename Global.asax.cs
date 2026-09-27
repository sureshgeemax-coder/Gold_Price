using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using GoldPriceDashboard.Repository;
using GoldPriceDashboard.Services;
using Serilog;

namespace GoldPriceDashboard
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Server.MapPath("~/Logs/goldrate-.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Application starting");

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            DependencyResolver.SetResolver(new SimpleDependencyResolver());
        }

        protected void Application_End()
        {
            Log.Information("Application ending");
            Log.CloseAndFlush();
        }
    }

    public class SimpleDependencyResolver : IDependencyResolver
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IGoldRateService))
            {
                var mustafa = new MustafaGoldRateService();
                var malabar = new MalabarGoldRateService();
                var grt = new GRTGoldRateService();
                var joyalukkas = new JoyalukkasGoldRateService();
                var excel = new ExcelGoldRateRepository();
                excel.InitializeAsync().Wait();
                return new GoldRateService(mustafa, malabar, grt, joyalukkas, excel);
            }
            if (serviceType == typeof(Repository.IExcelGoldRateRepository))
            {
                var repo = new ExcelGoldRateRepository();
                repo.InitializeAsync().Wait();
                return repo;
            }
            if (serviceType == typeof(IMustafaGoldRateService)) return new MustafaGoldRateService();
            if (serviceType == typeof(IMalabarGoldRateService)) return new MalabarGoldRateService();
            if (serviceType == typeof(IGRTGoldRateService)) return new GRTGoldRateService();
            if (serviceType == typeof(IJoyalukkasGoldRateService)) return new JoyalukkasGoldRateService();
            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            var service = GetService(serviceType);
            if (service != null) yield return service;
        }

    }
}
