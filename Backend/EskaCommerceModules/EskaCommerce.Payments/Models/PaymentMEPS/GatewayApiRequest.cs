using EskaCommerce.Payments.Helper.PaymentMEPS;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class GatewayApiRequest
    {
        private GatewayApiConfig gatewayApiConfig;

        public string OrderId { get; set; }
        public long TransactionId { get; set; }

        public string ApiOperation { get; set; }
        public string ApiMethod { get; set; } = "PUT";
        public string RequestUrl { get; set; }
        public string Payload { get; set; }

        public string SessionId { get; set; }
        public String SecureId { get; set; }
        public String SecureIdResponseUrl { get; set; }

        public String SourceType { get; set; }
        public String CardNumber { get; set; }
        public String ExpiryMonth { get; set; }
        public String ExpiryYear { get; set; }
        public String SecurityCode { get; set; }

        public decimal OrderAmount { get; set; }
        public String OrderCurrency { get; set; }
        public String OrderDescription { get; set; }

        public decimal TransactionAmount { get; set; }
        public String TransactionCurrency { get; set; }
        public String TargetTransactionId { get; set; }
        public String ReturnUrl { get; set; }

        public String PaymentAuthResponse { get; set; }
        public String SecureId3D { get; set; }
        public string InteractionOperation { get; set; }

        public String BrowserPaymentOperation { get; set; }
        public String BrowserPaymentPaymentConfirmation { get; set; }


        public Dictionary<String, String> NVPParameters { get; set; }

        public String ContentType { get; set; } = "application/json; charset=iso-8859-1";


        public String MasterpassOnline { get; set; }
        public String MasterpassOriginUrl { get; set; }

        public String MasterpassOauthToken { get; set; }
        public String MasterpassOauthVerifier { get; set; }
        public String MasterpassCheckoutUrl { get; set; }


        public String Token { get; set; }

        private String apiBaseUrl { get; set; }

        public GatewayApiRequest()
        {
        }

        public GatewayApiRequest(GatewayApiConfig gatewayApiConfig)
        {
            GatewayApiConfig = gatewayApiConfig;
        }

        public GatewayApiConfig GatewayApiConfig
        {
            get => gatewayApiConfig;
            set => gatewayApiConfig = value;
        }

        public string buildSessionRequestUrl()
        {
            return buildSessionRequestUrl(SessionId);
        }

        public string buildTokenUrl()
        {
            RequestUrl = $@"{getApiGatewayBaseURL()}/api/rest/version/{GatewayApiConfig.Version}/merchant/{GatewayApiConfig.MerchantId}/token";
            return RequestUrl;
        }

        public string buildSessionRequestUrl(String sessionId)
        {
            string url = $@"{getApiGatewayBaseURL()}/api/rest/version/{GatewayApiConfig.Version}/merchant/{GatewayApiConfig.MerchantId}/session";
            if (!String.IsNullOrEmpty(sessionId))
            {
                url = $"{url}/{sessionId}";
            }
            RequestUrl = url;
            return RequestUrl;
        }

        public string buildSecureIdRequestUrl()
        {
            string url = $@"{getApiGatewayBaseURL()}/api/rest/version/{GatewayApiConfig.Version}/merchant/{GatewayApiConfig.MerchantId}/3DSecureId";
            if (!String.IsNullOrEmpty(SecureId))
            {
                url = $"{url}/{SecureId}";
            }
            RequestUrl = url;
            return RequestUrl;
        }


        public string buildOrderUrl()
        {
            RequestUrl = $@"{getApiGatewayBaseURL()}/api/rest/version/{GatewayApiConfig.Version}/merchant/{GatewayApiConfig.MerchantId}/order/{OrderId}";
            return RequestUrl;
        }

        public string buildRequestUrl()
        {
            RequestUrl = $@"{getApiGatewayBaseURL()}/api/rest/version/{GatewayApiConfig.Version}/merchant/{GatewayApiConfig.MerchantId}/order/{OrderId}/transaction/{TransactionId}";
            return RequestUrl;
        }



        public string buildRequestNPVUrl()
        {
            RequestUrl = $@"{getApiGatewayBaseURL()}/api/nvp/version/{GatewayApiConfig.Version}";
            return RequestUrl;
        }

        public static GatewayApiRequest createSampleApiRequest(GatewayApiConfig gatewayApiConfig, long OrderId, long TransactionId, decimal Amount, string Description, string apiOperation = null, string sessionId = null)
        {
            GatewayApiRequest gatewayApiRequest = new GatewayApiRequest(gatewayApiConfig)
            {
                SessionId = sessionId,
                OrderId = OrderId.ToString(),
                TransactionId = TransactionId,
                ApiOperation = apiOperation,
                OrderAmount = Amount,
                OrderCurrency = gatewayApiConfig.Currency,
                OrderDescription = Description//"Wonderful product that you should buy!"
            };

            gatewayApiRequest.buildRequestUrl();

            if (apiOperation == "CAPTURE" || apiOperation == "REFUND")
            {
                gatewayApiRequest.TransactionAmount = Amount;
                gatewayApiRequest.TransactionCurrency = gatewayApiConfig.Currency;
                gatewayApiRequest.OrderId = "0";
            }
            if (apiOperation == "VOID" || apiOperation == "UPDATE_AUTHORIZATION")
            {
                gatewayApiRequest.OrderId ="0";
            }
            if (apiOperation == "RETRIEVE_ORDER" || apiOperation == "RETRIEVE_TRANSACTION")
            {
                gatewayApiRequest.ApiMethod = "GET";
                gatewayApiRequest.OrderId = "0";
                gatewayApiRequest.TransactionId = 0;
            }

            gatewayApiRequest.buildPayload();

            return gatewayApiRequest;
        }

        /// <summary>
        /// return the correct API gateway base URL, depends on the authentication method.
        /// </summary>
        /// <returns>The API gateway base URL.</returns>
        public string getApiGatewayBaseURL()
        {
            if (apiBaseUrl == null)
            {
                if (GatewayApiConfig.AuthenticationByCertificate)
                {
                    apiBaseUrl = GatewayApiConfig.GatewayUrlCertificate;
                }
                else
                {
                    apiBaseUrl = GatewayApiConfig.GatewayUrl;
                }
            }
            return apiBaseUrl;
        }


        /// <summary>
        /// Builds the NVP Map
        /// </summary>
        /// <returns>The NVP Map.</returns>
        public Dictionary<String, String> buildNVPMap()
        {
            NVPParameters = new Dictionary<string, string>();

            NVPParameters.Add("apiOperation", "PAY");
            NVPParameters.Add("order.id", OrderId.ToString());
            NVPParameters.Add("order.amount", OrderAmount.ToString());
            NVPParameters.Add("order.currency", OrderCurrency);
            NVPParameters.Add("transaction.id", TransactionId.ToString());
            NVPParameters.Add("session.id", SessionId);
            NVPParameters.Add("sourceOfFunds.type", "CARD");
            NVPParameters.Add("merchant", gatewayApiConfig.MerchantId);

            return NVPParameters;
        }


        /// <summary>
        /// Builds the JSON payload.
        /// </summary>
        /// <returns>The JSON payload.</returns>
        public string buildPayload()
        {

            NameValueCollection nvc = new NameValueCollection();
            if (!String.IsNullOrEmpty(ApiOperation))
            {
                nvc.Add("apiOperation", ApiOperation);
            }

            if (!String.IsNullOrEmpty(InteractionOperation))
            {
                nvc.Add("interaction.operation", InteractionOperation);
            }

            if (!String.IsNullOrEmpty(SecureId3D))
            {
                nvc.Add("3DSecureId", SecureId3D);
            }

            if (!String.IsNullOrEmpty(SessionId))
            {
                nvc.Add("session.id", SessionId);
            }
            if (!String.IsNullOrEmpty(SourceType))
            {
                nvc.Add("sourceOfFunds.type", SourceType);
            }

            if (!String.IsNullOrEmpty(Token))
            {
                nvc.Add("sourceOfFunds.token", Token);
            }

            if (!String.IsNullOrEmpty(CardNumber))
            {
                nvc.Add("sourceOfFunds.provided.card.number", CardNumber);
                nvc.Add("sourceOfFunds.provided.card.expiry.month", ExpiryMonth);
                nvc.Add("sourceOfFunds.provided.card.expiry.year", ExpiryYear);
                nvc.Add("sourceOfFunds.provided.card.securityCode", SecurityCode);
            }

            if ("CREATE_CHECKOUT_SESSION" == ApiOperation || String.IsNullOrEmpty(ApiOperation) || "UPDATE_SESSION_FROM_WALLET" == ApiOperation)
            {
                // Need to add order ID in the request body for CREATE_CHECKOUT_SESSION or UPDATE SESSION. 
                // Its presence in the body will cause an error for the other operations.

                nvc.Add("order.id", OrderId.ToString());



                //masterpass
                if (String.IsNullOrEmpty(ApiOperation) || "CREATE_CHECKOUT_SESSION" != ApiOperation)
                {

                    if (!String.IsNullOrEmpty(MasterpassOnline))
                    {
                        nvc.Add("order.walletProvider", MasterpassOnline);
                    }
                    if (!String.IsNullOrEmpty(MasterpassOriginUrl))
                    {
                        nvc.Add("wallet.masterpass.originUrl", MasterpassOriginUrl);
                    }
                    if (!String.IsNullOrEmpty(MasterpassOauthToken))
                    {
                        nvc.Add("wallet.masterpass.oauthToken", MasterpassOauthToken);
                    }
                    if (!String.IsNullOrEmpty(MasterpassOauthVerifier))
                    {
                        nvc.Add("wallet.masterpass.oauthVerifier", MasterpassOauthVerifier);
                    }
                    if (!String.IsNullOrEmpty(MasterpassCheckoutUrl))
                    {
                        nvc.Add("wallet.masterpass.checkoutUrl", MasterpassCheckoutUrl);
                    }
                }


                if (!String.IsNullOrEmpty(ReturnUrl) && "CREATE_CHECKOUT_SESSION" == ApiOperation)
                {
                    nvc.Add("interaction.returnUrl", ReturnUrl);
                }

            }

            nvc.Add("order.amount", Math.Round(OrderAmount, 3).ToString());

            if (!String.IsNullOrEmpty(OrderCurrency))
            {
                nvc.Add("order.currency", OrderCurrency);
            }
            //if (!String.IsNullOrEmpty(OrderDescription))
            //{
            //    nvc.Add("order.description", OrderDescription);
            //}

            //nvc.Add("transaction.amount", ToString());

            if (!String.IsNullOrEmpty(TransactionCurrency))
            {
                nvc.Add("transaction.currency", TransactionCurrency);
            }
            if (!String.IsNullOrEmpty(TargetTransactionId))
            {
                nvc.Add("transaction.targetTransactionId", TargetTransactionId);
            }
            if (!String.IsNullOrEmpty(SecureIdResponseUrl))
            {
                nvc.Add("3DSecure.authenticationRedirect.responseUrl", SecureIdResponseUrl);
                nvc.Add("3DSecure.authenticationRedirect.pageGenerationMode", "CUSTOMIZED");
            }
            if (!String.IsNullOrEmpty(PaymentAuthResponse))
            {
                nvc.Add("3DSecure.paRes", PaymentAuthResponse);
            }

            //browser payment

            if (!String.IsNullOrEmpty(BrowserPaymentOperation))
            {
                nvc.Add("browserPayment.operation", BrowserPaymentOperation);
            }


            //paypal
            if (!String.IsNullOrEmpty(BrowserPaymentPaymentConfirmation) && "PAYPAL" == SourceType)
            {
                nvc.Add("browserPayment.paypal.paymentConfirmation", BrowserPaymentPaymentConfirmation);
            }


            if ("INITIATE_BROWSER_PAYMENT" == ApiOperation)
            {
                nvc.Add("browserPayment.returnUrl", ReturnUrl);
            }


            //build json
            Payload = JsonHelper.BuildJsonFromNVC(nvc);

            return Payload;
        }
    }
}
