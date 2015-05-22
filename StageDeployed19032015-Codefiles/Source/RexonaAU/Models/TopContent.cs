using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RexonaAU.Models
{
    public class TopContent 
    {
        public long Id { get; set; }
        public string Source { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }
        public int Likes { get; set; }
        public string MediaURL { get; set; }
        public int JoinedPeoples { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Style { get; set; }
        public string LinkUrl { get; set; }
        public string ArticleTitle { get; set; }
        public string Excerpt { get; set; }
        public bool PublicPledge { get; set; }
        public bool IsOwner { get; set; }
        public bool IsMember { get; set; }
        public bool Step3Clear { get; set; }
        public string CategoryTag { get; set; }
    }
}
