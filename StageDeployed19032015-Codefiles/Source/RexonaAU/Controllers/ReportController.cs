using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.Logging;
using System.Data.Entity;
using System.Data.Entity.Validation;
using RexonaAU.Helpers;
using RexonaAU.Models;
using umbraco;
using umbraco.cms.businesslogic.member;
using umbraco.NodeFactory;
using Umbraco.Core;
using System.Drawing;
using System.Web.UI.DataVisualization.Charting;
using System.IO;
using System.Configuration;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace RexonaAU.Controllers
{
    public class ReportController : Umbraco.Web.Mvc.SurfaceController
    {
        //
        // GET: /Report/

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult ContentReporting(string startDate, string endDate)
        {
            string message = "Success";

            //create 4 lists
            List<TopArticles> topArticles = new List<TopArticles>();
            ContentModel reportData = new ContentModel();
            List<SocialContent> topTweets = new List<SocialContent>();
            List<SocialContent> topInstaGram = new List<SocialContent>();

            DateTime startFilter = DateTime.Now;
            DateTime endFilter = DateTime.Now;



            if (!string.IsNullOrEmpty(startDate))
            {
                startFilter = Convert.ToDateTime(startDate);
                Session["StartDate"] = startDate;
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                endFilter = Convert.ToDateTime(endDate + " 23:59:59");
                Session["EndDate"] = startDate;
            }
            try
            {
                Node artilceNode = uQuery.GetNodesByType("Empowerment").FirstOrDefault();

                if (artilceNode != null && artilceNode.Id > 0)
                {
                    if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                    {
                        reportData.AISArticles = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("AIS", StringComparison.OrdinalIgnoreCase)
                            && article.CreateDate > startFilter && article.CreateDate <= endFilter).ToList().Count;
                        if (reportData.AISArticles > 0)
                        {
                            //reportData.AISArticleViews = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("AIS", StringComparison.OrdinalIgnoreCase)).Select(article => article.GetProperty<int>("views")).Sum();
                        }
                        reportData.AmbassadorArticles = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("Ambassador", StringComparison.OrdinalIgnoreCase)
                             && article.CreateDate > startFilter && article.CreateDate <= endFilter).ToList().Count;
                        if (reportData.AmbassadorArticles > 0)
                        {
                            //reportData.AmbassadorArticleViews = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("Ambassador", StringComparison.OrdinalIgnoreCase)).Select(article => article.GetProperty<int>("views")).Sum();
                        }

                        reportData.DoMoreArticles = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("DoMore team", StringComparison.OrdinalIgnoreCase)
                              && article.CreateDate > startFilter && article.CreateDate <= endFilter).ToList().Count;
                        if (reportData.DoMoreArticles > 0)
                        {
                            //reportData.DoMoreArticleViews = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("DoMore team", StringComparison.OrdinalIgnoreCase)).Select(article => article.GetProperty<int>("views")).Sum();
                        }


                        #region Google Analytics for top articles
                        //referance http://stackoverflow.com/questions/10306872/use-google-analytics-api-to-show-information-in-c-sharp
                        GoogleAnalyticsAPI.AnalyticDataPoint analyticDataPoint = new GoogleAnalyticsAPI.AnalyticDataPoint();

                        GoogleAnalyticsAPI googleAnalyticsAPI = new RexonaAU.Helpers.GoogleAnalyticsAPI(Server.MapPath(System.Configuration.ConfigurationManager.AppSettings["GoogleReportingCertificateFilePath"]), System.Configuration.ConfigurationManager.AppSettings["GoogleReportingEmail"]);


                        analyticDataPoint = googleAnalyticsAPI.GetAnalyticsData(Convert.ToString(ConfigurationManager.AppSettings["GoogleViewId"]), new string[] { "ga:pagePath" }, new string[] { "ga:pageviews" }, startFilter, endFilter);

                        //get sign up node URL
                        string signUpNiceURL = uQuery.GetNodesByType("SignUp").FirstOrDefault().NiceUrl;


                        //get master of all articles
                        string aisAdviceNiceURL = string.Empty;
                        Node aisAdviceNode = uQuery.GetNodesByType("Empowerment").FirstOrDefault();
                        if (aisAdviceNode.Id > 0 && !string.IsNullOrEmpty(signUpNiceURL))
                        {
                            //get url of ais advice node
                            aisAdviceNiceURL = aisAdviceNode.NiceUrl;
                            if (analyticDataPoint.Rows.Count > 0)
                            {
                                foreach (var x in analyticDataPoint.Rows)
                                {
                                    //if url in google analytics contains aisAdviceNiceURL then it is an article
                                    if (x[0].Contains(aisAdviceNiceURL + "/"))
                                    {

                                        Node node = uQuery.GetNodeByUrl(x[0]);
                                        if (node.NodeTypeAlias.Equals("Article") && node.Id > 0)
                                        {
                                            switch (node.GetProperty<string>("articleType"))
                                            {
                                                case "AIS":
                                                    reportData.AISArticleViews += Convert.ToInt32(x[1]);
                                                    break;
                                                case "Ambassador":
                                                    reportData.AmbassadorArticleViews += Convert.ToInt32(x[1]);
                                                    break;
                                                case "DoMore team":
                                                    reportData.DoMoreArticleViews += Convert.ToInt32(x[1]);
                                                    break;
                                            }

                                            topArticles.Add(
                                                new TopArticles()
                                                {
                                                    ArticleName = node.Name,
                                                    TotalViews = Convert.ToInt32(x[1])
                                                });
                                            topArticles = topArticles.OrderByDescending(obj => obj.TotalViews).Take(20).ToList();
                                        }
                                    }
                                    else if (x[0].Contains(signUpNiceURL))
                                    {
                                        //This is added to session to minimize the call to google analytics
                                        //the method used to count sign-up dropoff must be called from jquey after execution of 
                                        //this code.
                                        Session["signupCount"] = Convert.ToInt32(x[1]);
                                    }
                                }
                            }
                        }
                        #endregion

                    }
                    else
                    {
                        //no start date end date filter

                        reportData.AISArticles = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("AIS", StringComparison.OrdinalIgnoreCase)).ToList().Count;
                        if (reportData.AISArticles > 0)
                        {
                            //reportData.AISArticleViews = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("AIS", StringComparison.OrdinalIgnoreCase)).Select(article => article.GetProperty<int>("views")).Sum();
                        }
                        reportData.AmbassadorArticles = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("Ambassador", StringComparison.OrdinalIgnoreCase)).ToList().Count;
                        if (reportData.AmbassadorArticles > 0)
                        {
                            //reportData.AmbassadorArticleViews = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("Ambassador", StringComparison.OrdinalIgnoreCase)).Select(article => article.GetProperty<int>("views")).Sum();
                        }

                        reportData.DoMoreArticles = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("DoMore team", StringComparison.OrdinalIgnoreCase)).ToList().Count;
                        if (reportData.DoMoreArticles > 0)
                        {
                            //reportData.DoMoreArticleViews = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("DoMore team", StringComparison.OrdinalIgnoreCase)).Select(article => article.GetProperty<int>("views")).Sum();
                        }


                        #region Google Analytics for top articles
                        //referance http://stackoverflow.com/questions/10306872/use-google-analytics-api-to-show-information-in-c-sharp
                        GoogleAnalyticsAPI.AnalyticDataPoint analyticDataPoint = new GoogleAnalyticsAPI.AnalyticDataPoint();

                        //GoogleAnalyticsAPI googleAnalyticsAPI = new RexonaAU.Helpers.GoogleAnalyticsAPI(Server.MapPath(System.Configuration.ConfigurationManager.AppSettings["GoogleReportingCertificateFilePath"]), System.Configuration.ConfigurationManager.AppSettings["GoogleReportingEmail"]);

                        GoogleAnalyticsAPI googleAnalyticsAPI = new RexonaAU.Helpers.GoogleAnalyticsAPI(HttpContext.Server.MapPath(System.Configuration.ConfigurationManager.AppSettings["GoogleReportingCertificateFilePath"]), System.Configuration.ConfigurationManager.AppSettings["GoogleReportingEmail"]);



                        analyticDataPoint = googleAnalyticsAPI.GetAnalyticsData(Convert.ToString(ConfigurationManager.AppSettings["GoogleViewId"]), new string[] { "ga:pagePath" }, new string[] { "ga:pageviews" }, new DateTime(2014, 01, 01), DateTime.Now);

                        //get sign up node URL
                        string signUpNiceURL = uQuery.GetNodesByType("SignUp").FirstOrDefault().NiceUrl;


                        //get master of all articles
                        string aisAdviceNiceURL = string.Empty;
                        Node aisAdviceNode = uQuery.GetNodesByType("Empowerment").FirstOrDefault();
                        if (aisAdviceNode.Id > 0 && !string.IsNullOrEmpty(signUpNiceURL))
                        {
                            //get url of ais advice node
                            aisAdviceNiceURL = aisAdviceNode.NiceUrl;

                            foreach (var x in analyticDataPoint.Rows)
                            {
                                //if url in google analytics contains aisAdviceNiceURL then it is an article
                                if (x[0].Contains(aisAdviceNiceURL + "/"))
                                {

                                    Node node = uQuery.GetNodeByUrl(x[0]);
                                    if (node.NodeTypeAlias.Equals("Article") && node.Id > 0)
                                    {
                                        switch (node.GetProperty<string>("articleType"))
                                        {
                                            case "AIS":
                                                reportData.AISArticleViews += Convert.ToInt32(x[1]);
                                                break;
                                            case "Ambassador":
                                                reportData.AmbassadorArticleViews += Convert.ToInt32(x[1]);
                                                break;
                                            case "DoMore team":
                                                reportData.DoMoreArticleViews += Convert.ToInt32(x[1]);
                                                break;
                                        }

                                        topArticles.Add(
                                            new TopArticles()
                                            {
                                                ArticleName = node.Name,
                                                TotalViews = Convert.ToInt32(x[1])
                                            });
                                        topArticles = topArticles.OrderByDescending(obj => obj.TotalViews).Take(20).ToList();
                                    }
                                }
                                else if (x[0].Contains(signUpNiceURL))
                                {
                                    //This is added to session to minimize the call to google analytics
                                    //the method used to count sign-up dropoff must be called from jquey after execution of 
                                    //this code.
                                    Session["signupCount"] = Convert.ToInt32(x[1]);
                                }
                            }
                        }
                        #endregion
                    }
                }

                #region fetch social content reporting details


                Entities dbEntity = new Entities();
                int awaitingApproval = 0;
                int approvedPosts = 0;
                int rejectedPosts = 0;
                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    reportData.InstaPosts = dbEntity.InstaGramHashTags.Where(insta => insta.CreatedDate.Value > startFilter && insta.CreatedDate.Value <= endFilter)
                        .ToList().Count();

                    reportData.Tweets = dbEntity.TwitterHashTags.Where(tweet => tweet.CreatedDate.Value > startFilter && tweet.CreatedDate.Value <= endFilter)
                        .ToList().Count();

                    awaitingApproval = dbEntity.InstaGramHashTags.Where(insta => insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Pending
                     && insta.CreatedDate.Value > startFilter && insta.CreatedDate.Value <= endFilter).ToList().Count();

                    reportData.AwaitingApproval = awaitingApproval + dbEntity.TwitterHashTags.Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Pending
                     && tweet.CreatedDate.Value > startFilter && tweet.CreatedDate.Value <= endFilter).ToList().Count();

                    approvedPosts = dbEntity.InstaGramHashTags.Where(insta => insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved
                       && insta.UpdatedDate.Value > startFilter && insta.UpdatedDate.Value <= endFilter).ToList().Count();

                    reportData.ApprovedPosts = approvedPosts + dbEntity.TwitterHashTags.Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved
                     && tweet.UpdatedDate.Value > startFilter && tweet.UpdatedDate.Value <= endFilter).ToList().Count();

                    rejectedPosts = dbEntity.InstaGramHashTags.Where(insta => insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Rejected
                     && insta.UpdatedDate.Value > startFilter && insta.UpdatedDate.Value <= endFilter).ToList().Count();

                    reportData.RejectedPosts = rejectedPosts + dbEntity.TwitterHashTags.Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Rejected
                     && tweet.UpdatedDate.Value > startFilter && tweet.UpdatedDate.Value <= endFilter).ToList().Count();

                    reportData.SocialPosts = reportData.InstaPosts + reportData.Tweets;


                    topTweets = dbEntity.TwitterHashTags.ToList().Where(tweets => tweets.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved
                      && tweets.CreatedDate.Value > startFilter && tweets.CreatedDate.Value <= endFilter).Select(tweets => new SocialContent()
                    {
                        Author = tweets.ScreeName,
                        DateToDisplay = (tweets.CreatedDate.HasValue ? tweets.CreatedDate.Value.ToString("h:mmtt dd/MM/yyyy") : DateTime.MinValue.ToString("h:mmtt dd/MM/yyyy")),
                        Likes = tweets.Likes.HasValue ? tweets.Likes.Value : 0,
                        MediaURL = tweets.MediaUrl, //TO DO ?item.GetProperty<string>("mediaURL"):string.Empty ;
                        TweetText = tweets.Post.Trim(),
                        Id = tweets.Id
                    }).OrderByDescending(tweet => tweet.Likes).Take(5).ToList();


                    topInstaGram = dbEntity.InstaGramHashTags.ToList().Where(Insta => Insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved
                    && Insta.CreatedDate.Value > startFilter && Insta.CreatedDate.Value <= endFilter).Select(Insta => new SocialContent()
                    {
                        Author = Insta.ScreeName,
                        DateToDisplay = (Insta.CreatedDate.HasValue ? Insta.CreatedDate.Value.ToString("h:mmtt dd/MM/yyyy") : DateTime.MinValue.ToString("h:mmtt dd/MM/yyyy")),
                        Likes = Insta.Likes.HasValue ? Insta.Likes.Value : 0,
                        MediaURL = Insta.MediaUrl, //TO DO ?item.GetProperty<string>("mediaURL"):string.Empty ;
                        TweetText = Insta.Post.Trim(),
                        Id = Insta.Id,
                        TweetURL = Insta.PostUrl
                    }).OrderByDescending(Insta => Insta.Likes).Take(5).ToList();
                }
                else
                {
                    //no start date end date filter

                    reportData.InstaPosts = dbEntity.InstaGramHashTags.ToList().Count();
                    reportData.Tweets = dbEntity.TwitterHashTags.ToList().Count();

                    awaitingApproval = dbEntity.InstaGramHashTags.Where(insta => insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Pending).ToList().Count();
                    reportData.AwaitingApproval = awaitingApproval + dbEntity.TwitterHashTags.Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Pending).ToList().Count();

                    approvedPosts = dbEntity.InstaGramHashTags.Where(insta => insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved).ToList().Count();
                    reportData.ApprovedPosts = approvedPosts + dbEntity.TwitterHashTags.Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved).ToList().Count();


                    rejectedPosts = dbEntity.InstaGramHashTags.Where(insta => insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Rejected).ToList().Count();
                    reportData.RejectedPosts = rejectedPosts + dbEntity.TwitterHashTags.Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Rejected).ToList().Count();

                    reportData.SocialPosts = reportData.InstaPosts + reportData.Tweets;


                    topTweets = dbEntity.TwitterHashTags.ToList().Where(tweets => tweets.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved).Select(tweets => new SocialContent()
                    {
                        Author = tweets.ScreeName,
                        DateToDisplay = (tweets.CreatedDate.HasValue ? tweets.CreatedDate.Value.ToString("h:mmtt dd/MM/yyyy") : DateTime.MinValue.ToString("h:mmtt dd/MM/yyyy")),
                        Likes = tweets.Likes.HasValue ? tweets.Likes.Value : 0,
                        MediaURL = tweets.MediaUrl, //TO DO ?item.GetProperty<string>("mediaURL"):string.Empty ;
                        TweetText = tweets.Post.Trim(),
                        Id = tweets.Id
                    }).OrderByDescending(tweet => tweet.Likes).Take(5).ToList();


                    topInstaGram = dbEntity.InstaGramHashTags.ToList().Where(Insta => Insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved).Select(Insta => new SocialContent()
                    {
                        Author = Insta.ScreeName,
                        DateToDisplay = (Insta.CreatedDate.HasValue ? Insta.CreatedDate.Value.ToString("h:mmtt dd/MM/yyyy") : DateTime.MinValue.ToString("h:mmtt dd/MM/yyyy")),
                        Likes = Insta.Likes.HasValue ? Insta.Likes.Value : 0,
                        MediaURL = Insta.MediaUrl, //TO DO ?item.GetProperty<string>("mediaURL"):string.Empty ;
                        TweetText = Insta.Post.Trim(),
                        Id = Insta.Id,
                        TweetURL = Insta.PostUrl
                    }).OrderByDescending(Insta => Insta.Likes).Take(5).ToList();
                }
                #endregion


            }
            catch (DbEntityValidationException ex)
            {
                message = "Error";
                var errorMessages = ex.EntityValidationErrors
                                    .SelectMany(x => x.ValidationErrors)
                                   .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS: SocialContentReporting() Twitter Push Content - " + exceptionMessage);

            }
            catch (Exception ex)
            {
                message = "Error";
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : SocialContentReporting() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);

            }
            var result = new { message = message, report = reportData, topArticles = topArticles, topTweet = topTweets, topInsta = topInstaGram };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GoalsReporting(string startDate, string endDate)
        {
            string message = "Success";
            GoalsModel goals = new GoalsModel();
            DropOffModel dropoffs = new DropOffModel();
            List<ReportBucketModel> categoryTags = new List<ReportBucketModel>();


            DateTime startFilter = DateTime.Now;
            DateTime endFilter = DateTime.Now;
            if (!string.IsNullOrEmpty(startDate))
            {
                startFilter = Convert.ToDateTime(startDate);
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                endFilter = Convert.ToDateTime(endDate + " 23:59:59");
            }

            try
            {
                //TO DO

                var pledges = uQuery.GetNodesByType("Pledge");
                if (pledges != null)
                {
                    var pledgeNodeResult = from L1 in pledges
                                           where (L1.ChildrenAsList.Exists(a => a.GetProperty<bool>("isOwner") && a.GetProperty<bool>("step3Clear")))
                                           select new { CreatedDate = L1.CreateDate, Value = L1.GetProperty<string>("title"), Id = L1.Id, IsPublic = L1.GetProperty<bool>("publicPledgeSelection"), Tag = L1.GetProperty<string>("categoryTag") };

                    if (pledgeNodeResult != null)
                    {
                        if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                        {
                            pledgeNodeResult = pledgeNodeResult.Where(pledge => pledge.CreatedDate > startFilter && pledge.CreatedDate <= endFilter).ToList();
                        }

                        if (pledgeNodeResult != null)
                        {

                            //Add total members joined i.e. pledgeMember to Total Goals
                            goals.TotalGoals = pledgeNodeResult.ToList().Count;
                            goals.UniqueGoals = pledgeNodeResult.ToList().Count;

                            int openGoalsCount = pledgeNodeResult.Where(pledge => pledge.IsPublic).ToList().Count;
                            int closedGoalsCount = pledgeNodeResult.Where(pledge => !pledge.IsPublic).ToList().Count;
                            if (goals.TotalGoals > 0)
                            {
                                goals.OpenGoals = Math.Round((decimal)openGoalsCount * 100 / goals.TotalGoals, 2);
                                goals.ClosedGoals = Math.Round((decimal)closedGoalsCount * 100 / goals.TotalGoals, 2);
                            }
                            else
                            {
                                goals.OpenGoals = 0;
                                goals.ClosedGoals = 0;
                            }


                        }
                    }


                    var keyWordsNode = uQuery.GetNodesByType("Keyword");

                    if (keyWordsNode != null && goals.TotalGoals > 0)
                    {
                        foreach (var keyword in keyWordsNode)
                        {
                            categoryTags.Add(new ReportBucketModel
                            {
                                CategoryTag = keyword.Name,
                                PledgesCount = pledgeNodeResult.Where(p => p.Tag.ToLower().IndexOf(keyword.Name.ToLower()) > -1).ToList().Count,
                                PledgesCountPercentage = goals.TotalGoals > 0 ? Math.Round(((decimal)pledgeNodeResult.Where(p => p.Tag.ToLower().IndexOf(keyword.Name.ToLower()) > -1).ToList().Count / goals.TotalGoals) * 100, 2) : 0
                            });
                        }
                    }
                }

                var memberList = Member.GetAllAsList().ToList();

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    memberList = memberList.Where(member => member.CreateDateTime > startFilter && member.CreateDateTime <= endFilter).ToList();
                }

                int totalCount = memberList.Count;

                var PledgeMemberList = Common.GetAllMemberPledges().ToList();

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    PledgeMemberList = PledgeMemberList.Where(pmember => pmember.CreatedDate > startFilter && pmember.CreatedDate <= endFilter).ToList();
                }

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    goals.Discussions = uQuery.GetNodesByType("PledgeDiscussion").Where(discussion => discussion.CreateDate > startFilter && discussion.CreateDate <= endFilter).ToList().Count;
                    goals.Events = uQuery.GetNodesByType("Event").Where(events => events.CreateDate > startFilter && events.CreateDate <= endFilter).ToList().Count;
                }
                else
                {
                    goals.Discussions = uQuery.GetNodesByType("PledgeDiscussion").ToList().Count;
                    goals.Events = uQuery.GetNodesByType("Event").ToList().Count;
                }

                var memberPledges = uQuery.GetNodesByType("PledgeMember");

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    memberPledges = memberPledges.Where(pmember => pmember.CreateDate > startFilter && pmember.CreateDate <= endFilter).ToList();
                }


                //count no of people joined goals
                goals.MembersJoined = memberPledges.Where(pledgeMem => pledgeMem.GetProperty<bool>("step3Clear") && !pledgeMem.GetProperty<bool>("isOwner")).ToList().Count;

                //added members joined  to total Goals
                //Suggested by JO

                /*Current State:
                225 unique goals
                1000 joined to one of those unique goals
                Total goals/pledges is 1225
                User A creates a new goal
                226 unique goals
                1000 joined to one of those unique goals
                Total goals/pledges is 1226

                Users B, C & D have joined the goal created by user A
                226 unique goals
                1003 joined to one of those unique goals
                Total goals/pledges is 1229*/

                goals.TotalGoals += goals.MembersJoined;

                GoogleAnalyticsAPI.AnalyticDataPoint analyticDataPoint = new GoogleAnalyticsAPI.AnalyticDataPoint();

                GoogleAnalyticsAPI googleAnalyticsAPI = new RexonaAU.Helpers.GoogleAnalyticsAPI(Server.MapPath(System.Configuration.ConfigurationManager.AppSettings["GoogleReportingCertificateFilePath"]), System.Configuration.ConfigurationManager.AppSettings["GoogleReportingEmail"]);

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    analyticDataPoint = googleAnalyticsAPI.GetAnalyticsData(Convert.ToString(ConfigurationManager.AppSettings["GoogleViewId"]), new string[] { "ga:pagePath" }, new string[] { "ga:pageviews" }, startFilter, endFilter);
                }
                else
                {
                    analyticDataPoint = googleAnalyticsAPI.GetAnalyticsData(Convert.ToString(ConfigurationManager.AppSettings["GoogleViewId"]), new string[] { "ga:pagePath" }, new string[] { "ga:pageviews" }, new DateTime(2014, 01, 01), DateTime.Now);
                }

                int stepTakeAnother = 0;
                int step2 = 0;
                int step3 = 0;
                int step1UsingGA = 0, step2UsingGA = 0;

                string signUpNiceURL = uQuery.GetNodesByType("SignUp").FirstOrDefault().NiceUrl;
                string enterNiceURL = uQuery.GetNodesByType("PledgeSteps").FirstOrDefault().NiceUrl;
                string takePhotoNiceURL = uQuery.GetNodesByType("PledgeStep2").FirstOrDefault().NiceUrl;

                int signupCount = 0;

                if (analyticDataPoint.Rows.Count > 0)
                {
                    foreach (var x in analyticDataPoint.Rows)
                    {
                        if (x[0].Contains(signUpNiceURL))
                        {
                            signupCount += Convert.ToInt32(x[1]);
                        }

                        if (x[0].Contains(enterNiceURL))
                        {
                            step1UsingGA = step1UsingGA + Convert.ToInt32(x[1]);
                        }

                        if (x[0].Contains(takePhotoNiceURL))
                        {
                            step2UsingGA = Convert.ToInt32(x[1]);
                        }

                    }
                }
                if (signupCount > 0)
                {
                    if (totalCount > 0)
                    {
                        dropoffs.SignUp = Math.Round(((decimal)(signupCount - totalCount) / signupCount) * 100, 2);
                    }
                    Session.Remove("signupCount");
                }
                if (memberPledges != null)
                {
                    int pledgeTotal = memberPledges.ToList().Count;
                    //int step1 = memberPledges.Where(p => !p.GetProperty<bool>("step2Clear") && !p.GetProperty<bool>("step3Clear")).ToList().Count;

                    stepTakeAnother = memberPledges.Where(p => p.GetProperty<bool>("step1Clear") && p.GetProperty<int>("stepTakePhoto") == 1 && !p.GetProperty<bool>("step2Clear")).ToList().Count;
                    step2 = memberPledges.Where(p => p.GetProperty<bool>("step1Clear") && p.GetProperty<int>("stepTakePhoto") == 2).ToList().Count;
                    step3 = memberPledges.Where(p => p.GetProperty<bool>("step2Clear") && !p.GetProperty<bool>("step3Clear")).ToList().Count;


                    if (step1UsingGA > 0)
                    {
                        dropoffs.EnterGoal = Math.Round(((decimal)(step1UsingGA - step2UsingGA) / step1UsingGA) * 100, 2);
                    }
                    else
                    {
                        dropoffs.EnterGoal = 0;
                    }

                    if (pledgeTotal > 0)
                    {
                        dropoffs.TakePhoto = Math.Round(((decimal)stepTakeAnother / pledgeTotal) * 100, 2);
                        dropoffs.HappyWithPhoto = Math.Round(((decimal)step2 / pledgeTotal) * 100, 2);
                        dropoffs.ConfirmGoal = Math.Round(((decimal)step3 / pledgeTotal) * 100, 2);
                    }
                    else
                    {
                        dropoffs.TakePhoto = 0;
                        dropoffs.HappyWithPhoto = 0;
                        dropoffs.ConfirmGoal = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                message = "Error";
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GoalsReporting() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
            }
            var result = new { message = message, goal = goals, dropoffs = dropoffs, bucket = categoryTags };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult UsersReporting(string startDate, string endDate)
        {
            string message = "Success";
            UserModel Users = new UserModel();

            DateTime DateToCompare = DateTime.Now.AddDays(-7); //Need to compare with last 7 days login details
            DateToCompare = Convert.ToDateTime(DateToCompare.ToString("dd-MM-yyyy 00:00:00"));

            var lastLoggedUsers = Member.GetAllAsList().Where(mem => !string.IsNullOrEmpty(mem.GetProperty<string>("lastLoginDate"))).ToList();

            DateTime startFilter = DateTime.Now;
            DateTime endFilter = DateTime.Now;
            if (!string.IsNullOrEmpty(startDate))
            {
                startFilter = Convert.ToDateTime(startDate);
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                endFilter = Convert.ToDateTime(endDate + " 23:59:59");
            }

            //IMP: Removed date filters for Active users as discussed with Sumeet. The reason being we are not keeping historical data for the last login of users

            try
            {
                Entities dbEntity = new Entities();

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    Users.TotalUsers = Member.GetAllAsList().Where(member => member.CreateDateTime > startFilter && member.CreateDateTime <= endFilter).ToList().Count();

                    Users.InvitedUsers = dbEntity.PledgeInviteDatas.Where(invited => invited.InvitedDate > startFilter && invited.InvitedDate <= endFilter).ToList().Count;

                    Users.InviteAcceptedUsers = dbEntity.PledgeInviteDatas.Where(i => i.IsUsed.Value)
                    .Where(accepted => accepted.AcceptedDate > startFilter && accepted.AcceptedDate <= endFilter).ToList().Count;

                    Users.ActiveUsers = lastLoggedUsers
                        .Where(Mem => Mem.GetProperty<DateTime>("lastLoginDate") > DateToCompare).ToList().Count();
                }
                else
                {
                    // no start date and END date filter

                    Users.TotalUsers = Member.GetAllAsList().ToList().Count;

                    Users.InvitedUsers = dbEntity.PledgeInviteDatas.ToList().Count;
                    Users.InviteAcceptedUsers = dbEntity.PledgeInviteDatas.Where(i => i.IsUsed.Value).ToList().Count;
                    Users.ActiveUsers = lastLoggedUsers
                        .Where(Mem => Mem.GetProperty<DateTime>("lastLoginDate") > DateToCompare).ToList().Count();

                }
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                                     .SelectMany(x => x.ValidationErrors)
                                    .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS:UsersReporting Method failed - " + exceptionMessage);

            }
            catch (Exception ex)
            {
                message = "Error";
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : UsersReporting() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
            }
            var result = new { message = message, Users = Users };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DrawChart(string src, string startDate, string endDate)
        {

            try
            {
                int openGoalsCount = 0; int closedGoalsCount = 0;
                decimal openGoalpercent = 0; decimal closedGoalpercent = 0;
                int pieChartWidth = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["pieChartWidth"]) ? Convert.ToInt32(ConfigurationManager.AppSettings["pieChartWidth"]) : 0;
                int pieChartHeight = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["pieChartHeight"]) ? Convert.ToInt32(ConfigurationManager.AppSettings["pieChartHeight"]) : 0;
                int GoalpieChartWidth = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["GoalpieChartWidth"]) ? Convert.ToInt32(ConfigurationManager.AppSettings["GoalpieChartWidth"]) : 0;


                DateTime startFilter = DateTime.Now;
                DateTime endFilter = DateTime.Now;
                if (!string.IsNullOrEmpty(startDate))
                {
                    startFilter = Convert.ToDateTime(startDate);
                }
                if (!string.IsNullOrEmpty(endDate))
                {
                    endFilter = Convert.ToDateTime(endDate + " 23:59:59");
                }

                if (!string.IsNullOrEmpty(src))
                {
                    Chart chart = new Chart();

                    chart.Legends.Add(new Legend()
                    {
                        Alignment = StringAlignment.Center,
                        Docking = Docking.Right,
                        LegendStyle = LegendStyle.Column,
                        Font = new Font("Microsoft Sans Serif", 8)
                    });
                    chart.ChartAreas.Add(new ChartArea());

                    chart.Width = pieChartWidth;
                    chart.Height = pieChartHeight;

                    var pledges = uQuery.GetNodesByType("Pledge");
                    decimal TotalGoals = 0;
                    decimal PledgesCount;
                    decimal PledgesCountPercentage;

                    Dictionary<string, string> dicCategoryGoalsPercentage = new Dictionary<string, string>();
                    Dictionary<string, string> dicOpenClosedGoalsPercentage = new Dictionary<string, string>();

                    if (src.Equals("Goals", StringComparison.OrdinalIgnoreCase))
                    {
                        chart.Series.Add(new Series("Data"));
                        chart.Series["Data"].ChartType = SeriesChartType.Pie;
                        chart.Series["Data"]["PieLabelStyle"] = "Disabled";

                        DataPoint Charttag;

                        if (pledges != null)
                        {
                            var pledgeNodeResult = from L1 in pledges
                                                   where (L1.ChildrenAsList.Exists(a => a.GetProperty<bool>("isOwner") && a.GetProperty<bool>("step3Clear")))
                                                   select new { CreatedDate = L1.CreateDate, Value = L1.GetProperty<string>("title"), Id = L1.Id, IsPublic = L1.GetProperty<bool>("publicPledgeSelection"), Tag = L1.GetProperty<string>("categoryTag") };

                            if (pledgeNodeResult != null)
                            {
                                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                                {
                                    pledgeNodeResult = pledgeNodeResult.Where(pledge => pledge.CreatedDate > startFilter && pledge.CreatedDate <= endFilter).ToList();
                                }

                                TotalGoals = pledgeNodeResult.ToList().Count;
                                var keyWordsNode = uQuery.GetNodesByType("Keyword");

                                if (TotalGoals > 0)
                                {
                                    if (keyWordsNode != null)
                                    {
                                        foreach (var keyword in keyWordsNode)
                                        {
                                            PledgesCount = pledgeNodeResult.Where(p => p.Tag.ToLower().IndexOf(keyword.Name.ToLower()) > -1).ToList().Count;
                                            PledgesCountPercentage = Math.Round(((decimal)pledgeNodeResult.Where(p => p.Tag.ToLower().IndexOf(keyword.Name.ToLower()) > -1).ToList().Count / TotalGoals) * 100, 2);

                                            Charttag = chart.Series["Data"].Points.Add(new double[] { pledgeNodeResult.Where(p => p.Tag.ToLower().IndexOf(keyword.Name.ToLower()) > -1).ToList().Count });
                                            Charttag.AxisLabel = keyword.Name + " (" + PledgesCountPercentage + "% of pledges)";

                                            dicCategoryGoalsPercentage.Add(keyword.Name, PledgesCountPercentage + "% of pledges");
                                        }
                                    }

                                    Session["CategoryGoalsPercentage"] = dicCategoryGoalsPercentage;
                                }
                                else
                                {
                                    dicCategoryGoalsPercentage.Add("No Data Found", "");
                                    Session["CategoryGoalsPercentage"] = dicCategoryGoalsPercentage;
                                    Charttag = chart.Series["Data"].Points.Add(new double[] { 0 });
                                    Charttag.AxisLabel = "No Data Found";
                                    chart.Width = 300;
                                    chart.Height = 300;
                                }
                            }
                        }
                        chart.Series["Data"].MarkerStyle = MarkerStyle.None;

                        chart.Series["Data"].IsValueShownAsLabel = false;

                        MemoryStream ms = new MemoryStream();
                        chart.SaveImage(ms, ChartImageFormat.Png);
                        return File(ms.ToArray(), "image/bytes");
                    }
                    else
                    {
                        Chart goalChart = new Chart();
                        var pledgeNodeResult = from L1 in pledges
                                               where (L1.ChildrenAsList.Exists(a => a.GetProperty<bool>("isOwner") && a.GetProperty<bool>("step3Clear")))
                                               select new { CreatedDate = L1.CreateDate, Value = L1.GetProperty<string>("title"), Id = L1.Id, IsPublic = L1.GetProperty<bool>("publicPledgeSelection"), Tag = L1.GetProperty<string>("categoryTag") };

                        if (pledgeNodeResult != null)
                        {
                            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                            {
                                pledgeNodeResult = pledgeNodeResult.Where(pledge => pledge.CreatedDate > startFilter && pledge.CreatedDate <= endFilter).ToList();
                            }

                            TotalGoals = pledgeNodeResult.ToList().Count;
                            openGoalsCount = pledgeNodeResult.Where(pledge => pledge.IsPublic).ToList().Count;
                            closedGoalsCount = pledgeNodeResult.Where(pledge => !pledge.IsPublic).ToList().Count;

                            if (TotalGoals > 0)
                            {
                                openGoalpercent = Math.Round((decimal)openGoalsCount * 100 / TotalGoals, 2);
                                dicOpenClosedGoalsPercentage.Add("Open Goals:", openGoalpercent + "% of pledges");

                                closedGoalpercent = Math.Round((decimal)closedGoalsCount * 100 / TotalGoals, 2);
                                dicOpenClosedGoalsPercentage.Add("Closed Goals:", closedGoalpercent + "% of pledges");
                            }
                            else
                            {
                                openGoalpercent = 0;
                                closedGoalpercent = 0;
                                dicOpenClosedGoalsPercentage.Add("No Data Found", "");
                            }
                        }

                        goalChart.ChartAreas.Add(new ChartArea());
                        goalChart.Width = GoalpieChartWidth;
                        goalChart.Height = pieChartHeight;

                        goalChart.Legends.Add(new Legend()
                        {
                            Alignment = StringAlignment.Center,
                            Docking = Docking.Right,
                            LegendStyle = LegendStyle.Column,
                            Font = new Font("Microsoft Sans Serif", 8),

                        });

                        goalChart.Series.Add(new Series("Data"));
                        goalChart.Series["Data"].ChartType = SeriesChartType.Pie;
                        goalChart.Series["Data"]["PieLabelStyle"] = "Disabled";

                        DataPoint openGoal = goalChart.Series["Data"].Points.Add(new double[] { openGoalsCount });
                        openGoal.AxisLabel = "Open Goals (" + openGoalpercent + "% of pledges)";

                        DataPoint closeGoal = goalChart.Series["Data"].Points.Add(new double[] { closedGoalsCount });
                        closeGoal.AxisLabel = "Closed Goals (" + closedGoalpercent + "% of pledges)";

                        Session["OpenClosedGoalsPercentage"] = dicOpenClosedGoalsPercentage;

                        goalChart.Series["Data"].MarkerStyle = MarkerStyle.None;

                        goalChart.Series["Data"].IsValueShownAsLabel = false;

                        MemoryStream ms1 = new MemoryStream();
                        goalChart.SaveImage(ms1, ChartImageFormat.Png);
                        return File(ms1.ToArray(), "image/bytes");
                    }


                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : DrawChart() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);

            }
            return File(new byte[] { }, "image/bytes");

        }

        [HttpGet]
        public JsonResult VideoStat(string startDate, string endDate, string filterType)
        {
            string message = "Success";
            List<TopVideos> AllVideos = new List<TopVideos>();
            HttpWebRequest myReq = null;
            HttpWebResponse webResponse = null;
            string results = string.Empty;
            VideoModel videoStat = new VideoModel();

            List<YouTubeVideo> lstValidVideos = new List<YouTubeVideo>();

            try
            {
                //Scan for all videos in application and save in database
                //Common.SaveYoutubeVideos();

                using (Entities dbEntities = new Entities())
                {
                    lstValidVideos = (from a in dbEntities.YouTubeVideos
                                      where a.IsDeleted == false
                                      select a).ToList();
                }

                DateTime startFilter = DateTime.Now;
                DateTime endFilter = DateTime.Now;

                if (!string.IsNullOrEmpty(startDate))
                {
                    startFilter = Convert.ToDateTime(startDate);
                }
                else
                {
                    startFilter = new DateTime(2014, 01, 01);
                }
                if (!string.IsNullOrEmpty(endDate))
                {
                    endFilter = Convert.ToDateTime(endDate + " 23:59:59");
                }
                else
                {
                    endFilter = DateTime.Now;
                }
                if (endFilter > DateTime.Now)
                {
                    endFilter = DateTime.Now;
                }

                if (lstValidVideos != null || lstValidVideos.Count > 0)
                {
                    //Created date time must be less than end filter so that the video must be present on selected date reange filter
                    //Last updated date time must be greater than or equal to end date so that it is not deleted


                    //remove the videos from output which are not 
                    lstValidVideos.RemoveAll(obj => obj.CreatedDateTime >= endFilter);

                }
                if (lstValidVideos == null || lstValidVideos.Count <= 0)
                {
                    return Json("No Data Found", JsonRequestBehavior.AllowGet);
                }
                GoogleAnalyticsAPI.AnalyticDataPoint analyticDataPoint = new GoogleAnalyticsAPI.AnalyticDataPoint();

                GoogleAnalyticsAPI googleAnalyticsAPI = new RexonaAU.Helpers.GoogleAnalyticsAPI(Server.MapPath(System.Configuration.ConfigurationManager.AppSettings["GoogleReportingCertificateFilePath"]), System.Configuration.ConfigurationManager.AppSettings["GoogleReportingEmail"]);

                try
                {

                    analyticDataPoint = googleAnalyticsAPI.GetAnalyticsData(Convert.ToString(ConfigurationManager.AppSettings["GoogleViewId"]), new string[] { "ga:eventLabel", "ga:eventAction" }, new string[] { "ga:totalEvents", "ga:uniqueEvents", "ga:avgEventValue" }, startFilter, endFilter);


                    //total plays calculation updated 
                    /*   if (analyticDataPoint.Rows.Count > 0)
                       {
                           videoStat.TotalPlays = analyticDataPoint.Rows.Where(obj => obj[1].Equals("Play", StringComparison.OrdinalIgnoreCase) && obj[0].IndexOf("||") > -1).Sum(obj => Convert.ToInt32(obj[2]));


                          videoStat.UniquePlays = analyticDataPoint.Rows.Where(obj => obj[1].Equals("Play", StringComparison.OrdinalIgnoreCase) && obj[0].IndexOf("||") > -1).Sum(obj => Convert.ToInt32(obj[3]));
                       }*/

                }
                catch (Exception ex)
                {
                    LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : VideoStat() method in getting Google Anlaytics:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                }


                try
                {
                    foreach (var x in analyticDataPoint.Rows.Where(obj => obj[1].Equals("Play", StringComparison.OrdinalIgnoreCase)))
                    {
                        string VideoTitleID = x[0];
                        //Title || videoID || pageName

                        if (VideoTitleID.IndexOf("||") > -1)
                        {
                            //last || Page name
                            string pageName = VideoTitleID.Substring(VideoTitleID.LastIndexOf("||") + 2);

                            //secondlast || video Id
                            VideoTitleID = VideoTitleID.Remove(VideoTitleID.LastIndexOf("||"));
                            string videoId = VideoTitleID.Substring(VideoTitleID.LastIndexOf("||") + 2).Trim();

                            //double averageEventValue = analyticDataPoint.Rows.Where(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && (obj[1].Equals("Watch to End", StringComparison.OrdinalIgnoreCase) || obj[1].Equals("Pause", StringComparison.OrdinalIgnoreCase))).Average(obj => Convert.ToDouble(obj[4]));
                            //var NoOfStops = analyticDataPoint.Rows.Where(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && obj[1].Equals("Watch to End", StringComparison.OrdinalIgnoreCase)).Select(obj => obj[2]).FirstOrDefault();
                            //TimeSpan avg = TimeSpan.FromSeconds(averageEventValue);

                            double averageEventValue = 0;
                            string NoOfStops = string.Empty;
                            TimeSpan avg = new TimeSpan(0000);
                            if (analyticDataPoint.Rows.Where(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && (obj[1].Equals("Watch to End", StringComparison.OrdinalIgnoreCase) || obj[1].Equals("Pause", StringComparison.OrdinalIgnoreCase))).ToList().Count > 0)
                            {
                                if (analyticDataPoint.Rows.Exists(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && ((obj[1].Equals("Pause", StringComparison.OrdinalIgnoreCase)) || (obj[1].Equals("Watch to End", StringComparison.OrdinalIgnoreCase)))))
                                {
                                    if (analyticDataPoint.Rows.Where(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && (obj[1].Equals("Pause", StringComparison.OrdinalIgnoreCase))).ToList().Count > 0)
                                    {
                                        int pauseCount = 0, stopCount = 0;

                                        var pauseRows = analyticDataPoint.Rows.Where(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && (obj[1].Equals("Pause", StringComparison.OrdinalIgnoreCase)));
                                        pauseCount = Convert.ToInt32(pauseRows.Select(obj => obj[2]).FirstOrDefault());
                                        
                                        averageEventValue = pauseRows.Average(obj => Convert.ToDouble(obj[4])) * pauseCount;
                                        if (analyticDataPoint.Rows.Where(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && (obj[1].Equals("Watch to End", StringComparison.OrdinalIgnoreCase))).ToList().Count > 0)
                                        {

                                            var stopRows = analyticDataPoint.Rows.Where(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && (obj[1].Equals("Watch to End", StringComparison.OrdinalIgnoreCase)));
                                            stopCount = Convert.ToInt32(stopRows.Select(obj => obj[2]).FirstOrDefault());
                                            averageEventValue = (averageEventValue + (stopRows.Average(obj => Convert.ToDouble(obj[4])) * stopCount)) / (pauseCount + stopCount);
                                        }
                                        else
                                        {
                                            averageEventValue = averageEventValue / pauseCount;
                                        }
                                    }
                                    else if (analyticDataPoint.Rows.Where(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && (obj[1].Equals("Watch to End", StringComparison.OrdinalIgnoreCase))).ToList().Count > 0)
                                    {
                                        averageEventValue = analyticDataPoint.Rows.Where(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && (obj[1].Equals("Watch to End", StringComparison.OrdinalIgnoreCase))).Average(obj => Convert.ToDouble(obj[4])); 
                                    }
                                }
                              
                                
                                if (analyticDataPoint.Rows.Exists(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && obj[1].Equals("Watch to End", StringComparison.OrdinalIgnoreCase)))
                                {
                                    NoOfStops = analyticDataPoint.Rows.Where(obj => obj[0].Equals(x[0], StringComparison.OrdinalIgnoreCase) && obj[1].Equals("Watch to End", StringComparison.OrdinalIgnoreCase)).Select(obj => obj[2]).FirstOrDefault();
                                }
                                else
                                {
                                    NoOfStops = "0";
                                }
                                avg = TimeSpan.FromSeconds(averageEventValue);
                            }
                            else
                            {
                                averageEventValue = 0;
                                NoOfStops = "0";
                                avg = TimeSpan.FromSeconds(0.00);
                            }

                            AllVideos.Add(new TopVideos()
                            {
                                TotalPlays = Convert.ToInt32(x[2]),
                                TotalStops = Convert.ToInt32(NoOfStops),
                                VideoId = videoId.Trim(),
                                VideoName = pageName.Trim(),
                                AvgViews = string.Format("{0:mm\\:ss}", avg),
                                UniquePlays = Convert.ToInt32(x[3])
                            });

                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : analyticDataPoint.Rows method in getting Google Anlaytics:" + Environment.NewLine + "rows count" + analyticDataPoint.Rows.Count + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                }

                //to replace /dashboard from Google Analytics in the table data /pledges/....

                lstValidVideos.Where(obj => obj.NiceUrl.Contains("/pledge")).ToList().ForEach(obj => obj.NiceUrl = "/dashboard");

                //Remove records from mainlist which are not present in list rendered from database
                AllVideos.RemoveAll(objAllVideos => !lstValidVideos.Exists(validVideo => objAllVideos.VideoId.Trim().Equals(validVideo.YouTubeVideoId.Trim()) && objAllVideos.VideoName.ToLower().Equals(validVideo.NiceUrl)));

                if (lstValidVideos.Count > 0 && AllVideos != null)
                {
                    //Remove records from db list which are rendered from Google Analytics
                    //lstValidVideos.RemoveAll(validVideo => AllVideos.Exists(objAllVideos => objAllVideos.VideoId ==
                    //    validVideo.YouTubeVideoId));

                    //Add 0 counts and bind to UI
                    if (lstValidVideos != null && lstValidVideos.Count > 0)
                    {
                        foreach (YouTubeVideo video in lstValidVideos)
                        {
                            string Url = string.Empty;
                            if (video.NiceUrl.Contains("/pledges/"))
                            {
                                Url = "/dashboard";
                            }
                            else
                            {
                                Url = video.NiceUrl;
                            }

                            //check video from db exist in google analytics result. If exist then continue
                            //if not exist in google analytics result then add 0 count
                            TopVideos thisVideo = AllVideos.Find(obj => Url.ToLower().Contains(obj.VideoName.ToLower())
                                    && obj.VideoId.Trim().Equals(video.YouTubeVideoId.Trim()));

                            if (thisVideo != null && !string.IsNullOrEmpty(thisVideo.VideoId))
                            {
                                //Set createdDateTime                                    
                                thisVideo.CreatedDate = video.CreatedDateTime;

                                thisVideo.Likes = Convert.ToInt32(video.VideoLikes);

                                //Don't add to main output list as it is already added
                                continue;
                            }

                            //This video not found in google analytics. Add statistics as default
                            AllVideos.Add(new TopVideos()
                            {
                                TotalPlays = 0,
                                TotalStops = 0,
                                VideoId = video.YouTubeVideoId.Trim(),


                                VideoName = Url,
                                AvgViews = string.Format("{0:mm\\:ss}", new TimeSpan(0)),
                                Likes = Convert.ToInt32(video.VideoLikes),
                                //Views = views,
                                CreatedDate = video.CreatedDateTime,
                                UniquePlays = 0
                            });

                        }
                    }

                }

                if (AllVideos != null && AllVideos.Count > 0)
                {
                    videoStat.TotalPlays = AllVideos.Sum(obj => obj.TotalPlays);

                    videoStat.UniquePlays = AllVideos.Sum(obj => obj.UniquePlays);
                }

                switch (filterType)
                {
                    case "popular":
                        AllVideos = AllVideos.OrderByDescending(video => video.Likes).ToList();
                        break;
                    case "plays":
                        AllVideos = AllVideos.OrderByDescending(video => video.TotalPlays).ToList();
                        break;
                    case "recent":
                        AllVideos = AllVideos.OrderByDescending(video => video.CreatedDate).ToList();
                        break;
                }
                videoStat.TotalVideos = AllVideos.Count;

            }
            catch (Exception ex)
            {
                message = "Error";
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : VideoStat() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
            }
            var result = new { message = message, Video = videoStat, VideoList = AllVideos };
            Session["AllVideosForExport"] = AllVideos;
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult TopVideos(string startDate, string endDate, string filterType)
        {
            string message = "Success";
            List<TopVideos> topvideos = new List<TopVideos>();
            try
            {

            }
            catch (Exception ex)
            {
                message = "Error";
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : TopVideos() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
            }
            var result = new { message = message, videoList = topvideos };
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
