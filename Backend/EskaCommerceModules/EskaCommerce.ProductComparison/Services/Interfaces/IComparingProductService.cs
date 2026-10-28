using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.ProductComparison.Areas.ProductComparison.ViewModels;
using EskaCommerce.ProductComparison.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCommerce.ProductComparison.Services
{
    public interface IComparingProductService
    {
        Task<AddToComparisonResult> AddToComparison([FromBody] AddToComparisonModel model);

        Task<List<ComparingProductVm>> Remove(long Id);

        void MigrateComparingProduct(long fromUserId, long toUserId);
        Task<List<ComparingProductVm>> GetCompareProducts();
    }
}
