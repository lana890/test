using EskaCommerce.Zoho.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Zoho.Services
{
  public  interface IZohoService
    {
        // Task AccessTokenRequest(string AuthorizationCode);
        Task<AccessTokenResponseVM> AuthorizationRequest();
    }
}
