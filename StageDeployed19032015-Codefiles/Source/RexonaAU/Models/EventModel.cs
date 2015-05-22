using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RexonaAU.Models
{
    public class EventModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string EventLocation { get; set; }
       
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public bool IsOwner { get; set; }
        public int OwnerId { get; set; }
        public bool IsJoined { get; set; }
        public string EventDescription { get; set; }
        public string EventURL { get; set; }
        public string MyStatus { get; set; }
        public string CapitalCity { get; set; }
        public string PostCode { get; set; }
        public string CapitalPostCode { get; set; }
        public string State { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}