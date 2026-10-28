using EskaCMS.Core.Cache;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using System.Web;

namespace EskaCMS.SSR.Controllers;

[ApiController]
[Route("[controller]")]
public class SSRController : ControllerBase
{


    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService;

    public SSRController(ICacheService cacheService, IConfiguration configuration)
    {
        _cacheService = cacheService;
        _configuration = configuration;
    }



    // NOTE!!
    // In Order to Make this api work you need 2 things
    // first the below rewrite rule
    // seconde to activate cashing from appsettings.json
    /* 
     
	<rule name="Redirect Crawler SSR" stopProcessing="true"> 
             <match url="(.*)" /> 
            <conditions logicalGrouping="MatchAll" trackAllCaptures="true">
                 <add input="{REQUEST_URI}" pattern="robots.txt$" negate="true" />
                 <add input="{HTTP_USER_AGENT}" pattern="googlebot|Google \(\+https|adsbot-google|mediapartners-google|googleimageproxy|facebot|Twitterbot|facebookexternalhit|ia_archiver|baiduspider|sogou|360Spider|mj12bot|bingbot|simplepie\/|yahoo! slurp|duckduckbot|yandex\.ru\/bots|yandex\.net\/bots|yandex\.com\/bots|Exabot|Slackbot-LinkExpanding|MSOffice|Microsoft Outlook|SkypeUriPreview|LinkedInBotGoogle-Extended|GoogleOther|Google-InspectionTool|Storebot-Google|Googlebot-News|Googlebot-Image|Googlebot|APIs-Google|AdsBot-Google-Mobile|AdsBot-Google|Mediapartners-Google|FeedFetcher-Google|PostmanRuntime|WhatsApp" />
                <add input="{QUERY_STRING}" pattern="(.*)" />   
                <add input="{HTTP_PROTOCOL}" pattern="(.*)" />  

          </conditions> 
            <action type="Rewrite" url="http://localhost:8787/SSR/SimulateSSR?url=https://{HTTP_HOST}/{UNENCODED_URL}?{QUERY_STRING}" appendQueryString="true" logRewrittenUrl="false" />
     </rule>
    */


    [HttpGet("SimulateSSR")]
    [AllowAnonymous]
    public async Task<IActionResult> SimulateSSR([FromQuery] string url)
    {
        try
        {
            url = url.Replace("//", "/").Replace("https:/", "https://").Replace("http:/", "http://").ToString();


            var cacheData = _cacheService?.GetData<ContentResult>("SSR" + url);

            if (cacheData != null)
            {

                return cacheData;
            }



            var html = await GetHtml(HttpUtility.UrlDecode(url));

            var result = new ContentResult()
            {
                ContentType = "text/html",
                Content = html
            };

            _cacheService.SetData<ContentResult>("SSR" + url, result, DateTimeOffset.Now.AddDays(double.Parse(_configuration.GetSection("CacheConfig:CachePeriod").Value)));

            return result;

        }

        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }


    }


    [NonAction]
    private async Task<string> GetHtml(string Url)
    {

        try
        {


            var options = new LaunchOptions
            {
                Headless = true,
                Timeout = 10000
            };

            // Launch a new browser instance
            using var browser = await Puppeteer.LaunchAsync(options);

            // Create a new page in the browser
            var pages = await browser.PagesAsync();
            using var page = pages.FirstOrDefault();

            // Navigate to a website
            await page.GoToAsync(Url);
            await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions()
            {
                Timeout = 1000000,
                IdleTime = 200
            });

            var pageTitle = await page.GetTitleAsync();
            var pageContent = await page.GetContentAsync();
            await browser.CloseAsync();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageContent);
             
            foreach (var style in htmlDoc.DocumentNode.SelectNodes("//style"))
            {
                style.Remove();

            }
            foreach (var style in htmlDoc.DocumentNode.SelectNodes("//meta"))
            {
                if (style.Attributes.Any(x => x.Name.ToLower() == "content" &&x.Value == string.Empty))
                {

                style.Remove();
                }

            }
            return htmlDoc.DocumentNode.InnerHtml;

        }
        catch (Exception)
        {
            await new BrowserFetcher().DownloadAsync();

            throw;
        }

    }
}
