using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RexonaAU.Models
{
    public class MemberDetails
    {
        public int MemberId { get; set; }
        public string DisplayName { get; set; }
        public string FullName { get; set; }
        public string FacebookId { get; set; }
        public string ProfilePic { get; set; }
    }

    public class FBMemberPledgeDetails
    {
        
        public int PledgeId { get; set; }
        public string Title { get; set; }
        public int Members { get; set; }
        public string PledgeUrl { get; set; }
        public string Type { get; set; }
        public bool IsRecommended { get; set; }
    }

    public class FbInviteObject
    {
        public int PledgeId { get; set; }
        public int MemberId { get; set; }
        public string Title { get; set; }
        public string PledgeType { get; set; }
        public bool FbInvite { get; set; }
        public int LinkId { get; set; }
        public string LinkType { get; set; }
    }
}