using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using DataAnnotationsExtensions;

namespace RexonaAU.Models
{
    //Forgotten Password View Model
    public class ForgottenPasswordViewModel : Umbraco.Web.Models.RenderModel
    {
        public ForgottenPasswordViewModel() : this(new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current).TypedContent(Umbraco.Web.UmbracoContext.Current.PageId)) { }
        public ForgottenPasswordViewModel(Umbraco.Core.Models.IPublishedContent content) : base(content) { }

        [DisplayName("Email address")]
        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string EmailAddress { get; set; }
    }

    //Reset Password View Model
    public class ResetPasswordViewModel: Umbraco.Web.Models.RenderModel
    {
        public ResetPasswordViewModel() : this(new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current).TypedContent(Umbraco.Web.UmbracoContext.Current.PageId)) { }
        public ResetPasswordViewModel(Umbraco.Core.Models.IPublishedContent content) : base(content) { }

        [DisplayName("Email address")]
        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string EmailAddress { get; set; }

        [UIHint("Password")]
        [Required(ErrorMessage = "Please enter your password")]
        public string Password { get; set; }

        [UIHint("Password")]       
        [Required(ErrorMessage = "Please enter your password")]
        [EqualTo("Password", ErrorMessage = "Your passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
    public class EditProfileViewModel : Umbraco.Web.Models.RenderModel
    {
        public EditProfileViewModel() : this(new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current).TypedContent(Umbraco.Web.UmbracoContext.Current.PageId)) { }
        public EditProfileViewModel(Umbraco.Core.Models.IPublishedContent content) : base(content) { }


        public string FirstName { get; set; }
        public string LastName { get; set; }

        [DisplayName("Email address")]
        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string EmailAddress { get; set; }

        [UIHint("Password")]
        [Required(ErrorMessage = "Please enter your password")]
        public string Password { get; set; }

        [UIHint("Password")]
        [Required(ErrorMessage = "Please enter your password")]
        [EqualTo("Password", ErrorMessage = "Your passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}