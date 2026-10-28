using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class AtrtributeGroupViewModel
    {
        public long? Id { get; set; }
        public EStatus Status { get; set; }
        public IList<AtrtributeGroupDetailsViewModel>  atrtributeGroupDetails { get; set; }
    }
    public class AtrtributeGroupDetailsViewModel
    {
        public long? Id { get; set; }

        public string Name { get; set; }
        public long CultureId { get; set; }
    }
}
