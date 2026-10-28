using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Core.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.OptionArea.Interfaces
{
    public interface IProductOptionsServices
    {
        Task<List<long>> AddProductsOptionsValue(List<ProductOptionViewModel> model);
        Task<ItemDetailOptionForAdmin> GetProductOptionsValueByOptionId(long OptionId);
        Task<List<OptionViewModel>> GetOptions();
        Task<OptionViewModel> GetOptionsById(long id);
        Task<bool> AddOptions(List<OptionViewModel> optionsModels);
        Task<List<ProductOptionWithOptionValueViewModel>> GetOptionsForComponent();
        Task<bool> DeleteOptions(long id);
        Task<OptionViewModel> GetOptionsByName(string Name);
        Task<bool> UpdateOptions(List<OptionViewModel> optionsModels);
        Task<List<ItemDetailOptionForAdmin>> GetProductOptionsDetailsWithOrdering(long Id);
        Task<bool> UpdateProductOptionsValueByValueId(List<ValueItemForAdmin> model);
    }
}
