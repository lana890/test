using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace EskaCommerce.Shipments.Models.AramixModels.Responses.Locations
{


    public class CitiesViewModel
    {
        public object Transaction { get; set; }
        public List<object> Notifications { get; set; }
        public bool HasErrors { get; set; }
        public List<string> Cities { get; set; }
    }




}
