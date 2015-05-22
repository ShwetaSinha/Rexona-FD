using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using umbraco.NodeFactory;
using Umbraco.Core.Logging;
using System.IO;
using LinqToTwitter;
using Umbraco.Core.Services;
using umbraco;
using RexonaAU.Models;
using System.Collections;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Data.Entity.Validation;
using System.Data.Objects;
using System.Data.Entity;
using System.Data.Objects.SqlClient;
using System.Globalization;

namespace RexonaAU.Helpers
{
    public class ManageSocialContent
    {
        //string prevResult = string.Empty;
        static string prevResult = string.Empty;

        static List<SocialContent> InstaPosts = new List<SocialContent>();

        public enum SocialContentStatus
        {
            Pending = 0,
            Approved = 1,
            Rejected = 2
        }

        public static bool GetTweets()
        {


            string _consumerKey = ConfigurationManager.AppSettings["consumerKey"];
            string _consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
            string _accessToken = ConfigurationManager.AppSettings["accessToken"];
            string _accessTokenSecret = ConfigurationManager.AppSettings["accessTokenSecret"];
            string HashTag = ConfigurationManager.AppSettings["hashTag"];
            bool result = false;

            try
            {
                var auth = new SingleUserAuthorizer
                {
                    CredentialStore = new SingleUserInMemoryCredentialStore
                    {
                        ConsumerKey = _consumerKey,
                        ConsumerSecret = _consumerSecret,
                        AccessToken = _accessToken,
                        AccessTokenSecret = _accessTokenSecret
                    }
                };


                using (var db = new TwitterContext(auth))
                {

                    var twitterCtx = new TwitterContext(auth);

                    var searchResponse =
                      (from search in twitterCtx.Search
                       where search.Type == SearchType.Search &&
                             search.Query == HttpUtility.HtmlEncode(HashTag) &&
                             search.Count == int.MaxValue
                       select search)
                      .SingleOrDefault();

                    Entities dbEntities = new Entities();

                    //Get List Of Existing Contents
                    var tweetIds = dbEntities.TwitterHashTags.ToList();
                    //

                    List<SocialContent> tweets = new List<SocialContent>();
                    if (searchResponse != null && searchResponse.Statuses != null)
                    {
                        foreach (var tweetDetail in searchResponse.Statuses)
                        {

                            if (!tweetIds.Exists(ids => ids.UniqueId == Convert.ToString(tweetDetail.StatusID)))
                            {

                                dbEntities.TwitterHashTags.Add(new TwitterHashTag()
                                {
                                    UniqueId = Convert.ToString(tweetDetail.StatusID),
                                    UserName = string.IsNullOrEmpty(tweetDetail.ScreenName) ? tweetDetail.User.Name : tweetDetail.ScreenName,
                                    Post = tweetDetail.Text,
                                    PostUrl = "https://twitter.com/" + tweetDetail.User.ScreenNameResponse + "/status/" + tweetDetail.StatusID,
                                    MediaUrl = tweetDetail.Entities.MediaEntities.Count > 0 ? tweetDetail.Entities.MediaEntities[0].MediaUrl : string.Empty,
                                    CreatedDate = tweetDetail.CreatedAt.ToLocalTime(),
                                    Likes = tweetDetail.FavoriteCount.Value,
                                    Location = tweetDetail.User.Location,
                                    IsApproved = (int)SocialContentStatus.Pending,
                                    UpdatedDate = DateTime.Now,
                                    ScreeName = "@" + tweetDetail.User.ScreenNameResponse,
                                    InsertedOn = DateTime.Now
                                });
                            }
                            else if (tweetIds.Exists(ids => ids.UniqueId == Convert.ToString(tweetDetail.StatusID)))
                            {
                                var Id = Convert.ToString(tweetDetail.StatusID);

                                var toBeUpdated = dbEntities.TwitterHashTags.Where(tweet => tweet.UniqueId == Id).FirstOrDefault();
                                if (toBeUpdated != null)
                                {
                                    toBeUpdated.Post = tweetDetail.Text;
                                    toBeUpdated.MediaUrl = tweetDetail.Entities.MediaEntities.Count > 0 ? tweetDetail.Entities.MediaEntities[0].MediaUrl : string.Empty;
                                    toBeUpdated.Likes = tweetDetail.FavoriteCount.Value;
                                }
                            }
                        } if (dbEntities.SaveChanges() > 0)
                            result = true;


                    }

                }


            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                                     .SelectMany(x => x.ValidationErrors)
                                    .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS: GetTweets() Twitter Push Content - " + exceptionMessage);
                result = false;
            }
            catch (Exception ex)
            {

                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetTweets() Fetch Twitter Content - " + Environment.NewLine
                    + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                result = false;
            }
            return result;
        }

        public static bool InstagramContent(string prevUrl, string nextUrl)
        {
            bool response = false;

            try
            {


                if (prevUrl != nextUrl)
                {
                    GetInstagramHashTags(prevUrl, nextUrl);
                }
                else
                {
                    //JavaScriptSerializer serializer = new JavaScriptSerializer();
                    //dynamic jsonObject = serializer.Deserialize<dynamic>(prevResult);
                    Entities dbEntities = new Entities();

                    //Get List Of Existing Contents
                    var instaIds = dbEntities.InstaGramHashTags.ToList();
                    //

                    foreach (var tweet in InstaPosts)
                    {
                        if (!instaIds.Exists(ids => ids.UniqueId == Convert.ToString(tweet.Id)))
                        {
                            dbEntities.InstaGramHashTags.Add(new InstaGramHashTag()
                          {
                              UniqueId = Convert.ToString(tweet.Id),
                              UserName = tweet.Author,
                              Post = tweet.TweetText,
                              PostUrl = tweet.TweetURL,
                              CreatedDate = tweet.CreatedAt,
                              MediaUrl = tweet.MediaURL,
                              Likes = tweet.Likes,
                              // Location = insta["location"] == null ? string.Empty : insta["location"],
                              IsApproved = (int)SocialContentStatus.Pending,
                              UpdatedDate = DateTime.Now,
                              ScreeName = tweet.ScreenNameResponse,
                              InsertedOn = DateTime.Now

                          });
                        }
                        else if (instaIds.Exists(ids => ids.UniqueId == Convert.ToString(tweet.Id)))
                        {
                            var Id = Convert.ToString(tweet.Id);
                            var toBeUpdated = dbEntities.InstaGramHashTags.Where(insta => insta.UniqueId == Id).FirstOrDefault();
                            if (toBeUpdated != null)
                            {
                                toBeUpdated.Post = tweet.TweetText;
                                toBeUpdated.MediaUrl = tweet.MediaURL;
                                toBeUpdated.Likes = tweet.Likes;
                            }
                        }
                    }
                    if (dbEntities.SaveChanges() > 0)
                        response = true;
                    InstaPosts.Clear();
                }
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                                     .SelectMany(x => x.ValidationErrors)
                                    .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS: InstagramContent() InstagramContent Push Instagram Content - " + exceptionMessage);
                response = false;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS: InstagramContent() InstagramContent Push Instagram Content - " + Environment.NewLine
                    + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                response = false;
            }
            return response;
        }

        public static void GetInstagramHashTags(string prevUrl, string nextUrl)
        {


            HttpWebRequest myReq = null;
            HttpWebResponse webResponse = null;

            string results = string.Empty;
            CultureInfo culture = new CultureInfo("en-AU");

            try
            {
                myReq = (HttpWebRequest)WebRequest.Create(nextUrl);
                myReq.Method = "GET";
                myReq.Timeout = (1000 * 60) * 10;
                webResponse = (HttpWebResponse)myReq.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8);
                results = sr.ReadToEnd();

                string hashtag = ConfigurationManager.AppSettings["hashTag"];
                if (!string.IsNullOrEmpty(results))
                {

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    dynamic jsonObject = serializer.Deserialize<dynamic>(results);

                    foreach (var insta in jsonObject["data"])
                    {
                        SocialContent instagram = new SocialContent();
                        if (insta["caption"] != null)
                        {
                            if (insta["caption"]["text"] == null ? string.Empty : culture.CompareInfo.IndexOf(insta["caption"]["text"],hashtag, CompareOptions.IgnoreCase) > -1)
                            {
                                instagram.Id = Int64.Parse(insta["caption"]["id"]);
                                instagram.Author = insta["user"]["full_name"].Replace("'", "''");
                                instagram.TweetText = insta["caption"]["text"];
                                instagram.TweetURL = insta["link"];
                                instagram.CreatedAt = new System.DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToDouble(insta["created_time"].ToString()));
                                instagram.MediaURL = insta["images"]["thumbnail"]["url"];
                                instagram.Likes = insta["likes"]["count"] == null ? 0 : insta["likes"]["count"];
                                // Location = insta["location"] == null ? string.Empty : insta["location"],
                                instagram.ScreenNameResponse = "@" + insta["user"]["username"];

                                InstaPosts.Add(instagram);
                            }
                        }
                    }


                    prevUrl = nextUrl;
                    var expandoObject = jsonObject["pagination"];
                    if (((IDictionary<String, object>)expandoObject).ContainsKey("next_url"))
                    {
                        nextUrl = jsonObject["pagination"]["next_url"] != null ? jsonObject["pagination"]["next_url"] : "";
                    }
                    if (nextUrl == string.Empty || nextUrl == "")
                    {
                        nextUrl = prevUrl;
                    }

                    InstagramContent(prevUrl, nextUrl);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetInstagramHashTags() Fetch Instagram Content - " + Environment.NewLine
                    + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            // return InstaPosts;
        }

        public static void AutoApproveSocialContents()
        {
            Entities dbEntities = new Entities();
            var newhrs = DateTime.Now;
            try
            {
                var tweetIds = dbEntities.TwitterHashTags
                    .Where(tweet => tweet.IsApproved == (int)SocialContentStatus.Pending && DbFunctions.AddHours(tweet.InsertedOn, 24) <= newhrs).ToList();

                var InstagramIds = dbEntities.InstaGramHashTags
                    .Where(Insta => Insta.IsApproved == (int)SocialContentStatus.Pending && DbFunctions.AddHours(Insta.InsertedOn, 24) <= newhrs).ToList();

                if (tweetIds != null)
                {
                    foreach (var tweet in tweetIds)
                    {
                        tweet.IsApproved = (int)SocialContentStatus.Approved;
                        tweet.UpdatedDate = DateTime.Now;
                    }

                }
                if (InstagramIds != null)
                {
                    foreach (var insta in InstagramIds)
                    {
                        insta.IsApproved = (int)SocialContentStatus.Approved;
                        insta.UpdatedDate = DateTime.Now;
                    }
                }
                dbEntities.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                                     .SelectMany(x => x.ValidationErrors)
                                    .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS:AutoApproveSocialContents Method failed - " + exceptionMessage);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS:AutoApproveSocialContents() Method failed - " + Environment.NewLine
                    + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
        }
    }
}