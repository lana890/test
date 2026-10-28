using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class CustomerVM
    {
        internal long Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }

        public long  ProductId { get; set; }         
        internal string FullName { get { return this.FirstName.Trim() + " " + this.LastName.Trim(); } set { } }

        public string UserName { get; set; }
        public int Age { get; set; }

        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }


        public string bloodType { get; set; }
        public string personofContact { get; set; }
        public bool allergy { get; set; }
        public string relationship { get; set; }
        public string emergencyNumber { get; set; }
        public string nationality { get; set; }
        public string residenceCountry { get; set; }
        public string team { get; set; }
        public string tShirtSize { get; set; }
        public string address { get; set; }
        public string occupation { get; set; }
        public string raceCategory { get; set; }
        public string gender { get; set; }

        public string CouponeCode { get; set; }

    }
}
