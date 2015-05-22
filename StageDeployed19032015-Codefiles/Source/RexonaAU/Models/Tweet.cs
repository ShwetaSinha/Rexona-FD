using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RexonaAU.Models
{
    public class SocialContent
    {
        public long Id { get; set; }
        public string TweetText { get; set; }
        public string Author { get; set; }
        public string ProfilePic { get; set; }
        public string ScreenNameResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public long FriendCount { get; set; }
        public string TweetURL { get; set; }
        public string MediaURL { get; set; }
        public string ProfileURL { get; set; }
        public string source { get; set; }
        public int Likes { get; set; }
        public string DateToDisplay { get; set; }
    }
}