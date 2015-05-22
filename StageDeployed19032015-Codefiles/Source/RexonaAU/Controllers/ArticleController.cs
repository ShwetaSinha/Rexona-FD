using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Examine;
using Newtonsoft.Json;
using RexonaAU.Models;
using System.Web.Mvc;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic.media;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.Tags;
using umbraco.cms.businesslogic.web;
using umbraco.MacroEngines;
using Tags = umbraco.editorControls.tags;
using umbraco;
namespace RexonaAU.Controllers
{
    public class ArticleController : Umbraco.Web.Mvc.SurfaceController
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ArticlesSearch(string search)
        {
            ISearcher Searcher = ExamineManager.Instance.SearchProviderCollection["ArticleSearch"];
            var criteria = Searcher.CreateSearchCriteria(UmbracoExamine.IndexTypes.Content);
            var fields = new String[] { "articleAuthor", "articleTag", "articleDescription", "nodeName" };

            Examine.SearchCriteria.IBooleanOperation filter = criteria.GroupedOr(fields, search);

            var searchResults = Searcher.Search(filter.Compile()).Select(p => p.Fields);
            List<ArticleDetail> model = new List<ArticleDetail>();
            foreach (var result in searchResults)
            {
                model.Add(new ArticleDetail
                {
                    ArticleName = result["nodeName"],
                    ArticleAuthor = result["articleAuthor"],
                    ArticleDescription = result["articleDescription"].Substring(0, 500),
                    ArticleId = Convert.ToInt32(result["id"]),
                    PostedOn = result["updateDate"]
                });
              
            }
            ViewBag.ModelList = model;                
            return View("SearchResult"); 
        }

           
        public JsonResult GetArticlesByTag(string ArticlesTag)
        {
            List<ArticleDetail> articleDetail = new List<ArticleDetail>();
            var matchingNodes = Tag.GetNodesWithTags(ArticlesTag).Where(x => x.ParentId == uQuery.GetNodesByName("Articles").FirstOrDefault().Id);

            foreach (var nodeid in matchingNodes)
            {
                DynamicNode node = new DynamicNode(nodeid.Id);
                articleDetail.Add(new ArticleDetail
                {

                    ArticleName = node.Name,
                    ArticleAuthor = node.GetProperty("articleAuthor").Value,
                    PostedOn = String.Format("{0:t}", node.UpdateDate.ToString("h:mm tt   MM/dd/yyyy")),
                    ArticleDescription = node.GetPropertyValue("articleDescription").Substring(0, 500),
                    ArticleUrl = node.Url,
                });
            }
            return Json(articleDetail, JsonRequestBehavior.AllowGet);
        }

         public JsonResult GetArticles()
         {
            DynamicNode node = new DynamicNode(uQuery.GetNodesByName("Articles").FirstOrDefault().Id); // Get Pledges NodeId
            List<ArticleDetail> articleDetail = new List<ArticleDetail>();
            foreach (var childNode in node.Children)
            {
                var Type = Tag.GetNodesWithTags(childNode.GetProperty("articleTag").Value).Where(x => x.ParentId == uQuery.GetNodesByType("PledgeCategories").FirstOrDefault().Id);
                int nodId = Type.Select(p => p.Id).FirstOrDefault();
                DynamicNode color = new DynamicNode(nodId);
                articleDetail.Add(new ArticleDetail
                {

                    ArticleName = childNode.Name,
                    ArticleAuthor = childNode.GetProperty("articleAuthor").Value,
                    ArticleUrl = childNode.Url,
                    ArticleDocType = "Inspiration",
                    ArticleType = Type.Select(t => t.Text).FirstOrDefault().ToString(),
                    color = color.GetProperty("color").Value,
                    ArticleId = childNode.Id

                });
            }
            return Json(articleDetail, JsonRequestBehavior.AllowGet);
        }
    }
}