using RexonaAU.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web.Models;

namespace RexonaAU.Controllers
{
    public class RSSFeedsController : Umbraco.Web.Mvc.RenderMvcController
    {
        //
        // GET: /RSS/

        [HttpGet]
        public ActionResult Index()
        {

            var lstArticles = Common.ArticlesHelper.GetAllArticles(4748);
            lstArticles = lstArticles.Where(article => article.IncludeInEDM && article.MailOutDate.Date == DateTime.Today.Date).ToList();

            //return View();
            var items = new List<SyndicationItem>();

            foreach (var item in lstArticles)
            {

                var helper = new UrlHelper(this.Request.RequestContext);
                //var url = helper.Action("Index", "Home", new { }, Request.IsSecureConnection ? "https" : "http");

                var feedPackageItem = new SyndicationItem(item.ArticleTitle, item.Excerpt, new Uri((IsOriginalRequestOverSSL ? "https://" : "http://") + item.ActualArticleURLWithDomain));
                feedPackageItem.PublishDate = item.UploadDateAsDateTime;

                feedPackageItem.Categories.Add(new SyndicationCategory() { Label = item.ArticleRSSCategory, Name = item.ArticleRSSCategory });

                items.Add(feedPackageItem);
            }

            return new RssResult("Article Content Feed", items);
        }

        private bool IsOriginalRequestOverSSL
        {
            get
            {
                if (Request.Headers.AllKeys.Contains("X-Forwarded-Proto"))
                {
                    if (Request.Headers["X-Forwarded-Proto"].Equals("https", StringComparison.OrdinalIgnoreCase))
                        return true;
                    else
                        return false;
                }
                else
                    return Request.IsSecureConnection;
            }
        }
    }
}
