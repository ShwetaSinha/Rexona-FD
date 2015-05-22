using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LinqToTwitter;
using RexonaAU.Models;
using System.Configuration;
using Umbraco.Core.Services;
using umbraco;
using umbraco.NodeFactory;
using Umbraco.Core.Logging;
using System.IO;
using RexonaAU.Helpers;
using System.Globalization;

namespace RexonaAU.Controllers
{
    public class SocialContentController : Umbraco.Web.Mvc.SurfaceController
    {
        //
        // GET: /SocialContent/

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetTweets()
        {
            Entities dbEntities = new Entities();
            try
            {

                var objTweet = dbEntities.TwitterHashTags
                    .Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Pending)
                    .Select(tweets =>
                            new
                            {
                                Id = tweets.Id,
                                UniqueId = tweets.UniqueId,
                                Username = tweets.UserName,
                                Post = tweets.Post,
                                PostUrl = tweets.PostUrl,
                                MediaUrl = tweets.MediaUrl,
                                CreatedDate = tweets.CreatedDate.Value.ToString(),
                                Likes = tweets.Likes,
                                Location = tweets.Location,
                                ScreeName = tweets.ScreeName,
                                CreatedDateTime = tweets.CreatedDate
                            })
                    .OrderByDescending(tweet => tweet.CreatedDateTime).ToList();

                return Json(objTweet, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetTweets() failed- " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }

        [HttpGet]
        public JsonResult GetInstagram()
        {
            Entities dbEntities = new Entities();
            try
            {

                var objInsta = dbEntities.InstaGramHashTags
                    .Where(tweet => tweet.IsApproved == (int)ManageSocialContent.SocialContentStatus.Pending)
                    .Select(tweets =>
                            new
                            {
                                Id = tweets.Id,
                                UniqueId = tweets.UniqueId,
                                Username = tweets.UserName,
                                Post = tweets.Post,
                                PostUrl = tweets.PostUrl,
                                MediaUrl = tweets.MediaUrl,
                                CreatedDate = tweets.CreatedDate.Value.ToString(),
                                Likes = tweets.Likes,
                                Location = tweets.Location,
                                ScreeName = tweets.ScreeName,
                                CreatedDateTime = tweets.CreatedDate
                            })
                    .OrderByDescending(tweet => tweet.CreatedDateTime).ToList();


                return Json(objInsta, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetInstagram() failed - " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }

        [HttpPost]
        public JsonResult RejectEntry(int entryId)
        {
            Entities dbEntities = new Entities();
            var tweets = dbEntities.TwitterHashTags.Where(tweet => tweet.Id == entryId).FirstOrDefault();
            try
            {
                if (tweets != null)
                {
                    tweets.IsApproved = (int)ManageSocialContent.SocialContentStatus.Rejected;
                    tweets.UpdatedDate = DateTime.Now;
                }
                dbEntities.SaveChanges();
                return Json(true);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : RejectEntry() method failed - " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(false);
            }

        }

        [HttpPost]
        public JsonResult ApproveEntry(int entryId)
        {
            Entities dbEntities = new Entities();
            var tweets = dbEntities.TwitterHashTags.Where(tweet => tweet.Id == entryId).FirstOrDefault();
            try
            {
                if (tweets != null)
                {
                    tweets.IsApproved = (int)ManageSocialContent.SocialContentStatus.Approved;
                    tweets.UpdatedDate = DateTime.Now;
                }
                dbEntities.SaveChanges();
                return Json(true);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : ApproveEntry - " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(false);
            }

        }

        [HttpPost]
        public JsonResult ApproveInstaGramEntry(int entryId)
        {
            Entities dbEntities = new Entities();
            var tweets = dbEntities.InstaGramHashTags.Where(tweet => tweet.Id == entryId).FirstOrDefault();
            try
            {
                if (tweets != null)
                {
                    tweets.IsApproved = (int)ManageSocialContent.SocialContentStatus.Approved;
                    tweets.UpdatedDate = DateTime.Now;
                }
                dbEntities.SaveChanges();
                return Json(true);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : ApproveInstaGramEntry() - " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(false);
            }

        }

        [HttpPost]
        public JsonResult RejectInstaEntry(int entryId)
        {
            Entities dbEntities = new Entities();
            var tweets = dbEntities.InstaGramHashTags.Where(tweet => tweet.Id == entryId).FirstOrDefault();
            try
            {
                if (tweets != null)
                {
                    tweets.IsApproved = (int)ManageSocialContent.SocialContentStatus.Rejected;
                    tweets.UpdatedDate = DateTime.Now;
                }
                dbEntities.SaveChanges();
                return Json(true);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : RejectInstaEntry() Failed - " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(false);
            }

        }

        [HttpPost]
        public JsonResult RejectApprovedContent(int entryId, string source)
        {
            Entities dbEntities = new Entities();
            try
            {
                if (source.Equals("tweet", StringComparison.OrdinalIgnoreCase))
                {
                    var tweets = dbEntities.TwitterHashTags.Where(tweet => tweet.Id == entryId).FirstOrDefault();

                    if (tweets != null)
                    {
                        tweets.IsApproved = (int)ManageSocialContent.SocialContentStatus.Rejected;
                        tweets.UpdatedDate = DateTime.Now;
                    }
                    dbEntities.SaveChanges();
                }
                else if (source.Equals("instagram", StringComparison.OrdinalIgnoreCase))
                {
                    var tweets = dbEntities.InstaGramHashTags.Where(tweet => tweet.Id == entryId).FirstOrDefault();

                    if (tweets != null)
                    {
                        tweets.IsApproved = (int)ManageSocialContent.SocialContentStatus.Rejected;
                        tweets.UpdatedDate = DateTime.Now;
                    }
                    dbEntities.SaveChanges();
                }
                return Json(true);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : RejectApprovedContent() failed - " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(false);
            }
        }
    }
}

//#region commented methods
// [HttpGet]
//        public JsonResult GetTweets()
//        {
//            string _consumerKey = ConfigurationManager.AppSettings["consumerKey"];
//            string _consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
//            string _accessToken = ConfigurationManager.AppSettings["accessToken"];
//            string _accessTokenSecret = ConfigurationManager.AppSettings["accessTokenSecret"];
//            string HashTag = ConfigurationManager.AppSettings["hashTag"];

//            var auth = new SingleUserAuthorizer
//            {
//                CredentialStore = new SingleUserInMemoryCredentialStore
//                {
//                    ConsumerKey = _consumerKey,
//                    ConsumerSecret = _consumerSecret,
//                    AccessToken = _accessToken,
//                    AccessTokenSecret = _accessTokenSecret
//                }
//            };



//            using (var db = new TwitterContext(auth))
//            {

//                var twitterCtx = new TwitterContext(auth);

//                var searchResponse =
//                  (from search in twitterCtx.Search
//                   where search.Type == SearchType.Search &&
//                         search.Query == HttpUtility.HtmlEncode(HashTag) &&
//                         search.Count == int.MaxValue
//                   select search)
//                  .SingleOrDefault();



//                List<SocialContent> tweets = new List<SocialContent>();
//                if (searchResponse != null && searchResponse.Statuses != null)
//                {
//                    foreach (var tweetDetail in searchResponse.Statuses)
//                    {
//                        SocialContent tweet = new SocialContent();
//                        tweet.Id = Convert.ToInt64(tweetDetail.StatusID);
//                        tweet.Author = string.IsNullOrEmpty(tweetDetail.ScreenName) ? tweetDetail.User.Name : tweetDetail.ScreenName;
//                        tweet.TweetText = tweetDetail.Text;
//                        tweet.ProfilePic = tweetDetail.User.ProfileImageUrl;
//                        tweet.ScreenNameResponse = "@" + tweetDetail.User.ScreenNameResponse;
//                        tweet.CreatedAt = tweetDetail.CreatedAt.ToLocalTime().ToString("dd-MMM-yyy HH:mm");
//                        tweet.FriendCount = tweetDetail.User.FriendsCount;
//                        tweet.TweetURL = "https://twitter.com/" + tweetDetail.User.ScreenNameResponse + "/status/" + tweetDetail.StatusID;
//                        tweet.Likes = tweetDetail.FavoriteCount.Value;

//                        if (tweetDetail.Entities.MediaEntities.Count > 0)
//                        {
//                            tweet.MediaURL = tweetDetail.Entities.MediaEntities[0].MediaUrl;
//                        }
//                        tweet.ProfileURL = "https://twitter.com/" + tweetDetail.User.ScreenNameResponse;
//                        tweets.Add(tweet);
//                    }

//                }

//                #region filter approved and rejected Tweets

//                var approvedentriesList = uQuery.GetNodesByType("HashTagEntries").FirstOrDefault().GetChildNodes().ToList().Where(a => a.GetProperty<bool>("isTwitter") == true).ToList();
//                //Node ParentId = new Node(approvedContentId);
//                //var approvedentriesList = ParentId.GetChildNodes().ToList();
//                var approvedIds = approvedentriesList.Select(e => e.GetProperty("contentId").Value);

//                var rejectedentriesList = uQuery.GetNodesByType("HashTagEntries").LastOrDefault().GetChildNodes().ToList().Where(a => a.GetProperty<bool>("isTwitter") == true).ToList();
//                //Node rejectedParentId = new Node(rejectedContentId);
//                //var rejectedentriesList = rejectedParentId.GetChildNodes().ToList();
//                var rejectedIds = rejectedentriesList.Select(e => e.GetProperty("contentId").Value);


//                #endregion

//                var result = new
//                {
//                    rows = tweets
//                };


//                return Json(result, JsonRequestBehavior.AllowGet);
//            }
//        }
//var service = new ContentService();
//           var entry = service.GetById(entryId);
//           Node node = new Node(entryId);
//           int competitionId = node.Parent.Parent.Id;
//           try
//           {

//               int deniedEntriesNodeId = uQuery.GetNodesByType("HashTagEntries").LastOrDefault().Id;
//               service.Move(entry, deniedEntriesNodeId);
//               entry.SetValue("parentNode", deniedEntriesNodeId);
//               service.SaveAndPublish(entry);
//           }
//           catch (Exception ex)
//           {
//               LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Denied Entry Failed Entry Id - " + entryId + " " + ex.StackTrace);
//               return Json(false);
//           }
//           finally
//           {
//               umbraco.library.UpdateDocumentCache(entryId);
//               umbraco.library.UpdateDocumentCache(uQuery.GetNodesByType("HashTagEntries").LastOrDefault().Id);
//               umbraco.library.RefreshContent();
//           }

//           return Json(true);
//[HttpPost]
//    public int RejectHashTagContents(SocialContent tweet)
//    {
//        int requiredEntryId = 0;

//        try
//        {
//            var service = new ContentService();

//            var hashTagEntry = service.CreateContent(Common.ReplaceSpecialChar(tweet.TweetText), uQuery.GetNodesByType("HashTagEntries").LastOrDefault().Id, "HashTagContents");

//            hashTagEntry.SetValue("userName", tweet.ScreenNameResponse);
//            hashTagEntry.SetValue("post", tweet.TweetText);
//            hashTagEntry.SetValue("imageUrl", tweet.ProfilePic);
//            hashTagEntry.SetValue("createdDate", tweet.CreatedAt);
//            hashTagEntry.SetValue("postUrl", tweet.TweetURL);
//            hashTagEntry.SetValue("dateSaved", DateTime.Now.ToShortDateString());

//            hashTagEntry.SetValue("contentId", tweet.Id);
//            hashTagEntry.SetValue("mediaURL", tweet.MediaURL);
//            hashTagEntry.SetValue("parentNode", uQuery.GetNodesByType("HashTagEntries").LastOrDefault().Id);
//            hashTagEntry.SetValue("isApproved", 0);
//            hashTagEntry.SetValue("likeCount", tweet.Likes);
//            //source:Twitter or Instagram
//            if (Convert.ToString(tweet.source) == "twitter")
//            {
//                hashTagEntry.SetValue("isTwitter", 1);
//            }
//            else if (Convert.ToString(tweet.source) == "instagram")
//            {
//                hashTagEntry.SetValue("isInstagram", 1);
//            }

//            service.Save(hashTagEntry);

//            hashTagEntry.Name = String.Format("{0} - {1}", tweet.ScreenNameResponse, hashTagEntry.Id);

//            service.SaveAndPublish(hashTagEntry);

//            requiredEntryId = hashTagEntry.Id;
//        }
//        catch (Exception ex)
//        {
//            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : Approve Content Failed - " + tweet.ScreenNameResponse + " Publish Failed. " + ex.StackTrace);
//            return requiredEntryId;
//        }
//        finally
//        {
//            if (requiredEntryId != 0)
//                umbraco.library.UpdateDocumentCache(requiredEntryId);
//            umbraco.library.UpdateDocumentCache(uQuery.GetNodesByType("HashTagEntries").LastOrDefault().Id);
//            umbraco.library.RefreshContent();
//        }
//        return requiredEntryId;
//    }
//#endregion