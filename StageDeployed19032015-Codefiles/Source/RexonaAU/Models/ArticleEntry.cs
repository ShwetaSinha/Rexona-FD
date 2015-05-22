using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RexonaAU.Models
{
    public class ArticleEntry
    {
        public long Id { get; set; }
        public DateTime UploadDateAsDateTime { get; set; }
        public string UploadedDateAsString { get; set; }
        public string ArticleThumbnail { get; set; }
        public string ArticleTitle { get; set; }
        public int AmbassadorId { get; set; }
        public string AmbassadorName { get; set; }
        public int Hearts { get; set; }
        public string ActualArticleURL { get; set; }
        public string AmbassadorURL { get; set; }
        public string AmbassadorImage { get; set; }
        public string Type { get; set; }
        public string Excerpt { get; set; }
        public string Tags { get; set; }
        public string ArticlepostedDate { get; set; }

        /*Traction : RSS and eDM related properties. Do not delete*/
        public bool IncludeInEDM { get; set; }
        public DateTime MailOutDate { get; set; }
        public string ArticleCategorty { get; set; }
        public string ArticleBuckets { get; set; }
        public string ActualArticleURLWithDomain { get; set; }
        public string ArticleRSSCategory
        {
            get
            {
                if (!string.IsNullOrEmpty(this.ArticleBuckets))
                {
                    string[] buckets = this.ArticleBuckets.Split(',');
                    if (buckets != null && buckets.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(ArticleCategorty))
                        {
                            return buckets[0] + "/" + ArticleCategorty;
                        }
                        else
                            return string.Empty;
                    }
                    else
                        return string.Empty;
                }
                else
                    return string.Empty;
            }
        }
        /*End - Traction + RSS + eDM closed*/
    }
}