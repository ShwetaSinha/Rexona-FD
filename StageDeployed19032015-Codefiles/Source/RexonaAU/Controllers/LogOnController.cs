using RexonaAU.Models;
using RexonaAU.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Traction.Fusion;
using Traction.Fusion.Customers;
using Traction.Fusion.Subscriptions;
using umbraco.cms.businesslogic.member;
using umbraco;
using umbraco.NodeFactory;
using System.Security.Cryptography;
using System.IO;
using Umbraco.Core.Logging;
using System.Configuration;
using Umbraco.Core.Services;
namespace RexonaAU.Controllers
{
    public class LogOnController : Umbraco.Web.Mvc.SurfaceController
    {

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SignUp(SignUpModel model)
        {
            Member createMember = null;

            //Decrement count on register click
            //var SignUpNode = uQuery.GetNodesByType("SignUp").FirstOrDefault();
            //int CurrentDropOff;
            //if (SignUpNode != null && SignUpNode.Id > 0)
            //{
            //    CurrentDropOff = SignUpNode.GetProperty<int>("dropOffCount");
            //    SignUpNode.SetProperty("dropOffCount", CurrentDropOff - 1);
            //}

            try
            {
                bool exists = CheckExistingUser(model.Email);

                if (exists)
                {
                    Member current = Member.GetMemberFromEmail(model.Email);
                    if (current.GetProperty<bool>("facebookConnect"))
                    {
                        TempData.Remove("ErrorMessage");
                        TempData.Add("ErrorMessage", "<strong>Error.</strong><br><br>An account with the entered Email already exists.<br>Please Sign In using Facebook.");

                    }
                    else
                    {
                        TempData.Remove("ErrorMessage");
                        TempData.Add("ErrorMessage", "<strong>Error.</strong><br><br>An account with the entered Email already exists.<br>Please Sign In to continue.");
                    }
                    return RedirectToCurrentUmbracoPage();
                }
                else
                {
                    MemberType demoMemberType = MemberType.GetByAlias("RexonaPledgeUser"); //id of membertype ‘Rexona’

                    createMember = Member.MakeNew(model.FirstName + ' ' + model.LastName, model.Email, model.Email, demoMemberType, new umbraco.BusinessLogic.User(0));
                    //var firstname = model.UserName.Split(' ')[0];
                    //var lastname = model.UserName.IndexOf(' ') > -1 ? model.UserName.Remove(0, model.UserName.IndexOf(' ') + 1) : " ";

                    //createMember.getProperty("firstName").Value = model.FirstName;
                    //createMember.getProperty("lastName").Value = model.LastName;
                    //createMember.getProperty("displayName").Value = model.DisplayName;

                    ////createMember.getProperty("birthDate").Value = model.DateOfBirth;
                    //createMember.getProperty("birthDate").Value = Convert.ToDateTime(model.DateOfBirth).ToString("dd MMMM yyyy");
                    //createMember.getProperty("postCode").Value = model.PostCode;
                    createMember.getProperty("subscribedForDoMOre").Value = model.Subscribe;
                    createMember.getProperty("facebookConnect").Value = model.FacebookConnect;
                    createMember.SetProperty("termsConditions", 1);
                    //string capitalValues = ConfigurationManager.AppSettings[model.State];
                    //model.CapitalCity = capitalValues.Split(',')[0];
                   // model.CapitalPostCode = capitalValues.Split(',')[1];

                    //createMember.getProperty("state").Value = model.State;
                    //createMember.getProperty("capitalCity").Value = model.CapitalCity;
                    //createMember.getProperty("capitalPostCode").Value = model.CapitalPostCode;

                    createMember.Password = model.Password;

                    //track last login date
                    createMember.SetProperty("lastLoginDate", DateTime.Now.ToString("dd/MM/yyyy HH:mm tt"));

                    createMember.Save();


                    Session["SignUp"] = true;

                    SignInModel loginmodel = new SignInModel();
                    loginmodel.EmailAddress = model.Email;
                    loginmodel.Password = model.Password;

                    setAuthentication(loginmodel.EmailAddress, false);


                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : SignUp() method failed during execution. Stack Trace: " + ex.StackTrace);
                TempData.Remove("ErrorMessage");
                TempData.Add("ErrorMessage", "<strong>Oops.</strong><br><br>Something went wrong. Please try again.");

            }
            return RedirectToCurrentUmbracoPage();
        }

        [HttpPost]
        public JsonResult SignIn(string email, string password)
        {
            try
            {

                var checkMember = Member.GetMemberFromEmail(email);

                //Check the member exists
                if (checkMember != null)
                {
                    //Get the member from their email address and password
                    var attemptLogin = Member.GetMemberFromLoginNameAndPassword(checkMember.LoginName, password);

                    if (attemptLogin != null)
                    {
                        //track last login date
                        attemptLogin.SetProperty("lastLoginDate", DateTime.Now.ToString("dd/MM/yyyy HH:mm tt"));

                        setAuthentication(email, false);
                        int count = getMemberPledgeCount(attemptLogin);

                        if (count == 0)
                        {
                            return Json("NoPledges", JsonRequestBehavior.AllowGet);
                        }
                     
                    }
                    else
                    {
                        //TempData.Remove("Status");
                        //TempData["Status"] = "<strong>Error.</strong><br><br>Invalid username or password";
                        return Json("Invalid", JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    //TempData.Remove("Status");
                    //TempData["Status"] = "<strong>Error.</strong><br><br>Invalid username or password";
                    return Json("NotSignedUp", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : Signin() method failed during execution. Stack Trace: " + ex.StackTrace);
                TempData.Remove("ErrorMessage");
                TempData.Add("ErrorMessage", "<strong>Oops.</strong><br><br>Something went wrong. Please try again.");
                return Json("Error", JsonRequestBehavior.AllowGet);
            }
            return Json("Success", JsonRequestBehavior.AllowGet);

        }


        private void setAuthentication(string Email, bool isFbUser)
        {
            HttpCookie cookie = null;
            FormsAuthenticationTicket authTicket = null;

            authTicket = new FormsAuthenticationTicket(1, Email, DateTime.Now, DateTime.Now.AddMinutes(30), false, string.Empty, FormsAuthentication.FormsCookiePath);

            //encrypt the ticket and add it to a cookie
            cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(authTicket));
            Response.Cookies.Add(cookie);
            if (!isFbUser && Convert.ToBoolean(Session["SignUp"]))
            {
                Node nextNode = uQuery.GetNodesByType("MyDashboard").FirstOrDefault();
                if (nextNode != null && nextNode.Id > 0 && !Convert.ToBoolean(Session["SignUp"]))
                {
                    Response.Redirect(nextNode.Url);
                }
                else
                {
                    //nextNode = uQuery.GetNodesByType("PledgeSteps").LastOrDefault();
                    nextNode = uQuery.GetNodesByType("MyDashboard").LastOrDefault();
                    if (nextNode != null && nextNode.Id > 0)
                    {
                        Response.Redirect(nextNode.Url);
                    }
                    Session["SignUp"] = false;
                }
            }
        }

        [HttpPost]
        public ActionResult Logout()
        {
            var url = "";
            try
            {

                //Member already logged in, lets log them out and redirect them home
                if (Member.IsLoggedOn())
                {
                    //Log member out
                    FormsAuthentication.SignOut();
                    Session.Abandon();

                    //Redirect home
                    Node nextNode = uQuery.GetNodesByType("Login").FirstOrDefault();
                    if (nextNode != null && nextNode.Id > 0)
                    {
                        url = nextNode.Url;
                    }
                }
                else
                {
                    //Redirect home
                    Node nextNode = uQuery.GetNodesByType("Master").FirstOrDefault();
                    if (nextNode != null && nextNode.Id > 0)
                    {
                        url = nextNode.Url;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : Logout() method failed during execution." + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            return Json(url, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult FBLogin(string FirstName, string LastName, string DisplayName, string Email, string FacebookId, string Referrer, int Subscribe)
        {
            try
            {
                bool isMember = CheckExistingUser(Email);

                if (isMember)
                {
                    
                    var currentMember = Member.GetMemberFromEmail(Email);
                    if (currentMember != null)
                    {
                        if (currentMember.GetProperty<bool>("facebookConnect"))
                        {
                            setAuthentication(Email, true);
                            int count = getMemberPledgeCount(currentMember);

                            if (count == 0)
                            {
                                //track last login date
                                currentMember.SetProperty("lastLoginDate", DateTime.Now.ToString("dd/MM/yyyy HH:mm tt"));
                                if (Referrer.Equals("register", StringComparison.OrdinalIgnoreCase)){
                                    currentMember.SetProperty("termsConditions", 1);
                                    if (currentMember.GetProperty<int>("subscribedForDoMOre") == 0)
                                    {
                                        currentMember.SetProperty("subscribedForDoMOre", Subscribe);
                                    }
                                }
                                return Json("SignUpSuccess", JsonRequestBehavior.AllowGet);
                            }
                        }
                        else
                        {
                            //Error duplicate email ID
                            return Json("duplicateemailId", JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {

                        return Json("error", JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    //Signup
                    if (Referrer.Equals("login", StringComparison.OrdinalIgnoreCase))
                    {
                        return Json("PleaseSignUp", JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        FBSignUp(FirstName, LastName, DisplayName, Email, FacebookId,Subscribe);
                        return Json("SignUpSuccess", JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : FBlogin() method failed during execution." + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json("error", JsonRequestBehavior.AllowGet);
            }
            return Json("Success", JsonRequestBehavior.AllowGet);
        }

        private void FBSignUp(string FirstName, string LastName, string DisplayName, string Email, string FacebookId, int Subscribe)
        {
            try
            {
                //Decrement count on register click
                //var SignUpNode = uQuery.GetNodesByType("SignUp").FirstOrDefault();
                //int CurrentDropOff;
                //if (SignUpNode != null && SignUpNode.Id > 0)
                //{
                //    CurrentDropOff = SignUpNode.GetProperty<int>("dropOffCount");
                //    SignUpNode.SetProperty("dropOffCount", CurrentDropOff - 1);
                //}

                Member createMember = null;
                MemberType demoMemberType = MemberType.GetByAlias("RexonaPledgeUser"); //id of membertype ‘Rexona’



                createMember = Member.MakeNew(FirstName + ' ' + LastName, Email, Email, demoMemberType, new umbraco.BusinessLogic.User(0));
                //var firstname = model.UserName.Split(' ')[0];
                //var lastname = model.UserName.IndexOf(' ') > -1 ? model.UserName.Remove(0, model.UserName.IndexOf(' ') + 1) : " ";
                createMember.getProperty("firstName").Value = FirstName;
                createMember.getProperty("lastName").Value = LastName;
                createMember.getProperty("displayName").Value = DisplayName;
                createMember.getProperty("facebookConnect").Value = 1;
                createMember.getProperty("facebookId").Value = FacebookId;
                createMember.SetProperty("termsConditions", 1);
                if (createMember.GetProperty<int>("subscribedForDoMOre") == 0)
                {
                    createMember.SetProperty("subscribedForDoMOre", Subscribe);
                }
                //track last login date
                createMember.SetProperty("lastLoginDate", DateTime.Now.ToString());

                createMember.Save();

                setAuthentication(Email, true);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : FBSignin() method failed during execution." + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
        }


        public int getMemberPledgeCount(Member currentMember)
        {
            List<MemberPledges> allpledges = Common.GetAllMemberPledges();
            allpledges = allpledges.Where(pledge => pledge.MemberId == currentMember.Id).ToList();

            return allpledges.Count;

        }
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

        [ChildActionOnly]
        public ActionResult RenderRegister()
        {
            SignUpModel userModel = new SignUpModel();           
            return PartialView("Register", userModel);
        }

        [ChildActionOnly]
        public ActionResult RenderLogin()
        {
            SignInModel userModel = new SignInModel();
            return PartialView("Login", userModel);
        }

    }

}
