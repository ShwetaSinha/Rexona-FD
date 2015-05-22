using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RexonaAU.Models;
using Umbraco.Core.Services;
using umbraco;
using umbraco.NodeFactory;
using Umbraco.Core.Logging;
using System.Configuration;
using RexonaAU.Helpers;
using RexonaAU.Controllers;
using umbraco.MacroEngines;
using umbraco.cms.businesslogic.media;
using System.Data.Entity.Validation;
using umbraco.cms.businesslogic.member;

namespace RexonaAU.Controllers
{
    public class TopContentController : Umbraco.Web.Mvc.SurfaceController
    {
        //
        // GET: /TopContent/

        //public Property CacheMinutes { get; set; }

        private const string CachePrefix = "RexonaAUTopContent_{0}";


        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public List<TopContent> FetchTwitterContent()
        {
            int CacheMinutes = 0;
            if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                CacheMinutes = 30;

            List<TopContent> twiter = new List<TopContent>();

            Entities dbEntities = new Entities();


            string cacheKey = string.Format(CachePrefix, string.Format("GetTweets_{0}", 04657));
            object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;

            try
            {
                if (configObj == null)
                {
                    var objTweets = dbEntities.TwitterHashTags
                            .Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved)
                            .Select(tweets =>
                                        new TopContent()
                                            {
                                                Author = tweets.ScreeName,
                                                CreatedAt = (tweets.CreatedDate.HasValue ? tweets.CreatedDate.Value : DateTime.MinValue),
                                                Likes = tweets.Likes.HasValue ? tweets.Likes.Value : 0,
                                                MediaURL = tweets.MediaUrl, //TO DO ?item.GetProperty<string>("mediaURL"):string.Empty ;
                                                Content = tweets.Post,
                                                Style = "socialContent",
                                                Source = "Tweet",
                                                Id = tweets.Id

                                            }
                                       )
                            .ToList()
                            .OrderByDescending(tweet => tweet.CreatedAt);

                    configObj = objTweets.ToList();

                    if (configObj != null && System.Web.HttpContext.Current != null)
                    {
                        System.Web.HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                         System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                    }
                }

                return configObj as List<TopContent>;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "FetchTwitterContent Failed " + Environment.NewLine + "Message: " + ex.Message
                    + Environment.NewLine + "Stack Trace: " + ex.StackTrace);

                return null;
            }

        }

        [HttpPost]
        public List<TopContent> FetchInstaGramContent()
        {
            int CacheMinutes = 0;
            if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                CacheMinutes = 30;

            List<TopContent> twiter = new List<TopContent>();

            Entities dbEntities = new Entities();


            string cacheKey = string.Format(CachePrefix, string.Format("GetInsta_{0}", 03465));
            object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;

            try
            {
                if (configObj == null)
                {

                    var objInsta = dbEntities.InstaGramHashTags
                            .Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved)
                            .Select(tweets =>
                                    new TopContent()
                                    {
                                        Author = tweets.ScreeName,
                                        CreatedAt = (tweets.CreatedDate.HasValue ? tweets.CreatedDate.Value : DateTime.MinValue),
                                        Likes = tweets.Likes.HasValue ? tweets.Likes.Value : 0,
                                        MediaURL = tweets.MediaUrl, //TO DO ?item.GetProperty<string>("mediaURL"):string.Empty ;
                                        Content = tweets.Post,
                                        Style = "socialContent",
                                        Source = "Instagram",
                                        Id = tweets.Id,
                                    })
                            .ToList()
                            .OrderByDescending(tweet => tweet.CreatedAt);

                    configObj = objInsta.ToList();

                    if (configObj != null && System.Web.HttpContext.Current != null)
                    {
                        System.Web.HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                         System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                    }
                }
                return configObj as List<TopContent>;

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "FetchInstaGramContent Failed " + Environment.NewLine
                    + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }

        }

        [HttpGet]
        public JsonResult FetchTopContent(int PageSize, int currentPageIndex, string sortingText, string filterText)
        {
            List<TopContent> topContent = new List<TopContent>();
            List<TopContent> mergedList = new List<TopContent>();
            
            //Twitter List Items
            List<TopContent> tweets = FetchTwitterContent();
            //Instagram List Items
            List<TopContent> instaGram = FetchInstaGramContent();
            //Home Page Articles 
            List<TopContent> inspirationArticles = GetHomePageArticles();
            //Total Pledges
            List<TopContent> pledgesMade = GetAllPledges();

            try
            {
                

                if (filterText.Equals("All Content", StringComparison.OrdinalIgnoreCase))
                {
                    Session.Remove("MergedList");
                    if (sortingText.Equals("MOST POPULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentPageIndex > 0 && Session["PopularMergedList"] != null)
                        {
                            topContent = Session["PopularMergedList"] as List<TopContent>;
                        }
                        else
                        {
                            tweets = tweets.OrderByDescending(top => top.Likes).ToList();
                            instaGram = instaGram.OrderByDescending(top => top.Likes).ToList();
                            inspirationArticles = inspirationArticles.OrderByDescending(top => top.Likes).ToList();
                            if (inspirationArticles != null && inspirationArticles.Count > 0)
                            {
                                inspirationArticles = ReturnListOfArticlesUsingTopContentAlgorithm(inspirationArticles);
                            }

                            pledgesMade = pledgesMade.OrderByDescending(top => top.Likes).ToList();
                            if (pledgesMade != null && pledgesMade.Count > 0)
                            {
                                pledgesMade = ReturnListOfPledgesUsingTopContentAlgorithm(pledgesMade);
                            }

                            ReturnListUsingAlgorithm(tweets, instaGram, inspirationArticles, pledgesMade);
                            if (Session["MergedList"] != null)
                            {
                                topContent = Session["MergedList"] as List<TopContent>;
                                Session["PopularMergedList"] = Session["MergedList"];
                                Session.Remove("MergedList");
                            }
                        }
                    }
                    else
                    {
                        if (currentPageIndex > 0 && Session["RecentMergedList"] != null)
                        {
                            topContent = Session["RecentMergedList"] as List<TopContent>;
                        }
                        else
                        {
                            tweets = tweets.OrderByDescending(top => top.CreatedAt).ToList();
                            instaGram = instaGram.OrderByDescending(top => top.CreatedAt).ToList();
                            inspirationArticles = inspirationArticles.OrderByDescending(top => top.CreatedAt).ToList();
                            pledgesMade = pledgesMade.OrderByDescending(top => top.CreatedAt).ToList();
                            ReturnListUsingAlgorithm(tweets, instaGram, inspirationArticles, pledgesMade);
                            if (Session["MergedList"] != null)
                            {
                                topContent = Session["MergedList"] as List<TopContent>;
                                Session["RecentMergedList"] = Session["MergedList"];
                                Session.Remove("MergedList");
                            }
                        }
                    }
 
                    

                }
                else if (filterText.Equals("Goals", StringComparison.OrdinalIgnoreCase))
                {
                    if (sortingText.Equals("MOST POPULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        topContent = pledgesMade.OrderByDescending(top => top.Likes).ToList();
                    }
                    else
                    {
                        topContent = pledgesMade.OrderByDescending(top => top.CreatedAt).ToList();
                    }
                }
                else if (filterText.Equals("Social Content", StringComparison.OrdinalIgnoreCase))
                {
                    if (tweets != null && instaGram != null)
                    {
                        mergedList = tweets.Concat(instaGram).ToList();
                    }
                    else if (tweets != null)
                    {
                        mergedList = tweets.ToList();
                    }
                    else if (instaGram != null)
                    {
                        mergedList = instaGram.ToList();
                    }

                    if (sortingText.Equals("MOST POPULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        topContent = mergedList.ToList().OrderByDescending(top => top.Likes).ToList();
                    }
                    else
                    {
                        topContent = mergedList.ToList().OrderByDescending(social => social.CreatedAt).ToList();
                    }

                }
                else if (filterText.Equals("Empower Articles", StringComparison.OrdinalIgnoreCase))
                {
                    if (sortingText.Equals("MOST POPULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        topContent = inspirationArticles.OrderByDescending(articles => articles.Likes).ToList();
                    }
                    else
                    {
                        topContent = inspirationArticles.OrderByDescending(articles => articles.CreatedAt).ToList();
                    }
                }
                
                var result = new
                {
                    topContent = topContent.Skip(currentPageIndex * PageSize).Take(PageSize),
                    totalPages = Math.Ceiling((decimal)(topContent.Count / (decimal)PageSize))
                };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                                     .SelectMany(x => x.ValidationErrors)
                                    .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS:Fetch Top Content - " + exceptionMessage);
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "FetchTopContent Failed "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        //Get Pledges based on Algorithm i.e. to put latest pledges with ZERO likes on Top and then add other pledges as per popularity
        public List<TopContent> ReturnListOfPledgesUsingTopContentAlgorithm(List<TopContent> Pledges)
        {
            Node keywordsNode = new Node(uQuery.GetNodesByName("Keywords").FirstOrDefault().Id);
            List<string> keywords = new List<string>();
            Dictionary<string, int> dicCategoryWithCount = new Dictionary<string, int>();
            if (keywordsNode != null)
            {
                keywords = keywordsNode.ChildrenAsList.Select(a => a.Name).ToList<string>();
                foreach (var keyword in keywords)
                {
                    int countOfPledgesOfThisCategory = Pledges.Where(a => a.CategoryTag.ToLower().Contains(keyword.ToLower())).Select(a => a).ToList().Count();
                    dicCategoryWithCount.Add(keyword, countOfPledgesOfThisCategory);
                }

                dicCategoryWithCount = dicCategoryWithCount.OrderByDescending(a => a.Value).ToDictionary(a => a.Key, a => a.Value);
            }

            //Subtract 2-3 days (configure in web.config)
            int NumberOfDaysConsideredForZeroLikes = 0;
            if (!int.TryParse(ConfigurationManager.AppSettings["NumberOfDaysConsideredForZeroLikes"], out NumberOfDaysConsideredForZeroLikes))
                NumberOfDaysConsideredForZeroLikes = 3;
            DateTime GetDateToCompare = DateTime.Now.AddDays(-NumberOfDaysConsideredForZeroLikes);
            GetDateToCompare = Convert.ToDateTime(GetDateToCompare.ToString("dd-MM-yyyy 00:00:00"));

            List<TopContent> newPledgesWithZeroLikes = Pledges.Where(a => (a.Likes == 0 && a.CreatedAt > GetDateToCompare)).Select(a => a).OrderByDescending(a => a.CreatedAt).ToList<TopContent>();
            List<TopContent> newPledgesWithGreaterThanZeroLikes = Pledges.Where(a => (a.Likes != 0 && a.CreatedAt > GetDateToCompare) || a.CreatedAt < GetDateToCompare).Select(a => a).ToList<TopContent>();

            List<TopContent> newMergedList = new List<TopContent>();
            foreach (var key in dicCategoryWithCount.Keys)
            {
                List<TopContent> specificCategoryList = newPledgesWithZeroLikes.Where(a => a.CategoryTag.ToLower().Contains(key.ToLower())).Select(a => a).ToList<TopContent>();
                newMergedList = newMergedList.Concat(specificCategoryList).ToList<TopContent>();
                newPledgesWithZeroLikes.RemoveAll(a => a.CategoryTag.ToLower().Contains(key.ToLower()));
            }

            newMergedList = newMergedList.Concat(newPledgesWithGreaterThanZeroLikes).ToList<TopContent>();
            return newMergedList;
        }


        //Get articles based on Algorithm i.e. the articles with zero likes should be placed at the top
        public List<TopContent> ReturnListOfArticlesUsingTopContentAlgorithm(List<TopContent> Articles)
        {
            Node keywordsNode = new Node(uQuery.GetNodesByName("Keywords").FirstOrDefault().Id);
            List<string> keywords = new List<string>();
            Dictionary<string, int> dicCategoryWithCount = new Dictionary<string, int>();
            if (keywordsNode != null)
            {
                keywords = keywordsNode.ChildrenAsList.Select(a => a.Name).ToList<string>();
                foreach (var keyword in keywords)
                {
                    int countOfPledgesOfThisCategory = Articles.Where(a => a.CategoryTag.ToLower().Contains(keyword.ToLower())).Select(a => a).ToList().Count();
                    dicCategoryWithCount.Add(keyword, countOfPledgesOfThisCategory);
                }

                dicCategoryWithCount = dicCategoryWithCount.OrderByDescending(a => a.Value).ToDictionary(a => a.Key, a => a.Value);
            }

            //Subtract days as configured in web.config
            int NumberOfDaysConsideredForZeroLikes = 0;
            if (!int.TryParse(ConfigurationManager.AppSettings["NumberOfDaysConsideredForZeroLikes"], out NumberOfDaysConsideredForZeroLikes))
                NumberOfDaysConsideredForZeroLikes = 3;
            DateTime GetDateToCompare = DateTime.Now.AddDays(-NumberOfDaysConsideredForZeroLikes);
            GetDateToCompare = Convert.ToDateTime(GetDateToCompare.ToString("dd-MM-yyyy 00:00:00"));

            List<TopContent> newArticlesWithZeroLikes = Articles.Where(a => (a.Likes == 0 && a.CreatedAt > GetDateToCompare)).Select(a => a).OrderByDescending(a => a.CreatedAt).ToList<TopContent>();
            List<TopContent> newArticlesWithGreaterThanZeroLikes = Articles.Where(a => (a.Likes != 0 && a.CreatedAt > GetDateToCompare) || a.CreatedAt < GetDateToCompare).Select(a => a).ToList<TopContent>();

            List<TopContent> newMergedList = new List<TopContent>();
            foreach (var key in dicCategoryWithCount.Keys)
            {
                List<TopContent> specificCategoryList = newArticlesWithZeroLikes.Where(a => a.CategoryTag.ToLower().Contains(key.ToLower())).Select(a => a).ToList<TopContent>();
                newMergedList = newMergedList.Concat(specificCategoryList).ToList<TopContent>();
                newArticlesWithZeroLikes.RemoveAll(a => a.CategoryTag.ToLower().Contains(key.ToLower()));
            }

            newMergedList = newMergedList.Concat(newArticlesWithGreaterThanZeroLikes).ToList<TopContent>();
            return newMergedList;
        }


        Random random = new Random();
        int randomNumber = 0;
        public void ReturnListUsingAlgorithm(List<TopContent> Tweets, List<TopContent> Instagrams, List<TopContent> Articles, List<TopContent> Pledges)
        {

            List<TopContent> returnList = new List<TopContent>();
            try
            {
                if (Session["MergedList"] != null)
                {
                    returnList = Session["MergedList"] as List<TopContent>;
                }

                if (Tweets.Count > 0 || Instagrams.Count > 0 || Articles.Count > 0 || Pledges.Count > 0)
                {
                    if (Tweets.Count > 0)
                    {
                        randomNumber = random.Next(1, 4);
                        returnList = returnList.Concat(Tweets.Take(randomNumber)).ToList<TopContent>();
                        if (Tweets.Count > randomNumber - 1)
                        {
                            Tweets.RemoveRange(0, randomNumber);
                        }
                        else
                        {
                            Tweets.RemoveRange(0, Tweets.Count);
                        }
                    }
                    
                    if (Articles.Count > 0)
                    {
                        randomNumber = random.Next(1, 4);
                        returnList = returnList.Concat(Articles.Take(randomNumber)).ToList<TopContent>();
                        if (Articles.Count > randomNumber - 1)
                        {
                            Articles.RemoveRange(0, randomNumber);
                        }
                        else
                        {
                            Articles.RemoveRange(0, Articles.Count);
                        }
                    }

                    if (Pledges.Count > 0)
                    {
                        randomNumber = random.Next(1, 4);
                        returnList = returnList.Concat(Pledges.Take(randomNumber)).ToList<TopContent>();
                        if (Pledges.Count > randomNumber - 1)
                        {
                            Pledges.RemoveRange(0, randomNumber);
                        }
                        else
                        {
                            Pledges.RemoveRange(0, Pledges.Count);
                        }
                    }

                    if (Instagrams.Count > 0)
                    {
                        randomNumber = random.Next(1, 4);
                        if (Tweets.Count == 0 && Articles.Count == 0 && Pledges.Count == 0)
                        {
                            returnList = returnList.Concat(Instagrams).ToList<TopContent>();
                            Instagrams.RemoveRange(0, Instagrams.Count);
                        }
                        else
                        {
                            returnList = returnList.Concat(Instagrams.Take(randomNumber)).ToList<TopContent>();
                        }

                        if (Instagrams.Count > randomNumber - 1)
                        {
                            Instagrams.RemoveRange(0, randomNumber);
                        }
                        else
                        {
                            if (Instagrams.Count > 0)
                            {
                                Instagrams.RemoveRange(0, Instagrams.Count);
                            }
                        }
                    }


                    Session["MergedList"] = returnList;

                    if (Tweets.Count > 0 || Instagrams.Count > 0 || Articles.Count > 0 || Pledges.Count > 0)
                    {
                        ReturnListUsingAlgorithm(Tweets, Instagrams, Articles, Pledges);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "ReturnListUsingAlgorithm Failed "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);                
            }
        }

        public List<TopContent> GetHomePageArticles()
        {

            int entriesNodeId = 0;
            entriesNodeId = uQuery.GetNodesByType("Empowerment").FirstOrDefault().Id;
            int CacheMinutes = 0;
            if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                CacheMinutes = 30;
            string cacheKey = string.Format(CachePrefix, string.Format("GetHomePageArticles_{0}", 04657));
            object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;
            List<TopContent> articles = new List<TopContent>();

            try
            {
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

                                    TopContent article = new TopContent();

                                    article.Id = children.Id;
                                    //  article.CreatedAt = children.CreateDate;
                                    article.CreatedAt = children.CreateDate;
                                    article.LinkUrl = children.NiceUrl;
                                    article.ArticleTitle = children.GetProperty<string>("articleTitle");
                                    article.Likes = String.IsNullOrWhiteSpace(children.GetProperty<string>("like")) ? 0 : Convert.ToInt32(children.GetProperty("like").Value);
                                    article.Excerpt = Convert.ToString(children.GetProperty("excerpt").Value);
                                    article.Source = "Articles";
                                    article.CategoryTag = children.GetProperty<string>("articleTag");
                                    int nodeid = String.IsNullOrWhiteSpace(children.GetProperty<string>("heroImage")) ? 0 : Convert.ToInt32(children.GetProperty("heroImage").Value);

                                    if (nodeid != 0)
                                    {
                                        Media imgNode = new Media(nodeid);

                                        article.MediaURL = imgNode.GetImageUrl();
                                    }
                                    else
                                    {
                                        article.MediaURL = "false";
                                    }

                                    // Author Type Umbraco CMS change 
                                    //int authorid = String.IsNullOrWhiteSpace(children.GetProperty<string>("articleAuthor")) ? 0 : Convert.ToInt32(children.GetProperty<string>("articleAuthor"));
                                    //Node author = new Node(authorid);
                                    //article.AmbassadorName = author.Name;
                                    //article.AmbassadorURL = author.NiceUrl;

                                    article.Source = Convert.ToString(children.GetProperty<string>("articleType"));
                                    article.Style = "inspirationArticle";
                                    article.Author = ! string.IsNullOrEmpty(children.GetProperty<string>("articleAuthor")) ? children.GetProperty<string>("articleAuthor") : string.Empty;

                                    articles.Add(article);
                                    configObj = articles;

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
                return configObj as List<TopContent>;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona Au : GetHomePageArticles() method failed during execution. Stack Trace: "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }


        public List<TopContent> GetAllPledges()
        {
           Node pledges = new Node(uQuery.GetNodesByType("Pledges").FirstOrDefault().Id);
            List<TopContent> pledgeDetails = new List<TopContent>();

            int CacheMinutes = 0;
            if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                CacheMinutes = 30;
            string cacheKey = string.Format(CachePrefix, string.Format("GetAllPledges{0}", pledges.Id));
            object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;
            int memberId = 0;
            Member currentmember = Member.GetCurrentMember();
            if (currentmember != null)
            {
                memberId = currentmember.Id;
            }
            try
            {

                if (configObj == null)
                {
                    if (pledges.Id > 0)
                    {
                        foreach (var childNode in pledges.ChildrenAsList)
                        {
                            if (childNode.ChildrenAsList.Count() > 0)
                            {
                                TopContent pledge = new TopContent();
                                var childItems = childNode.ChildrenAsList;


                                pledge.MediaURL = childItems.Count() > 0 ? childItems.Where(author => author.GetProperty<bool>("isOwner") == true).Select(l => l.GetProperty("imageUrl").Value).FirstOrDefault().ToString() : string.Empty;
                                pledge.Id = childNode.Id;
                                pledge.JoinedPeoples = childItems.Where(a => a.GetProperty<bool>("step3Clear")).ToList().Count;
                                pledge.PublicPledge = childNode.GetProperty<bool>("publicPledgeSelection");
                                pledge.Likes = childNode.GetProperty<int>("likeCount");
                                pledge.CreatedAt = Convert.ToDateTime(childNode.CreateDate);
                                pledge.Style = "pledges";
                                pledge.Source = "Pledge";
                                //Need to be changed to false to true if only logged in user need to see Join
                                pledge.IsOwner = memberId != 0 ? childNode.ChildrenAsList.Where(a => a.GetProperty<int>("memberId") == memberId).Count() > 0 : false;
                                pledge.LinkUrl = childNode.NiceUrl;
                                pledge.Step3Clear = childItems.Count() > 0 ? childItems.Where(author => author.GetProperty("isOwner").Value == "1").Select(a => a.GetProperty<bool>("step3Clear")).ToList<bool>()[0] : false;
                                pledge.CategoryTag = childNode.GetProperty<string>("categoryTag");

                                if (pledge.Step3Clear)
                                {
                                    pledgeDetails.Add(pledge);
                                }

                                configObj = pledgeDetails;

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
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU : GetAllPledges() method failed during execution. Stack Trace: "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            return configObj as List<TopContent>;
        }


    }
}
