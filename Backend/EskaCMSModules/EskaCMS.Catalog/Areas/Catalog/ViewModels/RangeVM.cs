using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class Range<T>
    {
        public Range(T From, T To)
        {
            this.From = From;
            this.To = To; 
        }
        public T From { get; set; }
        public T To { get; set; }
    }


}
