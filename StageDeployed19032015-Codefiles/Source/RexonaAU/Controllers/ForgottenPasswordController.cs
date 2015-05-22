using RexonaAU.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using System.Web.Security;
using umbraco;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic.member;
using umbraco.NodeFactory;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using RexonaAU.Helpers;

namespace RexonaAU.Controllers
{
    public class ForgottenPasswordController : SurfaceController
    {
        #region Forgot Password Form
        /// <summary>
        /// Renders the Forgot Password view        
        /// </summary>
        /// <returns></returns>
        public ActionResult RenderForgotPassword()
        {
            ForgottenPasswordViewModel resetModel = new ForgottenPasswordViewModel();

            return PartialView("ForgottenPassword", resetModel);
        }
        /// <summary>
        /// Forgot Password logic
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult HandleForgotPassword(ForgottenPasswordViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool isMailSent = false;
                    //Check the member exists
                    var checkMember = Member.GetMemberFromEmail(model.EmailAddress);

                    //Check the member exists
                    if (checkMember != null)
                    {
                        //If member is signed up via Facebook connect then don't show reset password link in dashboard page
                        //and also make forgot password feture feature non-functional
                        if (checkMember.GetProperty<bool>("facebookConnect"))
                        {
                            TempData.Remove("FBSignedUp");
                            TempData.Add("FBSignedUp", "<strong>Message.</strong><br><br>Could not reset the password. Please sign-in using Facebook credentials.");
                            return RedirectToCurrentUmbracoPage();

                        }
                        Node resetNode = uQuery.GetNodesByType("ForgotPassword-Step-2").FirstOrDefault();
                        if (resetNode != null)
                        {
                            string token = HttpUtility.UrlEncode(RexonaAU.Helpers.Common.ResetPasswordHelper.GetUserToken(checkMember));

                            Entities dbEntities = new Entities();
                            dbEntities.UserResetPasswords.Add(new UserResetPassword()
                            {
                                IsUsed = false,
                                Token = token,
                                UserEmail = checkMember.Email
                            });
                            dbEntities.SaveChanges();

                            //string userName = checkMember.GetProperty<string>("firstName") +" " + 
                            checkMember.GetProperty<string>("lastName");

                            string template = RexonaAU.Helpers.Common.GetEmailTemplate
                                (RexonaAU.Helpers.Common.EmailTemplates.ForgottenPassword,
                                checkMember.Id, string.Empty);

                            string resetPasswordPageURL = umbraco.library.NiceUrlWithDomain(resetNode.Id).ToLower();
                            if (IsOriginalRequestOverSSL)
                            {
                                resetPasswordPageURL = resetPasswordPageURL.Replace("http://", "https://");
                            }

                            template = template.Replace("{{ForgotPasswordLink}}",
                                resetPasswordPageURL +
                                string.Format("?memberId={0}&token={1}", checkMember.Id, token));

                            template = template.Replace("{{DisplayName}}", checkMember.GetProperty<string>("displayName"));

                            isMailSent = Common.SendEmail(checkMember.Email,
                                Common.GetEmailSubject(Common.EmailTemplates.ForgottenPassword), template, false);
                        }
                        if (isMailSent)
                        {
                            TempData.Remove("SuccessMsg");
                            TempData.Add("SuccessMsg", "<strong>Check your email.</strong><br><br>An email has been sent with a link to reset your password.");
                        }
                        else
                        {
                            TempData.Remove("ExceptionMsg");
                            TempData.Add("ExceptionMsg", "<strong>Oops.</strong><br><br>Something went wrong.");
                        }
                        return RedirectToCurrentUmbracoPage();
                    }
                    else
                    {
                        TempData.Remove("NoRegisteredEmail");
                        TempData.Add("NoRegisteredEmail", "<strong>Oops.</strong><br><br>An account using that email address does not exist.");
                        return RedirectToCurrentUmbracoPage();
                    }
                }
                else
                {
                    TempData.Remove("NoRegisteredEmail");
                    TempData.Add("NoRegisteredEmail", "<strong>Oops.</strong><br><br>An account using that email address does not exist.");

                    return RedirectToCurrentUmbracoPage();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: HandleForgotPassword() Forgot Password Failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                TempData.Remove("ExceptionMsg");
                TempData.Add("ExceptionMsg", "<strong>Oops.</strong><br><br>Something went wrong. Please try again.");
                return RedirectToCurrentUmbracoPage();
            }
        }
        #endregion

        #region Reset Password
        public ActionResult RenderResetPassword(int? memberId, string token)
        {
            Node signInNode = uQuery.GetNodesByType("Login").FirstOrDefault();

            if (Member.IsLoggedOn())
            {
                memberId = Member.CurrentMemberId();
            }

            if (Member.IsLoggedOn() || (memberId.HasValue && memberId > -1 && !string.IsNullOrEmpty(token)))
            {
                Member user = new Member(memberId.Value);
                if (user != null)
                {
                    if (Member.IsLoggedOn() || Common.PasswordTokenHelper.VerifyUserToken(user, token, true))
                    {
                        ResetPasswordViewModel resetModel = new ResetPasswordViewModel();
                        resetModel.EmailAddress = user.Email;

                        //For double security we have also set the user email in session so that even if user changes it from browser,
                        // it should not reset password of another user
                        Session["ResetPasswordEmail"] = user.Email;
                        return PartialView("ResetPassword", resetModel);
                    }
                    else
                    {
                        TempData.Remove("ResetPasswordMsg");
                        TempData.Add("ResetPasswordMsg", "<strong>Oops.</strong><br><br>The reset password link is invalid. Please re-generate using Forgot Password."); return RedirectToUmbracoPage(signInNode.Id);
                    }
                }
            }
            //else
            //    return 
            return RedirectToUmbracoPage(signInNode.Id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                Node signInNode = uQuery.GetNodesByType("Login").FirstOrDefault();
                if (ModelState.IsValid && Session["ResetPasswordEmail"] != null)
                {
                    string email = Convert.ToString(Session["ResetPasswordEmail"]);
                    Session.Remove("ResetPasswordEmail");

                    //Check the member exists
                    var checkMember = Member.GetMemberFromEmail(email);

                    //Check the member exists
                    if (checkMember != null)
                    {
                        checkMember.Password = model.Password;
                        checkMember.Save();
                        Common.ClearApplicationCache();

                        TempData.Remove("ResetPasswordMsg");
                        TempData.Add("ResetPasswordMsg", "<strong>Message.</strong><br><br>Your password has been reset successfully.");
                        #region Login and redirect to Dashboard
                        try
                        {
                            //Get the member from their email address and password
                            var attemptLogin = Member.GetMemberFromLoginNameAndPassword(checkMember.LoginName, model.Password);
                            if (attemptLogin != null)
                            {
                                HttpCookie cookie = null;
                                FormsAuthenticationTicket authTicket = null;

                                authTicket = new FormsAuthenticationTicket(1, model.EmailAddress, DateTime.Now, DateTime.Now.AddMinutes(30), false, string.Empty, FormsAuthentication.FormsCookiePath);


                                //encrypt the ticket and add it to a cookie
                                cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(authTicket));
                                Response.Cookies.Add(cookie);

                                Node nextNode = uQuery.GetNodesByType("MyDashboard").FirstOrDefault();
                                if (nextNode != null && nextNode.Id > 0)
                                {
                                    Response.Redirect(nextNode.Url);
                                }
                            }
                            else
                            {
                                TempData.Remove("Status");
                                TempData["Status"] = "<strong>Error.</strong><br><br>Invalid username or password";
                            }
                        }
                        catch (Exception ex)
                        {
                            TempData.Remove("ErrorMessage");
                            TempData.Add("ErrorMessage", "<strong>Oops.</strong><br><br>Something went wrong. Please try again.");
                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: HandleResetPassword() Forgot Password Failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                        }
                        return RedirectToCurrentUmbracoPage();
                        #endregion
                    }
                    else
                    {
                        TempData.Remove("ResetPasswordMsg");
                        TempData.Add("ResetPasswordMsg", "<strong>Oops.</strong><br><br>The reset password link used is for other user. Please re-generate link using Forgot Password.");
                        return RedirectToUmbracoPage(signInNode.Id);
                    }
                }
                else
                {
                    TempData.Remove("ResetPasswordMsg");
                    TempData.Add("ResetPasswordMsg", "<strong>Oops.</strong><br><br>The reset password link has expired. Please re-generate link using Forgot Password."); return RedirectToUmbracoPage(signInNode.Id);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AU: HandleResetPassword() Forgot Password Failed " + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                TempData.Remove("ErrorMessage");
                TempData.Add("ErrorMessage", "<strong>Oops.</strong><br><br>Something went wrong. Please try again.");
                return RedirectToCurrentUmbracoPage();
            }
        }
        #endregion

        private bool IsOriginalRequestOverSSL
        {
            get
            {
                if (Request.Headers.AllKeys.Contains("X-Forwarded-Proto"))
                {
                    if (Request.Headers["X-Forwarded-Proto"].Equals("https", StringComparison.OrdinalIgnoreCase))
                        return true;
                    else
                        return false;
                }
                else
                    return Request.IsSecureConnection;
            }
        }
    }
}
