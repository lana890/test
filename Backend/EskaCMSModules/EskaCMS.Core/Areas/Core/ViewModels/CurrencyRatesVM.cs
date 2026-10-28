using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Core.Areas.Core.ViewModels
{

    public class CurrencyRatesVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string CultreCode { get; set; }
        public string CurrencyCulture { get; set; }
        public decimal Rate { get; set; }
        public bool IsDefaultRate { get; set; }
     }

    public class CurrencyRatesSearchListVM
    {
        public string Text { get; set; }        
        public string currencyCulture { get; set; }
 
    }

    public class SmartSearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }

    }
    public class SmartGridOutputVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }
}
