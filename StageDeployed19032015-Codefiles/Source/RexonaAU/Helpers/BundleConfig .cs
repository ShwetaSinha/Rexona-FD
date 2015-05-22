using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;

namespace RexonaAU.Helpers
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            BundleTable.Bundles.Add(new Bundle("~/bundles/css").Include(
                 "~/css/app.css",
                   "~/css/style.css",
                     "~/css/default.css",
                       "~/css/default.date.css",
                       "~/css/font-awesome.min.css"

    
                            ));


            BundleTable.Bundles.Add(new Bundle("~/bundles/js").Include(

                 "~/scripts/modernizr.js",
                   "~/scripts/jquery.js",
                     "~/scripts/foundation.min.js",
                       "~/scripts/packery.js",
                         "~/scripts/tick.js",
                           "~/scripts/picker.js",
                             "~/scripts/picker.date.js",
                               "~/scripts/app.js"
                              ));
        }


    }
}