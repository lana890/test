using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.IO;
using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
//using OpenQA.Selenium.Chrome;


namespace EskaCMS.HOC.StaticCashing.Helpers
{
    public class ScrapingHelper
    {
        private static readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();

        private static string[] Temp = new[]
        {
            
            "Resources/styles/siteId_3/primaryStyle.css",
            "Resources/3/Inner_assets-search2.jpg",
            "Resources/3/Inner_assets-lab-contact.jpeg",
            "Resources/3/AMSA-icon.png",
            "Resources/3/Brands-Ansell.webp",
            "Resources/3/Home_assets-Banner-arrow.png",
            "Resources/3/Main_assets-AMSA-grey-logo.png",
            "Resources/3/Main_assets-Skills_Every_Business_Development_Pro_Should_Have.png",
            "Resources/3/Main_assets-healthcare-technology.png",
            "Resources/3/Main_assets-EHR-systems-for-hospitals.png",
            "Resources/3/Home_assets-partners_assets-Vector_Smart_Object-4.png",
            "Resources/3/Home_assets-partners_assets-Vector_Smart_Object-3.png",
            "Resources/3/Home_assets-partners_assets-Vector_Smart_Object-2.png",
            "Resources/3/Home_assets-partners_assets-Vector_Smart_Object-1.png",
            "Resources/3/Home_assets-partners_assets-Vector_Smart_Object.png",
            "Resources/3/Main_assets-AMSA-logo-color.png",
            "Resources/3/Main_assets-copyright-logo1.png",
            "Resources/3/Main_assets-copyright-logo2.png",
            "Resources/2/Home_assets-eska_boat.png",
            "Resources/3/Home_assets-partners_assets-partners-sec-lace.png",
           
        };

        private static string[] Bundels = new string[]{


"assets/bundels/models/bundles/models.umd.min.js",
"scripts.js",
"assets/bundels/shared/bundles/services.umd.min.js",
"assets/bundels/shared/bundles/shared-module.umd.min.js",
"assets/bundels/shared/bundles/shared-components.umd.min.js",
"assets/bundels/ecommerce-product-details/bundles/ecommerce-product-details.umd.js",
"assets/bundels/header/bundles/header.umd.js",
"assets/bundels/menu-details/bundles/menu-details.umd.js",
"assets/bundels/breadcrumb/bundles/breadcrumb.umd.js",
"assets/bundels/sub-menu/bundles/sub-menu.umd.js",
"assets/bundels/sitemap/bundles/sitemap.umd.js",
"assets/bundels/search/bundles/search.umd.js",
"assets/bundels/search-result/bundles/search-result.umd.js",
"assets/bundels/social-share-links/bundles/social-share-links.umd.js",
"assets/bundels/form-builder/bundles/form-builder.umd.js",
"assets/bundels/data-grid/bundles/data-grid.umd.js",
"assets/bundels/subscribe/bundles/subscribe.umd.js",
"assets/bundels//bundles/.umd.js",
"assets/bundels/login-client/bundles/login-client.umd.js",
"assets/bundels/data-html/bundles/data-html.umd.js",
"assets/bundels/image/bundles/image.umd.js",
"assets/bundels/blog-details/bundles/blog-details.umd.js",
"assets/bundels/photo-gallery/bundles/photo-gallery.umd.js",
"assets/bundels/blog-archive/bundles/blog-archive.umd.js",
"assets/bundels/static-article/bundles/static-article.umd.js",
"assets/bundels/menu/bundles/menu.umd.js",
"assets/bundels/gallery/bundles/gallery.umd.js",
"assets/bundels/user-profile/bundles/user-profile.umd.js",
"assets/bundels/banner/bundles/banner.umd.js",
"assets/bundels/paragraph/bundles/paragraph.umd.js",
"assets/bundels/video/bundles/video.umd.js",
"assets/bundels/marquee/bundles/marquee.umd.js",
"assets/bundels/popover/bundles/popover.umd.js",
"assets/bundels/button/bundles/button.umd.js",
"assets/bundels/calendar/bundles/calendar.umd.js",
"assets/bundels/faq/bundles/faq.umd.js",
"assets/bundels/payment/bundles/payment.umd.js",
"assets/bundels/category/bundles/category.umd.js",
"assets/bundels/ecommerce-category-details/bundles/ecommerce-category-details.umd.js",
"assets/bundels/ecommerce-view-cart/bundles/ecommerce-view-cart.umd.js",
"assets/bundels/shipping-address/bundles/shipping-address.umd.js",
"assets/bundels/ecommerce-widget/bundles/ecommerce-widget.umd.js",
"assets/bundels/ecommerce-categories/bundles/ecommerce-categories.umd.js",
"assets/bundels/ecommerce-brands/bundles/ecommerce-brands.umd.js",
"assets/bundels/cp-profile/bundles/cp-profile.umd.js",
"assets/bundels/policy-list/bundles/policy-list.umd.js",
"assets/bundels/cp-statment-of-account/bundles/cp-statment-of-account.umd.js",
"assets/bundels/cp-track-claims/bundles/cp-track-claims.umd.js",
"assets/bundels/cp-login/bundles/cp-login.umd.js",
"assets/bundels/cp-change-password/bundles/cp-change-password.umd.js",
"assets/bundels/newletter/bundles/newletter.umd.js",

            };

        private readonly string BaseUrl;
        private readonly string DestinationUrl;
        private readonly long SiteId;


        public ScrapingHelper(string BaseUrl, string BaseUrlPrefix, string DestinationUrl,long SiteId)
        {
            if (SiteId == 0) throw new ArgumentException("SiteId is required");
            if (string.IsNullOrEmpty(BaseUrl)) throw new ArgumentException("BaseUrl is required");
            if (string.IsNullOrEmpty(DestinationUrl)) throw new ArgumentException("DestinationUrl is required");

            this.BaseUrl = string.IsNullOrEmpty(BaseUrlPrefix)?BaseUrl:ConcateLinks( BaseUrl,BaseUrlPrefix);
            this.DestinationUrl = string.IsNullOrEmpty(BaseUrlPrefix)? DestinationUrl : ConcateLinks(DestinationUrl, BaseUrlPrefix);
            this.SiteId = SiteId;
        }

        #region Main Methods
        public async Task Scrape(string Slug)
        {
            try
            {
                var html = await GetHtml(Slug);
                SaveFile(html,Slug+"/index.html");
                await CashResources(html);
                await ScrapeCSSFiles();
            }
            catch (Exception e)
            {

                throw;
            }

        }

        public async Task ScrapeCSSFiles()
        {
            foreach (var BundelLink in Temp)
            {
                await SaveResource(BundelLink);
            }


            await SaveResource("styles.css");
            await SaveResource(string.Format("Resources/styles/siteId_{0}/primaryStyle.css", this.SiteId));
        }

        public async Task ScrapeAllSlugs(List<SlugInfo> SlugInfos)
        {
          //  await SaveResource(BundelLink);
        }

        #endregion



        //private static async  Task<string> GetHtml()
        //{
        //    var options = new ChromeOptions
        //    {
        //        BinaryLocation = @"C:\Program Files(x86)\Google\Chrome\Application\chrome.exe"
        //    };

        //    options.AddArguments("headless");

        //    var chrome = new ChromeDriver(options);
        //    chrome.Navigate().GoToUrl("http://172.16.0.45:4333/amsa/about-us");
        //    //chrome.GetScreenshot();
        //    return chrome.PageSource;
        //}



        private async Task CashResources(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            foreach (var style in htmlDoc.DocumentNode.SelectNodes("//link"))
            {
                var att = style.Attributes.Where(s => s.Name.ToLower() == "href").FirstOrDefault();
                string link = att != null ? att.Value : "";
                if(!string.IsNullOrEmpty(link))
                    await SaveResource(link);
            }

            foreach (var scriptTag in htmlDoc.DocumentNode.SelectNodes("//script"))
            {
                var att = scriptTag.Attributes.Where(s => s.Name.ToLower() == "src").FirstOrDefault();
                string link = att != null ? att.Value : "";
                if (!string.IsNullOrEmpty(link))
                    await SaveResource(link);
            }
            
            foreach (var BundelLink in Bundels)
            {
                if (!string.IsNullOrEmpty(BundelLink))
                    await SaveResource(BundelLink);
            }
            
         
        }

        #region Getting Methods
        private async Task<string> GetHtml(string PageUrl)
        {
            return await getFileContent(PageUrl);
        }
        private async Task<string> getFileContent(string path)
        {
            string url = ConcateWithBaseUrl(path);
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return "";

        }

        #endregion

        #region Saving Methods
        private async Task SaveResource(string path)
        {
            try
            {
                var FileContent = await getFileContent(path);
                SaveFile(FileContent, path);


            }
            catch (Exception e)
            {

                int x = 0;
            }
        }

        private void SaveFile(string Content, string path)
        {
            path = ConcateWithDestinationUrl(path);
            path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));
            var fileName = path.Split("\\").LastOrDefault();
            path = path.Replace("\\" + fileName, "");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            System.IO.StreamWriter file = new System.IO.StreamWriter(path + "/" + fileName);
            file.WriteLine(Content);

            file.Close();





        }
        #endregion





        #region ConcateLinks Helpers

        private string ConcateWithBaseUrl(string path)
        {
            return ConcateLinks(BaseUrl, path);
        }

        private string ConcateWithDestinationUrl(string path)
        {
            return ConcateLinks(DestinationUrl, path);
        }

        private string ConcateLinks(string Url, string link)
        {
            string destination = "";
            if (link.StartsWith("."))
            {
                destination += link.Substring(1);
            }
            else if (link.StartsWith("/"))
            {
                destination += link;

            }
            else if (link.StartsWith("http"))
            {
                return link;
            }
            else
            {
                destination += "/" + link;
            }
            return Url + destination;
        }

        #endregion

    }
}