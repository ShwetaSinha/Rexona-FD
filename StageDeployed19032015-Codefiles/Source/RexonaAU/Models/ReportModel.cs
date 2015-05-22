using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RexonaAU.Models
{
    public class ContentModel
    {
        public int AISArticles { get; set; }
        public int AISArticleViews { get; set; }
        public int AmbassadorArticles { get; set; }
        public int AmbassadorArticleViews { get; set; }
        public int DoMoreArticles { get; set; }
        public int DoMoreArticleViews { get; set; }
        public int InstaPosts { get; set; }
        public int Tweets { get; set; }
        public int SocialPosts { get; set; }
        public int AwaitingApproval { get; set; }
        public int ApprovedPosts { get; set; }
        public int RejectedPosts { get; set; }

    }
    public class TopArticles
    {
        public long Id { get; set; }
        public string ArticleName { get; set; }
        public int TotalViews { get; set; }
    }

    public class GoalsModel
    {
        public long TotalGoals { get; set; }
        public long UniqueGoals { get; set; }
        public long Events { get; set; }
        public long Discussions { get; set; }
        public decimal OpenGoals { get; set; }
        public decimal ClosedGoals { get; set; }
        public long MembersJoined { get; set; }
        //public string goalsUrl 
    }
    public class BucketModel
    {
        public long TotalGoals { get; set; }
        public long UniqueGoals { get; set; }
        public long Events { get; set; }
        public long Discussions { get; set; }
    }
    public class DropOffModel
    {
        public decimal SignUp { get; set; }
        public decimal EnterGoal { get; set; }
        public decimal TakePhoto { get; set; }
        public decimal HappyWithPhoto { get; set; }
        public decimal ConfirmGoal { get; set; }
    }
    public class UserModel
    {
        public long TotalUsers { get; set; }
        public long ActiveUsers { get; set; }
        public long InvitedUsers { get; set; }
        public long InviteAcceptedUsers { get; set; }
    }
    public class ReportBucketModel
    {
        public string CategoryTag { get; set; }
        public decimal PledgesCount { get; set; }
        public decimal PledgesCountPercentage { get; set; }
    }

    public class VideoModel
    {
        public long TotalPlays { get; set; }
        public long UniquePlays { get; set; }
        public int TotalVideos { get; set; }
    }
    public class TopVideos
    {
        public string VideoId { get; set; }
        public string VideoName { get; set; }
        public string VideoURL { get; set; }
        public int TotalPlays { get; set; }
        public int TotalStops { get; set; }
        public string AvgViews { get; set; }
        public int Likes { get; set; }
        public int Views { get; set; }
        public DateTime CreatedDate { get; set; }
        public int UniquePlays { get; set; }
    }
}

