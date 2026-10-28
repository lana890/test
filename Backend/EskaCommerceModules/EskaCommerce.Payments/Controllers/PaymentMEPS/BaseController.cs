using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using EskaCommerce.Payments.Helper.PaymentMEPS;
using EskaCommerce.Payments.Models.PaymentMEPS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EskaCommerce.Payments.Controllers.PaymentMEPS
{

    /// <summary>
    /// Abstract controller to group the common methods
    /// </summary>
    public abstract class BaseController : ControllerBase
    {
     
        protected readonly ILogger Logger;
        protected readonly GatewayApiClient GatewayApiClient;
        protected readonly NVPApiClient NVPApiClient;
        protected readonly Boolean isOSPlatformWindows;

        //workaround for session issue on MacOS and Linux
        private static Dictionary<string, string> FakeSession = new Dictionary<string, string>();

        protected Dictionary<string, string> ViewList = new Dictionary<string, string>();


        protected BaseController()
        {
        }

        protected BaseController( GatewayApiClient gatewayApiClient, NVPApiClient nvpApiClient, ILogger logger)
        {
           
            GatewayApiClient = gatewayApiClient;
            Logger = logger;
            NVPApiClient = nvpApiClient;
            isOSPlatformWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        }




        ///// <summary>
        ///// Builds the view data for NVP method.
        ///// </summary>
        ///// <param name="gatewayApiRequest">Gateway API request.</param>
        ///// <param name="response">Response.</param>
        //protected void buildViewDataNVP(GatewayApiRequest gatewayApiRequest, string response)
        //{

        //    ViewBag.Operation = gatewayApiRequest.ApiOperation;
        //    ViewBag.Method = gatewayApiRequest.ApiMethod;
        //    ViewBag.RequestUrl = gatewayApiRequest.RequestUrl;

        //    StringBuilder sb = new StringBuilder();

        //    //remove credentials from parameters before display
        //    gatewayApiRequest.NVPParameters.Remove("apiUsername");
        //    gatewayApiRequest.NVPParameters.Remove("apiPassword");
        //    foreach (var param in gatewayApiRequest.NVPParameters)
        //    {
        //        sb.Append(sb.Length > 0 ? ", " : "");
        //        sb.AppendFormat("{0}={1}", param.Key, param.Value);
        //    }
        //    ViewBag.Payload = sb.ToString();

        //    //split result and add one information per line
        //    sb = new StringBuilder();
        //    foreach (var param in response.Split("&"))
        //    {
        //        sb.AppendLine(param);
        //    }
        //    ViewBag.Response = sb.ToString();
        //}


        /// <summary>
        /// Builds the default view data 
        /// </summary>
        /// <param name="gatewayApiRequest">Gateway API request.</param>
        /// <param name="response">Response.</param>
        protected ViewDataVM buildViewData(GatewayApiRequest gatewayApiRequest, string response)
        {
            ViewDataVM viewDataVM = new ViewDataVM();
            viewDataVM.Operation = gatewayApiRequest.ApiOperation;
            viewDataVM.Method = gatewayApiRequest.ApiMethod;
            viewDataVM.RequestUrl = gatewayApiRequest.RequestUrl;
            viewDataVM.Payload = gatewayApiRequest.Payload;
            viewDataVM.Response = JsonHelper.prettyPrint(response);
            return viewDataVM;
        }

        //Session operations
        protected void setSessionValue(String key, String value)
        {
            removeSessionValue(key);
            if (isOSPlatformWindows)
            {
                this.HttpContext.Session.SetString(key, value);
            }
            else
            {
                FakeSession.Add(key, value);
            }
        }


        protected String getSessionValueAsString(String key)
        {
            String value;
            if (isOSPlatformWindows)
            {
                value = this.HttpContext.Session.GetString(key);
            }
            else
            {
                if (FakeSession.ContainsKey(key))
                {
                    value = FakeSession[key];
                }
                else
                {
                    value = null;
                }
            }
            return value;
        }

        protected void removeSessionValue(String key)
        {
            if (isOSPlatformWindows)
            {
                if (this.HttpContext.Session.Keys.Contains(key))
                {
                    this.HttpContext.Session.Remove(key);
                }
            }
            else
            {
                if (FakeSession.ContainsKey(key))
                {
                    FakeSession.Remove(key);
                }
            }
        }

        /// <summary>
        /// Returns default request id, based on the Activity if it exists or
        /// the trace identifier from http context
        /// </summary>
        /// <returns>The request identifier.</returns>
        protected string getRequestId()
        {
            return Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}
