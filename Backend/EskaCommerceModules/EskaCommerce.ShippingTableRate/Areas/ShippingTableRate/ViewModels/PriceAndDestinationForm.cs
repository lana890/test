using EskaCMS.Entities.Entity.Inventory;
using System;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShippingTableRate.Areas.ShippingTableRate.ViewModels
{
    public class PriceAndDestinationForm
    {
        public long Id { get; set; }

        public string CountryId { get; set; }

        public string CountryName { get; set; }

        public long? StateOrProvinceId { get; set; }

        public string StateOrProvinceName { get; set; }

        public long? DistrictId { get; set; }

        public string DistrictName { get; set; }

        public string ZipCode { get; set; }

        public string Note { get; set; }

        public decimal MinOrderSubtotal { get; set; }

        public decimal ShippingPrice { get; set; }
        public int? NoOfItems { get; set; }
        public int? EstimatedDeliveryDate { get; set; }

        public long WareHouseId { get; set; }
        public WareHouseDTO WareHouse { get; set; }
        public EStatus Status { get; set; }
    }

    public class WareHouseDTO {

        public long Id { get; set; }
        public string Name { get; set; }

        public string CountryId { get; set; }

        public string CountryName { get; set; }

        public long? StateOrProvinceId { get; set; }

        public string StateOrProvinceName { get; set; }

        public long? DistrictId { get; set; }

        public string DistrictName { get; set; }

        public string ZipCode { get; set; }



    }
    public class RequestRateAndDistination
    {
        public long? Id { get; set; }

        public string CountryId { get; set; }


        public long? StateOrProvinceId { get; set; }

        public long? DistrictId { get; set; }

        public string ZipCode { get; set; }

        public string Note { get; set; }

        public decimal MinOrderSubtotal { get; set; }

        public decimal ShippingPrice { get; set; }
        public int? NoOfItems { get; set; }
        public long WarehouseId { get; set; }
        public int? EstimatedDeliveryDate { get; set; }


    }
    public class SearchPriceAndDistinationViewModel
    {


        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public string SearchText { get; set; }
    }
}
