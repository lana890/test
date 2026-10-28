#nullable enable
using EskaCMS.Entities.Entity;
using EskaCommerce.Payments.Models.PayFort;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.ShoppingCart;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Microsoft.AspNetCore.Http;

namespace EskaCommerce.Payments.Services.PayFort.Interfaces
{
    public interface IPayFortConfigurationServices
    {
        Task<object> Request(Cart cart, ConfigurePayFort stripSettings, Order order, EPlatformSource? device, string? CP_URL, string? sdk_token,int type,long? CartId);
        bool VerifyResponseSigneture(IFormCollection form, string respnse_phrase);
       string GenerateSha256Signature(PayFortRequestViewModel model, string phrase);
       string GenerateSha256Signature(IDictionary<string, string> parameters, string phrase);

    }
}
