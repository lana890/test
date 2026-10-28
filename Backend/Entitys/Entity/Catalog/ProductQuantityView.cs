using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Tax;
using EskaCMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.Catalog
{
    [Keyless]

    public class ProductQuantityView 
    {
        public long Id { get; set; }
       
        public int StockQuentity { get;  set; }
        
    }
}
