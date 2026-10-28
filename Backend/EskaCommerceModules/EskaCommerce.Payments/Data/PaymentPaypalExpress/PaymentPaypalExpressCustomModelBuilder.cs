#nullable enable
using Microsoft.EntityFrameworkCore;

using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Infrastructure.Data;

namespace EskaCommerce.Payments.Data.PaymentPaypalExpress
{
    public class PaymentPaypalExpressCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
         

            //modelBuilder.Entity<PaymentProvider>().HasSeedData(
            //    new PaymentProvider("PayPal") { Name = "Paypal Payment", IsEnabled = true, AdditionalSettings = "{ \"ClientId\":\"AcOnfZjw1hhfz66HoggG_Da2e92p9DSBP6cEQWSYNLQZSDIOttEY502SZGVztK3FKfQRLsAtf7C0rT_D\",\"ClientSecret\":\"EHZzvjMLDUqolDr_cq3dSzLXLfixD2kpouz6JcheOznHimiQbhfMrWPVkmlJ5diQOCS8CnIocej6RLBB\",\"IsSandbox\":\"true\",\"ApiReturnUrl\":\"https://localhost:5000/api/PaymentPaypal/CapturePayment\",\"ApiCancelUrl\":\"https://localhost:5000/api/PaymentPaypal/CancelPayment\"}", SiteId=1 }
            //);
        }
    }
}

