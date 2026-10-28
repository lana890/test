using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class MakeOrder
    {
        private string _CustomerName;
        public string CustomerName
        {
            set { _CustomerName = value; }
            get { return _CustomerName; }
        }

        private string _CustomerPhone;
        public string CustomerPhone
        {
            set { _CustomerPhone = value; }
            get { return _CustomerPhone; }
        }

        public string Note { get; set; }

        public String date { get; set; }
        public String time { get; set; }
        public string branch { get; set; }
        public string address { get; set; }
        public List<Products> products { get; set; }
    }

}
