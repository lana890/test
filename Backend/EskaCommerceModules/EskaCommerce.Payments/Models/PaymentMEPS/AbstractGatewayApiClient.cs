using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;

namespace EskaCommerce.Payments.Models.PaymentMEPS
{
    public abstract class AbstractGatewayApiClient
    {

        public ILogger Logger { get; set; }
        public String POST { get; } = "POST";
        public String GET { get; } = "GET";
        public String PUT { get; } = "PUT";

        /// <summary>
        /// Abstract method reesponsible to prepare the request to be sent
        /// </summary>
        /// <returns>The transaction.</returns>
        /// <param name="gatewayApiRequest">Gateway API request.</param>
        public abstract string SendTransaction(GatewayApiRequest gatewayApiRequest);


        public String executeHTTPMethod(GatewayApiRequest gatewayApiRequest)
        {
            string body = String.Empty;

            GatewayApiConfig GatewayApiConfig = gatewayApiRequest.GatewayApiConfig;


            //proxy settings
            if (GatewayApiConfig.UseProxy)
            {
                WebProxy proxy = new WebProxy(GatewayApiConfig.ProxyHost, true);
                if (!String.IsNullOrEmpty(GatewayApiConfig.ProxyUser))
                {
                    if (String.IsNullOrEmpty(GatewayApiConfig.ProxyDomain))
                    {
                        proxy.Credentials = new NetworkCredential(GatewayApiConfig.ProxyUser, GatewayApiConfig.ProxyPassword);
                    }
                    else
                    {
                        proxy.Credentials = new NetworkCredential(GatewayApiConfig.ProxyUser, GatewayApiConfig.ProxyPassword, GatewayApiConfig.ProxyDomain);
                    }
                }
                WebRequest.DefaultWebProxy = proxy;
            }

            // Create the web request
            HttpWebRequest request = WebRequest.Create(gatewayApiRequest.RequestUrl) as HttpWebRequest;

            //http verb
            request.Method = gatewayApiRequest.ApiMethod;

            //content type, json, form, etc
            request.ContentType = gatewayApiRequest.ContentType;

            Logger.LogInformation($@"HttpWebRequest url {gatewayApiRequest.RequestUrl} method {request.Method}");


            //Authentication setting
            if (GatewayApiConfig.AuthenticationByCertificate)
            {

                //custom implementation fo SSL certificate validation callback
                request.ServerCertificateValidationCallback +=
                    (sender, cert, chain, error) =>
                    {
                        return error == SslPolicyErrors.None || (error != SslPolicyErrors.None && GatewayApiConfig.IgnoreSslErrors);
                    };

                //create a new certificate collection
                X509Certificate2Collection certificates = new X509Certificate2Collection();

                //load and add a new certificate loaded from file (p12) 
                certificates.Add(new X509Certificate2(new X509Certificate(GatewayApiConfig.CertificateLocation, GatewayApiConfig.CertificatePassword)));

                //attach certificate to request
                request.ClientCertificates = certificates;

            }
            else
            {
                string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(GatewayApiConfig.Username + ":" + GatewayApiConfig.Password));
                request.Headers.Add("Authorization", "Basic " + credentials);
            }

            //buuld the request
            try
            {
                if ((gatewayApiRequest.ApiMethod == "PUT" || gatewayApiRequest.ApiMethod == "POST") &&
                        !String.IsNullOrEmpty(gatewayApiRequest.Payload))
                {
                    Logger.LogInformation($@"HttpWebRequest payload {gatewayApiRequest.Payload}");

                    // Create a byte array of the data we want to send
                    byte[] utf8bytes = Encoding.UTF8.GetBytes(gatewayApiRequest.Payload);
                    byte[] iso8859bytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding("iso-8859-1"), utf8bytes);

                    // Set the content length in the request headers
                    request.ContentLength = iso8859bytes.Length;

                    // Write request data
                    using (Stream postStream = request.GetRequestStream())
                    {
                        postStream.Write(iso8859bytes, 0, iso8859bytes.Length);
                    }
                }

                // Get response
                try
                {
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        // Get the response stream
                        StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("iso-8859-1"));
                        body = reader.ReadToEnd();
                    }
                }
                catch (WebException wex)
                {
                    Logger.LogDebug($@"Response debug : {wex.Response.Headers}");
                    StreamReader reader = new StreamReader(wex.Response.GetResponseStream(), Encoding.GetEncoding("iso-8859-1"));
                    body = reader.ReadToEnd();
                }

                Logger.LogInformation($@"HttpWebResponse response {body}");

                return body;
            }
            catch (Exception ex)
            {
                return ex.Message + "\n\naddress:\n" + request.Address.ToString() + "\n\nheader:\n" + request.Headers.ToString() + "data submitted:\n" + gatewayApiRequest.Payload;
            }

        }

    }
}
