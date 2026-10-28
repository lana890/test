using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QRCoder;
using System;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public class LoyalityService : ILoyalityService
    {
        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
        private readonly ILogger _logger;
        public LoyalityService(IRepositoryWithTypedId<SiteSettings, string> appSettingRepository, ILoggerFactory loggerFactory)
        {
            _appSettingRepository = appSettingRepository;
            _logger = loggerFactory.CreateLogger<LoyalityService>();
        }
        public async Task<CustomerLoyalityObjectVm> GetCustomer(string Phone)
        {
            var urlSetting = await _appSettingRepository.Query().FirstOrDefaultAsync(x => x.Id == "LoyalityPointURL");
            string stripSetting = urlSetting.Value;
            WebResponse httpResponse;
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(stripSetting + "CustomerTransactions/GetCustomerBalances?sMobileNumber=" + Phone);
            httpRequest.Method = "GET";
            httpRequest.ContentType = "application/json";
            httpRequest.Credentials = CredentialCache.DefaultCredentials;
            httpResponse = httpRequest.GetResponse();
            CustomerLoyalityObjectVm result = new CustomerLoyalityObjectVm();
            string resultreader = "";
            using (var reader = new StreamReader(httpResponse.GetResponseStream()))
            {
                resultreader = reader.ReadToEnd(); // do something fun...
            }
            CustomerLoyalityVm couponResult = JsonConvert.DeserializeObject<CustomerLoyalityVm>(resultreader);
            CustomerLoyalitydataVm coupondataResult = new CustomerLoyalitydataVm();
            if (string.IsNullOrEmpty(couponResult.Data) || couponResult.Data == "null")
            {
                result = null;
            }
            else
            {
                coupondataResult = JsonConvert.DeserializeObject<CustomerLoyalitydataVm>(couponResult.Data);
                coupondataResult.Balance = Math.Round(coupondataResult.Balance / coupondataResult.Exrate, 2);
                result.Data = coupondataResult;
                result.Counter = couponResult.Counter;
                result.ErrorCode = couponResult.ErrorCode;
                result.ErrorMessage = couponResult.ErrorMessage;
                result.IsError = couponResult.IsError;
            }


            return result;
        }
        public async Task<dynamic> SaveCustomer(string CustomerName, string Phone, string Email)
        {
            var urlSetting = await _appSettingRepository.Query().FirstOrDefaultAsync(x => x.Id == "LoyalityPointURL");
            string stripSetting = urlSetting.Value;
            WebResponse httpResponse;
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(stripSetting + "CustomerTransactions/SaveCustomer?CustomerName=" + CustomerName + "&Phone=" + Phone + "&Email=" + Email);
            httpRequest.Method = "GET";
            httpRequest.ContentType = "application/json";
            httpRequest.Credentials = CredentialCache.DefaultCredentials;
            httpResponse = httpRequest.GetResponse();
            dynamic result = new ExpandoObject();
            using (var reader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = reader.ReadToEnd(); // do something fun...
            }
            return result;

        }
        public async Task<dynamic> InsertTransaction(string Email, string Phone, decimal TotalNetAmount, decimal UsedCash
            , decimal UsedPoints, string CurrencyCode)
        {
            var urlSetting = await _appSettingRepository.Query().FirstOrDefaultAsync(x => x.Id == "LoyalityPointURL");
            string stripSetting = urlSetting.Value;
            WebResponse httpResponse;
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(stripSetting + "CustomerTransactions/InsertTransactions?Email=" + Email + "&Phone=" + Phone + "&TotalNetAmount=" + Math.Round(TotalNetAmount, 2) + "&UsedCash=" + Math.Round(UsedCash, 2) + "&UsedPoints=" + Math.Round(UsedPoints, 2) + "&CurrencyCode=" + CurrencyCode);
            httpRequest.Method = "GET";
            httpRequest.ContentType = "application/json";
            httpRequest.Credentials = CredentialCache.DefaultCredentials;
            httpResponse = httpRequest.GetResponse();
            dynamic result = new ExpandoObject();
            string log = "";
            using (var reader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = reader.ReadToEnd(); // do something fun...
                log = reader.ReadToEnd();
            }

            _logger.LogInformation(1, log);
            return result;

        }

        public async Task<string> GenerateQRCode(string Phone)
        {

            //QRCodeGenerator qrGenerator = new QRCodeGenerator();
            //QRCodeData qrCodeData = qrGenerator.CreateQrCode(Phone, QRCodeGenerator.ECCLevel.Q);
            //QRCode qrCode = new QRCode(qrCodeData);
            //Bitmap qrCodeImage = qrCode.GetGraphic(20);
            //var imgByte = (byte[])new ImageConverter().ConvertTo(qrCodeImage, typeof(byte[]));
            //var img = Convert.ToBase64String(imgByte);
            //return img;
            return string.Empty;
        }

    }

}
