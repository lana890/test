using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public class TransactionResponseModel
    {
        public string ApiResult { get; set; }
        public string GatewayCode { get; set; }
        public string OrderAmount { get; set; }
        public string OrderCurrency { get; set; }
        public string OrderId { get; set; }
        public string OrderDescription { get; set; }

        /// <summary>
        /// Parses JSON response from Hosted/Browser Checkout transaction into TransactionResponse object
        /// </summary>
        /// <param name="response">response from API
        /// <returns>TransactionResponseModel</returns>
        public static TransactionResponseModel toTransactionResponseModel(string response)
        {
            TransactionResponseModel model = new TransactionResponseModel();

            JObject jObject = JObject.Parse(response);
            var transactionList = jObject["transaction"];
            model.GatewayCode = transactionList[0]["response"]["gatewayCode"].ToObject<String>();
            model.ApiResult = transactionList[0]["result"].ToObject<String>();
            model.OrderAmount = transactionList[0]["order"]["amount"].ToObject<String>();
            model.OrderCurrency = transactionList[0]["order"]["currency"].ToObject<String>();
            model.OrderId = transactionList[0]["order"]["id"].ToObject<String>();

            return model;
        }


        public static TransactionResponseModel fromMasterpassResponseToTransactionResponseModel(string response)
        {
            TransactionResponseModel model = new TransactionResponseModel();

            JObject jObject = JObject.Parse(response);
            model.GatewayCode = jObject["response"]["gatewayCode"].ToObject<String>();
            model.ApiResult = jObject["result"].ToObject<String>();
            model.OrderAmount = jObject["order"]["amount"].ToObject<String>();
            model.OrderCurrency = jObject["order"]["currency"].ToObject<String>();
            model.OrderId = jObject["order"]["id"].ToObject<String>();

            return model;
        }


    }



}
