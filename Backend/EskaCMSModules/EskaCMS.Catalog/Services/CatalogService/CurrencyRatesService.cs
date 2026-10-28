using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Core.Services;
using Microsoft.EntityFrameworkCore;


namespace EskaCommerce.Catalog.Services.CatalogService
{
    public class CurrencyRatesService
    {
        private const string CurrencyRatesEntityTypeId = "CurrencyRates";

       // private readonly IRepository<CurrencyRates> _currencyratesRepository;
        private readonly IEntityService _entityService;

        public CurrencyRatesService( IEntityService entityService)
        {
           // _currencyratesRepository = currencyratesRepository;
            _entityService = entityService;
        }

        //public async Task<IList<CurrencyRatesListItem>> GetAll()
        //{
        //    var currencyrates = await _currencyratesRepository.Query().ToListAsync();
        //    var categoriesList = new List<CurrencyRatesListItem>();
        //    foreach (var currency in currencyrates)
        //    {
        //        var currencyrateListItem = new CurrencyRatesListItem
        //        {
        //            Id = currency.Id,
        //            Name = currency.Name,
        //            Name_2 = currency.Name_2,
        //            CODE = currency.CODE,
        //            IS_DEFAULT_RATE = currency.IS_DEFAULT_RATE,
        //            RATE = currency.RATE
        //        };

        //       // var parentCategory = currencyrateListItem.ParentId;
        //        //while (parentCategory != null)
        //        //{
        //        //    currencyrateListItem.Name = $"{parentCategory.Name} >> {currencyrateListItem.Name}";
        //        //    parentCategory = parentCategory.Parent;
        //        //}

        //        categoriesList.Add(currencyrateListItem);
        //    }

        //    return categoriesList.OrderBy(x => x.Name).ToList();
        //}

        //public async Task Create(Category category)
        //{
        //    using (var transaction = _categoryRepository.BeginTransaction())
        //    {
        //        category.Slug = _entityService.ToSafeSlug(category.Slug, category.Id, CategoryEntityTypeId);
        //        _categoryRepository.Add(category);
        //        await _categoryRepository.SaveChangesAsync();

        //        _entityService.Add(category.Name, category.Slug, category.Id, CategoryEntityTypeId);
        //        await _categoryRepository.SaveChangesAsync();

        //        transaction.Commit();
        //    }
        //}

        //public async Task Update(Category category)
        //{
        //    category.Slug = _entityService.ToSafeSlug(category.Slug, category.Id, CategoryEntityTypeId);
        //    _entityService.Update(category.Name, category.Slug, category.Id, CategoryEntityTypeId);
        //    await _categoryRepository.SaveChangesAsync();
        //}

        //public async Task Delete(Category category)
        //{
        //    await _entityService.Remove(category.Id, CategoryEntityTypeId);
        //    category.IsDeleted = true;
        //    _categoryRepository.SaveChanges();
        //}
    }
}
