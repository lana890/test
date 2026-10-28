using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.General;
using EskaCMS.Entities.Entity.Shipping;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.DomesticShipment
{
    public class DomesticShipmentCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<SiteSettings>().HasSeedData(new SiteSettings("SimsCreation")
            //{
            //    IsVisibleInCommonSettingPage = true,
            //    Module = "SimsCreation",
            //    SiteId = 1,
            //    Value = "{\"password\":\"test@aghati.com\",\r\n  \"email\":\"12345678\" ,\r\n  \"BaseUrl\":\"http://system.simscreation.com/api/\", \r\n}"

            //});

            //modelBuilder.Entity<ShippingProvider>().HasSeedData(new ShippingProvider("TopDelivery")
            //{

            //    AdditionalSettings = "{ \r\n\"BaseUrl\":\"https://us-central1-doneproject-d998e.cloudfunctions.net/apiAghati?\",\r\n  \"Username\":\"aghati\",\r\n  \"Password\":\"agh0024\",\r\n  \"BaseUrlStatus\":\"https://us-central1-doneproject-d998e.cloudfunctions.net/getOrderStatus?\"\r\n }",
            //    IsEnabled = true,
            //    Name = "TopDelivery"
            //});

            //modelBuilder.Entity<SiteSettings>().HasSeedData(new SiteSettings("LookupTopDelivery")
            //{
            //    IsVisibleInCommonSettingPage = true,
            //    Module = "TopDelivery",
            //    SiteId = 1,
            //    Value = "1"
            //});


            //modelBuilder.Entity<Lookup>().HasSeedData(new Lookup { Id = 7L, Code = "WarehouseTopDelivery", SiteId = 1, CultureId = 3, Estatus = EStatus.Active, Name = "WarehouseTopDelivery", IsEditable = true });

            //modelBuilder.Entity<LookupValues>().HasSeedData(
            //new LookupValues {Id=10000L, Name = "5", Value = "-NrkpZe9hnh9ikydcRXV", lookupId =7 , ParentId = null },
            //new LookupValues { Id = 10001L, Name = "4", Value = "-Nrkp-tgKJDlKdRwiBYI", lookupId = 7, ParentId = null },
            //new LookupValues { Id = 10002L, Name = "2", Value = "-NrkpL_fyD49c2xgPvNi", lookupId = 7, ParentId = null },
            //new LookupValues { Id = 10003L, Name = "3", Value = "-NrktZG_IMl6xN0Nc9K_", lookupId = 7, ParentId = null });

        }
    }
}

