using Exchange.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Exchange
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static BackgroundJob getPrice;
        private static BackgroundJob checkHold;
        protected void Application_Start()
        {
            System.Web.Optimization.BundleTable.EnableOptimizations = false;
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            getPrice = new BackgroundJob(RealtimeActions.getCurrentPrice, 5);
            checkHold = new BackgroundJob(RealtimeActions.checkHoldBalance, 5);
        }
    }
}
