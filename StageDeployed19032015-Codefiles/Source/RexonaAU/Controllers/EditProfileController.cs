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
    public class EditProfileController : SurfaceController
    {
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
                        EditProfileViewModel editprofileviewModel = new EditProfileViewModel();
                        editprofileviewModel.EmailAddress = user.Email;
                        editprofileviewModel.FirstName = user.GetProperty<string>("firstName");
                        editprofileviewModel.LastName = user.GetProperty<string>("lastName");
                        //For double security we have also set the user email in session so that even if user changes it from browser,
                        // it should not reset password of another user
                        Session["ResetPasswordEmail"] = user.Email;
                        return PartialView("EditProfile", editprofileviewModel);
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
        public ActionResult HandleResetPassword(EditProfileViewModel model)
        {
            try
            {
                //string currentuseremail = Session["ResetPasswordEmail"].ToString();
                //bool exists = false;
                //if (currentuseremail != model.EmailAddress)
                //    exists = CheckExistingUser(model.EmailAddress);

                //if (exists)
                //{
                //    Member current = Member.GetMemberFromEmail(model.EmailAddress);
                //    if (current.GetProperty<bool>("facebookConnect"))
                //    {
                //        TempData.Remove("ErrorMessage");
                //        TempData.Add("ErrorMessage", "<strong>Error.</strong><br><br>An account with the entered Email already exists.<br>Please Sign In using Facebook.");

                //    }
                //    else
                //    {
                //        TempData.Remove("ErrorMessage");
                //        TempData.Add("ErrorMessage", "<strong>Error.</strong><br><br>An account with the entered Email already exists.<br>Please Sign In to continue.");
                //    }
                //    return RedirectToCurrentUmbracoPage();
                //}
                //else
                //{
                Node signInNode = uQuery.GetNodesByType("Login").FirstOrDefault();
                // ModelState.IsValid &&
                if (Session["ResetPasswordEmail"] != null)
                {
                    string email = Convert.ToString(Session["ResetPasswordEmail"]);
                    Session.Remove("ResetPasswordEmail");

                    //Check the member exists
                    var checkMember = Member.GetMemberFromEmail(email);

                    //Check the member exists
                    if (checkMember != null)
                    {
                        if(!string.IsNullOrEmpty(model.Password))
                            checkMember.Password = model.Password;
                        checkMember.getProperty("firstName").Value = model.FirstName;
                        checkMember.getProperty("lastName").Value = model.LastName;
                        checkMember.Save();
                        Common.ClearApplicationCache();
                        TempData.Remove("ResetPasswordMsg");
                        TempData.Add("ResetPasswordMsg", "<strong>Message.</strong><br><br>Your Profile has been updated successfully.");
                        #region Login and redirect to Dashboard
                        try
                        {
                            //Get the member from their email address and password
                            if (!string.IsNullOrEmpty(model.Password))
                            {
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
                // }

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


        [HttpPost]
        public bool CheckExistingUser(string Email)
        {
            try
            {
                var MemberExist = Member.GetMemberFromEmail(Email);
                if (MemberExist == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {

                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : CheckExistingUser() method failed during execution." + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return false;
            }

        }

        public ActionResult RenderEditprofile()
        {
            EditProfileViewModel editprofileModel = new EditProfileViewModel();
            return PartialView("EditProfile-step", editprofileModel);
        }

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
