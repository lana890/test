
using EskaCMS.Entities.Entity;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.Integrations.EskaBilling.Models
{
    public class CustomerSimsVM
    {


        [Range(1, long.MaxValue, ErrorMessage = "Please enter valid  Number")]
        public long AccountId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Please enter valid  Number")]
        public long OrderId { get; set; }


        [Range(1, long.MaxValue, ErrorMessage = "ICCID Value must be greater than zero.")]

        public long? ICCID { get; set; }
        [Range(1, long.MaxValue, ErrorMessage = "MSISDN Value must be greater than zero.")]

        public long? MSISDN { get; set; }
        [Range(1, long.MaxValue, ErrorMessage = "IMSI Value must be greater than zero.")]

        public long? IMSI { get; set; }
        public string SIMName { get; set; }
        public string SIMTag { get; set; }
        public string APNType { get; set; }
        public string City { get; set; }



    }

    public class CustomerSimsUpdateViewModel
    {
        public long Id { get; set; }


        [Range(1, long.MaxValue, ErrorMessage = "ICCID Value must be greater than zero.")]

        public long? ICCID { get; set; }
        [Range(1, long.MaxValue, ErrorMessage = "MSISDN Value must be greater than zero.")]

        public long? MSISDN { get; set; }
        [Range(1, long.MaxValue, ErrorMessage = "IMSI Value must be greater than zero.")]

        public long? IMSI { get; set; }
        public string SIMName { get; set; }
        public string SIMTag { get; set; }
        public string AssignIP { get; set; }
        public string APNType { get; set; }
        public string City { get; set; }
        public string Note { get; set; }

    }

    public class ChangeSIMsInfoVM
    {
        public string ServiceProfile { get; set; }
        public List<CustomerSims> SIMs { get; set; }
    }

    public class ChangePlanVM
    {
        public long NewProductId { get; set; }
        public string Note { get; set; }
        public string ServiceProfile { get; set; }
        public List<CustomerSims> SIMs { get; set; }
    }



}
