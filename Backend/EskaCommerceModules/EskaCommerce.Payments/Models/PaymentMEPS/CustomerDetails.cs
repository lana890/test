using System;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class CustomerDetails
    {
       
        public string Name { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }

        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Street1 { get; set; }
        public string Zip { get; set; }

    }
}
