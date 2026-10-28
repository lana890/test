using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Payments;

namespace EskaCommerce.Payments.Data.PaymentHyperPay
{
    public class PaymentMEPSCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<PaymentProvider>().HasSeedData(
            //         new PaymentProvider("Meps") { Name = "Meps", AdditionalSettings = "JsonSettings", IsEnabled = false, SiteId = 1 }
           // );
        }
    }
}

