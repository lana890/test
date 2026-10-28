 using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Payments;

namespace EskaCommerce.Payments.Data.PaymentCoD
{
    public class PaymentCoDCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<PaymentProvider>().HasSeedData(
            //     new PaymentProvider("CoD") { Name = "Cash On Delivery", SiteId = 1 }
            //);
        }
    }
}

