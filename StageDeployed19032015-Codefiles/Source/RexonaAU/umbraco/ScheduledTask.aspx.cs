using RexonaAU.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Umbraco.Core.Logging;
using System.Configuration;

namespace RexonaAU.umbracoScheduledTask
{
    public partial class ScheduledTask : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string prevUrl = string.Empty;
            string nextUrl = ConfigurationManager.AppSettings["InstaGramUrl"] + ConfigurationManager.AppSettings["hashTag"].Remove(0, 1)
                 + "/media/recent?access_token=" + ConfigurationManager.AppSettings["InstaGramAccessToken"];
            try
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "******In ScheduledTask Method Call******");

                if (Request.QueryString["id"] != null)
                {                    
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "ScheduledTask task Querystring Id Value " + Request.QueryString["id"]);

                    switch (Request.QueryString["id"])
                    {
                        case "sendMail":
                            //send schedule mails
                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "***ScheduledTask task In SendMail***");

                            Common.SendScheduledMails();

                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "***ScheduledTask task SendMail Done***");

                            break;

                        case "sendTractionMail":
                            //send traction mails
                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "***ScheduledTask task In SendTractionEmails***");

                            Common.TractionAPI.SendTractionEmails();

                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "***ScheduledTask task SendTractionEmails Done***");

                            break;


                        case "refreshSocialContent":

                            //Pull Social Contents from Instagram and Twitter Insert/Update Contents

                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "***ScheduledTask task In refresh Socail Content***");

                            ManageSocialContent.GetTweets();
                            ManageSocialContent.InstagramContent(prevUrl, nextUrl);

                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "***ScheduledTask refresh Socail Content Done***");

                            break;

                        case "approveSocialContent":

                            //Auto Approve Social Contents if they are in system for more than 24 hrs
                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "***ScheduledTask task In Approve Socail Content***");

                            ManageSocialContent.AutoApproveSocialContents();

                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "***ScheduledTask task In Approve Socail Done***");

                            break;

                        case "SaveYoutubeVideos":

                            //Auto Approve Social Contents if they are in system for more than 24 hrs
                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "***ScheduledTask task In SaveYoutubeVideos***");

                            Common.SaveYoutubeVideos();

                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "***ScheduledTask task In SaveYoutubeVideos Done***");

                            break;
                    }
                }
                else
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "ScheduledTask Id is null");
                }

            }

            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "ScheduledTask Failed - " +
                    Environment.NewLine + "Message: " + ex.Message + Environment.NewLine
                    + ex.StackTrace);

            }

        }
    }
}