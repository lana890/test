
//using EskaCMS.Components.Models;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Interfaces;
using EskaCMS.Core.Services;
using EskaCMS.Elements.Services.IServices;
using EskaCMS.Elements.ViewModels;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Module.Core.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Elements;

namespace EskaCMS.Elements.Services
{
    public class ElementService : IElementService
    {
        private readonly ISiteCultureService _siteCultureService;
        private readonly IRepository<Category> _categoryRepository;
        private Repository<ShareableElement> _SharableElementsRepository;

        private string  _Pattern = @"#$w*\$\d*\$d*\$#";
        private string  _CodeTemplate = "#${0}${1}${2}$#"; // where  {0} is Element Type  , {1}  is ElementId , {2} Cultureid

        public ElementService(Repository<ShareableElement> SharableElementsRepository ,
            ISiteCultureService siteCultureService,

            IRepository<Category> CategoryRepository
          )
        {
            _SharableElementsRepository = SharableElementsRepository;
            _siteCultureService = siteCultureService;
            _categoryRepository = CategoryRepository;
        }


        public async Task<List<ShareableElement>> ShareElements(List<ElementsVM> Elements,long SiteId)
        {

            return await _SharableElementsRepository.ExecuteInTransactionAsync(async () =>
            {
                {

                    List<ShareableElement> result = new List<ShareableElement>();

                    foreach (var Element in Elements)
                    {
                        var SiteCultures = await _siteCultureService.GetSiteCultures(SiteId);
                        long ParentId = 0;
                        if (Element.CultureId.HasValue && Element.CultureId.Value != 0 )
                        {
                            foreach (var Culture in SiteCultures)
                            {
                                ShareableElement _Element = new ShareableElement();
                                if (Element.Id != 0)
                                {
                                    _Element = _SharableElementsRepository.Query().Where(s => s.Id == Element.Id).FirstOrDefault();

                                    if (_Element == null)
                                    {
                                        throw new Exception("Element Not Found");
                                    }
                                    _Element.Name = Element.Name;
                                }
                                else
                                {

                                    _Element.Name = Element.Name + " - " + Culture.Culture.Split('-')[0];
                                }


                                _Element.Status = EStatus.Published;
                                _Element.CultureId = Culture.Id.Value;
                                _Element.ElementId = Element.ElementId.HasValue ? Element.ElementId.Value : 0;
                                _Element.ElementTypeId = Element.ElementTypeId;
                                _Element.Settings = Element.Settings;
                                _Element.SiteId = SiteId;
                                _Element.DataKey = Element.DataKey;


                                _SharableElementsRepository.Add(_Element);
                                await _SharableElementsRepository.SaveChangesAsync();

                                string code = "";
                                if (Element.CultureId != null)
                                {
                                    if (Element.CultureId == Culture.Id)
                                    {
                                        code = Element.IdentifierCode;
                                    }
                                    else
                                    {
                                        code = Element.IdentifierCode + _Element.Id;

                                    }
                                }
                                else
                                {
                                    if (ParentId == 0)
                                    {
                                        code = Element.IdentifierCode;
                                    }
                                    else
                                    {
                                        code = Element.IdentifierCode + _Element.Id;
                                    }
                                }
                                _Element.IdentifierCode = code;
                                _Element.ParentId = ParentId == 0 ? _Element.Id : ParentId;
                                ParentId = ParentId == 0 ? _Element.Id : ParentId;

                                result.Add(_Element);

                            }

                        }
                        else
                        {  
                            ShareableElement _Element = new ShareableElement();
                            if (Element.Id != 0)
                            {
                                _Element = _SharableElementsRepository.Query().Where(s => s.Id == Element.Id).FirstOrDefault();

                                if (_Element == null)
                                {
                                    throw new Exception("Element Not Found");
                                }
                                _Element.Name = Element.Name;
                            }
                            else
                            {

                                _Element.Name = Element.Name ;
                            }


                            _Element.Status = EStatus.Published;
                            _Element.CultureId = null;
                            _Element.ElementId = Element.ElementId.HasValue ? Element.ElementId.Value : 0;
                            _Element.ElementTypeId = Element.ElementTypeId;
                            _Element.Settings = Element.Settings;
                            _Element.SiteId = SiteId;
                            _Element.DataKey = Element.DataKey;


                            _SharableElementsRepository.Add(_Element);
                            await _SharableElementsRepository.SaveChangesAsync();

                            string code = "";
                            
                            if (ParentId == 0)
                            {
                                code = Element.IdentifierCode;
                            }
                            else
                            {
                                code = Element.IdentifierCode + _Element.Id;
                            }
                            
                            _Element.IdentifierCode = code;
                            _Element.ParentId = ParentId == 0 ? _Element.Id : ParentId;
                            ParentId = ParentId == 0 ? _Element.Id : ParentId;

                            result.Add(_Element);

                        }
                    }
                    await _SharableElementsRepository.SaveChangesAsync();
                    return result;
                }
            });
            
        }


        public async Task<bool> UpdateSharedElementSettings(ElementsVM Element)
        {
            var ElementObj = await _SharableElementsRepository.Query()
                                                        .Where(e=>e.Id == Element.Id )
                                                        .FirstOrDefaultAsync();
          
            ElementObj.Settings = Element.Settings;
            
           

            await _SharableElementsRepository.SaveChangesAsync();


            return true;
        }



        public async Task<bool> RemoveSharedElement(long ElementId)
        {
            var ElementObj = _SharableElementsRepository.Query().Where(e => e.Id == ElementId).FirstOrDefault();

            if (ElementObj == null)
            {
                throw new Exception("Element Not Found");
            }
            ElementObj.Status = EStatus.Deleted;
            await _SharableElementsRepository.SaveChangesAsync();


            return true;
        }

        public async Task<ShareableElement> GetSharedElementById(long ElementId)
        {
            var ElementObj = _SharableElementsRepository.Query()
                                                        .Where(e => e.Id == ElementId)
                                                        .Include(e=>e.Category)
                                                        .FirstOrDefault();

            if (ElementObj == null)
            {
                throw new Exception("Element Not Found");
            }
            
            return ElementObj;
        }

        public async Task<string> GetElementSettingsByCode(string Code , long? Culture)
        {
            var SettingsObj = _SharableElementsRepository.Query()
                                                        .Where(e => e.IdentifierCode == Code && (e.CultureId == Culture || Culture==null))
                                                        .Select(e => e.Settings)
                                                        .FirstOrDefault();

            if (string.IsNullOrEmpty(SettingsObj))
            {
                throw new Exception("Element Not Found");
            }

            return SettingsObj;
        }

        public async Task<string> ReplaceSharedCodesWithSettings(string Settings)
        {
            var ElementDictionary = _SharableElementsRepository.Query().Where(e=>e.Status != EStatus.Deleted)
                .ToDictionary(ele=> ele.IdentifierCode , ele => ele.Settings);

            if (ElementDictionary.Count ==0)
            {
                return Settings;
            }
            string settings = "\"  #$row$10$22$# \""; // for testing 
            var x= Regex.Replace(settings, _Pattern,Match=>
            {
                return ElementDictionary[Match.Value];
            });
            return settings;
        }

        public async Task<ElementsCategoriesViewModel> GetAllSheardElements(long SiteId,long? CultureId)
        {
            var data = new ElementsCategoriesViewModel();
            var Category = await GetCategoriesById((long)ECategory.SharedElements, CultureId);


            var ElementsList = _SharableElementsRepository
                                        .Query()
                                        .AsNoTracking()
                                        .Where(e => e.Status != EStatus.Deleted &&
                                                    e.SiteId == SiteId &&
                                                    (e.CultureId == CultureId || e.CultureId == null || CultureId== null))
                                        .Include(e=>e.Category)
                                        .Select(e=>new ElementsVM()
                                        {
                                            CultureId = e.CultureId,
                                            ElementId = e.ElementId,
                                            Id = e.Id,
                                            ElementTypeId = e.ElementTypeId,
                                            IdentifierCode = e.IdentifierCode,
                                            ParentId = e.ParentId.HasValue?e.ParentId.Value :0,
                                            SiteId = e.SiteId,
                                            Settings = e.Settings,
                                            DataKey=e.DataKey,
                                            Name=e.Name,
                                            CategoryId=e.CategoryId,
                                            Category=e.Category
                                        })
                                        .ToList();

            data.Components = ElementsList;
            data.Category = Category;

            return data;
        }

        public async Task<Dictionary<string, string>> GetAllSheardElementsSettings(long SiteId, long CultureId)
        {

            var ElementsList = _SharableElementsRepository
                                        .Query()
                                        .Where(e => e.Status != EStatus.Deleted &&
                                                    e.SiteId == SiteId &&
                                                    e.CultureId.Value == CultureId || !e.CultureId.HasValue)
                                        .ToDictionary(ele => ele.IdentifierCode, ele => ele.Settings);

            return ElementsList;
        }


        #region Helper Functions

        private string GenerateCode(string ElementTypeId ,long Id , long CultureId)
        {
            return string.Format(_CodeTemplate, ElementTypeId,Id, CultureId);

        }

        private async Task<CategoryElementViewModel> GetCategoriesById(long CategoryId,long? CultureId)
        {
            try
            {
                var Result = await _categoryRepository
                                        .Query()
                                        .Where(x => x.Id == CategoryId)
                                        .Include(x => x.CategoryDetails)
                                        .Include(x=>x.ThumbnailImage)
                                        .Include("CategoryDetails.SlugInfo")
                                        .Where(x=>x.CategoryDetails.Any(x => x.Culture == null || x.CultureId == CultureId))
                                        .Select(Category => new CategoryElementViewModel
                                        {
                                            Id = Category.Id,
                                            IncludeInMenu = Category.IncludeInMenu,
                                            ParentId = Category.ParentId,
                                            DisplayOrder = Category.DisplayOrder,
                                            Status = Category.Status,
                                            CategoryTypeId = (ECategoryTypes)Category.CategoryTypeId,
                                            MediaCategory = Category.ThumbnailImage==null?new MediaViewModel(): new MediaViewModel
                                            {
                                                FileName = Category.ThumbnailImage.Filename,
                                                Id = Category.ThumbnailImageId,
                                                PublicUrl = Category.ThumbnailImage.PublicUrl
                                            },
                                           
                                            MetaDescription = Category.CategoryDetails.OrderByDescending(x=>x.CultureId).FirstOrDefault().MetaDescription,
                                            MetaKeywords = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().MetaKeywords,
                                            MetaTitle = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().MetaTitle,
                                            CategoryTemplateId = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().CategoryTemplateId == null ? 0 : Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().CategoryTemplateId.Value,
                                            Slug = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().SlugInfo == null ? String.Empty : Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().SlugInfo.Slug,

                                            Description = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Description,

                                            CultureCode = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Culture ==null?"": Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Culture.CultureId,
                                            CultureId = Category.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().CultureId,


                                            
                                        }).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                return Result;
            }
            catch (Exception exc)
            {

                throw exc;
            }

        }

        #endregion

    }
}
