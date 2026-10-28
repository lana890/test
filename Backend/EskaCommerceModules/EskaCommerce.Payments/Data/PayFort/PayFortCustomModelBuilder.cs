#nullable enable
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Data.PayFort
{
    public class PayFortCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {

            //modelBuilder.Entity<PaymentProvider>().HasSeedData(new PaymentProvider("PayFort")
            //{
            //    SiteId = 1,
            //    IsEnabled = true,
            //    Name = "PayFort",
            //    AdditionalSettings =""
            //});
        }
    }
}

