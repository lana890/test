
using EskaCMS.Categories.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Categories.Services.Interfaces
{
    public interface ICategories
    {
        Task<bool> SaveCategory(CategoryViewModel category);
        Task<List<CategoryViewModel>> GetAllCategories();
        Task<List<CategoryViewModel>> GetCategoriesByTypeId(long CategoryTypeId);
        Task<long> DuplicateCategory(CategoryViewModel category);


    }
}
