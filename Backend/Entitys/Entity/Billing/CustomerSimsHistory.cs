
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using static EskaCMS.EskaBilling.Enums.IOTEnums;


namespace EskaCMS.Entities.Entity
{
    public class CustomerSimsHistory : EntityBase
    {
        public CustomerSimsHistory()
        {

        }

        public CustomerSimsHistory(CustomerSims SIM, EIOTSIMStatus? OldStatus, string Note, long CreatedById)
        {
            this.CustomerSimsId = SIM.Id;
            this.IdentifierType = SIM.IdentifierType;
            this.ProductId = SIM.ProductId;
            this.CustomerId = SIM.CustomerId;
            this.AccountId = SIM.AccountId;
            this.ICCID = SIM.ICCID;
            this.MSISDN = SIM.MSISDN;
            this.IMSI = SIM.IMSI;
            this.SIMName = SIM.SIMName;
            this.SIMTag = SIM.SIMTag;
            this.AssignIP = SIM.AssignIP;
            this.APNType = SIM.APNType;
            this.City = SIM.City;
            this.OldStatus = OldStatus;
            this.NewStatus = SIM.Status;
            this.Note = Note;
            this.CreatedById = CreatedById;
            this.CreationDate = DateTimeOffset.Now;
        }

        public long ProductId { get; set; }
        public Product Product { get; set; }


        public long CustomerId { get; set; }
        public virtual User Customer { get; set; }


        public long AccountId { get; set; }
        public long? ICCID { get; set; }
        public long? MSISDN { get; set; }
        public long? IMSI { get; set; }
        public string SIMName { get; set; }
        public string SIMTag { get; set; }
        public ESIMsIdentifiers IdentifierType { get; set; }
        public string AssignIP { get; set; }
        public string APNType { get; set; }
        public string City { get; set; }


        public long CustomerSimsId { get; set; }
        public CustomerSims CustomerSims { get; set; }



        public EIOTSIMStatus? OldStatus { get; set; }
        public EIOTSIMStatus NewStatus { get; set; }
        public string Note { get; set; }

        public long CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
