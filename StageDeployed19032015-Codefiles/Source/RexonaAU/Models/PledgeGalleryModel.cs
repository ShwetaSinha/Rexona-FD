using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RexonaAU.Models
{
    public class PledgeGalleryModel
    {
        public int Id { get; set; }
        public string ImageURL { get; set; }
        public int PledgeNodeId { get; set; }
        public int MemberCount { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public string IsPublicSelection { get; set; }
        public int LikeCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsMember { get; set; }
        public string PledgeURL { get; set; }
        public bool Step3Clear { get; set; }
        public string PledgeTitle { get; set; }
    }
}