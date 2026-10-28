using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Integrations.Models.Carseer
{

    public class CarseerProduct
    {
        public string Serial { get; set; } = string.Empty;
    }

    public class CarseerQueryResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<CarseerProduct> Products { get; set; } = [];
    }

}
