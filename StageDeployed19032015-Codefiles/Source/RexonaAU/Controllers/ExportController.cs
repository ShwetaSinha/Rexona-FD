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
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;


namespace RexonaAU.Controllers
{
    public class ExportController : Umbraco.Web.Mvc.SurfaceController
    {
        //
        // GET: /Export/

        public ActionResult Index()
        {
            return View();
        }

                
        public ActionResult ContentReporting(string contentTabName, string startDate, string endDate)
        {
            try
            {
                if (string.IsNullOrEmpty(contentTabName))
                {
                    return null;
                }
                else if (contentTabName.Equals("Content", StringComparison.OrdinalIgnoreCase))
                {
                    return ExportContentTab(startDate, endDate);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : ContentReporting() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return null;
            }

        }

        public ActionResult UsersReporting(string startDate, string endDate, string TotalUsers, string ActiveUsers, string InvitesSent, string InvitesAccepted)
        {
            try
            {
                return ExportUsersTab(startDate, endDate, TotalUsers, ActiveUsers, InvitesSent, InvitesAccepted);
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : UsersReporting() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return null;
            }
        }

        public ActionResult VideosReporting(string totalPlays, string uniquePlays, string totalVideos, string searchText)
        {
            try
            {
                return ExportVideoTab(totalPlays, uniquePlays, totalVideos, searchText);
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : VideosReporting() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return null;
            }
        }

        public ActionResult GoalsReporting(string startDate, string endDate, string TotalGoalsCreated, string UniqueGoalsCreated, string EventsCreated, string DiscussionsCreated, string DropOff, string MembersJoined)
        {
            try
            {
                return ExportGoalsTab(startDate, endDate, TotalGoalsCreated, UniqueGoalsCreated, EventsCreated, DiscussionsCreated, DropOff, MembersJoined);
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GoalsReporting() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return null;
            }
        }

        //Content Tab
        private FileContentResult ExportContentTab(string startDate, string endDate)
        {
            //create 4 lists
            List<TopArticles> topArticles = new List<TopArticles>();
            ContentModel reportData = new ContentModel();
            List<SocialContent> topTweets = new List<SocialContent>();
            List<SocialContent> topInstaGram = new List<SocialContent>();
            var csv = new StringBuilder();
            string HeaderCol = "IWillDo Reporting - Content Tab";
            csv.Append(string.Format(HeaderCol + "\r\n" + "\r\n"));

            string NextLine = "Articles:";
            csv.Append(string.Format(NextLine + "\r\n" + "Type\tNumber Of Articles\tPage Views" + "\r\n"));

            //Set default start date to SQl min dates for SQL server 2008 and above. Set max date to current date.
            DateTime startFilter = Convert.ToDateTime("01/01/1754"), endFilter = DateTime.Now;

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
                Node artilceNode = uQuery.GetNodesByType("Empowerment").FirstOrDefault();

                if (artilceNode != null && artilceNode.Id > 0)
                {
                    reportData.AISArticles = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("AIS", StringComparison.OrdinalIgnoreCase)
                        && article.CreateDate > startFilter && article.CreateDate <= endFilter).ToList().Count;


                    reportData.AmbassadorArticles = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("Ambassador", StringComparison.OrdinalIgnoreCase)
                         && article.CreateDate > startFilter && article.CreateDate <= endFilter).ToList().Count;

                    reportData.DoMoreArticles = artilceNode.ChildrenAsList.Where(article => article.GetProperty<string>("articleType").Equals("DoMore team", StringComparison.OrdinalIgnoreCase)
                          && article.CreateDate > startFilter && article.CreateDate <= endFilter).ToList().Count;

                    #region Google Analytics for top articles

                    //Please note the launch date og Google API is 01-01-2005, so set this date if start date is less than Google API launch date
                    DateTime GoogleAPILaunchDate = new DateTime(2005, 01, 02, 0, 0, 0, DateTimeKind.Utc);
                    if (startFilter < GoogleAPILaunchDate)
                    {
                        startFilter = GoogleAPILaunchDate;
                    }

                    //referance http://stackoverflow.com/questions/10306872/use-google-analytics-api-to-show-information-in-c-sharp
                    GoogleAnalyticsAPI.AnalyticDataPoint analyticDataPoint = new GoogleAnalyticsAPI.AnalyticDataPoint();

                    GoogleAnalyticsAPI googleAnalyticsAPI = new RexonaAU.Helpers.GoogleAnalyticsAPI(Server.MapPath(System.Configuration.ConfigurationManager.AppSettings["GoogleReportingCertificateFilePath"]), System.Configuration.ConfigurationManager.AppSettings["GoogleReportingEmail"]);

                    //TODO: Set the parameter
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

                    NextLine = "AIS Articles:\t" + reportData.AISArticles + "\t" + reportData.AISArticleViews;
                    csv.Append(string.Format(NextLine + "\r\n"));

                    NextLine = "Ambassador Articles:\t" + reportData.AmbassadorArticles + "\t" + reportData.AmbassadorArticleViews;
                    csv.Append(string.Format(NextLine + "\r\n"));

                    NextLine = "Team Do:More Articles:\t" + reportData.DoMoreArticles + "\t" + reportData.DoMoreArticleViews;
                    csv.Append(string.Format(NextLine + "\r\n"));


                    csv.Append(string.Format("\r\n" + "Top Articles:" + "\r\n"));
                    if (topArticles.Count > 0)
                    {
                        int i = 0;
                        foreach (var article in topArticles)
                        {
                            i++;
                            NextLine = i + ". " + article.ArticleName + "\t";
                            NextLine = NextLine + (article.TotalViews == 1 ? article.TotalViews + " view" : article.TotalViews + " views");
                            csv.Append(string.Format(NextLine + "\r\n"));
                        }
                    }
                    else
                    {
                        csv.Append("No Records Found." + "\r\n");
                    }

                    #endregion

                }

                NextLine = "\r\n" + "Social (posts with #IWillDo):";
                csv.Append(string.Format(NextLine + "\r\n"));

                Entities dbEntity = new Entities();
                int awaitingApproval = 0;
                int approvedPosts = 0;
                int rejectedPosts = 0;
                    reportData.InstaPosts = dbEntity.InstaGramHashTags.Where(insta => insta.CreatedDate.Value > startFilter && insta.CreatedDate.Value <= endFilter)
                        .ToList().Count();

                    NextLine = "Instagram Posts:\t" + reportData.InstaPosts;
                    csv.Append(NextLine + "\r\n");

                    reportData.Tweets = dbEntity.TwitterHashTags.Where(tweet => tweet.CreatedDate.Value > startFilter && tweet.CreatedDate.Value <= endFilter)
                        .ToList().Count();

                    NextLine = "Twitter Posts:\t" + reportData.Tweets;
                    csv.Append(NextLine + "\r\n");

                    reportData.SocialPosts = reportData.InstaPosts + reportData.Tweets;
                    NextLine = "Total Posts:\t" + reportData.SocialPosts;
                    csv.Append(NextLine + "\r\n");

                    awaitingApproval = dbEntity.InstaGramHashTags.Where(insta => insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Pending
                     && insta.CreatedDate.Value > startFilter && insta.CreatedDate.Value <= endFilter).ToList().Count();

                    reportData.AwaitingApproval = awaitingApproval + dbEntity.TwitterHashTags.Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Pending
                     && tweet.CreatedDate.Value > startFilter && tweet.CreatedDate.Value <= endFilter).ToList().Count();

                    NextLine = "Awaiting Moderation:\t" + reportData.AwaitingApproval;
                    csv.Append(NextLine + "\r\n");

                    approvedPosts = dbEntity.InstaGramHashTags.Where(insta => insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved
                       && insta.UpdatedDate.Value > startFilter && insta.UpdatedDate.Value <= endFilter).ToList().Count();

                    reportData.ApprovedPosts = approvedPosts + dbEntity.TwitterHashTags.Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved
                     && tweet.UpdatedDate.Value > startFilter && tweet.UpdatedDate.Value <= endFilter).ToList().Count();

                    rejectedPosts = dbEntity.InstaGramHashTags.Where(insta => insta.IsApproved == (int)ManageSocialContent.SocialContentStatus.Rejected
                     && insta.UpdatedDate.Value > startFilter && insta.UpdatedDate.Value <= endFilter).ToList().Count();

                    reportData.RejectedPosts = rejectedPosts + dbEntity.TwitterHashTags.Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Rejected
                     && tweet.UpdatedDate.Value > startFilter && tweet.UpdatedDate.Value <= endFilter).ToList().Count();

                    NextLine = "Rejected Posts:\t" + reportData.RejectedPosts;
                    csv.Append(NextLine + "\r\n");
                
                    NextLine = "Approved Posts:\t" + reportData.ApprovedPosts;
                    csv.Append(NextLine + "\r\n");

                    topTweets = dbEntity.TwitterHashTags.ToList().Where(tweets => tweets.IsApproved == (int)ManageSocialContent.SocialContentStatus.Approved
                      && tweets.CreatedDate.Value > startFilter && tweets.CreatedDate.Value <= endFilter).Select(tweets => new SocialContent()
                      {
                          Author = tweets.ScreeName,
                          DateToDisplay = (tweets.CreatedDate.HasValue ? tweets.CreatedDate.Value.ToString("h:mmtt dd/MM/yyyy") : DateTime.MinValue.ToString("h:mmtt dd/MM/yyyy")),
                          Likes = tweets.Likes.HasValue ? tweets.Likes.Value : 0,
                          MediaURL = tweets.MediaUrl, //TO DO ?item.GetProperty<string>("mediaURL"):string.Empty ;
                          TweetText = tweets.Post.Trim(),
                          Id = tweets.Id,
                          TweetURL = tweets.PostUrl
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

                    csv.Append("\r\n" + "Top Instagram Posts:" + "\r\n");
                    if (topInstaGram.Count > 0)
                    {
                        csv.Append("Title of Post\tPosted By\tPosted Date\tNumber Of Likes\tActual Post Link" + "\r\n");
                        foreach(var instagram in topInstaGram)
                        {
                            string InstagramText = Regex.Replace(instagram.TweetText, @"\t|\n|\r", "");
                            csv.Append(String.Format("{0}\t{1}\t{2}\t{3}\t{4}\r\n",Server.HtmlDecode(InstagramText), instagram.Author, instagram.DateToDisplay, instagram.Likes, instagram.TweetURL));
                        }

                    }
                    else
                    {
                        csv.Append("No Records Found." + "\r\n");
                    }

                    csv.Append("\r\n" + "Top Tweets:" + "\r\n");
                    if (topTweets.Count > 0)
                    {
                        csv.Append("Tweet Text\tPosted By\tPosted Date\tNumber Of Likes\tActual Post Link" + "\r\n");
                        foreach (var tweet in topTweets)
                        {
                            string TweetText = Regex.Replace(tweet.TweetText, @"\t|\n|\r", "");
                            csv.Append(String.Format("{0}\t{1}\t{2}\t{3}\t{4}\r\n", Server.HtmlDecode(TweetText), tweet.Author, tweet.DateToDisplay, tweet.Likes, tweet.TweetURL));
                        }

                    }
                    else
                    {
                        csv.Append("No Records Found." + "\r\n");
                    }

                
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                                    .SelectMany(x => x.ValidationErrors)
                                   .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS: ExportContentReporting() Twitter Push Content - " + exceptionMessage);

            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : ExportContentReporting() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);

            }


            byte[] data = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(csv.ToString())).ToArray();
            return File(data, "application/octet-stream", DateTime.Now.ToString("dd_MMM_yyyy-HH_mm_") + "ContentTabReport.csv");
        }

        //Goals Tab
        private FileContentResult ExportGoalsTab(string startDate, string endDate, string TotalGoalsCreated, string UniqueGoalsCreated, string EventsCreated, string DiscussionsCreated, string DropOff, string MembersJoined)
        {
            
            var csv = new StringBuilder();
            string HeaderCol = "IWillDo Reporting - Goals Tab";
            csv.Append(HeaderCol + "\r\n" + "\r\n");

            csv.Append("Goals:" + "\r\n");

            var newLine = string.Format("{0}\t{1}{2}", "Total Goals Created: ", TotalGoalsCreated, "\r\n");
            csv.Append(newLine);

            newLine = string.Format("{0}\t{1}{2}", "Total Goals Joined: ", MembersJoined, "\r\n");
            csv.Append(newLine);

            newLine = string.Format("{0}\t{1}{2}", "Unique Goals Created:", UniqueGoalsCreated, "\r\n");
            csv.Append(newLine);

            newLine = string.Format("{0}\t{1}{2}", "Events Created:", EventsCreated, "\r\n");
            csv.Append(newLine);

            newLine = string.Format("{0}\t{1}{2}", "Discussions Created:", DiscussionsCreated, "\r\n");
            csv.Append(newLine);

            csv.Append("\r\nBuckets:\r\n");
            if (Session["CategoryGoalsPercentage"] != null)
            {
                Dictionary<string, string> dicCategoryGoalsPercentage = Session["CategoryGoalsPercentage"] as Dictionary<string, string>;
                foreach(var item in dicCategoryGoalsPercentage)
                {
                    newLine = string.Format("{0}\t{1}{2}", item.Key + ":", item.Value, "\r\n");
                    csv.Append(newLine);
                }
            }

            csv.Append("\r\nOpen/Closed:\r\n");
            if (Session["OpenClosedGoalsPercentage"] != null)
            {
                Dictionary<string, string> dicOpenClosedGoalsPercentage = Session["OpenClosedGoalsPercentage"] as Dictionary<string, string>;
                foreach (var item in dicOpenClosedGoalsPercentage)
                {
                    newLine = string.Format("{0}\t{1}{2}", item.Key, item.Value, "\r\n");
                    csv.Append(newLine);
                }
            }

            csv.Append("\r\nSign Up Dropoffs:\r\n");
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic jsonObject = serializer.Deserialize<dynamic>(DropOff);
            foreach (var keyValue in jsonObject)
            {
                newLine = string.Format("{0}\t{1}{2}", keyValue.Key + ":", keyValue.Value, "\r\n");
                csv.Append(newLine);
            }
            
            
            byte[] data = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(csv.ToString())).ToArray();
            return File(data, "application/octet-stream", DateTime.Now.ToString("dd_MMM_yyyy-HH_mm_") + "GoalsTabReport.csv");
        }

        //Users Tab
        private FileContentResult ExportUsersTab(string startDate, string endDate, string TotalUsers, string ActiveUsers, string InvitesSent, string InvitesAccepted)
        {
            var csv = new StringBuilder();
            string HeaderCol = "IWillDo Reporting - Users Tab";
            csv.Append(string.Format(HeaderCol + "\r\n" + "\r\n"));

            var newLine = string.Format("{0}\t{1}{2}", "Total Users: ", TotalUsers, "\r\n");
            csv.Append(newLine);

            newLine = string.Format("{0}\t{1}{2}", "Active Users:", ActiveUsers, "\r\n");
            csv.Append(newLine);

            newLine = string.Format("{0}\t{1}{2}", "Invites Sent:", InvitesSent, "\r\n");
            csv.Append(newLine);

            newLine = string.Format("{0}\t{1}{2}", "Invites Accepted:", InvitesAccepted, "\r\n");
            csv.Append(newLine);

            byte[] data = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(csv.ToString())).ToArray();
            return File(data, "application/octet-stream", DateTime.Now.ToString("dd_MMM_yyyy-HH_mm_") + "UsersTabReport.csv");
        }

        //Videos tab
        private FileContentResult ExportVideoTab(string totalPlays, string uniquePlays, string totalVideos, string searchText)
        {
            var csv = new StringBuilder();
            string HeaderCol = "IWillDo Reporting - Videos Tab";
            csv.Append(string.Format(HeaderCol + "\r\n" + "\r\n"));
                        
            csv.Append("Videos Summary:\r\n");
            csv.Append("Total Plays:\t" + totalPlays + "\r\n");
            csv.Append("Unique Plays:\t" + uniquePlays + "\r\n");
            csv.Append("Total Videos:\t" + totalVideos + "\r\n");

            csv.Append("\r\nAll Videos:\r\n");

            List<TopVideos> AllVideos = new List<TopVideos>();
            if (Session["AllVideosForExport"] != null)
            {
                AllVideos = Session["AllVideosForExport"] as List<TopVideos>;
                if (AllVideos != null && AllVideos.Count > 0)
                {
                    if (searchText.Equals("plays", StringComparison.OrdinalIgnoreCase))
                    {
                        AllVideos = AllVideos.OrderByDescending(a => a.TotalPlays).ToList();
                    }
                    else if (searchText.Equals("popular", StringComparison.OrdinalIgnoreCase))
                    {
                        AllVideos = AllVideos.OrderByDescending(a => a.Likes).ToList();
                    }
                    else
                    {
                        AllVideos = AllVideos.OrderByDescending(a => a.CreatedDate).ToList();
                    }
                    csv.Append("Page\tVideo URL\tPlays\tStops\tAvg. View\tLikes\tDate on which Video was Added To Site\r\n");
                    string pageUrl = string.Empty;
                    foreach (var video in AllVideos)
                    {
                        string createdDate = video.CreatedDate.ToString();
                        pageUrl = Request.Url.Scheme + "://" + Request.Url.Host + video.VideoName;
                        csv.Append(pageUrl + "\t" + "https://www.youtube.com/watch?v=" + video.VideoId + "\t" + video.TotalPlays + "\t" + video.TotalStops + "\t" + video.AvgViews + "\t" + video.Likes + "\t" + createdDate + "\r\n");
                    }
                }
                else
                {
                    csv.Append("\r\nNo Videos Found.\r\n");
                }
            }

            byte[] data = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(csv.ToString())).ToArray();
            return File(data, "application/octet-stream", DateTime.Now.ToString("dd_MMM_yyyy-HH_mm_") + "VideosTabReport.csv");
        }

    }
}
