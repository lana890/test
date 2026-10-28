#nullable enable
using EskaCMS.Entities.Entity;
using EskaCommerce.Payments.Models.PayFort;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;

namespace EskaCommerce.Payments.Services.PayFort.Interfaces
{
    public interface IPayFortServices
    {
        Task<RedirectResponseViewModel> PayFortTransaction(CartTypes cart, long SiteId,string? CP_URL,int type,long? CartId);
        Task<object> GenerateSdkTokenAsync(CartTypes cart, long SiteId,string device_id,int type,long? CartId);

        // Task<string> PayFortRecipt(PayFortResultViewModel model);
        // Task<ConfigurePayFort> GetPayFortSettingsAsync(long SiteId);

        Task<ConfigurePayFort> GetPayFortSettingsAsync(long siteId, string providerId);

    }
}

