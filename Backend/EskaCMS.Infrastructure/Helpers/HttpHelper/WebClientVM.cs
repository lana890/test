using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Helpers.HttpHelper
{
    public class APIResultVM<T> where T : class
    {
        private string _StatusCode { get; set; }
        private string _Message { get; set; }
        private T _Response { get; set; }

        public string Code { get { return this._StatusCode; } set { this._StatusCode = value; } }
        public string Status { get { return this._StatusCode; } set { this._StatusCode = value; } }

        public T Content { get { return this._Response; } set { this._Response = value; } }
        public T Data { get { return this._Response; } set { this._Response = value; } }

        public string Message { get; set; }
        public string ErrorMessage { get; set; }

        public bool IsError { get; set; }
        public string ErrorInnerMessage { get; set; }


    }

    public class GetRequestVM
    {

        public string Url { get; set; }

        public Dictionary<string, string> Headers { get; set; }
        public string BaseUrl { get; set; }
        public BasicAuth BasicAuthCredinatials { get; set; } = null;


    }

    public class DeleteRequestVM : GetRequestVM
    {
    }


    public class PostRequestVM
    {
        public PostRequestVM()
        {
            Headers = new Dictionary<string, string>();
        }
        public string Url { get; set; }
        public string BaseUrl { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public string SerializedContent { get; set; }
        public BasicAuth BasicAuthCredinatials { get; set; } = null;

    }

    public class RequestWithFormDataVM
    {
        public RequestWithFormDataVM()
        {
            Headers = new Dictionary<string, string>();
        }
        public string Url { get; set; }
        public string BaseUrl { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public MultipartFormDataContent Content { get; set; }
        public BasicAuth BasicAuthCredinatials { get; set; } = null;


        public void SetContent(string Key, string SerializedPayload)
        {
            this.Content = new MultipartFormDataContent()
                                {
                                    { new StringContent(SerializedPayload),
                                       Key }
                                };
        }
    }


    public class PutRequestVM : PostRequestVM
    {
    }




    public class GeneralRequestVM
    {
        public GeneralRequestVM()
        {
            Headers = new Dictionary<string, string>();
        }
        public string Url { get; set; }
        public string BaseUrl { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public string SerializedContent { get; set; } = String.Empty;
        public MultipartFormDataContent FormDataContent { get; set; } = null;
        public void SetContent(string Key, string SerializedPayload)
        {
            this.FormDataContent = new MultipartFormDataContent()
                                {
                                    { new StringContent(SerializedPayload),
                                       Key }
                                };
        }
        public EContentType ContentType { get; set; } = EContentType.JSON;

        public BasicAuth BasicAuthCredinatials { get; set; } = null;

        public bool IsBasicAuth { get { return this.BasicAuthCredinatials != null; } }
    }

    public class BasicAuth
    {
        public string  ClientId { get; set; }
        public string  ClientSecret { get; set; }
    }
    public enum EContentType
    {
        JSON = 1,
        FormData = 2
    }
}
