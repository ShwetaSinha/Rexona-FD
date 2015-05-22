using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using System.Web.Optimization;
using umbraco.NodeFactory;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Web.Routing;


namespace RexonaAU.Helpers
{
    public class StartUp : IApplicationEventHandler
    {
        public StartUp()
        {
            PublishedContentRequest.Prepared += PublishedContentRequest_Prepared;
        }
        public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {

        }

        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {

        }

        private bool IsOriginalRequestOverSSL
        {
            get
            {
                if (HttpContext.Current.Request.Headers.AllKeys.Contains("X-Forwarded-Proto"))
                {
                    if (HttpContext.Current.Request.Headers["X-Forwarded-Proto"].Equals("https", StringComparison.OrdinalIgnoreCase))
                        return true;
                    else
                        return false;
                }
                else
                    return HttpContext.Current.Request.IsSecureConnection;
            }
        }

        private void PublishedContentRequest_Prepared(object sender, EventArgs e)
        {
            try
            {
                /*System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (string key in HttpContext.Current.Request.Headers.AllKeys)
                {
                    sb.AppendLine("Key = " + key + " Value = " + HttpContext.Current.Request.Headers[key]);
                }
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Request headers - " + Environment.NewLine + sb.ToString());*/


                PublishedContentRequest request = sender as PublishedContentRequest;
                HttpContext currentContext = HttpContext.Current;
                string url = request.Uri.ToString();
                IPublishedContent page = request.PublishedContent;

                if (page == null)
                    return;

                // check if the port should be stripped.
                if (ShouldStripPort())
                    url = StripPortFromUrl(url, currentContext.Request.Url);

                // check for matches
                if (HasMatch(page, request))
                {
                    // if the doc-type matches and is NOT on HTTPS...
                    if (!IsOriginalRequestOverSSL)
                    {
                        // ... then redirect the URL to HTTPS.
                        PerformRedirect(url.Replace(Settings.HTTP, Settings.HTTPS), currentContext);
                    }

                    return;
                }

                // otherwise if the URL is on HTTPS...
                if (IsOriginalRequestOverSSL)
                {
                    // ... redirect the URL back to HTTP.
                    PerformRedirect(url.Replace(Settings.HTTPS, Settings.HTTP), currentContext);
                    return;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : PublishedContentRequest_Prepared() event ", ex);

            }
        }

        private static string StripPortFromUrl(string url, Uri contextUri)
        {
            return url.Replace(string.Format(":{0}", contextUri.Port), string.Empty);
        }

        private static bool ShouldStripPort()
        {
            return Settings.GetValueFromKey<bool>(Settings.AppKey_StripPort);
        }

        private static bool ShouldRedirectTemporary()
        {
            return Settings.GetValueFromKey<bool>(Settings.AppKey_UseTemporaryRedirects);
        }

        private static bool HasMatch(IPublishedContent page, PublishedContentRequest request)
        {
            return MatchesDocTypeAlias(page.DocumentTypeAlias)
                || MatchesNodeId(page.Id)
                || MatchesTemplate(request.TemplateAlias)
                || MatchesPropertyValue((page.Id));
        }

        private static bool MatchesDocTypeAlias(string docTypeAlias)
        {
            return Settings.KeyContainsValue(Settings.AppKey_DocTypes, docTypeAlias);
        }

        private static bool MatchesNodeId(int pageId)
        {
            return Settings.KeyContainsValue(Settings.AppKey_PageIds, pageId);
        }

        private static bool MatchesTemplate(string templateAlias)
        {
            return Settings.KeyContainsValue(Settings.AppKey_Templates, templateAlias);
        }

        private static bool MatchesPropertyValue(int pageId)
        {
            var appSetting = Settings.GetValueFromKey(Settings.AppKey_Properties);

            if (string.IsNullOrEmpty(appSetting))
                return false;

            var node = new Node(pageId);
            var items = appSetting.Split(new[] { Settings.COMMA }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in items)
            {
                var parts = item.Split(new[] { Settings.COLON }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                var propertyAlias = parts[0];
                var propertyValue = Settings.CHECKBOX_TRUE;

                if (parts.Length > 1)
                    propertyValue = parts[1];

                var property = node.GetProperty(propertyAlias);
                if (property == null)
                    continue;

                var match = string.Equals(property.Value, propertyValue, StringComparison.InvariantCultureIgnoreCase);
                if (match)
                    return true;
            }

            return false;
        }

        private static void PerformRedirect(string targetUrl, HttpContext context)
        {
            if (ShouldRedirectTemporary())
                context.Response.Redirect(targetUrl, false);
            else
                context.Response.RedirectPermanent(targetUrl, false);
            context.ApplicationInstance.CompleteRequest();
        }

    }

    public class Settings
    {
        public const string CHECKBOX_TRUE = "1";

        public const char COLON = ':';

        public const char COMMA = ',';

        public const string HTTP = "http://";

        public const string HTTPS = "https://";

        public const string ICON = "Our.Umbraco.HttpsRedirect.Resources.Images.icon.png";

        public const string LOGO = "Our.Umbraco.HttpsRedirect.Resources.Images.logo.png";

        public const string PNG_MIME = "image/png";

        public const string AppKey_DocTypes = "HttpsRedirect:DocTypes";

        public const string AppKey_PageIds = "HttpsRedirect:PageIds";

        public const string AppKey_StripPort = "HttpsRedirect:StripPort";

        public const string AppKey_Properties = "HttpsRedirect:Properties";

        public const string AppKey_Templates = "HttpsRedirect:Templates";

        public const string AppKey_UseTemporaryRedirects = "HttpsRedirect:UseTemporaryRedirects";

        public static readonly Dictionary<string, string> AppKeys = new Dictionary<string, string>()
		{
			{ AppKey_DocTypes, "Document Types" },
			{ AppKey_PageIds, "Page Ids" },
			{ AppKey_Templates, "Templates" },
			{ AppKey_Properties, "Properties" },
			{ AppKey_StripPort, "Strip Port" },
			{ AppKey_UseTemporaryRedirects, "Temporary Redirects (302)" },
		};

        public static Version Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        public static string GetValueFromKey(string appKey)
        {
            return GetValueFromKey<string>(appKey);
        }

        public static T GetValueFromKey<T>(string appKey)
        {
            var appKeyValue = WebConfigurationManager.AppSettings[appKey] ?? string.Empty;

            if (string.IsNullOrEmpty(appKeyValue))
                return default(T);

            if (typeof(T) == typeof(bool))
            {
                if (appKeyValue == "1")
                    return (T)(object)true;

                if (appKeyValue == "0")
                    return (T)(object)false;

                bool result;
                bool.TryParse(appKeyValue, out result);

                return (T)(object)result;
            }

            var typeConverter = TypeDescriptor.GetConverter(typeof(T));
            return (T)typeConverter.ConvertFrom(appKeyValue);
        }

        public static bool KeyContainsValue(string appKey, object value)
        {
            if (string.IsNullOrWhiteSpace(appKey))
                return false;

            var appSetting = GetValueFromKey(appKey);
            if (string.IsNullOrWhiteSpace(appSetting))
                return false;

            var values = appSetting.Split(new[] { COMMA }, StringSplitOptions.RemoveEmptyEntries);

            if (value is int)
            {
                var pageIds = Array.ConvertAll(values, int.Parse);
                return pageIds.Contains((int)value);
            }

            return values.Contains(value);
        }
    }
}