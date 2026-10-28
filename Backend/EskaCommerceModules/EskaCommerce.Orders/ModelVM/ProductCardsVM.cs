using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.ModelVM
{
    public class ProductCardsVM
    {
        public Dictionary<string,CardDetailsVM> cardDetailsVM { get; set; }
    }
    public class CardDetailsVM
    {
        public double? Value { get; set; }
        public double? Percentage { get; set; }
    }
}
