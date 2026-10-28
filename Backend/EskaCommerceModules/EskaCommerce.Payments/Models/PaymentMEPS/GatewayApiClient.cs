

using EskaCommerce.Payments.Helper.PaymentMEPS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class GatewayApiClient : AbstractGatewayApiClient
    {
        private readonly GatewayApiConfig GatewayApiConfig;
        private readonly MepsConfigFactory _mepsConfigFactory;

        public GatewayApiClient(MepsConfigFactory mepsConfigfactory, ILogger<GatewayApiClient> logger)
        {
            _mepsConfigFactory = mepsConfigfactory;
            GatewayApiConfig = _mepsConfigFactory.GetMepsConfig();
            this.Logger = logger;
        }
        /// <summary>
        /// Sends the transaction.
        /// </summary>
        /// <returns>The transaction.</returns>
        /// <param name="gatewayApiRequest">Gateway API request.</param>
        public override string SendTransaction(GatewayApiRequest gatewayApiRequest)
        {
            return this.executeHTTPMethod(gatewayApiRequest);
        }

    }
}
