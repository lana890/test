using EskaCMS.Entities.Entity.Address;
using EskaCMS.Infrastructure.Models;
using EskaCommerce.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Addresses
{
    public class District : EntityBase
    {
        public District() { }

        public District(long id)
        {
            Id = id;
        }

        public long StateOrProvinceId { get; set; }

        public StateOrProvince StateOrProvince { get; set; }

        public IList<DistrictDetails>  DistrictDetails { get; set; }=new List<DistrictDetails>();
        public EStatus Status { get; set; }

        [StringLength(450)]
        public string Type { get; set; }

        public string Location { get; set; }

    }
}
