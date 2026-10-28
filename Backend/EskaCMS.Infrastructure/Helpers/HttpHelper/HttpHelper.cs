using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Helpers.HttpHelper
{

    public class HttpHelper : IHttpHelper
    {
        private readonly IHttpClientFactory _clientFactory;
        public HttpHelper(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;

        }

        private void AddContentAndContentType(HttpRequestMessage Request, string Content)
        {


            Request.Headers.Add("Accept", "application/json");

            Request.Content = new StringContent(Content, Encoding.UTF8);
            Request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");


        }

        private void AddContentAndContentType(HttpRequestMessage Request, MultipartFormDataContent content)
        {

            Request.Content = content;



        }


        private async Task<string> MakeARequest(HttpMethod Method, GeneralRequestVM Model)
        {
            HttpClient Client = _clientFactory.CreateClient();
            try
            {

                Client.Timeout = TimeSpan.FromMinutes(30);

                if (!string.IsNullOrEmpty(Model.BaseUrl))
                {
                    Client.BaseAddress = new Uri(Model.BaseUrl);
                }

                var Request = new HttpRequestMessage(Method, Model.Url);


                switch (Model.ContentType)
                {
                    case EContentType.JSON:

                        AddContentAndContentType(Request, Model.SerializedContent);

                        break;
                    case EContentType.FormData:
                        AddContentAndContentType(Request, Model.FormDataContent);
                        break;
                }


                if (Model.Headers != null)
                {
                    foreach (var Header in Model.Headers)
                    {

                        Request.Headers.Add(Header.Key, Header.Value);
                    }
                }

                if (Model.IsBasicAuth)
                {
                    var authenticationString = $"{Model.BasicAuthCredinatials.ClientId}:{Model.BasicAuthCredinatials.ClientSecret}";
                    var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));

                    Request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
                }

                HttpResponseMessage Response = await Client.SendAsync(Request);
                Client.Dispose();
                var ResponseStringContent = await Response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(ResponseStringContent))
                {
                    return ResponseStringContent;
                }
                APIResultVM<dynamic> Error = new APIResultVM<dynamic>();
                try
                {
                    Error = JsonConvert.DeserializeObject<APIResultVM<dynamic>>(ResponseStringContent);
                }
                catch (Exception)
                {
                }

                if (!Response.IsSuccessStatusCode)
                {


                    if (Error != null)
                    {
                        throw new Exception(ResponseStringContent);


                    }

                    throw new Exception(Response.ReasonPhrase);




                }
                return ResponseStringContent;

            }
            catch (Exception e)
            {
                Client.Dispose();
                throw e;
            }

        }

        #region GET 
        public async Task<string> Get(GetRequestVM Model)
        {

            GeneralRequestVM GeneralModel = new GeneralRequestVM()
            {
                SerializedContent = "",
                BasicAuthCredinatials=Model.BasicAuthCredinatials,
                Url = Model.Url,
                Headers = Model.Headers,
                BaseUrl = Model.BaseUrl,
            };
            return await MakeARequest(HttpMethod.Get, GeneralModel);

        }


        public async Task<string> Get(string URL, string BaseUrl = "")
        {

            GeneralRequestVM GeneralModel = new GeneralRequestVM()
            {
                SerializedContent = "",
                Url = URL,
                BaseUrl = BaseUrl,
            };
            return await MakeARequest(HttpMethod.Get, GeneralModel);

        }

        public async Task<string> Get(RequestWithFormDataVM Model)
        {

            GeneralRequestVM GeneralModel = new GeneralRequestVM()
            {
                FormDataContent = Model.Content,
                ContentType = EContentType.FormData,
                Url = Model.Url,
                BasicAuthCredinatials=Model.BasicAuthCredinatials,
                Headers = Model.Headers,
                BaseUrl = Model.BaseUrl
            };


            return await MakeARequest(HttpMethod.Get, GeneralModel);

        }

        #endregion

        #region Delete 
        public async Task<string> Delete(GetRequestVM Model)
        {
            GeneralRequestVM GeneralModel = new GeneralRequestVM()
            {
                SerializedContent = "",
                Url = Model.Url,
                Headers = Model.Headers,
                BasicAuthCredinatials = Model.BasicAuthCredinatials,
                BaseUrl = Model.BaseUrl,
            };
            return await MakeARequest(HttpMethod.Delete, GeneralModel);

        }

        public async Task<string> Delete(string URL, string BaseUrl = "")
        {
            GeneralRequestVM GeneralModel = new GeneralRequestVM()
            {
                SerializedContent = "",
                Url = URL,
                BaseUrl = BaseUrl,
            };
            return await MakeARequest(HttpMethod.Delete, GeneralModel);

        }

        #endregion

        #region Post 
        public async Task<string> Post(PostRequestVM Model)
        {

            GeneralRequestVM GeneralModel = new GeneralRequestVM()
            {
                SerializedContent = Model.SerializedContent,
                Url = Model.Url,
                Headers = Model.Headers,
                BaseUrl = Model.BaseUrl,
                BasicAuthCredinatials=Model.BasicAuthCredinatials,
            };


            return await MakeARequest(HttpMethod.Post, GeneralModel);

        }


        public async Task<string> Post(RequestWithFormDataVM Model)
        {

            GeneralRequestVM GeneralModel = new GeneralRequestVM()
            {
                FormDataContent = Model.Content,
                ContentType = EContentType.FormData,
                Url = Model.Url,
                Headers = Model.Headers,
                BaseUrl = Model.BaseUrl,
                BasicAuthCredinatials = Model.BasicAuthCredinatials,

            };


            return await MakeARequest(HttpMethod.Post, GeneralModel);

        }
        #endregion

        public async Task<string> Put(PostRequestVM Model)
        {

            GeneralRequestVM GeneralModel = new GeneralRequestVM()
            {
                SerializedContent = Model.SerializedContent,
                Url = Model.Url,
                Headers = Model.Headers,
                BasicAuthCredinatials = Model.BasicAuthCredinatials,
                BaseUrl = Model.BaseUrl
            };


            return await MakeARequest(HttpMethod.Put, GeneralModel);

        }


    }
}
