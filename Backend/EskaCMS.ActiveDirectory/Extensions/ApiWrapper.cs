using System;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;


namespace EskaDCMSActiveDirectory.Extensions
{
    public class ApiWrapper
    {
        /// <summary>
        /// API version class member
        /// </summary>
        private String ApiVersion;

        /// <summary>
        /// API name class member
        /// 
        /// </summary>
        private String ApiName;

        /// <summary>
        /// Request processing task member
        /// </summary>
        private readonly RequestDelegate Next;

        /// <summary>
        /// Initializes a new JsonApiWrapper Middle-ware instance.
        /// </summary>
        /// <param name="version">API version to be shown in all responses</param>
        /// <param name="name">API name to be shown in all responses</param>
        /// <param name="requestDelegate">A task that represents the completion of request processing</param>
        public ApiWrapper(string version, string name, RequestDelegate requestDelegate)
        {
            this.ApiVersion = version;
            this.ApiName = name;
            this.Next = requestDelegate;
        }

        /// <summary>
        /// Response post-processing handler.
        /// </summary>
        /// <param name="context">HttpContext object</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            Stream responseBody = context.Response.Body;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                context.Response.Body = memoryStream;

                await this.Next(context);
                memoryStream.Position = 0;
                memoryStream.Seek(0, SeekOrigin.Begin); //seek begin


                //string responseString = new StreamReader(memoryStream).ReadToEnd();Encoding.UTF8
                string jsonString = Encoding.UTF8.GetString(memoryStream.ToArray());
                string wrappedResponse = this.Wrap(jsonString, context);
                byte[] responseBytes = Encoding.UTF8.GetBytes(wrappedResponse);

                context.Response.Headers["Content-type"] = "application/vnd.api+json";
                context.Response.Headers.Remove("Content-Length");
                context.Response.Body = responseBody;
                await context.Response.Body.WriteAsync(responseBytes, 0, responseBytes.Length);

            }


        }



        /// <summary>
        /// Wraps body content into json:api specification.
        /// </summary>
        /// <param name="originalBody">Original body content</param>
        /// <param name="context">HttpContext object</param>
        /// <returns>Wrapped JSON string</returns>
        private dynamic Wrap(string originalBody, HttpContext context)
        {
            dynamic response;
            if (originalBody.StartsWith("{") && originalBody.EndsWith("}"))
            {
                response = JObject.Parse(originalBody);
            }
            else if (originalBody.StartsWith("[") && originalBody.EndsWith("]"))
            {
                response = JArray.Parse(originalBody);
            }
            else
            {
                response = originalBody;
            }

            object wrapper;
            if (this.IsSuccessResponse(context.Response.StatusCode))
                wrapper = this.DataWrap(response);
            else
                wrapper = this.ErrorWrap(response.ToString(), context);

            string newBody = JsonConvert.SerializeObject(wrapper, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver()
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                    {
                        ProcessDictionaryKeys = true
                    }
                }
            });

            return newBody;
        }

        /// <summary>
        /// Retrieves api:json's api root object
        /// </summary>
        /// <returns>API object info</returns>
        private Object ApiInfo()
        {
            return new
            {
                version = this.ApiVersion,
                name = this.ApiName
            };
        }

        /// <summary>
        /// Rewrites original body content for success wrapping.
        /// </summary>
        /// <param name="response">Response body</param>
        /// <returns>Formatted object</returns>
        private Object DataWrap(dynamic response)
        {
            return new
            {
                code = "",
                message = "",
                content = response
            };
        }

        /// <summary>
        /// Rewrites original body content for error wrapping.
        /// </summary>
        /// <param name="response">Response body</param>
        /// <returns>Formatted object</returns>
        private Object ErrorWrap(string response, HttpContext context)
        {
            string reason = String.IsNullOrWhiteSpace(response) ? this.StatusCodeMessage(context.Response.StatusCode) : response;
            string CustomCode = string.Empty;
            string CustomMessage = string.Empty;
            try
            {
                var GetError = response.Split('$');
                if (GetError.Length > 1)
                {
                    CustomCode = GetError[0].ToString();
                    CustomMessage = GetError[1].ToString();
                }
            }
            catch (Exception)
            {

            }

            return new
            {
                code = string.IsNullOrEmpty(CustomCode) ? context.Response.StatusCode.ToString() : CustomCode,
                message = string.IsNullOrEmpty(CustomMessage) ? reason : CustomMessage,

            };
        }

        /// <summary>
        /// Determine whether a response is a success or not by its status code.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        private Boolean IsSuccessResponse(int statusCode)
        {
            return (statusCode >= 200 && statusCode < 299);
        }

        private String StatusCodeMessage(int statusCode)
        {
            switch (statusCode)
            {
                case 400:
                    return "Bad request.";
                case 401:
                    return "Unauthorized access.";
                case 402:
                    return "Payment required.";
                case 403:
                    return "Forbidden access.";
                case 404:
                    return "Resource not found.";
                case 405:
                    return "Method not allowed.";
                case 406:
                    return "Not acceptable.";
                case 407:
                    return "Proxy authentication required.";
                case 408:
                    return "Request timeout.";
                case 409:
                    return "Conflict";
                case 410:
                    return "Resource is gone.";
                case 411:
                    return "Length is required.";
                case 500:
                    return "Internal server error.";
                case 501:
                    return "Not implemented.";
                case 502:
                    return "Bad gateway.";
                case 503:
                    return "Service unavailable.";
                case 504:
                    return "Gateway timeout.";
                case 505:
                    return "HTTP version not supported.";
            }
            return "";
        }
    }
}
