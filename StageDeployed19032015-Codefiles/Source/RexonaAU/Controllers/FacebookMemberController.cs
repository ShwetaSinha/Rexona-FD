using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RexonaAU.Models;
using RexonaAU.Helpers;
using System.Configuration;
using umbraco;
using umbraco.cms.businesslogic.member;
using umbraco.NodeFactory;
using Umbraco.Core.Logging;
using System.Web.Script.Serialization;
using System.Data.Entity.Validation;
namespace RexonaAU.Controllers
{
    public class FacebookMemberController : Umbraco.Web.Mvc.SurfaceController
    {

        #region get/remove Invited Members data
        [HttpPost]
        public JsonResult GetInvitedMembers(string memberId)
        {
            List<MemberDetails> facebookList = GetMembers(Convert.ToInt32(memberId), 1);


            //Getpledges();

            var result = new
                {
                    facebookMembers = facebookList

                };

            return Json(result, JsonRequestBehavior.AllowGet);

        }

        private const string CachePrefix = "RexonaAUSMembers_{0}";
        private List<MemberDetails> GetMembers(int memberId, int key)
        {
            try
            {
                object configObj = null;


                List<MemberDetails> InvitedUsers = new List<MemberDetails>();

                if (configObj == null)
                {

                    List<Member> memList = new List<Member>();
                    Member currentMember = new Member(memberId);

                    //string invitedValue = currentMember.GetProperty<string>("invitedFriends");

                    Entities dbEntity = new Entities();
                    List<MemberFriend> friendlist = dbEntity.MemberFriends.Where(member => member.MemberId == memberId && member.IsRemoved != true).ToList();

                    if (friendlist.Count > 0)
                    {

                        foreach (MemberFriend member in friendlist)
                        {

                            try
                            {
                                Member newm = new Member(Convert.ToInt32(member.FriendId));
                                if (newm != null)
                                {
                                    memList.Add(newm);
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                        List<MemberPledges> lstMemberPledges = Common.GetAllMemberPledges();

                        foreach (Member m in memList)
                        {
                            List<MemberPledges> thisMemberPledges = new List<MemberPledges>();
                            MemberDetails member = new MemberDetails();
                            member.MemberId = m.Id;
                            member.FullName = m.GetProperty<string>("firstName") + " " + m.GetProperty<string>("lastName");
                            member.DisplayName = m.GetProperty<string>("displayName");
                            member.FacebookId = m.GetProperty<string>("facebookId");

                            if (lstMemberPledges != null && lstMemberPledges.Count > 0)
                            {
                                //Bind profile picture as image added to first plage of this user
                                thisMemberPledges = lstMemberPledges.FindAll(obj => obj.MemberId == member.MemberId);
                                if (thisMemberPledges != null && thisMemberPledges.Count > 0)
                                {
                                    var firstPledge = thisMemberPledges.OrderBy(obj => obj.CreatedDate).FirstOrDefault();
                                    if (firstPledge != null && firstPledge.PledgeId > 0)
                                    {
                                        string profilePictureSrc = !string.IsNullOrEmpty(firstPledge.ImageUrl) ? firstPledge.ImageUrl : "http://placehold.it/70x70";
                                        member.ProfilePic = profilePictureSrc;
                                    }
                                }

                            }

                            InvitedUsers.Add(member);



                        }
                        configObj = InvitedUsers;
                    }

                }
                return configObj as List<MemberDetails>;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetFacebookMembers() method failed during execution. Stack Trace: " + ex.StackTrace);
                return null;
            }
        }

        [HttpPost]
        public JsonResult RemoveFriend(int Id)
        {
            var status = false;
            try
            {

                Entities dbEntity = new Entities();

                var memberId = Member.GetCurrentMember().Id;

                var friendToRemove1 = dbEntity.MemberFriends.First(member => member.MemberId == memberId && member.FriendId == Id);
                var friendToRemove2 = dbEntity.MemberFriends.First(member => member.MemberId == Id && member.FriendId == memberId);
                friendToRemove1.IsRemoved = true;
                friendToRemove2.IsRemoved = true;
                dbEntity.SaveChanges();
                status = true;


            }
            catch (Exception ex)
            {

                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : RemoveFriend() method executed successfully, something went wrong while sorting the results in GetHomePageAmbassadorData() method."
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);


            }
            var result = new
            {
                status = status

            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #region getmemberpledges
        private const string PledgeCachePrefix = "RexonaAUSFBPledges";
        public JsonResult Getpledges(string CurrentMemberId)
        {
            try
            {

                object configObj = null;

                List<MemberPledges> lstMemberPledges = Common.GetAllMemberPledges();
                List<MemberPledges> thisMemberPledges = new List<MemberPledges>();

                List<FBMemberPledgeDetails> resultList = new List<FBMemberPledgeDetails>();

                int memberId = Convert.ToInt32(CurrentMemberId);
                if (lstMemberPledges != null && lstMemberPledges.Count > 0)
                {
                    //Bind profile picture as image added to first plage of this user
                    thisMemberPledges = lstMemberPledges.FindAll(obj => obj.MemberId == memberId);

                    foreach (MemberPledges memberPledge in thisMemberPledges)
                    {
                        //Node pledgeMember = new Node(memberPledge.PledgeId);
                        if (memberPledge.PledgeId > 0)
                        {
                            //get parent node for pledge details 
                            Node pledge = new Node(memberPledge.ParentId);
                            FBMemberPledgeDetails mpledge = new FBMemberPledgeDetails();
                            mpledge.PledgeId = pledge.Id;
                            mpledge.Title = String.IsNullOrEmpty(pledge.GetProperty<string>("title")) ? pledge.Name : pledge.GetProperty<string>("title");
                            mpledge.PledgeUrl = pledge.NiceUrl;
                            mpledge.Members = pledge.ChildrenAsList.Where(a => a.GetProperty<bool>("step3Clear")).ToList().Count;
                            mpledge.Type = pledge.GetProperty<string>("publicPledgeSelection") == "1" ? "Open" : "Closed";

                            resultList.Add(mpledge);


                        }
                    }
                    configObj = resultList;
                }


                var result = new
                {
                    pledges = configObj as List<FBMemberPledgeDetails>

                };

                return Json(result, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : Dashboard Facebook Getpledges() method failed during execution. Stack Trace: " + ex.StackTrace);
                return null;
            }
        }
        #endregion
        #endregion

        #region invite feature
        public JsonResult getQuerystring(string memberId, string title, string pledgeId, string pledgeType, bool fbInvite,string type)
        {
            try
            {

                FbInviteObject fb = new FbInviteObject();
                fb.MemberId = Convert.ToInt32(memberId);
                fb.PledgeId = Convert.ToInt32(pledgeId);
                fb.Title = title;
                fb.PledgeType = pledgeType;
                fb.FbInvite = fbInvite;
                fb.LinkType = type;
                

                if (type.Equals("email", StringComparison.OrdinalIgnoreCase))
                {
                    fb.LinkId = AddInvite(type,false);
                }
                JavaScriptSerializer js = new JavaScriptSerializer();
                string res = js.Serialize(fb);

                string encquerystring = Common.PasswordTokenHelper.Encrypt(res);

                var jresult = new
                {
                    querystring = encquerystring

                };

                return Json(jresult, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException ex)
            {
               
                var errorMessages = ex.EntityValidationErrors
                                    .SelectMany(x => x.ValidationErrors)
                                   .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS: SocialContentReporting() Twitter Push Content - " + exceptionMessage);
                return Json(new { querystring = "error" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : getQuerystring() method executed successfully, something went wrong while sorting the results in GetHomePageAmbassadorData() method."
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(new { querystring = "error" }, JsonRequestBehavior.AllowGet);
            }
        }

        public int AddInvite(string type,bool IsUsed)
        {
            Entities dbEntity = new Entities();
            int lastRowId = dbEntity.PledgeInviteDatas.ToList().LastOrDefault() == null ? 0 : dbEntity.PledgeInviteDatas.ToList().LastOrDefault().Id;
            
            dbEntity.PledgeInviteDatas.Add(new PledgeInviteData()
            {
                LinkId = lastRowId + 1,
                IsUsed = IsUsed,
                Type = type,
                InvitedDate = DateTime.Now
            });

            if (dbEntity.SaveChanges() > 0)
            {

            }

            return lastRowId + 1;
        }

        public JsonResult decodeQuerystring(string queryString)
        {
            try
            {
                var IsloggedIn = false;
                string decquerystring = Common.PasswordTokenHelper.Decrypt(queryString);
                JavaScriptSerializer js = new JavaScriptSerializer();
                FbInviteObject resultObj = js.Deserialize<FbInviteObject>(decquerystring);

                Session["LinkId"] = resultObj.LinkId;
                if (resultObj.LinkId > 0)
                {
                    Entities db = new Entities();
                    int found = db.PledgeInviteDatas.Where(i => i.LinkId == resultObj.LinkId && i.IsUsed == true).ToList().Count;
                    if (found > 0)
                    {
                        return Json(new { details = false, IsLoggedIn = "error" }, JsonRequestBehavior.AllowGet);
                    }
                }
                Session["LinkType"] = resultObj.LinkType;
                Session["FBInvite"] = true;
                Session["InvitingMember"] = resultObj.MemberId;
                Session["InvitedPledgeId"] = resultObj.PledgeId;
                Session["InvitedPledgeTitle"] = resultObj.Title;
                Session["InvitedPledgeType"] = resultObj.PledgeType;
                Member member = Member.GetCurrentMember();
                if (member != null)
                {

                    IsloggedIn = true;
                }
                var jresult = new
                {
                    details = resultObj,
                    IsLoggedIn = IsloggedIn

                };

                return Json(jresult, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : decodeQuerystring() method executed successfully, something went wrong while sorting the results in GetHomePageAmbassadorData() method."
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(new { details = "error", IsLoggedIn = "error" }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult SendInviteEmail(string memberName, string pledgeTitle, string emailAddress, string message, string URLtoShare)
        {
            bool status = false;
            try
            {
                string subject = "Rexona I Will Do - Pledge Invitation";
                var mailBody = "<p><b>" + memberName + "</b> invited you to a pledge : <b>" + pledgeTitle + ".</b></p><br/>Click on this link to join the pledge : <a href='" + URLtoShare + "'>" + URLtoShare + "</a></br><br/>" + message;
                status = Common.SendEmail(emailAddress, subject, mailBody, false);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : Recommend() method executed successfully, something went wrong while sorting the results in GetHomePageAmbassadorData() method."
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);

            }
            var jresult = new
            {
                message = status

            };

            return Json(jresult, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region recommend an article
        [HttpPost]
        public JsonResult GetPledgesForRecommend(string CurrentMemberId, int ArticleId)
        {
            try
            {

                object configObj = null;
                Member currentMember = Member.GetCurrentMember();
                if (currentMember == null)
                {

                }
                else
                {

                    List<MemberPledges> lstMemberPledges = Common.GetAllMemberPledges();
                    List<MemberPledges> thisMemberPledges = new List<MemberPledges>();

                    List<FBMemberPledgeDetails> resultList = new List<FBMemberPledgeDetails>();

                    int memberId = Convert.ToInt32(CurrentMemberId);

                    Entities dbEntity = new Entities();
                    var recommendedArticlePledges = dbEntity.RecommendedArticlePledges.Where(obj => obj.ArticleId == ArticleId).Select(pledge => pledge.PledgeId);

                    if (lstMemberPledges != null && lstMemberPledges.Count > 0)
                    {
                        //Bind profile picture as image added to first plage of this user
                        thisMemberPledges = lstMemberPledges.FindAll(obj => obj.MemberId == memberId);

                        foreach (MemberPledges memberPledge in thisMemberPledges)
                        {
                            //Node pledgeMember = new Node(memberPledge.PledgeId);
                            if (memberPledge.PledgeId > 0)
                            {
                                //get parent node for pledge details 
                                Node pledge = new Node(memberPledge.ParentId);
                                FBMemberPledgeDetails mpledge = new FBMemberPledgeDetails();
                                mpledge.PledgeId = pledge.Id;
                                mpledge.Title = String.IsNullOrEmpty(pledge.GetProperty<string>("title")) ? pledge.Name : pledge.GetProperty<string>("title");
                                mpledge.PledgeUrl = pledge.NiceUrl;
                                mpledge.Members = pledge.ChildrenAsList.Where(a => a.GetProperty<bool>("step3Clear")).ToList().Count;
                                mpledge.Type = pledge.GetProperty<string>("publicPledgeSelection") == "1" ? "Open" : "Closed";

                                if (recommendedArticlePledges.Contains(mpledge.PledgeId))
                                {
                                    mpledge.IsRecommended = true;
                                }
                                else
                                {
                                    mpledge.IsRecommended = false;
                                }
                                if (mpledge.Members > 1)
                                {
                                    resultList.Add(mpledge);
                                }


                            }
                        }

                    }

                    configObj = resultList.FindAll(m => m.IsRecommended == false);
                }
                var result = new
                {
                    pledges = configObj as List<FBMemberPledgeDetails>

                };

                return Json(result, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : Dashboard Facebook Getpledges() method failed during execution. Stack Trace: " + ex.StackTrace);
                return null;
            }
        }

        [HttpPost]
        public JsonResult Recommend(string pledgeIds, string articleId)
        {
            try
            {

                List<string> pledgesToRecommend = new List<string>(pledgeIds.Split(','));
                var result = false;
                Entities dbEntity = new Entities();


                foreach (string pledge in pledgesToRecommend)
                {
                    bool alreadyRecommended = dbEntity.RecommendedArticlePledges.ToList().Exists(record => record.PledgeId == Convert.ToInt32(pledge) && record.ArticleId == Convert.ToInt32(articleId));

                    if (!alreadyRecommended)
                    {
                        dbEntity.RecommendedArticlePledges.Add(new RecommendedArticlePledge()
                        {
                            PledgeId = Convert.ToInt32(pledge),
                            ArticleId = Convert.ToInt32(articleId),
                            CreatedDateTime = DateTime.Now
                        });
                    }
                }
                if (dbEntity.SaveChanges() > 0)
                    result = true;

                var json = new { message = result };
                return Json(json, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : Recommend() method executed successfully, something went wrong while sorting the results in GetHomePageAmbassadorData() method."
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(new { message = false }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
    }
}