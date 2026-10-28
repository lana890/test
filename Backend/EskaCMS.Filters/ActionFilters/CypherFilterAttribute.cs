using EskaCMS.Filters.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Filters.ActionFilters
{
    public class CypherFilterAttribute : Attribute,IActionFilter , IResultFilter
    {

        public void OnActionExecuted(ActionExecutedContext context)
        {

            // No Need

        }

        public async void OnActionExecuting(ActionExecutingContext context)
        {
            //try
            //{
            //    var Request = context.HttpContext.Request;

            //    string Body = await FormatRequest(Request); // Must Be Stringifed List Of String

            //    List<string> CipherList = JsonConvert.DeserializeObject<List<string>>(Body);
            //    RSA_Helper RSA = new RSA_Helper();

            //    // var Encrypted = RSA.Encrypt(Body); // To Be Removed
            //    var Decrypted = RSA.DecryptStringList(CipherList);

            //    Request.Body = await ConvertStringToStreem(Body);
            //}
            //catch (Exception e)
            //{

            //    throw e;
            //}
            
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {

            ////This line allows us to set the reader for the request back at the beginning of its stream.
            //request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);
            //We now need to read the request stream.  First, we create a new byte[] with the same length as the request stream...
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];

            //...Then we copy the entire request stream into the new buffer.
            await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);

            //We convert the byte[] into a string using UTF8 encoding...
            var bodyAsText = Encoding.UTF8.GetString(buffer);

            // reset the stream position to 0, which is allowed because of EnableBuffering()
            request.Body.Seek(0, SeekOrigin.Begin);

            return bodyAsText;
        }


        private async Task<string> FormatResponse(HttpResponse Response)
        {

            ////This line allows us to set the reader for the request back at the beginning of its stream.
            //request.EnableBuffering();
            Response.Body.Seek(0, SeekOrigin.Begin);
            //We now need to read the request stream.  First, we create a new byte[] with the same length as the request stream...
            var buffer = new byte[Convert.ToInt32(Response.ContentLength)];

            //...Then we copy the entire request stream into the new buffer.
            await Response.Body.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);

            //We convert the byte[] into a string using UTF8 encoding...
            var bodyAsText = Encoding.UTF8.GetString(buffer);

            // reset the stream position to 0, which is allowed because of EnableBuffering()
            Response.Body.Seek(0, SeekOrigin.Begin);

            return bodyAsText;
        }



        private async Task<Stream> ConvertStringToStreem(string Text)
        {

            var TextAsBytes = Encoding.UTF8.GetBytes(Text);

            return new MemoryStream(TextAsBytes);

        }



        public void OnResultExecuting(ResultExecutingContext context)
        {
            // No Need
        }

        public async void OnResultExecuted(ResultExecutedContext context)
        {
            var Response = context.HttpContext.Response;

            string Body = await FormatResponse(Response); // Must Be Stringifed List Of String

            RSA_Helper RSA = new RSA_Helper();

            // var Encrypted = RSA.Encrypt(Body); // To Be Removed
            var EncryptList = RSA.EncryptToStringList(Body);

            var EncryptString = JsonConvert.SerializeObject(EncryptList);

            Response.Body = await ConvertStringToStreem(EncryptString);
        }
    }
}
