using EskaCMS.Core.BusinessModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.ApiMapperAndLogs;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static EskaCMS.Core.Services.WebServiceClient;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public class WebServiceClient
    {
        #region Delegates
        public delegate string DelegateInvokeService();
        #endregion

        #region Enumerators
        public enum ServiceType
        {
            Traditional = 0,
            WCF = 1
        }
        #endregion

        #region Classes
        public class Parameter
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
        #endregion

        #region Member Variables
        string _soapEnvelope =
                @"<soap:Envelope
                    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                    xmlns:xsd='http://www.w3.org/2001/XMLSchema'
                    xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
                <soap:Body></soap:Body></soap:Envelope>";


        #endregion

        #region Properties
        public string Url { get; set; }

        public string WebMethod { get; set; }

        public IDictionary<string, object> Parameters { get; set; }

        public ServiceType WSServiceType { get; set; }

        public string WCFContractName { get; set; }
        public string Accept { get; set; }
        public string ContentType { get; set; }

        public ApihttpMethods HttpMethod { get; set; }
        #endregion

        #region Private Methods
        private string CreateSoapEnvelope()
        {
            string MethodCall = "<" + this.WebMethod + @" xmlns=""http://tempuri.org/"">";
            string StrParameters = string.Empty;


            //foreach (KeyValuePair<string, object> kvp in this.Parameters)
            //    Console.WriteLine("Key: {0}, Value: {1}", kvp.Key, kvp.Value);
            foreach (KeyValuePair<string, object> param in this.Parameters)
            {
                StrParameters = StrParameters + "<" + param.Key + ">" + param.Value + "</" + param.Key + ">";
            }

            MethodCall = MethodCall + StrParameters + "</" + this.WebMethod + ">";

            StringBuilder sb = new StringBuilder(_soapEnvelope);
            sb.Insert(sb.ToString().IndexOf("</soap:Body>"), MethodCall);

            return sb.ToString();
        }

        private HttpWebRequest CreateWebRequest()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(this.Url);
            if (this.WSServiceType == WebServiceClient.ServiceType.WCF)
                webRequest.Headers.Add("SOAPAction", "\"http://tempuri.org/" + this.WCFContractName + "/" + this.WebMethod + "\"");
            else
                webRequest.Headers.Add("SOAPAction", "\"http://tempuri.org/" + this.WebMethod + "\"");

            webRequest.Headers.Add("To", this.Url);

            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml; charset=utf-8";
            webRequest.Method = "POST";
            return webRequest;
        }

        private string StripResponse(string SoapResponse)
        {
            string RegexExtract = @"<" + this.WebMethod + "Result>(?<Result>.*?)</" + this.WebMethod + "Result>";

            return Regex.Match(SoapResponse, RegexExtract).Groups["Result"].Captures[0].Value;
        }
        #endregion

        #region Public Methods
        public string InvokeService()
        {
            WebResponse response = null;
            string strResponse = "";

            //Create the request
            HttpWebRequest req = this.CreateWebRequest();

            //write the soap envelope to request stream
            using (Stream stm = req.GetRequestStream())
            {
                using (StreamWriter stmw = new StreamWriter(stm))
                {
                    stmw.Write(this.CreateSoapEnvelope());
                }
            }

            //get the response from the web service
            response = req.GetResponse();

            Stream str = response.GetResponseStream();

            StreamReader sr = new StreamReader(str);

            strResponse = sr.ReadToEnd();

            return this.StripResponse(HttpUtility.HtmlDecode(strResponse));
        }
        #endregion


    }
    public static class JObjectExtensions
    {
        public static IDictionary<string, string> ToDictionary(this JObject @object)
        {
            Dictionary<string, string> lastObject = new Dictionary<string, string>();

            var inParams = JsonConvert.SerializeObject(@object["inputType"]);

            // No inputType at all means there is nothing to read, it used to throw a NullReferenceException.
            var inputParams = JsonConvert.DeserializeObject<List<string>>(inParams) ?? new List<string>();

            foreach (var item in inputParams)
            {
                if (string.IsNullOrEmpty(item) || lastObject.ContainsKey(item))
                {
                    // the same name listed twice used to throw "An item with the same key has already been added"
                    continue;
                }

                // An object or an array is kept as its JSON text: this dictionary holds strings, and asking a
                // JObject for ToObject<string>() threw "Can not convert Object to String." - which is what a
                // value coming from a parent API with a nested response looks like.
                lastObject[item] = TokenToText(GetValueByKey(@object, item));
            }

            return lastObject;
        }

        /// <summary>
        /// The text of a JSON value for the string based request dictionaries: null stays null, an object or an
        /// array becomes its JSON text, anything else its plain value.
        /// </summary>
        public static string TokenToText(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            return token is JContainer ? token.ToString(Formatting.None) : token.ToObject<string>();
        }

        private static JToken GetValueByKey(JObject jObject, string key)
        {
            if (jObject != null)
            {
                foreach (KeyValuePair<string, JToken> jProperty in jObject)
                {
                    if (jProperty.Key.Equals(key) && jProperty.Value != null)
                    {
                        return jProperty.Value;
                    }
                    else if (jProperty.Value is JObject)
                    {
                        // Only when the nested object holds it: this used to return the nested result even when
                        // it was null, so every key sitting after the first object valued one was lost.
                        var nestedValue = GetValueByKey((JObject)jProperty.Value, key);
                        if (nestedValue != null)
                        {
                            return nestedValue;
                        }
                    }
                }
            }
            return null;
        }

    }


    public class ApiMapperService : IApiMapper
    {
        private readonly IRepository<ApiMapper> _mapperRepository;
        private readonly IRepository<ApiMapperTransactionLog> apiMapperTransactionLogRepository;
        private readonly IRepository<ApiMapperParams> _maparParamsRepository;
        private readonly IRepository<ApiMapperDetails> _mapperDetailsRepository;
        private readonly IRepository<APIMapperParamChild> _mapperParamsChildRepository;
        private readonly IdcmsLogService _dcmsLogService;
        private readonly IRepository<ApiMapperAuthorization> _mapperAuthorizationRepository;
        private readonly IApiLogService _dCMSCoreLogService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<UserOptions> _UserOptionsRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ServerExecutionContext _serverExecution;


        public ApiMapperService(IRepository<ApiMapper> mapperRepository, IRepository<ApiMapperTransactionLog> apiMapperTransactionLogRepository, IRepository<ApiMapperParams> maparParamsRepository, IRepository<ApiMapperAuthorization> mapperAuthorizationRepository,
            IRepository<ApiMapperDetails> mapperDetailsRepository, IdcmsLogService dcmsLogService, IApiLogService dCMSCoreLogService, IWorkContext workContext,
             IRepository<UserOptions> UserOptionsRepository, IHttpClientFactory httpClientFactory, IRepository<APIMapperParamChild> mapperParamChildRepository,
             IConfiguration configuration, ServerExecutionContext serverExecution)
        {
            _configuration = configuration;
            _serverExecution = serverExecution;
            _mapperRepository = mapperRepository;
            this.apiMapperTransactionLogRepository = apiMapperTransactionLogRepository;
            _maparParamsRepository = maparParamsRepository;
            _mapperDetailsRepository = mapperDetailsRepository;
            _dcmsLogService = dcmsLogService;
            _mapperAuthorizationRepository = mapperAuthorizationRepository;
            _dCMSCoreLogService = dCMSCoreLogService;
            _workContext = workContext;
            _UserOptionsRepository = UserOptionsRepository;
            _httpClientFactory = httpClientFactory;
            _mapperParamsChildRepository = mapperParamChildRepository;
        }

        /// <summary>
        /// Data sources listed in DataSources:ServerOnlyIds (e.g. an insurer's convert-to-policy
        /// call) run only from a backend scope (any background job that enters one); every other
        /// caller is refused, admins and parent/child chaining from a web request included. Read
        /// on each call, so a change to appsettings.json applies without a restart.
        /// </summary>
        private void EnsureDataSourceAllowed(long dataSourceId)
        {
            if (_serverExecution.IsServerScope)
                return;

            var serverOnly = _configuration.GetSection("DataSources:ServerOnlyIds").Get<long[]>();
            if (serverOnly != null && Array.IndexOf(serverOnly, dataSourceId) >= 0)
                throw new UnauthorizedAccessException($"Data source {dataSourceId} can only be run by the server.");
        }

        private static HttpContent CreateHttpContent(object content, string HttpMediaType)
        {
            HttpContent httpContent = null;

            if (content != null)
            {
                var ms = new MemoryStream();
                SerializeJsonIntoStream(content, ms);
                ms.Seek(0, SeekOrigin.Begin);
                httpContent = new StreamContent(ms);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue(HttpMediaType);
            }

            return httpContent;
        }
        public static void SerializeJsonIntoStream(object value, Stream stream)
        {
            using (var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            using (var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                var js = new JsonSerializer();
                js.Serialize(jtw, value);
                jtw.Flush();
            }
        }


        private List<Object> MatchingBodyArrayValue(IDictionary<string, string> Values, ICollection<ApiMapperParams> paramsValues)
        {
            var newObjList = new List<Object>();
            if (paramsValues != null && paramsValues.Any(p => p.ParamType == ParamsTypes.BodyArray))
            {
                foreach (var item in paramsValues)
                {
                    if (item.Direction != 0 && item.ParamType.ToString().ToUpper() == "BODYARRAY" && item.IsDeleted == false)
                    {
                        if (item.SourceType.ToString().ToUpper() == "CONTROL")
                        {
                            newObjList = JsonConvert.DeserializeObject<List<Object>>(Values.Where(x => x.Key == item.ParamName).FirstOrDefault().Value);
                        }
                        else if (item.SourceType.ToString().ToUpper() == "STATIC")
                        {
                            newObjList = JsonConvert.DeserializeObject<List<Object>>(item.ParamValue);
                        }
                        else if (item.SourceType == SourceType.DependOnOtherApi)
                        {
                            // resolved from the parent API into the request values, see ResolveDependentApiValues
                            string dependentValue;
                            if (Values != null && Values.TryGetValue(item.ParamName, out dependentValue))
                            {
                                newObjList = ToBodyArray(dependentValue);
                            }
                        }
                    }
                }
                return newObjList;
            }
            return null;
        }

        private Dictionary<string, object> MatchingParamsValues(IDictionary<string, string> Values, ICollection<ApiMapperParams> paramsValues)
        {
            var newObjList = new Dictionary<string, object>();
            if (paramsValues != null)
            {
                foreach (var item in paramsValues)
                {
                    if (item.SourceType == SourceType.DependOnOtherApi && item.IsDeleted == false)
                    {
                        // the value comes from the parent API, not from ParamValue, see ResolveDependentApiValues
                        string dependentValue;
                        if (Values != null && Values.TryGetValue(item.ParamName, out dependentValue))
                        {
                            newObjList[item.ParamName] = dependentValue;
                        }
                        continue;
                    }

                    if ((!string.IsNullOrEmpty(item.ParamValue) || item.ParamType.ToString().ToUpper() == "QUERYSTRING") && item.IsDeleted == false) // remove || item.ParamType.ToString().ToUpper() == "QUERYSTRING"
                    {
                        newObjList.Add(item.ParamName, item.ParamValue);
                    }
                }
            }

            return newObjList;
        }

        private Dictionary<string, string> MatchingQueryValues(IDictionary<string, string> Values, ICollection<ApiMapperParams> paramsValues)
        {
            var newObjList = new Dictionary<string, string>();
            if (paramsValues != null)
            {
                foreach (var item in paramsValues)
                {
                    if (item.Direction != 0 && item.ParamType.ToString().ToUpper() == "QUERYSTRING" && item.IsDeleted == false)
                    {
                        if (item.SourceType.ToString().ToUpper() == "CONTROL")
                        {
                            string value = Values.Where(x => x.Key == item.ParamName).FirstOrDefault().Value;
                            newObjList.Add(item.ParamName, value);
                        }
                        else if (item.SourceType.ToString().ToUpper() == "STATIC")
                        {
                            newObjList.Add(item.ParamName, item.ParamValue);
                        }
                        else if (item.SourceType == SourceType.DependOnOtherApi)
                        {
                            // resolved from the parent API into the request values, see ResolveDependentApiValues
                            string dependentValue;
                            Values.TryGetValue(item.ParamName, out dependentValue);
                            newObjList[item.ParamName] = dependentValue;
                        }
                    }
                }
            }
            return newObjList;
        }
        private string MatchingPathValues(IDictionary<string, string> Values, ICollection<ApiMapperParams> paramsValues)
        {
            var PathParams = "/";
            if (paramsValues != null)
            {
                foreach (var item in paramsValues)
                {
                    if (item.Direction != 0 && item.ParamType.ToString().ToUpper() == "PATH" && item.IsDeleted == false)
                    {
                        if (item.SourceType.ToString().ToUpper() == "CONTROL")
                        {
                            string value = Values.Where(x => x.Key == item.ParamName).FirstOrDefault().Value;
                            PathParams += value + "/";
                        }
                        else if (item.SourceType.ToString().ToUpper() == "STATIC")
                        {
                            PathParams += item.ParamValue + "/";
                        }
                        else if (item.SourceType == SourceType.DependOnOtherApi)
                        {
                            // resolved from the parent API into the request values, see ResolveDependentApiValues
                            string dependentValue;
                            if (Values != null && Values.TryGetValue(item.ParamName, out dependentValue))
                            {
                                PathParams += dependentValue + "/";
                            }
                        }
                    }
                }
            }
            return PathParams.Replace("//", "/").TrimEnd('/');
        }

        /// <summary>
        /// Resolves every SourceType.DependOnOtherApi input parameter of the API that is being invoked through
        /// PostStream (TestInvokerAPI), reusing ResolveDependentApiValue - the same logic the normal invoke
        /// pipeline (HandlingParams) uses. Every parent API is executed at most once and its response is cached
        /// for the whole call.
        /// The values are returned as the JSON they are (an object stays an object), and they are resolved
        /// against a copy of the request context: the resolution writes what it finds into the context it is
        /// given, and the caller still has to read the posted context as it was sent.
        /// </summary>
        private async Task<Dictionary<string, JToken>> ResolveDependentApiValues(ApiMapperViewModel ApiInfo, JObject content, long userId, string authToken)
        {
            var resolvedValues = new Dictionary<string, JToken>();

            var allParams = ApiInfo.ApiMapperParams;
            if (allParams == null)
            {
                return resolvedValues;
            }

            var dependentParams = allParams
                .Where(x => x.IsDeleted == false && x.Direction == ParameterDirection.Input && x.SourceType == SourceType.DependOnOtherApi)
                .OrderBy(x => x.ParamOrder)
                .ToList();

            if (dependentParams.Count == 0)
            {
                return resolvedValues;
            }

            // ParentApiMapperId -> Parent API response, shared by all the dependent params of this call.
            var apiResponses = new Dictionary<long, JToken>();
            var visited = new HashSet<long>();

            // A copy: ResolveDependentApiValue writes each resolved value into the context it is given, and a
            // nested value written into the posted context broke the reading of that context afterwards.
            var context = content == null ? new JObject() : (JObject)content.DeepClone();

            foreach (var item in dependentParams)
            {
                // the test screen can post the params without their owner, the dependency
                // in APIMapperParamChild is looked up by (ApiMapperId, ParamName).
                if (item.ApiMapperId == 0)
                {
                    item.ApiMapperId = ApiInfo.Id;
                }

                var value = await ResolveDependentApiValue(item, context, userId, authToken, apiResponses, visited, allParams);

                var valueToken = value == null ? null : (value as JToken ?? JToken.FromObject(value));
                resolvedValues[item.ParamName] = valueToken == null || valueToken.Type == JTokenType.Null ? null : valueToken;
            }

            return resolvedValues;
        }

        private async Task<object> PostStream(JObject content, ApiMapperViewModel ApiInfo, long userId, string authToken = "")
        {
            try
            {
                // SourceType.DependOnOtherApi inputs are resolved from their parent API (APIMapperParamChild)
                // before the request is built, exactly like the normal invoke pipeline does in HandlingParams.
                var dependentValues = await ResolveDependentApiValues(ApiInfo, content, userId, authToken);

                var values = JObjectExtensions.ToDictionary(content);

                // the resolved values are not necessarily listed in the incoming "inputType" list,
                // so push them into the request values explicitly (as text, for headers / query / path).
                foreach (var dependentValue in dependentValues)
                {
                    values[dependentValue.Key] = JObjectExtensions.TokenToText(dependentValue.Value);
                }

                string QuerstringVal = "";

                var HeaderValue = MatchingParamsValues(values, ApiInfo.ApiMapperParams).ToList();
                var QueryValues = MatchingQueryValues(values, ApiInfo.ApiMapperParams);
                var PathValues = MatchingPathValues(values, ApiInfo.ApiMapperParams);
                var BodyArrayValues = MatchingBodyArrayValue(values, ApiInfo.ApiMapperParams);
                if (ApiInfo.URL.EndsWith('/'))
                {
                    ApiInfo.URL.TrimEnd('/');
                }
                ApiInfo.URL += PathValues;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiInfo.URL);
                CookieContainer cookieContainer = new CookieContainer();
                request.CookieContainer = cookieContainer;

                // add URL query strings to QuerstringVal
                foreach (var item in QueryValues)
                {
                    if (QueryValues.Last().Key != item.Key)
                    {

                        if (item.Value != null)
                        {
                            QuerstringVal += item.Key + "=" + item.Value.ToString() + "&";
                        }
                        else
                        {

                            QuerstringVal += item.Key + "=" + "&"; ;
                        }
                    }
                    else
                    {
                        if (item.Value != null)
                        {
                            QuerstringVal += item.Key + "=" + item.Value.ToString();
                        }
                        else
                        {
                            QuerstringVal += item.Key + "=";
                        }
                    }
                }

                // if QuerstringVal contains any data, we will concat it to the base URL
                // do not change the order of creating the new URL to avoid reset the request data
                if (!string.IsNullOrEmpty(QuerstringVal))
                {
                    if (!ApiInfo.URL.EndsWith("?"))
                    {
                        ApiInfo.URL += "?";
                    }
                    string url = string.Concat(ApiInfo.URL, QuerstringVal);
                    request = (HttpWebRequest)WebRequest.Create(url);
                }

                request.Method = ApiInfo.HttpMethod.ToString();
                request.Credentials = CredentialCache.DefaultCredentials;
                request = addAuthorizationToHeader(ApiInfo, request, userId, authToken);
                string newContent = string.Empty;

                if (BodyArrayValues != null)
                {
                    newContent = JsonConvert.SerializeObject(BodyArrayValues);
                }
                else
                {
                    // A value resolved from a parent API keeps its JSON type in the body: an object stays an
                    // object instead of being sent as a quoted string, which is what a nested parent response
                    // (data.levelOne.levelTwo ...) gives.
                    var bodyObject = JObject.FromObject(values);

                    foreach (var dependentValue in dependentValues)
                    {
                        if (dependentValue.Value is JContainer)
                        {
                            bodyObject[dependentValue.Key] = dependentValue.Value;
                        }
                    }

                    newContent = bodyObject.ToString(Formatting.None);
                }

                var postData = Encoding.UTF8.GetBytes(newContent.ToString());

                // The request has a body when the API declares a Body or a BodyArray param. It is computed once
                // here and the body is written once below: it used to be written inside the params loop, which
                // means one full write per body param, and the second write threw
                // "Unable to write content to request stream; content would exceed Content-Length".
                var hasBodyContent = ApiInfo.ApiMapperParams != null && ApiInfo.ApiMapperParams.Any(item =>
                    item.IsDeleted == false
                    && item.Direction != ParameterDirection.Output
                    && (item.ParamType == ParamsTypes.Body || item.ParamType == ParamsTypes.BodyArray));

                // need review
                if (ApiInfo.ApiMapperParams != null)
                {
                    foreach (var param in ApiInfo.ApiMapperParams)
                    {
                        foreach (var item in HeaderValue)
                        {
                            if (item.Key == param.ParamName)
                            {
                                if (param.ParamType == ParamsTypes.Header || param.ParamType == ParamsTypes.QueryString)
                                {
                                    if (item.Value != null)
                                    {
                                        request.Headers.Add(item.Key, item.Value.ToString());
                                        values.Remove(item.Key);
                                    }
                                }
                            }
                        }
                    }

                }

                // add the content if the request is POST request, if you add it without checking the method type, 
                // it will return a bad request on GET requests
                if (request.Method.ToUpper() == "POST")
                {
                    // BodyArray counts as a body too, it used to be left out and the write below then
                    // exceeded a Content-Length of 0.
                    request.ContentLength = hasBodyContent ? postData.Length : 0;
                    request.ContentType = ApiInfo.MediaTypes;
                }

                // need review
                if (ApiInfo.ApiMapperParams != null)
                {
                    foreach (var param in ApiInfo.ApiMapperParams)
                    {
                        if ((param.ParamType == ParamsTypes.Header) || (param.ParamType == ParamsTypes.QueryString))
                        {
                            foreach (var item in HeaderValue)
                            {
                                if (item.Key == param.ParamName)
                                {
                                    if (param.ParamType == ParamsTypes.Header)
                                    {
                                        if (item.Value != null)
                                        {
                                            if (request.Headers[item.Key] != null)
                                            {
                                                request.Headers.Remove(item.Key);
                                            }
                                            request.Headers.Add(item.Key, item.Value.ToString());
                                        }
                                        else
                                        {
                                            request.Headers.Add(item.Key, "");
                                        }

                                    }
                                }
                            }
                        }
                    }

                }

                request.ServicePoint.ConnectionLeaseTimeout = 1200000;
                request.ServicePoint.MaxIdleTime = 50000;
                ServicePointManager.DefaultConnectionLimit = 200;

                // The body is sent straight away instead of waiting for a "100 Continue" answer. A target behind
                // a tunnel or a proxy that does not complete that handshake receives the headers and the
                // Content-Length but no content at all, so its [FromBody] parameter arrives null.
                request.ServicePoint.Expect100Continue = false;

                var isFormData = !string.IsNullOrEmpty(ApiInfo.MediaTypes) && ApiInfo.MediaTypes.ToLower().Equals("multipart/form-data");

                // One single write, now that every header is set: getting the request stream sends the headers,
                // so writing from inside the loop above also broke any header that came after a body param.
                // multipart/form-data is sent further down through its own HttpClient request.
                if (hasBodyContent && !isFormData && MethodAllowsBody(request.Method))
                {
                    using (var requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(postData, 0, postData.Length);
                    }
                }

                if (isFormData)
                {
                    string apiResponse = "";
                    var form = new MultipartFormDataContent();

                    if (values != null)
                    {
                        foreach (var item in values)
                        {
                            form.Add(new StringContent(item.Value.ToString()), item.Key);
                        }
                    }

                    var FormDataRequest = new HttpRequestMessage()
                    {
                        RequestUri = new Uri(ApiInfo.URL),
                        Content = form,
                        Method = new HttpMethod(ApiInfo.HttpMethod.ToString())

                    };
                    FormDataRequest = await addAuthorizationToHeader(ApiInfo, FormDataRequest, userId, authToken);


                    if (HeaderValue != null)
                    {
                        foreach (var item in HeaderValue)
                        {
                            FormDataRequest.Headers.Add(item.Key, item.Value.ToString());
                        }

                    }
                    var handler = new HttpClientHandler
                    {
                        UseCookies = true,
                        CookieContainer = new CookieContainer(),
                    };
                    // Dispose the client (and its handler) at method scope to avoid leaking
                    // sockets/handlers per call. Used only within this method.
                    using HttpClient client = new HttpClient(handler);

                    using (var formDataResponse = await client.SendAsync(FormDataRequest))
                    {

                        var cookies = handler.CookieContainer.GetCookies(request.RequestUri);

                        foreach (Cookie cookie in cookies)
                        {
                            await _workContext.AppendCookies(cookie.Name, cookie.Value);

                        }
                        apiResponse = await formDataResponse.Content.ReadAsStringAsync();
                        ApiLogVM dCMSCoreLog = new ApiLogVM
                        {
                            ApiUrl = FormDataRequest.RequestUri.LocalPath,
                            Response = apiResponse.ToString(),
                            FullUrl = FormDataRequest.RequestUri.Host.ToString(),
                            HttpMethod = ApiInfo.HttpMethod.ToString(),
                            Request = FormDataRequest.ToString(),
                            SiteId = await _workContext.GetCurrentSiteIdAsync(),
                            StatusCode = (long)formDataResponse.StatusCode,
                            UserId = userId
                        };
                        _dCMSCoreLogService.CreateLog(dCMSCoreLog);

                        var resultObj = JsonConvert.DeserializeObject(apiResponse);
                        return resultObj;
                    }

                }
                else
                {
                    string result = "";
                    HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse();

                    var cookies = httpResponse.Cookies;

                    foreach (Cookie cookie in cookies)
                    {
                        await _workContext.AppendCookies(cookie.Name, cookie.Value);

                    }
                    ApiLogVM dCMSCoreLog = new ApiLogVM
                    {
                        ApiUrl = request.RequestUri.LocalPath,
                        Response = httpResponse.ToString(),
                        FullUrl = request.RequestUri.Host.ToString(),
                        HttpMethod = request.Method,
                        Request = request.ToString(),
                        SiteId = await _workContext.GetCurrentSiteIdAsync(),
                        StatusCode = (long)httpResponse.StatusCode,
                        UserId = userId
                    };
                    _dCMSCoreLogService.CreateLog(dCMSCoreLog);
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                        streamReader.Dispose();
                        streamReader.Close();
                        httpResponse.Dispose();
                        httpResponse.Close();
                    }

                    var resultObj = JsonConvert.DeserializeObject(result);
                    return resultObj;
                }


            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private dynamic PostStreamInvoke(JObject content, ApiMapperViewModel ApiInfo, long userId, string authToken = "")
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiInfo.URL);

            var values = JObjectExtensions.ToDictionary(content);
            string QuerstringVal = string.Empty;
            string responseFromServer = string.Empty;
            var HeaderValue = MatchingParamsValues(values, ApiInfo.ApiMapperParams);
            var QueryValues = MatchingQueryValues(values, ApiInfo.ApiMapperParams);
            var BodyArrayValues = MatchingBodyArrayValue(values, ApiInfo.ApiMapperParams);
            foreach (var item in QueryValues)
            {
                if (QueryValues.Last().Key != item.Key)
                {

                    if (item.Value != null)
                    {
                        QuerstringVal += item.Key + "=" + item.Value.ToString() + "&";
                    }
                    else
                    {
                        QuerstringVal += item.Key + "=" + "&"; ;
                    }
                }
                else
                {
                    if (item.Value != null)
                    {
                        QuerstringVal += item.Key + "=" + item.Value.ToString();
                    }
                    else
                    {
                        QuerstringVal += item.Key + "=";
                    }
                }
            }

            string url = string.Empty;
            if (!string.IsNullOrEmpty(QuerstringVal))
            {
                url = string.Concat(ApiInfo.URL, QuerstringVal);
                request = (HttpWebRequest)WebRequest.Create(url);
            }

            if (!string.IsNullOrEmpty(ApiInfo.MediaTypes) && ApiInfo.MediaTypes.Equals("multipart/form-data"))
            {
                var formData = new MultipartFormDataContent();

                foreach (var item in values)
                {
                    formData.Add(new StringContent(item.Value), item.Key);
                }
                using HttpClient client = _httpClientFactory.CreateClient();
                var formDataRequest = client.PostAsync(ApiInfo.URL, formData).Result;
                responseFromServer = formDataRequest.Content.ReadAsStringAsync().Result;
            }
            else
            {
                request.Method = ApiInfo.HttpMethod.ToString();
                request.Credentials = CredentialCache.DefaultCredentials;
                string newContent = string.Empty;

                if (BodyArrayValues != null)
                {
                    JsonConvert.SerializeObject(BodyArrayValues);
                }
                else
                {
                    JsonConvert.SerializeObject(values);

                }

                var postData = Encoding.UTF8.GetBytes(newContent.ToString());
                if (request.Method.ToUpper() == "POST")
                {
                    request.ContentLength = postData.Length;
                    request.ContentType = ApiInfo.MediaTypes;
                    request.Accept = "*/*";
                }

                foreach (var param in ApiInfo.ApiMapperParams)
                {
                    foreach (var item in HeaderValue)
                    {
                        if (item.Key == param.ParamName)
                        {
                            if (param.ParamType == ParamsTypes.Header || param.ParamType == ParamsTypes.QueryString)
                            {
                                if (item.Value != null)
                                {
                                    request.Headers.Add(item.Key, item.Value.ToString());
                                    values.Remove(item.Key);
                                }
                            }
                        }
                    }
                }



                foreach (var param in ApiInfo.ApiMapperParams)
                {
                    if ((param.ParamType == ParamsTypes.Header) || (param.ParamType == ParamsTypes.QueryString))
                    {
                        foreach (var item in HeaderValue)
                        {
                            if (item.Key == param.ParamName)
                            {
                                if (item.Value != null)
                                {
                                    if (request.Headers[item.Key] != null)
                                    {
                                        request.Headers.Remove(item.Key);
                                    }
                                    request.Headers.Add(item.Key, item.Value.ToString());
                                }
                                else
                                {
                                    request.Headers.Add(item.Key, "");
                                }

                            }
                        }
                    }
                    else if (param.ParamType == ParamsTypes.Body || param.ParamType == ParamsTypes.BodyArray)
                    {
                        request.GetRequestStream().Write(postData, 0, postData.Length);
                    }
                }

                request = addAuthorizationToHeader(ApiInfo, request, userId, authToken);

                request.ServicePoint.ConnectionLeaseTimeout = 1200000;
                request.ServicePoint.MaxIdleTime = 50000;
                ServicePointManager.DefaultConnectionLimit = 200;

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    int statusCode = (int)response.StatusCode;
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                }

            }

            JToken token = JToken.Parse(responseFromServer);

            var outParams = ApiInfo.ApiMapperParams.Where(p => p.Direction == ParameterDirection.Output);
            switch (ApiInfo.ApiReponseType)
            {
                case ApiReponseType.Array:
                    JArray objectToFetch = new JArray(JArray.Parse(responseFromServer));
                    return CreateArrayResult(objectToFetch, outParams, ApiInfo.OutputPath);

                case ApiReponseType.Object:
                    return CreateObjectResult(token, outParams, ApiInfo.Path);

                default:
                    return token;
            }


        }

        private dynamic CreateObjectResult(object tokentemp, IEnumerable<ApiMapperParams> outParams, string responsePath)
        {
            try
            {
                var newToken = JsonConvert.SerializeObject(tokentemp);
                var token = JsonConvert.DeserializeObject<JToken>(newToken);
                if (token.GetType() == typeof(JArray))
                {
                    token = token[0];
                }
                JArray innerValues = new JArray();

                if (token.Type != JTokenType.Object) throw new ArgumentException("Response does not match the type defined");
                if (string.IsNullOrEmpty(responsePath))
                {
                    JObject value = new JObject();
                    if (outParams != null && outParams.Count() > 0)
                    {
                        foreach (var param in outParams)
                        {
                            if (token.Children().Select(x => x.Path == param.ParamName).Any())
                            {
                                string ParamName_Alias = string.IsNullOrEmpty(param.Alias) ? param.ParamName : param.Alias;
                                value.Add(ParamName_Alias, token[param.ParamName]);
                            }
                            else throw new ArgumentException("Array does not contain submitted property");
                        }
                    }
                    else
                    {
                        value = (JObject)token;
                    }
                    return value;
                }
                else
                {
                    var childs = token.Children().Where(x => x.Path == responsePath).FirstOrDefault();
                    JObject innerObjects = new JObject();

                    if (childs != null)
                    {
                        foreach (var child in childs.Children())
                        {
                            for (int i = 0; i < child.Children().Count(); i++)
                            {
                                var obj = child.Children().ToList()[i];

                                if (obj.Type == JTokenType.Property)
                                {
                                    var item = obj.Children().ToList();
                                    var PropName = ((JProperty)obj).Name;

                                    string ParamName_Alias = PropName;

                                    if (outParams.Count() > 0)
                                    {
                                        var matchingParam = outParams.FirstOrDefault(param => param.ParamName == PropName);

                                        if (matchingParam != null)
                                        {
                                            if (matchingParam.Alias != null)
                                            {
                                                ParamName_Alias = matchingParam.Alias;
                                            }
                                        }
                                    }

                                    bool ShouldAddParam = outParams.Count() == 0 || outParams.FirstOrDefault(param => param.ParamName == PropName) != null;
                                    var tokenPathArray = item.Where(x => x.Path == responsePath + '[' + i + "]." + PropName)
                                                            .Children()
                                                            .FirstOrDefault();
                                    var tokenPathObj = item.Where(x => x.Path == responsePath + "." + PropName)
                                                          .FirstOrDefault();

                                    if (ShouldAddParam && tokenPathArray != null)
                                    {
                                        innerObjects.Add(ParamName_Alias, tokenPathArray); // Convert to string
                                    }
                                    else if (ShouldAddParam)
                                    {
                                        innerObjects.Add(ParamName_Alias, tokenPathObj); // Convert to string
                                    }
                                }

                                else
                                {
                                    foreach (var param in outParams)
                                    {
                                        if (obj.Select(x => x.Path == param.ParamName) != null)
                                        {
                                            var item = obj.Children().ToList();
                                            var tokenPathArray = item.Where(x => x.Path == responsePath + '[' + i + "]." + param.ParamName)
                                                .Children()
                                                .FirstOrDefault();
                                            var tokenPathObj = item.Where(x => x.Path == responsePath + "." + param.ParamName)
                                                .FirstOrDefault();

                                            string ParamName_Alias = string.IsNullOrEmpty(param.Alias) ? param.ParamName : param.Alias;

                                            if (tokenPathArray != null)
                                            {

                                                innerObjects.Add(ParamName_Alias, tokenPathArray);
                                            }
                                            else
                                            {
                                                innerObjects.Add(ParamName_Alias, tokenPathObj);
                                            }
                                        }
                                        else throw new ArgumentException("Object does not contain submitted property");
                                    }
                                }

                            }
                        }

                    }
                    return innerObjects;
                }

            }
            catch (Exception exc)
            {

                throw exc;
            }

        }

        private JArray CreateArrayResult(dynamic token, IEnumerable<ApiMapperParams> outParams, List<string> responsePath)
        {
            JArray objectToFetch = new JArray();
            try
            {
                objectToFetch = JArray.Parse(token);
                token = objectToFetch;
            }
            catch (Exception)
            {
                var obj = JsonConvert.DeserializeObject(token);
                if (responsePath != null && responsePath.Count() > 0)
                {
                    foreach (var path in responsePath)
                    {
                        if (obj != null)
                        {
                            try
                            {
                                obj = obj[path];
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    token = obj ?? new JArray();
                }
                else
                {
                    token = obj;
                }
            }

            if (responsePath == null || responsePath.Count() == 0)
            {
                if (token is JTokenType.Array) throw new ArgumentException("Response does not match the type defined");
                JArray values = new JArray();
                if (outParams.Count() > 0)
                {
                    for (int i = 0; i < token.Count; i++)
                    {
                        var obj = token[i] as JObject;
                        JObject value = new JObject();
                        List<JObject> listResponse = new List<JObject>();
                        JObject matchObject = new JObject();
                        List<JObject> matches;
                        foreach (var param in outParams)
                        {

                            matches = FindKey(token, param.ParamName, param.Alias, listResponse, matchObject);
                        }
                        var resLength = 0;
                        if (outParams.Count() > 0)
                        {
                            resLength = listResponse.Count / outParams.Count();
                        }
                        for (var item = 0; item < resLength; item++)
                        {
                            var finalKey = listResponse[item];
                            for (var p = 1; p < outParams.Count(); p++)
                            {
                                var keyToAdd = listResponse[resLength * p];
                                finalKey.Merge(keyToAdd, new JsonMergeSettings
                                {
                                    MergeArrayHandling = MergeArrayHandling.Union
                                });
                            }
                            values.Add(finalKey);
                        }
                    }
                }
                else
                {
                    values = token;
                }
                return values;
            }
            else
            {
                JArray values = new JArray();
                if (outParams.Count() > 0 && token != null)
                {
                    for (int i = 0; i < token.Count; i++)
                    {
                        JObject item = token[i];
                        JObject obj = new JObject();

                        foreach (var param in outParams)
                        {
                            string ParamName_Alias = string.IsNullOrEmpty(param.Alias) ? param.ParamName : param.Alias;

                            JToken temp = null;
                            if (item.TryGetValue(param.ParamName, out temp))
                            {
                                obj.Add(ParamName_Alias, item[param.ParamName]);
                            }
                            else
                            {
                                obj.Add(ParamName_Alias, null);
                            }
                        }


                        values.Add(obj);
                    }
                }
                else
                {
                    values = token;
                }
                return values ?? new JArray();
            }

        }
        private static List<JObject> FindKey(JToken containerToken, string name, string Alias, List<JObject> listResponse, JObject matchObject)
        {

            if (containerToken.Type == JTokenType.Object)
            {
                foreach (JProperty child in containerToken.Children<JProperty>())
                {

                    var preObject = listResponse.FirstOrDefault(x => x.ContainsKey(child.Name) && x.GetValue(child.Name) == child.Value);
                    if (preObject == null)
                    {
                        matchObject = new JObject();
                        if (child.Name == name)
                        {
                            string ParamName_Alias = string.IsNullOrEmpty(Alias) ? child.Name : Alias;
                            matchObject.Add(ParamName_Alias, child.Value);
                            listResponse.Add(matchObject);
                            return listResponse;
                        }
                    }
                    else
                    {
                        if (preObject.GetValue(name) == null)
                        {
                            preObject.Add(child.Next);
                            return listResponse;
                        }
                        else
                        {
                            matchObject = new JObject();
                            if (child.Name == name)
                            {
                                string ParamName_Alias = string.IsNullOrEmpty(Alias) ? child.Name : Alias;
                                matchObject.Add(ParamName_Alias, child.Value);
                                listResponse.Add(matchObject);
                                return listResponse;
                            }
                        }
                    }

                    FindKey(child.Value, name, Alias, listResponse, matchObject);
                }
            }
            else if (containerToken.Type == JTokenType.Array)
            {
                foreach (JToken child in containerToken.Children())
                {
                    FindKey(child, name, Alias, listResponse, matchObject);
                }
                return listResponse;
            }
            return listResponse;


        }

        private static object WebApiResponse(string Res, ApiMapperViewModel ApiInfo)
        {
            try
            {
                var jsonObj = JsonConvert.SerializeObject(Res);

                return jsonObj;
            }
            catch (Exception)
            {

                throw;
            }
        }



        private string InvokeWCF(ApiMapperViewModel ApiInfo, JObject Params)
        {
            try
            {
                WebServiceClient Inv = new WebServiceClient();
                Inv.Url = ApiInfo.URL;

                var values = JObjectExtensions.ToDictionary(Params);
                Inv.Parameters = (IDictionary<string, object>)values;
                Inv.WCFContractName = ApiInfo.ContractName; //"IService";
                Inv.WebMethod = ApiInfo.WebMethodName;// "GetData";
                Inv.WSServiceType = ServiceType.WCF;
                return Inv.InvokeService();
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        private async Task<object> InvokeWebApi(ApiMapperViewModel ApiInfo, JObject Params, long userId, string authToken = "")
        {
            try
            {
                return await PostStream(Params, ApiInfo, userId, authToken);
            }

            catch (Exception exc)
            {

                throw exc;
            }
        }
        private dynamic InvokeWebResponse(ApiMapperViewModel ApiInfo, JObject Params, long userId = 0, string token = null)
        {
            try
            {

                string Json = JsonConvert.SerializeObject(Params);

                return PostStreamInvoke(Params, ApiInfo, userId, token);

            }

            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<dynamic> StaticInvoke(StaticInvokeVM model)
        {
            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(model.ApiUrl);

                request.Method = model.HttpMethod;
                request.Credentials = CredentialCache.DefaultCredentials;
                var newContent = JsonConvert.SerializeObject(model.Requestdata);

                var postData = Encoding.UTF8.GetBytes(newContent.ToString());

                if (request.Method.ToUpper() == "POST")
                {
                    request.ContentLength = postData.Length;
                    request.ContentType = model.ContentType;
                    request.GetRequestStream().Write(postData, 0, postData.Length);

                }



                request.ServicePoint.ConnectionLeaseTimeout = 1200000;
                request.ServicePoint.MaxIdleTime = 50000;
                ServicePointManager.DefaultConnectionLimit = 200;

                string result = "";
                HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse();
                ApiLogVM dCMSCoreLog = new ApiLogVM
                {
                    ApiUrl = request.RequestUri.LocalPath,
                    Response = httpResponse.ToString(),
                    FullUrl = request.RequestUri.Host.ToString(),
                    HttpMethod = request.Method,
                    Request = request.ToString(),
                    SiteId = await _workContext.GetCurrentSiteIdAsync(),
                    StatusCode = (long)httpResponse.StatusCode,
                    UserId = _workContext.GetCurrentUserIdValue()
                };
                _dCMSCoreLogService.CreateLog(dCMSCoreLog);
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    streamReader.Dispose();
                    streamReader.Close();
                    httpResponse.Dispose();
                    httpResponse.Close();
                }

                var resultObj = JsonConvert.DeserializeObject(result);
                return resultObj;
            }

            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<dynamic> Invoke(long ApiId, JObject obj, long userId, string token = null)
        {
            EnsureDataSourceAllowed(ApiId);

            try
            {
                var ApiInfo = await GetApiMapperById(ApiId);

                if (ApiInfo.Type == ApiTypes.WebApi)
                {
                    return InvokeWebResponse(ApiInfo, obj, userId, token);
                }
                else if (ApiInfo.Type == ApiTypes.WCF)
                {
                    return InvokeWCF(ApiInfo, obj);
                }
                else
                {
                    return await InvokeWebApi(ApiInfo, obj, userId);
                }

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<object> TestInvokerAPI(ApiMapperViewModel ApiInfo, JObject obj, long userId, string authToken = "")
        {
            if (ApiInfo != null && ApiInfo.Id > 0)
                EnsureDataSourceAllowed(ApiInfo.Id);

            try
            {
                if (ApiInfo.Type == ApiTypes.WebApi)
                {
                    return await InvokeWebApi(ApiInfo, obj, userId, authToken);
                }
                else if (ApiInfo.Type == ApiTypes.WCF)
                {
                    return InvokeWCF(ApiInfo, obj);
                }
                else
                {
                    return await InvokeWebApi(ApiInfo, obj, userId);
                }

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        /////////////////////////////////////////// luay new api /////// start//////////

        public async Task<List<inputValuesViewModel>> GetInputValues(long Id)
        {
            try
            {
                List<inputValuesViewModel> InputValuesLst = new List<inputValuesViewModel>();

                var values = await _maparParamsRepository.Query().Where(x => x.ApiMapperId == Id && x.IsDeleted == false)
                 .Select(x => new
                 {
                     x.ParamName,
                     x.ParamValue,
                     x.SourceType,
                     x.DefaultValue,
                     path = _mapperDetailsRepository.Query().Where(x => x.ApiMapperId == Id).Select(x => x.ResponsePath).FirstOrDefault()
                 }).ToListAsync();

                foreach (var item in values)
                {
                    inputValuesViewModel InputValuesObj = new inputValuesViewModel();
                    Dictionary<string, string> inputValueDic = new Dictionary<string, string>();
                    inputValueDic.Add(item.ParamName, item.ParamValue);
                    InputValuesObj.SourceType = item.SourceType;
                    InputValuesObj.inputValue = inputValueDic;
                    InputValuesObj.path = item.path;
                    InputValuesLst.Add(InputValuesObj);
                }

                return InputValuesLst;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<ApiMapperViewModel>> GetApiMapperSiteId(long SiteId)
        {
            try
            {
                List<ApiMapperViewModel> mapperList = await _mapperRepository.Query().Include(x=>x.APIMapperParamChild)
                    .Where(x =>
                    x.Status != EStatus.Deleted)
                    .Select(p => new ApiMapperViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        URL = p.URL,
                        HttpMethod = p.HttpMethod,
                        Type = p.Type,
                        Status = p.Status,
                        AuthorizationType = p.AuthorizationType,
                        SiteId = p.SiteId,
                        EnableLog = p.EnableLog,
                        APIMapperParamChild=p.APIMapperParamChild

                    }).ToListAsync();

                return mapperList;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        //public async Task<List<ApiMapper>> GetDataSourceListBySiteId(long siteId)
        //{
        //    try
        //    {
        //        //List<ApiMapper> resultList = new List<ApiMapper>();
        //        var DataSourceList = await _mapperRepository.Query()
        //            .Include(x => x.ApiMapperAuthorizations).Include(x=>x.APIMapperParamChild).Include(s=>s.ApiMapperParams)
        //            .Where(x =>
        //            x.Status != EStatus.Deleted && x.SiteId == siteId)
        //            .AsNoTracking()
        //            .ToListAsync();

        //        // The Parent relation was removed from ApiMapper, it is now described by APIMapperParamChild.
        //        // The (legacy) ParentId still shown by the admin screens is derived from those links.
        //        //var parentLinks = await _mapperParamsChildRepository.Query()
        //        //    .Select(x => new
        //        //    {
        //        //        x.ApiMapperId,
        //        //        ParentApiMapperId = x.ParentId ?? x.ApiMapperParams.ApiMapperId
        //        //    })
        //        //    .ToListAsync();

        //        //foreach (var item in DataSourceList)
        //        //{
        //        //    ApiMapperDetailsViewModel DataSourceObject = new ApiMapperDetailsViewModel();

        //        //    DataSourceObject.Id = item.Id;
        //        //    DataSourceObject.Name = item.Name;
        //        //    DataSourceObject.Description = item.Description;
        //        //    DataSourceObject.URL = item.URL;
        //        //    DataSourceObject.HttpMethod = item.HttpMethod;
        //        //    DataSourceObject.MediaTypes = item.MediaType;
        //        //    DataSourceObject.Type = item.Type;
        //        //    DataSourceObject.Status = item.Status;
        //        //    DataSourceObject.AuthorizationType = item.AuthorizationType;
        //        //    DataSourceObject.ApiReponseType = item.ApiReponseType;
        //        //    DataSourceObject.SiteId = item.SiteId;
        //        //    DataSourceObject.EnableLog = item.EnableLog;
        //        //    DataSourceObject.ParentId = parentLinks.FirstOrDefault(x => x.ApiMapperId == item.Id)?.ParentApiMapperId;
        //        //    DataSourceObject.ContractName = item.ContractName;
        //        //    DataSourceObject.ContentType = item.ContentType;
        //        //    DataSourceObject.Accept = item.Accept;
        //        //    DataSourceObject.WebMethodName = item.WebMethodName;
        //        //    DataSourceObject.ApiMapperParams = item.ApiMapperParams;
        //        //    DataSourceObject.path = _mapperDetailsRepository.Query().Where(x => x.ApiMapperId == DataSourceObject.Id).Select(x => x.ResponsePath).FirstOrDefault();
        //        //    DataSourceObject.inputs = getInputsById(item.Id);
        //        //    DataSourceObject.outputs = getOutputsById(item.Id);
        //        //    DataSourceObject.AuthorizationKeys = item.ApiMapperAuthorizations;
        //        //    resultList.Add(DataSourceObject);
        //        //}
        //        return DataSourceList;
        //    }
        //    catch (Exception ex)
        //    {

        //        throw ex;
        //    }
        //}



        public async Task<List<ApiMapperDetailsViewModel>> GetDataSourceListBySiteId(long siteId)
{
    var list = await _mapperRepository.Query()
        .Include(x => x.ApiMapperAuthorizations)
        .Include(x => x.APIMapperParamChild)
        .Include(x => x.ApiMapperParams)
        .Where(x => x.Status != EStatus.Deleted && x.SiteId == siteId)
        .AsNoTracking()
        .ToListAsync();

    var ids = list.Select(x => x.Id).ToList();

    var paths = await _mapperDetailsRepository.Query()
        .Where(x => ids.Contains(x.ApiMapperId))
        .Select(x => new { x.ApiMapperId, x.ResponsePath })
        .ToListAsync();

    // Parent now lives in APIMapperParamChild (same rule as the commented-out parentLinks query)
    var parentLinks = await _mapperParamsChildRepository.Query()
        .Where(x => ids.Contains(x.ApiMapperId))
        .Select(x => new { x.ApiMapperId, ParentApiMapperId = x.ParentId ?? x.ApiMapperParams.ApiMapperId })
        .ToListAsync();

	return list.Select(item => new ApiMapperDetailsViewModel
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        URL = item.URL,
        HttpMethod = item.HttpMethod,
        MediaTypes = item.MediaType,
        Type = item.Type,
        Status = item.Status,
        AuthorizationType = item.AuthorizationType,
        AuthorizationKeys = item.ApiMapperAuthorizations,
        ApiReponseType = item.ApiReponseType,
        SiteId = item.SiteId,
        EnableLog = item.EnableLog,
        ContractName = item.ContractName,
        ContentType = item.ContentType,
        Accept = item.Accept,
        WebMethodName = item.WebMethodName,
        ApiMapperParams = item.ApiMapperParams,
        APIMapperParamChild = item.APIMapperParamChild,
        path = paths.FirstOrDefault(p => p.ApiMapperId == item.Id)?.ResponsePath,
        ParentId = parentLinks.FirstOrDefault(p => p.ApiMapperId == item.Id)?.ParentApiMapperId,

        //parentInputs = list.Where(x => x.Id == parentLinks.FirstOrDefault(p => p.ApiMapperId == item.Id)?.ParentApiMapperId).FirstOrDefault(),


		// same filter as getInputsById / getOutputsById
		inputs = item.ApiMapperParams
            .Where(p => p.Direction == ParameterDirection.Input && !p.IsDeleted)
            .Select(p => new ApiMapperParamsViewModel
            {
                Id = p.Id, Direction = p.Direction, ApiMapperId = p.ApiMapperId,
                ParamName = p.ParamName, ParamOrder = p.ParamOrder, ParamValue = p.ParamValue,
                SourceType = p.SourceType, ParamType = p.ParamType, CreatedById = p.CreatedById,
                DefaultValue = p.DefaultValue, Alias = p.Alias
            }).ToList(),
        outputs = item.ApiMapperParams
            .Where(p => p.Direction == ParameterDirection.Output)
            .Select(p => new OutputsParameters { Id = p.Id, Name = p.ParamName, Alias = p.Alias })
            .ToList()
    }).ToList();
}

        public async Task<ApiMapperDetailsViewModel> GetDatasourceById(long Id)
        {
            try
            {
                ApiMapperDetailsViewModel MapperObject = new ApiMapperDetailsViewModel();
                MapperObject = await _mapperRepository.Query().Include(x=>x.APIMapperParamChild)
                    .Where(x => x.Id == Id)
                    .Include(x => x.ApiMapperAuthorizations)
                    .Select(p => new ApiMapperDetailsViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        URL = p.URL,
                        ApiMapperParams = p.ApiMapperParams,
                        HttpMethod = p.HttpMethod,
                        Type = p.Type,
                        Status = p.Status,
                        AuthorizationType = p.AuthorizationType,
                        SiteId = p.SiteId,
                        MediaTypes = p.MediaType,
                        EnableLog = p.EnableLog,
                        ApiReponseType = p.ApiReponseType,
                        AuthorizationKeys = p.ApiMapperAuthorizations,
                        APIMapperParamChild = p.APIMapperParamChild
                        // Parent is no longer stored on ApiMapper, it comes from APIMapperParamChild.
                        //ParentId = _mapperParamsChildRepository.Query()
                        //    .Where(x => x.ApiMapperId == p.Id)
                        //    .Select(x => (long?)(x.ParentId ?? x.ApiMapperParams.ApiMapperId))
                        //    .FirstOrDefault()
                    }).FirstOrDefaultAsync();
                MapperObject.inputs = getInputsById(MapperObject.Id);
                MapperObject.outputs = getOutputsById(MapperObject.Id);
                return MapperObject;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<dynamic> GetDatasourceByName(string name, JObject obj, int userId = 0, string token = "")
        {
            try
            {
                var dataSource = await _mapperRepository.Query().AsNoTracking().Where(x => x.URL.ToUpper().Contains(name.ToUpper())).FirstOrDefaultAsync();
                return await Invoke(dataSource.Id, obj, userId, token);
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public List<ApiMapperParamsViewModel> getInputsById(long Id)
        {

            return _maparParamsRepository.Query().Where(x => x.ApiMapperId == Id && x.Direction == ParameterDirection.Input && x.IsDeleted == false).Select(p => new ApiMapperParamsViewModel
            {
                Id = p.Id,
                Direction = p.Direction,
                ApiMapperId = p.ApiMapperId,
                ParamName = p.ParamName,
                ParamOrder = p.ParamOrder,
                ParamValue = p.ParamValue,
                SourceType = p.SourceType,
                ParamType = p.ParamType,
                CreatedById = p.CreatedById,
                DefaultValue = p.DefaultValue,
				APIMapperParamChild=p.APIMapperParamChild,

				Alias = p.Alias
            }).ToList();
        }
        public List<OutputsParameters> getOutputsById(long Id)
        {
            return _maparParamsRepository.Query().Where(x => x.ApiMapperId == Id && x.Direction == ParameterDirection.Output).Select(p => new OutputsParameters {Id=p.Id, Name = p.ParamName, Alias = p.Alias }).ToList();
        }




        public async Task<long> AddApiMapper(DatasourceVM datasource, long siteId, long userID)
        {
            try
            {
                ApiMapper maperObj = new ApiMapper();
                maperObj.Name = datasource.apiMapperViewModel.Name;
                maperObj.Description = datasource.apiMapperViewModel.Description;
                maperObj.URL = datasource.apiMapperViewModel.URL;
                maperObj.HttpMethod = datasource.apiMapperViewModel.HttpMethod;
                maperObj.Status = datasource.apiMapperViewModel.Status;
                maperObj.MediaType = datasource.apiMapperViewModel.MediaTypes;
                maperObj.AuthorizationType = datasource.apiMapperViewModel.AuthorizationType;
                maperObj.SiteId = siteId;
                maperObj.CreatedById = userID;
                maperObj.CreationDate = DateTimeOffset.Now;
                maperObj.ApiReponseType = datasource.apiMapperViewModel.ApiReponseType;
                maperObj.Type = datasource.apiMapperViewModel.Type;
                maperObj.ContentType = datasource.apiMapperViewModel.ContentType;
                maperObj.WebMethodName = datasource.apiMapperViewModel.WebMethodName;
                maperObj.ContractName = datasource.apiMapperViewModel.ContractName;
                maperObj.Accept = datasource.apiMapperViewModel.Accept;
                maperObj.ApiReponseType = datasource.apiMapperViewModel.ApiReponseType;
                // ParentId is no longer kept on ApiMapper, the dependency is stored in APIMapperParamChild
                // by AutoWireParentChildParams below.
                maperObj.EnableLog = datasource.apiMapperViewModel.EnableLog;

                _mapperRepository.Add(maperObj);
                await _mapperRepository.SaveChangesAsync();

                await changeAuthorizationType(datasource.apiMapperViewModel.AuthorizationKeys, maperObj.Id, datasource.tokenSource);


                for (int i = 0; i < datasource.inputs.Count; i++)
                {

                    ApiMapperParams NewParam = new ApiMapperParams();
                    NewParam.ApiMapperId = maperObj.Id;
                    NewParam.ParamName = datasource.inputs[i].ParamName;
                    NewParam.ParamOrder = datasource.inputs[i].ParamOrder;
                    NewParam.CreationDate = DateTimeOffset.Now;
                    NewParam.CreatedById = userID;
                    NewParam.ParamType = datasource.inputs[i].ParamType;
                    NewParam.Direction = datasource.inputs[i].Direction;
                    NewParam.SourceType = datasource.inputs[i].SourceType;
                    NewParam.ParamValue = datasource.inputs[i].ParamValue;
                    NewParam.DefaultValue = datasource.inputs[i].DefaultValue;
                    NewParam.Alias = datasource.inputs[i].Alias;
                    _maparParamsRepository.Add(NewParam);
                    await _maparParamsRepository.SaveChangesAsync();
                }

                for (int i = 0; i < datasource.outputs.Count; i++)
                {
                    ApiMapperParams NewParam = new ApiMapperParams();
                    NewParam.ApiMapperId = maperObj.Id;
                    NewParam.ParamName = datasource.outputs[i].Name;
                    NewParam.ParamOrder = 0;
                    NewParam.CreationDate = DateTimeOffset.Now; ;
                    NewParam.CreatedById = userID;
                    NewParam.Alias = datasource.outputs[i].Alias;
                    NewParam.ParamType = 0;
                    NewParam.Direction = ParameterDirection.Output;
                    NewParam.SourceType = SourceType.Output;
                    NewParam.ParamValue = string.Empty;
                    _maparParamsRepository.Add(NewParam);
                    await _maparParamsRepository.SaveChangesAsync();
                }
                foreach (var item in datasource.paths)
                {
                    _mapperDetailsRepository.Add(new ApiMapperDetails
                    {
                        ApiMapperId = maperObj.Id,
                        CreatedById = userID,
                        CreationDate = DateTimeOffset.Now,
                        ResponsePath = item
                    });
                    await _mapperDetailsRepository.SaveChangesAsync();
                }

                // the links posted with the datasource are created first, so the auto wiring below
                // (which skips the params that are already wired) cannot overwrite them.
                await AddDatasourceChildren(maperObj.Id, datasource.children);

                //if (datasource.apiMapperViewModel.ParentId.HasValue)
                //{
                //    await AutoWireParentChildParams(maperObj.Id, datasource.apiMapperViewModel.ParentId.Value);
                //}

                return maperObj.Id;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }


        /// <summary>
        /// Replaces the (child input parameter -&gt; parent API output parameter) links of the datasource with the
        /// ones posted in DatasourceVM.children: every link the API has now is deleted first, then the posted ones
        /// are created again - the same APIMapperParamChild rows the LinkChildParamToParent API writes one by one.
        /// A children list that is not sent at all (null) leaves the existing links untouched, an empty one clears them.
        /// </summary>
        private async Task AddDatasourceChildren(long apiMapperId, List<DatasourceChildParamVM> children)
        {
            try
            {
                if (children == null)
                    return;

                // the posted list is the whole set of links of this API, so what is stored now is dropped first.
                var currentLinks = await _mapperParamsChildRepository.Query()
                    .Where(x => x.ApiMapperId == apiMapperId)
                    .ToListAsync();

                foreach (var currentLink in currentLinks)
                {
                    _mapperParamsChildRepository.Remove(currentLink);
                }

                await _mapperParamsChildRepository.SaveChangesAsync();

                if (children.Count == 0)
                    return;

                foreach (var child in children)
                {
                    // ApiMapperParamChildId is the parent API output param the input depends on, without it
                    // there is nothing to resolve the value from.
                    if (!child.ApiMapperParamChildId.HasValue || child.ApiMapperParamChildId.Value == 0)
                        throw new Exception($"The child '{child.ParamName}' has no ApiMapperParamChildId (the parent API output param it depends on)");

					APIMapperParamChild link = new APIMapperParamChild
					{
                        // the link always belongs to the datasource being saved, on Add its Id is known here only.
                        ApiMapperId = apiMapperId,
						ParamName = child.ParamName,
						ApiMapperParamChildId = child.ApiMapperParamChildId.Value,
						ParentId = child.parentId,
						ExecutionOrder = child.executionOrder,
                        // optional path to read the value inside the parent response, see ResolveDependentApiValue
                        ResponsePath = string.IsNullOrWhiteSpace(child.ResponsePath) ? null : child.ResponsePath.Trim()
					};
					_mapperParamsChildRepository.Add(link);

				}

                await _mapperParamsChildRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        /// <summary>
        /// Removes the APIMapperParamChild rows that reference a param that is about to be deleted:
        /// - the links of any API that reads its value from this param (ApiMapperParamChildId), which is the
        ///   restricted foreign key that makes the delete fail;
        /// - for an input param, the link that describes the param itself (ApiMapperId + ParamName), otherwise a
        ///   row would be left behind pointing at a param that no longer exists and a later param with the same
        ///   name would pick it up.
        /// A child that depended on a deleted param loses its dependency, there is nothing left to read from.
        /// </summary>
        private async Task DeleteParamChildLinks(ApiMapperParams param)
        {
            if (param == null)
            {
                return;
            }

            var isInputParam = param.Direction != ParameterDirection.Output;

            var links = await _mapperParamsChildRepository.Query()
                .Where(x => x.ApiMapperParamChildId == param.Id
                         || (isInputParam && x.ApiMapperId == param.ApiMapperId && x.ParamName == param.ParamName))
                .ToListAsync();

            if (links.Count == 0)
            {
                return;
            }

            foreach (var link in links)
            {
                _mapperParamsChildRepository.Remove(link);
            }

            await _mapperParamsChildRepository.SaveChangesAsync();
        }

        private async Task AutoWireParentChildParams(long apiMapperId, long parentApiMapperId)
        {
            try
            {
                var childInputs = await _maparParamsRepository.Query()
                    .Where(x => x.ApiMapperId == apiMapperId && x.Direction == ParameterDirection.Input && x.IsDeleted == false)
                    .ToListAsync();

                var parentOutputs = await _maparParamsRepository.Query()
                    .Where(x => x.ApiMapperId == parentApiMapperId && x.Direction == ParameterDirection.Output)
                    .ToListAsync();

                var wiredParamNames = await _mapperParamsChildRepository.Query()
                    .Where(x => x.ApiMapperId == apiMapperId)
                    .Select(x => x.ParamName)
                    .ToListAsync();

                foreach (var childInput in childInputs)
                {
                    //already wired
                    if (wiredParamNames.Contains(childInput.ParamName))
                        continue;

                    var matchingParentOutput = parentOutputs.FirstOrDefault(x => x.ParamName == childInput.ParamName);
                    if (matchingParentOutput == null)
                        continue;

                    _mapperParamsChildRepository.Add(new APIMapperParamChild
                    {
                        ApiMapperId = apiMapperId,
                        ParamName = childInput.ParamName,
                        ApiMapperParamChildId = matchingParentOutput.Id,
                        ParentId = parentApiMapperId
                    });
                    wiredParamNames.Add(childInput.ParamName);
                }

                await _mapperParamsChildRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<long> EditApiMapper(DatasourceVM datasource, long siteId, long userID)
        {
            try
            {
                ApiMapper maperObj = await _mapperRepository.Query().Where(x => x.Id == datasource.apiMapperViewModel.Id).FirstOrDefaultAsync();
                maperObj.Name = datasource.apiMapperViewModel.Name;
                maperObj.Description = datasource.apiMapperViewModel.Description;
                maperObj.URL = datasource.apiMapperViewModel.URL;
                maperObj.HttpMethod = datasource.apiMapperViewModel.HttpMethod;
                maperObj.Status = datasource.apiMapperViewModel.Status;
                maperObj.MediaType = datasource.apiMapperViewModel.MediaTypes;
                maperObj.AuthorizationType = datasource.apiMapperViewModel.AuthorizationType;
                maperObj.SiteId = datasource.apiMapperViewModel.SiteId;
                maperObj.ModifiedById = userID;
                maperObj.Type = datasource.apiMapperViewModel.Type;
                maperObj.ModificationDate = DateTimeOffset.Now;
                maperObj.ApiReponseType = datasource.apiMapperViewModel.ApiReponseType;
                // ParentId is no longer kept on ApiMapper, the dependency is stored in APIMapperParamChild
                // by AutoWireParentChildParams below.
                maperObj.EnableLog = datasource.apiMapperViewModel.EnableLog;
                _mapperRepository.Update(maperObj);

                await changeAuthorizationType(datasource.apiMapperViewModel.AuthorizationKeys, datasource.apiMapperViewModel.Id, datasource.tokenSource);

                await _mapperRepository.SaveChangesAsync();

                var deletedParams = _maparParamsRepository.Query().Where(x => x.ApiMapperId == datasource.apiMapperViewModel.Id && x.Direction == ParameterDirection.Input && x.IsDeleted == false).Select(x => x.Id).ToList();

                for (int i = 0; i < datasource.inputs.Count; i++)
                {
                    deletedParams.Remove(datasource.inputs[i].Id);
                    if (datasource.inputs[i].Id != 0)
                    {
                        await EditApiMapperParams(datasource.inputs[i], userID);
                    }
                    else
                    {
                        ApiMapperParams NewParam = new ApiMapperParams();
                        NewParam.ApiMapperId = datasource.inputs[i].ApiMapperId;
                        NewParam.ParamName = datasource.inputs[i].ParamName;
                        NewParam.ParamOrder = datasource.inputs[i].ParamOrder;
                        NewParam.CreationDate = DateTimeOffset.Now; ;
                        NewParam.CreatedById = userID;
                        NewParam.ParamType = datasource.inputs[i].ParamType;
                        NewParam.Direction = datasource.inputs[i].Direction;
                        NewParam.SourceType = datasource.inputs[i].SourceType;
                        NewParam.ParamValue = datasource.inputs[i].ParamValue;
                        NewParam.DefaultValue = datasource.inputs[i].DefaultValue;
                        NewParam.Alias = datasource.inputs[i].Alias;
                        _maparParamsRepository.Add(NewParam);
                        await _maparParamsRepository.SaveChangesAsync();
                    }

                }
                if (deletedParams.Count > 0)
                {
                    foreach (var item in deletedParams)
                    {
                        await DeleteApiMapperParams(item, userID);
                    }
                }
                var CurrentInputs = await _maparParamsRepository.Query().Where(x => x.ApiMapperId == datasource.apiMapperViewModel.Id && x.Direction == ParameterDirection.Input).ToListAsync();
                for (int ii = 0; ii < CurrentInputs.Count; ii++)
                {
                    if (!isMatchedInputs(datasource.inputs, CurrentInputs[ii].ParamName))
                    {
                        // The APIMapperParamChild rows that reference this param go first: ApiMapperParamChildId
                        // is a restricted foreign key, so deleting the param while a link points at it fails.
                        await DeleteParamChildLinks(CurrentInputs[ii]);

                        _maparParamsRepository.Remove(CurrentInputs[ii]);
                        await _maparParamsRepository.SaveChangesAsync();
                    }
                }
                var apiDetailsObj = await _mapperDetailsRepository.Query().Where(x => x.ApiMapperId == maperObj.Id).ToListAsync();
                foreach (var item in apiDetailsObj)
                {
                    _mapperDetailsRepository.Remove(item);
                }
                await _mapperDetailsRepository.SaveChangesAsync();

                foreach (var item in datasource.paths)
                {
                    _mapperDetailsRepository.Add(new ApiMapperDetails
                    {
                        ApiMapperId = maperObj.Id,
                        CreatedById = userID,
                        CreationDate = DateTimeOffset.Now,
                        ResponsePath = item
                    });
                    await _mapperDetailsRepository.SaveChangesAsync();
                }



                var existOutputs = await _maparParamsRepository.Query().Where(x => x.ApiMapperId == datasource.apiMapperViewModel.Id && x.Direction == ParameterDirection.Output).ToListAsync();
                var matchedOutputs = getMatchedOutputs(existOutputs, datasource.outputs.Select(x => x.Name).ToList());

                for (int i = 0; i < existOutputs.Count; i++)
                {
                    if (!isMatchedOutput(matchedOutputs, existOutputs[i].ParamName))
                    {
                        // An output param is what the children of other APIs point at (ApiMapperParamChildId),
                        // so those links go first here too, for the same restricted foreign key.
                        await DeleteParamChildLinks(existOutputs[i]);

                        _maparParamsRepository.Remove(existOutputs[i]);

                    }
                    else
                    {
                        existOutputs[i].Alias = datasource.outputs.Where(x => x.Name == existOutputs[i].ParamName).Select(x => x.Alias).FirstOrDefault();
                    }
                }
                ;

                for (int i = 0; i < datasource.outputs.Count; i++)
                {
                    if (!isMatchedOutput(matchedOutputs, datasource.outputs[i].Name))
                    {
                        ApiMapperParams NewParam = new ApiMapperParams();
                        NewParam.ApiMapperId = maperObj.Id;
                        NewParam.ParamName = datasource.outputs[i].Name;
                        NewParam.ParamOrder = 0;
                        NewParam.CreationDate = DateTimeOffset.Now; ;
                        NewParam.CreatedById = userID;
                        NewParam.ParamType = 0;
                        NewParam.Direction = ParameterDirection.Output;
                        NewParam.SourceType = SourceType.Output;
                        NewParam.ParamValue = string.Empty;
                        NewParam.Alias = datasource.outputs[i].Alias;
                        _maparParamsRepository.Add(NewParam);
                        await _maparParamsRepository.SaveChangesAsync();

                    }

                }
                await _maparParamsRepository.SaveChangesAsync();

                // the links posted with the datasource are created (or re-pointed) first, so the auto wiring
                // below (which skips the params that are already wired) cannot overwrite them.
                await AddDatasourceChildren(maperObj.Id, datasource.children);

                //if (datasource.apiMapperViewModel.ParentId.HasValue)
                //{
                //    await AutoWireParentChildParams(maperObj.Id, datasource.apiMapperViewModel.ParentId.Value);
                //}

                return maperObj.Id;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<UserOptionsVM>> GetAuthenticationUserOptions(string propertyName, string refreshProperty)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                return await _UserOptionsRepository.Query().Where(x => x.UserId == userId && (x.PropertyName == propertyName || x.PropertyName == refreshProperty)).Select(x => new UserOptionsVM
                {
                    PropertyName = x.PropertyName,
                    ControlType = x.ControlType,
                    Value = x.Value
                }).ToListAsync();
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public List<ApiMapperParams> getMatchedOutputs(List<ApiMapperParams> existOutputs, List<string> oldOutputs)
        {
            var result = new List<ApiMapperParams>();

            for (int ii = 0; ii < existOutputs.Count; ii++)
                for (int i = 0; i < oldOutputs.Count; i++)
                    if (existOutputs[ii].ParamName == oldOutputs[i])
                        result.Add(existOutputs[ii]);

            return result;
        }


        public bool isMatchedOutput(List<ApiMapperParams> matchedOutputs, string output)
        {
            for (int i = 0; i < matchedOutputs.Count; i++)
                if (matchedOutputs[i].ParamName == output)
                    return true;

            return false;
        }

        public bool isMatchedInputs(List<ApiMapperParamsViewModel> Inputs, string ParamName)
        {
            for (int i = 0; i < Inputs.Count; i++)
                if (Inputs[i].ParamName == ParamName)
                    return true;

            return false;
        }

        private async Task changeAuthorizationType(List<ApiMapperAuthorization> authorizationKeys, long apiId, ETokenSource? tokenSource)
        {

            List<ApiMapperAuthorization> mapperAuthList = await _mapperAuthorizationRepository.Query()
                 .Where(x => x.ApiMapperId == apiId).ToListAsync<ApiMapperAuthorization>();

            foreach (var item in mapperAuthList)
            {
                _mapperAuthorizationRepository.Remove(item);
            }

            foreach (var auth in authorizationKeys)
            {
                _mapperAuthorizationRepository.Add(new ApiMapperAuthorization
                {
                    ApiMapperId = apiId,
                    Key = auth.Key,
                    Value = auth.Value,
                    TokenSource = tokenSource
                });
            }
            await _mapperAuthorizationRepository.SaveChangesAsync();
        }

        private HttpWebRequest addAuthorizationToHeader(ApiMapperViewModel ApiInfo, HttpWebRequest request, long userId, string token)
        {

            //append cookies

            var allCookies = _workContext.GetCookies();

            foreach (var cookie in allCookies)
            {
                request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
            }


            //  ----- Added By Isra'a ----- Adding Authorization to headers

            if (ApiInfo.AuthorizationType != ApiAuthorizationTypes.NoAuth)
            {
                List<ApiMapperAuthorization> credentials = _mapperAuthorizationRepository.Query().Where(x => x.ApiMapperId == ApiInfo.Id).ToList();
                string auth = string.Empty;


                switch (ApiInfo.AuthorizationType)
                {
                    case ApiAuthorizationTypes.BasicAuth:
                        {
                            var usernameObj = credentials.FirstOrDefault(x => x.Key.ToLower() == "username");
                            var passwordObj = credentials.FirstOrDefault(x => x.Key.ToLower() == "password");
                            if (usernameObj == null)
                            {
                                usernameObj = new ApiMapperAuthorization();
                                usernameObj.Value = "";
                            }
                            if (passwordObj == null)
                            {
                                passwordObj.Value = "";
                            }

                            NetworkCredential myNetworkCredential = new NetworkCredential(usernameObj.Value, passwordObj.Value);

                            CredentialCache myCredentialCache = new CredentialCache();
                            Uri ApiUri = new Uri(ApiInfo.URL);
                            myCredentialCache.Add(ApiUri, "Basic", myNetworkCredential);

                            request.PreAuthenticate = true;
                            request.Credentials = myCredentialCache;
                        }
                        break;

                    case ApiAuthorizationTypes.BearerToken:

                        {
                            switch (ApiInfo.TokenSource)
                            {
                                case ETokenSource.DataBase:
                                    var tokenNameInDB = credentials.FirstOrDefault(x => x.Key == "DatabaseToken");
                                    var userOption = _UserOptionsRepository.Query().FirstOrDefault(x => x.PropertyName == (tokenNameInDB.Value != null ? tokenNameInDB.Value : "") && x.UserId == userId);
                                    auth = userOption != null ? userOption.Value : string.Empty;
                                    break;
                                case ETokenSource.LocalStorage:
                                    auth = !string.IsNullOrEmpty(token) ? token : string.Empty;
                                    break;
                            }
                            request.Headers["Authorization"] = string.Format("Bearer {0}", token);
                        }
                        break;

                }
            }
            return request;
            //
        }
        private async Task<HttpRequestMessage> addAuthorizationToHeader(ApiMapperViewModel ApiInfo, HttpRequestMessage request, long userId, string token)
        {

            var allCookies = _workContext.GetCookies();

            foreach (var cookie in allCookies)
            {
                request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
            }
            //  ----- Added By Isra'a ----- Adding Authorization to headers

            if (ApiInfo.AuthorizationType != ApiAuthorizationTypes.NoAuth)
            {
                List<ApiMapperAuthorization> credentials = _mapperAuthorizationRepository.Query().Where(x => x.ApiMapperId == ApiInfo.Id).ToList();
                string auth = string.Empty;
                switch (ApiInfo.AuthorizationType)
                {
                    case ApiAuthorizationTypes.BasicAuth:
                        {
                            var usernameObj = credentials
                                .FirstOrDefault(x => x.Key.Equals("username", StringComparison.OrdinalIgnoreCase));

                            var passwordObj = credentials
                                .FirstOrDefault(x => x.Key.Equals("password", StringComparison.OrdinalIgnoreCase));

                            var username = usernameObj?.Value ?? string.Empty;
                            var password = passwordObj?.Value ?? string.Empty;

                            var rawCredentials = $"{username}:{password}";
                            var encodedCredentials = Convert.ToBase64String(
                                System.Text.Encoding.UTF8.GetBytes(rawCredentials)
                            );

                            request.Headers.Remove("Authorization");
                            request.Headers.Add("Authorization", $"Basic {encodedCredentials}");
                        }
                        break;

                    case ApiAuthorizationTypes.BearerToken:

                        {
                            switch (ApiInfo.TokenSource)
                            {
                                case ETokenSource.DataBase:
                                    var tokenNameInDB = credentials.FirstOrDefault(x => x.Key == "DatabaseToken");
                                    var userOption = _UserOptionsRepository.Query().FirstOrDefault(x => x.PropertyName == (tokenNameInDB.Value != null ? tokenNameInDB.Value : "") && x.UserId == userId);
                                    auth = userOption != null ? userOption.Value : string.Empty;
                                    break;
                                case ETokenSource.LocalStorage:
                                    if (string.IsNullOrEmpty(token) && ApiInfo.ParentId.HasValue)
                                    {
                                        var obj = new JObject();
                                        var Result = await HandleWebAPIRequests(ApiInfo.ParentId.Value, obj, userId);
                                        token = Result;
                                        auth = token;
                                    }
                                    else
                                    {
                                        auth = token;
                                    }
                                    break;
                            }
                            request.Headers.Add("Authorization", string.Format("Bearer {0}", token));
                        }
                        break;

                }
            }
            return request;

        }





        /////////////////////////////////////////// luay new api /////// end//////////


        public async Task<ApiMapperParams> AddApiMapperParams(ApiMapperParamsViewModel MapperParams, long userID)
        {
            try
            {
                ApiMapperParams NewParam = new ApiMapperParams();
                NewParam.ApiMapperId = MapperParams.ApiMapperId;
                NewParam.ParamName = MapperParams.ParamName;
                NewParam.ParamOrder = MapperParams.ParamOrder;
                NewParam.CreationDate = DateTimeOffset.Now; ;
                NewParam.CreatedById = userID;
                NewParam.ParamType = MapperParams.ParamType;
                NewParam.Direction = MapperParams.Direction;
                NewParam.SourceType = MapperParams.SourceType;
                NewParam.ParamValue = MapperParams.ParamValue;
                NewParam.Alias = MapperParams.Alias;


                _maparParamsRepository.Add(NewParam);
                await _maparParamsRepository.SaveChangesAsync();
                return NewParam;

            }
            catch (Exception exc)
            {
                throw exc;
            }

        }
        public async Task<ApiMapperParams> EditApiMapperParams(ApiMapperParamsViewModel MapperParams, long userID)
        {
            try
            {
                ApiMapperParams EditedParam = await _maparParamsRepository.Query().Where(x => x.Id == MapperParams.Id).FirstOrDefaultAsync();
                EditedParam.ApiMapperId = MapperParams.ApiMapperId;
                EditedParam.ParamName = MapperParams.ParamName;
                EditedParam.ParamOrder = MapperParams.ParamOrder;
                EditedParam.ModificationDate = DateTimeOffset.Now; ;
                EditedParam.ParamType = MapperParams.ParamType;
                EditedParam.CreatedById = userID;
                EditedParam.Direction = MapperParams.Direction;
                EditedParam.SourceType = MapperParams.SourceType;
                EditedParam.ParamValue = MapperParams.ParamValue;
                EditedParam.DefaultValue = MapperParams.DefaultValue;
                EditedParam.Alias = MapperParams.Alias;

                _maparParamsRepository.Update(EditedParam);
                await _maparParamsRepository.SaveChangesAsync();
                return EditedParam;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<ApiMapperParams> DeleteApiMapperParams(long paramId, long userID)
        {
            try
            {
                ApiMapperParams EditedParam = await _maparParamsRepository.Query().Where(x => x.Id == paramId).FirstOrDefaultAsync();
                EditedParam.ApiMapperId = EditedParam.ApiMapperId;
                EditedParam.ParamName = EditedParam.ParamName;
                EditedParam.ParamOrder = EditedParam.ParamOrder;
                EditedParam.ModificationDate = DateTimeOffset.Now; ;
                EditedParam.ParamType = EditedParam.ParamType;
                EditedParam.CreatedById = userID;
                EditedParam.Direction = EditedParam.Direction;
                EditedParam.SourceType = EditedParam.SourceType;
                EditedParam.ParamValue = EditedParam.ParamValue;
                EditedParam.DefaultValue = EditedParam.DefaultValue;
                EditedParam.Alias = EditedParam.Alias;
                EditedParam.IsDeleted = true;
                _maparParamsRepository.Update(EditedParam);
                await _maparParamsRepository.SaveChangesAsync();
                return EditedParam;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }
        public async Task<ApiMapperViewModel> GetApiMapperById(long Id)
        {
            try
            {
                return await _mapperRepository.Query().Include(x => x.ApiMapperAuthorizations).Where(x => x.Id == Id).Select(p => new ApiMapperViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    URL = p.URL,
                    ApiMapperParams = p.ApiMapperParams.Where(x => x.IsDeleted == false).ToList(),
                    HttpMethod = p.HttpMethod,
                    // Parent is no longer stored on ApiMapper, it comes from APIMapperParamChild.
                    // Still needed by addAuthorizationToHeader (BearerToken / LocalStorage fallback).
                    ParentId = p.ParentId,
                    Type = p.Type,
                    Status = p.Status,
                    AuthorizationType = p.AuthorizationType,
                    SiteId = p.SiteId,
                    MediaTypes = p.MediaType,
                    EnableLog = p.EnableLog,
                    ApiReponseType = p.ApiReponseType,
                    Path = _mapperDetailsRepository.Query().FirstOrDefault(x => x.ApiMapperId == p.Id).ResponsePath,
                    OutputPath = _mapperDetailsRepository.Query().Where(x => x.ApiMapperId == p.Id).Select(y => y.ResponsePath).ToList(),
                    AuthorizationKeys = p.ApiMapperAuthorizations,
                    TokenSource = p.ApiMapperAuthorizations.FirstOrDefault() != null ? p.ApiMapperAuthorizations.FirstOrDefault().TokenSource : null

                }).FirstOrDefaultAsync();
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<dynamic> GetMapperParamsByMapperId(long mapperId, long siteId, long userID, ParameterDirection direction)
        {
            try
            {
                List<ApiMapperParams> headerArray = new List<ApiMapperParams>();
                List<ApiMapperParams> bodyArray = new List<ApiMapperParams>();
                List<ApiMapperParams> QSArray = new List<ApiMapperParams>();
                List<ApiMapperParams> OutParamArray = new List<ApiMapperParams>();

                List<ApiMapperParams> mapperParams = await _maparParamsRepository.Query().Where(x => x.ApiMapperId == mapperId && x.Direction == direction)
                    .Select(p => new ApiMapperParams
                    {
                        Id = p.Id,
                        ApiMapperId = p.ApiMapperId,
                        ParamName = p.ParamName,
                        ParamOrder = p.ParamOrder,
                        ParentId = p.ParentId,
                        Direction = p.Direction,
                        CreatedById = p.CreatedById,
                        ParamType = p.ParamType,
                        CreationDate = p.CreationDate,
                        ModificationDate = p.ModificationDate,
                        Alias = p.Alias
                    }).ToListAsync();

                foreach (var item in mapperParams)
                {

                    if (item.ParamType == ParamsTypes.Header)
                    {
                        headerArray.Add(item);
                    }
                    else if (item.ParamType == ParamsTypes.Body)
                    {
                        bodyArray.Add(item);
                    }
                    else if (item.ParamType == ParamsTypes.QueryString)
                    {
                        QSArray.Add(item);
                    }
                    else if (item.ParamType == 0 && item.Direction == direction)
                    {
                        OutParamArray.Add(item);
                    }


                }

                dynamic Result = new ExpandoObject();
                Result.HeaderData = headerArray;
                Result.BodyData = bodyArray;
                Result.QSData = QSArray;
                Result.OutParamArray = OutParamArray;
                return Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<bool> DeletMapperParams(string paramName, long siteId, long userId)
        {
            ApiMapperParams paramObj = await _maparParamsRepository.Query().Where(x => x.ParamName == paramName).FirstOrDefaultAsync();
            _maparParamsRepository.Remove(paramObj);
            return true;
        }


		public async Task<bool> DeletMapperParamsChild(long id, long siteId, long userId)
		{
			var paramObj = await _mapperParamsChildRepository.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
			_mapperParamsChildRepository.Remove(paramObj);
			return true;
		}


		public bool DeleteDataSource(long Id)
        {
            try
            {
                ApiMapper mapperObj = _mapperRepository.Query().Where(x => x.Id == Id).FirstOrDefault();
                mapperObj.Status = EStatus.Deleted;
                _mapperRepository.Update(mapperObj);
                _mapperRepository.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }



        //////////////// New Invoker ///////////

        ///////////////////////////////   Israa New APIs /////////////////////////////////////////////

        public async Task<dynamic> HandleWebAPIRequests(long ApiMapperId, dynamic obj, long userId, string authToken = "", dynamic FormData = null)
        {
            EnsureDataSourceAllowed(ApiMapperId);

            try
            {

                string response = string.Empty;

                //Get API Data
                var apiInfo = await GetApiMapperById(ApiMapperId);


                if (apiInfo != null)
                {
                    // ParentApiMapperId -> Parent API response. The dependent (SourceType.DependOnOtherApi)
                    // parameters are now resolved while they are being built, inside HandlingParams /
                    // HandlingHeaderAndBodyParams, so every parent API is executed at most once per request.
                    var apiResponses = new Dictionary<long, JToken>();
                    var visited = new HashSet<long> { ApiMapperId };

                    var finalParam = await HandlingParams(apiInfo.ApiMapperParams, apiInfo.MediaTypes, obj, FormData?.Files, userId, authToken, apiResponses, visited);
                    if (!string.IsNullOrEmpty(finalParam.Path))
                    {
                        if (apiInfo.URL.EndsWith('/'))
                        {
                            finalParam.Path = finalParam.Path.TrimStart('/');
                        }
                        else if (!finalParam.Path.StartsWith('/'))
                        {
                            finalParam.Path = "/" + finalParam.Path;
                        }

                        apiInfo.URL = string.Concat(apiInfo.URL, finalParam.Path.Replace("//", "/"));
                    }

                    if (!string.IsNullOrEmpty(finalParam.QueryString))
                    {
                        apiInfo.URL = string.Concat(apiInfo.URL, finalParam.QueryString); apiInfo.URL = apiInfo.URL.Replace("??", "?");
                    }
                    var handler = new HttpClientHandler
                    {
                        UseCookies = true,
                        CookieContainer = new CookieContainer(),
                    };

                    using (var client = new HttpClient(handler))
                    {
                        var apiUri = new System.Uri(apiInfo.URL);
                        client.Timeout = TimeSpan.FromSeconds(120);
                        var sp = ServicePointManager.FindServicePoint(apiUri);
                        sp.ConnectionLeaseTimeout = 1200000;
                        sp.MaxIdleTime = 1200000;
                        sp.ConnectionLimit = 1200000;
                        ServicePointManager.DefaultConnectionLimit = 200;
                        if (!string.IsNullOrEmpty(apiInfo.MediaTypes) && apiInfo.MediaTypes.ToLower().Equals("multipart/form-data"))
                        {
                            string apiResponse = "";
                            var form = new MultipartFormDataContent();


                            if (finalParam.BodyObj != null)
                            {
                                foreach (var item in finalParam.BodyObj)
                                {
                                    try
                                    {
                                        form.Add(new StringContent(ToFormFieldValue(item.Value)), item.Key);

                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                            }

                            if (finalParam.FormDataFields != null)
                            {
                                foreach (var item in finalParam.FormDataFields)
                                {
                                    try
                                    {
                                        form.Add(new StringContent(ToFormFieldValue(item.Value)), item.Key);
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                            }

                            var request = new HttpRequestMessage()
                            {
                                RequestUri = apiUri,
                                Content = form,
                                Method = HttpMethod.Post,

                            };

                            request = await addAuthorizationToHeader(apiInfo, request, userId, authToken);

                            if (finalParam.HeaderObj != null)
                            {
                                foreach (var item in finalParam.HeaderObj)
                                {
                                    request.Headers.Add(item.Key, item.Value.ToString());
                                }

                            }



                            if (finalParam.FormDataObj != null)
                            {

                                var Files = finalParam.FormDataObj as MultipartFormDataContent ?? new MultipartFormDataContent();
                                foreach (var file in Files)
                                {
                                    form.Add(file);
                                }
                                using (var formDataResponse = await SendInvokerRequestAsync(client, request, apiInfo.URL, finalParam))
                                {

                                    if (formDataResponse.IsSuccessStatusCode)
                                    {
                                        var cookies = handler.CookieContainer.GetCookies(request.RequestUri);

                                        foreach (Cookie cookie in cookies)
                                        {

                                            await _workContext.AppendCookies(cookie.Name, cookie.Value);

                                        }

                                        apiResponse = await formDataResponse.Content.ReadAsStringAsync();
                                        if (apiInfo.EnableLog)
                                        {
                                            var HeadersObj = JsonConvert.SerializeObject(request.Headers.Select(x =>
                                                                                                                x.Key + ":" +
                                                                                                                (x.Value is IEnumerable<string> ? string.Join(",", x.Value) : x.Value.ToString()))
                                                                                                                                                 .ToList());
                                            var token = request.Headers.Where(x => x.Key == "Authorization").SelectMany(x => x.Value).FirstOrDefault();

                                            await AddMapperTransactionLog(apiInfo.Id, token, finalParam.BodyObj, apiResponse, HeadersObj);
                                        }
                                        return await HandleResponseDependOnType(apiResponse, apiInfo, formDataResponse);

                                    }
                                    else
                                    {
                                        apiResponse = await formDataResponse.Content.ReadAsStringAsync();
                                        apiResponse = !string.IsNullOrEmpty(apiResponse) ? apiResponse : formDataResponse.ReasonPhrase;
                                        if (apiInfo.EnableLog)
                                        {
                                            var HeadersObj = JsonConvert.SerializeObject(request.Headers.Select(x =>
                                                                           x.Key + ":" +
                                                                           (x.Value is IEnumerable<string> ? string.Join(",", x.Value) : x.Value.ToString()))
                                                                       .ToList());
                                            var token = request.Headers.Where(x => x.Key == "Authorization").SelectMany(x => x.Value).FirstOrDefault();

                                            await AddMapperTransactionLog(apiInfo.Id, token, finalParam.BodyObj, apiResponse, HeadersObj);
                                        }
                                        await _dcmsLogService.AddError("EskaCms.Core.ApiInvoker", formDataResponse.StatusCode.ToString(), " Remote server returned ( StatusCode: " + (int)formDataResponse.StatusCode + " Response:" + apiResponse + ")", $"Request: {Newtonsoft.Json.JsonConvert.SerializeObject(request)}, Response: {Newtonsoft.Json.JsonConvert.SerializeObject(formDataResponse)}", null);
                                        if (formDataResponse.StatusCode == HttpStatusCode.Unauthorized)
                                        {
                                            throw new Exception((int)formDataResponse.StatusCode + "-Invoker- Remote server returned ( StatusCode: " + (int)formDataResponse.StatusCode + " Response: " + apiResponse + ")");
                                        }
                                        else
                                        {
                                            throw new Exception("Remote server returned (" + apiResponse + ")");
                                        }
                                    }

                                }

                            }
                            else
                            {
                                using (var formDataResponse = await SendInvokerRequestAsync(client, request, apiInfo.URL, finalParam))
                                {

                                    if (formDataResponse.IsSuccessStatusCode)
                                    {
                                        var cookies = handler.CookieContainer.GetCookies(request.RequestUri);

                                        foreach (Cookie cookie in cookies)
                                        {
                                            await _workContext.AppendCookies(cookie.Name, cookie.Value);

                                        }
                                        apiResponse = await formDataResponse.Content.ReadAsStringAsync();

                                        if (apiInfo.EnableLog)
                                        {
                                            var HeadersObj = JsonConvert.SerializeObject(request.Headers.Select(x =>
                                                                                                               x.Key + ":" +
                                                                                                               (x.Value is IEnumerable<string> ? string.Join(",", x.Value) : x.Value.ToString()))
                                                                                                           .ToList());
                                            var token = request.Headers.Where(x => x.Key == "Authorization").SelectMany(x => x.Value).FirstOrDefault();

                                            await AddMapperTransactionLog(apiInfo.Id, token, finalParam.BodyObj, apiResponse, HeadersObj);
                                        }

                                    }
                                    else
                                    {
                                        apiResponse = await formDataResponse.Content.ReadAsStringAsync();
                                        apiResponse = !string.IsNullOrEmpty(apiResponse) ? apiResponse : formDataResponse.ReasonPhrase;
                                        await _dcmsLogService.AddError("EskaCms.Core.ApiInvoker", formDataResponse.StatusCode.ToString(), " Remote server returned ( StatusCode: " + (int)formDataResponse.StatusCode + " Response:" + apiResponse + ")", $"Request: {Newtonsoft.Json.JsonConvert.SerializeObject(request)}, Response: {Newtonsoft.Json.JsonConvert.SerializeObject(formDataResponse)}", null);

                                        if (apiInfo.EnableLog)
                                        {
                                            var HeadersObj = JsonConvert.SerializeObject(request.Headers.Select(x =>
                                                                                                                                                     x.Key + ":" +
                                                                                                                                                     (x.Value is IEnumerable<string> ? string.Join(",", x.Value) : x.Value.ToString()))
                                                                                                                                                 .ToList());
                                            var token = request.Headers.Where(x => x.Key == "Authorization").SelectMany(x => x.Value).FirstOrDefault();

                                            await AddMapperTransactionLog(apiInfo.Id, token, finalParam.BodyObj, apiResponse, HeadersObj);
                                        }
                                        if (formDataResponse.StatusCode == HttpStatusCode.Unauthorized)
                                        {
                                            throw new Exception((int)formDataResponse.StatusCode + "-Invoker- Remote server returned ( StatusCode: " + (int)formDataResponse.StatusCode + " Response: " + apiResponse + ")");
                                        }
                                        else
                                        {
                                            throw new Exception("Remote server returned (" + apiResponse + ")");
                                        }
                                    }


                                    return await HandleResponseDependOnType(apiResponse, apiInfo, formDataResponse);

                                }
                            }
                            //        finalParam.BodyObj;



                        }
                        else
                        {

                            var method = new HttpMethod(apiInfo.HttpMethod.ToString());
                            HttpRequestMessage request = new HttpRequestMessage()
                            {
                                Method = method,
                                RequestUri = apiUri
                            };

                            if (MethodAllowsBody(method))
                            {


								request.Content = new StringContent(
                                    JsonConvert.SerializeObject(finalParam.BodyArray == null ? finalParam.BodyObj : finalParam.BodyArray),
                                    System.Text.Encoding.UTF8,
                                    apiInfo.MediaTypes);
                            }

                            request = await addAuthorizationToHeader(apiInfo, request, userId, authToken);

                            if (finalParam.HeaderObj != null)
                            {
                                foreach (var item in finalParam.HeaderObj)
                                {
                                    AddHeaderToRequest(request, item.Key, item.Value);
                                }

                            }

                            HttpResponseMessage result = await SendInvokerRequestAsync(client, request, apiInfo.URL, finalParam);
                            if (result.IsSuccessStatusCode)
                            {
                                var cookies = handler.CookieContainer.GetCookies(request.RequestUri);

                                foreach (Cookie cookie in cookies)
                                {
                                    await _workContext.AppendCookies(cookie.Name, cookie.Value);

                                }

                                response = await result.Content.ReadAsStringAsync();
                                if (apiInfo.EnableLog)
                                {
                                    var HeadersObj = JsonConvert.SerializeObject(request.Headers.Select(x =>
                                                                           x.Key + ":" +
                                                                           (x.Value is IEnumerable<string> ? string.Join(",", x.Value) : x.Value.ToString()))
                                                                       .ToList());
                                    var token = request.Headers.Where(x => x.Key == "Authorization").SelectMany(x => x.Value).FirstOrDefault();

                                    await AddMapperTransactionLog(apiInfo.Id, token, finalParam.BodyObj, response, HeadersObj);
                                }
                                if (apiInfo.ApiReponseType == ApiReponseType.String && IsOnlyString(response))
                                {
                                    return response;
                                }
                                else
                                {
                                    try
                                    {

                                        return await HandleResponseDependOnType(response, apiInfo, result);
                                    }
                                    catch (Exception ex)
                                    {

                                        throw ex;
                                    }
                                }
                            }
                            else
                            {
                                //var test = result.ReasonPhrase;
                                var res = await result.Content.ReadAsStringAsync();
                                res = !string.IsNullOrEmpty(res) ? res : result.ReasonPhrase;
                                if (apiInfo.EnableLog)
                                {
                                    var HeadersObj = JsonConvert.SerializeObject(request.Headers.Select(x =>
                                                                             x.Key + ":" +
                                                                             (x.Value is IEnumerable<string> ? string.Join(",", x.Value) : x.Value.ToString()))
                                                                         .ToList());
                                    var token = request.Headers.Where(x => x.Key == "Authorization").SelectMany(x => x.Value).FirstOrDefault();

                                    await AddMapperTransactionLog(apiInfo.Id, token, finalParam.BodyObj, res, HeadersObj);
                                }
                                await _dcmsLogService.AddError("EskaCms.Core.ApiInvoker", result.StatusCode.ToString(), " Remote server returned ( StatusCode: " + (int)result.StatusCode + " Response:" + res + ")", $"Request: {Newtonsoft.Json.JsonConvert.SerializeObject(request)}, Response: {Newtonsoft.Json.JsonConvert.SerializeObject(result)}", null);
                                if (result.StatusCode == HttpStatusCode.Unauthorized)
                                {
                                    throw new Exception((int)result.StatusCode + "-Invoker- Remote server returned ( StatusCode: " + (int)result.StatusCode + " Response: " + res + ")");
                                }
                                else
                                {
                                    throw new Exception("Remote server returned (" + res + ")");
                                }
                            }
                        }
                    }

                }
                else
                {
                    throw new Exception("Data Source Not configuered!");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<dynamic> InvokeWithChildren(long ApiMapperId, JObject context, long userId, string authToken = "", dynamic FormData = null)
        {
            return await InvokeWithChildrenInternal(ApiMapperId, context, userId, authToken, FormData, new HashSet<long>());
        }


        private async Task<dynamic> InvokeWithChildrenInternal(long ApiMapperId, JObject context, long userId, string authToken, dynamic FormData, HashSet<long> visited)
        {
            if (!visited.Add(ApiMapperId))
                throw new Exception($"Circular reference detected while invoking ApiMapper {ApiMapperId}");

            // find every row where THIS api is the owner/target
            var childLinks = await _mapperParamsChildRepository.Query()
                .Where(x => x.ApiMapperId == ApiMapperId)
                .Include(x => x.ApiMapperParams)
                .OrderBy(x => x.Id)
                .ToListAsync();

            foreach (var link in childLinks)
            {
                var childApiMapperId = link.ApiMapperParams.ApiMapperId;

                // the child may itself have rows in APIMapperParamChild
                var childResponse = await InvokeWithChildrenInternal(childApiMapperId, context, userId, authToken, null, visited);
                var childToken = childResponse as JToken ?? JToken.FromObject(childResponse);
                context[link.ParamName] = childToken[link.ApiMapperParams.ParamName];
            }

            // now that context has every child's value folded in, call this API itself
            return await HandleWebAPIRequests(ApiMapperId, context, userId, authToken, FormData);
        }
        private async Task AddMapperTransactionLog(long mapperId, string token, dynamic Request, string Response, string Headers)
        {
            try
            {

                var TransactionLog = new ApiMapperTransactionLog
                {

                    CreationDate = DateTimeOffset.Now,
                    ApiMapperId = mapperId,
                    CreatedById = await _workContext.GetCurrentUserId(),
                    Response = JsonConvert.SerializeObject(Response),
                    Request = JsonConvert.SerializeObject(Request),
                    Token = token,
                    Headers = Headers

                };
                apiMapperTransactionLogRepository.Add(TransactionLog);
                await apiMapperTransactionLogRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private async Task<dynamic> HandleResponseDependOnType(string response, ApiMapperViewModel apiInfo, HttpResponseMessage HttpResult)
        {
            try
            {
                JToken token = null;
                var outParams = apiInfo.ApiMapperParams.Where(p => p.Direction == ParameterDirection.Output);


                switch (apiInfo.ApiReponseType)
                {
                    case ApiReponseType.Array:
                        return CreateArrayResult(response, outParams, apiInfo.OutputPath);

                    case ApiReponseType.Object:
                        token = JToken.Parse(response);
                        return CreateObjectResult(token, outParams, apiInfo.Path);

                    case ApiReponseType.String:
                        if (!string.IsNullOrEmpty(apiInfo.Path))
                        {
                            token = JToken.Parse(response);
                            var ResponseObj = token[apiInfo.Path];
                            string TargetKey = apiInfo.ApiMapperParams.FirstOrDefault(x => x.SourceType == SourceType.Output)?.ParamName;
                            return string.IsNullOrEmpty(TargetKey) ? ResponseObj : ResponseObj[TargetKey];
                        }
                        else
                        {
                            token = JToken.Parse(response);

                            return token;
                        }
                    case ApiReponseType.File:
                        {
                            var contentType = HttpResult.Content.Headers.ContentType?.MediaType
                                              ?? "application/octet-stream";

                            var fileName =
                                HttpResult.Content.Headers.ContentDisposition?.FileNameStar ??
                                HttpResult.Content.Headers.ContentDisposition?.FileName ??
                                "DownloadedFile";

                            fileName = fileName.Replace("\"", "");

                            var fileBytes = await HttpResult.Content.ReadAsByteArrayAsync();

                            return new InvokerFileResult(
                                fileName,
                                fileBytes,
                                contentType
                            );
                        }


                    case ApiReponseType.Number:
                    default:
                        token = JToken.Parse(response);
                        return token;

                }


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private bool IsOnlyString(dynamic response)
        {
            // this function check if the response is only string and not a stringified object.
            try
            {
                JToken.Parse(response);
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private async Task<HttpResponseMessage> SendInvokerRequestAsync(HttpClient client, HttpRequestMessage request, string url, APIMapperParamTypesVM finalParam)
        {
            try
            {
                return await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                var bodyFields = finalParam.BodyObj is IEnumerable<KeyValuePair<string, object>> bodyObj
                    ? string.Join(", ", bodyObj.Select(x => x.Key))
                    : string.Empty;

                var fileFields = finalParam.FormDataObj is MultipartFormDataContent formData
                    ? string.Join(", ", formData.Select(x =>
                    {
                        var name = x.Headers.ContentDisposition?.Name?.Trim('"') ?? string.Empty;
                        var fileName = x.Headers.ContentDisposition?.FileName?.Trim('"') ?? string.Empty;
                        return string.IsNullOrEmpty(fileName) ? name : $"{name}:{fileName}";
                    }))
                    : string.Empty;

                var details = ex.InnerException?.Message ?? ex.Message;
                await _dcmsLogService.AddError(
                    "EskaCms.Core.ApiInvoker",
                    "SendAsync",
                    $"Failed to send invoker request to {url}. Error: {details}",
                    $"Method: {request.Method}, BodyFields: {bodyFields}, FileFields: {fileFields}",
                    null);

                throw new Exception($"Failed to send invoker request to {url}. {details}", ex);
            }
        }

        private async Task<APIMapperParamTypesVM> HandlingParams(ICollection<ApiMapperParams> ParamsValues, string APIMediaType, dynamic Values = null, dynamic file = null,
            long userId = 0, string authToken = "", Dictionary<long, JToken> apiResponses = null, HashSet<long> visited = null)
        {
            APIMapperParamTypesVM apiMapperParamTypeVM = new APIMapperParamTypesVM();

            // ParentApiMapperId -> Parent API response (Step 2 of the DependOnOtherApi resolution).
            apiResponses = apiResponses ?? new Dictionary<long, JToken>();
            visited = visited ?? new HashSet<long>();

            if (ParamsValues != null)
            {
                ParamsValues = ParamsValues.OrderBy(x => x.ParamOrder).ToList();
                var headerObj = new ExpandoObject() as IDictionary<string, Object>;
                var bodyObj = new ExpandoObject() as IDictionary<string, Object>;
                var formDataFields = new ExpandoObject() as IDictionary<string, Object>;
                var formFile = new MultipartFormDataContent();
                var usedFiles = new HashSet<IFormFile>();
                List<Object> bodyArray = null;

                var formFileParamCount = ParamsValues.Count(x =>
                    !x.IsDeleted &&
                    x.Direction == ParameterDirection.Input &&
                    x.ParamType == ParamsTypes.FormFile);

                foreach (var item in ParamsValues)
                {
                    if (!item.IsDeleted && item.Direction == ParameterDirection.Input)
                    {
                        switch (item.ParamType)
                        {
                            case ParamsTypes.Header:
                                await HandlingHeaderAndBodyParams(item, headerObj, Values, userId, authToken, apiResponses, visited, ParamsValues);
                                break;
                            case ParamsTypes.Body:
                                await HandlingHeaderAndBodyParams(item, bodyObj, Values, userId, authToken, apiResponses, visited, ParamsValues);
                                break;
                            case ParamsTypes.Path:
                                apiMapperParamTypeVM.Path = await HandlingPathParams(apiMapperParamTypeVM.Path, item, Values, userId, authToken, apiResponses, visited, ParamsValues);
                                break;
                            case ParamsTypes.QueryString:
                                apiMapperParamTypeVM.QueryString = await HandlingQueryString(apiMapperParamTypeVM.QueryString, item, Values, userId, authToken, apiResponses, visited, ParamsValues);
                                break;
                            case ParamsTypes.BodyArray:
                                apiMapperParamTypeVM.BodyArray = await HandlingBodyArrayParam(item, Values, userId, authToken, apiResponses, visited, ParamsValues);
                                break;
                            case ParamsTypes.FormFile:
                                apiMapperParamTypeVM.FormDataObj = HandlingFormFile(item, formFile, file, formFileParamCount, usedFiles);
                                break;
                            case ParamsTypes.FormData:
                                await HandlingHeaderAndBodyParams(item, formDataFields, Values, userId, authToken, apiResponses, visited, ParamsValues);
                                break;
                        }

                    }
                }
                apiMapperParamTypeVM.HeaderObj = (ExpandoObject)headerObj;

                apiMapperParamTypeVM.BodyObj = (ExpandoObject)bodyObj;

                apiMapperParamTypeVM.FormDataFields = (ExpandoObject)formDataFields;

                apiMapperParamTypeVM.FormDataObj = formFile;
            }
            return apiMapperParamTypeVM;
        }

        private async Task<List<object>> HandlingBodyArrayParam(ApiMapperParams item, dynamic Values = null,
            long userId = 0, string authToken = "", Dictionary<long, JToken> apiResponses = null, HashSet<long> visited = null, ICollection<ApiMapperParams> allParams = null)
        {
            if (item.SourceType == SourceType.Control)
            {
                return Values != null ? JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(Values[item.ParamName])) : new List<object>();
            }
            else if (item.SourceType == SourceType.Static)
            {
                return JsonConvert.DeserializeObject<List<object>>(item.ParamValue);
            }
			else if (item.SourceType == SourceType.DependOnOtherApi)
			{
				// The value comes from another API. The dependency (and its parent) is described by
				// APIMapperParamChild - see ResolveDependentApiValue.
				object arrayValue = await ResolveDependentApiValue(item, Values, userId, authToken, apiResponses, visited, allParams);

				return ToBodyArray(arrayValue);
			}
			else if (item.SourceType == SourceType.ControlOrDefault)
            {
                if (Values != null && DoesPropertyExist(Values, item.ParamName))
                {
                    return Values != null ? JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(Values[item.ParamName])) : new List<object>();
                }
                else
                {
                    return JsonConvert.DeserializeObject<List<object>>(item.DefaultValue);
                }
            }
            else if (item.SourceType == SourceType.TokenClaim)
            {
                string Value = _workContext.GetTokenClaimValue(item.ParamValue);
                return JsonConvert.DeserializeObject<List<object>>(Value);
            }

            return null;
        }

        /// <summary>
        /// Turns a value resolved from a parent API into a BodyArray value: the parent output can already be
        /// an array, or a string holding the serialized array.
        /// </summary>
        private List<object> ToBodyArray(object value)
        {
            if (value == null)
            {
                return new List<object>();
            }

            var valueToken = value as JToken;
            if (valueToken != null)
            {
                if (valueToken.Type == JTokenType.Null)
                {
                    return new List<object>();
                }

                if (valueToken.Type != JTokenType.String)
                {
                    return valueToken.ToObject<List<object>>();
                }

                value = valueToken.ToObject<string>();
            }

            var serializedArray = value as string;
            if (serializedArray != null)
            {
                return string.IsNullOrWhiteSpace(serializedArray)
                    ? new List<object>()
                    : JsonConvert.DeserializeObject<List<object>>(serializedArray);
            }

            return JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(value));
        }


        private async Task<string> HandlingQueryString(string QueryString, ApiMapperParams item, dynamic Values = null,
            long userId = 0, string authToken = "", Dictionary<long, JToken> apiResponses = null, HashSet<long> visited = null, ICollection<ApiMapperParams> allParams = null)
        {
            string str = string.IsNullOrEmpty(QueryString) ? "?{0}={1}" : "&{0}={1}";
            if (item.SourceType == SourceType.Control)
            {
                string queryValue = "";
                try
                {
                    queryValue = Values != null && Values[item.ParamValue] != null ? Values[item.ParamValue] : null;
                }
                catch (Exception)
                {

                    queryValue = string.Empty;
                }
                if (queryValue != null)
                {
                    QueryString += string.Format(str, item.ParamName, string.IsNullOrEmpty(queryValue) ? "" : queryValue);

                }
            }
            else if (item.SourceType == SourceType.Static)
            {
                QueryString += string.Format(str, item.ParamName, string.IsNullOrEmpty(item.ParamValue) ? null : item.ParamValue);
            }
			else if (item.SourceType == SourceType.DependOnOtherApi)
			{
				var headerValue = Convert.ToString(await ResolveDependentApiValue(item, Values, userId, authToken, apiResponses, visited, allParams));

				QueryString += string.Format(str, item.ParamName, string.IsNullOrEmpty(headerValue) ? "" : headerValue);


			}
			else if (item.SourceType == SourceType.ControlOrDefault)
            {
                if (Values != null && DoesPropertyExist(Values, item.ParamValue))
                {
                    string queryValue = "";
                    try
                    {
                        queryValue = Values != null ? Values[item.ParamValue] : string.Empty;
                    }
                    catch (Exception)
                    {

                        queryValue = string.Empty;
                    }
                    QueryString += string.Format(str, item.ParamName, string.IsNullOrEmpty(queryValue) ? "" : queryValue);
                }
                else
                {
                    QueryString += string.Format(str, item.ParamName, string.IsNullOrEmpty(item.DefaultValue) ? "" : item.DefaultValue);
                }
            }
            else if (item.SourceType == SourceType.TokenClaim)
            {
                string Value = _workContext.GetTokenClaimValue(item.ParamValue);
                QueryString += string.Format(str, item.ParamName, string.IsNullOrEmpty(Value) ? null : Value);
            }
            return QueryString;
        }

        /// <summary>
        /// Single place that resolves a SourceType.DependOnOtherApi parameter (the logic that used to live
        /// in ResolveParentValues / HandleWebAPIRequests).
        /// ApiMapper has no Parent any more: the dependency is described by APIMapperParamChild, whose
        /// ApiMapperParamChildId points at the parent API OUTPUT parameter that feeds this parameter.
        /// Parent responses are cached per request in apiResponses, keyed by Parent Api Mapper Id.
        /// </summary>
        private async Task<object> ResolveDependentApiValue(ApiMapperParams item, dynamic Values, long userId, string authToken,
            Dictionary<long, JToken> apiResponses, HashSet<long> visited, ICollection<ApiMapperParams> allParams)
        {
            apiResponses = apiResponses ?? new Dictionary<long, JToken>();
            visited = visited ?? new HashSet<long>();

            // Step 1 - does this key have a parent in APIMapperParamChild?
            var dependency = await GetParamDependency(item);

            // No parent (or it points at the API itself) => keep the previous, non dependent behaviour:
            // the value is expected to be already present in the incoming context.
            if (dependency == null || dependency.Value.ParentApiMapperId == item.ApiMapperId)
                return Values != null ? Values[item.ParamName] : "";

            // Step 2 - cache check first: if this dependent API was already invoked during this request,
            // reuse the cached response instead of calling it again. Otherwise execute the parent API
            // (once) and keep its response in apiResponses[ParentApiMapperId].
            JToken parentToken;
            if (!apiResponses.TryGetValue(dependency.Value.ParentApiMapperId, out parentToken))
                parentToken = await GetParentApiResponse(dependency.Value.ParentApiMapperId, Values, userId, authToken, apiResponses, visited, allParams);

            // Step 3 - take the child value out of the parent response, at whatever level it sits.
            // The path stored on the link wins: it says exactly where to read the value, so it is taken
            // literally (no search by name) and nothing deeper can be picked up by accident.
            JToken valueToken = null;

            if (!string.IsNullOrWhiteSpace(dependency.Value.ResponsePath))
                valueToken = GetValueFromParentResponse(parentToken, dependency.Value.ResponsePath.Trim(), false);

            // No path, or a path that matches nothing: fall back to the parent output param name.
            // When that output has an alias, that is the name its response is keyed by, so it comes first.
            if (valueToken == null && !string.IsNullOrEmpty(dependency.Value.ParentParamAlias))
                valueToken = GetValueFromParentResponse(parentToken, dependency.Value.ParentParamAlias);

            if (valueToken == null)
                valueToken = GetValueFromParentResponse(parentToken, dependency.Value.ParentParamName);

            // a parent that returns a bare value (just a token, just an id) carries no name to match,
            // and this parameter is explicitly linked to it, so the response itself is the value.
            if (valueToken == null && parentToken is JValue)
                valueToken = parentToken;

            var value = valueToken?.ToObject<object>();

            if (item.ParamType == ParamsTypes.Header
                && string.Equals(item.ParamName, "Authorization", StringComparison.OrdinalIgnoreCase)
                && value is string tokenValue
                && !tokenValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                value = "Bearer " + tokenValue;
            }

            // Keep the shared context in sync, exactly like ResolveParentValues did, so anything else
            // reading the request context sees the resolved value too.
            JObject contextObj = ((object)Values) as JObject;
            if (contextObj != null)
            {
                contextObj[item.ParamName] = value != null ? JToken.FromObject(value) : null;
                return contextObj[item.ParamName];
            }

            return value;
        }

        /// <summary>
        /// Finds the APIMapperParamChild row that describes the parent dependency of the given input parameter,
        /// and returns the Parent Api Mapper Id together with the parent output parameter name and alias.
        /// The alias matters because a parent that declares output params returns them keyed by alias
        /// (see CreateObjectResult), so that is the name to look for in its response.
        /// Returns null when the parameter has no parent.
        /// </summary>
        private async Task<(long ParentApiMapperId, string ParentParamName, string ParentParamAlias, string ResponsePath)?> GetParamDependency(ApiMapperParams item)
        {
            // The link row is identified by its natural key (owning API + param name).
            // ParentId (when it is filled in) wins, otherwise the parent is the API that owns the linked output param.
            var link = await _mapperParamsChildRepository.Query()
                             .Where(x => x.ApiMapperId == item.ApiMapperId && x.ParamName == item.ParamName)
                             .OrderBy(x => x.ExecutionOrder).ThenBy(x => x.Id)
                             .Select(x => new
                             {
                                 ParentApiMapperId = x.ParentId ?? x.ApiMapperParams.ApiMapperId,
                                 ParentParamName = x.ApiMapperParams.ParamName,
                                 ParentParamAlias = x.ApiMapperParams.Alias,
                                 x.ResponsePath
                             })
                             .FirstOrDefaultAsync();

            if (link == null)
                return null;

            return (link.ParentApiMapperId, link.ParentParamName, link.ParentParamAlias, link.ResponsePath);
        }

        /// <summary>
        /// Reads one value out of a parent API response, which is not always the flat object the old code assumed:
        /// it can be wrapped in one or more levels ({ data: { customer: { id } } }), it can be an array of rows, or
        /// a bare value. The name is looked up in this order:
        /// 1. the property itself, when the response is already flat;
        /// 2. the name read as a path (data.customer.id, items[0].id, $.data.id), so a parent can declare the depth
        ///    in the output param name;
        /// 3. the shallowest property with that name anywhere in the response, arrays included, so a wrapped
        ///    response resolves without the API having to declare anything - skipped when searchNested is false,
        ///    which is how an explicit APIMapperParamChild.ResponsePath is read: literally.
        /// Returns null when nothing matches. A response that is a bare value carries no name, so it is handled
        /// by the caller that knows it asked for one single value.
        /// </summary>
        private static JToken GetValueFromParentResponse(JToken parentToken, string name, bool searchNested = true)
        {
            if (parentToken == null || string.IsNullOrEmpty(name))
                return null;

            // 1 - flat response: what a parent with declared output params returns.
            if (parentToken.Type == JTokenType.Object)
            {
                var directValue = parentToken[name];
                if (directValue != null)
                    return directValue;
            }

            // 2 - the name itself describes where the value is inside the response.
            if (name.IndexOf('.') >= 0 || name.IndexOf('[') >= 0 || name.IndexOf('$') >= 0)
            {
                try
                {
                    var valueByPath = parentToken.SelectToken(name);
                    if (valueByPath != null)
                        return valueByPath;
                }
                catch (Exception)
                {
                    // not a valid path, it is handled as a plain name below
                }
            }

            // 3 - the value is nested somewhere in the response.
            return searchNested ? FindShallowestProperty(parentToken, name) : null;
        }

        /// <summary>
        /// Shows what a path reads out of a parent API response: the value it resolves to and every path that
        /// response offers, so an APIMapperParamChild.ResponsePath can be picked and checked before it is saved.
        /// The value is read with GetValueFromParentResponse, the very function the invoker uses, so what this
        /// returns is what the child param will receive.
        /// </summary>
        public async Task<ResponsePathPreviewVM> PreviewResponsePath(ResponsePathPreviewRequest req, long userId, string authToken = "")
        {
            if (req == null)
                throw new Exception("No request");

            JToken parentToken;

            if (!string.IsNullOrWhiteSpace(req.RawResponse))
            {
                // a response at hand: the path is tried against it without calling anything
                try
                {
                    parentToken = JToken.Parse(req.RawResponse);
                }
                catch (Exception exc)
                {
                    throw new Exception($"RawResponse is not valid JSON. {exc.Message}");
                }
            }
            else
            {
                if (req.ApiMapperId == 0)
                    throw new Exception("Either ApiMapperId (the parent API to read) or RawResponse is required");

                // the parent runs through the normal pipeline, so its own response shaping applies and what is
                // searched here is exactly what a child link searches at runtime.
                var parentResponse = await HandleWebAPIRequests(req.ApiMapperId, req.obj ?? new JObject(), userId, authToken);
                parentToken = parentResponse as JToken ?? (parentResponse == null ? JValue.CreateNull() : JToken.FromObject(parentResponse));
            }

            var result = new ResponsePathPreviewVM
            {
                ApiMapperId = req.ApiMapperId,
                ResponsePath = req.ResponsePath,
                Response = parentToken
            };

            const int maxPaths = 500;
            bool truncated;
            result.Paths = GetResponsePaths(parentToken, maxPaths, out truncated);
            result.PathsTruncated = truncated;

            if (!string.IsNullOrWhiteSpace(req.ResponsePath))
            {
                var valueToken = GetValueFromParentResponse(parentToken, req.ResponsePath.Trim(), false);

                result.Matched = valueToken != null;
                result.ValueType = valueToken == null ? null : valueToken.Type.ToString();
                result.Value = ShortenPathValue(valueToken);
            }

            return result;
        }

        /// <summary>
        /// Every path a response offers, in document order: the containers too (an object or an array is the path
        /// to use for a param that takes a whole object or a body array), each with the value it reads.
        /// </summary>
        private static List<ResponsePathVM> GetResponsePaths(JToken token, int maxPaths, out bool truncated)
        {
            var paths = new List<ResponsePathVM>();
            truncated = false;

            var container = token as JContainer;
            if (container == null)
                return paths;

            foreach (var descendant in container.DescendantsAndSelf())
            {
                // JProperty carries no value of its own, its value is visited right after it
                if (descendant is JProperty || string.IsNullOrEmpty(descendant.Path))
                    continue;

                if (paths.Count >= maxPaths)
                {
                    truncated = true;
                    break;
                }

                paths.Add(new ResponsePathVM
                {
                    Path = descendant.Path,
                    Type = descendant.Type.ToString(),
                    Value = descendant is JContainer ? null : ShortenPathValue(descendant)
                });
            }

            return paths;
        }

        private static string ShortenPathValue(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            var value = token is JContainer ? token.ToString(Formatting.None) : Convert.ToString(token);

            if (!string.IsNullOrEmpty(value) && value.Length > 200)
                value = value.Substring(0, 200) + "...";

            return value;
        }

        /// <summary>
        /// Breadth first search for the property with the given name that sits closest to the root of the response,
        /// so a wrapper level never hides the value and a deeper namesake never wins over the intended one.
        /// Arrays are searched through their items, the name is matched ignoring case.
        /// </summary>
        private static JToken FindShallowestProperty(JToken token, string name)
        {
            var pending = new Queue<JToken>();
            pending.Enqueue(token);

            while (pending.Count > 0)
            {
                var current = pending.Dequeue();

                var currentObject = current as JObject;
                if (currentObject != null)
                {
                    // the whole level is checked before going deeper
                    foreach (var property in currentObject.Properties())
                    {
                        if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                            return property.Value;
                    }

                    foreach (var property in currentObject.Properties())
                    {
                        if (property.Value != null && (property.Value.Type == JTokenType.Object || property.Value.Type == JTokenType.Array))
                            pending.Enqueue(property.Value);
                    }

                    continue;
                }

                var currentArray = current as JArray;
                if (currentArray != null)
                {
                    foreach (var arrayItem in currentArray)
                    {
                        if (arrayItem.Type == JTokenType.Object || arrayItem.Type == JTokenType.Array)
                            pending.Enqueue(arrayItem);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Names of the parameters of the given API that have a row in APIMapperParamChild, i.e. the ones
        /// whose value comes from a parent API output instead of the incoming request context.
        /// </summary>
        private async Task<HashSet<string>> GetLinkedParamNames(ICollection<ApiMapperParams> allParams)
        {
            var apiMapperId = allParams.Select(p => p.ApiMapperId).FirstOrDefault();

            var names = await _mapperParamsChildRepository.Query()
                .Where(x => x.ApiMapperId == apiMapperId)
                .Select(x => x.ParamName)
                .ToListAsync();

            return new HashSet<string>(names);
        }

        /// <summary>
        /// Executes a parent API at most once per request and caches the response by Parent Api Mapper Id.
        /// The parent runs through the normal pipeline, so a parent that itself depends on another API
        /// (A -&gt; B -&gt; C) is resolved recursively, and a failing parent keeps throwing exactly like before.
        /// </summary>
        private async Task<JToken> GetParentApiResponse(long parentApiMapperId, dynamic Values, long userId, string authToken,
            Dictionary<long, JToken> apiResponses, HashSet<long> visited, ICollection<ApiMapperParams> allParams)
        {
            JToken cached;
            if (apiResponses.TryGetValue(parentApiMapperId, out cached))
                return cached;

            if (!visited.Add(parentApiMapperId))
                throw new Exception($"Circular ParentId chain detected at ApiMapper {parentApiMapperId}");

            JObject contextObj = ((object)Values) as JObject;

            var parentResponse = await HandleWebAPIRequests(parentApiMapperId, contextObj ?? new JObject(), userId, authToken);
            var parentToken = parentResponse as JToken ?? JToken.FromObject(parentResponse);

            apiResponses[parentApiMapperId] = parentToken;

            // Same fallback ResolveParentValues had: input params that are not linked to a parent output
            // pick up a value of the same name from the parent response when the context has none.
            if (contextObj != null && allParams != null)
            {
                var linkedParamNames = await GetLinkedParamNames(allParams);

                foreach (var param in allParams.Where(p => p.Direction == ParameterDirection.Input && !linkedParamNames.Contains(p.ParamName)))
                {
                    if (contextObj[param.ParamName] == null)
                    {
                        var fallback = GetValueFromParentResponse(parentToken, param.ParamName);
                        if (fallback != null)
                            contextObj[param.ParamName] = fallback;
                    }
                }
            }

            return parentToken;
        }

        private async Task HandlingHeaderAndBodyParams(ApiMapperParams item, IDictionary<string, Object> obj, dynamic Values = null,
            long userId = 0, string authToken = "", Dictionary<long, JToken> apiResponses = null, HashSet<long> visited = null, ICollection<ApiMapperParams> allParams = null)
        {
            if (item.SourceType == SourceType.Control)
            {
                var headerValue = Values != null ? Values[item.ParamValue] : "";
                obj.Add(item.ParamName, headerValue);
            }
			else if (item.SourceType == SourceType.DependOnOtherApi)
			{
				// The value comes from another API. The dependency (and its parent) is described by
				// APIMapperParamChild - see ResolveDependentApiValue.
				var headerValue = await ResolveDependentApiValue(item, Values, userId, authToken, apiResponses, visited, allParams);
				obj.Add(item.ParamName, headerValue);
			}

			else if (item.SourceType == SourceType.Static)
            {
                obj.Add(item.ParamName, item.ParamValue);
            }
            else if (item.SourceType == SourceType.ControlOrDefault)
            {
                if (Values != null && DoesPropertyExist(Values, item.ParamValue))
                {
                    var headerValue = Values != null ? Values[item.ParamValue] : "";
                    obj.Add(item.ParamName, headerValue);
                }
                else
                {
                    obj.Add(item.ParamName, item.DefaultValue);
                }
            }
            else if (item.SourceType == SourceType.TokenClaim)
            {
                string Value = _workContext.GetTokenClaimValue(item.ParamValue);
                obj.Add(item.ParamName, Value);
            }

        }
        // This Method Handles The Routing Path Param
        private async Task<string> HandlingPathParams(string Path, ApiMapperParams item, dynamic Values = null,
            long userId = 0, string authToken = "", Dictionary<long, JToken> apiResponses = null, HashSet<long> visited = null, ICollection<ApiMapperParams> allParams = null)
        {
            string pathValue = "/";
            if (item.SourceType == SourceType.Control) { try { pathValue += Values != null ? Values[item.ParamName] : string.Empty; } catch (Exception) { pathValue = string.Empty; } }
            else if (item.SourceType == SourceType.Static) { pathValue += item.ParamValue; }
			else if (item.SourceType == SourceType.DependOnOtherApi)
			{
				var headerValue = await ResolveDependentApiValue(item, Values, userId, authToken, apiResponses, visited, allParams);
				pathValue += headerValue;
			}
			else if (item.SourceType == SourceType.ControlOrDefault)
            {
                if (Values != null && DoesPropertyExist(Values, item.ParamName))
                {
                    try { pathValue += Values != null ? Values[item.ParamName] : string.Empty; } catch (Exception) { pathValue = string.Empty; }
                }
                else
                {
                    pathValue += item.DefaultValue;
                }
            }
            else if (item.SourceType == SourceType.TokenClaim)
            {
                string Value = _workContext.GetTokenClaimValue(item.ParamValue);
                pathValue += Value;
            }
            return Path + pathValue;
        }
        private MultipartFormDataContent HandlingFormFile(ApiMapperParams item, MultipartFormDataContent formData, dynamic files = null, int formFileParamCount = 0, HashSet<IFormFile> usedFiles = null)
        {
            IFormFile file = ResolveMappedFormFile(files, item, formFileParamCount, usedFiles);
            if (file != null)
            {
                var memoryStream = new MemoryStream();
                file.CopyTo(memoryStream);
                var content = new ByteArrayContent(memoryStream.ToArray());
                content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
                formData.Add(content, item.ParamName, file.FileName);

            }
            return formData;
        }

        private IFormFile ResolveMappedFormFile(dynamic files, ApiMapperParams item, int formFileParamCount, HashSet<IFormFile> usedFiles = null)
        {
            if (files == null)
            {
                return null;
            }

            if (files is IFormFileCollection fileCollection)
            {
                var file = GetFileByMappedName(fileCollection, item);
                if (file != null)
                {
                    usedFiles?.Add(file);
                    return file;
                }

                // Positional fallback: the next upload not already consumed by another param,
                // so multiple unnamed FormFile params don't all grab the same file.
                var next = fileCollection.FirstOrDefault(f => usedFiles == null || !usedFiles.Contains(f));
                if (next != null && (formFileParamCount == 1 || usedFiles != null))
                {
                    usedFiles?.Add(next);
                    return next;
                }

                return null;
            }

            if (files is IEnumerable<IFormFile> fileList)
            {
                var filesArray = fileList.ToArray();
                var mappedNames = GetMappedFileNames(item).ToArray();
                var file = filesArray.FirstOrDefault(x => mappedNames.Any(name => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));
                if (file != null)
                {
                    usedFiles?.Add(file);
                    return file;
                }

                var next = filesArray.FirstOrDefault(f => usedFiles == null || !usedFiles.Contains(f));
                if (next != null && (formFileParamCount == 1 || usedFiles != null))
                {
                    usedFiles?.Add(next);
                    return next;
                }

                return null;
            }

            return null;
        }

        private IFormFile GetFileByMappedName(IFormFileCollection files, ApiMapperParams item)
        {
            foreach (var name in GetMappedFileNames(item))
            {
                var file = files.GetFile(name);
                if (file != null)
                {
                    return file;
                }
            }

            return null;
        }

        private IEnumerable<string> GetMappedFileNames(ApiMapperParams item)
        {
            if (!string.IsNullOrWhiteSpace(item.ParamName))
            {
                yield return item.ParamName;
            }

            if (!string.IsNullOrWhiteSpace(item.ParamValue))
            {
                yield return item.ParamValue;
            }

            if (!string.IsNullOrWhiteSpace(item.Alias))
            {
                yield return item.Alias;
            }
        }

        private bool DoesPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return settings[name] != null;
        }

        public async Task<PaginationResult<GetApiMapperTransactionLogVM>> GetApiMapperTransactionHistory(SearchApiMapperTransactionHistory model)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var result = await apiMapperTransactionLogRepository.Query()
                                            .Include(x => x.ApiMapper)
                                            .Where(x => x.ApiMapper.SiteId == siteId)
                                            .Where(x => model.ApiMapperId == 0 || model.ApiMapperId == null || x.Id == model.ApiMapperId)
                                            .Include(x => x.CreatedBy)
                                            .Where(x => string.IsNullOrEmpty(model.SearchText) ||x.CreatedBy.FullName.Contains(model.SearchText) || x.ApiMapper.Name.Contains(model.SearchText))
                                            .Where(x => model.FromDate == null || x.CreationDate >= model.FromDate)
                                            .Where(x => model.ToDate == null || x.CreationDate <= model.ToDate)
                                            .Select(x => new GetApiMapperTransactionLogVM
                                            {
                                                ApiMapperId = x.ApiMapperId,
                                                ApiMapperName = x.ApiMapper.Name,
                                                //history
                                                CreationDate = x.CreationDate,
                                                CreatedById = x.CreatedById,
                                                FullName = x.CreatedBy.FullName,
                                                Headers = x.Headers,
                                                PhoneNumber = x.CreatedBy.PhoneNumber,
                                                Request = x.Request,
                                                Response = x.Response,
                                                Token = x.Token,
                                                UserName = x.CreatedBy.UserName
                                                //history
                                            }).PaginateAsync(model.PageSize, model.PageNumber);

                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        // Serializes a body / form-data field value for a multipart request: scalars pass
        // through as their string, while objects and arrays are emitted as JSON instead of
        // the CLR type name that a bare .ToString() would produce for non-scalar values.
        private static string ToFormFieldValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Null:
                        return string.Empty;
                    case JTokenType.Object:
                    case JTokenType.Array:
                        return token.ToString(Formatting.None);
                    default:
                        return token.ToString();
                }
            }

            if (value is string || value is decimal || value is DateTime || value is Guid || value.GetType().IsPrimitive)
            {
                return value.ToString();
            }

            return JsonConvert.SerializeObject(value);
        }
        public async Task<APIMapperParamChild> LinkChildParamToParent(LinkChildParamToParentRequest req, long userID)
        {
            var childInput = await _maparParamsRepository.Query().FirstOrDefaultAsync(x => x.Id == req.childParamId);
            var parentOutput = await _maparParamsRepository.Query().FirstOrDefaultAsync(x => x.Id == req.parentOutputParamId);

            if (childInput == null || parentOutput == null)
                throw new Exception("Invalid param reference");

            return await LinkChildParamToParent(childInput, parentOutput, null);
        }

        /// <summary>
        /// Creates (or re-points) the link that makes a child input parameter take its value from a parent API
        /// output parameter. Shared by the LinkChildParamToParent API and by AddApiMapper, which receives the
        /// links in DatasourceVM.children.
        /// </summary>
        private async Task<APIMapperParamChild> LinkChildParamToParent(ApiMapperParams childInput, ApiMapperParams parentOutput, long? executionOrder)
        {
            // A parameter has at most one link row, identified by (ApiMapperId, ParamName): re-linking the
            // same parameter re-points the existing row instead of leaving a stale one behind.
            var link = await _mapperParamsChildRepository.Query()
                .FirstOrDefaultAsync(x => x.ApiMapperId == childInput.ApiMapperId && x.ParamName == childInput.ParamName);

            if (link == null)
            {
                link = new APIMapperParamChild
                {
                    ApiMapperId = childInput.ApiMapperId,
                    ParamName = childInput.ParamName,
                    ApiMapperParamChildId = parentOutput.Id,
                    ParentId = parentOutput.ApiMapperId,
                    ExecutionOrder = executionOrder ?? 0
                };
                _mapperParamsChildRepository.Add(link);
            }
            else
            {
                link.ApiMapperParamChildId = parentOutput.Id;
                link.ParentId = parentOutput.ApiMapperId;
                if (executionOrder.HasValue)
                {
                    link.ExecutionOrder = executionOrder.Value;
                }
                _mapperParamsChildRepository.Update(link);
            }

            await _mapperParamsChildRepository.SaveChangesAsync();
            return link;
        }
        public static bool ShouldAddToContentHeaders(string headerName)
        {
            var invalidContentHeaders = new List<string>()
            {
                "host",
                "authorization",
                "user-agent",
                "accept",
                "content-length",
                "cache-control",
                "connection",
                "cookie",
                "referer",
                "accept-encoding",
                "if-modified-since"
            };

            return !invalidContentHeaders.Contains(headerName.ToLower());
        }

        private static bool MethodAllowsBody(HttpMethod method)
        {
            return method != HttpMethod.Get && method != HttpMethod.Head;
        }

        /// <summary>
        /// Same rule for the HttpWebRequest path, where the method is a plain string: GET and HEAD carry no
        /// body, and asking an HttpWebRequest for its request stream on those throws.
        /// </summary>
        private static bool MethodAllowsBody(string httpMethod)
        {
            return !string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(httpMethod, "HEAD", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddHeaderToRequest(HttpRequestMessage request, string key, object value)
        {
            var headerValue = value == null ? string.Empty : value.ToString();

            if (request.Headers.TryAddWithoutValidation(key, headerValue))
            {
                return;
            }

            if (request.Content != null)
            {
                request.Content.Headers.TryAddWithoutValidation(key, headerValue);
            }
        }
    }


}