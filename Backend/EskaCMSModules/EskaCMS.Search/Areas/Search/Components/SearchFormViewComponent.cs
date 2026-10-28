using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web;
using EskaCommerce.Search.Areas.Search.ViewModels;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Core;

using EskaCMS.Entities.Entity;

namespace EskaCommerce.Search.Areas.Search.Components
{
    public class SearchFormViewComponent : ViewComponent
    {
        private IRepository<Category> _categoryRepository;

        public SearchFormViewComponent(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public IViewComponentResult Invoke()
        {
            var model = new SearchForm();
            model.AvailableCategories = _categoryRepository
                .Query()
                .Include(x=>x.CategoryDetails)
                .ThenInclude(x=>x.SlugInfo)
                .Where(x => x.Status!= EskaCMS.Entities.Enum.GeneralEnums.EStatus.Deleted && x.Parent == null)
                .SelectMany(x=>x.CategoryDetails)
                
                .Select(x => new SelectListItem
                {
                    Value = x.SlugInfo.Slug,
                    Text = x.Name
                }).ToList();

            model.Query = Request.Query["query"];
            model.Category = Request.Query["category"];

            return View(this.GetViewPath(), model);
        }
    }
}
