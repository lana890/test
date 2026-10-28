using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Sites.Models
{
    public class SiteSearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }

    }
    public class SiteListSearchVM
    {
        public string Name { get; set; }
        public EStatus? Status { get; set; }
        public DateTimeOffset? CreationDateFrom { get; set; }
        public DateTimeOffset? CreationDateTo { get; set; }
    }
    public class SiteGridOutputVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }
}
