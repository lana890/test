using System.ComponentModel.DataAnnotations;
using EskaCMS.Entities.Entity.Address;
using EskaCMS.Infrastructure.Models;
using static EskaCMS.Entities.Enum.GeneralEnums;


namespace EskaCMS.Entities.Entity.Addresses
{
    public class StateOrProvince : EntityBase
    {
        public StateOrProvince()
        {

        }

        public StateOrProvince(long id)
        {
            Id = id;
        }

        [StringLength(450)]
        public string CountryId { get; set; }

        public Country Country { get; set; }

        [StringLength(450)]
        public string Code { get; set; }

        public IList<StateOrProvinceDetails> StateOrProvinceDetails { get; set; } = new List<StateOrProvinceDetails>();
        public EStatus Status { get; set; }

        [StringLength(450)]
        public string Type { get; set; }
    }
}
