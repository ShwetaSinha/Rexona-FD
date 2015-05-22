using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RexonaAU.Models
{
    public class MemberPledgeDetails
    {
        public int PledgeId { get; set; }
        public int MemberId { get; set; }
        public bool IsLike { get; set; }
        //public int ID { get; set; }
    }

    public class MemberLikes
    {
        public int MemberId { get; set; }
        public int PledgeOrArticleId { get; set; }
        public bool IsLike { get; set; }
        public int ID { get; set; }
    }
    public class MemberPledges
    {
        public int PledgeId { get; set; }
        public int MemberId { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ImageUrl { get; set; }
        public string YouTubeUrl { get; set; }
        public bool Shared { get; set; }
        public int ParentId { get; set; }
        public bool IsDeleted { get; set; }
        //public int ID { get; set; }
    }

    public class DashboardDiscussion
    {
        public int Id { get; set; }
        public string DiscussionTitle { get; set; }
        public string DiscussionDescription { get; set; }        
        public string PostedBy { get; set; }
        public DateTime PostedDateTime { get; set; }
        public DateTime DiscussionFollowingDateTime { get; set; }
        public int UnreadCount { get; set; }
        public string LinkURL { get; set; }
        public bool isReply { get; set; }
    }
}