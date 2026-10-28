using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class MepsViewModel
    {
        [JsonPropertyName("authentication")]
        public AuthenticationViewModel Authentication { get; set; }

        [JsonPropertyName("billing")]
        public BillingViewModel Billing { get; set; }

        [JsonPropertyName("customer")]
        public CustomerViewModel Customer { get; set; }

        [JsonPropertyName("device")]
        public DeviceViewModel Device { get; set; }

        [JsonPropertyName("merchant")]
        public MerchantViewModel Merchant { get; set; }

        [JsonPropertyName("order")]
        public MepsOrderViewModel Order { get; set; }

        [JsonPropertyName("response")]
        public ResponseViewModel Response { get; set; }

        [JsonPropertyName("result")]
        public string Result { get; set; }

        [JsonPropertyName("sourceOfFunds")]
        public SourceOfFundsViewModel SourceOfFunds { get; set; }

        [JsonPropertyName("timeOfLastUpdate")]
        public DateTime TimeOfLastUpdate { get; set; }

        [JsonPropertyName("timeOfRecord")]
        public DateTime TimeOfRecord { get; set; }

        [JsonPropertyName("transaction")]
        public TransactionViewModel Transaction { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
    public class AuthenticationViewModel
    {
        [JsonPropertyName("3ds")]
        public ThreeDSAuthentication ThreeDS { get; set; }

        [JsonPropertyName("3ds2")]
        public ThreeDS2Authentication ThreeDS2 { get; set; }

        [JsonPropertyName("acceptVersions")]
        public string AcceptVersions { get; set; }

        [JsonPropertyName("channel")]
        public string Channel { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("payerInteraction")]
        public string PayerInteraction { get; set; }

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; }

        [JsonPropertyName("redirect")]
        public RedirectViewModel Redirect { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    public class BillingViewModel
    {
        [JsonPropertyName("address")]
        public AddressViewModel Address { get; set; }
    }

    public class CustomerViewModel
    {
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }
    }

    public class DeviceViewModel
    {
        [JsonPropertyName("browser")]
        public string Browser { get; set; }

        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; }
    }

    public class MerchantViewModel
    {
        [JsonPropertyName("merchant")]
        public string Name { get; set; }
    }

    public class MepsOrderViewModel
    {
        [JsonPropertyName("amount")]
        public double Amount { get; set; }

        [JsonPropertyName("authenticationStatus")]
        public string AuthenticationStatus { get; set; }

        [JsonPropertyName("chargeback")]
        public ChargebackViewModel Chargeback { get; set; }

        [JsonPropertyName("creationTime")]
        public DateTime CreationTime { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("lastUpdatedTime")]
        public DateTime LastUpdatedTime { get; set; }

        [JsonPropertyName("merchantAmount")]
        public double MerchantAmount { get; set; }

        [JsonPropertyName("merchantCategoryCode")]
        public string MerchantCategoryCode { get; set; }

        [JsonPropertyName("merchantCurrency")]
        public string MerchantCurrency { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("totalAuthorizedAmount")]
        public double TotalAuthorizedAmount { get; set; }

        [JsonPropertyName("totalCapturedAmount")]
        public double TotalCapturedAmount { get; set; }

        [JsonPropertyName("totalRefundedAmount")]
        public double TotalRefundedAmount { get; set; }

        [JsonPropertyName("valueTransfer")]
        public ValueTransferViewModel ValueTransfer { get; set; }

        public long GetOrderId()
        {
            return Convert.ToInt64(Id.Split('-')[1]);
        }
    }

    public class RedirectViewModel
    {
        [JsonPropertyName("domainName")]
        public string DomainName { get; set; }
    }

    public class ChargebackViewModel
    {
        [JsonPropertyName("amount")]
        public double Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }

    public class ResponseViewModel
    {
        [JsonPropertyName("gatewayCode")]
        public string GatewayCode { get; set; }

        [JsonPropertyName("gatewayRecommendation")]
        public string GatewayRecommendation { get; set; }
    }

    public class SourceOfFundsViewModel
    {
        [JsonPropertyName("provided")]
        public ProvidedViewModel Provided { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class ProvidedViewModel
    {
        [JsonPropertyName("card")]
        public CardViewModel Card { get; set; }
    }

    public class CardViewModel
    {
        [JsonPropertyName("brand")]
        public string Brand { get; set; }

        [JsonPropertyName("expiry")]
        public ExpiryViewModel Expiry { get; set; }

        [JsonPropertyName("fundingMethod")]
        public string FundingMethod { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("scheme")]
        public string Scheme { get; set; }
    }

    public class ExpiryViewModel
    {
        [JsonPropertyName("month")]
        public string Month { get; set; }

        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class ValueTransferViewModel
    {
        [JsonPropertyName("accountType")]
        public string AccountType { get; set; }
    }

    public class AddressViewModel
    {
        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("postcodeZip")]
        public string PostcodeZip { get; set; }

        [JsonPropertyName("stateProvince")]
        public string StateProvince { get; set; }

        [JsonPropertyName("street")]
        public string Street { get; set; }
    }

    public class ThreeDSAuthentication
    {
        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; }
    }

    public class ThreeDS2Authentication
    {
        [JsonPropertyName("acsTransactionId")]
        public string AcsTransactionId { get; set; }

        [JsonPropertyName("directoryServerId")]
        public string DirectoryServerId { get; set; }

        [JsonPropertyName("dsTransactionId")]
        public string DsTransactionId { get; set; }

        [JsonPropertyName("methodCompleted")]
        public bool MethodCompleted { get; set; }

        [JsonPropertyName("methodSupported")]
        public string MethodSupported { get; set; }

        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonPropertyName("requestorId")]
        public string RequestorId { get; set; }

        [JsonPropertyName("requestorName")]
        public string RequestorName { get; set; }

        [JsonPropertyName("transactionStatus")]
        public string TransactionStatus { get; set; }
    }

   
    public class TransactionViewModel
    {
        [JsonPropertyName("acquirer")]
        public AcquirerViewModel Acquirer { get; set; }

        [JsonPropertyName("amount")]
        public double Amount { get; set; }

        [JsonPropertyName("authenticationStatus")]
        public string AuthenticationStatus { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("stan")]
        public string Stan { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
    public class AcquirerViewModel
    {
        [JsonPropertyName("merchantId")]
        public string MerchantId { get; set; }
    }

    public class CommenProperties
    {

        public long OrderId { get; set; } 
        public long CultureId { get; set; } 
        public string Status { get; set; } 
        public string SessionId { get; set; } 
    }
}
