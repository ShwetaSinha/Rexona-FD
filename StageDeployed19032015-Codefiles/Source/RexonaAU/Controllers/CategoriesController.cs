using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using umbraco.cms.businesslogic.Tags;
using umbraco.MacroEngines;
using umbraco.presentation.nodeFactory;
//using umbraco.MacroEngines;
//using umbraco.presentation.nodeFactory;
using Tags = umbraco.editorControls.tags;

namespace RexonaAU.Controllers
{
    public class CategoriesController : Umbraco.Web.Mvc.SurfaceController
    {  
        //Get url when Page ID is passed
        [HttpPost]       
        public ActionResult GetPageUrl(int PageId)
        {
            string PageUrl = Umbraco.NiceUrl(PageId);
            return Json(PageUrl, JsonRequestBehavior.AllowGet);
        }
  }

   

}
