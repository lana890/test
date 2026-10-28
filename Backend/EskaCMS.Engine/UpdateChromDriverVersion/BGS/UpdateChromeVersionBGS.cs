using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Core.Data;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using System.Threading;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.IO.Compression;
using EskaCMS.Engine;

namespace EskaCMS.Enigine.UpdateChromDriverVersion.BGS
{
    public class UpdateChromeVersionBGS : BaseBackgroundService
    {
        private readonly IConfiguration _configuration;
        public readonly ILogger<UpdateChromeVersionBGS> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource tokenSource = new();
        private readonly string _BackgroundServiceName;


        public UpdateChromeVersionBGS(ILogger<UpdateChromeVersionBGS> logger, IServiceProvider serviceProvider, IConfiguration configuration)
            : base(nameof(UpdateChromeVersionBGS), logger, serviceProvider, configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            //_BackgroundServiceName = BackgroundServiceName;
            _configuration = configuration;


        }




        public override async Task<bool> Execute()
        {
            try
            {
               
                string chromePath = _configuration.GetSection("SSR:ChromePath")?.Value.ToString();
                //get driver version
                IWebDriver driver = new ChromeDriver("./");
                ICapabilities capabilities = ((WebDriver)driver).Capabilities;
                var  DriverVersion=(capabilities.GetCapability("chrome") as Dictionary<string, object>)["chromedriverVersion"];
               
                string ChromeVersion = FileVersionInfo.GetVersionInfo(chromePath.ToString()).FileVersion;
                var DchromeVersion = DriverVersion.ToString().Substring(0, DriverVersion.ToString().LastIndexOf('('));

                if (DchromeVersion!= ChromeVersion)
                {
                    DownloadChromeDriver(ChromeVersion);

                }
            }
           
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }
        public static void DownloadChromeDriver(string ChromeVersion)
        {
            try
            {
                using(var client=new WebClient())
                {
                   client.DownloadFile("https://chromedriver.storage.googleapis.com/" + ChromeVersion + "/chromedriver_win32.zip", "ChromeDriver.zip");
                    //client.DownloadFile("https://chromedriver.storage.googleapis.com/index.html?path=" + ChromeVersion + "/chromedriver_win32.zip", "chromedriver_win32.zip");

                }
                Thread th = new Thread(extractZip);th.Start();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        public static void extractZip()
        {
            for( ; ; )
            {
                Thread.Sleep(1000);
                if (File.Exists("chromedriver.zip") == true)
                {
                    //ZipFile.ExtractToDirectory()
                }
            }
        }
    }
}
