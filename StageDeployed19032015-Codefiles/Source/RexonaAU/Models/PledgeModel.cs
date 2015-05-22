using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RexonaAU.Models
{
    public class PledgeModel
    {
        public int nodeid { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string createdBy { get; set; }
        public string CategoryType { get; set; }
        public string ChildDoctype { get; set; }
        public string ImageUrl { get; set; }
       // public string IsPopular { get; set; }
        public string color { get; set; }
        public int likecount { get; set; }
        public string ArticleUrl { get; set; }
       // public string expertise { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool Subscribe { get; set; }

        public string SelectedValue { get; set; }
        public IEnumerable<SelectListItem> Values
        {
            get
            {
                return new[]
            {
                new SelectListItem { Value = "public", Text="an Open Goal"},
                new SelectListItem { Value = "private", Text="a Closed Goal"}              
            };
            }
        }

    }
}