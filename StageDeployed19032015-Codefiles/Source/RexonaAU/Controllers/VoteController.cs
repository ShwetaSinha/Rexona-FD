using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic.media;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.propertytype;
using umbraco.NodeFactory;
using Umbraco;
using Umbraco.Core.Logging;
using umbraco;
using Umbraco.Web.Mvc;
using Umbraco.Core.Services;
using RexonaAU.Helpers;

namespace RexonaAU.Controllers
{
    public class VoteController : SurfaceController
    {
        //
        // GET: /Vote/

        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public JsonResult CountVote(bool vote, int entryId, string fieldName)
        {
            int currentLikes = 0;
            string message = string.Empty;
            try
            {
                //increment
                currentLikes = manageVoteCount(vote, entryId, fieldName);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU : CountVote() failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }

            if (currentLikes == -1)
            {
                message = "Error";
            }
            else
            {
                message = "Success";
            }
            var result = new { message = message, like = currentLikes };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public int manageVoteCount(bool vote, int entryId, string fieldName)
        {
            var currentLikes = 0;
            try
            {
                Node node = new Node(entryId);
                var service = new ContentService();
                var entry = service.GetById(entryId);

                int.TryParse(Convert.ToString(entry.GetValue(fieldName)), out currentLikes);

                if (vote)
                {
                    //entry.SetValue("entryLikes", currentLikes + 1);
                    entry.SetValue(fieldName, currentLikes + 1);
                }
                else
                {
                    //entry.SetValue("entryLikes", currentLikes - 1);
                    if (currentLikes > 0)
                    {
                        entry.SetValue(fieldName, currentLikes - 1);
                    }
                }

                service.SaveAndPublish(entry);
                umbraco.library.UpdateDocumentCache(node.Parent.Id);
                umbraco.library.UpdateDocumentCache(entry.Id);
                umbraco.library.RefreshContent();
                int.TryParse(entry.GetValue(fieldName).ToString(), out currentLikes);

                //Clear cache for Home page stories after every vote/unvote
                //int entriesNodeId = 0;
                //if (!int.TryParse(System.Configuration.ConfigurationManager.AppSettings["EntriesNodeId"], out entriesNodeId))
                //    entriesNodeId = 1115;
                //string strCacheKey = String.Format("RexonaAULike_GetChildrenNodesOfEntriesByID_{0}", entriesNodeId);
                //System.Web.HttpContext.Current.Cache.Remove(strCacheKey);

                //clear all pages cache 
                Common.ClearApplicationCache();

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU : manageVoteCount() failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return -1;
            }
            return currentLikes;
        }

    }
}
