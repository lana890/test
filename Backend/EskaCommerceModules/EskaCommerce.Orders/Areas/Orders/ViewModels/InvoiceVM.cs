using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class InvoiceVM
    {
        public long InvoiceNo { get; set; }
        public string HtmlContent { get; set; }
    }

    public class QrCode
    {

        public long OrderId { get; set; }
        public string Url { get; set; }
    }
}

