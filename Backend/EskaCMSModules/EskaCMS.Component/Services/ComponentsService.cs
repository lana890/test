
using EskaCMS.Components.Models;
using EskaCMS.Components.Services.Interfaces;
using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.Components.Services
{
    public class ComponentService : IComponent
    {
        private readonly IRepository<Category> _categoriesRepository;
        private readonly IRepository<CategoryDetails> _categoryDetailsRepository;
        private readonly IRepository<EskaCMS.Entities.Entity.Components.Component> _componentRepository;
        private readonly IWorkContext _workContext;
        private readonly EskaDCMSDbContext _context;
        private readonly ISlugServices _slugServices;

        public ComponentService(IRepository<Category> categoriesRepository,
            IRepository<CategoryDetails> CategoryDetailsRepository,
                                IRepository<EskaCMS.Entities.Entity.Components.Component> componentRepository,
                               IWorkContext workContext,
                               EskaDCMSDbContext context,
                               ISlugServices slugServices)
        {
            _categoriesRepository = categoriesRepository;
            _categoryDetailsRepository = CategoryDetailsRepository;
            _componentRepository = componentRepository;
            _workContext = workContext;
            _context = context;
            _slugServices = slugServices;
        }
        public async Task<bool> SaveComponent(ComponentViewModel comp)
        {
            try
            {

                if (comp.Id == 0)
                {
                    var new_comp = new EskaCMS.Entities.Entity.Components.Component();

                    new_comp.CreationDate = DateTimeOffset.Now;
                    new_comp.CreatedById = comp.UserId;
                    new_comp.ModifiedById = comp.UserId;

                    new_comp.CategoryId = (long)comp.CategoryId;
                    //new_comp.CompnayId = comp.CompanyId;
                    new_comp.DataKey = comp.DataKey;
                    new_comp.Name = comp.Name;
                    new_comp.Name2 = comp.Name;
                    new_comp.SiteId = comp.SiteId;
                    new_comp.Status = EStatus.Unpublished;
                    new_comp.StatusDate = DateTimeOffset.Now;
                    new_comp.Title = comp.Title;
                    new_comp.TokenId = comp.TokenId;
                    new_comp.Type = EComponentType.Web;

                    _componentRepository.Add(new_comp);
                    await _componentRepository.SaveChangesAsync();
                }
                else
                {
                    var mycomp = await _componentRepository.Query().Where(x => x.Id == comp.Id).FirstOrDefaultAsync();
                    if (mycomp == null)
                        return false;

                    mycomp.ModificationDate = DateTime.Now;
                    mycomp.ModifiedById = comp.UserId;
                    mycomp.CategoryId = (long)comp.CategoryId;
                    //  mycomp.CompnayId = comp.CompanyId;
                    mycomp.DataKey = comp.DataKey;
                    mycomp.Name = comp.Name;
                    mycomp.Name2 = comp.Name;
                    mycomp.SiteId = comp.SiteId;
                    mycomp.Title = comp.Title;
                    mycomp.TokenId = comp.TokenId;
                    if (comp.Type != null)
                        mycomp.Type = (EComponentType)comp.Type;
                    else
                        mycomp.Type = EComponentType.Web;
                    await _componentRepository.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<IList<ComponentsCategoriesViewModel>> GetAllComponents(long siteId)
        {
            try
            {
                var data = new List<ComponentsCategoriesViewModel>();

                //var commonCat = new ComponentsCategoriesViewModel();
                //commonCat.Category = await _CategoriesService.GetCategoriesById((long)ECategory.CommonComponents);// _CategoriesRepository.Query().Where(x => x.CategoryTypeId == (long)ECategoryTypes.Component).FirstOrDefault();
                //commonCat.Components = _componentRepository.Query().Where(y => y.CategoryId == commonCat.Category.Id && y.Status != 3).ToArray();
                //data.Add(commonCat);


                var Categories = await GetCategoriesByTypeId(ECategoryTypes.Component, 0,null, 0);// _CategoriesRepository.Query().Where(x => x.CategoryTypeId == (long)ECategoryTypes.Component && x.Status != EStatus.Deleted && x.Id != 35).ToListAsync();
                Categories = Categories.Where(c => c.Id !=  (long)ECategory.SharedElements).ToList();
                foreach (var item in Categories)
                {
                    var cat = new ComponentsCategoriesViewModel();
                    cat.Category = item;
                    cat.Components = _componentRepository.Query().Where(y => y.CategoryId == item.Id && y.Status != EStatus.Deleted).ToArray();
                    data.Add(cat);
                }

                return data;

            }
            catch (Exception exc)
            {

                throw exc;
            }

        }
        public async Task<ComponentViewModel> GetComponentByID(long id)
        {
            try
            {
                return await _componentRepository.Query().Where(y => y.Id == id).Select(x =>

                new ComponentViewModel
                {
                    TokenId = x.TokenId,
                    Name = x.Name,
                    Name2 = x.Name2,
                    Title = x.Title,
                    DataKey = x.DataKey,
                    Type = (long)x.Type,
                    CategoryId = x.CategoryId,
                    Status = x.Status,
                    StatusDate = x.StatusDate,
                    CreatedById = x.CreatedById,
                    CreationDate = x.CreationDate,
                    ModifiedById = x.ModifiedById,
                    ModificationDate = x.ModificationDate,
                    Settings =  x.Settings

                }
                ).FirstOrDefaultAsync();


            }
            catch (Exception exc)
            {
                throw exc;

            }
        }

        public async Task<EskaCMS.Entities.Entity.Components.Component> DeleteComponent(long userId, int id)
        {
            try
            {
                var comp = _componentRepository.Query().Where(y => y.Id == id).FirstOrDefault();

                comp.Status = EStatus.Deleted; //deleted
                comp.ModifiedById = userId;
                comp.ModificationDate = DateTimeOffset.Now;

                await _componentRepository.SaveChangesAsync();

                return comp;

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        //public async Task<bool> SaveComponentCategory(long siteId, string name, long id, long UserId)
        //{
        //    try
        //    {
        //        if (id != 0)
        //        {
        //            var cat = _CategoriesRepository.Query().Where(y => y.Id == id).FirstOrDefault();
        //            cat.Name = name;
        //            await _CategoriesRepository.SaveChangesAsync();
        //        }
        //        else
        //        {
        //            var cat = new Category();
        //            cat.Name = name;
        //            cat.CreatedById = UserId;
        //           // cat.SiteId = siteId;
        //            cat.Status = 1;
        //            cat.Slug = "no-slug";
        //            cat.CategoryTypeId = 3;//type compomemnt category

        //            _CategoriesRepository.Add(cat);
        //            await _CategoriesRepository.SaveChangesAsync();
        //        }


        //        return true;

        //    }
        //    catch (Exception exc)
        //    {
        //        throw exc;
        //    }
        //}

        //public async Task<Category> DeleteComponentCategory(int userId, int id)
        //{
        //    try
        //    {
        //        var cat = _CategoriesRepository.Query().Where(y => y.Id == id).FirstOrDefault();
        //        cat.Status = 3; //deleted
        //        cat.ModifiedById = userId;
        //        cat.ModificationDate = DateTimeOffset.Now;

        //        await _CategoriesRepository.SaveChangesAsync();

        //        return cat;

        //    }
        //    catch (Exception exc)
        //    {
        //        throw exc;
        //    }
        //}

        public async Task<bool> SaveComponentSettings(string settings, int id)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();
                var comp = _componentRepository.Query().Where(y => y.Id == id).FirstOrDefault();
                comp.Settings =  settings;
                comp.ModifiedById = userId;
                comp.ModificationDate = DateTimeOffset.Now;

                await _componentRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception exc)
            {
                throw exc;
            }

        }

        public async Task<EskaCMS.Entities.Entity.Components.Component> ShareComponentAsync(ComponentViewModel comp)
        {

            try
            {

                if (comp.category.Id == 0)
                {
                    var Category = new CategoryViewModel();
                    Category.Name = comp.category.Name;
                    Category.UserId = comp.UserId;
                    Category.SiteId = comp.SiteId;
                    Category.Slug = "no-slug";
                    Category.CategoryTypeId = ECategoryTypes.Component;
                    comp.category.Id = await AddCategory(Category);
                }



                var new_comp = new EskaCMS.Entities.Entity.Components.Component();

                new_comp.CreationDate = DateTimeOffset.Now;
                new_comp.CreatedById = comp.UserId;// userId;
                new_comp.ModifiedById = comp.UserId;// userId;
                new_comp.CategoryId = comp.category.Id;
                new_comp.DataKey = comp.DataKey;
                new_comp.Name = comp.Name;
                new_comp.Name2 = comp.Name;
                new_comp.SiteId = comp.SiteId;
                new_comp.Status = EStatus.Unpublished;
                new_comp.StatusDate = DateTimeOffset.Now;
                new_comp.Settings = comp.Settings;
                new_comp.Title = _componentRepository.Query().Where(x => x.DataKey == comp.DataKey).FirstOrDefault().Title;
                new_comp.TokenId = comp.TokenId;
                new_comp.Type = EComponentType.Web;

                _componentRepository.Add(new_comp);
                await _componentRepository.SaveChangesAsync();
                return new_comp;





            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<CategoryComponentViewModel>> GetCategoriesByTypeId(ECategoryTypes CategoryTypeId, long? SiteId, long? ParentId, long? CultureId, bool WithShared = false)
        {
            try
            {
                var Query = await _categoriesRepository.Query()
                                .Include(c => c.CategoryDetails)
                                .Include(x=>x.ThumbnailImage)
                                .Include("CategoryDetails.SlugInfo")
                                .Where(x => x.CategoryTypeId == (long)CategoryTypeId
                                       && ((x.ParentId == ParentId) || ParentId == 0)

                                       && (x.SiteId == SiteId || SiteId == 0 || WithShared)
                                       && x.Status != EStatus.Deleted)
                                .Where(x => x.CategoryDetails.Any(x => (CultureId == null || CultureId == 0 || x.CultureId == CultureId)))

                                .Select(
                                  Category => new CategoryComponentViewModel
                                  {
                                      Id = Category.Id,
                                      IncludeInMenu = Category.IncludeInMenu,
                                      ParentId = Category.ParentId,
                                      DisplayOrder = Category.DisplayOrder,
                                      Status = Category.Status,
                                      CategoryTypeId = Category.CategoryTypeId,
                                      Slug = Category.CategoryDetails.Count()<=0  ? String.Empty : Category.CategoryDetails.Select(x=>x.SlugInfo.Slug).FirstOrDefault(),
                                      Name=Category.CategoryDetails.Count>0? Category.CategoryDetails.FirstOrDefault().Name:""

                                   
                                  }).OrderByDescending(x => x.Id).ToListAsync();
                return Query;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<long> AddCategory(CategoryViewModel model)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    long UserId = await _workContext.GetCurrentUserId();
                    long SiteId = _workContext.GetCurrentSiteId();

                    var Category = new Category();

                    Category.SiteId = SiteId;
                    Category.CategoryTypeId =Convert.ToInt64( model.CategoryTypeId);
                    Category.CreatedById = UserId;
                    Category.IncludeInMenu = model.IncludeInMenu;
                    Category.StatusDate = DateTime.Now;
                    Category.CreationDate = DateTime.Now;
                    Category.ParentId = model.ParentId == 0 ? null : model.ParentId;
                    Category.DisplayOrder = model.DisplayOrder;
                    Category.Status = model.Status;
                    _categoriesRepository.Add(Category);
                    await _categoriesRepository.SaveChangesAsync();

                    await InsertCategoryDetails(model, Category.Id);

                    return Category.Id;

                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    throw new Exception(ex.Message.Replace("Category: ", ""));
                }
            });
        }

        private async Task<bool> InsertCategoryDetails(CategoryViewModel category, long id)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();

                var CategoriesDetails = new CategoryDetails();

          

                    var slugInfo = new SlugManagementVm
                    {

                        CultureId = category.CultureId,
                        Entity = EEntity.Category,
                        SiteId = SiteId,
                        Slug = category.Slug


                    };




                CategoriesDetails.CategoryId = id;
                CategoriesDetails.Name = category.Name;
                CategoriesDetails.Description = category.Description;
                CategoriesDetails.MetaTitle = category.MetaTitle;
                CategoriesDetails.MetaDescription = category.MetaDescription;
                CategoriesDetails.Description = category.Description;
                CategoriesDetails.CategoryTemplateId = category.CategoryTemplateId == 0 || category.CategoryTemplateId == null ? null : category.CategoryTemplateId;
                CategoriesDetails.CultureId = category.CultureId;
                CategoriesDetails.SlugInfoId = await _slugServices.UpsertAndGetId(slugInfo);
                CategoriesDetails.Name = category.Name;
                _categoryDetailsRepository.Add(CategoriesDetails);
                    
                    await _categoryDetailsRepository.SaveChangesAsync();



                




                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }      
        
        
        }
    }
}
