using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RexonaAU.Models
{
    public class SignInModel : Umbraco.Web.Models.RenderModel
    {
         public SignInModel() : this(new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current).TypedContent(Umbraco.Web.UmbracoContext.Current.PageId)) { }
         public SignInModel(Umbraco.Core.Models.IPublishedContent content) : base(content) { }

        public string EmailAddress { get; set; }
        public string Password { get; set; }
    }

    
}
