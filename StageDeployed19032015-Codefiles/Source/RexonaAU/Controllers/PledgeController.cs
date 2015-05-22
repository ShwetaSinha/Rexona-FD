using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using RexonaAU.Models;
using umbraco.MacroEngines;
using umbraco.cms.businesslogic.Tags;
using Umbraco.Core.Persistence;
using umbraco;
using umbraco.NodeFactory;
using Umbraco.Core.Logging;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using Examine;
using UmbracoExamine;
using Examine.SearchCriteria;
using umbraco.cms.businesslogic.member;
using System.Net;
using System.Text;
using Umbraco.Core.Services;
using umbraco.presentation.LiveEditing;
using Gibe.Umbraco.AmazonFileSystemProvider;
using System.Configuration;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading.Tasks;
using RexonaAU.Helpers;

namespace RexonaAU.Controllers
{
    public class PledgeController : Umbraco.Web.Mvc.SurfaceController
    {

        #region static Properties
        private static string S3AccessKeyId = ConfigurationManager.AppSettings["accessKeyId"];
        private static string S3SecretAccessKey = ConfigurationManager.AppSettings["secretAccessKey"];
        private static string S3BucketName = ConfigurationManager.AppSettings["bucketName"];
        private static string S3Region = ConfigurationManager.AppSettings["region"];
        private static string S3MediaRoot = ConfigurationManager.AppSettings["mediaRoot"];
        private static string S3UseHttps = ConfigurationManager.AppSettings["useHttps"];
        private static string S3BucketStaticPath = ConfigurationManager.AppSettings["S3bucketURL"];

        static AmazonS3Provider amazonS3Provider = new AmazonS3Provider(S3AccessKeyId, S3SecretAccessKey, S3BucketName, S3Region, S3MediaRoot, S3UseHttps);

        #endregion

        #region Get Pledges for Auto suggestion box

        /// <summary>
        /// GetPublicPledges
        /// This method is used for autosuggestion box in step-1
        /// </summary>
        /// <param name="term">Pleadge Text</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetPublicPledges(string term)
        {
            try
            {
                Member currentmember = Member.GetCurrentMember();
                if (currentmember != null)
                {
                    int memberId = currentmember.Id;
                    var pledges = uQuery.GetNodesByType("Pledge");
                    if (pledges != null)
                    {
                        var result = from L1 in pledges
                                     where (L1.Name.ToLower().Contains(term.ToLower()) && L1.GetProperty("publicPledgeSelection").Value == "1" && L1.ChildrenAsList.Where(a => a.GetProperty<int>("memberId") == memberId).Count() == 0 && L1.ChildrenAsList.Exists(a => a.GetProperty<bool>("isOwner") && a.GetProperty<bool>("step3Clear")))
                                     select new { Value = L1.GetProperty<string>("title"), Id = L1.Id };

                        if (result != null)
                            return Json(result, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetPublicPledges() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }

            return null;
        }

        #endregion

        #region Create Pledge after step-1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePledge(PledgeModel model)
        {
            try
            {
                if (Convert.ToBoolean(Session["FBInvite"]))
                {
                    return CreatePledgeForInvitedmMember(model);
                }
                else
                {
                    //For Set your goal - new fields (First/Last Name)
                    //var contentServiceUp = Services.ContentService;
                    //var pledgeMemberName = Member.(model.FirstName + ' ' + model.LastName, model.Email, model.Email, demoMemberType, new umbraco.BusinessLogic.User(0));

                    
                    //     pledgeMemberName.getProperty("firstName").Value = model.FirstName;
                    

                   
                    //   pledgeMemberName.getProperty("lastName").Value = model.LastName;
                    
                   
                    

                    //contentServiceUp.UpdateName(pledgeMemberName);
                    //End
                   

                    Member currentmember = Member.GetCurrentMember();
                    if (currentmember != null)
                    {
                         //Check the member exists
                        var checkMember = Member.GetCurrentMember();

                        //Check the member exists
                        if (checkMember != null)
                        {
                            checkMember.getProperty("firstName").Value = model.FirstName;
                            checkMember.getProperty("lastName").Value = model.LastName;
                            checkMember.getProperty("subscribedForDoMOre").Value = model.Subscribe;
                            checkMember.Save();
                        }
                        //var service = new ContentService();
                        //var entry = service.GetById(currentmember.Id);                                                  
                        
                        ////new fields 
                        //entry.SetValue("firstName", model.FirstName);
                        //entry.SetValue("lastName", model.LastName);
                        //entry.SetValue("subscribedForDoMOre", model.Subscribe);
                        //service.SaveAndPublish(entry);                                
                        

                        if (model.nodeid == 0)
                        {
                            var pledges = uQuery.GetNodesByType("Pledge");
                            if (pledges != null)
                            {

                                var existingPledge = pledges.Where(pledge => model.title.ToLower().Equals(pledge.GetProperty<string>("title").ToLower(), StringComparison.OrdinalIgnoreCase) &&
                                    model.SelectedValue.Equals(pledge.GetProperty<bool>("publicPledgeSelection") ? "public" : "private", StringComparison.OrdinalIgnoreCase)).ToList();

                                if (existingPledge != null)
                                {
                                    foreach (Node pledge in existingPledge)
                                    {
                                        if (!checkExisting(currentmember, pledge.Id))
                                        {
                                            model.nodeid = pledge.Id;
                                        }
                                        else
                                        {
                                            TempData.Remove("DuplicateErrorMessage");
                                            TempData.Add("DuplicateErrorMessage", "<strong>Error.</strong><br><br>You are already a member of this pledge.<br>Please make a new pledge.");

                                            return RedirectToCurrentUmbracoPage();
                                        }
                                    }
                                }

                            }
                        }


                        if (checkExisting(currentmember, model.nodeid))
                        {
                            TempData.Remove("DuplicateErrorMessage");
                            TempData.Add("DuplicateErrorMessage", "<strong>Error.</strong><br><br>You are already a member of this pledge.<br>Please make a new pledge.");

                            return RedirectToCurrentUmbracoPage();
                        }
                        else
                        {
                            if (currentmember.getProperty("firstName").Value != "")
                            {
                                model.createdBy = currentmember.getProperty("firstName").Value + " " + currentmember.getProperty("lastName").Value;
                            }
                            else
                            {
                                model.createdBy = model.FirstName + " " + model.LastName;
                            }
                            // Get the Umbraco Content Service
                            var contentService = Services.ContentService;
                            bool IsOwner = false;

                            //Get pledge text from nodeID and set title property
                            if (model.nodeid > 0)
                            {
                                Node pledgeNode = new Node(model.nodeid);
                                if (pledgeNode != null)
                                {
                                    model.title = pledgeNode.GetProperty<string>("title");
                                }
                            }

                            if (model.nodeid == 0 || (model.nodeid > 0 && model.SelectedValue.Equals("private", StringComparison.OrdinalIgnoreCase)))
                            {
                                var pledge = contentService.CreateContent(Common.ReplaceSpecialChar(model.title), uQuery.GetNodesByType("Pledges").FirstOrDefault().Id, "pledge", 0);
                                pledge.SetValue("title", model.title);
                                if (model.SelectedValue.Equals("public", StringComparison.OrdinalIgnoreCase))
                                    pledge.SetValue("publicPledgeSelection", true);

                                contentService.SaveAndPublish(pledge);

                                model.nodeid = pledge.Id;
                                List<String> tags = GetTagsforPledgeKeyword(model.title);

                                if (tags != null)
                                {
                                    DynamicNode doc = new DynamicNode(model.nodeid);
                                    foreach (var tag in tags)
                                    {
                                        int tagId = Tag.GetTagId(tag, "default");
                                        if (tagId != 0)
                                        {
                                            Tag.AssociateTagToNode(pledge.Id, tagId);
                                            pledge.SetValue("categoryTag", tag);
                                        }
                                    }
                                }
                                IsOwner = true;
                                contentService.SaveAndPublish(pledge);
                            }

                            var pledgeMember = contentService.CreateContent(Common.ReplaceSpecialChar(model.createdBy), model.nodeid, "PledgeMember", 0);

                            if (model.startDate != null)
                            {
                                pledgeMember.SetValue("startDate", model.startDate.ToString("dd/MM/yyyy"));
                            }

                            if (model.endDate != null)
                            {
                                pledgeMember.SetValue("endDate", model.endDate == default(DateTime) ? string.Empty : model.endDate.ToString("dd/MM/yyyy"));
                            }
                            pledgeMember.SetValue("isOwner", IsOwner);
                            pledgeMember.SetValue("memberId", currentmember.Id);
                            pledgeMember.SetValue("step1Clear", true);
                           
                            //update choose another log
                            //this field will decide two steps take photo and Happy with photo 
                            pledgeMember.SetValue("stepTakePhoto", 1);

                            contentService.SaveAndPublish(pledgeMember);
                            Session["nodeid"] = pledgeMember.Id;
                            var has_name = currentmember.HasProperty("firstName") && currentmember.HasProperty("lastName");
                            if (currentmember.getProperty("firstName").Value.ToString() == "")
                            {
                                Session["Author"] = currentmember.Text != null ? currentmember.Text : has_name ? currentmember.getProperty("firstName").Value : "";
                                Session["DisplayName"] = currentmember.getProperty("displayName").Value != null ? currentmember.getProperty("displayName").Value : currentmember.Text;
                            }
                            else
                            {
                                Session["Author"] = model.FirstName;
                                Session["DisplayName"] =model.FirstName+" "+ model.LastName;
                            }
                            Session["Title"] = model.title;
                            umbraco.library.UpdateDocumentCache(uQuery.GetNodesByType("Pledges").FirstOrDefault().Id);
                            //umbraco.library.UpdateDocumentCache(node);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : CreatePledge() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            finally
            {
                umbraco.library.RefreshContent();
            }

            return RedirectToUmbracoPage(uQuery.GetNodesByType("PledgeStep2").FirstOrDefault().Id);
        }


        [HttpGet]
        public JsonResult JoinPledge(string NodeId)
        {
            string message = "success";
            string pledgeTitle = string.Empty;
            bool IsMemberLoggedIn = false;
            int pledgeId = 0;
            try
            {
                //Get title from using pledge ID
                if (Int32.TryParse(NodeId, out pledgeId))
                {
                    Node node = new Node(pledgeId);
                    if (node != null)
                    {
                        pledgeTitle = node.Name;

                    }
                }

                //Check whether the user is signed in to system
                Member currentmember = Member.GetCurrentMember();
                if (currentmember != null && currentmember.Id > 0)
                {
                    IsMemberLoggedIn = true;
                }
            }
            catch (Exception ex)
            {
                message = "error";
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : JoinPledge() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }

            var result = new
            {
                IsMemberLoggedIn = IsMemberLoggedIn,
                PledgeTitle = pledgeTitle,
                PledgeId = NodeId,
                Message = message
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Tags Generation

        /// <summary>
        /// GetTagsforPledgeKeyword Method
        /// </summary>
        /// <param name="searchTerm"> Pleadge Text</param>
        /// <returns></returns>
        public List<string> GetTagsforPledgeKeyword(string searchTerm)
        {
            DynamicNode keywordsNode = new DynamicNode(uQuery.GetNodesByName("Keywords").FirstOrDefault().Id);
            List<string> results = new List<string>();
            try
            {
                bool isTagFound = false;
                foreach (var childNode in keywordsNode.Children)
                {
                    if (childNode != null)
                    {
                        var keywords = childNode.GetProperty("keywordsList").Value.Replace("\r\n", string.Empty).Replace("\n", string.Empty);

                        string[] keywordsarr = keywords.Split(',');
                        foreach (var arr in keywordsarr)
                        {
                            if (searchTerm.ToLower().Contains(arr.ToLower()))
                            {
                                isTagFound = true;
                                results.Add(childNode.GetProperty("keywordTag").Value);
                            }
                        }
                    }
                }
                if (!isTagFound)
                {
                    results.Add(keywordsNode.ChildrenAsList.FirstOrDefault(child => child.Name.ToLower().Equals("other")).Name);
                }
                return results.Distinct().ToList();
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetTagsforPledgeKeyword() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }

        }

        #endregion

        #region Image Processing

        [HttpPost]
        public JsonResult ImgProcessing(string base64, int orientation = 0)
        {
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(base64)))
            {
                using (Bitmap bm2 = new Bitmap(ms))
                {

                    string imagePath = string.Empty, textToBeSentToYolk = string.Empty, message = "success";
                    try
                    {
                        Bitmap readyImg = Bitmap.FromFile(Server.MapPath("\\images/skeleton.png")) as Bitmap;
                        System.Drawing.Image sourceimage = (Image)bm2;

                        Bitmap original = new Bitmap(sourceimage, readyImg.Size);
                        // original.Save(Server.MapPath("/images/original.png"),ImageFormat.Png);


                        //determine orientation of image
                        //orientation = 1 -> don’t rotate
                        //orientation = 3 -> rotate 180° clockwise
                        //orientation = 6 -> rotate 90° clockwise
                        //orientation = 8 -> rotate -90° clockwise
                        switch (orientation)
                        {
                            case 3: original.RotateFlip(RotateFlipType.Rotate180FlipNone);
                                break;
                            case 6: original.RotateFlip(RotateFlipType.Rotate270FlipY);
                                break;
                            case 8: original.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                break;
                            case 1: original.RotateFlip(RotateFlipType.RotateNoneFlipNone);
                                break;
                            default: original.RotateFlip(RotateFlipType.RotateNoneFlipNone);
                                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                          "Rexona AUS : ImgProcessing() method:" + Environment.NewLine + "Message: "
                          + "Method execution completed - Orientation value is " + orientation);
                                break;
                        }



                        System.Drawing.Image first_1 = MakeGrayscale(original);


                        Bitmap second = SetImageOpacity(Bitmap.FromFile(Server.MapPath("~/images/metal-overlay.jpg")), 0.65f);

                        Bitmap result_2 = new Bitmap(readyImg.Width, readyImg.Height);

                        Graphics g2 = Graphics.FromImage(result_2);

                        g2.DrawImageUnscaled(first_1, 0, 0);

                        g2.DrawImageUnscaled(second, 0, 0);


                        BrightnessCorrection filter1 = new BrightnessCorrection(-10);
                        filter1.ApplyInPlace(result_2);

                        GaussianSharpen filter = new GaussianSharpen(100, 50);
                        filter.ApplyInPlace(result_2);

                        ContrastCorrection filter5 = new ContrastCorrection(100);
                        filter5.ApplyInPlace(result_2);
                        filter5.ApplyInPlace(result_2);

                        // g2.DrawImageUnscaled(readyImg, 0, 0);  

                        System.Drawing.Image final = MakeGrayscale(result_2);

                        Graphics g = Graphics.FromImage(final);
                        g.DrawImageUnscaled(readyImg, 0, 0);


                        FontFamily sophia = LoadFont().Where(family => family.Name == "Sofia Pro Black").First();
                        if (sophia == null)
                        {
                            var result1 = new
                            {
                                ImagePath = "",
                                TextToBeSentToYolk = "",
                                Message = "error"
                            };
                            return Json(result1, JsonRequestBehavior.AllowGet);
                        }

                        Font sofiaFontPledge = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["CharLimitpledge"]), FontStyle.Bold);
                        Font sofiaFontAuthor = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["authorFontBig"]), FontStyle.Bold);
                        Font sofiaFontIWILLDO = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["IwllFont"]), FontStyle.Bold);

                        //Align pledge name and author name center
                        StringFormat stringFormat = new StringFormat();
                        stringFormat.Alignment = StringAlignment.Center;
                        stringFormat.LineAlignment = StringAlignment.Center;


                        string PledgeTitle = string.Empty, Author = string.Empty;
                        if (Session["Title"] != null)
                        {
                            PledgeTitle = Session["Title"].ToString();
                        }

                        if (Session["DisplayName"] != null)
                        {
                            Author = Session["DisplayName"].ToString();
                            textToBeSentToYolk = Author;
                        }

                        int x = int.Parse(ConfigurationManager.AppSettings["XCords"]);
                        int y = int.Parse(ConfigurationManager.AppSettings["YCords"]);

                        if (!string.IsNullOrEmpty(PledgeTitle))
                        {
                            if (PledgeTitle.Length <= 30)
                            {
                                sofiaFontPledge = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["CharLimitpledge"]), FontStyle.Bold);
                            }
                            else if (PledgeTitle.Length <= 35)
                            {
                                sofiaFontPledge = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["CharLimitpledgeMed"]), FontStyle.Bold);
                            }
                            else
                            {
                                sofiaFontPledge = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["CharLimitpledgeTop"]), FontStyle.Bold);
                            }
                        }

                        if (!string.IsNullOrEmpty(Author))
                        {
                            if (Author.Length <= 15)
                            {
                                sofiaFontAuthor = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["authorFontBig"]), FontStyle.Bold);
                            }
                            else if (Author.Length <= 20)
                            {
                                sofiaFontAuthor = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["authorFontMedium"]), FontStyle.Bold);
                                y = int.Parse(ConfigurationManager.AppSettings["YMedCords"]);
                            }
                            else if (Author.Length >= 21)
                            {
                                sofiaFontAuthor = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["authorFontTop"]), FontStyle.Bold);
                                y = int.Parse(ConfigurationManager.AppSettings["YTopCords"]);
                            }
                        }

                        //  Font font1 = new Font(FontName, pledgeTextFont, FontStyle.Bold);

                        float angle;
                        angle = -1F;

                        // g.TranslateTransform(0F, 11.5F); // offset the origin to our calculated values
                        g.RotateTransform(angle);
                        g.DrawString(PledgeTitle.ToUpper(), sofiaFontPledge, Brushes.Black, 235, 400, stringFormat);


                        //Font font2 = new Font(FontName, pledgeAuthorFont, FontStyle.Bold);

                        g.RotateTransform(angle);
                        g.TextRenderingHint = TextRenderingHint.AntiAlias;
                        g.DrawString(Author.ToUpper(), sofiaFontAuthor, Brushes.Black, x, y, stringFormat);


                        //  Font font = new Font(IWillFontName, iWillDoFont, FontStyle.Bold);

                        angle = -1F;
                        g.RotateTransform(angle);
                        g.DrawString(ConfigurationManager.AppSettings["PledgeTextPrefix"], sofiaFontIWILLDO, Brushes.Black, 11, 320);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;


                        int entryid = 0;
                        if (Session["nodeid"] != null)
                        {
                            int.TryParse(Session["nodeid"].ToString(), out entryid);
                        }

                        //Create unit folder for each user
                        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media") + @"\" + entryid);
                        string newResizedImagePath = (Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media") + @"\" + entryid) + "\\processedImage.png";
                        final.Save(newResizedImagePath, ImageFormat.Png);

                        var fs = new FileStream(newResizedImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                        string S3bucketPath = entryid + "\\processedImage.png";
                        amazonS3Provider.AddFile(S3bucketPath, fs);
                        fs.Dispose();
                        imagePath = S3BucketStaticPath + entryid + @"/processedImage.png";

                        //Delete the folder once processed image is sent to S3:

                        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media") + @"\" + entryid;

                        var directory = new DirectoryInfo(folderPath);
                        if (directory.Exists)
                        {
                            foreach (FileInfo filetodelete in directory.GetFiles())
                            {
                                filetodelete.Delete();
                            }
                            foreach (DirectoryInfo dir in directory.GetDirectories())
                            {
                                dir.Delete(true);
                            }

                            directory.Delete();
                        }

                        g2.Dispose();
                        g.Dispose();

                        var service = new ContentService();
                        var entry = service.GetById(entryid);
                        try
                        {
                            Node node = new Node(entryid);

                            entry.SetValue("step2Clear", true);
                            entry.SetValue("stepTakePhoto", 0);
                            entry.SetValue("imageUrl", imagePath);
                            service.SaveAndPublish(entry);

                        }
                        catch (Exception ex)
                        {
                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                                "Rexona AUS : ImgProcessing() method:" + Environment.NewLine + "Message: "
                                + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                        }
                        finally
                        {
                            umbraco.library.RefreshContent();
                        }

                    }
                    catch (Exception ex)
                    {
                        message = "error";
                        ViewBag.Message = "ERROR:" + ex.Message.ToString();
                        LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                            "Rexona AUS : ImgProcessing() method:" + Environment.NewLine + "Message: "
                            + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                    }

                    var result = new
                    {
                        ImagePath = imagePath,
                        TextToBeSentToYolk = Session["Author"] != null ? Session["Author"].ToString() : "",
                        Message = message
                    };

                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }

            // return null;
        }

        /// <summary>
        /// MakeGrayscale
        /// </summary>
        /// <param name="original">Original Image</param>
        /// <returns></returns>
        public static Bitmap MakeGrayscale(Bitmap original)
        {
            //make an empty bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);
            try
            {

                for (int i = 0; i < original.Width; i++)
                {
                    for (int j = 0; j < original.Height; j++)
                    {
                        //get the pixel from the original image
                        Color originalColor = original.GetPixel(i, j);

                        //create the grayscale version of the pixel
                        int grayScale = (int)((originalColor.R * .3) + (originalColor.G * .3)
                            + (originalColor.B * .3));

                        //create the color object
                        Color newColor = Color.FromArgb(grayScale, grayScale, grayScale);

                        //set the new image's pixel to the grayscale version
                        newBitmap.SetPixel(i, j, newColor);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : MakeGrayscale() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);

            }
            return newBitmap;
        }

        /// <summary>
        /// ScaleImage
        /// </summary>
        /// <param name="image"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        public static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);
            Graphics.FromImage(newImage).DrawImage(image, 0, 7, newWidth, newHeight);
            return newImage;
        }

        /// <summary>
        /// SetImageOpacity
        /// </summary>
        /// <param name="image">Image</param>
        /// <param name="opacity">Opacity Value</param>
        /// <returns></returns>
        public Bitmap SetImageOpacity(System.Drawing.Image image, float opacity)
        {

            //create a Bitmap the size of the image provided  
            Bitmap bmp = new Bitmap(image.Width, image.Height);
            try
            {

                //create a graphics object from the image  
                using (Graphics gfx = Graphics.FromImage(bmp))
                {

                    //create a color matrix object  
                    ColorMatrix matrix = new ColorMatrix();

                    //set the opacity  
                    matrix.Matrix33 = opacity;

                    //create image attributes  
                    ImageAttributes attributes = new ImageAttributes();

                    //set the color(opacity) of the image  
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    //now draw the image  
                    gfx.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                }

                return bmp;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : SetImageOpacity() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }

        #endregion

        #region Yolk Video generation

        [HttpGet]
        public JsonResult VideoGenerationFromYolk(string imageURL, string text)
        {
            string message = "error";
            int entryid = 0;
            if (Session["nodeid"] != null)
            {
                if (int.TryParse(Session["nodeid"].ToString(), out entryid))
                {
                    var service = new ContentService();
                    var entry = service.GetById(entryid);
                    try
                    {
                        Node node = new Node(entryid);

                        Member member = Member.GetCurrentMember();

                        if (node != null && entryid > 0)
                        {

                            Task.Run(() => SendForYolkVideoProcessing(entryid, imageURL, text, node.Id, member));

                            entry.SetValue("step3Clear", true);
                            service.SaveAndPublish(entry);
                            message = "success";

                            //To show welcome message add 'Step3Done' in tempdate
                            TempData["Step3Done"] = true;

                            //'View discussions' button does not appear if user is only member of pledge
                            //'View discussions'button takes the user to the Pledge Network page of their newly joined pledge.. )
                            if (node.GetSiblingNodes().Where(obj => obj.GetProperty<bool>("step3Clear")).Count() > 0)
                            {
                                TempData["ViewdiscussionsURL"] = node.Parent.NiceUrl;
                            }

                            TempData["ImageUrl"] = imageURL;
                            TempData["PledgeShareUrl"] = node.Parent.NiceUrl;
                            TempData["PledgeTitle"] = node.Parent.GetProperty<string>("title");
                            TempData["PledgeCreator"] = node.Id;
                            if (Convert.ToBoolean(Session["FBInvite"]))
                            {
                                int InvitingMemberId = Convert.ToInt32(Session["InvitingMember"]);

                                Entities dbEntity = new Entities();

                                if (Convert.ToString(Session["LinkType"]).Equals("email", StringComparison.OrdinalIgnoreCase))
                                {
                                    int linkId = Convert.ToInt32(Session["LinkId"]);
                                    var foundEntry = dbEntity.PledgeInviteDatas.FirstOrDefault(i => i.LinkId == linkId && i.Type.Equals("email", StringComparison.OrdinalIgnoreCase));

                                    if (foundEntry != null)
                                    {
                                        foundEntry.IsUsed = true;
                                        foundEntry.AcceptedDate = DateTime.Now;
                                        if (dbEntity.SaveChanges() > 0) { }
                                    }

                                }
                                else
                                {
                                    int lastRowId = dbEntity.PledgeInviteDatas.ToList().LastOrDefault() == null ? 0 : dbEntity.PledgeInviteDatas.ToList().LastOrDefault().Id;
                                    string type = Convert.ToString(Session["LinkType"]);
                                    dbEntity.PledgeInviteDatas.Add(new PledgeInviteData()
                                    {
                                        LinkId = lastRowId + 1,
                                        IsUsed = true,
                                        Type = type,
                                        InvitedDate = DateTime.Now,
                                        AcceptedDate = DateTime.Now
                                    });

                                    if (dbEntity.SaveChanges() > 0)
                                    {

                                    }
                                }

                                Member InvitingMember = new Member(InvitingMemberId);
                                Member currentmember = Member.GetCurrentMember();

                                AddFriends(InvitingMember, currentmember.Id);
                                AddFriends(currentmember, InvitingMember.Id);
                            }
                        }
                      
                        AddDiscussionFollower(member.Id,entryid);


                    }
                    catch (Exception ex)
                    {
                        message = "error";
                        LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : VideoGenerationFromYolk() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                    }
                    finally
                    {
                        umbraco.library.RefreshContent();
                    }

                }
            }

            var result = new
            {
                Message = message
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private async Task<ActionResult> SendForYolkVideoProcessing(int entryid, string imageURL, string text, int nodeID, Member member)
        {
            var service = new ContentService();
            try
            {
                var entry = service.GetById(entryid);
                HttpWebRequest myReq = null;
                HttpWebResponse webResponse = null;
                string results = string.Empty;
                string YolkURL = ConfigurationManager.AppSettings["YolkURL"];
                string YouTubeURL = ConfigurationManager.AppSettings["YouTubeURL"];
                if (!string.IsNullOrEmpty(YolkURL))
                {
                    myReq = (HttpWebRequest)WebRequest.Create(YolkURL + "?image=" + imageURL + "&text=" + text.ToUpper());
                    myReq.Method = "GET";
                    myReq.ContentType = "text/plain";

                    myReq.Timeout = (1000 * 60) * 10;
                    webResponse = (HttpWebResponse)myReq.GetResponse();
                    StreamReader sr = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8);
                    results = sr.ReadToEnd();

                    entry.SetValue("youTubeUrl", YouTubeURL + results.Replace("\n", string.Empty));
                    service.SaveAndPublish(entry);
                }

                SendWelcomeEmailUsingTraction(nodeID, member);

                return Json("sucess");
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : ASYNC VideoGenerationFromYolk() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return Json("error");
            }

        }

        #endregion

        #region Send Welcome Email Using Traction

        public void SendWelcomeEmailUsingTraction(int entryId, Member member)
        {
            try
            {


                if (ConfigurationManager.AppSettings["TractionBaseURL_WelcomeEmail"] == null
                    || ConfigurationManager.AppSettings["TractionPassword_WelcomeEmail"] == null)
                {
                    return;
                }

                //get entry node
                Node node = new Node(entryId);




                //build Traction URL with all it's parameters
                var uribuilder = new UriBuilder(ConfigurationManager.AppSettings["TractionBaseURL_WelcomeEmail"]);

                var queryString = System.Web.HttpUtility.ParseQueryString(uribuilder.Query);

                queryString["password"] = ConfigurationManager.AppSettings["TractionPassword_WelcomeEmail"];

                //TODO: Add term accept value
                queryString["customer.ACCEPT_TCS_REXONA_I_WILL"] = "1";
                queryString["customer.FIRSTNAME"] = member.GetProperty<string>("firstName");
                queryString["customer.LASTNAME"] = member.GetProperty<string>("lastName");
                queryString["customer.PCODE"] = member.GetProperty<string>("postCode");
                queryString["customer.REXONA_I_WILL_PLEDGE"] = node.Parent.GetProperty<string>("title");

                #region REXONA_PLEDGES / Category tags

                string categoryTag = node.Parent.GetProperty<string>("categoryTag");
                if (!string.IsNullOrEmpty(categoryTag))
                {
                    if (categoryTag.Contains(','))
                    {
                        categoryTag = categoryTag.Split(',')[0];
                    }
                }
                queryString["customer.REXONA_PLEDGES"] = categoryTag;
                #endregion

                #region ULDM_UNILIVER_EMAIL_OPT_IN_SOURCE
                if (ConfigurationManager.AppSettings["UnileverEmailOptInSource"] != null)
                {
                    queryString["customer.ULDM_UNILEVER_EMAIL_OPT-IN_SOURCE"] = ConfigurationManager.AppSettings["UnileverEmailOptInSource"];
                }
                else
                {
                    queryString["customer.ULDM_UNILEVER_EMAIL_OPT-IN_SOURCE"] = "null";
                }
                #endregion

                #region ULDM_BRAND_EMAIL_OPT_IN_SOURCE
                if (ConfigurationManager.AppSettings["BrandEmailOptInSource"] != null)
                {
                    queryString["customer.ULDM_BRAND_EMAIL_OPT_IN_SOURCE"] = ConfigurationManager.AppSettings["BrandEmailOptInSource"];
                }
                else
                {
                    queryString["customer.ULDM_BRAND_EMAIL_OPT_IN_SOURCE"] = "null";
                }
                #endregion

                #region DATE OF BIRTH
                if (!string.IsNullOrWhiteSpace(member.GetProperty<string>("birthDate")))
                {
                    DateTime dt;
                    if (DateTime.TryParse(member.GetProperty<string>("birthDate"), out dt))
                    {
                        queryString["customer.DOB"] = dt.ToString("d-MMM-yyyy");
                    }
                    else
                    {
                        queryString["customer.DOB"] = "";
                    }
                }
                else
                {
                    queryString["customer.DOB"] = "";
                }
                #endregion

                queryString["customer.email"] = member.Email;

                #region Subscriptions
                if (member.GetProperty<bool>("subscribedForDoMOre"))
                {
                    queryString["subscriptions." + ConfigurationManager.AppSettings["subscriptionsID"]] = "SUBSCRIBE";
                }
                else
                {
                    queryString["subscriptions." + ConfigurationManager.AppSettings["subscriptionsID"]] = "UNSUBSCRIBE";
                }
                #endregion

                uribuilder.Query = queryString.ToString();
                string url = uribuilder.ToString();

                //Welcome email must be sent after 30 min(configurable)
                int delayMinutes = ConfigurationManager.AppSettings["WelcomeEmailDelayMinutes"] != null ?
                    Convert.ToInt32(ConfigurationManager.AppSettings["WelcomeEmailDelayMinutes"]) : 0;

                RexonaAU.Helpers.Common.TractionAPI.SendEmailUsingTraction(url, "WelcomeEmail", delayMinutes);

                //Sample URL
                //"https://int.api.tractionplatform.com:443/wkf/ym5kkddd2f9g98liicb6?password=%24%5eGWE0%5efj&customer.ACCEPT_TCS_REXONA_I_WILL=1&customer.FIRSTNAME=Vaishali&customer.LASTNAME=Dengle&customer.PCODE=1234&customer.REXONA_I_WILL_PLEDGE=swim+on+holidays+%26%26%3e%3e&customer.REXONA_PLEDGES=Swim&customer.ULDM_UNILEVER_EMAIL_OPT-IN_SOURCE=Rexona&customer.ULDM_BRAND_EMAIL_OPT_IN_SOURCE=Rexona+I+will+do&customer.DOB=13-Oct-1996&customer.email=vaishaliu%40cybage.com&subscriptions.18274160=SUBSCRIBE");
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : SendWelcomeEmailUsingTraction() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
            }
        }

        #endregion

        #region Invited pledges

        public void AddFriends(Member currentmember, int othermemberId)
        {
            try
            {
                string currentFriends = currentmember.GetProperty<string>("invitedFriends");
                Entities dbEntity = new Entities();
                var result = false;
                int memberF = dbEntity.MemberFriends.Where(member => member.MemberId == currentmember.Id && member.FriendId == othermemberId && member.IsRemoved == true).ToList().Count;

                if (memberF == 0)
                {
                    memberF = dbEntity.MemberFriends.Where(member => member.MemberId == currentmember.Id && member.FriendId == othermemberId && member.IsRemoved == false).ToList().Count;

                    if (memberF == 0)
                    {
                        dbEntity.MemberFriends.Add(new MemberFriend()
                        {
                            MemberId = currentmember.Id,
                            FriendId = othermemberId,
                            IsRemoved = false
                        });
                    }
                }
                else
                {
                    var foundMember = dbEntity.MemberFriends.FirstOrDefault(member => member.MemberId == currentmember.Id && member.FriendId == othermemberId);
                    if (foundMember != null)
                    {
                        foundMember.IsRemoved = false;
                    }
                }
                if (dbEntity.SaveChanges() > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : AddFriends() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }

        }
        
        


        public bool checkExisting(Member currentmember, int pledgeId)
        {
            if (pledgeId > 0)
            {
                Node pledgeNode = new Node(pledgeId);

                IEnumerable<Node> members = pledgeNode.GetChildNodes();               
                int count = members.Where(m => m.GetProperty<int>("memberId") == currentmember.Id && m.GetProperty<bool>("step3Clear") && m.GetProperty<bool>("isDeleted")==false).ToList().Count;
                if (count != 0)
                {
                    Session["FBInvite"] = false;
                    return true;
                }

            }
            return false;
        }

        public ActionResult CreatePledgeForInvitedmMember(PledgeModel model)
        {
            try
            {
                Member currentmember = Member.GetCurrentMember();
                if (currentmember != null)
                {

                    model.createdBy = currentmember.getProperty("firstName").Value + " " + currentmember.getProperty("lastName").Value;
                    // Get the Umbraco Content Service
                    var contentService = Services.ContentService;
                    bool IsOwner = false;

                    //Get pledge text from nodeID and set title property
                    if (model.nodeid > 0)
                    {
                        if (checkExisting(currentmember, model.nodeid))
                        {
                            TempData.Remove("DuplicateErrorMessage");
                            TempData.Add("DuplicateErrorMessage", "<strong>Error.</strong><br><br>You are already a member of this pledge.<br>Please make a new pledge.");
                            return RedirectToCurrentUmbracoPage();

                        }
                        Node pledgeNode = new Node(model.nodeid);
                        if (pledgeNode != null)
                        {
                            model.title = pledgeNode.GetProperty<string>("title");
                        }
                    }

                    #region commentedFromstep1
                    //if (model.nodeid == 0 || (model.nodeid > 0 && model.SelectedValue.Equals("private", StringComparison.OrdinalIgnoreCase)))
                    //{

                    //    var pledge = contentService.CreateContent(Common.ReplaceSpecialChar(model.title), uQuery.GetNodesByType("Pledges").FirstOrDefault().Id, "pledge", 0);
                    //    pledge.SetValue("title", model.title);
                    //    if (model.SelectedValue.Equals("public", StringComparison.OrdinalIgnoreCase))
                    //        pledge.SetValue("publicPledgeSelection", true);

                    //    contentService.SaveAndPublish(pledge);

                    //    model.nodeid = pledge.Id;
                    //    List<String> tags = GetTagsforPledgeKeyword(model.title);

                    //    if (tags != null)
                    //    {
                    //        DynamicNode doc = new DynamicNode(model.nodeid);
                    //        foreach (var tag in tags)
                    //        {
                    //            int tagId = Tag.GetTagId(tag, "default");
                    //            if (tagId != 0)
                    //            {
                    //                Tag.AssociateTagToNode(pledge.Id, tagId);
                    //                pledge.SetValue("categoryTag", tag);
                    //            }
                    //        }
                    //    }
                    //    IsOwner = true;
                    //    contentService.SaveAndPublish(pledge);
                    //}
                    #endregion

                    var pledgeMember = contentService.CreateContent(Common.ReplaceSpecialChar(model.createdBy), model.nodeid, "PledgeMember", 0);

                    if (model.startDate != null)
                    {
                        pledgeMember.SetValue("startDate", model.startDate.ToString("dd/MM/yyyy"));
                    }

                    if (model.endDate != null)
                    {
                        pledgeMember.SetValue("endDate", model.endDate == default(DateTime) ? string.Empty : model.endDate.ToString("dd/MM/yyyy"));
                    }
                    pledgeMember.SetValue("isOwner", IsOwner);
                    pledgeMember.SetValue("memberId", currentmember.Id);
                    pledgeMember.SetValue("step1Clear", true);
                    pledgeMember.SetValue("isInvited", true);
                    contentService.SaveAndPublish(pledgeMember);
                    Session["nodeid"] = pledgeMember.Id;
                    var has_name = currentmember.HasProperty("firstName") && currentmember.HasProperty("lastName");
                    Session["Author"] = currentmember.Text != null ? currentmember.Text : has_name ? currentmember.getProperty("firstName").Value : "";
                    Session["DisplayName"] = currentmember.getProperty("displayName").Value != null ? currentmember.getProperty("displayName").Value : currentmember.Text;
                    Session["Title"] = model.title;
                    umbraco.library.UpdateDocumentCache(uQuery.GetNodesByType("Pledges").FirstOrDefault().Id);
                    //umbraco.library.UpdateDocumentCache(node);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : CreatePledge() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }
            finally
            {
                umbraco.library.RefreshContent();
            }

            //return RedirectToUmbracoPage(uQuery.GetNodesByName("Step 2").FirstOrDefault().Id);
            return RedirectToUmbracoPage(uQuery.GetNodesByType("PledgeStep2").FirstOrDefault().Id);
        }
        #endregion

        #region Add Member as Discussion Follower

        public void AddDiscussionFollower(int MemberId, int PledgeId)
        {
            try
            {
                var result = false;
                int ParentPledgeID;

                List<MemberPledges> lstMemberPledges = Common.GetAllMemberPledges();
                lstMemberPledges = lstMemberPledges.FindAll(a => a.PledgeId == PledgeId).ToList();
                if (lstMemberPledges.Count > 0)
                {
                    ParentPledgeID = lstMemberPledges.Select(a => a.ParentId).ToList()[0];

                    List<Discussion> lstDiscussions = new List<Discussion>();
                    //Get all discussions list for the pledge
                    lstDiscussions = CreateDiscussionListObject(ParentPledgeID, "AllDiscussions");

                    if (lstDiscussions.Count > 0)
                    {
                        //Update Follower data with memeberId and DiscussionID available for the Joined Goal
                        for (int i = 0; i < lstDiscussions.Count; i++)
                        {
                            Entities dbEntities = new Entities();

                            dbEntities.DiscussionFollowers.Add(new DiscussionFollower()
                            {
                                CreatedDateTime = DateTime.Now,
                                DiscussionId = lstDiscussions.Select(a => a.ID).ToList()[i],
                                MemberId = MemberId,
                                LastReadDateTime = DateTime.Now
                            });
                        }
                    }
                }

                
                result = true;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : AddFriends() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
            }

        }

        private const string CachePrefix = "RexonaAUSDiscussion_{0}";
        private static object lockObj = new object();
        /// <summary>
        /// Common method to create discussion list for a pledge
        /// </summary>
        /// <param name="pledgeId"></param>
        /// <returns></returns>
        /// 
        public List<Discussion> CreateDiscussionListObject(int pledgeId, string cacheSuffix)
        {
            try
            {
                int CacheMinutes = 0;
                if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes"], out CacheMinutes))
                    CacheMinutes = 30;

                string cacheKey = string.Format(CachePrefix, string.Format("GetDiscussionDetailsByPledgeId_{0}_{1}", pledgeId, cacheSuffix));
                object configObj = System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Cache[cacheKey] : null;

                if (configObj == null)
                {
                    lock (lockObj)
                    {
                        int MasterDiscussionID = uQuery.GetNodesByType("PledgeDiscussionsMaster").FirstOrDefault().Id;
                        if (MasterDiscussionID > 0)
                        {
                            try
                            {
                                Node MasterDiscussionNode = new Node(MasterDiscussionID);
                                if (MasterDiscussionNode != null)
                                {
                                    if (MasterDiscussionNode.ChildrenAsList.Where(a => a.Name == Convert.ToString(pledgeId)).Count() > 0)
                                    {
                                        int nodeId = MasterDiscussionNode.ChildrenAsList.Where(a => a.Name == Convert.ToString(pledgeId)).Select(a => a.Id).ToList()[0];
                                        Node pledgeDiscussionNode = new Node(nodeId);
                                        if (pledgeDiscussionNode != null && pledgeDiscussionNode.Children.Count > 0)
                                        {
                                            List<Discussion> lstDiscussions = new List<Discussion>();
                                            List<MemberPledges> lstMemberPledges = Common.GetAllMemberPledges();
                                            foreach (var childNode in pledgeDiscussionNode.ChildrenAsList)
                                            {
                                                bool isMemberExist = true;
                                                Discussion discussion = new Discussion();
                                                discussion.ID = childNode.Id;
                                                discussion.Title = childNode.GetProperty<string>("discussionTitle");
                                                discussion.Description = childNode.GetProperty<string>("discussionDescription");
                                                int createdById = childNode.GetProperty<int>("createdById");
                                                discussion.PostedById = createdById;
                                                discussion.PostedByDate = childNode.CreateDate;
                                                discussion.PostedDateTimeAsString = childNode.CreateDate.ToString("dd/MM/yyyy hh:mmtt");
                                                int replycount = Common.GetDiscussionNotification(discussion.ID);
                                                discussion.Repliescount = replycount;

                                                if (createdById > 0)
                                                {
                                                    try
                                                    {
                                                        Member member = new Member(createdById);
                                                        bool has_name = member.HasProperty("firstName") && member.HasProperty("lastName");
                                                        discussion.PostedBy = !string.IsNullOrEmpty(member.Text) ? member.Text : has_name ? member.GetProperty<string>("firstName") + " " + member.GetProperty<string>("lastName") : string.Empty;
                                                    }
                                                    catch
                                                    {
                                                        isMemberExist = false;
                                                    }
                                                }

                                                if (lstMemberPledges != null && lstMemberPledges.Count > 0)
                                                {
                                                    List<MemberPledges> thisMemberPledges = lstMemberPledges.FindAll(obj => obj.MemberId == createdById);
                                                    if (thisMemberPledges != null && thisMemberPledges.Count > 0)
                                                    {
                                                        var firstPledge = thisMemberPledges.OrderBy(obj => obj.CreatedDate).FirstOrDefault();
                                                        if (firstPledge != null && firstPledge.PledgeId > 0)
                                                        {
                                                            discussion.PostedByAvatar = !string.IsNullOrEmpty(firstPledge.ImageUrl) ? firstPledge.ImageUrl : "http://placehold.it/50x50";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        discussion.PostedByAvatar = "http://placehold.it/50x50";
                                                    }
                                                }

                                                if (isMemberExist)
                                                {
                                                    lstDiscussions.Add(discussion);
                                                }
                                            }


                                            configObj = lstDiscussions;
                                            if (configObj != null && System.Web.HttpContext.Current != null)
                                            {
                                                System.Web.HttpContext.Current.Cache.Add(cacheKey, configObj, null, DateTime.Now.AddMinutes(CacheMinutes),
                                                 System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                }

                return configObj as List<Discussion>;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : MyDiscussions() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }

        public class Discussion
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public string PostedBy { get; set; }
            public int PostedById { get; set; }
            public DateTime PostedByDate { get; set; }
            public string PostedDateTimeAsString { get; set; }
            public string PostedByAvatar { get; set; }
            public string Description { get; set; }
            public int Repliescount { get; set; }
        }

        #endregion

        #region Pledge Gallery

        [HttpGet]
        public JsonResult GetPledges(int PageSize, int currentPageIndex, string sortingText)
        {
            string message = "error";
            List<PledgeGalleryModel> lstPledges = GetAllPledges();
            try
            {
                if (lstPledges != null && lstPledges.Count > 0)
                {
                    lstPledges = lstPledges.Where(a => a.Step3Clear).ToList();
                    if (!string.IsNullOrEmpty(sortingText) && sortingText.Equals("popular", StringComparison.OrdinalIgnoreCase))
                    {
                        lstPledges = lstPledges.OrderByDescending(a => a.LikeCount).ThenByDescending(a => a.CreatedDate).ToList<PledgeGalleryModel>();
                    }
                    else
                    {
                        lstPledges = lstPledges.OrderByDescending(a => a.CreatedDate).ToList<PledgeGalleryModel>();
                    }

                    var result = new
                    {
                        pledges = lstPledges.Skip(currentPageIndex * PageSize).Take(PageSize),
                        totalPages = Math.Ceiling((decimal)(lstPledges.Count / (decimal)PageSize)),
                        message = "success"
                    };

                    return Json(result, JsonRequestBehavior.AllowGet);
                }

                return Json(new { totalPages = 0, message = "No Pledges Found" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetPledges() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return base.Json("Please try again", "text/plain");
            }
        }

        public List<PledgeGalleryModel> GetAllPledges()
        {
            DynamicNode pledges = new DynamicNode(uQuery.GetNodesByType("Pledges").FirstOrDefault().Id);
            List<PledgeGalleryModel> model = new List<PledgeGalleryModel>();
            int memberId = 0;
            Member currentmember = Member.GetCurrentMember();
            if (currentmember != null)
            {
                memberId = currentmember.Id;
            }
            if (pledges != null)
            {
                try
                {
                    foreach (var childNode in pledges.Children)
                    {
                        if (childNode.Children.Count() > 0)
                        {
                            var childItems = childNode.Children.Items;
                            model.Add(new PledgeGalleryModel
                            {
                                Id = childNode.Id,
                                ImageURL = childItems.Count() > 0 ? childItems.Where(author => author.GetProperty("isOwner").Value == "1").Select(l => l.GetProperty("imageUrl").Value).FirstOrDefault().ToString() : string.Empty,
                                IsPublicSelection = childNode.GetProperty("publicPledgeSelection").Value,
                                PledgeNodeId = childNode.Id,
                                MemberCount = childItems.Where(a => a.GetProperty<bool>("step3Clear")).ToList().Count,
                                LikeCount = childNode.GetProperty<int>("likeCount"),
                                CreatedDate = Convert.ToDateTime(childNode.CreateDate),
                                PledgeURL = childNode.NiceUrl,
                                IsMember = memberId != 0 ? childNode.ChildrenAsList.Where(a => a.GetProperty<int>("memberId") == memberId).Count() > 0 : false,
                                Step3Clear = childItems.Count() > 0 ? childItems.Where(author => author.GetProperty("isOwner").Value == "1").Select(a => a.GetProperty<bool>("step3Clear")).ToList<bool>()[0] : false
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetAllPledges() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                }
            }
            return model;

        }

        /// <summary>
        /// Share Pledge
        /// </summary>
        /// <param name="pledgeId">Pledge ID</param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult SharePledge(int pledgeId)
        {
            string message = "success";
            Member currentmember = Member.GetCurrentMember();
            if (currentmember != null)
            {
                int memberId = currentmember.Id;
                if (pledgeId > 0)
                {
                    try
                    {
                        Node node = new Node(pledgeId);
                        //Check if the current logged in member is a part of pledge
                        if (node != null && node.ChildrenAsList.Where(a => a.GetProperty<int>("memberId") == memberId).Count() > 0)
                        {
                            //Get the group member of the member to update his/her sharing 
                            List<int> userNodeId = node.ChildrenAsList.Where(a => a.GetProperty<int>("memberId") == memberId).Select(a => a.Id).ToList();
                            if (userNodeId.Count > 0)
                            {
                                var service = new ContentService();
                                var entry = service.GetById(userNodeId[0]);
                                if (entry != null)
                                {
                                    entry.SetValue("shared", true);
                                    service.SaveAndPublish(entry);
                                    umbraco.library.UpdateDocumentCache(node.Parent.Id);
                                    umbraco.library.UpdateDocumentCache(entry.Id);
                                    umbraco.library.RefreshContent();
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : SharePledge() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                        message = "error";
                    }

                }
            }

            var result = new
            {
                Message = message
            };

            return Json(result, JsonRequestBehavior.AllowGet);


        }


        #endregion

        #region Ticker Count

        [HttpPost]
        public JsonResult PledgeTickerCount()
        {
            int pledgeCount = 0;
            string message = "Success";
            try
            {
                var pledges = uQuery.GetNodesByType("Pledge");
                if (pledges != null)
                {
                    var pledgeNodeResult = from L1 in pledges
                                           where (L1.ChildrenAsList.Exists(a => a.GetProperty<bool>("isOwner") && a.GetProperty<bool>("step3Clear")))
                                           select new { Value = L1.GetProperty<string>("title"), Id = L1.Id };
                    if (pledgeNodeResult != null)
                    {
                        pledgeCount = pledgeNodeResult.ToList().Count;
                    }
                }
            }

            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : Get Pledge Ticker Count Failed" +
                    Environment.NewLine + "Message: " + ex.Message + Environment.NewLine
                    + ex.StackTrace);
                pledgeCount = -1;
                message = "Error";
            }

            var result = new { message = message, pledgeCount = pledgeCount };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Load Font
        private FontFamily[] LoadFont()
        {
            try
            {
                PrivateFontCollection privateFontCollection = new PrivateFontCollection();

                var memory = IntPtr.Zero;

                try
                {
                    byte[] fontFile = null;

                    var assembly = Assembly.GetExecutingAssembly();

                    foreach (string resourceName in assembly.GetManifestResourceNames().Where(name => name.EndsWith("ttf", StringComparison.OrdinalIgnoreCase)))
                    {
                        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            using (var streamReader = new MemoryStream())
                            {
                                stream.CopyTo(streamReader);
                                fontFile = streamReader.ToArray();
                            }
                        }

                        memory = Marshal.AllocCoTaskMem(fontFile.Length);

                        Marshal.Copy(fontFile, 0, memory, fontFile.Length);
                        privateFontCollection.AddMemoryFont(memory, fontFile.Length);
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(memory);
                }

                return privateFontCollection.Families;
            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : LoadFont() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return null;
            }
        }

        #endregion

        #region  Step2.1 clear
        [HttpPost]
        public JsonResult LogHappyWithPhoto()
        {
            string message = "Success";
            var service = new ContentService();
            try
            {
                if (Session["nodeid"] != null)
                {
                    var entry = service.GetById(Convert.ToInt32(Session["nodeid"]));
                    if (entry != null)
                    {
                        entry.SetValue("stepTakePhoto", 2);
                        service.SaveAndPublish(entry);

                    }
                    umbraco.library.UpdateDocumentCache(uQuery.GetNodesByType("Pledges").FirstOrDefault().Id);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS :LogStep2 Failed" +
                    Environment.NewLine + "Message: " + ex.Message + Environment.NewLine
                    + ex.StackTrace, ex);
                message = "Error";
            }

            var result = new { message = message };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult LogAnotherPhoto()
        {
            string message = "Success";
            var service = new ContentService();
            try
            {
                if (Session["nodeid"] != null)
                {
                    var entry = service.GetById(Convert.ToInt32(Session["nodeid"]));
                    if (entry != null)
                    {
                        entry.SetValue("stepTakePhoto", 1);
                        service.SaveAndPublish(entry);
                    }
                    umbraco.library.UpdateDocumentCache(uQuery.GetNodesByType("Pledges").FirstOrDefault().Id);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS :LogStep2 Failed" +
                    Environment.NewLine + "Message: " + ex.Message + Environment.NewLine
                    + ex.StackTrace, ex);
                message = "Error";
            }

            var result = new { message = message };
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion    
        
        #region Default Image Processing

        [HttpPost]
        public JsonResult DefaultImgProcessing(int orientation = 0)
        {
           
                    string imagePath = string.Empty, textToBeSentToYolk = string.Empty, message = "success";
                    try
                    {
                        Bitmap readyImg = Bitmap.FromFile(Server.MapPath("\\images/skeleton.png")) as Bitmap;
                        Bitmap sourceimage = Bitmap.FromFile(Server.MapPath("\\images/profilephoto.jpg")) as Bitmap;

                        Bitmap original = new Bitmap(sourceimage, readyImg.Size);


                        //determine orientation of image
                        //orientation = 1 -> don’t rotate
                        //orientation = 3 -> rotate 180° clockwise
                        //orientation = 6 -> rotate 90° clockwise
                        //orientation = 8 -> rotate -90° clockwise
                        switch (orientation)
                        {
                            case 3: original.RotateFlip(RotateFlipType.Rotate180FlipNone);
                                break;
                            case 6: original.RotateFlip(RotateFlipType.Rotate270FlipY);
                                break;
                            case 8: original.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                break;
                            case 1: original.RotateFlip(RotateFlipType.RotateNoneFlipNone);
                                break;
                            default: original.RotateFlip(RotateFlipType.RotateNoneFlipNone);
                                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                          "Rexona AUS : ImgProcessing() method:" + Environment.NewLine + "Message: "
                          + "Method execution completed - Orientation value is " + orientation);
                                break;
                        }



                        System.Drawing.Image first_1 = MakeGrayscale(original);


                        Bitmap second = SetImageOpacity(Bitmap.FromFile(Server.MapPath("~/images/metal-overlay.jpg")), 0.65f);

                        Bitmap result_2 = new Bitmap(readyImg.Width, readyImg.Height);

                        Graphics g2 = Graphics.FromImage(result_2);

                        g2.DrawImageUnscaled(first_1, 0, 0);

                        g2.DrawImageUnscaled(second, 0, 0);


                        BrightnessCorrection filter1 = new BrightnessCorrection(-10);
                        filter1.ApplyInPlace(result_2);

                        GaussianSharpen filter = new GaussianSharpen(100, 50);
                        filter.ApplyInPlace(result_2);

                        ContrastCorrection filter5 = new ContrastCorrection(100);
                        filter5.ApplyInPlace(result_2);
                        filter5.ApplyInPlace(result_2);

                        // g2.DrawImageUnscaled(readyImg, 0, 0);  

                        System.Drawing.Image final = MakeGrayscale(result_2);

                        Graphics g = Graphics.FromImage(final);
                        g.DrawImageUnscaled(readyImg, 0, 0);


                        FontFamily sophia = LoadFont().Where(family => family.Name == "Sofia Pro Black").First();
                        if (sophia == null)
                        {
                            var result1 = new
                            {
                                ImagePath = "",
                                TextToBeSentToYolk = "",
                                Message = "error"
                            };
                            return Json(result1, JsonRequestBehavior.AllowGet);
                        }

                        Font sofiaFontPledge = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["CharLimitpledge"]), FontStyle.Bold);
                        Font sofiaFontAuthor = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["authorFontBig"]), FontStyle.Bold);
                        Font sofiaFontIWILLDO = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["IwllFont"]), FontStyle.Bold);

                        //Align pledge name and author name center
                        StringFormat stringFormat = new StringFormat();
                        stringFormat.Alignment = StringAlignment.Center;
                        stringFormat.LineAlignment = StringAlignment.Center;


                        string PledgeTitle = string.Empty, Author = string.Empty;
                        if (Session["Title"] != null)
                        {
                            PledgeTitle = Session["Title"].ToString();
                        }

                        if (Session["DisplayName"] != null)
                        {
                            Author = Session["DisplayName"].ToString();
                            textToBeSentToYolk = Author;
                        }

                        int x = int.Parse(ConfigurationManager.AppSettings["XCords"]);
                        int y = int.Parse(ConfigurationManager.AppSettings["YCords"]);

                        if (!string.IsNullOrEmpty(PledgeTitle))
                        {
                            if (PledgeTitle.Length <= 30)
                            {
                                sofiaFontPledge = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["CharLimitpledge"]), FontStyle.Bold);
                            }
                            else if (PledgeTitle.Length <= 35)
                            {
                                sofiaFontPledge = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["CharLimitpledgeMed"]), FontStyle.Bold);
                            }
                            else
                            {
                                sofiaFontPledge = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["CharLimitpledgeTop"]), FontStyle.Bold);
                            }
                        }

                        if (!string.IsNullOrEmpty(Author))
                        {
                            if (Author.Length <= 15)
                            {
                                sofiaFontAuthor = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["authorFontBig"]), FontStyle.Bold);
                            }
                            else if (Author.Length <= 20)
                            {
                                sofiaFontAuthor = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["authorFontMedium"]), FontStyle.Bold);
                                y = int.Parse(ConfigurationManager.AppSettings["YMedCords"]);
                            }
                            else if (Author.Length >= 21)
                            {
                                sofiaFontAuthor = new Font(sophia, float.Parse(ConfigurationManager.AppSettings["authorFontTop"]), FontStyle.Bold);
                                y = int.Parse(ConfigurationManager.AppSettings["YTopCords"]);
                            }
                        }

                        //  Font font1 = new Font(FontName, pledgeTextFont, FontStyle.Bold);

                        float angle;
                        angle = -1F;

                        // g.TranslateTransform(0F, 11.5F); // offset the origin to our calculated values
                        g.RotateTransform(angle);
                        g.DrawString(PledgeTitle.ToUpper(), sofiaFontPledge, Brushes.Black, 235, 400, stringFormat);


                        //Font font2 = new Font(FontName, pledgeAuthorFont, FontStyle.Bold);

                        g.RotateTransform(angle);
                        g.TextRenderingHint = TextRenderingHint.AntiAlias;
                        g.DrawString(Author.ToUpper(), sofiaFontAuthor, Brushes.Black, x, y, stringFormat);


                        //  Font font = new Font(IWillFontName, iWillDoFont, FontStyle.Bold);

                        angle = -1F;
                        g.RotateTransform(angle);
                        g.DrawString(ConfigurationManager.AppSettings["PledgeTextPrefix"], sofiaFontIWILLDO, Brushes.Black, 11, 320);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;


                        int entryid = 0;
                        if (Session["nodeid"] != null)
                        {
                            int.TryParse(Session["nodeid"].ToString(), out entryid);
                        }

                        //Create unit folder for each user
                        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media") + @"\" + entryid);
                        string newResizedImagePath = (Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media") + @"\" + entryid) + "\\processedImage.png";
                        final.Save(newResizedImagePath, ImageFormat.Png);

                        var fs = new FileStream(newResizedImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                        string S3bucketPath = entryid + "\\processedImage.png";
                        amazonS3Provider.AddFile(S3bucketPath, fs);
                        fs.Dispose();
                        imagePath = S3BucketStaticPath + entryid + @"/processedImage.png";

                        //Delete the folder once processed image is sent to S3:

                        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media") + @"\" + entryid;

                        var directory = new DirectoryInfo(folderPath);
                        if (directory.Exists)
                        {
                            foreach (FileInfo filetodelete in directory.GetFiles())
                            {
                                filetodelete.Delete();
                            }
                            foreach (DirectoryInfo dir in directory.GetDirectories())
                            {
                                dir.Delete(true);
                            }

                            directory.Delete();
                        }

                        g2.Dispose();
                        g.Dispose();

                        var service = new ContentService();
                        var entry = service.GetById(entryid);
                        try
                        {
                            Node node = new Node(entryid);

                            entry.SetValue("step2Clear", true);
                            entry.SetValue("stepTakePhoto", 0);
                            entry.SetValue("imageUrl", imagePath);
                            service.SaveAndPublish(entry);

                        }
                        catch (Exception ex)
                        {
                            LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                                "Rexona AUS : ImgProcessing() method:" + Environment.NewLine + "Message: "
                                + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                        }
                        finally
                        {
                            umbraco.library.RefreshContent();
                        }

                    }
                    catch (Exception ex)
                    {
                        message = "error";
                        ViewBag.Message = "ERROR:" + ex.Message.ToString();
                        LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                            "Rexona AUS : ImgProcessing() method:" + Environment.NewLine + "Message: "
                            + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                    }

                    var result = new
                    {
                        ImagePath = imagePath,
                        TextToBeSentToYolk = Session["Author"] != null ? Session["Author"].ToString() : "",
                        Message = message
                    };

                    return Json(result, JsonRequestBehavior.AllowGet);
               
        }

        

        #endregion

        #region Remove Goal
        [HttpPost]
        public JsonResult RemoveGoal(int GoalId, int MemberId, int MemberCount)
        {
            bool status = false;          

           try
            {
                Member currentmember = Member.GetCurrentMember();
                if (currentmember != null && currentmember.Id > 0)
                {
                    var service = new ContentService();
                    Node entry = new Node(GoalId);
                    if (entry != null)
                    {  
                        if (MemberCount > 1)
                        {
                            int JoinedMemberId=0;
                            JoinedMemberId = entry.ChildrenAsList.Where(member => member.GetProperty<int>("memberId") == currentmember.Id && member.GetProperty<bool>("isDeleted") == false).Select(a => a.Id).ToList()[0];
                            if (JoinedMemberId > 0)
                            {
                                var JoinedMember = service.GetById(JoinedMemberId);
                                if (JoinedMember != null)
                                {
                                    JoinedMember.SetValue("isDeleted", true);
                                    service.SaveAndPublish(JoinedMember);
                                    status = true;
                                }
                            }
                        }
                        else
                        {
                            var JoinedMember = service.GetById(GoalId);
                            if (JoinedMember != null)
                            {
                                service.Delete(JoinedMember);
                                status = true;
                            }
                        }                    
                    }
                }
                return Json(new { status = status, message = "Success" }, JsonRequestBehavior.AllowGet);
           }
           catch (Exception ex)
            {
                LogHelper.Error(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "DeleteGoal Failed "
                + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace, ex);
                return Json(new { status = false, message = "Something went wrong" }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                umbraco.library.RefreshContent();
            }   
        }
        #endregion 

        #region Dashboard_Gallery

        [HttpGet]
        public JsonResult GetPledgesDashboard(int PageSize, int currentPageIndex, string sortingText)
        {
            string message = "error";
            List<PledgeGalleryModel> lstPledges = GetAllPledgesDashboard();
            try
            {
                if (lstPledges != null && lstPledges.Count > 0)
                {
                    lstPledges = lstPledges.Where(a => a.Step3Clear).ToList();
                    if (!string.IsNullOrEmpty(sortingText) && sortingText.Equals("popular", StringComparison.OrdinalIgnoreCase))
                    {
                        lstPledges = lstPledges.OrderByDescending(a => a.LikeCount).ThenByDescending(a => a.CreatedDate).Take(4).ToList<PledgeGalleryModel>();
                        
                    }
                    else
                    {
                        lstPledges = lstPledges.OrderByDescending(a => a.CreatedDate).ToList<PledgeGalleryModel>();
                    }

                    var result = new
                    {
                        pledgesDashboard = lstPledges.Skip(currentPageIndex * PageSize).Take(PageSize),
                        totalPages = Math.Ceiling((decimal)(lstPledges.Count / (decimal)PageSize)),
                        message = "success"
                    };

                    return Json(result, JsonRequestBehavior.AllowGet);
                }

                return Json(new { totalPages = 0, message = "No Pledges Found" }, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetPledges() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                return base.Json("Please try again", "text/plain");
            }
        }

        public List<PledgeGalleryModel> GetAllPledgesDashboard()
        {
            DynamicNode pledges = new DynamicNode(uQuery.GetNodesByType("Pledges").FirstOrDefault().Id);
            List<PledgeGalleryModel> model = new List<PledgeGalleryModel>();
            int memberId = 0;
            Member currentmember = Member.GetCurrentMember();
            if (currentmember != null)
            {
                memberId = currentmember.Id;
            }
            if (pledges != null)
            {
                try
                {
                    foreach (var childNode in pledges.Children)
                    {
                        if (childNode.Children.Count() > 0)
                        {
                            var childItems = childNode.Children.Items;
                            string publicpledge = childNode.GetProperty("publicPledgeSelection").Value;
                            if (publicpledge == "1")
                            {
                                model.Add(new PledgeGalleryModel
                                {
                                    Id = childNode.Id,
                                    ImageURL = childItems.Count() > 0 ? childItems.Where(author => author.GetProperty("isOwner").Value == "1").Select(l => l.GetProperty("imageUrl").Value).FirstOrDefault().ToString() : string.Empty,
                                    IsPublicSelection = childNode.GetProperty("publicPledgeSelection").Value,
                                    PledgeNodeId = childNode.Id,
                                    MemberCount = childItems.Where(a => a.GetProperty<bool>("step3Clear")).ToList().Count,
                                    LikeCount = childNode.GetProperty<int>("likeCount"),
                                    CreatedDate = Convert.ToDateTime(childNode.CreateDate),
                                    PledgeURL = childNode.NiceUrl,
                                    IsMember = memberId != 0 ? childNode.ChildrenAsList.Where(a => a.GetProperty<int>("memberId") == memberId).Count() > 0 : false,
                                    Step3Clear = childItems.Count() > 0 ? childItems.Where(author => author.GetProperty("isOwner").Value == "1").Select(a => a.GetProperty<bool>("step3Clear")).ToList<bool>()[0] : false,
                                    PledgeTitle = childNode.GetProperty("title").Value
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType, "Rexona AUS : GetAllPledges() method:" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "Stack Trace: " + ex.StackTrace);
                }
            }
            return model;

        }

        #endregion
    }
}