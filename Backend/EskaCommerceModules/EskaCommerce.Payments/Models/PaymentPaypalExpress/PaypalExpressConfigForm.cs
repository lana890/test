#nullable enable
namespace EskaCommerce.Payments.Models.PaymentPaypalExpress
{
    public class PaypalExpressConfigForm
    {
        public bool IsSandbox { get; set; }

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ApiReturnUrl { get; set; }
        public string ApiCancelUrl { get; set; }

        public decimal PaymentFee { get; set; }

        public string Environment
        {
            get
            {
                return IsSandbox ? "sandbox" : "production";
            }
        }

        public string EnvironmentUrlPart
        {
            get
            {
                return IsSandbox ? ".sandbox" : "";
            }
        }
    }
}
