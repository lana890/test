//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using EskaCommerce.ShippingPrices.Services;
//using EskaCMS.Infrastructure.Data;
//using EskaCMS.Core.Services;
//using EskaCMS.Core.Models;
//using System;
//using System.Net;
//using System.Xml.Serialization;
//using System.IO;
//using System.Xml;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Globalization;

//using System.Xml.Linq;
//using EskaCMS.Entities.Entity.Catalog;
//using EskaCMS.Entities.Entity;
//using EskaCMS.Entities.Entity.Addresses;
//using EskaCMS.Entities.Entity.Shipping;
//using EskaCMS.Entities.Entity.ShippingTableRate;
//using EskaCMS.Entities.Entity.ShoppingCart;
//using EskaCMS.Entities.Entity.Core;
//using EskaCommerce.Shipments.Services.ShippingJeeblyService.Interfaces;


//namespace EskaCommerce.ShippingTableRate.Services
//{
//    public class TableRateShippingServiceProvider : IShippingPriceServiceProvider
//    {
//        public readonly IRepository<PriceAndDestination> _priceAndDestinationRepository;
//        private readonly IRepositoryWithTypedId<SiteSettings, string> _appSettingRepository;
//        private readonly ICurrencyService _currencyService;
//        private readonly IRepository<ProductAttributeValue> _productAttributeRepository;
//        private readonly IRepository<StateOrProvince> _statesRepository;
//        private readonly IJeeblyService _jeeblyService;

//        public TableRateShippingServiceProvider(IRepository<PriceAndDestination> priceAndDestinationRepository, ICurrencyService currencyService,
//            IRepositoryWithTypedId<SiteSettings, string> appSettingRepository,
//            IRepository<ProductAttributeValue> productAttributeRepository,
//            IRepository<StateOrProvince> statesRepository
//           )
//        {
//            _priceAndDestinationRepository = priceAndDestinationRepository;
//            _currencyService = currencyService;
//            _appSettingRepository = appSettingRepository;
//            _productAttributeRepository = productAttributeRepository;
//            _statesRepository = statesRepository;
           
//        }

//        public async Task<GetShippingPriceResponse> GetShippingPrices(GetShippingPriceRequest request, ShippingProvider provider, Cart cart)
//        {
//            try
//            {
//                var response = new GetShippingPriceResponse { IsSuccess = true };
//                var priceAndDestinations = await _priceAndDestinationRepository.Query().ToListAsync();

//               // var dhlIntegration = await _appSettingRepository.Query().Where(x => x.Id == "DHLIntegration").Select(x => x.Value).FirstOrDefaultAsync();
//                var query = priceAndDestinations.Where(x =>
//                    (x.CountryId == null || x.CountryId == request.ShippingAddress.CountryId)
//                    && (x.StateOrProvinceId == null || x.StateOrProvinceId == request.ShippingAddress.StateOrProvinceId)
//                    && (x.DistrictId == null || x.DistrictId == request.ShippingAddress.DistrictId)
//                    && (x.ZipCode == null || x.ZipCode == request.ShippingAddress.ZipCode)
//                    && request.OrderAmount >= x.MinOrderSubtotal);
//                int numofItems = 0;
//                foreach (var item in request.CartItems)
//                {
//                    numofItems = numofItems + item.Quantity;
//                }
//                var cheapestApplicable = query.OrderBy(x => x.ShippingPrice).FirstOrDefault();

//                if (cheapestApplicable != null)
//                {
//                    decimal decResponse = 0;
                
//                    //  dhlQuote(cart, request, out decResponse, _currencyService.CurrencyCulture.LCID);
//                    if (decResponse > 0)
//                    {
//                        response.ApplicablePrices.Add(new ShippingPrice(_currencyService)
//                        {
//                            Name = "Standard",
//                            Price = decResponse
//                        });

//                    }
//                    else
//                    {

//                        response.IsSuccess = false;
//                        response.Error = "Something Wrong";
//                    }
//                }
//                return response;
//            }
//            catch (Exception exc)
//            {

//                throw exc;
//            }
//        }
//        public async Task<GetShippingPriceResponse> GetShippingPrices(GetShippingPriceRequest request, ShippingProvider provider)
//        {
//            var response = new GetShippingPriceResponse { IsSuccess = true };
//            var priceAndDestinations = await _priceAndDestinationRepository.Query().ToListAsync();
//            var settings = await _appSettingRepository.Query()
//               .Where(x => x.IsVisibleInCommonSettingPage && x.Id == "ShipingPerItem" && x.Value == "true")
//               .FirstOrDefaultAsync();
//            var dhlIntegration = await _appSettingRepository.Query().Where(x => x.Id == "DHLIntegration").Select(x => x.Value).FirstOrDefaultAsync();
//            var query = priceAndDestinations.Where(x =>
//                (x.CountryId == null || x.CountryId == request.ShippingAddress.CountryId)
//                && (x.StateOrProvinceId == null || x.StateOrProvinceId == request.ShippingAddress.StateOrProvinceId)
//                && (x.DistrictId == null || x.DistrictId == request.ShippingAddress.DistrictId)
//                && (x.ZipCode == null || x.ZipCode == request.ShippingAddress.ZipCode)
//                && request.OrderAmount >= x.MinOrderSubtotal);
//            int numofItems = 0;
//            foreach (var item in request.CartItems)
//            {
//                numofItems = numofItems + item.Quantity;
//            }
//            var cheapestApplicable = query.OrderBy(x => x.ShippingPrice).FirstOrDefault();

//            if (cheapestApplicable != null)
//            {

//                if (settings != null)
//                {
//                    var NoOfItems = 0;
//                    if (query.Count() > 1)
//                        NoOfItems = query.FirstOrDefault(x => x.CountryId == request.ShippingAddress.CountryId).NoOfItems.Value;
//                    else
//                        NoOfItems = query.FirstOrDefault().NoOfItems.Value;
//                    var rate = Math.Ceiling((decimal)numofItems / NoOfItems);

//                    response.ApplicablePrices.Add(new ShippingPrice(_currencyService)
//                    {
//                        Name = "Standard",
//                        Price = cheapestApplicable.ShippingPrice * rate
//                    });
//                }
//                else
//                {


//                    response.ApplicablePrices.Add(new ShippingPrice(_currencyService)
//                    {
//                        Name = "Standard",
//                        Price = cheapestApplicable.ShippingPrice
//                    });
//                }



//            }
//            return response;
//        }


//        private void dhlQuote(Cart order, GetShippingPriceRequest request, out decimal shippingRate, int LCID)
//        {

//            try
//            {

//                shippingRate = 0;
//                var XML = _appSettingRepository.Query().Where(x => x.Id == "DHLQouta").Select(x => x.Value).FirstOrDefault();
//                var regionInfo = new RegionInfo(LCID);
//                var url = _appSettingRepository.Query().Where(x => x.Id == "DHLApiURL").Select(x => x.Value).FirstOrDefault();
//                XML = XML.Replace("[Currency]", regionInfo.ISOCurrencySymbol);
//                XML = XML.Replace("[Date]", DateTime.Now.ToString("yyyy-MM-dd"));
//                XML = XML.Replace("[Country]", request.ShippingAddress.CountryId);
//                if (!string.IsNullOrEmpty(request.ShippingAddress.ZipCode))
//                {
//                    XML = XML.Replace("[PostalCode]", $"<Postalcode>{request.ShippingAddress.ZipCode}</Postalcode>");
//                }
//                else
//                {
//                    var StateOrProvinceName = string.Empty;
//                    //if (request.ShippingAddress.StateOrProvince != null)
//                    //    StateOrProvinceName = request.ShippingAddress.StateOrProvince.Name;
//                    //else
//                    //    StateOrProvinceName = _statesRepository.Query().Where(x => x.Id == request.ShippingAddress.StateOrProvinceId).Select(x => x.Name).FirstOrDefault();

//                    XML = XML.Replace("[PostalCode]", $"<City>{StateOrProvinceName}</City>");
//                }
//                decimal quantity = 0;
//                foreach (var item in order.Items)
//                {
//                    var weightStr = _productAttributeRepository.Query().Include(x=>x.AttributeValueDetails).Where(x => x.AttributeId == 4 && x.ProductId == item.ProductId).SelectMany(x => x.AttributeValueDetails).Select(x=>x.Value).FirstOrDefault();
//                    string weight = Regex.Replace(weightStr, "[^0-9.]", "");

//                    quantity += Convert.ToDecimal(weight) * item.Quantity;
//                }
//                XML = XML.Replace("[Weight]", quantity.ToString());
//                string Response = "";


//                try
//                {
//                    HttpWebRequest myReq = null;

//                    myReq = WebRequest.Create(url) as HttpWebRequest;
//                    myReq.ContentType = "application/x-www-form-urlencoded";
//                    myReq.Method = "POST";

//                    using (System.IO.Stream stream = myReq.GetRequestStream())
//                    {
//                        byte[] arrBytes = ASCIIEncoding.ASCII.GetBytes(XML);
//                        stream.Write(arrBytes, 0, arrBytes.Length);
//                        stream.Close();
//                    }

//                    WebResponse myRes = myReq.GetResponse();
//                    System.IO.Stream respStream = myRes.GetResponseStream();
//                    System.IO.StreamReader reader = new System.IO.StreamReader(respStream, System.Text.Encoding.ASCII);
//                    Response = reader.ReadToEnd();
//                    myRes.Close();
//                    myRes = null;
//                }
//                catch (Exception ex)
//                {
//                    Response = ex.ToString();
//                }
//                var response = XDocument.Parse(Response);
//                var doc = new XmlDocument();
//                doc.LoadXml(Response);
//                var root = response.Root.Element("GetQuoteResponse");
//                var Error = root.Element("Note");
//                var Success = root.Element("BkgDetails");
//                if (Error == null && Success != null)
//                {
//                    var ExchangeRate = Convert.ToDecimal(doc.GetElementsByTagName("ExchangeRate")[0].InnerText);
//                    var WeightCharge = Convert.ToDecimal(doc.GetElementsByTagName("WeightCharge")[0].InnerText);
//                    var WeightChargeTax = Convert.ToDecimal(doc.GetElementsByTagName("WeightChargeTax")[0].InnerText);
//                    shippingRate = (WeightChargeTax + WeightCharge) * ExchangeRate;
//                }
//                else
//                {
//                    shippingRate = -1;
//                }

//            }
//            catch
//            {

//                shippingRate = -1;
//            }

//        }
//        public string SerializeToXML<T>(T toSerialize)
//        {
//            string Result = "";

//            XmlSerializerNamespaces ns = new XmlSerializerNamespaces(); ns.Add("", "");
//            using (TextWriter tw = new StringWriter())
//            {
//                using (XmlWriter writer = XmlWriter.Create(tw, new XmlWriterSettings { OmitXmlDeclaration = true }))
//                {
//                    new XmlSerializer(typeof(T)).Serialize(writer, toSerialize, ns);
//                    Result = tw.ToString();
//                }
//            }

//            return Result;
//        }
//        public T DeserializeFromXML<T>(string xml)
//        {
//            var serializer = new XmlSerializer(typeof(T));
//            return (T)serializer.Deserialize(new StringReader(xml));
//        }
//    }
//}
