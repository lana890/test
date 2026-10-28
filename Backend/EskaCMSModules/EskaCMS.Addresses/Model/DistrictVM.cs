using EskaCMS.Infrastructure.Web.SmartTable;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EskaCMS.Addresses.ViewModels
{
    public class DistrictVM
    {
        public long Id { get; set; }

        public long StateOrProvinceId { get; set; }

        public string Location { get; set; }
        public string StateOrProvinceName { get; set; }
        
        public string Name { get; set; }

        public string Type { get; set; }
    }

    public class DistrictSearchListVM
    {
        public string Name { get; set; }
        public string Location { get; set; }

    }

    public class DistrictSmartSearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }

    }
    public class DistrictSmartGridOutputVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }

    public class DistrictRequestResponseVM
    {
        public long Id { get; set; }

        public long StateOrProvinceId { get; set; }

        public string Location { get; set; }
        public string StateOrProvinceName { get; set; }

        public IList<DistrictDetailsVM> DistrictDetails { get; set; }=new List<DistrictDetailsVM>();

        public string Type { get; set; }
    }
    public class DistrictDetailsVM
    {
      
        public string Name { get; set; }
        public long CultureId { get; set; }

    }
}
