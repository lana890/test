using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Entities.Entity.Shipping;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Aramex.Data
{
    public class AramexCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {

            //modelBuilder.Entity<ShippingProvider>().HasSeedData(new ShippingProvider("Aramex")
            //{
            //    AdditionalSettings = "{  \"clientInfo\": {    \"userName\": \"testingapi@aramex.com\",    \"password\": \"R123456789$r\",    \"version\": \"1.0\",    \"accountNumber\": \"20016\",    \"accountPin\": \"331421\",    \"accountEntity\": \"AMM\",    \"accountCountryCode\": \"JO\",    \"source\": \"0\"  },  \"baseUrlShipping\": \"https://ws.sbx.aramex.net/shippingapi.v2/shipping/service_1_0.svc/xml/\",  \"baseUrlTracking\": \"https://ws.sbx.aramex.net/ShippingAPI.V2/Tracking/Service_1_0.svc/json/\",  \"baseUrlCalculateRate\": \"https://ws.sbx.aramex.net/ShippingAPI.V2/RateCalculator/Service_1_0.svc/json/\",  \"baseUrlLocation\": \"https://ws.dev.aramex.net/ShippingAPI/Location/Service_1_0.svc/json/\",  \"DomisticSettings\":{   \"ProductGroup\":\"DOM\",   \"ProductType\":\"OND\"},\"ExpressSettings\":{   \"ProductGroup\":\"EXP\",   \"ProductType\":\"PPX\"}}",
            //    IsEnabled = true,
            //    Name = "Aramex"
            //});
            //modelBuilder.Entity<ShippingProvider>().HasSeedData(new ShippingProvider("Pickuped")
            //{
            //    AdditionalSettings = "",
            //    IsEnabled = true,
            //    Name = "Pickuped",
            //});
            //modelBuilder.Entity<SiteSettings>().HasSeedData(new SiteSettings("DeliveryOption")
            //{

            //    IsVisibleInCommonSettingPage = true,
            //    Module = "Delivery",
            //    SiteId = 2,
            //    Value = "2"

            //});
           

        }
    }
}

