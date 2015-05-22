using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using umbraco.cms.businesslogic.member;
using umbraco;
using umbraco.NodeFactory;
using Umbraco.Core.Logging;
using RexonaAU.Models;
using Umbraco.Core.Services;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RexonaAU.Helpers;

namespace RexonaAU.Controllers
{
    public class EventController : Umbraco.Web.Mvc.SurfaceController
    {
        private const string CachePrefix = "RexonaAUEvent_{0}";

        [HttpPost]
        public JsonResult CreateEvent(EventModel newEvent)
        {
            string message = "Success";
            try
            {
                Member currentmember = Member.GetCurrentMember();

                string EventTitle = string.Empty;

                string capitalValues = string.Empty;

                var contentService = Services.ContentService;
                if (currentmember != null)
                {
                    if (newEvent.EventId == 0)
                    {
                        //create new event

                        capitalValues = ConfigurationManager.AppSettings[newEvent.State];
                        newEvent.CapitalCity = capitalValues.Split(',')[0];
                        newEvent.CapitalPostCode = capitalValues.Split(',')[1];

                        //if special characters
                        EventTitle = newEvent.EventTitle.Trim().Replace(" ", "_");
                        EventTitle = newEvent.EventTitle.Trim().Replace("#", "_");
                        EventTitle = newEvent.EventTitle.Trim().Replace("@", "_");
                        EventTitle = Regex.Replace(newEvent.EventTitle, "[^0-9a-zA-Z.]+", "_");
                        //end

                      
                        //create event node with all details
                        var createdEvent = contentService.CreateContent(Common.ReplaceSpecialChar(EventTitle), uQuery.GetNodesByType("Events").FirstOrDefault().Id, "Event", 0);

                        createdEvent.SetValue("eventTitle", newEvent.EventTitle);
                        //newEvent.EventDescription
                        createdEvent.SetValue("eventDescription", newEvent.EventDescription);
                        createdEvent.SetValue("startDate", newEvent.StartDate);
                        createdEvent.SetValue("endDate", newEvent.EndDate);
                        createdEvent.SetValue("eventLocation", newEvent.EventLocation);

                        createdEvent.SetValue("capitalCity", newEvent.CapitalCity);
                        createdEvent.SetValue("capitalPostCode", newEvent.CapitalPostCode);
                        createdEvent.SetValue("state", newEvent.State);

                        createdEvent.SetValue("postCode", newEvent.PostCode);
                        createdEvent.SetValue("startTime", newEvent.StartTime);
                        createdEvent.SetValue("endTime", newEvent.EndTime);
                        createdEvent.SetValue("isDeleted", false);
                        createdEvent.SetValue("createdBy", currentmember.Id);
                        createdEvent.SetValue("pledgeURL", newEvent.EventURL);
                        contentService.SaveAndPublish(createdEvent);
                        newEvent.EventId = createdEvent.Id;

                        //create event member node
                        var eventMember = contentService.CreateContent(Common.ReplaceSpecialChar(currentmember.GetProperty<string>("displayName")), newEvent.EventId, "EventMember", 0);
                        if (eventMember != null)
                        {
                            eventMember.SetValue("memberId", currentmember.Id);
                            eventMember.SetValue("isJoined", true);
                            // eventMember.SetValue("isOwner", true);
                            eventMember.SetValue("joinedDate", DateTime.Now.ToString("dd/MM/yyyy"));
                            contentService.SaveAndPublish(eventMember);
                        }
                    }
                    else if (newEvent.EventId > 0)
                    {
                        //update event


                        capitalValues = ConfigurationManager.AppSettings[newEvent.State];
                        newEvent.CapitalCity = capitalValues.Split(',')[0];
                        newEvent.CapitalPostCode = capitalValues.Split(',')[1];

                        var updateEvent = contentService.GetById(newEvent.EventId);
                        if (updateEvent != null)
                        {
                            updateEvent.SetValue("eventTitle", newEvent.EventTitle);
                            //newEvent.EventDescription
                            updateEvent.SetValue("eventDescription", newEvent.EventDescription);
                            updateEvent.SetValue("startDate", newEvent.StartDate);
                            updateEvent.SetValue("endDate", newEvent.EndDate);
                            updateEvent.SetValue("eventLocation", newEvent.EventLocation);

                            updateEvent.SetValue("capitalCity", newEvent.CapitalCity);
                            updateEvent.SetValue("capitalPostCode", newEvent.CapitalPostCode);
                            updateEvent.SetValue("state", newEvent.State);

                            updateEvent.SetValue("postCode", newEvent.PostCode);
                            updateEvent.SetValue("startTime", newEvent.StartTime);
                            updateEvent.SetValue("endTime", newEvent.EndTime);
                            updateEvent.SetValue("isDeleted", false);
                            updateEvent.SetValue("createdBy", currentmember.Id);

                            
                           // updateEvent.SetValue("pledgeURL", newEvent.EventURL);

                            contentService.SaveAndPublish(updateEvent);

                        }

                    }
                    umbraco.library.UpdateDocumentCache(uQuery.GetNodesByType("Events").FirstOrDefault().Id);
                }
            }
            catch (Exception ex)
            {

                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "CreateEvent Failed "
                   + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                message = "error";
            }
            finally
            {
                umbraco.library.RefreshContent();
            }

            return Json(message, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteEvent(int EventId)
        {
            bool status = false;

            var service = new ContentService();
            var entry = service.GetById(EventId);
            try
            {
                Node node = new Node(EventId);
                if (node != null && EventId > 0)
                {
                    entry.SetValue("isDeleted", true);
                    service.SaveAndPublish(entry);
                    status = true;
                }
                return Json(new { status = status, message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "DeleteEvent Failed "
                + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return Json(new { status = false, message = "Something went wrong" }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                umbraco.library.RefreshContent();
            }
        }

        [HttpGet]
        public JsonResult GetEvents()
        {
            int CacheMinutes = 0;
            if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                CacheMinutes = 30;
            string cacheKey = string.Format(CachePrefix, string.Format("GetMyEvents{0}", 10));
            object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;
            var EventsNode = uQuery.GetNodesByType("Events").FirstOrDefault();
            string message = "Success";

            Member loggedMember = Member.GetCurrentMember();
            List<EventModel> eventsList = new List<EventModel>();

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

                                EventModel myEvent = new EventModel();

                                myEvent.CreatedDate = childNode.CreateDate;

                                myEvent.EventId = childNode.Id;
                                myEvent.EventTitle = childNode.GetProperty<string>("eventTitle");
                                myEvent.EventDescription = childNode.GetProperty<string>("eventDescription");
                                //myEvent.StartDate = String.IsNullOrEmpty(childNode.GetProperty<string>("startDate")) ? string.Empty : Convert.ToDateTime(childNode.GetProperty<string>("startDate")).ToString("D");

                                myEvent.State = childNode.GetProperty<string>("state");
                                myEvent.CapitalCity = childNode.GetProperty<string>("capitalCity");
                                myEvent.CapitalPostCode = childNode.GetProperty<string>("capitalPostCode");

                                myEvent.StartDate = String.IsNullOrEmpty(childNode.GetProperty<string>("startDate")) ? string.Empty : formatDate(childNode.GetProperty<string>("startDate"));
                                myEvent.EndDate = String.IsNullOrEmpty(childNode.GetProperty<string>("endDate")) ? string.Empty : formatDate(childNode.GetProperty<string>("endDate"));
                                myEvent.EventLocation = childNode.GetProperty<string>("eventLocation");
                                myEvent.PostCode = childNode.GetProperty<string>("postCode");
                                myEvent.StartTime = childNode.GetProperty<string>("startTime");
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
                                myEvent.OwnerId = childNode.GetProperty<int>("createdBy");
                                myEvent.EventURL = childNode.GetProperty<string>("pledgeURL");
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

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "GetMyEvents Failed "
                 + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return Json(new { status = false, message = "Something went wrong" }, JsonRequestBehavior.AllowGet);
            }
        }

        [NonAction]
        public string formatDate(string startDate)
        {
            string formattedDate = string.Empty;
            DateTime newDate = new DateTime();
            DateTime.TryParseExact(startDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out newDate);
            string day = string.Empty;
            int dayOfWeek = 0;
            int.TryParse(newDate.ToString("dd"), out dayOfWeek);
            try
            {
                switch (dayOfWeek)
                {
                    case 1:
                    case 21:
                    case 31:
                        day = dayOfWeek + "st";
                        break;
                    case 2:
                    case 22:
                        day = dayOfWeek + "nd";
                        break;
                    case 3:
                    case 23:
                        day = dayOfWeek + "rd";
                        break;
                    default:
                        day = dayOfWeek + "th";
                        break;
                }

            }
            catch (Exception ex)
            {

                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "formatDate Failed "
                + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
            }

            //newDate = newDate.Replace(Convert.ToString(dayOfWeek), day);
            formattedDate = day + Convert.ToDateTime(newDate).ToString(" MMMM yyyy");
            return formattedDate;
        }

        [HttpPost]
        public JsonResult GetEventDetails(int EventId, int IsEdit)
        {
            var service = new ContentService();
            var entry = service.GetById(EventId);
            try
            {
                Member loggedMember = Member.GetCurrentMember();
                EventModel myEvent = new EventModel();
                Node node = new Node(EventId);
                if (node != null && EventId > 0)
                {

                    if (IsEdit == 0)
                    {

                        myEvent.EventTitle = node.GetProperty<string>("eventTitle");
                        myEvent.EventDescription = node.GetProperty<string>("eventDescription");
                        //myEvent.StartDate = String.IsNullOrEmpty(childNode.GetProperty<string>("startDate")) ? string.Empty : Convert.ToDateTime(childNode.GetProperty<string>("startDate")).ToString("D");

                        myEvent.StartDate = String.IsNullOrEmpty(node.GetProperty<string>("startDate")) ? string.Empty : formatDate(node.GetProperty<string>("startDate"));
                        myEvent.EndDate = String.IsNullOrEmpty(node.GetProperty<string>("endDate")) ? string.Empty : formatDate(node.GetProperty<string>("endDate"));
                        myEvent.EventLocation = node.GetProperty<string>("eventLocation");
                        myEvent.PostCode = node.GetProperty<string>("postCode");
                        myEvent.StartTime = node.GetProperty<string>("startTime");
                        myEvent.EndTime = node.GetProperty<string>("endTime");
                        if (loggedMember != null)
                        {
                            myEvent.IsJoined = loggedMember.Id != 0 ? node.ChildrenAsList.Where(a => a.GetProperty<int>("memberId") == loggedMember.Id && a.GetProperty<bool>("isJoined")).Count() > 0 : false;
                            myEvent.IsOwner = node.GetProperty<int>("createdBy") == loggedMember.Id ? true : false;
                        }
                        else
                        {
                            myEvent.IsJoined = false;
                            myEvent.IsOwner = false;
                        }
                        myEvent.State = node.GetProperty<string>("state");
                        myEvent.EventURL = String.IsNullOrEmpty(node.NiceUrl) ? string.Empty : node.NiceUrl;
                        myEvent.MyStatus = myEvent.IsJoined ? "Attending" : "Not Attending";
                        myEvent.EventId = EventId;
                    }
                    else
                    {
                        myEvent.EventTitle = node.GetProperty<string>("eventTitle");
                        myEvent.EventDescription = node.GetProperty<string>("eventDescription");
                        //myEvent.StartDate = String.IsNullOrEmpty(childNode.GetProperty<string>("startDate")) ? string.Empty : Convert.ToDateTime(childNode.GetProperty<string>("startDate")).ToString("D");

                        myEvent.StartDate = String.IsNullOrEmpty(node.GetProperty<string>("startDate")) ? string.Empty : node.GetProperty<string>("startDate");
                        myEvent.EndDate = String.IsNullOrEmpty(node.GetProperty<string>("endDate")) ? string.Empty : node.GetProperty<string>("endDate");
                        myEvent.EventLocation = node.GetProperty<string>("eventLocation");
                        myEvent.PostCode = node.GetProperty<string>("postCode");
                        myEvent.StartTime = node.GetProperty<string>("startTime");
                        myEvent.EndTime = node.GetProperty<string>("endTime");
                        if (loggedMember != null)
                        {
                            myEvent.IsJoined = loggedMember.Id != 0 ? node.ChildrenAsList.Where(a => a.GetProperty<int>("memberId") == loggedMember.Id && a.GetProperty<bool>("isJoined")).Count() > 0 : false;
                            myEvent.IsOwner = node.GetProperty<int>("createdBy") == loggedMember.Id ? true : false;
                        }
                        else
                        {
                            myEvent.IsJoined = false;
                            myEvent.IsOwner = false;
                        }
                        myEvent.State = node.GetProperty<string>("state");
                        myEvent.EventURL = String.IsNullOrEmpty(node.NiceUrl) ? string.Empty : node.NiceUrl;
                        myEvent.MyStatus = myEvent.IsJoined ? "Attending" : "Not Attending";
                        myEvent.EventId = EventId;
                    }
                }
                return Json(new { status = true, result = myEvent }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "GetEventDetails Failed "
                + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return Json(new { status = false, message = "Something went wrong" }, JsonRequestBehavior.AllowGet);

            }
        }

        [HttpPost]
        public JsonResult GetEventsNearMe(string Postcode)
        {
            var EventsNode = uQuery.GetNodesByType("Events").FirstOrDefault();
            List<EventModel> eventsList = new List<EventModel>();
            Member loggedMember = Member.GetCurrentMember();
            try
            {
                foreach (var childNode in EventsNode.ChildrenAsList)
                {
                    if (!childNode.GetProperty<bool>("isDeleted") && childNode.GetProperty<string>("postCode") == Postcode)
                    {
                        var childItems = childNode.ChildrenAsList;
                        EventModel myEvent = new EventModel();
                        myEvent.EventId = childNode.Id;
                        myEvent.EventTitle = childNode.GetProperty<string>("eventTitle");
                        myEvent.EventDescription = childNode.GetProperty<string>("eventDescription");
                        //myEvent.StartDate = String.IsNullOrEmpty(childNode.GetProperty<string>("startDate")) ? string.Empty : Convert.ToDateTime(childNode.GetProperty<string>("startDate")).ToString("D");

                        myEvent.StartDate = String.IsNullOrEmpty(childNode.GetProperty<string>("startDate")) ? string.Empty : formatDate(childNode.GetProperty<string>("startDate"));
                        myEvent.EndDate = childNode.GetProperty<string>("endDate");
                        myEvent.EventLocation = childNode.GetProperty<string>("eventLocation");
                        myEvent.PostCode = childNode.GetProperty<string>("postCode");
                        myEvent.StartTime = childNode.GetProperty<string>("startTime");
                        myEvent.EndTime = childNode.GetProperty<string>("endTime");
                        myEvent.IsJoined = loggedMember.Id != 0 ? childNode.ChildrenAsList.Where(a => a.GetProperty<int>("memberId") == loggedMember.Id).Count() > 0 : false;
                        myEvent.IsOwner = childNode.GetProperty<int>("createdBy") == loggedMember.Id ? true : false;
                        eventsList.Add(myEvent);
                    }
                }

                return Json(new { status = true, result = eventsList }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "CreateEvent Failed "
              + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return Json(new { status = false, message = "Something went wrong" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult LeaveEvent(int EventId)
        {
            bool status = false;
            try
            {
                Member currentmember = Member.GetCurrentMember();
                if (currentmember != null && currentmember.Id > 0)
                {
                    var service = new ContentService();
                    Node entry = new Node(EventId);
                    if (entry != null)
                    {
                        int JoinedMemberId = entry.ChildrenAsList.Where(member => member.GetProperty<int>("memberId") == currentmember.Id).FirstOrDefault().Id;
                        if (JoinedMemberId > 0)
                        {
                            var JoinedMember = service.GetById(JoinedMemberId);
                            if (JoinedMember != null)
                            {
                                JoinedMember.SetValue("isJoined", false);
                                JoinedMember.SetValue("leftDate", DateTime.Now.ToString("dd-MM-yyy"));
                                service.SaveAndPublish(JoinedMember);
                                status = true;
                            }
                        }
                    }
                }
                return Json(new { status = status, message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Leave Event Failed "
              + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return Json(new { status = false, message = "Something went wrong" }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                umbraco.library.RefreshContent();
            }
        }

        [HttpPost]
        public JsonResult AttendEvent(int EventId)
        {
            bool status = false;
            try
            {
                Member currentmember = Member.GetCurrentMember();
                var contentService = Services.ContentService;
                if (currentmember != null && currentmember.Id > 0 && EventId > 0)
                {
                    var service = new ContentService();
                    //check if Member Already 

                    Node entry = new Node(EventId);
                    if (entry != null)
                    {
                        int JoinedMemberId = 0;
                        var JoinedMemberList = entry.ChildrenAsList.Where(member => member.GetProperty<int>("memberId") == currentmember.Id).ToList();
                        if (JoinedMemberList != null && JoinedMemberList.Count > 0)
                        {
                            JoinedMemberId = JoinedMemberList.FirstOrDefault().Id;

                            if (JoinedMemberId > 0)
                            {
                                var JoinedMember = service.GetById(JoinedMemberId);
                                if (JoinedMember != null)
                                {
                                    JoinedMember.SetValue("isJoined", true);
                                    JoinedMember.SetValue("joinedDate", DateTime.Now.ToString("dd-MM-yyyy"));
                                    JoinedMember.SetValue("leftDate", string.Empty);
                                    service.SaveAndPublish(JoinedMember);
                                    status = true;
                                }
                            }
                        }
                        else
                        {
                            //if not create new member 
                            var eventMember = contentService.CreateContent(Common.ReplaceSpecialChar(currentmember.GetProperty<string>("displayName")), EventId, "EventMember", 0);
                            if (eventMember != null)
                            {

                                eventMember.SetValue("memberId", currentmember.Id);
                                eventMember.SetValue("isJoined", true);
                                eventMember.SetValue("joinedDate", DateTime.Now.ToString("dd-MM-yyyy"));
                                eventMember.SetValue("leftDate", string.Empty);
                                contentService.SaveAndPublish(eventMember);
                                status = true;
                            }
                        }

                    }
                }
                return Json(new { status = status, message = "Success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "AttendEvent Failed "
             + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return Json(new { status = false, message = "Something went wrong" }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}