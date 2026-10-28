
//using EskaCMS.Core.BusinessModels;
//using EskaCMS.Entities.Entity;
//using EskaCMS.Core.Extensions;
//using EskaCMS.Core.Models;
//using EskaCMS.Infrastructure.Data;
//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Dynamic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using System.Web;
//using static EskaCMS.Entities.Enum.GeneralEnums;
//using static EskaCMS.Core.Services.WebServiceClient;

//namespace EskaCMS.Core.Services
//{
//    public class WebServiceClient
//    {
//        #region Delegates
//        public delegate string DelegateInvokeService();
//        #endregion

//        #region Enumerators
//        public enum ServiceType
//        {
//            Traditional = 0,
//            WCF = 1
//        }
//        #endregion

//        #region Classes
//        public class Parameter
//        {
//            public string Name { get; set; }
//            public string Value { get; set; }
//        }
//        #endregion

//        #region Member Variables
//        string _soapEnvelope =
//                @"<soap:Envelope
//                    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
//                    xmlns:xsd='http://www.w3.org/2001/XMLSchema'
//                    xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
//                <soap:Body></soap:Body></soap:Envelope>";


//        #endregion

//        #region Properties
//        public string Url { get; set; }

//        public string WebMethod { get; set; }

//        public IDictionary<string, object> Parameters { get; set; }

//        public ServiceType WSServiceType { get; set; }

//        public string WCFContractName { get; set; }
//        public string Accept { get; set; }
//        public string ContentType { get; set; }

//        public ApihttpMethods HttpMethod { get; set; }
//        #endregion

//        #region Private Methods
//        private string CreateSoapEnvelope()
//        {
//            string MethodCall = "<" + this.WebMethod + @" xmlns=""http://tempuri.org/"">";
//            string StrParameters = string.Empty;


//            //foreach (KeyValuePair<string, object> kvp in this.Parameters)
//            //    Console.WriteLine("Key: {0}, Value: {1}", kvp.Key, kvp.Value);
//            foreach (KeyValuePair<string, object> param in this.Parameters)
//            {
//                StrParameters = StrParameters + "<" + param.Key + ">" + param.Value + "</" + param.Key + ">";
//            }

//            MethodCall = MethodCall + StrParameters + "</" + this.WebMethod + ">";

//            StringBuilder sb = new StringBuilder(_soapEnvelope);
//            sb.Insert(sb.ToString().IndexOf("</soap:Body>"), MethodCall);

//            return sb.ToString();
//        }

//        private HttpWebRequest CreateWebRequest()
//        {
//            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(this.Url);
//            if (this.WSServiceType == WebServiceClient.ServiceType.WCF)
//                webRequest.Headers.Add("SOAPAction", "\"http://tempuri.org/" + this.WCFContractName + "/" + this.WebMethod + "\"");
//            else
//                webRequest.Headers.Add("SOAPAction", "\"http://tempuri.org/" + this.WebMethod + "\"");

//            webRequest.Headers.Add("To", this.Url);

//            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
//            webRequest.Accept = "text/xml; charset=utf-8";
//            webRequest.Method = "POST";
//            return webRequest;
//        }

//        private string StripResponse(string SoapResponse)
//        {
//            string RegexExtract = @"<" + this.WebMethod + "Result>(?<Result>.*?)</" + this.WebMethod + "Result>";

//            return Regex.Match(SoapResponse, RegexExtract).Groups["Result"].Captures[0].Value;
//        }
//        #endregion

//        #region Public Methods
//        //public void BeginInvokeService(AsyncCallback InvokeCompleted)
//        //{
//        //    DelegateInvokeService Invoke = new DelegateInvokeService(this.InvokeService);

//        //    IAsyncResult result = Invoke.BeginInvoke(InvokeCompleted, null);
//        //}

//        //public string EndInvokeService(IAsyncResult result)
//        //{
//        //    var asyncResult = (AsyncResult)result;
//        //    ReturnMessage msg = (ReturnMessage)asyncResult.GetReplyMessage();

//        //    return msg.ReturnValue.ToString();
//        //}

//        public string InvokeService()
//        {
//            WebResponse response = null;
//            string strResponse = "";

//            //Create the request
//            HttpWebRequest req = this.CreateWebRequest();

//            //write the soap envelope to request stream
//            using (Stream stm = req.GetRequestStream())
//            {
//                using (StreamWriter stmw = new StreamWriter(stm))
//                {
//                    stmw.Write(this.CreateSoapEnvelope());
//                }
//            }

//            //get the response from the web service
//            response = req.GetResponse();

//            Stream str = response.GetResponseStream();

//            StreamReader sr = new StreamReader(str);

//            strResponse = sr.ReadToEnd();

//            return this.StripResponse(HttpUtility.HtmlDecode(strResponse));
//        }
//        #endregion


//    }
//    public static class JObjectExtensions
//    {
//        public static IDictionary<string, string> ToDictionary(this JObject @object)
//        {
//            Dictionary<string, string> lastObject = new Dictionary<string, string>();
//            var inParams = JsonConvert.SerializeObject(@object["inputType"]);
//            var inputParams = JsonConvert.DeserializeObject<List<string>>(inParams);
//            foreach (var item in inputParams)
//            {
//                var result = GetValueByKey(@object, item);
//                try
//                {
//                    var jresult = result.ToObject<JObject>();
//                    result = GetValueByKey(result.ToObject<JObject>(), item);
//                    var jobject = result.ToObject<string>();
//                    lastObject.Add(item, jobject);
//                }
//                catch
//                {
//                    var jobject = result.ToObject<string>();
//                    if (jobject != null)
//                        lastObject.Add(item, jobject);
//                }




//            }
//            //var JObjectKeys = (from r in lastObject
//            //                   let key = r.Key
//            //                   let value = r.Value
//            //                   where value.GetType() == typeof(JObject)
//            //                   select key).ToList();

//            //var JArrayKeys = (from r in lastObject
//            //                  let key = r.Key
//            //                  let value = r.ValueGetDataSourceListBySiteId
//            //                  where value.GetType() == typeof(JArray)
//            //                  select key).ToList();

//            //JArrayKeys.ForEach(key => result[key] = ((JArray)result[key]).Values().Select(x => ((JValue)x).Value).ToArray());
//            //JObjectKeys.ForEach(key => lastObject[key] = ToDictionary(lastObject[key] as JObject));

//            return lastObject;
//        }

//        private static JToken GetValueByKey(JObject jObject, string key)
//        {
//            foreach (KeyValuePair<string, JToken> jProperty in jObject)
//            {
//                if (jProperty.Key.Equals(key) && jProperty.Value != null)
//                {
//                    return jProperty.Value;
//                }
//                else if (jProperty.Value is JObject)
//                {
//                    return GetValueByKey((JObject)jProperty.Value, key);
//                }
//            }
//            return null;
//        }

//    }


//    public class ApiMapperService : IApiMapper
//    {
//        private readonly IRepository<ApiMapper> _mapperRepository;
//        private readonly IRepository<ApiMapperParams> _maparParamsRepository;
//        private readonly IRepository<ApiMapperDetails> _mapperDetailsRepository;
//        private readonly IRepository<ApiMapperAuthorization> _mapperAuthorizationRepository;
//        private readonly IApiLogService _dCMSCoreLogService;
//        private readonly IWorkContext _workContext;
//        private readonly IRepository<UsersAndRolesManagement.UserOptions> _UserOptionsRepository;

//        public ApiMapperService(IRepository<ApiMapper> mapperRepository, IRepository<ApiMapperParams> maparParamsRepository,
//            IRepository<ApiMapperDetails> mapperDetailsRepository, IRepository<ApiMapperAuthorization> mapperAuthorizationRepository,
//            IApiLogService dCMSCoreLogService, IWorkContext workContext,
//            IRepository<UsersAndRolesManagement.UserOptions> UserOptionsRepository)
//        {
//            _mapperRepository = mapperRepository;
//            _maparParamsRepository = maparParamsRepository;
//            _mapperDetailsRepository = mapperDetailsRepository;
//            _mapperAuthorizationRepository = mapperAuthorizationRepository;
//            _dCMSCoreLogService = dCMSCoreLogService;
//            _workContext = workContext;
//            _UserOptionsRepository = UserOptionsRepository;
//        }

//        private static HttpContent CreateHttpContent(object content, string HttpMediaType)
//        {
//            HttpContent httpContent = null;

//            if (content != null)
//            {
//                var ms = new MemoryStream();
//                SerializeJsonIntoStream(content, ms);
//                ms.Seek(0, SeekOrigin.Begin);
//                httpContent = new StreamContent(ms);
//                httpContent.Headers.ContentType = new MediaTypeHeaderValue(HttpMediaType);
//            }

//            return httpContent;
//        }
//        public static void SerializeJsonIntoStream(object value, Stream stream)
//        {
//            using (var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
//            using (var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None })
//            {
//                var js = new JsonSerializer();
//                js.Serialize(jtw, value);
//                jtw.Flush();
//            }
//        }
//        private Dictionary<string, object> MatchingParamsValues(IDictionary<string, string> Values, ICollection<ApiMapperParams> paramsValues)
//        {
//            var newObjList = new Dictionary<string, object>();
//            if (paramsValues != null)
//            {
//                foreach (var item in paramsValues)
//                {

//                    if ((!string.IsNullOrEmpty(item.ParamValue) || item.ParamType.ToString().ToUpper() == "QUERYSTRING") && item.IsDeleted == false) // remove || item.ParamType.ToString().ToUpper() == "QUERYSTRING"
//                    {
//                        newObjList.Add(item.ParamName, item.ParamValue);
//                    }
//                }
//            }

//            return newObjList;
//        }

//        private Dictionary<string, string> MatchingQueryValues(IDictionary<string, string> Values, ICollection<ApiMapperParams> paramsValues)
//        {
//            var newObjList = new Dictionary<string, string>();
//            if (paramsValues != null)
//            {
//                foreach (var item in paramsValues)
//                {
//                    if (item.Direction != 0 && item.ParamType.ToString().ToUpper() == "QUERYSTRING" && item.IsDeleted == false)
//                    {
//                        if (item.SourceType.ToString().ToUpper() == "CONTROL")
//                        {
//                            string value = Values.Where(x => x.Key == item.ParamName).FirstOrDefault().Value;
//                            newObjList.Add(item.ParamName, value);
//                        }
//                        else if (item.SourceType.ToString().ToUpper() == "STATIC")
//                        {
//                            newObjList.Add(item.ParamName, item.ParamValue);
//                        }
//                    }
//                }
//            }
//            return newObjList;
//        }
//        private async Task<object> PostStream(JObject content, ApiMapperViewModel ApiInfo, long userId, string authToken = "")
//        {
//            try
//            {
//                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiInfo.URL);
//                var values = JObjectExtensions.ToDictionary(content);

//                string QuerstringVal = "";

//                var HeaderValue = MatchingParamsValues(values, ApiInfo.ApiMapperParams).ToList();
//                var QueryValues = MatchingQueryValues(values, ApiInfo.ApiMapperParams);

//                // add URL query strings to QuerstringVal
//                foreach (var item in QueryValues)
//                {
//                    if (QueryValues.Last().Key != item.Key)
//                    {

//                        if (item.Value != null)
//                        {
//                            QuerstringVal += item.Key + "=" + item.Value.ToString() + "&";
//                        }
//                        else
//                        {

//                            QuerstringVal += item.Key + "=" + "&"; ;
//                        }
//                    }
//                    else
//                    {
//                        if (item.Value != null)
//                        {
//                            QuerstringVal += item.Key + "=" + item.Value.ToString();
//                        }
//                        else
//                        {
//                            QuerstringVal += item.Key + "=";
//                        }
//                    }
//                }

//                // if QuerstringVal contains any data, we will concat it to the base URL
//                // do not change the order of creating the new URL to avoid reset the request data
//                if (!string.IsNullOrEmpty(QuerstringVal))
//                {
//                    string url = string.Concat(ApiInfo.URL, QuerstringVal);
//                    request = (HttpWebRequest)WebRequest.Create(url);
//                }

//                request.Method = ApiInfo.HttpMethod.ToString();
//                request.Credentials = CredentialCache.DefaultCredentials;
//                request = addAuthorizationToHeader(ApiInfo, request, userId, authToken);
//                var newContent = JsonConvert.SerializeObject(values);
//                var postData = Encoding.ASCII.GetBytes(newContent.ToString());

//                // need review
//                if(ApiInfo.ApiMapperParams != null)
//                {
//                    foreach (var param in ApiInfo.ApiMapperParams)
//                    {
//                        foreach (var item in HeaderValue)
//                        {
//                            if (item.Key == param.ParamName)
//                            {
//                                if (param.ParamType == ParamsTypes.Header || param.ParamType == ParamsTypes.QueryString)
//                                {
//                                    if (item.Value != null)
//                                    {
//                                        request.Headers.Add(item.Key, item.Value.ToString());
//                                        values.Remove(item.Key);
//                                    }
//                                }
//                            }
//                        }
//                    }

//                }

//                // add the content if the request is POST request, if you add it without checking the method type, 
//                // it will return a bad request on GET requests
//                if (request.Method.ToUpper() == "POST")
//                {
//                    request.ContentLength = postData.Length;
//                    request.ContentType = ApiInfo.MediaTypes;
//                }

//                // need review
//                if(ApiInfo.ApiMapperParams != null)
//                {
//                foreach (var param in ApiInfo.ApiMapperParams)
//                {
//                    if ((param.ParamType == ParamsTypes.Header) || (param.ParamType == ParamsTypes.QueryString))
//                    {
//                        foreach (var item in HeaderValue)
//                        {
//                            if (item.Key == param.ParamName)
//                            {
//                                if (param.ParamType == ParamsTypes.Header)
//                                {
//                                    if (item.Value != null)
//                                    {
//                                        request.Headers.Add(item.Key, item.Value.ToString());
//                                    }
//                                    else
//                                    {
//                                        request.Headers.Add(item.Key, "");
//                                    }

//                                }
//                                //else
//                                //{
//                                //    if (HeaderValue.Last().Key != item.Key)
//                                //    {

//                                //        if (item.Value != null)
//                                //        {
//                                //            QuerstringVal += item.Key + "=" + item.Value.ToString() + "&&";
//                                //        }
//                                //        else
//                                //        {
//                                //            QuerstringVal += item.Key + "=" + "&&"; ;
//                                //        }
//                                //    }
//                                //    else
//                                //    {
//                                //        if (item.Value != null)
//                                //        {
//                                //            QuerstringVal += item.Key + "=" + item.Value.ToString();
//                                //        }
//                                //        else
//                                //        {
//                                //            QuerstringVal += item.Key + "=";
//                                //        }
//                                //    }
//                                //}
//                            }
//                        }
//                    }
//                    else if (param.ParamType == ParamsTypes.Body)
//                    {
//                        request.GetRequestStream().Write(postData, 0, postData.Length);
//                    }
//                }

//                }

//                request.ServicePoint.ConnectionLeaseTimeout = 50000;
//                request.ServicePoint.MaxIdleTime = 50000;
//                ServicePointManager.DefaultConnectionLimit = 200;

//                string result = "";
//                HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse();
//                //
//                ApiLogVM dCMSCoreLog = new ApiLogVM
//                {
//                    ApiUrl = request.RequestUri.LocalPath,
//                    Response = httpResponse.ToString(),
//                    FullUrl = request.RequestUri.Host.ToString(),
//                    HttpMethod = request.Method,
//                    Request = request.ToString(),
//                    SiteId = await _workContext.GetCurrentSiteIdAsync(),
//                    StatusCode = (long)httpResponse.StatusCode,
//                    UserId = userId
//                };
//                _dCMSCoreLogService.CreateLog(dCMSCoreLog);
//                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
//                {
//                    result = streamReader.ReadToEnd();
//                    streamReader.Dispose();
//                    streamReader.Close();
//                    httpResponse.Dispose();
//                    httpResponse.Close();
//                }

//                var resultObj = JsonConvert.DeserializeObject(result);
//                return resultObj;
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//        }
//        private dynamic PostStreamInvoke(JObject content, ApiMapperViewModel ApiInfo, long userId, string authToken = "")
//        {
//            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiInfo.URL);

//            var values = JObjectExtensions.ToDictionary(content);
//            string QuerstringVal = "";
//            var HeaderValue = MatchingParamsValues(values, ApiInfo.ApiMapperParams);
//            var QueryValues = MatchingQueryValues(values, ApiInfo.ApiMapperParams);

//            foreach (var item in QueryValues)
//            {
//                if (QueryValues.Last().Key != item.Key)
//                {

//                    if (item.Value != null)
//                    {
//                        QuerstringVal += item.Key + "=" + item.Value.ToString() + "&";
//                    }
//                    else
//                    {
//                        QuerstringVal += item.Key + "=" + "&"; ;
//                    }
//                }
//                else
//                {
//                    if (item.Value != null)
//                    {
//                        QuerstringVal += item.Key + "=" + item.Value.ToString();
//                    }
//                    else
//                    {
//                        QuerstringVal += item.Key + "=";
//                    }
//                }
//            }

//            // add URL query strings to QuerstringVal
//            if (!string.IsNullOrEmpty(QuerstringVal))
//            {
//                string url = string.Concat(ApiInfo.URL, QuerstringVal);
//                request = (HttpWebRequest)WebRequest.Create(url);
//            }

//            request.Method = ApiInfo.HttpMethod.ToString();
//            request.Credentials = CredentialCache.DefaultCredentials;
//            var newContent = JsonConvert.SerializeObject(values);
//            var postData = Encoding.ASCII.GetBytes(newContent.ToString());
//            // add the content if the request is POST request, if you add it without checking the method type, 
//            // it will return a bad request on GET requests
//            if (request.Method.ToUpper() == "POST")
//            {
//                request.ContentLength = postData.Length;
//                request.ContentType = ApiInfo.MediaTypes;
//            }

//            foreach (var param in ApiInfo.ApiMapperParams)
//            {
//                foreach (var item in HeaderValue)
//                {
//                    if (item.Key == param.ParamName)
//                    {
//                        if (param.ParamType == ParamsTypes.Header || param.ParamType == ParamsTypes.QueryString) //check if it's header or query I think
//                        {
//                            if (item.Value != null)
//                            {
//                                request.Headers.Add(item.Key, item.Value.ToString());
//                                values.Remove(item.Key);
//                            }
//                        }
//                    }
//                }
//            }



//            // need review
//            foreach (var param in ApiInfo.ApiMapperParams)
//            {
//                if ((param.ParamType == ParamsTypes.Header) || (param.ParamType == ParamsTypes.QueryString))
//                {
//                    foreach (var item in HeaderValue)
//                    {
//                        if (item.Key == param.ParamName)
//                        {
//                            if (param.ParamType == ParamsTypes.Header)//
//                            {
//                                if (item.Value != null)
//                                {
//                                    request.Headers.Add(item.Key, item.Value.ToString());
//                                }
//                                else
//                                {
//                                    request.Headers.Add(item.Key, "");
//                                }
//                            }
//                            //else
//                            //{
//                            //    if (HeaderValue.Last().Key != item.Key)
//                            //    {
//                            //        if (item.Value != null)
//                            //        {
//                            //            QuerstringVal += item.Key + "=" + item.Value.ToString() + "&&";
//                            //        }
//                            //        else
//                            //        {
//                            //            QuerstringVal += item.Key + "=" + "&&"; ;
//                            //        }
//                            //    }
//                            //    else
//                            //    {
//                            //        if (item.Value != null)
//                            //        {
//                            //            QuerstringVal += item.Key + "=" + item.Value.ToString();
//                            //        }
//                            //        else
//                            //        {
//                            //            QuerstringVal += item.Key + "=";
//                            //        }
//                            //    }
//                            //}
//                        }
//                    }
//                }
//                else if (param.ParamType == ParamsTypes.Body)
//                {
//                    request.GetRequestStream().Write(postData, 0, postData.Length);
//                }
//            }

//            request = addAuthorizationToHeader(ApiInfo, request, userId, authToken);

//            string responseFromServer = "";
//            request.ServicePoint.ConnectionLeaseTimeout = 50000;
//            request.ServicePoint.MaxIdleTime = 50000;
//            ServicePointManager.DefaultConnectionLimit = 200;
//            var x = request.Headers;

//            HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse();
//            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
//            {
//                responseFromServer = streamReader.ReadToEnd();
//                streamReader.Close();
//                httpResponse.Close();
//            }

//            JToken token = JToken.Parse(responseFromServer);

//            var outParams = ApiInfo.ApiMapperParams.Where(p => p.Direction == ParameterDirection.Output);
//            switch (ApiInfo.ApiReponseType)
//            {
//                case ApiReponseType.Array:
//                    // var modifiedResult = "[" + responseFromServer + "]";
//                    // JObject jsonObject = JObject.Parse(responseFromServer);
//                    JArray objectToFetch = new JArray(JArray.Parse(responseFromServer));
//                    //foreach (JArray o in jsonObject)
//                    //{
//                    //    objectToFetch.Add(o);
//                    //}
//                    return CreateArrayResult(objectToFetch, outParams, ApiInfo.Path);

//                case ApiReponseType.Object:
//                    return CreateObjectResult(token, outParams, ApiInfo.Path);

//                default:
//                    return token;
//            }

//            //var resultObj = JsonConvert.DeserializeObject<JObject>(result);
//            //dynamic valObj = new ExpandoObject();
//            //dynamic newObject = new ExpandoObject();
//            //if (!string.IsNullOrEmpty(path))
//            //    valObj = resultObj[path];
//            //else
//            //    valObj = resultObj;
//            //string outNum = JsonConvert.SerializeObject(valObj);
//            //var newresultObj = JsonConvert.DeserializeObject<JObject>(outNum);
//            //var outParamcount = ApiInfo.ApiMapperParams.Where(x=> x.Direction == ParameterDirection.Output).Count();
//            //if (outParamcount > 0)
//            //{
//            //    foreach (var item in ApiInfo.ApiMapperParams)
//            //    {
//            //        if (item.Direction == ParameterDirection.Output)
//            //        {
//            //             newresultObj.ContainsKey(item.ParamName);
//            //        }

//            //    }
//            //    return newObject;
//            //}
//            //else
//            //    return valObj;
//        }

//        private JObject CreateObjectResult(object tokentemp, IEnumerable<ApiMapperParams> outParams, string responsePath)
//        {
//            try
//            {
//                var newToken = JsonConvert.SerializeObject(tokentemp);
//                var token = JsonConvert.DeserializeObject<JToken>(newToken);
//                if (token.Type != JTokenType.Object) throw new ArgumentException("Response does not match the type defined");
//                JObject value = new JObject();
//                if (string.IsNullOrEmpty(responsePath))
//                {
//                    if (outParams.Count() > 0)
//                    {
//                        foreach (var param in outParams)
//                        {
//                            //if (token.Contains(param.ParamName))
//                            if (token.Children().Select(x => x.Path == param.ParamName).Any())
//                            {
//                                value.Add(param.ParamName, token[param.ParamName]);
//                            }
//                            else throw new ArgumentException("Array does not contain submitted property");
//                        }
//                    }
//                    else
//                    {
//                        value = (JObject)token;
//                    }
//                    return value;
//                }
//                else
//                {
//                    var param = outParams.FirstOrDefault(x => x.ParamName == responsePath);
//                    if (token.Children().Select(x => x.Path == param.ParamName) != null)
//                    {
//                        var tokenPath = token.SelectToken(responsePath);
//                        value.Add(param.ParamName, tokenPath);
//                    }
//                    else throw new ArgumentException("Object does not contain submitted property");
//                    return value;
//                }

//            }
//            catch (Exception exc)
//            {

//                throw exc;
//            }

//        }

//        private JArray CreateArrayResult(JArray token, IEnumerable<ApiMapperParams> outParams, string responsePath)
//        {
//            if (string.IsNullOrEmpty(responsePath))
//            {
//                if (token.Type != JTokenType.Array) throw new ArgumentException("Response does not match the type defined");
//                JArray values = new JArray();
//                for (int i = 0; i < token.Count; i++)
//                {
//                    var obj = token[i] as JObject;
//                    JObject value = new JObject();
//                    List<JObject> listResponse = new List<JObject>();
//                    JObject matchObject = new JObject();
//                    List<JObject> matches;
//                    foreach (var param in outParams)
//                    {
//                        matches = FindKey(token, param.ParamName, listResponse, matchObject);
//                    }
//                    var resLength = 0;
//                    if (outParams.Count() > 0)
//                    {
//                        resLength = listResponse.Count / outParams.Count();
//                    }
//                    for (var item = 0; item < resLength; item++)
//                    {
//                        var finalKey = listResponse[item];
//                        for (var p = 1; p < outParams.Count(); p++)
//                        {
//                            var keyToAdd = listResponse[resLength * p];
//                            finalKey.Merge(keyToAdd, new JsonMergeSettings
//                            {
//                                MergeArrayHandling = MergeArrayHandling.Union
//                            });
//                        }
//                        values.Add(finalKey);
//                    }
//                }
//                return values;
//            }
//            else
//            {
//                if (token.Type != JTokenType.Array) throw new ArgumentException("Response does not match the type defined");
//                JArray values = new JArray();
//                for (int i = 0; i < token.Count; i++)
//                {
//                    JObject value = new JObject();
//                    List<JObject> listResponse = new List<JObject>();
//                    JObject matchObject = new JObject();
//                    List<JObject> matches;
//                    foreach (var param in outParams)
//                    {
//                        var tokenPath = token.First().SelectToken(responsePath);//.Children().FirstOrDefault(x => x.Path == responsePath);
//                        matches = FindKey(tokenPath, param.ParamName, listResponse, matchObject);
//                    }
//                    var resLength = listResponse.Count / outParams.Count();
//                    for (var item = 0; item < resLength; item++)
//                    {
//                        var finalKey = listResponse[item];
//                        for (var p = 1; p < outParams.Count(); p++)
//                        {
//                            var keyToAdd = listResponse[resLength * p];
//                            finalKey.Merge(keyToAdd, new JsonMergeSettings
//                            {
//                                MergeArrayHandling = MergeArrayHandling.Union
//                            });
//                        }
//                        values.Add(finalKey);
//                    }
//                }
//                return values;
//            }

//        }
//        private static List<JObject> FindKey(JToken containerToken, string name, List<JObject> listResponse, JObject matchObject)
//        {

//            if (containerToken.Type == JTokenType.Object)
//            {
//                foreach (JProperty child in containerToken.Children<JProperty>())
//                {

//                    var preObject = listResponse.FirstOrDefault(x => x.ContainsKey(child.Name) && x.GetValue(child.Name) == child.Value);
//                    if (preObject == null)
//                    {
//                        //matchObject.Add(child.Name, child.Value);
//                        matchObject = new JObject();
//                        if (child.Name == name)
//                        {
//                            matchObject.Add(child.Name, child.Value);
//                            listResponse.Add(matchObject);
//                            return listResponse;
//                        }
//                    }
//                    else
//                    {
//                        if (preObject.GetValue(name) == null)
//                        {
//                            preObject.Add(child.Next);
//                            return listResponse;
//                        }
//                        else
//                        {
//                            matchObject = new JObject();
//                            if (child.Name == name)
//                            {
//                                matchObject.Add(child.Name, child.Value);
//                                listResponse.Add(matchObject);
//                                return listResponse;
//                            }
//                        }
//                    }

//                    FindKey(child.Value, name, listResponse, matchObject);
//                }
//            }
//            else if (containerToken.Type == JTokenType.Array)
//            {
//                foreach (JToken child in containerToken.Children())
//                {
//                    FindKey(child, name, listResponse, matchObject);
//                }
//                return listResponse;
//            }
//            return listResponse;


//        }

//        private static object WebApiResponse(string Res, ApiMapperViewModel ApiInfo)
//        {
//            try
//            {
//                //var result = Res.Content.ReadAsStringAsync().Result;
//                //var OutParams = ApiInfo.ApiMapperParams.Where(x => x.Direction == ParameterDirection.Output).ToList();
//                //var jsonResult = JObject.Parse(result);
//                //  var responseObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(Res);
//                var jsonObj = JsonConvert.SerializeObject(Res);

//                return jsonObj;
//            }
//            catch (Exception)
//            {

//                throw;
//            }
//        }



//        private string InvokeWCF(ApiMapperViewModel ApiInfo, JObject Params)
//        {
//            try
//            {
//                WebServiceClient Inv = new WebServiceClient();
//                Inv.Url = ApiInfo.URL;

//                var values = JObjectExtensions.ToDictionary(Params);
//                Inv.Parameters = (IDictionary<string, object>)values;
//                Inv.WCFContractName = ApiInfo.ContractName; //"IService";
//                Inv.WebMethod = ApiInfo.WebMethodName;// "GetData";
//                Inv.WSServiceType = ServiceType.WCF;
//                return Inv.InvokeService();
//            }
//            catch (Exception exc)
//            {

//                throw exc;
//            }
//        }
//        private async Task<object> InvokeWebApi(ApiMapperViewModel ApiInfo, JObject Params, long userId, string authToken = "")
//        {
//            try
//            {

//                //JObject jsonResult;
//                //string Json = JsonConvert.SerializeObject(Params);
//                return await PostStream(Params, ApiInfo, userId, authToken);
//                // return res;
//                //  HttpContent Obj = new StringContent(Json, Encoding.UTF8, "application/json");


//                //if (response.IsSuccessStatusCode)
//                //{
//                //    jsonResult = JObject.Parse(result);
//                //    JObject content = jsonResult.GetValue("content").ToObject<JObject>();
//                //    GlobalContext.Token = content["Token"].Value<string>();
//                //    HttpService.client.AddBearer(GlobalContext.Token);
//                //    SessionId = content["SessionId"].ToString();
//                //    ErrorMSG = jsonResult["message"].ToString();
//                //    ErrorCode = jsonResult["code"].ToString();
//                //    if (!string.IsNullOrEmpty(ErrorMSG))
//                //        return false;
//                //    else
//                //        return true;
//                //}
//                //else
//                //{

//                //    jsonResult = JObject.Parse(result);
//                //    // JObject content = jsonResult.GetValue("content").ToObject<JObject>();
//                //    // GlobalContext.Token = content["Token"].Value<string>();
//                //    //  HttpService.client.AddBearer(GlobalContext.Token);
//                //    // SessionId = content["SessionId"].ToString();
//                //    ErrorMSG = jsonResult["message"].ToString();
//                //    ErrorCode = jsonResult["code"].ToString();
//                //    if (!string.IsNullOrEmpty(ErrorMSG))
//                //        return false;
//                //    else
//                //        return true;

//                //    //  throw new Exception(result);
//                //    throw new Exception("Not emplemtned");
//            }

//            catch (Exception exc)
//            {

//                throw exc;
//            }
//        }
//        private dynamic InvokeWebResponse(ApiMapperViewModel ApiInfo, JObject Params, long userId = 0, string token = null)
//        {
//            try
//            {

//                string Json = JsonConvert.SerializeObject(Params);

//                return PostStreamInvoke(Params, ApiInfo, userId, token);
//                // return res;
//                //  HttpContent Obj = new StringContent(Json, Encoding.UTF8, "application/json");


//                //if (response.IsSuccessStatusCode)
//                //{
//                //    jsonResult = JObject.Parse(result);
//                //    JObject content = jsonResult.GetValue("content").ToObject<JObject>();
//                //    GlobalContext.Token = content["Token"].Value<string>();
//                //    HttpService.client.AddBearer(GlobalContext.Token);
//                //    SessionId = content["SessionId"].ToString();
//                //    ErrorMSG = jsonResult["message"].ToString();
//                //    ErrorCode = jsonResult["code"].ToString();
//                //    if (!string.IsNullOrEmpty(ErrorMSG))
//                //        return false;
//                //    else
//                //        return true;
//                //}
//                //else
//                //{

//                //    jsonResult = JObject.Parse(result);
//                //    // JObject content = jsonResult.GetValue("content").ToObject<JObject>();
//                //    // GlobalContext.Token = content["Token"].Value<string>();
//                //    //  HttpService.client.AddBearer(GlobalContext.Token);
//                //    // SessionId = content["SessionId"].ToString();
//                //    ErrorMSG = jsonResult["message"].ToString();
//                //    ErrorCode = jsonResult["code"].ToString();
//                //    if (!string.IsNullOrEmpty(ErrorMSG))
//                //        return false;
//                //    else
//                //        return true;

//                //    //  throw new Exception(result);
//                //    throw new Exception("Not emplemtned");
//            }

//            catch (Exception exc)
//            {

//                throw exc;
//            }
//        }
//        public async Task<dynamic> StaticInvoke(StaticInvokeVM model)
//        {
//            try
//            {

//                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(model.ApiUrl);
//                request.Method = model.HttpMethod;
//                request.Credentials = CredentialCache.DefaultCredentials;
//                var newContent = JsonConvert.SerializeObject(model.Requestdata);
//                var postData = Encoding.UTF8.GetBytes(newContent.ToString());

//                if (request.Method.ToUpper() == "POST")
//                {
//                    request.ContentLength = postData.Length;
//                    request.ContentType = model.ContentType;
//                }


//                request.GetRequestStream().Write(postData, 0, postData.Length);

//                request.ServicePoint.ConnectionLeaseTimeout = 50000;
//                request.ServicePoint.MaxIdleTime = 50000;
//                ServicePointManager.DefaultConnectionLimit = 200;

//                string result = "";
//                HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse();
//                ApiLogVM dCMSCoreLog = new ApiLogVM
//                {
//                    ApiUrl = request.RequestUri.LocalPath,
//                    Response = httpResponse.ToString(),
//                    FullUrl = request.RequestUri.Host.ToString(),
//                    HttpMethod = request.Method,
//                    Request = request.ToString(),
//                    SiteId = await _workContext.GetCurrentSiteIdAsync(),
//                    StatusCode = (long)httpResponse.StatusCode,
//                    UserId = _workContext.GetCurrentUserIdValue()
//                };
//                _dCMSCoreLogService.CreateLog(dCMSCoreLog);
//                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
//                {
//                    result = streamReader.ReadToEnd();
//                    streamReader.Dispose();
//                    streamReader.Close();
//                    httpResponse.Dispose();
//                    httpResponse.Close();
//                }

//                var resultObj = JsonConvert.DeserializeObject(result);
//                return resultObj;
//            }

//            catch (Exception exc)
//            {
//                throw exc;
//            }
//        }

//        public async Task<dynamic> Invoke(long ApiId, JObject obj, long userId, string token = null)
//        {
//            try
//            {
//                var ApiInfo = await GetApiMapperById(ApiId);

//                if (ApiInfo.Type == ApiTypes.WebApi)
//                {
//                    return InvokeWebResponse(ApiInfo, obj, userId, token);
//                }
//                else if (ApiInfo.Type == ApiTypes.WCF)
//                {
//                    return InvokeWCF(ApiInfo, obj);
//                }
//                else
//                {
//                    return await InvokeWebApi(ApiInfo, obj, userId);
//                }

//            }
//            catch (Exception exc)
//            {

//                throw exc;
//            }
//        }
//        public async Task<object> TestInvokerAPI(ApiMapperViewModel ApiInfo, JObject obj, long userId, string authToken = "")
//        {
//            try
//            {
//                //var ApiInfo = await GetApiMapperById(ApiId);

//                if (ApiInfo.Type == ApiTypes.WebApi)
//                {
//                    return await InvokeWebApi(ApiInfo, obj, userId, authToken);
//                }
//                else if (ApiInfo.Type == ApiTypes.WCF)
//                {
//                    return InvokeWCF(ApiInfo, obj);
//                }
//                else
//                {
//                    return await InvokeWebApi(ApiInfo, obj, userId);
//                }
//                //   return 0;

//            }
//            catch (Exception exc)
//            {

//                throw exc;
//            }
//        }

//        /////////////////////////////////////////// luay new api /////// start//////////

//        public async Task<List<inputValuesViewModel>> GetInputValues(long Id)
//        {
//            try
//            {
//                List<inputValuesViewModel> InputValuesLst = new List<inputValuesViewModel>();

//                var values = await _maparParamsRepository.Query().Where(x => x.ApiMapperId == Id && x.IsDeleted == false)
//                 .Select(x => new
//                 {
//                     x.ParamName,
//                     x.ParamValue,
//                     x.SourceType,
//                     path = _mapperDetailsRepository.Query().Where(x => x.ApiMapperId == Id).Select(x => x.ResponsePath).FirstOrDefault()
//                 }).ToListAsync();

//                foreach (var item in values)
//                {
//                    inputValuesViewModel InputValuesObj = new inputValuesViewModel();
//                    Dictionary<string, string> inputValueDic = new Dictionary<string, string>();
//                    inputValueDic.Add(item.ParamName, item.ParamValue);
//                    InputValuesObj.SourceType = item.SourceType;
//                    InputValuesObj.inputValue = inputValueDic;
//                    InputValuesObj.path = item.path;
//                    InputValuesLst.Add(InputValuesObj);
//                }

//                return InputValuesLst;
//            }
//            catch (Exception)
//            {

//                throw;
//            }
//        }

//        public async Task<List<ApiMapperViewModel>> GetApiMapperSiteId(long SiteId)
//        {
//            try
//            {
//                List<ApiMapperViewModel> mapperList = await _mapperRepository.Query()
//                    .Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted).Select(p => new ApiMapperViewModel
//                    {
//                        Id = p.Id,
//                        Name = p.Name,
//                        Description = p.Description,
//                        URL = p.URL,
//                        HttpMethod = p.HttpMethod,
//                        Type = p.Type,
//                        Status = p.Status,
//                        AuthorizationType = p.AuthorizationType,
//                        SiteId = p.SiteId

//                    }).ToListAsync();

//                return mapperList;
//            }
//            catch (Exception exc)
//            {
//                throw exc;
//            }
//        }

//        public async Task<List<ApiMapperDetailsViewModel>> GetDataSourceListBySiteId(long id)
//        {
//            try
//            {
//                List<ApiMapperDetailsViewModel> resultList = new List<ApiMapperDetailsViewModel>();
//                var DataSourceList = await _mapperRepository.Query().Include(x => x.ApiMapperAuthorizations).Where(x => x.SiteId == id && x.Status != EStatus.Deleted).ToListAsync();

//                foreach (var item in DataSourceList)
//                {
//                    ApiMapperDetailsViewModel DataSourceObject = new ApiMapperDetailsViewModel();

//                    DataSourceObject.Id = item.Id;
//                    DataSourceObject.Name = item.Name;
//                    DataSourceObject.Description = item.Description;
//                    DataSourceObject.URL = item.URL;
//                    DataSourceObject.HttpMethod = item.HttpMethod;
//                    DataSourceObject.MediaTypes = item.MediaType;
//                    DataSourceObject.Type = item.Type;
//                    DataSourceObject.Status = item.Status;
//                    DataSourceObject.AuthorizationType = item.AuthorizationType;
//                    DataSourceObject.ApiReponseType = item.ApiReponseType;
//                    DataSourceObject.SiteId = item.SiteId;
//                    DataSourceObject.ContractName = item.ContractName;
//                    DataSourceObject.ContentType = item.ContentType;
//                    DataSourceObject.Accept = item.Accept;
//                    DataSourceObject.WebMethodName = item.WebMethodName;
//                    DataSourceObject.ApiMapperParams = item.ApiMapperParams;
//                    DataSourceObject.path = _mapperDetailsRepository.Query().Where(x => x.ApiMapperId == DataSourceObject.Id).Select(x => x.ResponsePath).FirstOrDefault();
//                    DataSourceObject.inputs = getInputsById(item.Id);
//                    DataSourceObject.outputs = getOutputsById(item.Id);
//                    DataSourceObject.AuthorizationKeys = item.ApiMapperAuthorizations;
//                    resultList.Add(DataSourceObject);
//                }
//                return resultList;
//            }
//            catch (Exception ex)
//            {

//                throw ex;
//            }
//        }



//        public async Task<ApiMapperDetailsViewModel> GetDatasourceById(long Id)
//        {
//            try
//            {
//                ApiMapperDetailsViewModel MapperObject = new ApiMapperDetailsViewModel();
//                MapperObject = await _mapperRepository.Query().Include(x => x.ApiMapperAuthorizations).Where(x => x.Id == Id).Select(p => new ApiMapperDetailsViewModel
//                {
//                    Id = p.Id,
//                    Name = p.Name,
//                    Description = p.Description,
//                    URL = p.URL,
//                    ApiMapperParams = p.ApiMapperParams,
//                    HttpMethod = p.HttpMethod,
//                    Type = p.Type,
//                    Status = p.Status,
//                    AuthorizationType = p.AuthorizationType,
//                    SiteId = p.SiteId,
//                    MediaTypes = p.MediaType,
//                    ApiReponseType = p.ApiReponseType,
//                    AuthorizationKeys = p.ApiMapperAuthorizations,
//                    ParentId = p.ParentId
//                }).FirstOrDefaultAsync();
//                MapperObject.inputs = getInputsById(MapperObject.Id);
//                MapperObject.outputs = getOutputsById(MapperObject.Id);
//                return MapperObject;
//            }
//            catch (Exception exc)
//            {
//                throw exc;
//            }
//        }
//        public async Task<dynamic> GetDatasourceByName(string name, JObject obj, int userId = 0, string token = "")
//        {
//            try
//            {
//                var dataSource = await _mapperRepository.Query().Where(x => x.URL.ToUpper().Contains(name.ToUpper())).FirstOrDefaultAsync();
//                return await Invoke(dataSource.Id, obj, userId, token);
//            }
//            catch (Exception exc)
//            {
//                throw exc;
//            }
//        }

//        public List<ApiMapperParamsViewModel> getInputsById(long Id)
//        {

//            return _maparParamsRepository.Query().Where(x => x.ApiMapperId == Id && x.Direction == ParameterDirection.Input && x.IsDeleted == false).Select(p => new ApiMapperParamsViewModel
//            {
//                Id = p.Id,
//                Direction = p.Direction,
//                ApiMapperId = p.ApiMapperId,
//                ParamName = p.ParamName,
//                ParamOrder = p.ParamOrder,
//                ParamValue = p.ParamValue,
//                SourceType = p.SourceType,
//                ParamType = p.ParamType,
//                CreatedById = p.CreatedById,
//            }).ToList();
//        }
//        public List<string> getOutputsById(long Id)
//        {
//            return _maparParamsRepository.Query().Where(x => x.ApiMapperId == Id && x.Direction == ParameterDirection.Output).Select(p => p.ParamName).ToList();
//        }




//        public async Task<long> AddApiMapper(DatasourceVM datasource, long siteId, long userID)
//        {
//            try
//            {
//                ApiMapper maperObj = new ApiMapper();
//                maperObj.Name = datasource.apiMapperViewModel.Name;
//                maperObj.Description = datasource.apiMapperViewModel.Description;
//                maperObj.URL = datasource.apiMapperViewModel.URL;
//                maperObj.HttpMethod = datasource.apiMapperViewModel.HttpMethod;
//                maperObj.Status = datasource.apiMapperViewModel.Status;
//                maperObj.MediaType = datasource.apiMapperViewModel.MediaTypes;
//                maperObj.AuthorizationType = datasource.apiMapperViewModel.AuthorizationType;
//                maperObj.SiteId = siteId;
//                maperObj.CreatedById = userID;
//                maperObj.CreationDate = DateTimeOffset.Now;
//                maperObj.ApiReponseType = datasource.apiMapperViewModel.ApiReponseType;
//                maperObj.Type = datasource.apiMapperViewModel.Type;
//                maperObj.ContentType = datasource.apiMapperViewModel.ContentType;
//                maperObj.WebMethodName = datasource.apiMapperViewModel.WebMethodName;
//                maperObj.ContractName = datasource.apiMapperViewModel.ContractName;
//                maperObj.Accept = datasource.apiMapperViewModel.Accept;
//                maperObj.ApiReponseType = datasource.apiMapperViewModel.ApiReponseType;
//                maperObj.ParentId = datasource.apiMapperViewModel.ParentId;

//                _mapperRepository.Add(maperObj);
//                await _mapperRepository.SaveChangesAsync();

//                await changeAuthorizationType(datasource.apiMapperViewModel.AuthorizationKeys, maperObj.Id, datasource.tokenSource);


//                for (int i = 0; i < datasource.inputs.Count; i++)
//                {
//                    //ApiMapperParamsViewModel paramData = new ApiMapperParamsViewModel();
//                    //paramData.ApiMapperId = maperObj.Id;
//                    //paramData.ParamName = datasource.inputs[i].ParamName;
//                    //paramData.ParamOrder = 0;
//                    //paramData.Direction = ParameterDirection.Input;
//                    //paramData.ParamType = datasource.inputs[i].ParamType;
//                    //paramData.SourceType = datasource.inputs[i].SourceType;
//                    //paramData.ParamValue = datasource.inputs[i].ParamValue;
//                    //AddApiMapperParams(paramData, userID);

//                    ApiMapperParams NewParam = new ApiMapperParams();
//                    NewParam.ApiMapperId = maperObj.Id;
//                    NewParam.ParamName = datasource.inputs[i].ParamName;
//                    NewParam.ParamOrder = datasource.inputs[i].ParamOrder;
//                    NewParam.CreationDate = DateTimeOffset.Now; ;
//                    NewParam.CreatedById = userID;
//                    NewParam.ParamType = datasource.inputs[i].ParamType;
//                    NewParam.Direction = datasource.inputs[i].Direction;
//                    NewParam.SourceType = datasource.inputs[i].SourceType;
//                    NewParam.ParamValue = datasource.inputs[i].ParamValue;
//                    _maparParamsRepository.Add(NewParam);
//                    await _maparParamsRepository.SaveChangesAsync();
//                }

//                for (int i = 0; i < datasource.outputs.Count; i++)
//                {
//                    //ApiMapperParamsViewModel paramData = new ApiMapperParamsViewModel();
//                    //paramData.ApiMapperId = maperObj.Id;
//                    //paramData.ParamName = datasource.outputs[i];
//                    //paramData.ParamOrder = 0;
//                    //paramData.Direction = ParameterDirection.Output;
//                    //paramData.ParamType = 0;
//                    //AddApiMapperParams(paramData, userID);

//                    ApiMapperParams NewParam = new ApiMapperParams();
//                    NewParam.ApiMapperId = maperObj.Id;
//                    NewParam.ParamName = datasource.outputs[i];
//                    NewParam.ParamOrder = 0;
//                    NewParam.CreationDate = DateTimeOffset.Now; ;
//                    NewParam.CreatedById = userID;
//                    NewParam.ParamType = 0;
//                    NewParam.Direction = ParameterDirection.Output;
//                    NewParam.SourceType = SourceType.Output;
//                    NewParam.ParamValue = string.Empty;
//                    _maparParamsRepository.Add(NewParam);
//                    await _maparParamsRepository.SaveChangesAsync();
//                }
//                foreach (var item in datasource.paths)
//                {
//                    _mapperDetailsRepository.Add(new ApiMapperDetails
//                    {
//                        ApiMapperId = maperObj.Id,
//                        CreatedById = userID,
//                        CreationDate = DateTimeOffset.Now,
//                        ResponsePath = item
//                    });
//                    await _mapperDetailsRepository.SaveChangesAsync();
//                }

//                return maperObj.Id;

//            }
//            catch (Exception exc)
//            {

//                throw exc;
//            }
//        }

//        public async Task<long> EditApiMapper(DatasourceVM datasource, long siteId, long userID)
//        {
//            try
//            {
//                ApiMapper maperObj = await _mapperRepository.Query().Where(x => x.Id == datasource.apiMapperViewModel.Id).FirstOrDefaultAsync();
//                maperObj.Name = datasource.apiMapperViewModel.Name;
//                maperObj.Description = datasource.apiMapperViewModel.Description;
//                maperObj.URL = datasource.apiMapperViewModel.URL;
//                maperObj.HttpMethod = datasource.apiMapperViewModel.HttpMethod;
//                maperObj.Status = datasource.apiMapperViewModel.Status;
//                maperObj.MediaType = datasource.apiMapperViewModel.MediaTypes;
//                maperObj.AuthorizationType = datasource.apiMapperViewModel.AuthorizationType;
//                maperObj.SiteId = datasource.apiMapperViewModel.SiteId;
//                maperObj.ModifiedById = userID;
//                maperObj.Type = datasource.apiMapperViewModel.Type;
//                maperObj.ModificationDate = DateTimeOffset.Now;
//                maperObj.ApiReponseType = datasource.apiMapperViewModel.ApiReponseType;
//                maperObj.ParentId = datasource.apiMapperViewModel.ParentId;
//                _mapperRepository.Update(maperObj);

//                await changeAuthorizationType(datasource.apiMapperViewModel.AuthorizationKeys, datasource.apiMapperViewModel.Id, datasource.tokenSource);

//                await _mapperRepository.SaveChangesAsync();

//                var deletedParams = _maparParamsRepository.Query().Where(x => x.ApiMapperId == datasource.apiMapperViewModel.Id && x.Direction == ParameterDirection.Input && x.IsDeleted == false).Select(x => x.Id).ToList();

//                for (int i = 0; i < datasource.inputs.Count; i++)
//                {
//                    deletedParams.Remove(datasource.inputs[i].Id);
//                    if (datasource.inputs[i].Id != 0)
//                    {
//                        await EditApiMapperParams(datasource.inputs[i], userID);
//                    }
//                    else
//                    {
//                        ApiMapperParams NewParam = new ApiMapperParams();
//                        NewParam.ApiMapperId = datasource.inputs[i].ApiMapperId;
//                        NewParam.ParamName = datasource.inputs[i].ParamName;
//                        NewParam.ParamOrder = datasource.inputs[i].ParamOrder;
//                        NewParam.CreationDate = DateTimeOffset.Now; ;
//                        NewParam.CreatedById = userID;
//                        NewParam.ParamType = datasource.inputs[i].ParamType;
//                        NewParam.Direction = datasource.inputs[i].Direction;
//                        NewParam.SourceType = datasource.inputs[i].SourceType;
//                        NewParam.ParamValue = datasource.inputs[i].ParamValue;
//                        _maparParamsRepository.Add(NewParam);
//                        await _maparParamsRepository.SaveChangesAsync();
//                    }

//                }
//                if (deletedParams.Count > 0)
//                {
//                    foreach (var item in deletedParams)
//                    {
//                        await DeleteApiMapperParams(item, userID);
//                    }
//                }
//                // check the following code ****
//                var CurrentInputs = await _maparParamsRepository.Query().Where(x => x.ApiMapperId == datasource.apiMapperViewModel.Id && x.Direction == ParameterDirection.Input).ToListAsync();
//                for (int ii = 0; ii < CurrentInputs.Count; ii++)
//                {
//                    if (!isMatchedInputs(datasource.inputs, CurrentInputs[ii].ParamName))
//                    {

//                        _maparParamsRepository.Remove(CurrentInputs[ii]);
//                        await _maparParamsRepository.SaveChangesAsync();
//                    }
//                }
//                var apiDetailsObj = await _mapperDetailsRepository.Query().Where(x => x.ApiMapperId == maperObj.Id).ToListAsync();
//                foreach (var item in apiDetailsObj)
//                {
//                    _mapperDetailsRepository.Remove(item);
//                }
//                await _mapperDetailsRepository.SaveChangesAsync();

//                foreach (var item in datasource.paths)
//                {
//                    _mapperDetailsRepository.Add(new ApiMapperDetails
//                    {
//                        ApiMapperId = maperObj.Id,
//                        CreatedById = userID,
//                        CreationDate = DateTimeOffset.Now,
//                        ResponsePath = item
//                    });
//                    await _mapperDetailsRepository.SaveChangesAsync();
//                }



//                //////////////Outputs 

//                var existOutputs = await _maparParamsRepository.Query().Where(x => x.ApiMapperId == datasource.apiMapperViewModel.Id && x.Direction == ParameterDirection.Output).ToListAsync();
//                var matchedOutputs = getMatchedOutputs(existOutputs, datasource.outputs);

//                for (int i = 0; i < existOutputs.Count; i++)
//                    if (!isMatchedOutput(matchedOutputs, existOutputs[i].ParamName))
//                        _maparParamsRepository.Remove(existOutputs[i]);

//                for (int i = 0; i < datasource.outputs.Count; i++)
//                    if (!isMatchedOutput(matchedOutputs, datasource.outputs[i]))
//                    {
//                        ApiMapperParams NewParam = new ApiMapperParams();
//                        NewParam.ApiMapperId = maperObj.Id;
//                        NewParam.ParamName = datasource.outputs[i];
//                        NewParam.ParamOrder = 0;
//                        NewParam.CreationDate = DateTimeOffset.Now; ;
//                        NewParam.CreatedById = userID;
//                        NewParam.ParamType = 0;
//                        NewParam.Direction = ParameterDirection.Output;
//                        NewParam.SourceType = SourceType.Output;
//                        NewParam.ParamValue = string.Empty;
//                        _maparParamsRepository.Add(NewParam);
//                        await _maparParamsRepository.SaveChangesAsync();

//                    }
//                await _maparParamsRepository.SaveChangesAsync();
//                return maperObj.Id;
//            }
//            catch (Exception exc)
//            {
//                throw exc;
//            }
//        }

//        public async Task<List<UserOptionsVM>> GetAuthenticationUserOptions(string propertyName, string refreshProperty)
//        {
//            try
//            {
//                var userId = await _workContext.GetCurrentUserId();
//                return await _UserOptionsRepository.Query().Where(x => x.UserId == userId && (x.PropertyName == propertyName || x.PropertyName == refreshProperty)).Select(x => new UserOptionsVM
//                {
//                    PropertyName = x.PropertyName,
//                    ControlType = x.ControlType,
//                    Value = x.Value
//                }).ToListAsync();
//            }
//            catch (Exception exc)
//            {
//                throw exc;
//            }
//        }

//        public List<ApiMapperParams> getMatchedOutputs(List<ApiMapperParams> existOutputs, List<string> oldOutputs)
//        {
//            var result = new List<ApiMapperParams>();

//            for (int ii = 0; ii < existOutputs.Count; ii++)
//                for (int i = 0; i < oldOutputs.Count; i++)
//                    if (existOutputs[ii].ParamName == oldOutputs[i])
//                        result.Add(existOutputs[ii]);

//            return result;
//        }


//        public bool isMatchedOutput(List<ApiMapperParams> matchedOutputs, string output)
//        {
//            for (int i = 0; i < matchedOutputs.Count; i++)
//                if (matchedOutputs[i].ParamName == output)
//                    return true;

//            return false;
//        }

//        public bool isMatchedInputs(List<ApiMapperParamsViewModel> Inputs, string ParamName)
//        {
//            for (int i = 0; i < Inputs.Count; i++)
//                if (Inputs[i].ParamName == ParamName)
//                    return true;

//            return false;
//        }


//        private async Task changeAuthorizationType(List<ApiMapperAuthorization> authorizationKeys, long apiId, ETokenSource? tokenSource)
//        {

//            List<ApiMapperAuthorization> mapperAuthList = await _mapperAuthorizationRepository.Query()
//                 .Where(x => x.ApiMapperId == apiId).ToListAsync<ApiMapperAuthorization>();

//            foreach (var item in mapperAuthList)
//            {
//                _mapperAuthorizationRepository.Remove(item);
//            }

//            foreach (var auth in authorizationKeys)
//            {
//                _mapperAuthorizationRepository.Add(new ApiMapperAuthorization
//                {
//                    ApiMapperId = apiId,
//                    Key = auth.Key,
//                    Value = auth.Value,
//                    TokenSource = tokenSource
//                });
//            }
//            await _mapperAuthorizationRepository.SaveChangesAsync();
//        }

//        private HttpWebRequest addAuthorizationToHeader(ApiMapperViewModel ApiInfo, HttpWebRequest request, long userId, string token)
//        {
//            //  ----- Added By Isra'a ----- Adding Authorization to headers

//            if (ApiInfo.AuthorizationType != ApiAuthorizationTypes.NoAuth)
//            {
//                List<ApiMapperAuthorization> credentials = _mapperAuthorizationRepository.Query().Where(x => x.ApiMapperId == ApiInfo.Id).ToList();
//                string auth = string.Empty;
//                switch (ApiInfo.AuthorizationType)
//                {
//                    case ApiAuthorizationTypes.BasicAuth:
//                        {
//                            var usernameObj = credentials.FirstOrDefault(x => x.Key.ToLower() == "username");
//                            var passwordObj = credentials.FirstOrDefault(x => x.Key.ToLower() == "password");
//                            if (usernameObj == null)
//                            {
//                                usernameObj = new ApiMapperAuthorization();
//                                usernameObj.Value = "";
//                            }
//                            if (passwordObj == null)
//                            {
//                                passwordObj.Value = "";
//                            }

//                            NetworkCredential myNetworkCredential = new NetworkCredential(usernameObj.Value, passwordObj.Value);

//                            CredentialCache myCredentialCache = new CredentialCache();
//                            Uri ApiUri = new Uri(ApiInfo.URL);
//                            myCredentialCache.Add(ApiUri, "Basic", myNetworkCredential);

//                            request.PreAuthenticate = true;
//                            request.Credentials = myCredentialCache;
//                        }
//                        break;

//                    case ApiAuthorizationTypes.BearerToken:

//                        {
//                            switch (ApiInfo.TokenSource)
//                            {
//                                case ETokenSource.DataBase:
//                                    var tokenNameInDB = credentials.FirstOrDefault(x => x.Key == "DatabaseToken");
//                                    var userOption = _UserOptionsRepository.Query().FirstOrDefault(x => x.PropertyName == (tokenNameInDB.Value != null ? tokenNameInDB.Value : "") && x.UserId == userId);
//                                    auth = userOption != null ? userOption.Value : string.Empty;
//                                    break;
//                                case ETokenSource.LocalStorage:
//                                    auth = !string.IsNullOrEmpty(token) ? token : string.Empty;
//                                    break;
//                            }
//                            request.Headers["Authorization"] = string.Format("Bearer {0}", token);
//                        }
//                        break;
//                }
//            }
//            return request;
//            //
//        }  
//          private HttpRequestMessage addAuthorizationToHeader(ApiMapperViewModel ApiInfo, HttpRequestMessage request, long userId, string token)
//        {
//            //  ----- Added By Isra'a ----- Adding Authorization to headers

//            if (ApiInfo.AuthorizationType != ApiAuthorizationTypes.NoAuth)
//            {
//                List<ApiMapperAuthorization> credentials = _mapperAuthorizationRepository.Query().Where(x => x.ApiMapperId == ApiInfo.Id).ToList();
//                string auth = string.Empty;
//                switch (ApiInfo.AuthorizationType)
//                {
//                    case ApiAuthorizationTypes.BasicAuth:
//                        {
//                            var usernameObj = credentials.FirstOrDefault(x => x.Key.ToLower() == "username");
//                            var passwordObj = credentials.FirstOrDefault(x => x.Key.ToLower() == "password");
//                            if (usernameObj == null)
//                            {
//                                usernameObj = new ApiMapperAuthorization();
//                                usernameObj.Value = "";
//                            }
//                            if (passwordObj == null)
//                            {
//                                passwordObj.Value = "";
//                            }

//                            NetworkCredential myNetworkCredential = new NetworkCredential(usernameObj.Value, passwordObj.Value);

//                            CredentialCache myCredentialCache = new CredentialCache();
//                            Uri ApiUri = new Uri(ApiInfo.URL);
//                            myCredentialCache.Add(ApiUri, "Basic", myNetworkCredential);

//                        }
//                        break;

//                    case ApiAuthorizationTypes.BearerToken:

//                        {
//                            switch (ApiInfo.TokenSource)
//                            {
//                                case ETokenSource.DataBase:
//                                    var tokenNameInDB = credentials.FirstOrDefault(x => x.Key == "DatabaseToken");
//                                    var userOption = _UserOptionsRepository.Query().FirstOrDefault(x => x.PropertyName == (tokenNameInDB.Value != null ? tokenNameInDB.Value : "") && x.UserId == userId);
//                                    auth = userOption != null ? userOption.Value : string.Empty;
//                                    break;
//                                case ETokenSource.LocalStorage:
//                                    auth = !string.IsNullOrEmpty(token) ? token : string.Empty;
//                                    break;
//                            }
//                            request.Headers.Add("Authorization",  string.Format("Bearer {0}", token));
//                        }
//                        break;
//                }
//            }
//            return request;
//            //
//        }  
        
       



//        /////////////////////////////////////////// luay new api /////// end//////////


//        public async Task<ApiMapperParams> AddApiMapperParams(ApiMapperParamsViewModel MapperParams, long userID)
//        {
//            try
//            {
//                ApiMapperParams NewParam = new ApiMapperParams();
//                NewParam.ApiMapperId = MapperParams.ApiMapperId;
//                NewParam.ParamName = MapperParams.ParamName;
//                NewParam.ParamOrder = MapperParams.ParamOrder;
//                NewParam.CreationDate = DateTimeOffset.Now; ;
//                NewParam.CreatedById = userID;
//                NewParam.ParamType = MapperParams.ParamType;
//                NewParam.Direction = MapperParams.Direction;
//                NewParam.SourceType = MapperParams.SourceType;
//                NewParam.ParamValue = MapperParams.ParamValue;
//                _maparParamsRepository.Add(NewParam);
//                await _maparParamsRepository.SaveChangesAsync();
//                return NewParam;

//            }
//            catch (Exception exc)
//            {
//                throw exc;
//            }

//        }
//        public async Task<ApiMapperParams> EditApiMapperParams(ApiMapperParamsViewModel MapperParams, long userID)
//        {
//            try
//            {
//                ApiMapperParams EditedParam = await _maparParamsRepository.Query().Where(x => x.Id == MapperParams.Id).FirstOrDefaultAsync();
//                EditedParam.ApiMapperId = MapperParams.ApiMapperId;
//                EditedParam.ParamName = MapperParams.ParamName;
//                EditedParam.ParamOrder = MapperParams.ParamOrder;
//                EditedParam.ModificationDate = DateTimeOffset.Now; ;
//                EditedParam.ParamType = MapperParams.ParamType;
//                EditedParam.CreatedById = userID;
//                EditedParam.Direction = MapperParams.Direction;
//                EditedParam.SourceType = MapperParams.SourceType;
//                EditedParam.ParamValue = MapperParams.ParamValue;
//                _maparParamsRepository.Update(EditedParam);
//                await _maparParamsRepository.SaveChangesAsync();
//                return EditedParam;

//            }
//            catch (Exception exc)
//            {
//                throw exc;
//            }
//        }


//        public async Task<ApiMapperParams> DeleteApiMapperParams(long paramId, long userID)
//        {
//            try
//            {
//                ApiMapperParams EditedParam = await _maparParamsRepository.Query().Where(x => x.Id == paramId).FirstOrDefaultAsync();
//                EditedParam.ApiMapperId = EditedParam.ApiMapperId;
//                EditedParam.ParamName = EditedParam.ParamName;
//                EditedParam.ParamOrder = EditedParam.ParamOrder;
//                EditedParam.ModificationDate = DateTimeOffset.Now; ;
//                EditedParam.ParamType = EditedParam.ParamType;
//                EditedParam.CreatedById = userID;
//                EditedParam.Direction = EditedParam.Direction;
//                EditedParam.SourceType = EditedParam.SourceType;
//                EditedParam.ParamValue = EditedParam.ParamValue;
//                EditedParam.IsDeleted = true;
//                _maparParamsRepository.Update(EditedParam);
//                await _maparParamsRepository.SaveChangesAsync();
//                return EditedParam;
//            }
//            catch (Exception exc)
//            {
//                throw exc;
//            }
//        }
//        public async Task<ApiMapperViewModel> GetApiMapperById(long Id)
//        {
//            try
//            {
//                return await _mapperRepository.Query().Include(x => x.ApiMapperAuthorizations).Where(x => x.Id == Id).Select(p => new ApiMapperViewModel
//                {
//                    Id = p.Id,
//                    Name = p.Name,
//                    Description = p.Description,
//                    URL = p.URL,
//                    ApiMapperParams = p.ApiMapperParams.Where(x => x.IsDeleted == false).ToList(),
//                    HttpMethod = p.HttpMethod,
//                    Type = p.Type,
//                    Status = p.Status,
//                    AuthorizationType = p.AuthorizationType,
//                    SiteId = p.SiteId,
//                    MediaTypes = p.MediaType,
//                    ApiReponseType = p.ApiReponseType,
//                    Path = _mapperDetailsRepository.Query().FirstOrDefault(x => x.ApiMapperId == p.Id).ResponsePath,
//                    AuthorizationKeys = p.ApiMapperAuthorizations,
//                    TokenSource = p.ApiMapperAuthorizations.FirstOrDefault() != null ? p.ApiMapperAuthorizations.FirstOrDefault().TokenSource : null

//                }).FirstOrDefaultAsync();
//            }
//            catch (Exception exc)
//            {
//                throw exc;
//            }
//        }


//        public async Task<dynamic> GetMapperParamsByMapperId(long mapperId, long siteId, long userID, ParameterDirection direction)
//        {
//            try
//            {
//                List<ApiMapperParams> headerArray = new List<ApiMapperParams>();
//                List<ApiMapperParams> bodyArray = new List<ApiMapperParams>();
//                List<ApiMapperParams> QSArray = new List<ApiMapperParams>();
//                List<ApiMapperParams> OutParamArray = new List<ApiMapperParams>();

//                List<ApiMapperParams> mapperParams = await _maparParamsRepository.Query().Where(x => x.ApiMapperId == mapperId && x.Direction == direction)
//                    .Select(p => new ApiMapperParams
//                    {
//                        Id = p.Id,
//                        ApiMapperId = p.ApiMapperId,
//                        ParamName = p.ParamName,
//                        ParamOrder = p.ParamOrder,
//                        ParentId = p.ParentId,
//                        Direction = p.Direction,
//                        CreatedById = p.CreatedById,
//                        ParamType = p.ParamType,
//                        CreationDate = p.CreationDate,
//                        ModificationDate = p.ModificationDate
//                    }).ToListAsync();

//                foreach (var item in mapperParams)
//                {

//                    if (item.ParamType == ParamsTypes.Header)
//                    {
//                        headerArray.Add(item);
//                    }
//                    else if (item.ParamType == ParamsTypes.Body)
//                    {
//                        bodyArray.Add(item);
//                    }
//                    else if (item.ParamType == ParamsTypes.QueryString)
//                    {
//                        QSArray.Add(item);
//                    }
//                    else if (item.ParamType == 0 && item.Direction == direction)
//                    {
//                        OutParamArray.Add(item);
//                    }


//                }

//                dynamic Result = new ExpandoObject();
//                Result.HeaderData = headerArray;
//                Result.BodyData = bodyArray;
//                Result.QSData = QSArray;
//                Result.OutParamArray = OutParamArray;
//                return Result;
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//        }


//        public async Task<bool> DeletMapperParams(string paramName, long siteId, long userId)
//        {
//            ApiMapperParams paramObj = await _maparParamsRepository.Query().Where(x => x.ParamName == paramName).FirstOrDefaultAsync();
//            _maparParamsRepository.Remove(paramObj);
//            return true;
//        }

//        public bool DeleteDataSource(long Id)
//        {
//            try
//            {
//                ApiMapper mapperObj = _mapperRepository.Query().Where(x => x.Id == Id).FirstOrDefault();
//                mapperObj.Status = EStatus.Deleted;
//                _mapperRepository.Update(mapperObj);
//                _mapperRepository.SaveChanges();
//                return true;
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }


//        }



//        //////////////// New Invoker ///////////


//        public async Task<dynamic> HandleWebAPIRequests(long ApiMapperId, dynamic obj, long userId,string authToken="", dynamic FormData = null)
//        {
//            string response = string.Empty;
//            var content = new StringContent(string.Empty);

//            //Get API Data
//            var apiInfo = await GetApiMapperById(ApiMapperId);

//            if (apiInfo != null)
//            {
//                // This step format all the data needed for api request (Body, header and querystring)
//                var finalParam = HandlingParams(apiInfo.ApiMapperParams, apiInfo.MediaTypes, obj);
//                if (!string.IsNullOrEmpty(finalParam.QueryString))
//                {
//                    apiInfo.URL = string.Concat(apiInfo.URL, finalParam.QueryString);
//                    apiInfo.URL = apiInfo.URL.Replace("??", "?");
//                }
//                content = new StringContent(JsonConvert.SerializeObject(finalParam.BodyObj), System.Text.Encoding.UTF8, apiInfo.MediaTypes);

//                foreach (var item in finalParam.HeaderObj)
//                {
//                    content.Headers.Add(item.Key, item.Value);
//                }
//                using (var client = new HttpClient())
//                {
//                    var apiUri = new System.Uri(apiInfo.URL);
//                    client.Timeout = TimeSpan.FromSeconds(50);
//                    var sp = ServicePointManager.FindServicePoint(apiUri);
//                    sp.ConnectionLeaseTimeout = 50000; 
//                    sp.MaxIdleTime = 50000;
//                    sp.ConnectionLimit = 50000;
//                    ServicePointManager.DefaultConnectionLimit = 200;
//                    if (!string.IsNullOrEmpty(apiInfo.MediaTypes) && apiInfo.MediaTypes.Equals("multipart/form-data"))
//                    {
//                        //content = finalParam.FormDataObj;
//                        //var formDataRequest = client.PostAsync(apiInfo.URL, new FormUrlEncodedContent(FormData)).Result;
//                        //var responseFromServer = formDataRequest.Content.ReadAsStringAsync().Result;

//                        string apiResponse = "";
//                        var form = new MultipartFormDataContent();
//                        //foreach (var file in FormData.Files)
//                        //{
//                        var file = FormData.Files[0];
//                        using (var fileStream = file.OpenReadStream())
//                        {
//                            form.Add(new StreamContent(fileStream), "file", file.FileName);
//                            using (var formDataResponse = await client.PostAsync(apiInfo.URL, form))
//                            {
//                                apiResponse = formDataResponse.Content.ReadAsStringAsync().Result;
//                                return HandleResponseDependOnType(apiResponse, apiInfo);
//                            }
//                        }
//                        //}

//                        foreach (var key in FormData.Keys)
//                        {

//                        }



//                    }
//                    else
//                    {
//                        HttpRequestMessage request = new HttpRequestMessage
//                        {
//                            Method = new HttpMethod(apiInfo.HttpMethod.ToString()),
//                            RequestUri = apiUri,
//                            Content = content,
//                        };


//                        request = addAuthorizationToHeader(apiInfo, request, userId, authToken);

//                        HttpResponseMessage result = await client.SendAsync(request);
//                        if (result.IsSuccessStatusCode)
//                        {
//                            response = result.Content.ReadAsStringAsync().Result;

//                            return HandleResponseDependOnType(response, apiInfo);
//                        }
//                        else
//                        {
//                            //var test = result.ReasonPhrase;
//                            throw new Exception("Remote server returned (" + result.Content.ReadAsStringAsync().Result + ")");
//                        }
//                    }
//                }

//            }
//            else
//            {
//                throw new Exception("Data Source Not configuered!");
//            }
//        }

//        private dynamic HandleResponseDependOnType(string response, ApiMapperViewModel apiInfo)
//        {
//            JToken token = JToken.Parse(response);
//            var outParams = apiInfo.ApiMapperParams.Where(p => p.Direction == ParameterDirection.Output);
//            switch (apiInfo.ApiReponseType)
//            {
//                case ApiReponseType.Array:
//                    return CreateArrayResult(response, outParams, apiInfo.Path);

//                case ApiReponseType.Object:
//                    return CreateObjectResult(token, outParams, apiInfo.Path);


//                case ApiReponseType.Number:
//                case ApiReponseType.String:
//                default:
//                    return token;
//            }
//        }

//        private JArray CreateArrayResult(dynamic token, IEnumerable<ApiMapperParams> outParams, string responsePath)
//        {
//            JArray objectToFetch = new JArray();
//            try
//            {
//                objectToFetch = JArray.Parse(token);
//            }
//            catch (Exception)
//            {
//                var obj = JsonConvert.DeserializeObject(token);
//                if (!string.IsNullOrEmpty(responsePath))
//                {
//                    token = obj[responsePath];
//                }
//                else
//                {
//                    token = obj;
//                }
//            }

//            if (string.IsNullOrEmpty(responsePath))
//            {
//                if (token.Type != JTokenType.Array) throw new ArgumentException("Response does not match the type defined");
//                JArray values = new JArray();
//                for (int i = 0; i < token.Count; i++)
//                {
//                    var obj = token[i] as JObject;
//                    JObject value = new JObject();
//                    List<JObject> listResponse = new List<JObject>();
//                    JObject matchObject = new JObject();
//                    List<JObject> matches;
//                    foreach (var param in outParams)
//                    {
//                        matches = FindKey(token, param.ParamName, listResponse, matchObject);
//                    }
//                    var resLength = 0;
//                    if (outParams.Count() > 0)
//                    {
//                        resLength = listResponse.Count / outParams.Count();
//                    }
//                    for (var item = 0; item < resLength; item++)
//                    {
//                        var finalKey = listResponse[item];
//                        for (var p = 1; p < outParams.Count(); p++)
//                        {
//                            var keyToAdd = listResponse[resLength * p];
//                            finalKey.Merge(keyToAdd, new JsonMergeSettings
//                            {
//                                MergeArrayHandling = MergeArrayHandling.Union
//                            });
//                        }
//                        values.Add(finalKey);
//                    }
//                }
//                return values;
//            }
//            else
//            {
//                JArray values = new JArray();
//                for (int i = 0; i < token.Count; i++)
//                {
//                    JObject item = token[i];
//                    JObject obj = new JObject();

//                    foreach (var param in outParams)
//                    {
//                        JToken temp = null;
//                        if (item.TryGetValue(param.ParamName, out temp))
//                        {
//                            obj.Add(param.ParamName, item[param.ParamName]);
//                        }
//                        else
//                        {
//                            obj.Add(param.ParamName, null);
//                        }
//                    }

//                    values.Add(obj);
//                }
//                return values;
//            }

//        }
//        private APIMapperParamTypesVM HandlingParams(ICollection<ApiMapperParams> ParamsValues, string APIMediaType, dynamic Values = null, dynamic file = null)
//        {
//            APIMapperParamTypesVM apiMapperParamTypeVM = new APIMapperParamTypesVM();

//            if (ParamsValues != null)
//            {
//                var headerObj = new ExpandoObject() as IDictionary<string, Object>;
//                var bodyObj = new ExpandoObject() as IDictionary<string, Object>;
//                var formData = new MultipartFormDataContent();

//                foreach (var item in ParamsValues)
//                {
//                    if (!item.IsDeleted && item.Direction == ParameterDirection.Input)
//                    {
//                        switch (item.ParamType)
//                        {
//                            case ParamsTypes.Header:
//                                HandlingHeaderAndBodyParams(item, headerObj, Values);
//                                break;
//                            case ParamsTypes.Body:
//                                HandlingHeaderAndBodyParams(item, bodyObj, Values);
//                                break;
//                            case ParamsTypes.QueryString:
//                                apiMapperParamTypeVM.QueryString = HandlingQueryString(apiMapperParamTypeVM.QueryString, item, Values);
//                                break;
//                        }

//                    }
//                }
//                apiMapperParamTypeVM.HeaderObj = (ExpandoObject)headerObj;

//                apiMapperParamTypeVM.BodyObj = (ExpandoObject)bodyObj;

//                apiMapperParamTypeVM.FormDataObj = formData;
//            }
//            return apiMapperParamTypeVM;
//        }

//        private string HandlingQueryString(string QueryString, ApiMapperParams item, dynamic Values = null)
//        {
//            string str = string.IsNullOrEmpty(QueryString) ? "?{0}={1}" : "&{0}={1}";
//            if (item.SourceType == SourceType.Control)
//            {
//                string queryValue = "";
//                try
//                {
//                    queryValue = Values != null ? Values[item.ParamName] : string.Empty;
//                }
//                catch (Exception)
//                {

//                    queryValue = string.Empty;
//                }
//                QueryString += string.Format(str, item.ParamName, string.IsNullOrEmpty(queryValue) ? "" : queryValue);
//            }
//            else if (item.SourceType == SourceType.Static)
//            {
//                QueryString += string.Format(str, item.ParamName, string.IsNullOrEmpty(item.ParamValue) ? "" : item.ParamValue);
//            }
//            return QueryString;
//        }

//        private void HandlingHeaderAndBodyParams(ApiMapperParams item, IDictionary<string, Object> obj, dynamic Values = null)
//        {
//            if (item.SourceType == SourceType.Control)
//            {
//                var headerValue = Values != null ? Values[item.ParamName] : "";
//                obj.Add(item.ParamName, headerValue);
//            }
//            else if (item.SourceType == SourceType.Static)
//            {
//                obj.Add(item.ParamName, item.ParamValue);
//            }
//        }

//        private void HandlingFormData(ApiMapperParams item, MultipartFormDataContent formData, dynamic Values = null, dynamic file = null)
//        {
//            if (item.SourceType == SourceType.Control)
//            {
//                var headerValue = Values != null ? Values[item.ParamName] : "";
//                formData.Add(new StringContent(headerValue), item.ParamName);

//            }
//            else if (item.SourceType == SourceType.Static)
//            {
//                formData.Add(new StringContent(item.ParamValue), item.ParamName);
//            }
//            else if (item.SourceType == SourceType.File)
//            {
//                using (var fileStream = file.OpenReadStream())
//                {
//                    var content = new StreamContent(fileStream);
//                    content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType.ToString());
//                    formData.Add(content, "file", file.FileName);
//                }
//            }
//        }
    
//    }


//}
