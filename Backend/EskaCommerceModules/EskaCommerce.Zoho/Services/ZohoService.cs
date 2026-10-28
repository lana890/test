using EskaCMS.Core.Services;
using EskaCommerce.Zoho.Models;
using EskaCommerce.Zoho.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Zoho
{
    public class ZohoService :IZohoService
    {
      

        private readonly ISiteSettingsService _settingservice;
        public ZohoService( ISiteSettingsService settingservice)
        {
          
            _settingservice = settingservice;
        }
        private async Task CreateGuestUser()
        {
           
                //var URL = "https://www.zohoapis.com/crm/v2/functions/createguestuser/actions/execute";

                //try
                //{
                //    var settings = _settingservice.GetSiteSettingById(SMSProviders.SMSGlobal.ToString(), SiteId).Result;
                //    if (settings == null)
                //        throw new Exception("No settings for such provider");

                //    var ObjSettings = JsonConvert.DeserializeObject<SMSGlobalSettings>(settings.Value);

                //    string ApiKey = ObjSettings.ApiKey;// "8c0e4c3e21a6607ed328ab2f1a253017";
                //    string ApiSecret = ObjSettings.ApiSecret;// "92575d6ce94643eb6356a2bdb17b4699";

                //    string method = ObjSettings.method;// "GET";
                //    string host = ObjSettings.host;// "api.smsglobal.com";
                //    string port = ObjSettings.port;// "443";
                //    string version = ObjSettings.version;// "v2";
                //    string path = ObjSettings.path;// "sms";
                //                                   //string smsid = ObjSettings.smsid;// "";
                //    string extradata = string.Empty;
                //    Uri uri = new Uri(string.Format("https://{0}/{1}/{2}", host, version, path));

                //    string timestamp = ((int)(DateTime.UtcNow.AddSeconds(-60).Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
                //    string nonce = Guid.NewGuid().ToString("N");
                //    string mac = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n", timestamp, nonce, method, uri.PathAndQuery, uri.Host, port, extradata);
                //    mac = Convert.ToBase64String((new HMACSHA256(Encoding.ASCII.GetBytes(ApiSecret))).ComputeHash(Encoding.ASCII.GetBytes(mac)));

                //    string authToken = string.Format("id=\"{0}\", ts=\"{1}\", nonce=\"{2}\", mac=\"{3}\"", ApiKey, timestamp, nonce, mac);

                //    SMSGlobalVM objtoken = new SMSGlobalVM();
                //    objtoken.destination = Destination;// "971509190910";
                //    objtoken.message = Message;//.Add(  "TestHomeCuts");
                //    objtoken.origin = ObjSettings.origin;// "Test";
                //    string result = string.Empty;
                //    JObject jsonResult;
                //    string Json = JsonConvert.SerializeObject(objtoken);
                //    HttpContent Obj = new StringContent(Json, Encoding.UTF8, "application/json");
                //    using (var client = new HttpClient())
                //    {
                //        client.DefaultRequestHeaders.Accept.Clear();
                //        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("MAC", authToken);
                //        //  client.DefaultRequestHeaders.Add("Authorization",string.Format("{0}", authToken));
                //        var Base = new Uri(string.Format("{0}://{1}/{2}/{3}", uri.Scheme, uri.Host, version, path));
                //        HttpResponseMessage response = client.PostAsync(Base, Obj).Result;
                //        result = response.Content.ReadAsStringAsync().Result;
                //        if (!response.IsSuccessStatusCode)
                //        {
                //        }
                //    }
                //}
                //catch (Exception exc)
                //{

                //    throw exc;
                //}
          //  }
          
        }
        private async Task<AccessTokenResponseVM> AccessTokenRequest(string AuthorizationCode)
        {
            var BaseURL = "https://accounts.zoho.com/oauth/v2/token";
            var client_id = "1000.BWVFEYI927NFALY042IDHQNK4NGGLE";
            var client_secret = "d7fe693a94c4cc5789fc467c189254ffc0dd80e40a";
            var redirect_uri = "http://localhost:5000/swagger/index.html";
            var grant_type = AuthorizationCode;

      //var Url =  "https://accounts.zoho.com/oauth/v2/token?
      //      client_id = 1000.GMB0YULZHJK411248S8I5GZ4CHUEX0 &
      //      grant_type = authorization_code &
      //      client_secret = 122c324d3496d5d777ceeebc129470715fbb856b7 &
      //        redirect_uri = https://www.zylker.com/oauthredirect&
      //      code = 1000.86a03ca5dbfccb7445b1889b8215efb0.cad9e1ae4989a1196fe05aa729fcb4e1""

            AccessTokenVM objReqParams = new AccessTokenVM();
            objReqParams.BaseURL = BaseURL;
            objReqParams.client_id = client_id;
            objReqParams.client_secret = client_secret;
            objReqParams.redirect_uri = redirect_uri;
            objReqParams.grant_type = grant_type;
            string result = string.Empty;
            JObject jsonResult;
            string Json = JsonConvert.SerializeObject(objReqParams);
            HttpContent Obj = new StringContent(Json, Encoding.UTF8, "application/json");
            AccessTokenResponseVM Resobj = new AccessTokenResponseVM();
            using (var client = new HttpClient())
            {
               
                HttpResponseMessage response = client.PostAsync(BaseURL, Obj).Result;
                result = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    
                     Resobj = JsonConvert.DeserializeObject<AccessTokenResponseVM>(result);

                    return Resobj;
                }
                else
                {
                    return Resobj;
                }

            }
        }

        public async Task<AccessTokenResponseVM> AuthorizationRequest()
        {
            try
            {
               var BaseUrl= "https://accounts.zoho.com/oauth/v2/auth";
                    //var Method = "POST";

                var client_id = "1000.BWVFEYI927NFALY042IDHQNK4NGGLE";
               // var response_type = "code";
                var redirect_uri = "http://localhost:5000/swagger/index.html";
                var Scope = "ZohoWriter.documentEditor.ALL";
                var access_type = "online";
                var hhh = " 1000.30338a86c930b79d2cd86e6d7aac9de9.77ebc170c9da9ca183f1fab99177b531";

                //        var    URL=  "https://accounts.zoho.com/oauth/v2/auth
                //? response_type = code &
                //client_id = 1000.GMB0YULZHJK411284S8I5GZ4CHUEX0 &
                //scope = AaaServer.profile.Read &
                //redirect_uri = https ://www.zylker.com/oauthredirect&
                //prompt = consent""
                var URL = "https://accounts.zoho.com/oauth/v2/auth?scope=ZohoWriter.documentEditor.ALL&client_id=1000.3F3CVV6RYZOB6M4DXFKYETE1CLOYJP&response_type=code&access_type=online&redirect_uri=http://localhost:5000/swagger/index.html";

                BaseUrl += "?response_type=code&client_id=" + client_id + "&scope=" + Scope + "&redirect_uri=" + redirect_uri;
                using (var client = new HttpClient())
                {
                   
                    HttpResponseMessage response = client.GetAsync(URL).Result;
                  var  result = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        return await AccessTokenRequest(result);
                    }
                    else
                    {
                        return new AccessTokenResponseVM();
                    }
                }

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
    }
}
