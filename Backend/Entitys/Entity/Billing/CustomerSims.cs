
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.Entities.Entity
{

    [Index(nameof(ICCID), Name = "ICCIDidx")]
    [Index(nameof(MSISDN), Name = "MSISDNidx")]
    public class CustomerSims : EntityBase
    {

        public long ProductId { get; set; }
        public Product Product { get; set; }


        public long CustomerId { get; set; }
        public virtual User Customer { get; set; }


        public long AccountId { get; set; }
        public long OrderId { get; set; }
        [Range(1, long.MaxValue, ErrorMessage = "ICCID Value must be greater than zero.")]
        public long? ICCID { get; set; }
        [Range(1, long.MaxValue, ErrorMessage = "MSISDN Value must be greater than zero.")]
        public long? MSISDN { get; set; }
        [Range(1, long.MaxValue, ErrorMessage = "IMSI Value must be greater than zero.")]
        public long? IMSI { get; set; }

        [Required]
        public string SIMName { get; set; }
        public string SIMTag { get; set; }
        public ESIMsIdentifiers IdentifierType { get; set; }
        public string AssignIP { get; set; }
        [Required]
        public string APNType { get; set; }
        public string City { get; set; }


        public EIOTSIMStatus Status { get; set; }

        public bool IsDeleted { get; set; }
        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset? FirstActivationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public long? ModifiedById { get; set; }
        public virtual User ModifiedBy { get; set; }
        public long SiteId { get; set; }
        public virtual Site Site { get; set; }
        public List<CustomerSimsHistory> CustomerSimsHistory { get; set; }

        public void Delete(long DeletedById)
        {
            this.IsDeleted = true;
            this.ICCID = null;
            this.IMSI = null;
            this.SIMName = this.SIMName + " - Deleted";
            this.ModificationDate = DateTimeOffset.Now;
            this.ModifiedById = DeletedById;

        }

    }

}
