using EskaCMS.Entities.Entity.EcomLog;
using EskaCMS.Entities.Enum;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

namespace EskaCommerce.ShoppingCart.EcommerceLog.ModelView
{


    public class EcommerceLogWithVersionsVM
    {
        public long Id { get; set; }
        public long? CartId { get; set; }
        public long? OrderId { get; set; }
        public long SiteId { get; set; }
        public DateTime CreatedDate { get; set; }
        public TransactionStatus Status { get; set; }
        public string UserName { get; set; }
        public long? CurrentVersionId { get; set; }

        // All versions
        public List<EcommerceLogVersionsVM> Versions { get; set; }
    }

    public class EcommerceLogVersionsVM
    {
        public long Id { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string Header { get; set; }
        public string Method { get; set; }
        public string URL { get; set; }
        public int? StatusCode { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsCurrent { get; set; }
    }


    public class AddVersionRequest
    {
        [JsonPropertyName("ecomLogId")]
        public long EcomLogId { get; set; }
        [JsonPropertyName("request")]
        public string Request { get; set; }
        [JsonPropertyName("header")]
        public string Header { get; set; }
        [JsonPropertyName("method")]
        public string Method { get; set; }
        [JsonPropertyName("URL")]
        public string Url { get; set; }
        [JsonPropertyName("response")]
        public string Response { get; set; }
        [JsonPropertyName("statusCode")]
        public int? StatusCode { get; set; }

    }
    public class EditVersionRequest
    {
        public long Id { set; get; }
        public long EcomLogId { get; set; }
        [Required]
        public string Request { get; set; }
        public string Header { get; set; }
        [Required]
        public string Method { get; set; }
        [Required]
        [Url]
        public string Url { get; set; }
        public string Response { get; set; }

        public int StatusCode { get; set; }

    }
    public class SearchLogParam
    {
        //public long? userId { set; get; }
        //public string UserName { set; get; }
        public TransactionStatus status { set; get; }
        public string SearchValue { set; get; }

        //public long? OrderId { set; get; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; } 
    }

    public class RequestPayload
    {

        public string Url { get; set; }
        public string Method { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }


        public bool IsValid => !string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(Method);
    }
    public class TestCartRequest
    {
        public long ProductId { get; set; }
        public long Quantity { get; set; }
        public long CartType { get; set; }
    }

    public class RetryConfig
    {
        public int MaxRetry { get; set; }
        public int RetryIntervalMin { get; set; }
        public int RetryDelaySec { get; set; }
    }
}
