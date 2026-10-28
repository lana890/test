using System;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Inventory.Areas.Inventory.ViewModels
{
    public class WarehouseVm
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        public bool IsMain { get; set; }

        public long AddressId { get; set; }

        public string ContactName { get; set; } 

        public string Phone { get; set; }
        public bool IsVisible { get; set; }

        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        public string City { get; set; }

        public string ZipCode { get; set; }

        public long? DistrictId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "State or Province is required")]
        public long? StateOrProvinceId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public string CountryId { get; set; }
        public string VendorName { get; set; }
        public long? VendorId { get; set; }
    }
    public class WarehouseWorkingHoursViewModel
    {
        public long Id { get; set; }
        public long WarehouseId { get; set; }
        public WarehouseWorkingDay DayOfWeek { get; set; }  // 1 = Sunday ... 7 = Saturday
        public string DayName { get; set; } // optional helper
        public TimeSpan? OpenTime { get; set; } // "08:00"
        public TimeSpan? CloseTime { get; set; } // "17:00"
        public bool IsClosed { get; set; }
    }
    public class CreateWarehouseWorkingHoursRequest
    {
        public long WarehouseId { get; set; }
        public WarehouseWorkingDay DayOfWeek { get; set; }
        public TimeSpan? OpenTime { get; set; } // optional if closed
        public TimeSpan? CloseTime { get; set; }
        public bool IsClosed { get; set; }
    }
    public class UpdateWarehouseWorkingHoursRequest
    {
        public long Id { get; set; }
        public long WarehouseId { get; set; }
        public WarehouseWorkingDay DayOfWeek { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public bool IsClosed { get; set; }
    }
    public class ValidateWarehouseWorkingHours
    {
        public string OpenTime { get; set; }
        public string CloseTime { get; set; }
        public bool IsClosed { get; set; }
    }
}
