using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
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
using Umbraco.Web;
using RexonaAU.Models;
using System.Text;

namespace RexonaAU.Controllers
{
    public class AmbassadorController : Umbraco.Web.Mvc.SurfaceController
    {
        //
        // GET: /Ambassador/

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetArticles(int PageSize, int currentPageIndex, string sortingText, string ArticleType)
        {
            List<ArticleEntry> lstStories = GetAllArticles(1);
            try
            {
                if (lstStories != null && lstStories.Count > 0)
                {
                    lstStories = lstStories.Where(article => ArticleType.Equals(article.Type, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (sortingText.Equals("MOST POPULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        lstStories = lstStories.OrderByDescending(a => a.Hearts).ToList<ArticleEntry>();
                    }
                    else
                    {
                        lstStories = lstStories.OrderByDescending(a => a.UploadDateAsDateTime).ToList<ArticleEntry>();
                    }


                }
                var result = new
                {
                    articles = lstStories.Skip(currentPageIndex * PageSize).Take(PageSize),
                    totalPages = Math.Ceiling((decimal)(lstStories.Count / (decimal)PageSize))
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetArticles() method not executed successfully, something went wrong while sorting the results in GetArticles() method. : "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return base.Json("GetArticles, Please try again", "text/plain");
            }
        }

        [HttpGet]
        public JsonResult GetHomePageAmbassadorData()
        {
            List<AmbassadorModel> lstAmbassadors = GetAmbassadors(3);
            int ambassadorCount = 0;
            int.TryParse(ConfigurationManager.AppSettings["AmbassadorCount"], out ambassadorCount);
            try
            {
                lstAmbassadors = lstAmbassadors.OrderByDescending(a => a.createdDate).Take(ambassadorCount).ToList();
                return Json(lstAmbassadors, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetHomePageAmbassadorData() something went wrong while sorting the results in GetHomePageAmbassadorData() method."
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return base.Json("GetHomePageAmbassadorData failed, Please try again", "text/plain");
            }
        }

        [HttpGet]
        public JsonResult GetIndividualArticles(int PageSize, int currentPageIndex, string sortingText, string ArticleType, int AId)
        {
            List<ArticleEntry> lstStories = GetAllArticles(2);
            var ambassadorNOde = uQuery.GetNodesByType("Author").Where(a => a.Id == AId).FirstOrDefault();
            string ambassadorName = string.Empty;
            if (ambassadorNOde != null && ambassadorNOde.Id > 0)
            {
                ambassadorName = ambassadorNOde.GetProperty<string>("ambassadorName").Trim();
            }

            try
            {
                if (lstStories != null && lstStories.Count > 0)
                {
                    lstStories = lstStories.Where(article => ArticleType.Equals(article.Type, StringComparison.OrdinalIgnoreCase)).Where(x => ambassadorName.Equals(x.AmbassadorName.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

                    if (sortingText.Equals("MOST POPULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        lstStories = lstStories.OrderByDescending(a => a.Hearts).ToList<ArticleEntry>();
                    }
                    else
                    {
                        lstStories = lstStories.OrderByDescending(a => a.UploadDateAsDateTime).ToList<ArticleEntry>();
                    }


                }
                var result = new
                {
                    articles = lstStories.Skip(currentPageIndex * PageSize).Take(PageSize),
                    totalPages = Math.Ceiling((decimal)(lstStories.Count / (decimal)PageSize))
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetIndividualArticles() , something went wrong while sorting the results in GetIndividualArticles() method.: "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return base.Json("Upload Video failed, Please try again", "text/plain");
            }
        }

        [HttpGet]
        public JsonResult GetRecommendedArticles(int PageSize, string ArticleType, string Tag, int ArticleId)
        {
            List<ArticleEntry> lstStories = GetAllArticles(4);

            List<string> ArticleTags = new List<string>(Tag.Split(','));
            ArticleTags.Add(Tag);
            try
            {
                if (lstStories != null && lstStories.Count > 0)
                {
                    lstStories = lstStories.Where(a => ArticleTags.Intersect(a.Tags.Split(',').ToList()).Count() > 0).ToList<ArticleEntry>();



                    lstStories = lstStories.OrderByDescending(a => a.UploadDateAsDateTime).ThenByDescending(a => a.Hearts).ToList<ArticleEntry>();
                    lstStories.RemoveAll(a => a.Id == ArticleId);

                }
                var result = new
                {
                    articles = lstStories.Take(PageSize)
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetArticles() method not executed successfully, something went wrong while sorting the results in GetArticles() method. : "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return base.Json("GetArticles, Please try again", "text/plain");
            }
        }

        #region Dashboard: Recommended Articles
        [HttpGet]
        public JsonResult GetRecommendedArticlesForDashboard(int PageSize, int currentPageIndex)
        {
            Entities dbEntities = new Entities();
            int currentMemberId = Member.GetCurrentMember().Id;
            List<ArticleEntry> lstRecommenededArticles = new List<ArticleEntry>();

            //Step1: Get all articles
            //Step2: Get all tags for user pledges One pledge may contains two tags
            //Step3: Filter Step1 articles based Step2 'tags' and then sort descending on 'popularity' then sort by 'Recency'
            //Step4: Output must contain total 8 articles divided in all categories

            //All article id recommended for the pledges created/ joinded by user            
            List<int> lstArticles = new List<int>();

            //All tags collected from pledges joined/ created by user
            List<string> PledgeTags = new List<string>();

            //string builder is used to select all tags and then add to list
            StringBuilder strPledgeTags = new StringBuilder();
            try
            {
                #region Step1
                List<ArticleEntry> lstAllArticles = GetAllArticles(4);
                #endregion
                if (lstAllArticles != null && lstAllArticles.Count > 0)
                {
                    #region Step2
                    //Get all pledges of user and get the tags and recommended article id from the pledges
                    IEnumerable<umbraco.NodeFactory.Node> lstMemberPledges = uQuery.GetNodesByType("PledgeMember").ToList()
                        .Where(obj => obj.GetProperty<bool>("step3Clear") && obj.GetProperty<int>("memberId") == currentMemberId);

                    if (lstMemberPledges != null && lstMemberPledges.Count() > 0)
                    {
                        //Local variable only used to optimize database operations
                        List<int> pledgeIds = new List<int>();
                        foreach (Node pledge in lstMemberPledges)
                        {
                            pledgeIds.Add(pledge.Parent.Id);

                            //collect all tags for pledges joined/ created by user.
                            strPledgeTags.Append(pledge.Parent.GetProperty<string>("categoryTag").ToLower() + ",");
                        }
                    }
                    PledgeTags.AddRange(strPledgeTags.ToString().Split(','));

                    //remove all white spaces and empty strings
                    PledgeTags.RemoveAll(obj => string.IsNullOrWhiteSpace(obj));
                    #endregion


                    if (PledgeTags != null && PledgeTags.Count > 0)
                    {
                        PledgeTags = PledgeTags.Distinct().ToList();

                        #region Step3
                        lstAllArticles = lstAllArticles.Where(a =>
                            a.Tags.ToLower().Split(',').Intersect(PledgeTags).Any()).OrderByDescending(a => a.Hearts)
                            .ThenByDescending(a => a.UploadDateAsDateTime).ToList();
                        #endregion
                        //Do grouping and selection stuff only if more than 8 articles comes in result
                        if (lstAllArticles != null && lstAllArticles.Count() > 8)
                        {
                            #region Step4

                            //Create group from the list of all artilcles based on tags. This is needed to select the articles from 
                            //all mathing tags
                            var grpArticleGroups = from b in lstAllArticles
                                                   group b by b.Tags;

                            //Select total 8 articles as per requirement to show in UI
                            for (int articleIterator = 0; articleIterator < 8; articleIterator++)
                            {
                                //Articles must selected from each matching group of tags
                                //For each group
                                for (int grpIterator = 0; grpIterator < grpArticleGroups.Count(); grpIterator++)
                                {
                                    //If group within iteration contains article at articleIteration-location then select that article. If not
                                    //then go to next group
                                    if (grpArticleGroups.ElementAt(grpIterator).Count() > articleIterator)
                                    {
                                        //Article fonund in group at iteration location. Add to output list
                                        lstRecommenededArticles.Add(grpArticleGroups.ElementAt(grpIterator).ElementAt(articleIterator));
                                        if (lstRecommenededArticles.Count >= 8)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (lstRecommenededArticles.Count >= 8)
                                {
                                    break;
                                }
                            }
                            #endregion

                            #region Step5
                            //Sort output list based on Popularity and Recency 
                            lstRecommenededArticles = lstRecommenededArticles.OrderByDescending(a => a.Hearts)
                                .ThenByDescending(a => a.UploadDateAsDateTime).ToList();

                            //if list contains more than 8 element then remove extra objects
                            if (lstRecommenededArticles.Count > 8)
                            {
                                lstRecommenededArticles = lstRecommenededArticles.Take(8).ToList();
                            }
                            #endregion
                        }
                        else
                        {
                            //Add all articles to output
                            lstRecommenededArticles.AddRange(lstAllArticles);
                        }
                    }
                }

                var result = new
                {
                    articles = lstRecommenededArticles.Skip(currentPageIndex * PageSize).Take(PageSize),
                    totalPages = Math.Ceiling((decimal)(lstRecommenededArticles.Count / (decimal)PageSize))
                };

                return Json(result, JsonRequestBehavior.AllowGet);
                //return Json(lstRecommenededArticles, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetRecommendedArticlesForDashboard() method not executed successfully: "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return base.Json("GetArticles, Please try again", "text/plain");
            }
        }
        #endregion
        #region common method to get all articles

        private const string CachePrefix = "RexonaAUSArticles_{0}";
        [NonAction]
        private List<ArticleEntry> GetAllArticles(int key)
        {
            return RexonaAU.Helpers.Common.ArticlesHelper.GetAllArticles(key);
        }

        [HttpGet]
        public JsonResult GetAllArticles()
                {
            try
                    {
                List<ArticleEntry> entriesWithDate = GetAllArticles(147).Where(ar => ar.MailOutDate != DateTime.MinValue).OrderBy(ar => ar.MailOutDate).ToList();
                List<ArticleEntry> entriesWithoutDate = GetAllArticles(147).Where(ar => ar.MailOutDate == DateTime.MinValue).ToList();
                entriesWithDate.AddRange(entriesWithoutDate.ToArray());
                var results = entriesWithDate.Select(ar => new
                        {

                    Id = ar.Id,
                    ArticleTitle = ar.ArticleTitle,
                    ActualArticleURL = ar.ActualArticleURL,
                    IncludeInEDM = ar.IncludeInEDM,
                    MailOutDate = ar.MailOutDate == DateTime.MinValue ? string.Empty : ar.MailOutDate.ToShortDateString()
                });
                return Json(results, JsonRequestBehavior.AllowGet);
                }
            catch (Exception ex)
            {
                LogHelper.Error(this.GetType(), "GetAllArticles - AJAX failed", ex);
                return Json(string.Empty);
            }
        }

        [HttpGet]
        public JsonResult GetAISArticles(int PageSize, int currentPageIndex, string sortingText)
        {
            List<ArticleEntry> lstStories = GetAISDoMoreArticles(3651);
            try
            {
                if (lstStories != null && lstStories.Count > 0)
                {

                    if (sortingText.Equals("MOST POPULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        lstStories = lstStories.OrderByDescending(a => a.Hearts).ToList<ArticleEntry>();
                    }
                    else
                    {
                        lstStories = lstStories.OrderByDescending(a => a.UploadDateAsDateTime).ToList<ArticleEntry>();
                    }


                }
                var result = new
                {
                    articles = lstStories.Skip(currentPageIndex * PageSize).Take(PageSize),
                    totalPages = Math.Ceiling((decimal)(lstStories.Count / (decimal)PageSize))
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : something went wrong while sorting the results in GetAISArticles() method.: "
                      + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return base.Json("GetAISArticles failed, Please try again", "text/plain");
            }
        }

        [NonAction]
        private List<ArticleEntry> GetAISDoMoreArticles(int key)
        {
            try
            {
                int CacheMinutes = 0, entriesNodeId = 0;
                if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                    CacheMinutes = 0;
                if (!int.TryParse(ConfigurationManager.AppSettings["EntriesNodeId"], out entriesNodeId))
                    entriesNodeId = uQuery.GetNodesByType("Empowerment").FirstOrDefault().Id;

                string cacheKey = string.Format(CachePrefix, string.Format("GetChildrenNodesOfEntriesByID_{0}", key));
                object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;


                List<ArticleEntry> userStories = new List<ArticleEntry>();

                if (configObj == null)
                {
                    if (entriesNodeId > 0)
                    {
                        umbraco.NodeFactory.Node node = new umbraco.NodeFactory.Node(entriesNodeId);
                        if (node != null)
                        {
                            umbraco.NodeFactory.Nodes childrenNodes = node.Children;
                            if (childrenNodes != null && childrenNodes.Count > 0)
                            {
                                foreach (umbraco.NodeFactory.Node children in childrenNodes)
                                {
                                    string articletype = Convert.ToString(children.GetProperty("articleType").Value);

                                    if (articletype.Equals("AIS", StringComparison.OrdinalIgnoreCase) || articletype.Equals("DoMore team", StringComparison.OrdinalIgnoreCase))
                                    {
                                        ArticleEntry article = new ArticleEntry();
                                        article.Id = children.Id;
                                        article.UploadDateAsDateTime = children.CreateDate;
                                        article.UploadedDateAsString = children.CreateDate.ToString("dd/MM/yyy");

                                        article.ActualArticleURL = children.NiceUrl;
                                        article.ArticleTitle = children.GetProperty<string>("articleTitle");
                                        article.Hearts = String.IsNullOrEmpty(children.GetProperty<string>("like")) ? 0 : Convert.ToInt32(children.GetProperty("like").Value);
                                        article.Excerpt = Convert.ToString(children.GetProperty("excerpt").Value);

                                        #region get article thumbnail
                                        int nodeid = String.IsNullOrWhiteSpace(children.GetProperty<string>("heroImage")) ? 0 : Convert.ToInt32(children.GetProperty("heroImage").Value);

                                        if (nodeid != 0)
                                        {
                                            Media imgNode = new Media(nodeid);

                                            article.ArticleThumbnail = imgNode.GetImageUrl();
                                        }
                                        else
                                        {
                                            article.ArticleThumbnail = "false";
                                        }
                                        #endregion

                                        // Author Type Umbraco CMS change 
                                        //int authorid = String.IsNullOrEmpty(children.GetProperty<string>("articleAuthor")) ? 0 : Convert.ToInt32(children.GetProperty<string>("articleAuthor"));
                                        //article.AmbassadorId = authorid;

                                        //if (authorid > 0)
                                        //{
                                        //    Node author = new Node(authorid);
                                        //    article.AmbassadorName = author.Name;
                                        //    article.AmbassadorURL = author.NiceUrl;

                                        //    #region get author image
                                        //    int authorimageid = String.IsNullOrEmpty(author.GetProperty<string>("ambassadorImage")) ? 0 : Convert.ToInt32(author.GetProperty("ambassadorImage").Value);

                                        //    if (authorimageid != 0)
                                        //    {
                                        //        Media authorimgNode = new Media(authorimageid);

                                        //        article.AmbassadorImage = authorimgNode.GetImageUrl();
                                        //    }
                                        //    else
                                        //    {
                                        //        article.AmbassadorImage = "false";
                                        //    }

                                        //    #endregion
                                        //}

                                        article.Type = Convert.ToString(children.GetProperty<string>("articleType"));

                                        article.AmbassadorName = !string.IsNullOrEmpty(children.GetProperty<string>("articleAuthor")) ? children.GetProperty<string>("articleAuthor") : string.Empty;

                                        userStories.Add(article);

                                        configObj = userStories;

                                        if (configObj != null && System.Web.HttpContext.Current != null)
                                        {
                                            System.Web.HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                                             System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                                        }
                                    }

                                }

                            }

                        }
                    }
                }
                return configObj as List<ArticleEntry>;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : something went wrong while in GetAISDoMoreArticles() method.: "
                     + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }

        #endregion

        [NonAction]
        public List<AmbassadorModel> GetAmbassadors(int key)
        {
            int CacheMinutes = 0, entriesNodeId = 0;
            if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                CacheMinutes = 0;
            if (!int.TryParse(ConfigurationManager.AppSettings["EntriesNodeId"], out entriesNodeId))
                entriesNodeId = uQuery.GetNodesByType("Ambassadors").FirstOrDefault().Id;

            string cacheKey = string.Format(CachePrefix, string.Format("GetAmbassadors{0}", key));
            object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;

            try
            {

                List<AmbassadorModel> Ambassadors = new List<AmbassadorModel>();

                if (configObj == null)
                {
                    if (entriesNodeId > 0)
                    {
                        umbraco.NodeFactory.Node node = new umbraco.NodeFactory.Node(entriesNodeId);
                        if (node != null)
                        {
                            umbraco.NodeFactory.Nodes childrenNodes = node.Children;
                            if (childrenNodes != null && childrenNodes.Count > 0)
                            {
                                foreach (umbraco.NodeFactory.Node children in childrenNodes)
                                {
                                    AmbassadorModel AmbassadorEntry = new AmbassadorModel();
                                    AmbassadorEntry.AmbassadorName = children.GetProperty<string>("ambassadorName");
                                    AmbassadorEntry.AmbassadorGoal = children.GetProperty<string>("ambassadorGoal");
                                    AmbassadorEntry.AmbassadorDescription = children.GetProperty<string>("ambassadorDescription");

                                    AmbassadorEntry.AmbassadorURL = children.NiceUrl;
                                    AmbassadorEntry.AmbassadorId = children.Id;
                                    AmbassadorEntry.createdDate = children.CreateDate;

                                    #region get AmbassadorImage
                                    int nodeid = String.IsNullOrWhiteSpace(children.GetProperty<string>("ambassadorImage")) ? 0 : Convert.ToInt32(children.GetProperty("ambassadorImage").Value);

                                    if (nodeid != 0)
                                    {
                                        Media imgNode = new Media(nodeid);

                                        AmbassadorEntry.AmbassadorImage = imgNode.GetImageUrl();
                                    }
                                    else
                                    {
                                        AmbassadorEntry.AmbassadorImage = "false";
                                    }
                                    #endregion

                                    Ambassadors.Add(AmbassadorEntry);

                                    configObj = Ambassadors;

                                    if (configObj != null && System.Web.HttpContext.Current != null)
                                    {
                                        System.Web.HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                                         System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS :GetAmbassadors() method failed: "
                     + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;

            }

            return configObj as List<AmbassadorModel>;
        }

        [HttpPost]
        public JsonResult SignUpForUpdates(string emailAddress)
        {
            try
            {
                if (!string.IsNullOrEmpty(emailAddress))
                {
                    if (RexonaAU.Helpers.Common.TractionAPI.customerwithCustomAttribute(emailAddress))
                        return Json("true");
                    else
                        return Json("false");
                }
                else
                    return Json("false");

            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : SignUpForUpdates() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return Json("false");
            }
        }

        [HttpPost]
        public JsonResult UpdateArticleEDMInfo(int articleId, bool includeInEDM, DateTime mailOutDate/*, string articleCategory, string articleBucket*/)
        {
            try
            {
                var service = new ContentService();
                var article = service.GetById(articleId);


                if (article != null)
                {
                    article.SetValue("includeInEDM", includeInEDM);
                    article.SetValue("mailOutDate", mailOutDate);
                    /*article.SetValue("articleCategory", articleCategory);
                    article.SetValue("articleBucket", articleBucket);*/

                    service.SaveAndPublish(article);
                    return Json("true");
                }
                else
                    return Json("false");

            }
            catch (Exception ex)
            {
                LogHelper.Error(this.GetType(), "Unable to UpdateArticleEDMInfo", ex);
                return Json("false");
            }
        }
    }
}
