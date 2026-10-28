using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Security.Services
{
    public class OptionService : IOptionService
    {
        private readonly IRepository<Option> _optionRepository;
        private readonly IRepository<Category> categoryrepository;

        public OptionService(IRepository<Option> optionRepository, IRepository<Category> Categoryrepository)
        {
            _optionRepository = optionRepository;
            categoryrepository = Categoryrepository;
        }




        public async Task<List<OptionLevelViewModel>> GetOptionsLevels(long CategoryTypeId , long? SiteId, List<long> UserOptionsIds = null)
        {
            try
            {
                var levels = new List<OptionLevelViewModel>();
                var Categories = await GetCategoriesByTypeId((ECategoryTypes)CategoryTypeId, SiteId, 0, null, true);

                foreach (var item in Categories)
                {
                    var data = new OptionLevelViewModel();
                    data.Level = item;
                    data.OptionsList = await _optionRepository.Query()
                                    .Where(x => x.CategoryId == item.Id
                                            && (UserOptionsIds == null || UserOptionsIds.Contains(x.Id)))
                                    .Select(x => new OptionRequestViewModel
                                    {
                                        Id = x.Id,
                                        Name = x.Name,
                                        Code = x.Code,
                                        CategoryId = x.CategoryId,
                                        Type = x.Type
                                    }).ToListAsync();

                    var sublevels = new List<OptionLevelViewModel>();
                    var subCategories = await GetCategoriesByTypeId((ECategoryTypes)CategoryTypeId, SiteId, item.Id, 0);
                    data.SubLevelList = new List<OptionLevelViewModel>();
                    foreach (var subitem in subCategories)
                    {
                        var data2 = new OptionLevelViewModel();
                        data2.Level = subitem;
                        data2.OptionsList = await _optionRepository.Query()
                                    .Where(x => x.CategoryId == subitem.Id
                                     && (UserOptionsIds == null || UserOptionsIds.Contains(x.Id)))
                                    .Select(x => new OptionRequestViewModel
                                    {
                                        Id = x.Id,
                                        Name = x.Name,
                                        Code = x.Code,
                                        CategoryId = x.CategoryId,
                                        Type = x.Type
                                    }).ToListAsync();

                        data.SubLevelList.Add(data2);
                    }
                    levels.Add(data);
                }

                return levels;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }


        public async Task<bool> CreateAsync(AddOptionViewModel Option)
        {
            try
            {
                foreach (var item in Option.lstOptions)
                {
                    var objOption = new Option();
                    objOption.SiteId = Option.SiteId;
                    objOption.CreatedById = Option.UserId;
                    objOption.CreationDate = DateTimeOffset.Now;
                    objOption.Name = item.Name;
                    objOption.Code = item.Code;
                    objOption.CategoryId = item.CategoryId;
                    objOption.Type = EDefinitionType.UserDefined;
                    _optionRepository.Add(objOption);
                    await _optionRepository.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<bool> EditOption(AddOptionViewModel Option)
        {
            try
            {
                foreach (var item in Option.lstOptions)
                {
                    var objOption = _optionRepository.Query().Where(x => x.Id == item.Id).FirstOrDefault();
                    objOption.ModifiedById = Option.UserId;
                    objOption.ModificationDate = DateTimeOffset.Now;
                    objOption.Name = item.Name;
                    objOption.Code = item.Code;
                    _optionRepository.Update(objOption);
                    await _optionRepository.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        public async Task<List<Option>> GetBySiteAsync(long? SiteId)
        {
            try
            {

                return await _optionRepository.Query().Where(x => x.SiteId == SiteId || x.SiteId == null).ToListAsync();
            }
            catch (Exception exc)
            {

                throw exc;

            }
        }

        public async Task<bool> DeleteAsync(long OptionId)
        {
            try
            {
                var Option = await _optionRepository.Query().Where(x => x.Id == OptionId).FirstOrDefaultAsync();
                _optionRepository.Remove(Option);
                await _optionRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {

                throw exc;

            }
        }
        public async Task<List<CategoryAdminViewModel>> GetCategoriesByTypeId(ECategoryTypes CategoryTypeId, long? SiteId, long? ParentId, long? CultureId, bool WithShared = false)
        {
            try
            {
                var Result = new List<CategoryAdminViewModel>();
                var Query = categoryrepository.Query()
                                .Include(c => c.CategoryDetails)
                                .Include(x => x.ThumbnailImage)
                                .Include("CategoryDetails.SlugInfo")
                                .Where(x => x.CategoryTypeId == (long)CategoryTypeId
                                       && (x.SiteId == SiteId || SiteId == 0 || WithShared)
                                       && x.Status != EStatus.Deleted)
                                .Where(x => x.CategoryDetails.Any(x => (CultureId == null || CultureId == 0 || x.CultureId == CultureId)));


                Query = ParentId == 0
                                ? Query.Where(c => c.ParentId == null)
                                : Query.Where(c => c.ParentId == ParentId);

                Result = await Query.Select(
                            Category => new CategoryAdminViewModel
                            {
                                Id = Category.Id,
                                IncludeInMenu = Category.IncludeInMenu,
                                ParentId = Category.ParentId,
                                DisplayOrder = Category.DisplayOrder,
                                Status = Category.Status,
                                CategoryTypeId = Category.CategoryTypeId,
                                ThumbnailImage = new MediaViewModel
                                {
                                    FileName = Category.ThumbnailImage.Filename,
                                    Id = Category.ThumbnailImageId,
                                    PublicUrl = Category.ThumbnailImage.PublicUrl
                                },
                                Name = Category.CategoryDetails.Select(x => x.Name).FirstOrDefault()
                            }).OrderByDescending(x => x.Id).ToListAsync();


                return Result;

            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

    }
}
