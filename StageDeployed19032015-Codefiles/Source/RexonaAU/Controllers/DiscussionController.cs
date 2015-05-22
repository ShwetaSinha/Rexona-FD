using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using RexonaAU.Models;
using umbraco.MacroEngines;
using umbraco.cms.businesslogic.Tags;
using Umbraco.Core.Persistence;
using umbraco;
using umbraco.NodeFactory;
using Umbraco.Core.Logging;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using Examine;
using UmbracoExamine;
using Examine.SearchCriteria;
using umbraco.cms.businesslogic.member;
using System.Net;
using System.Text;
using Umbraco.Core.Services;
using umbraco.presentation.LiveEditing;
using Gibe.Umbraco.AmazonFileSystemProvider;
using System.Configuration;
using RexonaAU.Helpers;
using System.Globalization;

namespace RexonaAU.Controllers
{
    public class DiscussionController : Umbraco.Web.Mvc.SurfaceController
    {
        #region Post Discussions/Follow/MyPost
        [HttpGet]
        public JsonResult CreateDiscussion(string discussionTitle, string discussionBody, int pledgeId)
        {
            string message = "success";
            int newpledgeId = pledgeId;
            try
            {
                if (pledgeId > 0)
                {
                    Member currentmember = Member.GetCurrentMember();
                    if (currentmember != null)
                    {
                        //Get Master dicussion ID  PledgeDiscussion
                        int MasterDiscussionID = uQuery.GetNodesByType("PledgeDiscussionsMaster").FirstOrDefault().Id;
                        if (MasterDiscussionID > 0)
                        {
                            Node MasterDiscussionNode = new Node(MasterDiscussionID);
                            if (MasterDiscussionNode != null)
                            {
                                var contentService = Services.ContentService;
                                if (MasterDiscussionNode.Children.Count > 0 && MasterDiscussionNode.ChildrenAsList.Where(a => a.Name == Convert.ToString(pledgeId)).ToList().Count > 0)
                                {
                                    pledgeId = MasterDiscussionNode.ChildrenAsList.Where(a => a.Name == Convert.ToString(pledgeId)).Select(a => a.Id).ToList()[0];
                                }
                                else
                                {
                                    //Create folder first                                
                                    var masterDiscussion = contentService.CreateContent(Convert.ToString(pledgeId), MasterDiscussionID, "PledgeDiscussions", 0);
                                    contentService.SaveAndPublish(masterDiscussion);
                                    pledgeId = masterDiscussion.Id;
                                }

                                //Create discussion
                                string newDiscussionTitle = discussionTitle.Length > 20 ? discussionTitle.Substring(0, 20) + ".." : discussionTitle;
                                var discussion = contentService.CreateContent(Common.ReplaceSpecialChar(newDiscussionTitle), pledgeId, "PledgeDiscussion", 0);
                                contentService.SaveAndPublish(discussion);
                                discussion.SetValue("discussionTitle", discussionTitle);
                                discussion.SetValue("discussionDescription", discussionBody);//createdById
                                discussion.SetValue("createdById", currentmember.Id);
                                contentService.SaveAndPublish(discussion);

                                //Add member to follower table
                                List<Discussion> lstDiscussions = new List<Discussion>();                              

                                lstDiscussions = CreateDiscussionListObject(newpledgeId, "AllDiscussions");
                                List<int> lstDiscussionsId = lstDiscussions.Where(a => a.PostedById == currentmember.Id && a.Title == discussionTitle).Select(a => a.ID).ToList<int>();

                                if (currentmember.Id > 0)
                                {
                                    if (lstDiscussionsId.Count > 0)
                                    {
                                        Entities dbEntities = new Entities();

                                        dbEntities.DiscussionFollowers.Add(new DiscussionFollower()
                                        {
                                            CreatedDateTime = DateTime.Now,
                                            DiscussionId = lstDiscussionsId.ToList()[0],
                                            MemberId = currentmember.Id,
                                            LastReadDateTime = DateTime.Now
                                        });

                                        if (!(dbEntities.SaveChanges() > 0))
                                        {
                                            message = "error";
                                        }
                                    }
                                }


                                //To get all members of the pledge
                                List<MemberPledges> lstMemberPledges = Common.GetAllMemberPledges();
                                lstMemberPledges = lstMemberPledges.FindAll(a => a.ParentId == newpledgeId && a.MemberId != currentmember.Id && a.IsDeleted==false).ToList();

                                if (lstMemberPledges.Count > 0)
                                {
                                    for (int i = 0; i < lstMemberPledges.Count; i++)
                                    {
                                        Entities dbEntities = new Entities();

                                        dbEntities.DiscussionFollowers.Add(new DiscussionFollower()
                                        {
                                            CreatedDateTime = DateTime.Now,
                                            DiscussionId = lstDiscussionsId.ToList()[0],
                                            MemberId = lstMemberPledges.Select(a => a.MemberId).ToList()[i],
                                            LastReadDateTime = DateTime.Now
                                        });

                                        if (!(dbEntities.SaveChanges() > 0))
                                        {
                                            message = "error";
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : CreateDiscussion() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                message = "error";
            }

            var result = new
            {
                Message = message
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult FollowDiscussion(int discussionId, int memberId)
        {
            string message = "success";

            try
            {
                if (memberId > 0)
                {
                    if (discussionId > 0)
                    {
                        Entities dbEntities = new Entities();

                        dbEntities.DiscussionFollowers.Add(new DiscussionFollower()
                        {
                            CreatedDateTime = DateTime.Now,
                            DiscussionId = discussionId,
                            MemberId = memberId,
                            LastReadDateTime = DateTime.Now
                        });

                        if (!(dbEntities.SaveChanges() > 0))
                        {
                            message = "error";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : FollowDiscussion() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                message = "error";
            }

            var result = new
            {
                Message = message
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetDiscussions(int PageSize, int currentPageIndex, int pledgeId)
        {
            string message = "error";
            List<Discussion> lstDiscussions = new List<Discussion>();
            try
            {
                lstDiscussions = CreateDiscussionListObject(pledgeId, "AllDiscussions");

                if (lstDiscussions != null && lstDiscussions.Count > 0)
                {
                    lstDiscussions = lstDiscussions.OrderByDescending(a => a.PostedByDate).ToList();
                    message = "success";
                    var result = new
                    {
                        Discussions = lstDiscussions,//.Skip(currentPageIndex * PageSize).Take(PageSize),
                        totalPages = Math.Ceiling((decimal)(lstDiscussions.Count / (decimal)PageSize)),
                        Message = message
                    };

                    return Json(result, JsonRequestBehavior.AllowGet);
                }

                return Json(new { Discussions = lstDiscussions, Message = message }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetDiscussions() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(new { Message = message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult MyDiscussions(int PageSize, int currentPageIndex, int pledgeId)
        {
            string message = "error";
            List<Discussion> lstDiscussions = new List<Discussion>(), MyDiscussions = new List<Discussion>();
            try
            {
                lstDiscussions = CreateDiscussionListObject(pledgeId, "MyDiscussions");
                if (lstDiscussions != null && lstDiscussions.Count() > 0)
                {
                    List<int> MyDiscussionIds = new List<int>();
                    Member currentmember = Member.GetCurrentMember();
                    if (currentmember != null)
                    {
                        MyDiscussions = lstDiscussions.Where(a => a.PostedById == currentmember.Id).OrderByDescending(a => a.PostedByDate).ToList();
                        MyDiscussionIds = lstDiscussions.Where(a => a.PostedById == currentmember.Id).Select(a => a.ID).ToList<int>();
                    }

                    if (MyDiscussions != null && MyDiscussions.Count > 0)
                    {
                        message = "success";
                        var result = new
                        {
                            MyDiscussions = MyDiscussions.Skip(currentPageIndex * PageSize).Take(PageSize),
                            MyDiscussionIds = MyDiscussionIds,
                            totalPages = Math.Ceiling((decimal)(MyDiscussions.Count / (decimal)PageSize)),
                            Message = message
                        };

                        return Json(result, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { Message = message }, JsonRequestBehavior.AllowGet);
                    }
                }

                return Json(new { Message = message }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : MyDiscussions() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(new { Message = message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult MyFollowedDiscussions(int PageSize, int currentPageIndex, int pledgeId, int memberId)
        {
            string message = "error";
            Entities dbEntities = new Entities();
            List<Discussion> lstDiscussions = new List<Discussion>();
            List<MyDiscussion> MyFollowedDiscussions = new List<MyDiscussion>();
            try
            {
                lstDiscussions = CreateDiscussionListObject(pledgeId, "MyFollowedDiscussions");
                if (lstDiscussions != null && lstDiscussions.Count() > 0)
                {

                    if (memberId > 0)
                    {
                        var followedDiscussions = dbEntities.DiscussionFollowers.Where(a => a.MemberId == memberId).Select(a => new
                            {
                                DiscussionId = a.DiscussionId,
                                CreatedDate = a.CreatedDateTime
                            }).ToList();

                        List<long> discussionIds = dbEntities.DiscussionFollowers.Where(a => a.MemberId == memberId).Select(a => new
                            {
                                DiscussionId = a.DiscussionId
                            }).Select(a => a.DiscussionId).ToList<long>();


                        if (discussionIds != null && discussionIds.Count > 0)
                        {
                            foreach (var discussion in lstDiscussions)
                            {
                                MyDiscussion myDiscussion = new MyDiscussion();
                                if (discussionIds.Contains(discussion.ID))
                                {
                                    myDiscussion.ID = discussion.ID;
                                    myDiscussion.Title = discussion.Title;
                                    myDiscussion.PostedBy = discussion.PostedBy;
                                    myDiscussion.PostedByAvatar = discussion.PostedByAvatar;
                                    myDiscussion.PostedByDate = discussion.PostedByDate;
                                    myDiscussion.PostedById = discussion.PostedById;
                                    myDiscussion.PostedDateTimeAsString = discussion.PostedDateTimeAsString;
                                    myDiscussion.FollowedDateTime = followedDiscussions.Where(a => a.DiscussionId == discussion.ID).Select(a => a.CreatedDate).ToList()[0];
                                    MyFollowedDiscussions.Add(myDiscussion);
                                }
                            }

                            if (MyFollowedDiscussions != null && MyFollowedDiscussions.Count > 0)
                            {
                                message = "success";
                                MyFollowedDiscussions = MyFollowedDiscussions.OrderByDescending(a => a.FollowedDateTime).ToList();
                                var result = new
                                {
                                    MyFollowedDiscussions = MyFollowedDiscussions.Skip(currentPageIndex * PageSize).Take(PageSize),
                                    FollowedDiscussionIds = discussionIds,
                                    totalPages = Math.Ceiling((decimal)(MyFollowedDiscussions.Count / (decimal)PageSize)),
                                    Message = message
                                };

                                return Json(result, JsonRequestBehavior.AllowGet);
                            }
                            else
                            {
                                return Json(new { Message = message }, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }


                }

                return Json(new { Message = message }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : MyDiscussions() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return Json(new { Message = message }, JsonRequestBehavior.AllowGet);
            }
        }


        private const string CachePrefix = "RexonaAUSDiscussion_{0}";
        private static object lockObj = new object();
        /// <summary>
        /// Common method to create discussion list for a pledge
        /// </summary>
        /// <param name="pledgeId"></param>
        /// <returns></returns>
        /// 
        public List<Discussion> CreateDiscussionListObject(int pledgeId, string cacheSuffix)
        {
            try
            {
                int CacheMinutes = 0;
                if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                    CacheMinutes = 30;

                string cacheKey = string.Format(CachePrefix, string.Format("GetDiscussionDetailsByPledgeId_{0}_{1}", pledgeId, cacheSuffix));
                object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;

                if (configObj == null)
                {
                    lock (lockObj)
                    {
                        int MasterDiscussionID = uQuery.GetNodesByType("PledgeDiscussionsMaster").FirstOrDefault().Id;
                        if (MasterDiscussionID > 0)
                        {
                            try
                            {
                                Node MasterDiscussionNode = new Node(MasterDiscussionID);
                                if (MasterDiscussionNode != null)
                                {
                                    if (MasterDiscussionNode.ChildrenAsList.Where(a => a.Name == Convert.ToString(pledgeId)).Count() > 0)
                                    {
                                        int nodeId = MasterDiscussionNode.ChildrenAsList.Where(a => a.Name == Convert.ToString(pledgeId)).Select(a => a.Id).ToList()[0];
                                        Node pledgeDiscussionNode = new Node(nodeId);
                                        if (pledgeDiscussionNode != null && pledgeDiscussionNode.Children.Count > 0)
                                        {
                                            List<Discussion> lstDiscussions = new List<Discussion>();
                                            List<MemberPledges> lstMemberPledges = Common.GetAllMemberPledges();
                                            foreach (var childNode in pledgeDiscussionNode.ChildrenAsList)
                                            {
                                                bool isMemberExist = true;
                                                Discussion discussion = new Discussion();
                                                discussion.ID = childNode.Id;
                                                discussion.Title = childNode.GetProperty<string>("discussionTitle");
                                                discussion.Description = childNode.GetProperty<string>("discussionDescription");
                                                int createdById = childNode.GetProperty<int>("createdById");
                                                discussion.PostedById = createdById;
                                                discussion.PostedByDate = childNode.CreateDate;
                                                discussion.PostedDateTimeAsString = childNode.CreateDate.ToString("dd MMM yyyy");
                                                int replycount = Common.GetDiscussionNotification(discussion.ID);
                                                discussion.Repliescount = replycount;
                                                
                                                if (createdById > 0)
                                                {
                                                    try
                                                    {
                                                        Member member = new Member(createdById);
                                                        bool has_name = member.HasProperty("firstName") && member.HasProperty("lastName");
                                                        if (!String.IsNullOrEmpty(member.GetProperty<string>("firstName")) && !String.IsNullOrEmpty(member.GetProperty<string>("lastName")))
                                                        {
                                                            //discussion.PostedBy = !string.IsNullOrEmpty(member.Text) ? member.Text : has_name ? member.GetProperty<string>("firstName") + " " + member.GetProperty<string>("lastName") : string.Empty;
                                                            discussion.PostedBy = member.GetProperty<string>("firstName") + " " + member.GetProperty<string>("lastName");
                                                        }
                                                        else
                                                        {
                                                            discussion.PostedBy = member.Email;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        isMemberExist = false;
                                                    }
                                                }

                                                if (lstMemberPledges != null && lstMemberPledges.Count > 0)
                                                {
                                                    List<MemberPledges> thisMemberPledges = lstMemberPledges.FindAll(obj => obj.MemberId == createdById);
                                                    if (thisMemberPledges != null && thisMemberPledges.Count > 0)
                                                    {
                                                        var firstPledge = thisMemberPledges.OrderBy(obj => obj.CreatedDate).FirstOrDefault();
                                                        if (firstPledge != null && firstPledge.PledgeId > 0)
                                                        {
                                                            discussion.PostedByAvatar = !string.IsNullOrEmpty(firstPledge.ImageUrl) ? firstPledge.ImageUrl : "http://placehold.it/50x50";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        discussion.PostedByAvatar = "http://placehold.it/50x50";
                                                    }
                                                }

                                                if (isMemberExist)
                                                {
                                                    lstDiscussions.Add(discussion);
                                                }
                                            }


                                            configObj = lstDiscussions;
                                            if (configObj != null && System.Web.HttpContext.Current != null)
                                            {
                                                System.Web.HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                                                 System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                    
                }

                return configObj as List<Discussion>;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : MyDiscussions() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }
        public List<Discussion_Dashboard> CreateDiscussionListObject_DisccusionTab(int pledgeId, string cacheSuffix)
        {
            try
            {
                int CacheMinutes = 0;
                if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                    CacheMinutes = 30;

                string cacheKey = string.Format(CachePrefix, string.Format("GetDiscussionDetailsByPledgeId_{0}_{1}", pledgeId, cacheSuffix));
                object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;

                if (configObj == null)
                {
                    lock (lockObj)
                    {
                        int MasterDiscussionID = uQuery.GetNodesByType("PledgeDiscussionsMaster").FirstOrDefault().Id;
                        if (MasterDiscussionID > 0)
                        {
                            try
                            {
                                Node MasterDiscussionNode = new Node(MasterDiscussionID);
                                if (MasterDiscussionNode != null)
                                {
                                    if (MasterDiscussionNode.ChildrenAsList.Where(a => a.Name == Convert.ToString(pledgeId)).Count() > 0)
                                    {
                                        int nodeId = MasterDiscussionNode.ChildrenAsList.Where(a => a.Name == Convert.ToString(pledgeId)).Select(a => a.Id).ToList()[0];
                                        Node pledgeDiscussionNode = new Node(nodeId);
                                        if (pledgeDiscussionNode != null && pledgeDiscussionNode.Children.Count > 0)
                                        {
                                            List<Discussion_Dashboard> lstDiscussions = new List<Discussion_Dashboard>();
                                            List<MemberPledges> lstMemberPledges = Common.GetAllMemberPledges();
                                            foreach (var childNode in pledgeDiscussionNode.ChildrenAsList)
                                            {
                                                bool isMemberExist = true;
                                                Discussion_Dashboard discussion = new Discussion_Dashboard();
                                                discussion.ID = childNode.Id;
                                                discussion.DB_Title = childNode.GetProperty<string>("discussionTitle");
                                                discussion.DB_Description = childNode.GetProperty<string>("discussionDescription");
                                                int createdById = childNode.GetProperty<int>("createdById");
                                                discussion.DB_PostedById = createdById;
                                                discussion.DB_PostedByDate = childNode.CreateDate;
                                                discussion.DB_PostedDateTimeAsString = childNode.CreateDate.ToString("dd MMM yyyy");
                                                int replycount=DiscussionRepliesCount(discussion.ID);
                                                discussion.DB_Repliescount = replycount;
                                                
                                                if (createdById > 0)
                                                {
                                                    try
                                                    {
                                                        Member member = new Member(createdById);
                                                        bool has_name = member.HasProperty("firstName") && member.HasProperty("lastName");
                                                        discussion.DB_PostedBy = !string.IsNullOrEmpty(member.Text) ? member.Text : has_name ? member.GetProperty<string>("firstName") + " " + member.GetProperty<string>("lastName") : string.Empty;
                                                    }
                                                    catch
                                                    {
                                                        isMemberExist = false;
                                                    }
                                                }

                                                if (lstMemberPledges != null && lstMemberPledges.Count > 0)
                                                {
                                                    List<MemberPledges> thisMemberPledges = lstMemberPledges.FindAll(obj => obj.MemberId == createdById);
                                                    if (thisMemberPledges != null && thisMemberPledges.Count > 0)
                                                    {
                                                        var firstPledge = thisMemberPledges.OrderBy(obj => obj.CreatedDate).FirstOrDefault();
                                                        if (firstPledge != null && firstPledge.PledgeId > 0)
                                                        {
                                                            discussion.DB_PostedByAvatar = !string.IsNullOrEmpty(firstPledge.ImageUrl) ? firstPledge.ImageUrl : "http://placehold.it/50x50";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        discussion.DB_PostedByAvatar = "http://placehold.it/50x50";
                                                    }
                                                }

                                                if (isMemberExist)
                                                {
                                                    lstDiscussions.Add(discussion);
                                                }
                                            }


                                            configObj = lstDiscussions;
                                            if (configObj != null && System.Web.HttpContext.Current != null)
                                            {
                                                System.Web.HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                                                 System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                    
                }

                return configObj as List<Discussion_Dashboard>;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : MyDiscussions() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }

        public class Discussion
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public string PostedBy { get; set; }
            public int PostedById { get; set; }
            public DateTime PostedByDate { get; set; }
            public string PostedDateTimeAsString { get; set; }
            public string PostedByAvatar { get; set; }
            public string Description { get; set; }
            public int Repliescount { get; set; }
        }

        public class Discussion_Dashboard
        {
            public int ID { get; set; }
            public string DB_Title { get; set; }
            public string DB_PostedBy { get; set; }
            public int DB_PostedById { get; set; }
            public DateTime DB_PostedByDate { get; set; }
            public string DB_PostedDateTimeAsString { get; set; }
            public string DB_PostedByAvatar { get; set; }
            public string DB_Description { get; set; }
            public int DB_Repliescount { get; set; }
        }
        public class MyDiscussion : Discussion
        {
            public DateTime? FollowedDateTime { get; set; }
        }

        #endregion

        #region Discussion Details/ Replies

        [HttpGet]
        public JsonResult DiscussionDetails(int DiscussionId)
        {
            string message = "success";

            try
            {
                if (DiscussionId > 0)
                {
                    string DiscussionTitle = string.Empty, DiscussionDescription = string.Empty, CreatedDate = string.Empty, CreatedBy = string.Empty;
                    int CreatedById = 0;
                    Node discussionNode = new Node(DiscussionId);
                    List<Reply> lstReplies = new List<Reply>();
                    if (discussionNode != null)
                    {
                        DiscussionTitle = discussionNode.GetProperty<string>("discussionTitle");
                        DiscussionDescription = discussionNode.GetProperty<string>("discussionDescription");

                        CreatedById = discussionNode.GetProperty<int>("createdById");
                        if (CreatedById > 0)
                        {
                            CreatedBy = GetMembername(CreatedById);
                        }

                        CreatedDate = discussionNode.CreateDate.ToString("dd/MM/yyyy hh:mmtt");

                        foreach (var childnode in discussionNode.ChildrenAsList)
                        {
                            Reply reply = new Reply();
                            reply.Id = childnode.Id;
                            reply.PostedDate = childnode.CreateDate;
                            reply.PostedDateAsString = childnode.CreateDate.ToString("dd/MM/yyyy hh:mmtt");
                            if (childnode.GetProperty<int>("createdById") > 0)
                            {
                                reply.PostedBy = GetMembername(childnode.GetProperty<int>("createdById"));
                            }
                            reply.ReplyText = childnode.GetProperty<string>("discussionReply");

                            List<Reply> lstChildReplies = new List<Reply>();
                            foreach (var child in childnode.ChildrenAsList)
                            {
                                Reply childReply = new Reply();
                                childReply.Id = child.Id;
                                childReply.PostedDate = child.CreateDate;
                                childReply.PostedDateAsString = child.CreateDate.ToString("dd/MM/yyyy hh:mmtt");
                                if (child.GetProperty<int>("createdById") > 0)
                                {
                                    childReply.PostedBy = GetMembername(child.GetProperty<int>("createdById"));
                                }
                                childReply.ReplyText = child.GetProperty<string>("discussionReply");
                                lstChildReplies.Add(childReply);

                                List<Reply> lst3rdChildReplies = new List<Reply>();
                                foreach (var ThirdLevelChild in child.ChildrenAsList)
                                {
                                    Reply ThirdChildReply = new Reply();
                                    ThirdChildReply.Id = ThirdLevelChild.Id;
                                    ThirdChildReply.PostedDate = ThirdLevelChild.CreateDate;
                                    ThirdChildReply.PostedDateAsString = ThirdLevelChild.CreateDate.ToString("dd/MM/yyyy hh:mmtt");
                                    if (ThirdLevelChild.GetProperty<int>("createdById") > 0)
                                    {
                                        ThirdChildReply.PostedBy = GetMembername(ThirdLevelChild.GetProperty<int>("createdById"));
                                    }
                                    ThirdChildReply.ReplyText = ThirdLevelChild.GetProperty<string>("discussionReply");

                                    List<Reply> lstFourthChildReplies = new List<Reply>();
                                    foreach (var FourthLevelChild in ThirdLevelChild.ChildrenAsList)
                                    {
                                        Reply FourthChildReply = new Reply();
                                        FourthChildReply.Id = FourthLevelChild.Id;
                                        FourthChildReply.PostedDate = FourthLevelChild.CreateDate;
                                        FourthChildReply.PostedDateAsString = FourthLevelChild.CreateDate.ToString("dd/MM/yyyy hh:mmtt");
                                        if (FourthLevelChild.GetProperty<int>("createdById") > 0)
                                        {
                                            FourthChildReply.PostedBy = GetMembername(FourthLevelChild.GetProperty<int>("createdById"));
                                        }
                                        FourthChildReply.ReplyText = FourthLevelChild.GetProperty<string>("discussionReply");

                                        //Changes done to fetch all other level responses
                                        List<umbraco.interfaces.INode> otherReplies = FourthLevelChild.ChildrenAsList;
                                        Session.Remove("OtherChildList");
                                        List<Reply> lstOtherReplies = GetOtherLevelReplies(otherReplies, 1);
                                        if (lstOtherReplies != null && lstOtherReplies.Count > 0)
                                        {
                                            FourthChildReply.OtherLevelReplies = lstOtherReplies;
                                        }
                                        lstFourthChildReplies.Add(FourthChildReply);
                                    }
                                    if (lstFourthChildReplies != null && lstFourthChildReplies.Count > 0)
                                    {
                                        lstFourthChildReplies = lstFourthChildReplies.OrderBy(a => a.PostedDate).ToList();
                                        ThirdChildReply.SecondLevelReplies = lstFourthChildReplies;
                                    }

                                    lst3rdChildReplies.Add(ThirdChildReply);
                                }
                                if (lst3rdChildReplies != null && lst3rdChildReplies.Count > 0)
                                {
                                    lst3rdChildReplies = lst3rdChildReplies.OrderBy(a => a.PostedDate).ToList();
                                    childReply.SecondLevelReplies = lst3rdChildReplies;
                                }
                            }

                            if (lstChildReplies != null && lstChildReplies.Count > 0)
                            {
                                lstChildReplies = lstChildReplies.OrderBy(a => a.PostedDate).ToList();
                                reply.SecondLevelReplies = lstChildReplies;
                            }

                            lstReplies.Add(reply);
                        }

                        if (lstReplies != null && lstReplies.Count > 0)
                        {
                            lstReplies = lstReplies.OrderBy(a => a.PostedDate).ToList();
                        }

                        //Update last read date time if member is logged in and if user is following this discussion
                        if (Member.IsLoggedOn() && Member.GetCurrentMember().Id > 0)
                        {

                            Entities dbEntity = new Entities();

                            var memberId = Member.GetCurrentMember().Id;

                            var recordToUpdate = dbEntity.DiscussionFollowers.FirstOrDefault(record => record.MemberId == memberId && record.DiscussionId == discussionNode.Id);
                            if (recordToUpdate != null && recordToUpdate.DiscussionId > 0)
                            {
                                recordToUpdate.LastReadDateTime = DateTime.Now;
                                dbEntity.SaveChanges();
                            }
                            
                            recordToUpdate = dbEntity.DiscussionFollowers.FirstOrDefault(record => record.MemberId == memberId && record.DiscussionId == discussionNode.Id);
                            

                        }
                    }


                    var result = new
                    {
                        DiscussionTitle = DiscussionTitle,
                        DiscussionDescription = DiscussionDescription,
                        Replies = lstReplies,
                        CreatedBy = CreatedBy,
                        CreatedDate = CreatedDate
                    };

                    return Json(result, JsonRequestBehavior.AllowGet);


                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : DiscussionDetails() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                message = "error";
            }

            return null;
        }

        public List<Reply> GetOtherLevelReplies(List<umbraco.interfaces.INode> otherList, int level)
        {
            List<Reply> returnList = new List<Reply>();

            if (Session["OtherChildList"] != null)
            {
                returnList = Session["OtherChildList"] as List<Reply>;
            }

            if (otherList != null && otherList.Count > 0)
            {
                foreach (var child in otherList)
                {
                    Reply reply = new Reply();
                    reply.Id = child.Id;
                    reply.PostedDate = child.CreateDate;
                    reply.PostedDateAsString = child.CreateDate.ToString("dd/MM/yyyy hh:mmtt");
                    if (child.GetProperty<int>("createdById") > 0)
                    {
                        reply.PostedBy = GetMembername(child.GetProperty<int>("createdById"));
                    }
                    reply.ReplyText = child.GetProperty<string>("discussionReply");
                    reply.level = level;
                    returnList.Add(reply);
                    Session["OtherChildList"] = returnList;
                    GetOtherLevelReplies(child.ChildrenAsList, level + 1);
                }
            }

            return returnList;
        }

        [HttpGet]
        public JsonResult GetSharedArticles(int PageSize, int currentPageIndex, int pledgeId,string pledgeTag)
        {
            string Message = "error";
            Entities dbEntities = new Entities();            
            List<int> lstArticles = new List<int>();
            List<ArticleEntry> recommendedArticles = new List<ArticleEntry>();

            try
            {
                //Pass 5 as key to have a separate cache object
                List<ArticleEntry> lstAllArticles = GetAllArticles(5);

                if (lstAllArticles != null && lstAllArticles.Count > 0)
                {
                    
                    recommendedArticles = lstAllArticles.Where(a => a.Tags.Contains(pledgeTag)).ToList();
                    
                    
                    List<int> allarticles = new List<int>();
                    allarticles=lstAllArticles.Where(a => a.Tags.Contains(pledgeTag)).Select(a=>Convert.ToInt32(a.Id)).ToList();
                    //select article ids for recommended articles
                    IQueryable<int> articleId = dbEntities.RecommendedArticlePledges.Where(obj => obj.PledgeId == pledgeId)
                           .Select(obj => obj.ArticleId);

                    if (articleId.Count() > 0)
                    {
                        if (!String.IsNullOrEmpty(pledgeTag))
                        {
                            allarticles.AddRange(articleId.Except(allarticles));
                            lstArticles.AddRange(allarticles);
                        }
                        else
                        {
                            lstArticles.AddRange(articleId);
                        }
                        recommendedArticles = lstAllArticles.Where(obj => lstArticles.Contains(Convert.ToInt32(obj.Id))).ToList();

                        //if (recommendedArticles != null && recommendedArticles.Count() > 0)
                        //{
                        //    lstRecommenededArticles.AddRange(recommendedArticles);
                        //}
                    }

                    if (recommendedArticles != null && recommendedArticles.Count > 0)
                    {
                        recommendedArticles = recommendedArticles.OrderBy(a => a.UploadDateAsDateTime).ToList();
                    }
                }
                var result = new
                {
                    Message = "success",
                    SharedArticles = recommendedArticles,
                    //SharedArticles = recommendedArticles.Skip(currentPageIndex * PageSize).Take(PageSize),
                   // totalPages = Math.Ceiling((decimal)(recommendedArticles.Count / (decimal)PageSize))
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetSharedArticles() method not executed successfully: "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return base.Json("GetSharedArticles, Please try again", "text/plain");
            }
        }

        [NonAction]
        private List<ArticleEntry> GetAllArticles(int key)
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
                                    article.ArticleTitle = children.GetProperty<string>("articleTitle");
                                    article.Hearts = String.IsNullOrEmpty(children.GetProperty<string>("like")) ? 0 : Convert.ToInt32(children.GetProperty("like").Value);
                                    article.Excerpt = Convert.ToString(children.GetProperty("excerpt").Value);
                                    article.ArticlepostedDate = Convert.ToString(children.CreateDate.ToString("dd MMM yyyy")); ;
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


                                    article.AmbassadorName = children.GetProperty<string>("articleAuthor");
                                    article.Tags = children.GetProperty<string>("articleTag");
                                    article.Type = Convert.ToString(children.GetProperty<string>("articleType"));
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

        public string GetMembername(int memberId)
        {
            string MemberName = string.Empty;
            if (memberId > 0)
            {
                Member member = new Member(memberId);
                bool has_name = member.HasProperty("firstName") && member.HasProperty("lastName");

                if (has_name)
                    MemberName = member.GetProperty<string>("firstName") + " " + member.GetProperty<string>("lastName");
                else
                    MemberName = "(No Name)";

                //MemberName = !string.IsNullOrEmpty(member.Text) ? member.Text : has_name ? member.GetProperty<string>("firstName") + " " + member.GetProperty<string>("lastName") : string.Empty;
            }

            return MemberName;
        }


        [HttpGet]
        public JsonResult AddReply(int ParentId, string ReplyText)
        {
            string message = "success";

            try
            {
                if (ParentId > 0)
                {
                    Member currentmember = Member.GetCurrentMember();
                    if (currentmember != null)
                    {
                        if (!string.IsNullOrEmpty(ReplyText))
                        {
                            Node MasterDiscussionNode = new Node(ParentId);
                            if (MasterDiscussionNode != null)
                            {
                                string replyTitle = ReplyText.Length > 10 ? ReplyText.Substring(0, 9) + "..." : ReplyText;
                                var contentService = Services.ContentService;
                                var reply = contentService.CreateContent(Common.ReplaceSpecialChar(replyTitle), ParentId, "PledgeDiscussionReply", 0);
                                contentService.SaveAndPublish(reply);
                                reply.SetValue("discussionReply", ReplyText);
                                reply.SetValue("createdById", currentmember.Id);
                                contentService.SaveAndPublish(reply);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : DiscussionDetails() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                message = "error";
            }

            var result = new
            {
                Message = message
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }


        public class Reply
        {
            public int Id { get; set; }
            public string ReplyText { get; set; }
            public DateTime PostedDate { get; set; }
            public string PostedDateAsString { get; set; }
            public string PostedBy { get; set; }
            public List<Reply> SecondLevelReplies { get; set; }
            public List<Reply> OtherLevelReplies { get; set; }
            public int level { get; set; }
        }

        public int DiscussionRepliesCount(int DiscussionId)
        {
            try
            {
                int count = 0;
                if (DiscussionId > 0)
                {
                    string DiscussionTitle = string.Empty, DiscussionDescription = string.Empty, CreatedDate = string.Empty, CreatedBy = string.Empty;
                    int CreatedById = 0;
                    Node discussionNode = new Node(DiscussionId);
                    List<Reply> lstReplies = new List<Reply>();
                    if (discussionNode != null)
                    {
                        foreach (var childnode in discussionNode.ChildrenAsList)
                        {
                            Reply reply = new Reply();
                            reply.Id = childnode.Id;

                            List<Reply> lstChildReplies = new List<Reply>();
                            foreach (var child in childnode.ChildrenAsList)
                            {
                                Reply childReply = new Reply();
                                childReply.Id = child.Id;
                                lstChildReplies.Add(childReply);
                                List<Reply> lst3rdChildReplies = new List<Reply>();
                                foreach (var ThirdLevelChild in child.ChildrenAsList)
                                {
                                    Reply ThirdChildReply = new Reply();
                                    ThirdChildReply.Id = ThirdLevelChild.Id;
                                    List<Reply> lstFourthChildReplies = new List<Reply>();
                                    foreach (var FourthLevelChild in ThirdLevelChild.ChildrenAsList)
                                    {
                                        Reply FourthChildReply = new Reply();
                                        FourthChildReply.Id = FourthLevelChild.Id;
                                        lstFourthChildReplies.Add(FourthChildReply);
                                        count = count + 1;
                                    }
                                    if (lstFourthChildReplies != null && lstFourthChildReplies.Count > 0)
                                    {
                                        lstFourthChildReplies = lstFourthChildReplies.OrderBy(a => a.PostedDate).ToList();
                                        ThirdChildReply.SecondLevelReplies = lstFourthChildReplies;
                                    }

                                    lst3rdChildReplies.Add(ThirdChildReply);
                                    count = count +1;

                                }
                                if (lst3rdChildReplies != null && lst3rdChildReplies.Count > 0)
                                {
                                    lst3rdChildReplies = lst3rdChildReplies.OrderBy(a => a.PostedDate).ToList();
                                    childReply.SecondLevelReplies = lst3rdChildReplies;
                                }
                                count = count + 1;
                            }

                            if (lstChildReplies != null && lstChildReplies.Count > 0)
                            {
                                lstChildReplies = lstChildReplies.OrderBy(a => a.PostedDate).ToList();
                                reply.SecondLevelReplies = lstChildReplies;
                            }

                            lstReplies.Add(reply);
                            count = count + 1;
                        }

                        if (lstReplies != null && lstReplies.Count > 0)
                        {
                            lstReplies = lstReplies.OrderBy(a => a.PostedDate).ToList();
                        }

                    }
                    return count;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : DiscussionDetails() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }

            return 0;
        }

        #endregion
        #region Get_Articles_Discussions_Events
        
        
        [HttpGet]
        public JsonResult GetAll(int currentPageIndex, int pledgeId, string pledgeTag)
        {
            string message = "error";
            try
            {
                //List<ADE> alltabs = new List<ADE>();
                List<ADE> alltabs_article = new List<ADE>();
                List<ADE> alltabs_Discussion = new List<ADE>(); 
                List<ADE> alltabs_event = new List<ADE>();
                List<ADE> alltab = new List<ADE>();
                
                alltabs_article =GetSharedArticles_all( currentPageIndex, pledgeId,pledgeTag);
                alltabs_Discussion =GetDiscussions_all( currentPageIndex, pledgeId);
                alltabs_event = GetEvents_all();
                
                //alltabs.AddRange(GetSharedArticles_all(PageSize, currentPageIndex, pledgeId));
                //alltabs.AddRange(GetDiscussions_all(PageSize, currentPageIndex, pledgeId));
                //alltabs.AddRange(GetEvents_all());
                article:
                if (alltabs_article!=null)
                {

                    for (int i = 0; i < alltabs_article.Count; i++)
                    {
                        if (alltabs_article[i].Typeof == "article" && !alltab.Contains(alltabs_article[i]))
                        {
                            alltab.Add(alltabs_article[i]);
                            goto disscussion;
                        }
                    }
                }
                disscussion:
                if (alltabs_Discussion != null)
                {
                    for (int j = 0; j < alltabs_Discussion.Count; j++)
                    {
                        if (alltabs_Discussion[j].Typeof == "Discussion" && !alltab.Contains(alltabs_Discussion[j]))
                        {
                            alltab.Add(alltabs_Discussion[j]);
                            goto events;
                        }
                    }
                }
                events:
                if (alltabs_event != null)
                {

                    for (int k = 0; k < alltabs_event.Count; k++)
                    {
                        if (alltabs_event[k].Typeof == "Event" && !alltab.Contains(alltabs_event[k]))
                        {
                            alltab.Add(alltabs_event[k]);
                            goto article;
                        }
                    }
                }

                int cnt_article = 0, cnt_discussion = 0, cnt_event = 0;
                cnt_article = alltab.Where(a => a.Typeof == "article").Count();
                cnt_discussion = alltab.Where(a => a.Typeof == "Discussion").Count();
                cnt_event = alltab.Where(a => a.Typeof == "Event").Count();
                if(alltabs_article!=null)
                    if (cnt_article < alltabs_article.Count)
                        goto article;
                if (alltabs_Discussion != null)
                    if (cnt_discussion < alltabs_Discussion.Count)
                        goto disscussion;
                if (alltabs_event != null)
                    if (cnt_event < alltabs_event.Count)
                        goto events;

                if (alltab.Count > 0)
                {
                    message = "success";
                }
                var result = new
                {
                    ADE_all = alltab,
                    //totalPages = Math.Ceiling((decimal)(alltab.Count / (decimal)PageSize)),
                    Message = message
                };



                return Json(result, JsonRequestBehavior.AllowGet); 

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetAll() method not executed successfully: "
                     + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return base.Json("GetAll, Please try again", "text/plain");
            }
            //return Json("", JsonRequestBehavior.AllowGet); 
        }
        [NonAction]
        private List<ADE> GetAllArticles_all(int key)
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


                List<ADE> userStories = new List<ADE>();

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

                                    ADE article = new ADE();
                                    article.ID = children.Id;
                                    article.UploadDateAsDateTime = children.CreateDate;
                                    //article.UploadedDateAsString = children.CreateDate.ToString("dd/MM/yyy");

                                    article.ActualArticleURL = children.NiceUrl;
                                    article.Title = children.GetProperty<string>("articleTitle");
                                    article.Hearts = String.IsNullOrEmpty(children.GetProperty<string>("like")) ? 0 : Convert.ToInt32(children.GetProperty("like").Value);
                                    article.Excerpt = Convert.ToString(children.GetProperty("excerpt").Value);
                                    article.PostedDate = Convert.ToString(children.CreateDate.ToString("dd MMM yyyy"));
                                    // ;
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


                                    article.AmbassadorName = children.GetProperty<string>("articleAuthor");
                                    article.Tags = children.GetProperty<string>("articleTag");
                                    article.Type = Convert.ToString(children.GetProperty<string>("articleType"));
                                    article.Typeof = "article";
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
                return configObj as List<ADE>;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetAllArticles() method failed during execution.: "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }
        public List<ADE> GetSharedArticles_all(int currentPageIndex, int pledgeId, string pledgeTag)
        {
            string Message = "error";
            Entities dbEntities = new Entities();
            List<int> lstArticles = new List<int>();
            List<ADE> recommendedArticles = new List<ADE>();


            try
            {
                //Pass 5 as key to have a separate cache object
                List<ADE> lstAllArticles = GetAllArticles_all(5);

                if (lstAllArticles != null && lstAllArticles.Count > 0)
                {
                    recommendedArticles = lstAllArticles.Where(a => a.Tags.Contains(pledgeTag)).ToList();


                    List<int> allarticles = new List<int>();
                    allarticles = lstAllArticles.Where(a => a.Tags.Contains(pledgeTag)).Select(a => Convert.ToInt32(a.ID)).ToList();
                    //select article ids for recommended articles
                    IQueryable<int> articleId = dbEntities.RecommendedArticlePledges.Where(obj => obj.PledgeId == pledgeId)
                           .Select(obj => obj.ArticleId);

                    if (articleId.Count() > 0)
                    {
                        if (!String.IsNullOrEmpty(pledgeTag))
                        {
                            allarticles.AddRange(articleId.Except(allarticles));
                            lstArticles.AddRange(allarticles);
                        }
                        else
                        {
                            lstArticles.AddRange(articleId);
                        }
                        recommendedArticles = lstAllArticles.Where(obj => lstArticles.Contains(Convert.ToInt32(obj.ID))).ToList();

                        //if (recommendedArticles != null && recommendedArticles.Count() > 0)
                        //{
                        //    lstRecommenededArticles.AddRange(recommendedArticles);
                        //}
                    }

                    if (recommendedArticles != null && recommendedArticles.Count > 0)
                    {
                        recommendedArticles = recommendedArticles.OrderBy(a => a.UploadDateAsDateTime).ToList();
                    }
                }
                //var result = new
                //{
                //    Message = "success",
                //    SharedArticles = lstRecommenededArticles.Skip(currentPageIndex * PageSize).Take(PageSize),
                //    totalPages = Math.Ceiling((decimal)(lstRecommenededArticles.Count / (decimal)PageSize))
                //};

                //return Json(result, JsonRequestBehavior.AllowGet);
                return recommendedArticles;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetSharedArticles() method not executed successfully: "
                    + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                //return base.Json("GetSharedArticles, Please try again", "text/plain");
                return null;
            }
        }

        public List<ADE> CreateDiscussionListObject_all(int pledgeId, string cacheSuffix)
        {
            try
            {
                int CacheMinutes = 0;
                if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                    CacheMinutes = 30;

                string cacheKey = string.Format(CachePrefix, string.Format("GetDiscussionDetailsByPledgeId_{0}_{1}", pledgeId, cacheSuffix));
                object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;

                if (configObj == null)
                {
                    lock (lockObj)
                    {
                        int MasterDiscussionID = uQuery.GetNodesByType("PledgeDiscussionsMaster").FirstOrDefault().Id;
                        if (MasterDiscussionID > 0)
                        {
                            try
                            {
                                Node MasterDiscussionNode = new Node(MasterDiscussionID);
                                if (MasterDiscussionNode != null)
                                {
                                    if (MasterDiscussionNode.ChildrenAsList.Where(a => a.Name == Convert.ToString(pledgeId)).Count() > 0)
                                    {
                                        int nodeId = MasterDiscussionNode.ChildrenAsList.Where(a => a.Name == Convert.ToString(pledgeId)).Select(a => a.Id).ToList()[0];
                                        Node pledgeDiscussionNode = new Node(nodeId);
                                        if (pledgeDiscussionNode != null && pledgeDiscussionNode.Children.Count > 0)
                                        {
                                            List<ADE> lstDiscussions = new List<ADE>();
                                            List<MemberPledges> lstMemberPledges = Common.GetAllMemberPledges();
                                            foreach (var childNode in pledgeDiscussionNode.ChildrenAsList)
                                            {
                                                bool isMemberExist = true;
                                                ADE discussion = new ADE();
                                                discussion.ID = childNode.Id;
                                                discussion.Title = childNode.GetProperty<string>("discussionTitle");
                                                discussion.Description = childNode.GetProperty<string>("discussionDescription");
                                                int createdById = childNode.GetProperty<int>("createdById");
                                                //discussion.PostedById = createdById;
                                                //discussion.PostedDate = childNode.CreateDate;
                                                discussion.PostedDate = childNode.CreateDate.ToString("dd MMM yyyy");
                                                int replycount = Common.GetDiscussionNotification(discussion.ID);
                                                discussion.Repliescount = replycount;
                                                discussion.Typeof = "Discussion";

                                                if (createdById > 0)
                                                {
                                                    try
                                                    {
                                                        Member member = new Member(createdById);
                                                        bool has_name = member.HasProperty("firstName") && member.HasProperty("lastName");
                                                        discussion.PostedBy = !string.IsNullOrEmpty(member.Text) ? member.Text : has_name ? member.GetProperty<string>("firstName") + " " + member.GetProperty<string>("lastName") : string.Empty;                                                        
                                                        if (has_name)
                                                            discussion.PostedBy = member.GetProperty<string>("firstName") + " " + member.GetProperty<string>("lastName");
                                                        else
                                                            discussion.PostedBy = "(No Name)";

                                                    }
                                                    catch
                                                    {
                                                        isMemberExist = false;
                                                    }
                                                }

                                                //if (lstMemberPledges != null && lstMemberPledges.Count > 0)
                                                //{
                                                //    List<MemberPledges> thisMemberPledges = lstMemberPledges.FindAll(obj => obj.MemberId == createdById);
                                                //    if (thisMemberPledges != null && thisMemberPledges.Count > 0)
                                                //    {
                                                //        var firstPledge = thisMemberPledges.OrderBy(obj => obj.CreatedDate).FirstOrDefault();
                                                //        if (firstPledge != null && firstPledge.PledgeId > 0)
                                                //        {
                                                //            discussion.PostedByAvatar = !string.IsNullOrEmpty(firstPledge.ImageUrl) ? firstPledge.ImageUrl : "http://placehold.it/50x50";
                                                //        }
                                                //    }
                                                //    else
                                                //    {
                                                //        discussion.PostedByAvatar = "http://placehold.it/50x50";
                                                //    }
                                                //}

                                                if (isMemberExist)
                                                {
                                                    lstDiscussions.Add(discussion);
                                                }
                                            }


                                            configObj = lstDiscussions;
                                            if (configObj != null && System.Web.HttpContext.Current != null)
                                            {
                                                System.Web.HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                                                 System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                }

                return configObj as List<ADE>;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : MyDiscussions() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }
        public List<ADE> GetDiscussions_all(int currentPageIndex, int pledgeId)
        {
            //string message = "error";
            List<ADE> lstDiscussions = new List<ADE>();
            try
            {
                lstDiscussions = CreateDiscussionListObject_all(pledgeId, "AllDiscussions");

                if (lstDiscussions != null && lstDiscussions.Count > 0)
                {
                    lstDiscussions = lstDiscussions.OrderByDescending(a => a.PostedDate).ToList();
                    //message = "success";
                    //var result = new
                    //{
                    //    Discussions = lstDiscussions.Skip(currentPageIndex * PageSize).Take(PageSize),
                    //    totalPages = Math.Ceiling((decimal)(lstDiscussions.Count / (decimal)PageSize)),
                    //    Message = message
                    //};

                    return lstDiscussions;
                    //return Json(result, JsonRequestBehavior.AllowGet);
                }
                return lstDiscussions;
                //return Json(new { Discussions = lstDiscussions, Message = message }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetDiscussions() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
               // return Json(new { Message = message }, JsonRequestBehavior.AllowGet);
                return lstDiscussions;
            }
        }
        public List<ADE> GetEvents_all()
        {
            int CacheMinutes = 0;
            if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                CacheMinutes = 30;
            string cacheKey = string.Format(CachePrefix, string.Format("GetMyEvents{0}", 10));
            object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;
            var EventsNode = uQuery.GetNodesByType("Events").FirstOrDefault();
            string message = "Success";

            Member loggedMember = Member.GetCurrentMember();
            List<ADE> eventsList = new List<ADE>();

            try
            {
                if (configObj == null)
                {
                    if (EventsNode != null && EventsNode.Id > 0)
                    {
                        foreach (var childNode in EventsNode.ChildrenAsList)
                        {
                            if (!childNode.GetProperty<bool>("isDeleted"))
                            {
                                var childItems = childNode.ChildrenAsList;
                                DateTime currentDate = DateTime.Now;

                                ADE myEvent = new ADE();

                                myEvent.CreatedDate = childNode.CreateDate;

                                myEvent.ID = childNode.Id;
                                myEvent.Title = childNode.GetProperty<string>("eventTitle");
                                myEvent.Description = childNode.GetProperty<string>("eventDescription");
                                //myEvent.StartDate = String.IsNullOrEmpty(childNode.GetProperty<string>("startDate")) ? string.Empty : Convert.ToDateTime(childNode.GetProperty<string>("startDate")).ToString("D");

                                //myEvent.State = childNode.GetProperty<string>("state");
                                //myEvent.CapitalCity = childNode.GetProperty<string>("capitalCity");
                                //myEvent.CapitalPostCode = childNode.GetProperty<string>("capitalPostCode");
                                EventController ec = new EventController();

                                myEvent.StartDate = String.IsNullOrEmpty(childNode.GetProperty<string>("startDate")) ? string.Empty : ec.formatDate(childNode.GetProperty<string>("startDate"));
                                //myEvent.EndDate = String.IsNullOrEmpty(childNode.GetProperty<string>("endDate")) ? string.Empty : formatDate(childNode.GetProperty<string>("endDate"));
                                myEvent.EventLocation = childNode.GetProperty<string>("eventLocation");
                                myEvent.PostCode = childNode.GetProperty<string>("postCode");
                                myEvent.Typeof = "Event";
                                //myEvent.StartTime = childNode.GetProperty<string>("startTime");
                                myEvent.EndTime = childNode.GetProperty<string>("endTime");
                                if (loggedMember != null)
                                {
                                    myEvent.IsJoined = loggedMember.Id != 0 ? childNode.ChildrenAsList.Where(a => a.GetProperty<int>("memberId") == loggedMember.Id && a.GetProperty<bool>("isJoined")).Count() > 0 : false;
                                    myEvent.IsOwner = childNode.GetProperty<int>("createdBy") == loggedMember.Id ? true : false;
                                }
                                else
                                {
                                    myEvent.IsJoined = false;
                                    myEvent.IsOwner = false;
                                }
                                //myEvent.OwnerId = childNode.GetProperty<int>("createdBy");
                                //myEvent.EventURL = childNode.GetProperty<string>("pledgeURL");
                                if (!String.IsNullOrEmpty(childNode.GetProperty<string>("endDate")))
                                {

                                    DateTime finalendDate = new DateTime();

                                    var endDate = childNode.GetProperty<string>("endDate") + ' ' + myEvent.EndTime;
                                    DateTime.TryParseExact(endDate, "dd-MM-yyyy h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.None, out finalendDate);
                                    if (finalendDate > currentDate)
                                    {
                                        eventsList.Add(myEvent);

                                    }
                                }
                                else
                                {
                                    eventsList.Add(myEvent);
                                }
                                configObj = eventsList;

                                if (configObj != null && System.Web.HttpContext.Current != null)
                                {
                                    System.Web.HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                                     System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                                }
                            }
                        }
                    }

                }

                var result = new
                {
                    events = eventsList.OrderByDescending(events => events.CreatedDate).ToList(),
                    status = true
                };
                return eventsList;
               // return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "GetMyEvents Failed "
                 + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
               // return Json(new { status = false, message = "Something went wrong" }, JsonRequestBehavior.AllowGet);
                return null;
            }
        }
        public class ADE
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string PostedBy { get; set; }
            public string PostedDate { get; set; }
            public int Repliescount { get; set; }
            public string PostCode { get; set; }
            public DateTime CreatedDate { get; set; }
            public string StartDate { get; set; }
            public string EndTime { get; set; }
            public bool IsJoined { get; set; }
            public bool IsOwner { get; set; }
            public string ActualArticleURL { get; set; }
            public string ArticleThumbnail { get; set; }
            public string Excerpt { get; set; }
            public string AmbassadorName { get; set; }
            public int Hearts { get; set; }
            public string Type { get; set; }
            public string Typeof { get; set; }
            public string EventLocation { get; set; }
            public string Tags { get; set; }
            public DateTime UploadDateAsDateTime { get; set; }

            
        #endregion
            
    }

    }

}
