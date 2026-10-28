 using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Payments;
using EskaCMS.Entities.Entity.Core;
using EskaCommerce.Payments.Helper;

namespace EskaCommerce.Payments.Data
{
    public class PaymentsCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentProvider>().ToTable("Payments_PaymentProvider");

            modelBuilder.Entity<PaymentProvider>().HasSeedData(
                     new PaymentProvider(PaymentProviderHelper.MepsProviderId)
                     { Name = "Meps Payment", AdditionalSettings = "{\"ApiPassword\":\"272457f43a8ccc99ca5b0daf816a57f1\",\r  \"ENVIRONMENT\":\"UAT-\" ,\r  \"BaseUrl\":\"https://mepspay.gateway.mastercard.com\", \r  \"Currency\":\"USD\", \r  \"MerchantId\":\"TEST9920029800\",\r   \"Version\":\"60\",\r   \"ServerKey\":\"STJ99RGBWN-JHN22H2KW6-DKDRNWLHMJ\",\r\"ReturnUrl\":\"http://go.com.sa\"}", IsEnabled = true, SiteId = 1L });  modelBuilder.Entity<PaymentProvider>().HasSeedData(
                     new PaymentProvider(PaymentProviderHelper.MepsPayTabsProviderId)
                     { Name = "PayTabs", AdditionalSettings = "{\"PayTapsURL\":\"https://secure-jordan.paytabs.com/payment/request/\",\"Environment\":\"UAT-\" ,\"CallBackURL\":\"https://7f69-82-212-75-147.ngrok-free.app/api/PayTaps/VerifyResponseCallBack/\", \"ReturnURL\":\"http://go.com.sa/en/business\", \"ProfileId\":\"138543\", \"ServerKey\":\"SNJ9NJ99RN-JHNMW6TJ2J-JRNDKGLKKN\",\"CartDesc\":\"Eska Commerce\" }", IsEnabled = true, SiteId = 1L });

            modelBuilder.Entity<PaymentProvider>().HasSeedData(
                   new PaymentProvider(PaymentProviderHelper.NetworkProviderId)
                   { Name = "Network Payment", AdditionalSettings = "{\"PASSWORD\":\"e65bbe9da80cbe6e6545bf7fd02c5f90\",\"BASE_URL\":\"https://test-network.mtf.gateway.mastercard.com/api/rest/version/\", \"CURRENCY\":\"USD\",\"CURRENCY_CULTUER\":\"en-US\",\"MASTER_API_PASSWORD\":\"ac63181fe688fe7ce3cf5a1f105a145a\", \"MERCHANT_ID\":\"TESTNITEST2\",\"VERSION\":\"74\",\"MERCHANT_NAME\":\"Aghati Sweets\",\"RETURN_URL\":\"https://769f-82-212-75-147.ngrok-free.app/api/PaymentMEPS/EmpededHostedReceipt\"}", IsEnabled = true, SiteId = 1L });
        }
    }
}

