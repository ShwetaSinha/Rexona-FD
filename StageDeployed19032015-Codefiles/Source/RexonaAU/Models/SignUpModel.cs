using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RexonaAU.Models
{
    public class SignUpModel : Umbraco.Web.Models.RenderModel
    {
        public SignUpModel() : this(new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current).TypedContent(Umbraco.Web.UmbracoContext.Current.PageId)) { }
        public SignUpModel(Umbraco.Core.Models.IPublishedContent content) : base(content) { }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }


        public string Email { get; set; }

        public string Password { get; set; }

      //  public string ConfirmPassword { get; set; }

       // public string DateOfBirth { get; set; }

       // public string PostCode { get; set; }

        public bool Subscribe { get; set; }

        public bool FacebookConnect { get; set; }

       // public string State { get; set; }
       // public string CapitalCity { get; set; }
       // public string CapitalPostCode { get; set; }

    }
}