using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class SerialsExportVm
    {
        public string BatchNumber { get; set; }
        public string ControlNumber { get; set; }
        public string Status { get; set; }
        public string Expiry { get; set; }
        public string Balance { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public long? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public long? OrderId { get; set; }

    }

    // this VM is customized for run jordan
    public class SerialsExportWithOwnersVM
    {
        public string Bib { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Year { get; set; }
        public string Sex { get; set; }
        public string Nation { get; set; }
        public string Club { get; set; }

    }

}
