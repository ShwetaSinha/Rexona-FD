using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.Objects;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using umbraco;
using umbraco.cms.businesslogic.member;
using Umbraco.Core.Models;
using Umbraco.Web;
using System.Security.Cryptography;
using Umbraco.Core.Logging;
using System.Text;
using System.Globalization;
using System.Collections;
//using umbraco.presentation.nodeFactory;
using RexonaAU.Models;
using umbraco.NodeFactory;
using System.Net;
using Traction.Fusion;
using Traction.Fusion.Customers;
using Traction.Fusion.Configuration;
using umbraco.cms.businesslogic.media;
using Traction.Fusion.Subscriptions;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace RexonaAU.Helpers
{
    public class Common
    {
        #region Private variables
        //All data fetched from Umbraco is data cached. This string denotes prefix of cahce entries so that it does not collide with other cache entries.
        public const string CachePrefix = "CommonCache_{0}";

        //Lock object used to synchronize cache write operation in all methods.
        public static object lockObj = new object();

        //Cache duration will be read into following variable for further use.
        public static int CacheMinutes;

        private static Entities dbEntities = new Entities();
        #endregion
        public enum EmailTemplates
        {
            //CommonEmail,
            //CoachSignup,
            //PlayerSignup,
            //SpectatorSignup,
            //CompetitionStart,
            //CompetitionEndForPlayer,
            //CompetitionEndForCoach,
            ForgottenPassword,
            //SubmitReminder,
            //PlayerDesignatedAsCoach,
            //CoachInvite,
            //SpectatorInvite,
            //PlayerNotLoggedInFor10Days,
            //PlayerNotLoggedInFor30Days,
            //PlayerNotLoggedInFor60Days,
            //CompetitionOver
        }

        /// <summary>
        /// Add Entry to EmailTransaction table and email will be send when service runs next time
        /// </summary>
        /// <param name="ToAddress"></param>
        /// <param name="Subject"></param>
        /// <param name="Body"></param>
        /// <param name="appendImage"></param>
        /// <returns></returns>
        public static bool SendEmail(string ToAddress, string Subject, string Body, bool appendImage = true)
        {
            try
            {
                //add record to email transaction table 
                using (Entities dbEntities = new Entities())
                {
                    dbEntities.EmailTransactions.Add(new EmailTransaction()
                    {
                        Body = Body,
                        Subject = Subject,
                        ToEmailAddress = ToAddress,
                        RetryCount = 0,
                        IsEmailSent = false,
                        AppendCLLogo = appendImage,
                        CreatedDateTime = DateTime.Now
                    });
                    if (dbEntities.SaveChanges() > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, string.Format("Exception occured in SendEmail() while adding record to email transaction table for email {0} having subject {1}.", ToAddress, Subject), ex);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: SendEmail() Failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            return false;
        }

        /// <summary>
        /// Method to call using service. Will send all mails from EmailTransaction table
        /// </summary>
        public static void SendScheduledMails()
        {
            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, string.Format("Email sending service has started at {0}", DateTime.Now));

            using (Entities dbEntities = new Entities())
            {
                int retryCount = 0;
                int.TryParse(ConfigurationManager.AppSettings["RetryCount"], out retryCount);

                List<EmailTransaction> lstmailToBeSent = dbEntities.EmailTransactions.Where(obj => obj.RetryCount < retryCount && !obj.IsEmailSent)
                                                         .OrderBy(obj => obj.CreatedDateTime).ToList();

                if (lstmailToBeSent != null && lstmailToBeSent.Count > 0)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, string.Format("Email will be sent to {0} email addresses", lstmailToBeSent.Count));

                    foreach (EmailTransaction item in lstmailToBeSent)
                    {
                        if (!SendEmail(item.Id, item.ToEmailAddress, item.Subject, item.Body, item.AppendCLLogo))
                            item.RetryCount += 1;
                        else
                            item.IsEmailSent = true;

                        dbEntities.SaveChanges();
                    }
                }
                else

                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Email will be sent to 0 email addresses");
            }

            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, string.Format("Email sending service has ended at {0}", DateTime.Now));
        }

        private static bool SendEmail(long id, string ToAddress, string Subject, string Body, bool appendImage = true)
        {
            bool blnIsMailSent = true;

            try
            {
                using (var client = new SmtpClient())
                {
                    MailMessage newMail = new MailMessage();

                    newMail.To.Add(ToAddress);

                    newMail.Subject = Subject;
                    newMail.IsBodyHtml = true;

                    if (appendImage)
                    {
                        var inlineLogo = new LinkedResource(HttpContext.Current.Server.MapPath(
                      ConfigurationManager.AppSettings["InlineLogo"]));

                        inlineLogo.ContentId = Guid.NewGuid().ToString();

                        string body = Body.Replace("{{ImageID}}", inlineLogo.ContentId);

                        var view = AlternateView.CreateAlternateViewFromString(body, null, "text/html");
                        view.LinkedResources.Add(inlineLogo);
                        newMail.AlternateViews.Add(view);
                    }
                    else
                        newMail.Body = Body;

                    client.Send(newMail);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, string.Format("Exception occured while sending Email from console application function SendEmail() to email {0} having id {1}. ", ToAddress, id), ex);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: SendEmail() Failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                blnIsMailSent = false;
            }
            return blnIsMailSent;
        }

        public static string GetEmailTemplate(EmailTemplates TemplateId, int MemberId, string UserName)
        {
            string cacheKey = string.Format(CachePrefix, "GetEmailTemplate_" + TemplateId.ToString());
            object configObj = HttpContext.Current != null ? HttpContext.Current.Cache[cacheKey] : null;
            if (configObj == null)
            {
                lock (lockObj)
                {
                    configObj = HttpContext.Current != null ? HttpContext.Current.Cache[cacheKey] : null;
                    if (configObj == null)
                    {
                        string xmlFilePath = HttpContext.Current.Server.MapPath("~/App_Data/EmailTemplate.xml");
                        XmlCDataSection cdataSectionCommon = null, cdataSectionInner = null;
                        XmlDocument doc = new XmlDocument();
                        doc.Load(xmlFilePath);

                        XmlElement root = doc.DocumentElement;
                        XmlNode commonNode = doc.DocumentElement.SelectSingleNode(@"//EmailTemplates/EmailTemplate[@Id='CommonEmail']");
                        XmlNode childNode = commonNode.ChildNodes[0];
                        if (childNode is XmlCDataSection)
                        {
                            cdataSectionCommon = childNode as XmlCDataSection;
                        }

                        XmlNode specificNode = doc.DocumentElement.SelectSingleNode(@"//EmailTemplates/EmailTemplate[@Id='" + TemplateId.ToString() + "']");
                        XmlNode childNodeInner = specificNode.ChildNodes[0];
                        if (childNodeInner is XmlCDataSection)
                        {
                            cdataSectionInner = childNodeInner as XmlCDataSection;
                        }

                        configObj = cdataSectionCommon.InnerText.Replace("{{BodyCopy}}", cdataSectionInner.InnerText.Replace("{{UserName}}", UserName));

                        if (configObj != null && HttpContext.Current != null)
                        {
                            HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                                 System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                        }
                    }
                }
            }

            return (configObj as string);
        }

        public static string GetEmailSubject(EmailTemplates TemplateId)
        {
            string cacheKey = string.Format(CachePrefix, "GetEmailSubject_" + TemplateId.ToString());
            object configObj = HttpContext.Current != null ? HttpContext.Current.Cache[cacheKey] : null;
            if (configObj == null)
            {
                lock (lockObj)
                {
                    configObj = HttpContext.Current != null ? HttpContext.Current.Cache[cacheKey] : null;
                    if (configObj == null)
                    {
                        string xmlFilePath = HttpContext.Current.Server.MapPath("~/App_Data/EmailTemplate.xml");
                        XmlDocument doc = new XmlDocument();
                        doc.Load(xmlFilePath);

                        XmlNode specificNode = doc.DocumentElement.SelectSingleNode(@"//EmailTemplates/EmailTemplate[@Id='" + TemplateId.ToString() + "']/@Subject");
                        configObj = specificNode.Value;

                        if (configObj != null && HttpContext.Current != null)
                        {
                            HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                                 System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                        }
                    }
                }
            }

            return (configObj as string) != null ? (configObj as string) : string.Empty;
        }

        public static void SetNoBrowserCacheForpage()
        {
            try
            {
                HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                HttpContext.Current.Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
                HttpContext.Current.Response.Cache.SetNoStore();
                HttpContext.Current.Response.Cache.SetValidUntilExpires(false);
                HttpContext.Current.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);

                HttpContext.Current.Response.AppendHeader("Cache-control", "no-store, no-cache, must-revalidate, post-check=0, pre-check=0, max-age=0");
                HttpContext.Current.Response.AppendHeader("pragma", "no-cache");
            }
            catch
            {
            }
        }

        public static string AssignReturnUrl(int currentPageId)
        {
            string strReturnUrl = string.Empty;

            var queryString = System.Web.HttpUtility.ParseQueryString(HttpContext.Current.Request.QueryString.ToString());

            // remove ReturnUrl key from querystring collection else it will throw error
            queryString.Remove("ReturnUrl");

            // assign current page url to querystring
            queryString.Add("ReturnUrl", new Node(currentPageId).NiceUrl);

            // create return url
            strReturnUrl = uQuery.GetNodesByType("Login").FirstOrDefault().Url ?? "/";
            strReturnUrl = strReturnUrl.Remove(strReturnUrl.LastIndexOf('/')/*, strReturnUrl.Length*/) + "?" + queryString.ToString();

            return strReturnUrl;
        }

        public static void CheckUserSessionValidity(int currentPageId = 0)
        {
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                if (Member.GetCurrentMember() == null)
                {
                    //Log member out
                    System.Web.Security.FormsAuthentication.SignOut();
                    HttpContext.Current.Session.Abandon();

                    if (currentPageId != 0)
                    {
                        string nextUrlNode = string.Empty;
                        if (HttpContext.Current.Request.Url.ToString().IndexOf("dashboard") > -1)
                        {
                            nextUrlNode = uQuery.GetNodesByType("Login").FirstOrDefault().Url;
                        }
                        else if (HttpContext.Current.Request.Url.ToString().IndexOf("enter-your-goal") > -1)
                        {
                            nextUrlNode = uQuery.GetNodesByType("PledgeSteps").FirstOrDefault().NiceUrl;
                        }
                        else
                        {
                            nextUrlNode = new Node(currentPageId).NiceUrl;
                        }
                        string nextURL = nextUrlNode;
                        HttpContext.Current.Response.Redirect(nextURL, true);
                    }
                }
                else
                    if (currentPageId == 0)
                    {
                        string nextUrlNode = string.Empty;
                        if (HttpContext.Current.Request.Url.ToString().IndexOf("login") > -1)
                        {
                            nextUrlNode = uQuery.GetNodesByType("MyDashboard").FirstOrDefault().Url;
                        }
                        if (HttpContext.Current.Request.Url.ToString().IndexOf("sign-up") > -1)
                        {
                            nextUrlNode = uQuery.GetNodesByType("PledgeSteps").FirstOrDefault().NiceUrl;
                        }
                        string nextURL = nextUrlNode;
                        HttpContext.Current.Response.Redirect(nextURL ?? "/", true);
                    }
            }
            else
            {
                //not authenticated
                if (currentPageId != 0)
                {
                    string nextUrlNode = string.Empty;
                    if (HttpContext.Current.Request.Url.ToString().IndexOf("dashboard") > -1)
                    {
                        nextUrlNode = uQuery.GetNodesByType("Login").FirstOrDefault().Url;
                    }
                    else if (HttpContext.Current.Request.Url.ToString().IndexOf("enter-your-goal") > -1)
                    {
                        nextUrlNode = uQuery.GetNodesByType("SignUp").LastOrDefault().Url;
                    }
                    else
                    {
                        nextUrlNode = new Node(currentPageId).NiceUrl;
                    }
                    string nextURL = nextUrlNode;
                    HttpContext.Current.Response.Redirect(nextURL, true);
                }

            }
        }

        public static void ClearApplicationCache()
        {
            try
            {
                if (HttpContext.Current != null)
                {
                    List<string> keys = new List<string>();
                    // retrieve application Cache enumerator
                    IDictionaryEnumerator enumerator = HttpContext.Current.Cache.GetEnumerator();
                    // copy all keys that currently exist in Cache
                    while (enumerator.MoveNext())
                    {
                        if (!enumerator.Key.ToString().ToLower().Contains("umbraco"))
                            keys.Add(enumerator.Key.ToString());
                    }
                    // delete every key from cache
                    for (int i = 0; i < keys.Count; i++)
                    {
                        HttpContext.Current.Cache.Remove(keys[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Exception occured in ClearApplicationCache" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
        }

        public static List<MemberPledges> GetAllMemberPledges()
        {
            List<MemberPledges> lstMemberPledges = new List<MemberPledges>();
            try
            {
                List<umbraco.NodeFactory.Node> lstPledges = uQuery.GetNodesByType("PledgeMember").ToList();
                foreach (umbraco.NodeFactory.Node pledge in lstPledges)
                {
                    if (!pledge.GetProperty<bool>("step3Clear"))
                    {
                        continue;
                    }
                    MemberPledges objMemberPledges = new MemberPledges();
                    objMemberPledges.MemberId = pledge.GetProperty<int>("memberId");
                    objMemberPledges.PledgeId = pledge.Id;

                    DateTime outDateTime;
                    if (!string.IsNullOrEmpty(pledge.GetProperty<string>("startDate"))
                        && DateTime.TryParseExact(pledge.GetProperty<string>("startDate"),
                        "dd/MM/yyyy", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out outDateTime))
                    {

                        objMemberPledges.StartDate = outDateTime; //DateTime.ParseExact(pledge.GetProperty<string>("startDate"), "dd-MM-yyyy h:mm:ss", CultureInfo.InvariantCulture);
                    }

                    if (!string.IsNullOrEmpty(pledge.GetProperty<string>("endDate"))
                        && DateTime.TryParseExact(pledge.GetProperty<string>("endDate"),
                        "dd/MM/yyyy", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out outDateTime))
                    {
                        objMemberPledges.EndDate = outDateTime; //DateTime.ParseExact(pledge.GetProperty<string>("endDate"), "dd-MM-yyyy h:mm:ss", CultureInfo.InvariantCulture);
                    }

                    objMemberPledges.CreatedDate = pledge.CreateDate;
                    objMemberPledges.ImageUrl = pledge.GetProperty<string>("imageUrl");
                    objMemberPledges.YouTubeUrl = pledge.GetProperty<string>("youTubeUrl");
                    objMemberPledges.Shared = pledge.GetProperty<bool>("shared");
                    objMemberPledges.ParentId = pledge.Parent.Id;
                    objMemberPledges.IsDeleted = pledge.GetProperty<bool>("isDeleted");
                    lstMemberPledges.Add(objMemberPledges);
                }

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetAllMemberPledges() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            return lstMemberPledges;
        }

        public static List<DashboardDiscussion> GetDashboardDiscussions()
        {
            //Main 'Discussoins' list
            List<DashboardDiscussion> lstDashboardDiscussion = new List<DashboardDiscussion>();

            //Discussion 'Following tab' list
            List<DashboardDiscussion> lstDashboardDiscussionFollowing = new List<DashboardDiscussion>();

            //Discussion 'Replies tab' list
            List<DashboardDiscussion> lstDashboardDiscussionReplies = new List<DashboardDiscussion>();
            try
            {
                Entities dbEntities = new Entities();

                //Get all discussion that this user following
                int currentMemberId = Member.GetCurrentMember().Id;

                #region Discussion Following
                List<DiscussionFollower> lstDiscussionFollower = dbEntities.DiscussionFollowers.Where(obj =>
                    obj.MemberId == currentMemberId).ToList();

                foreach (DiscussionFollower discussionFollower in lstDiscussionFollower)
                {
                    //get details and bind it to list
                    umbraco.NodeFactory.Node discussion = new Node(Convert.ToInt32(discussionFollower.DiscussionId));
                    if (discussion != null && discussion.Id > 0)
                    {
                        DashboardDiscussion dashboardDiscussionFollowing = new DashboardDiscussion();
                        dashboardDiscussionFollowing.DiscussionTitle = discussion.GetProperty<string>("discussionTitle");
                        //dashboardDiscussion.DiscussionDescription = discussion.GetProperty<string>("discussionDescription");
                        dashboardDiscussionFollowing.LinkURL = discussion.NiceUrl;

                        //This is the date when discussion has been created
                        dashboardDiscussionFollowing.PostedDateTime = discussion.CreateDate;

                        //This is the date when user clicks on 'follow' button
                        if (discussionFollower.CreatedDateTime.HasValue)
                        {
                            dashboardDiscussionFollowing.DiscussionFollowingDateTime = discussionFollower.CreatedDateTime.Value;
                        }

                        //get reply count
                        dashboardDiscussionFollowing.UnreadCount = discussion.GetDescendantNodes().Where(obj
                            => obj.CreateDate >= discussionFollower.LastReadDateTime).Count();

                        //set this true to bind reply grid
                        dashboardDiscussionFollowing.isReply = false;

                        //Get Posted by
                        if (discussion.GetProperty<int>("createdById") > 0)
                        {
                            try
                            {
                                Member member = new Member(discussion.GetProperty<int>("createdById"));
                                if (member != null && member.Id > 0)
                                {
                                    dashboardDiscussionFollowing.PostedBy = member.GetProperty<string>("firstName") + " "
                                        + member.GetProperty<string>("lastName");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                                    "Rexona AUS : GetDashboardDiscussions() method: Get Member:" + Environment.NewLine
                                    + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                            }
                        }

                        dashboardDiscussionFollowing.Id = discussion.Id;

                        lstDashboardDiscussionFollowing.Add(dashboardDiscussionFollowing);
                    }
                }
                //Order the dashboard discussion on
                // 1. Descending order of Unread counts
                // 2. Then by descending order of => User 'follow' click date time
                if (lstDashboardDiscussionFollowing != null && lstDashboardDiscussionFollowing.Count > 0)
                {
                    lstDashboardDiscussion.AddRange(lstDashboardDiscussionFollowing.OrderByDescending(obj => obj.UnreadCount)
                        .ThenByDescending(obj => obj.DiscussionFollowingDateTime).ToList());
                }

                #endregion

                #region Discussion Replies
                //Order the dashboard replies on: latest reply on top

                //Step1
                //get all replies created by user
                var replies = uQuery.GetNodesByType("PledgeDiscussionReply")
                    .Where(obj => obj.GetProperty<int>("createdById") == currentMemberId).OrderByDescending(obj => obj.CreateDate);

                foreach (Node reply in replies)
                {
                    //get discussion title
                    Node discussionNodeForReply = reply.GetAncestorNodes()
                         .Where(node => node.NodeTypeAlias == "PledgeDiscussion").FirstOrDefault();
                    string discussionTitle = discussionNodeForReply != null && discussionNodeForReply.Id > 0 ?
                        discussionNodeForReply.GetProperty<string>("discussionTitle") : string.Empty;

                    //get all reply people added to the reply send by this user
                    if (reply.Children.Count > 0)
                    {
                        //Step2
                        //Get all replies
                        foreach (Node replyChield in reply.Children)
                        {
                            DashboardDiscussion DashboardDiscussionReply = new DashboardDiscussion();
                            DashboardDiscussionReply.DiscussionTitle = discussionTitle;
                            DashboardDiscussionReply.DiscussionDescription = replyChield.GetProperty<string>("discussionReply");
                            DashboardDiscussionReply.LinkURL = replyChield.NiceUrl;

                            DashboardDiscussionReply.PostedDateTime = replyChield.CreateDate;

                            //Get Posted by
                            if (replyChield.GetProperty<int>("createdById") > 0)
                            {
                                try
                                {
                                    Member member = new Member(replyChield.GetProperty<int>("createdById"));
                                    if (member != null && member.Id > 0)
                                    {
                                        DashboardDiscussionReply.PostedBy = member.GetProperty<string>("firstName") + " "
                                            + member.GetProperty<string>("lastName");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                                        "Rexona AUS : GetDashboardDiscussions() method: Get Member:" + Environment.NewLine
                                        + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                                }
                            }

                            //set this true to bind reply grid
                            DashboardDiscussionReply.isReply = true;

                            DashboardDiscussionReply.Id = discussionNodeForReply.Id;

                            lstDashboardDiscussionReplies.Add(DashboardDiscussionReply);
                        }
                    }
                }
                //Add replies to final output and return the list and order so that latest shold be displayed on top
                if (lstDashboardDiscussionReplies != null && lstDashboardDiscussionReplies.Count > 0)
                {
                    lstDashboardDiscussion.AddRange(lstDashboardDiscussionReplies.OrderByDescending(obj => obj.PostedDateTime));
                }

                #endregion
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                    "Rexona AUS : GetDashboardDiscussions() method:" + Environment.NewLine
                    + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            return lstDashboardDiscussion;
        }

        public static class PasswordTokenHelper
        {
            static readonly string PasswordHash = "rExoNAAuS";
            static readonly string SaltKey = "RUs@ltkEY";
            static readonly string VIKey = "#wierd$V&I!Key~`";
            static readonly int TokenExpiryMinutes = 120;

            public static string Encrypt(string plainText)
            {
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

                byte[] keyBytes = new Rfc2898DeriveBytes(PasswordHash, Encoding.ASCII.GetBytes(SaltKey)).GetBytes(256 / 8);
                var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
                var encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));

                byte[] cipherTextBytes;

                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        cipherTextBytes = memoryStream.ToArray();
                        cryptoStream.Close();
                    }
                    memoryStream.Close();
                }
                return Convert.ToBase64String(cipherTextBytes);
            }

            public static string Decrypt(string encryptedText)
            {
                byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);
                byte[] keyBytes = new Rfc2898DeriveBytes(PasswordHash, Encoding.ASCII.GetBytes(SaltKey)).GetBytes(256 / 8);
                var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.None };

                var decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));
                var memoryStream = new MemoryStream(cipherTextBytes);
                var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                byte[] plainTextBytes = new byte[cipherTextBytes.Length];

                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                memoryStream.Close();
                cryptoStream.Close();
                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
            }

            public static string GetUserToken(Member user)
            {
                try
                {
                    string userEmail = Convert.ToString(user.Email);
                    string createdDate = DateTime.UtcNow.ToString("MM/dd/yyyy h:mm:ss tt");
                    string expiryDate = DateTime.UtcNow.AddMinutes(TokenExpiryMinutes).ToString("MM/dd/yyyy h:mm:ss tt");
                    string tokenFormat = "userEmail:{0}{3}createdDate:{1}{3}expiryDate:{2}";

                    string encryptedToken = Encrypt(string.Format(tokenFormat, userEmail, createdDate, expiryDate, (char)7));

                    return encryptedToken;

                }
                catch (Exception ex)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: GetUserToken() Token generation failed. " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                    return string.Empty;
                }
            }

            public static bool VerifyUserToken(Member user, string encryptedToken, bool updateData = false)
            {
                try
                {
                    string userEmail = Convert.ToString(user.Email);
                    string plainTextToken = Decrypt(encryptedToken);
                    string[] tokenElements = plainTextToken.Split((char)7);
                    DateTime createdDate, expiryDate;

                    //we expect token to contain 3 elements.
                    if (tokenElements.Length != 3) return false;

                    //we expect token's first element to be user email
                    if (!tokenElements[0].StartsWith("userEmail:")) return false;

                    //we expect token's second element to be token generation time
                    if (!tokenElements[1].StartsWith("createdDate:")) return false;

                    //we expect token's third element to be token expiry time
                    if (!tokenElements[2].StartsWith("expiryDate:")) return false;

                    //if we get a token with user email other than that of current user, reject token.
                    if (!string.Equals(tokenElements[0].Replace("userEmail:", string.Empty), userEmail)) return false;

                    //if createdDate was not parsed succesfully reject token
                    if (!DateTime.TryParse(tokenElements[1].Replace("createdDate:", string.Empty), out createdDate)) return false;

                    //if expiryDate was not parsed succesfully reject token
                    if (!DateTime.TryParse(tokenElements[2].Replace("expiryDate:", string.Empty), out expiryDate)) return false;

                    //someone tried to change expiry date. reject token
                    //if (expiryDate.Subtract(createdDate).TotalMinutes != TokenExpiryMinutes) return false;

                    //token has expired. reject token
                    if (!(createdDate <= DateTime.UtcNow && DateTime.UtcNow <= expiryDate)) return false;

                    Entities dbEntities = new Entities();
                    UserResetPassword userResetPasswordRow = dbEntities.UserResetPasswords.Where(urp => urp.UserEmail.Equals(user.Email) && urp.Token.Equals(encryptedToken)).FirstOrDefault();
                    if (userResetPasswordRow != null)
                    {
                        if (userResetPasswordRow.IsUsed)
                        {
                            return false;
                        }
                        else
                        {
                            userResetPasswordRow.IsUsed = true;
                        }
                    }
                    else
                    {
                        dbEntities.UserResetPasswords.Add(new UserResetPassword()
                        {
                            IsUsed = true,
                            Token = encryptedToken,
                            UserEmail = user.Email
                        });
                    }
                    if (updateData)
                        dbEntities.SaveChanges();

                    return true;
                }
                catch (Exception ex)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU:VerifyUserToken() Token verification failed. " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                    return false;

                }
            }
        }

        public static class ResetPasswordHelper
        {
            static readonly string PasswordHash = "rExoNAAuS";
            static readonly string SaltKey = "RUs@ltkEY";
            static readonly string VIKey = "#wierd$V&I!Key~`";
            static readonly int TokenExpiryMinutes = 120;

            public static string GetUserToken(Member user)
            {
                try
                {
                    string userEmail = Convert.ToString(user.Email);
                    string createdDate = DateTime.UtcNow.ToString("MM/dd/yyyy h:mm:ss tt");
                    string expiryDate = DateTime.UtcNow.AddMinutes(TokenExpiryMinutes).ToString("MM/dd/yyyy h:mm:ss tt");
                    string tokenFormat = "userEmail:{0}{3}createdDate:{1}{3}expiryDate:{2}";

                    string encryptedToken = PasswordTokenHelper.Encrypt(string.Format(tokenFormat, userEmail, createdDate, expiryDate, (char)7));

                    return encryptedToken;

                }
                catch (Exception ex)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: GetUserToken() Token generation failed. " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                    return string.Empty;
                }
            }

            public static bool VerifyUserToken(Member user, string encryptedToken, bool updateData = false)
            {
                try
                {
                    string userEmail = Convert.ToString(user.Email);
                    string plainTextToken = PasswordTokenHelper.Decrypt(encryptedToken);
                    string[] tokenElements = plainTextToken.Split((char)7);
                    DateTime createdDate, expiryDate;

                    //we expect token to contain 3 elements.
                    if (tokenElements.Length != 3) return false;

                    //we expect token's first element to be user email
                    if (!tokenElements[0].StartsWith("userEmail:")) return false;

                    //we expect token's second element to be token generation time
                    if (!tokenElements[1].StartsWith("createdDate:")) return false;

                    //we expect token's third element to be token expiry time
                    if (!tokenElements[2].StartsWith("expiryDate:")) return false;

                    //if we get a token with user email other than that of current user, reject token.
                    if (!string.Equals(tokenElements[0].Replace("userEmail:", string.Empty), userEmail)) return false;

                    //if createdDate was not parsed succesfully reject token
                    if (!DateTime.TryParse(tokenElements[1].Replace("createdDate:", string.Empty), out createdDate)) return false;

                    //if expiryDate was not parsed succesfully reject token
                    if (!DateTime.TryParse(tokenElements[2].Replace("expiryDate:", string.Empty), out expiryDate)) return false;

                    //someone tried to change expiry date. reject token
                    //if (expiryDate.Subtract(createdDate).TotalMinutes != TokenExpiryMinutes) return false;

                    //token has expired. reject token
                    if (!(createdDate <= DateTime.UtcNow && DateTime.UtcNow <= expiryDate)) return false;

                    Entities dbEntities = new Entities();
                    UserResetPassword userResetPasswordRow = dbEntities.UserResetPasswords.Where(urp => urp.UserEmail.Equals(user.Email) && urp.Token.Equals(encryptedToken)).FirstOrDefault();
                    if (userResetPasswordRow != null)
                    {
                        if (userResetPasswordRow.IsUsed)
                        {
                            return false;
                        }
                        else
                        {
                            userResetPasswordRow.IsUsed = true;
                        }
                    }
                    else
                    {
                        dbEntities.UserResetPasswords.Add(new UserResetPassword()
                        {
                            IsUsed = true,
                            Token = encryptedToken,
                            UserEmail = user.Email
                        });
                    }
                    if (updateData)
                        dbEntities.SaveChanges();

                    return true;
                }
                catch (Exception ex)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: VerifyUserToken() Token verification failed. " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                    return false;

                }
            }

        }

        public static class TractionAPI
        {
            #region Save Traction Data
            public static bool SendEmailUsingTraction(string TractionQueryString, string EmailType, int DelayMinutes = 0)
            {
                try
                {
                    //add record to email transaction table 
                    using (Entities dbEntities = new Entities())
                    {
                        dbEntities.TractionEmails.Add(new TractionEmail()
                        {
                            TractionQueryString = TractionQueryString,
                            RetryCount = 0,
                            IsSent = false,
                            EmailType = EmailType,

                            SendDateTime = DateTime.Now.AddMinutes(DelayMinutes),

                            CreatedDateTime = DateTime.Now
                        });
                        if (dbEntities.SaveChanges() > 0)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                        "Rexona AUS : SendWelcomeEmailUsingTraction() method failed during execution. Stack Trace: " + ex.StackTrace);
                }
                return false;
            }

            #endregion

            #region Post Data To Traction
            public static void SendTractionEmails()
            {
                try
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                        "*******Rexona AUS : SendTractionEmails() method execution starts*******");

                    using (Entities dbEntities = new Entities())
                    {
                        int retryCount = 0;
                        int.TryParse(ConfigurationManager.AppSettings["TractionRetryCount"], out retryCount);

                        List<TractionEmail> lstTractionEmail = dbEntities.TractionEmails.Where(obj => !obj.IsSent
                            && obj.RetryCount < retryCount).OrderBy(obj => obj.CreatedDateTime).ToList();

                        if (lstTractionEmail != null && lstTractionEmail.Count > 0)
                        {

                            //send data to traction only if send date time is null or send date time is past
                            var records = lstTractionEmail.Where(obj => (!obj.SendDateTime.HasValue
                                || obj.SendDateTime.Value <= DateTime.Now));

                            if (records != null && records.Count() > 0)
                            {
                                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, string.Format("Sending Traction Email to {0} Emails", records.Count()));

                                foreach (TractionEmail item in records)
                                {
                                    if (PostTractionData(item.TractionQueryString))
                                    {
                                        item.IsSent = true;
                                    }
                                    else
                                    {
                                        item.RetryCount += 1;
                                    }
                                    dbEntities.SaveChanges();
                                }
                            }
                            else
                            {
                                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                                    "No record found to send email using Traction API");
                            }
                        }
                        else
                        {
                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                                "No record found to send email using Traction API");
                        }
                    }

                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                        string.Format("Send Traction Emails sending service has ended at {0}", DateTime.Now));
                }
                catch (Exception ex)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                        "Rexona AUS : SendTractionEmails() method failed during execution. Stack Trace: " + ex.StackTrace);
                }
                finally
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                        "*******Rexona AUS : SendTractionEmails() method execution Ended*******");
                }
            }

            private static bool PostTractionData(string Url)
            {
                try
                {
                    HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(Url);

                    ASCIIEncoding encoding = new ASCIIEncoding();
                    byte[] data = encoding.GetBytes(" ");

                    httpWReq.Method = "POST";
                    httpWReq.ContentType = "application/json";
                    httpWReq.ContentLength = data.Length;

                    using (Stream stream = httpWReq.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }

                    HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();

                    string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    if (response != null && response.StatusCode == HttpStatusCode.OK && !responseString.ToLower().Contains("error"))
                    {
                        return true;
                    }
                    else
                    {
                        LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                        "Rexona AUS : PostTractionData() failed for a email. Request URL: " + Url);

                        LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                        "Rexona AUS : PostTractionData() failed for a email. Response Text: " + responseString);
                    }

                }
                catch (Exception ex)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                        "Rexona AUS : PostTractionData() method failed during execution. Stack Trace: " + ex.StackTrace);
                }
                return false;
            }
            #endregion

            #region Traction - Signup for updates
            public static bool customerwithCustomAttribute(string emailAddress)
            {
                try
                {
                    TractionConnection connection = TractionConnection.Create("TractionAPI");
                    AddCustomerService service = new AddCustomerService(connection);

                    UpdateAttributeCollection attributes = new UpdateAttributeCollection();
                    //Email is default attrubte.
                    attributes.Add(DefaultAttribute.Email, emailAddress);

                    #region Subscription
                    SubscriptionService subscriptionService = new SubscriptionService(connection);

                    long subscriptionId;
                    if (long.TryParse(ConfigurationManager.AppSettings["subscriptionsID"], out subscriptionId))
                    {
                        subscriptionService.Subscribe(subscriptionId, emailAddress);
                    }
                    #endregion

                    //attributes.Add(DefaultAttribute.FirstName, null);
                    //attributes.Add(DefaultAttribute.LastName, null);

                    //all these attribute ID need to pass to traction and are configured in web.config files.
                    //Note that Name of custom attributes must match that configured in web.config. So names should NOT be changed
                    /*
                     *** As of now, we only have email address of user. Hence all other parameters CANNOT be sent to Traction.
                     */
                    /*attributes.Add(TractionConfiguration.CustomAttributes["PCODE"], null);
                    attributes.Add(TractionConfiguration.CustomAttributes["ULDM_BRAND_EMAIL_OPT_IN_SOURCE"], null);
                    attributes.Add(TractionConfiguration.CustomAttributes["DOB"], null);
                    attributes.Add(TractionConfiguration.CustomAttributes["ACCEPT_TCS_REXONA_I_WILL"], null);
                    attributes.Add(TractionConfiguration.CustomAttributes["REXONA_I_WILL_PLEDGE"], null);
                    attributes.Add(TractionConfiguration.CustomAttributes["REXONA_PLEDGES"], null);*/

                    // Submit the request to Traction for processing using email as the match key
                    EmailMatchKey matchKey = new EmailMatchKey(emailAddress);
                    service.AddCustomer(matchKey, attributes);
                    return true;
                }
                catch (Exception ex)
                {
                    LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : PostTractionData() method failed during execution.", ex);
                    return false;
                }
            }
            #endregion
        }
        public static class ArticlesHelper
        {
            public static List<ArticleEntry> GetAllArticles(int key)
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

                                        ArticleEntry article = new ArticleEntry();
                                        article.Id = children.Id;
                                        article.UploadDateAsDateTime = children.CreateDate;
                                        article.UploadedDateAsString = children.CreateDate.ToString("dd/MM/yyy");

                                        article.ActualArticleURL = children.NiceUrl;
                                        article.ActualArticleURLWithDomain = children.GetFullNiceUrl();
                                        article.ArticleTitle = children.GetProperty<string>("articleTitle");
                                        article.Hearts = String.IsNullOrEmpty(children.GetProperty<string>("like")) ? 0 : Convert.ToInt32(children.GetProperty("like").Value);
                                        article.Excerpt = Convert.ToString(children.GetProperty("excerpt").Value);

                                        #region get article thumbnail
                                        int nodeid = String.IsNullOrWhiteSpace(children.GetProperty<string>("heroImage")) ? 0 : Convert.ToInt32(children.GetProperty("heroImage").Value);

                                        if (nodeid != 0)
                                        {
                                            umbraco.cms.businesslogic.media.Media imgNode = new umbraco.cms.businesslogic.media.Media(nodeid);

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
                                        article.AmbassadorName = children.GetProperty<string>("articleAuthor");
                                        article.Tags = children.GetProperty<string>("articleTag");
                                        article.Type = Convert.ToString(children.GetProperty<string>("articleType"));

                                        #region Traction RSS + eDM related extra properties
                                        article.IncludeInEDM = children.GetProperty<bool>("includeInEDM");
                                        article.MailOutDate = children.GetProperty<DateTime>("mailOutDate");
                                        article.ArticleCategorty = Convert.ToString(children.GetProperty<string>("articleCategory"));
                                        article.ArticleBuckets = Convert.ToString(children.GetProperty<string>("articleBucket"));
                                        #endregion

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
                    return configObj as List<ArticleEntry>;
                }
                catch (Exception ex)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetAllArticles() method failed during execution.: "
                        + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                    return null;
                }
            }
        }

        /// <summary>
        /// Replace special charactor in new Node name as special char is not allowed in node name
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static string ReplaceSpecialChar(string rawString)
        {
            try
            {
                return System.Text.RegularExpressions.Regex.Replace(rawString, @"[^0-9a-zA-Z]+", "_");
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Exception occured in ReplaceSpecialChar()", ex);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: ReplaceSpecialChar() Failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }

            return rawString;
        }

        #region Google Analytics
        /// <summary>
        /// Save All Youtube video ids in the database used in the application
        /// This is used in video reporting feature
        /// </summary>
        /// <returns></returns>
        public static bool SaveYoutubeVideos()
        {
            try
            {                
                //YouTubeVideosTable
                //Update all records set IsDeleted = '1'
                //Add NEW records to database
                //upadate updatedDateTime if record ALREADY EXIST

                //Update all records set IsDeleted = '1'
                var objCtx = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)dbEntities).ObjectContext;
                objCtx.ExecuteStoreCommand("UPDATE [YouTubeVideos] SET isDeleted = '1'");

                #region Home Page
                Node homeNode = uQuery.GetNodesByType("Home").FirstOrDefault();
                if (homeNode != null && homeNode.Id > 0)
                {
                    string data = homeNode.GetProperty<string>("youTubeUrl");
                    if (!string.IsNullOrEmpty(data))
                    {
                        string youTubeIds = GetYouTubeVideoIdsFromData(data);
                        if (!string.IsNullOrEmpty(youTubeIds))
                        {
                            foreach (string youTubeId in youTubeIds.Split(','))
                            {
                                SaveYouTubeVideoIdsForPage(homeNode.Id.ToString(), homeNode.NiceUrl, youTubeId);
                            }
                        }
                    }
                }
                #endregion

                #region Pledges
                IEnumerable<umbraco.NodeFactory.Node> lstMemberPledges = uQuery.GetNodesByType("PledgeMember").ToList()
                        .Where(obj => obj.GetProperty<bool>("step3Clear"));

                foreach (Node pledge in lstMemberPledges)
                {
                    string data = pledge.GetProperty<string>("youTubeUrl");
                    if (!string.IsNullOrEmpty(data))
                    {
                        string youTubeIds = GetYouTubeVideoIdsFromData(data);
                        if (!string.IsNullOrEmpty(youTubeIds))
                        {
                            foreach (string youTubeId in youTubeIds.Split(','))
                            {
                                SaveYouTubeVideoIdsForPage(pledge.Id.ToString(), pledge.NiceUrl, youTubeId);
                            }
                        }
                    }
                }
                #endregion

                #region Articles
                IEnumerable<umbraco.NodeFactory.Node> lstArticle = uQuery.GetNodesByType("Article").ToList();

                foreach (Node article in lstArticle)
                {
                    string data = article.GetProperty<string>("articleDescription");
                    //get YouTube URLs from Iframe tag
                    MatchCollection matches = Regex.Matches(data, "<iframe.+?src=[\"'](.+?)[\"'].*?>", RegexOptions.IgnoreCase);//.Groups[1].Value;
                    foreach (Match origionalLink in matches)
                    {
                        if (origionalLink.Groups.Count > 1)
                        {
                            string strLink = origionalLink.Groups[1].Value;
                            if (!string.IsNullOrEmpty(strLink))
                            {
                                string youTubeIds = GetYouTubeVideoIdsFromData(strLink);
                                if (!string.IsNullOrEmpty(youTubeIds))
                                {
                                    foreach (string youTubeId in youTubeIds.Split(','))
                                    {
                                        SaveYouTubeVideoIdsForPage(article.Id.ToString(), article.NiceUrl, youTubeId);
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                
                UpdateYouTubeVideoLikes();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Exception occured in SaveYoutubeVideos()", ex);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: SaveYoutubeVideos() Failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            return false;
        }

        /// <summary>
        /// Get YouTubeVideos from service
        /// </summary>
        private static void UpdateYouTubeVideoLikes()
        {
            try
            {
                using (Entities dbEntities = new Entities())
                {
                    List<YouTubeVideo> youTubeVideos = dbEntities.YouTubeVideos.Where(obj => !obj.IsDeleted).ToList();
                    if (youTubeVideos != null && youTubeVideos.Count > 0)
                    {
                        int recordCount = 0;

                        while (recordCount < youTubeVideos.Count)
                        {
                            try
                            {
                                System.Threading.Thread.Sleep(1000);
                                string youTubeVideoIds = string.Empty;
                                for (int count = 0; count < 50 && recordCount < youTubeVideos.Count; count++)
                                {
                                    if (youTubeVideoIds == string.Empty)
                                    {
                                        youTubeVideoIds = youTubeVideos[recordCount].YouTubeVideoId;
                                    }
                                    else
                                    {
                                        youTubeVideoIds += "," + youTubeVideos[recordCount].YouTubeVideoId;
                                    }

                                    recordCount++;
                                }

                                dynamic jsonObject;
                                GetYouTubeVideoStatistics(youTubeVideoIds, out jsonObject);

                                if (jsonObject != null)
                                {
                                    var a = jsonObject["items"];
                                    foreach (var item in a)
                                    {
                                        //dynamic item = test as IDictionary<string, object>;
                                        if (item["id"] != null && item["statistics"] != null && item["statistics"]["likeCount"] != null
                                            && dbEntities.YouTubeVideos.ToList().Exists(obj => obj.YouTubeVideoId == item["id"]))
                                        {
                                            //dbEntities.YouTubeVideos.ToList().Find(obj => obj.YouTubeVideoId == item["id"]).VideoLikes = long.Parse(item["statistics"]["likeCount"]);
                                        
                                            string youTubeIdFromResponse = item["id"];
                                            dbEntities.YouTubeVideos.Where(obj => obj.YouTubeVideoId == youTubeIdFromResponse).ToList().ForEach(obj1 => obj1.VideoLikes = long.Parse(item["statistics"]["likeCount"]));
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Exception occured in UpdateYouTubeVideoLikes() Loop for while", ex);
                                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: UpdateYouTubeVideoLikes() Failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                            }
                        }
                    }
                    dbEntities.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Exception occured in UpdateYouTubeVideoLikes()", ex);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: UpdateYouTubeVideoLikes() Failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
        }

        private static void GetYouTubeVideoStatistics(string videoIds, out dynamic jsonObject)
        {

            HttpWebRequest myReq = null;
            HttpWebResponse webResponse = null;
            jsonObject = null;
            string results = string.Empty;
            try
            {

                //myReq = (HttpWebRequest)WebRequest.Create("https://gdata.youtube.com/feeds/api/videos/" + videoIds + "?v=2&alt=json" + ConfigurationManager.AppSettings["YouTubeVideoAPIClientKey"]);

                //https://www.googleapis.com/youtube/v3/videos?part=statistics&id=Ozl7HrTtMBQ,wy2PYRl78Us,vBOzvNO8lvk&key=AIzaSyCDDiv9Pnndv-2giDT-6HRfdeOggxenVvs

                myReq = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/youtube/v3/videos?part=statistics&fields=items&id=" + videoIds + "&key=" + ConfigurationManager.AppSettings["YouTubeVideoAPIServerKey"]);
                myReq.Method = "GET";
                myReq.Timeout = (1000 * 60) * 10;
                webResponse = (HttpWebResponse)myReq.GetResponse();

                StreamReader sr = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8);
                results = sr.ReadToEnd();


                if (!string.IsNullOrEmpty(results))
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    jsonObject = serializer.Deserialize<dynamic>(results);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetYouTubeVideoStatistics() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace + Environment.NewLine + "Response Result: "+ results, ex);
            }
        }

        private static bool SaveYouTubeVideoIdsForPage(string NodeId, string niceUrl, string youTubeVideoId)
        {
            try
            {
                int nodeId;
                if (!Int32.TryParse(NodeId, out nodeId))
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                   "Rexona AU: SaveYouTubeVideoIdsForPage() Failed " + Environment.NewLine
                   + "Message: " + "Can not parse nodeId" + Environment.NewLine
                   + "Data: NodeId: " + NodeId + " niceUrl: " + niceUrl
                   + " youTubeVideoId: " + youTubeVideoId);
                    return false;
                }
                using (Entities dbEntities = new Entities())
                {
                    YouTubeVideo youTubeVideo = dbEntities.YouTubeVideos.ToList().Find(obj => obj.NodeId.Equals(nodeId)
                        && obj.YouTubeVideoId == youTubeVideoId);
                    if (youTubeVideo != null && youTubeVideo.Id > 0)
                    {
                        //upadate updatedDateTime if record ALREADY EXIST
                        youTubeVideo.UpdatedDateTime = DateTime.Now;
                        youTubeVideo.IsDeleted = false;
                    }
                    else
                    {
                        //Add NEW records to database
                        dbEntities.YouTubeVideos.Add(new YouTubeVideo()
                                           {
                                               NodeId = nodeId,
                                               NiceUrl = niceUrl,
                                               YouTubeVideoId = youTubeVideoId,
                                               IsDeleted = false,
                                               //TODO: Update it later
                                               //CreatedDateTime = new Node(nodeId).CreateDate,
                                               CreatedDateTime = DateTime.Now,
                                               UpdatedDateTime = DateTime.Now
                                           });
                    }
                    if (dbEntities.SaveChanges() > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                    "Exception occured in SaveYouTubeVideoIdsForPage()", ex);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                    "Rexona AU: SaveYouTubeVideoIdsForPage() Failed " + Environment.NewLine
                    + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace
                    + Environment.NewLine + "Data: NodeId: " + NodeId + " niceUrl: " + niceUrl
                    + " youTubeVideoId: " + youTubeVideoId);
            }
            return false;
        }
        #endregion

        private static string GetYouTubeVideoIdsFromData(string data)
        {
            string youTubeIds = string.Empty;
            try
            {

                MatchCollection matches = Regex.Matches(data, @"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);

                //MatchCollection matches = Regex.Matches(data, @"\s*(youtube(?:-nocookie)?.com\/(?:v|embed)\/([a-zA-Z0-9-]+)).*", RegexOptions.IgnoreCase);

                //Regex.Matches(data, @"^(?:\/\/)?(?:www\.)?(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))((\w|-){11})(?:\S+)?$", RegexOptions.IgnoreCase);//.Groups[1].Value;

                foreach (Match youTubeLink in matches)
                {
                    if (youTubeLink.Groups.Count > 1)
                    {
                        if (!string.IsNullOrWhiteSpace(youTubeIds))
                        {
                            youTubeIds += ",";
                        }
                        youTubeIds += youTubeLink.Groups[1].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Exception occured in GetYouTubeVideoIdsFromData()", ex);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: GetYouTubeVideoIdsFromData() Failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            return youTubeIds;
        }

        #region DashBoard Notification

        public static List<DashboardDiscussion> GetDashboardDiscussionsNotification()
        {
            //Main 'Discussoins' list
            List<DashboardDiscussion> lstDashboardDiscussion = new List<DashboardDiscussion>();

            //Discussion 'Following tab' list
            List<DashboardDiscussion> lstDashboardDiscussionFollowing = new List<DashboardDiscussion>();

            //Discussion 'Replies tab' list
            List<DashboardDiscussion> lstDashboardDiscussionReplies = new List<DashboardDiscussion>();
            try
            {
                Entities dbEntities = new Entities();

                //Get all discussion that this user following
                int currentMemberId = Member.GetCurrentMember().Id;

                #region Discussion Following
                List<DiscussionFollower> lstDiscussionFollower = dbEntities.DiscussionFollowers.Where(obj =>
                    obj.MemberId == currentMemberId).ToList();

                foreach (DiscussionFollower discussionFollower in lstDiscussionFollower)
                {
                    //get details and bind it to list
                    umbraco.NodeFactory.Node discussion = new Node(Convert.ToInt32(discussionFollower.DiscussionId));
                    if (discussion != null && discussion.Id > 0)
                    {
                        DashboardDiscussion dashboardDiscussionFollowing = new DashboardDiscussion();
                        dashboardDiscussionFollowing.DiscussionTitle = discussion.GetProperty<string>("discussionTitle");
                        //dashboardDiscussion.DiscussionDescription = discussion.GetProperty<string>("discussionDescription");
                        dashboardDiscussionFollowing.LinkURL = discussion.NiceUrl;

                        //This is the date when discussion has been created
                        dashboardDiscussionFollowing.PostedDateTime = discussion.CreateDate;

                        //This is the date when user clicks on 'follow' button
                        if (discussionFollower.CreatedDateTime.HasValue)
                        {
                            dashboardDiscussionFollowing.DiscussionFollowingDateTime = discussionFollower.CreatedDateTime.Value;
                        }

                        //get reply count
                        dashboardDiscussionFollowing.UnreadCount = discussion.GetDescendantNodes().Where(obj
                            => obj.CreateDate >= discussionFollower.LastReadDateTime).Count();

                        //set this true to bind reply grid
                        dashboardDiscussionFollowing.isReply = false;

                        //Get Posted by
                        if (discussion.GetProperty<int>("createdById") > 0)
                        {
                            try
                            {
                                Member member = new Member(discussion.GetProperty<int>("createdById"));
                                if (member != null && member.Id > 0)
                                {
                                    dashboardDiscussionFollowing.PostedBy = member.GetProperty<string>("firstName") + " "
                                        + member.GetProperty<string>("lastName");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                                    "Rexona AUS : GetDashboardDiscussions() method: Get Member:" + Environment.NewLine
                                    + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                            }
                        }

                        dashboardDiscussionFollowing.Id = discussion.Id;

                        lstDashboardDiscussionFollowing.Add(dashboardDiscussionFollowing);
                    }
                }
                //Order the dashboard discussion on
                // 1. Descending order of Unread counts
                // 2. Then by descending order of => User 'follow' click date time
                if (lstDashboardDiscussionFollowing != null && lstDashboardDiscussionFollowing.Count > 0)
                {
                    lstDashboardDiscussion.AddRange(lstDashboardDiscussionFollowing.OrderByDescending(obj => obj.UnreadCount)
                        .ThenByDescending(obj => obj.DiscussionFollowingDateTime).ToList());
                }

                #endregion

                //#region Discussion Replies
                ////Order the dashboard replies on: latest reply on top

                ////Step1
                ////get all replies created by user
                //var replies = uQuery.GetNodesByType("PledgeDiscussionReply")
                //    .Where(obj => obj.GetProperty<int>("createdById") == currentMemberId).OrderByDescending(obj => obj.CreateDate);

                //foreach (Node reply in replies)
                //{
                //    //get discussion title
                //    Node discussionNodeForReply = reply.GetAncestorNodes()
                //         .Where(node => node.NodeTypeAlias == "PledgeDiscussion").FirstOrDefault();
                //    string discussionTitle = discussionNodeForReply != null && discussionNodeForReply.Id > 0 ?
                //        discussionNodeForReply.GetProperty<string>("discussionTitle") : string.Empty;

                //    //get all reply people added to the reply send by this user
                //    if (reply.Children.Count > 0)
                //    {
                //        //Step2
                //        //Get all replies
                //        foreach (Node replyChield in reply.Children)
                //        {
                //            DashboardDiscussion DashboardDiscussionReply = new DashboardDiscussion();
                //            DashboardDiscussionReply.DiscussionTitle = discussionTitle;
                //            DashboardDiscussionReply.DiscussionDescription = replyChield.GetProperty<string>("discussionReply");
                //            DashboardDiscussionReply.LinkURL = replyChield.NiceUrl;

                //            DashboardDiscussionReply.PostedDateTime = replyChield.CreateDate;

                //            //Get Posted by
                //            if (replyChield.GetProperty<int>("createdById") > 0)
                //            {
                //                try
                //                {
                //                    Member member = new Member(replyChield.GetProperty<int>("createdById"));
                //                    if (member != null && member.Id > 0)
                //                    {
                //                        DashboardDiscussionReply.PostedBy = member.GetProperty<string>("firstName") + " "
                //                            + member.GetProperty<string>("lastName");
                //                    }
                //                }
                //                catch (Exception ex)
                //                {
                //                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                //                        "Rexona AUS : GetDashboardDiscussions() method: Get Member:" + Environment.NewLine
                //                        + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                //                }
                //            }

                //            //set this true to bind reply grid
                //            DashboardDiscussionReply.isReply = true;

                //            DashboardDiscussionReply.Id = discussionNodeForReply.Id;

                //            lstDashboardDiscussionReplies.Add(DashboardDiscussionReply);
                //        }
                //    }
                //}
                ////Add replies to final output and return the list and order so that latest shold be displayed on top
                //if (lstDashboardDiscussionReplies != null && lstDashboardDiscussionReplies.Count > 0)
                //{
                //    lstDashboardDiscussion.AddRange(lstDashboardDiscussionReplies.OrderByDescending(obj => obj.PostedDateTime));
                //}

                //#endregion
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                    "Rexona AUS : GetDashboardDiscussions() method:" + Environment.NewLine
                    + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            return lstDashboardDiscussion;
        }

        public static int GetDiscussionNotification(int discussionid)
        {
            List<DashboardDiscussion> lstDiscussions = new List<DashboardDiscussion>();
            Entities dtEntities = new Entities();
            List<DashboardDiscussion> DbUnread = new List<DashboardDiscussion>();
            int unreadcnt = 0;
            try
            {
                //Getting Current Member Id.
                int currentMemberId = Member.GetCurrentMember().Id;
                List<DiscussionFollower> lstDiscussionFollower = dbEntities.DiscussionFollowers.Where(obj =>
                    obj.MemberId == currentMemberId).ToList();
                //var recordToUpdate = dbEntities.DiscussionFollowers.FirstOrDefault(record => record.MemberId == currentMemberId && record.DiscussionId == discussionNode.Id);

                if (lstDiscussionFollower.Count > 0)
                {
                    foreach (DiscussionFollower discussionfollower in lstDiscussionFollower)
                    {
                        if (discussionid == discussionfollower.DiscussionId)
                        {
                            Entities dbEntity = new Entities();
                            DashboardDiscussion dashboardDiscussionunread = new DashboardDiscussion();
                            umbraco.NodeFactory.Node discussion = new Node(Convert.ToInt32(discussionfollower.DiscussionId));
                            var recordToUpdate = dbEntity.DiscussionFollowers.FirstOrDefault(record => record.MemberId == currentMemberId && record.DiscussionId == discussionfollower.DiscussionId);

                            //get reply count
                            dashboardDiscussionunread.UnreadCount = discussion.GetDescendantNodes().Where(obj
                                => obj.CreateDate >= recordToUpdate.LastReadDateTime).Count();
                            unreadcnt = dashboardDiscussionunread.UnreadCount;
                        }
                    }
                }
            }
            catch (Exception)
            {
                
                throw;
            }

            return unreadcnt;
        }
        #endregion
    }
}