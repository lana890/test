using EskaCMS.Core.Areas.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public interface ILoyalityService
    {
        Task<CustomerLoyalityObjectVm> GetCustomer(string Phone);
        Task<dynamic> SaveCustomer(string CustomerName, string Phone, string Email);
        Task<dynamic> InsertTransaction(string Email, string Phone, decimal TotalNetAmount, decimal UsedCash
            , decimal UsedPoints, string CurrencyCode);
        Task<string> GenerateQRCode(string Phone);
    }
}
