using EskaCMS.Core.Data;

using EskaCMS.Infrastructure.Data;
using EskaCMS.DynamicForms.Services.IServices;
using EskaCMS.DynamicForms.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EskaCMS.Entities.Entity.Core;
using Microsoft.EntityFrameworkCore;

using System.Collections.Immutable;
using System.Net.NetworkInformation;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.MediaEntity;
using EskaCMS.Media.Models;
using Microsoft.AspNetCore.Identity;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.DynamicForms.Services
{
    public class FormItemService : IFormItemService
    {
        private readonly IRepository<FormItem> _FormItemRepository;
        private readonly IRepository<FormItemDetails> _FormItemDetailsRepository;
        private readonly EskaDCMSDbContext _context;
        private readonly RoleManager<Role> roleManager;
        private readonly IWorkContext _workContext;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<Criteria> _CriteriasRepository;
        private readonly IRepository<FormField> _FormFieldRepository;
        private readonly IRepository<FormItemRole> formItemRoleRepository;
        private readonly IRepository<Category> categoryRepository;
        private readonly IRepository<CategoryDetails> _categoryDeatilsRepository;

        public FormItemService(
            IRepository<FormItem> FormItemRepository,
            IRepository<Criteria> CriteriasRepository,
             IRepository<FormItemDetails> FormItemDetailsRepository,
              EskaDCMSDbContext context, RoleManager<Role> roleManager,
              IRepository<FormField> FormFieldRepository,
              IRepository<FormItemRole> FormItemRoleRepository,
              IRepository<Category> CategoryRepository,
              IWorkContext workContext,
              IUserSiteService userSiteService,
               IRepository<CategoryDetails> categoryDeatilsRepository
            )
        {
            _FormItemDetailsRepository = FormItemDetailsRepository;
            _context = context;
            this.roleManager = roleManager;
            _FormItemRepository = FormItemRepository;
            _CriteriasRepository = CriteriasRepository;
            _FormFieldRepository = FormFieldRepository;
            formItemRoleRepository = FormItemRoleRepository;
            categoryRepository = CategoryRepository;
            _categoryDeatilsRepository = categoryDeatilsRepository;
            _workContext = workContext;
            this.userSiteService = userSiteService;
        }
        public async Task<List<FormItemVM>> GetAllFormItems(long SiteId)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();

                var userRoles = await userSiteService.GetUserRoles(SiteId, userId);
                var FormItemList = await _FormItemRepository.Query()
                    .Where(p => p.SiteId == SiteId && p.Status == EStatus.Published)
                    .Include(p => p.Thumbnail)
                    .Include(x => x.FormItemRoles)
                    .Where(x => x.FormItemRoles.Any() ? x.FormItemRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                    .Select(p => new FormItemVM()
                    {
                        Id = p.Id,
                        CreationDate = p.CreationDate,
                        Status = p.Status,
                        SiteId = p.SiteId,
                        ThumbnailUrl = p.ThumbnailId.HasValue ? p.Thumbnail.PublicUrl : "",
                        ThumbnailId = p.ThumbnailId,
                        Icon = p.Icon,
                        Type = p.Type,
                        Cultures = p.FormItemDetails.Select(x => new FormItemDetailsVM
                        {
                            Name = x.Name
                        ,
                            Description = x.Description
                        ,
                            FormItemId = x.FormItemId
                        ,
                            CultureId = x.CultureId,
                            CultureCode = x.Culture == null ? "" : x.Culture.CultureId
                        }).ToList(),


                    }).OrderByDescending(x => x.Id)
                      .ToListAsync();

                return FormItemList;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<List<FormItemForWebVM>> GetAllFormItemsForWeb(long SiteId, long cultureId, RequestFormItemWebViewModel model)
        {
            try
            {


                var userId = await _workContext.GetCurrentUserId();

                var userRoles = await userSiteService.GetUserRoles(SiteId, userId);

                var query = await _FormItemRepository.Query()
                                                .AsNoTracking()
                                                .Where(p => p.SiteId == SiteId && p.Status == EStatus.Published
                                                && (model.FormItemIds == null || !model.FormItemIds.Any() || model.FormItemIds.Contains(p.Id)))
                                                .Where(x => model.type == null || (x.Type == model.type))
                                                .Include(p => p.Thumbnail)
                                                .Include(x => x.FormItemRoles)
                                                .Include(p => p.FormItemDetails.Where(x => x.CultureId == cultureId))
                                                .Include(p => p.FormFields.Where(x => x.Status == EStatus.Published))
                                                .ThenInclude(p => p.FormFieldDetails.Where(x => x.CultureId == cultureId))
                                                .Include("FormFields.FormFieldDetails.Category")
                                                .Include("FormFields.FormFieldDetails.Category.CategoryDetails")
                                                .Include("FormFields.FormFieldDetails.Group")
                                                .Include("FormFields.FormFieldDetails.DataSource")
                                                .Where(x => x.FormItemRoles.Any() ? x.FormItemRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                                                .OrderByDescending(x => x.Id)
                                                .ToListAsync();

                if (model.type != null && model.type==FormItemType.Survey &&(model.FormItemIds == null|| !model.FormItemIds.Any()))
                {
                    return new List<FormItemForWebVM>();
                }

                var MatrixList = query.Where(x => x.FormItemDetails.Any(p => p.CultureId == cultureId)).
                                       Select(p => new FormItemForWebVM()
                                       {
                                           Id = p.Id,
                                           CultureId = cultureId,
                                           Name = p.FormItemDetails.Where(x => x.CultureId == cultureId)
                                                                .Select(t => t.Name).FirstOrDefault() ?? "",
                                           Description = p.FormItemDetails.Where(x => x.CultureId == cultureId)
                                                                       .Select(t => t.Description).FirstOrDefault() ?? "",
                                           ThumbnailUrl = p.Thumbnail != null ? p.Thumbnail.PublicUrl : "",
                                           ThumbnailId = p.ThumbnailId,
                                           Icon = p.Icon,
                                           Type = p.Type,
                                           AdditionalInfo = p.AdditionalInfo,
                                           FormFields = HandlerFormFields(p.FormFields.Where(x => x.FormFieldDetails.Any(x => x.CultureId == cultureId)).ToList(), cultureId)

                                       }).ToList();

                return MatrixList;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<List<FormItemWithClassificationsVM>> GetAllFormItemsWithClassification(long SiteId, long CultureId)
        {

            var userId = await _workContext.GetCurrentUserId();

            var userRoles = await userSiteService.GetUserRoles(SiteId, userId);

            var MatricesList = await _FormItemRepository.Query()
                 .Where(p => p.SiteId == SiteId && p.Status == EStatus.Published && p.FormItemDetails.Any(x => x.CultureId == CultureId))
                 .Include(p => p.Thumbnail)
                 .Include(p => p.FormFieldGroups).ThenInclude(g => g.FormFieldDetails).ThenInclude(ps => ps.Category).ThenInclude(c => c.CategoryDetails)
                 .Include(p => p.FormFields)
                 .Include(x => x.FormItemRoles)
                 .Where(x => x.FormItemRoles.Any() ? x.FormItemRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                 .OrderByDescending(x => x.Id)
                 .Select(p => new FormItemWithClassificationsVM()
                 {

                     Type = p.Type,
                     Classifications = p.Classifications.Select(cl => new
                     {
                         Id = cl.Id,
                         Name = cl.Name,
                         CriteriaList = cl.CriteriaList.Select(cr => new
                         {
                             Name = cr.Name
                         }).ToList(),

                     }).ToList(),
                     FormFields = new
                     {
                         GroupedFormField = p.FormFieldGroups.Select(g => new
                         {
                             Name = g.Name,
                             FormField = g.FormFieldDetails.Where(p => p.CultureId == CultureId).Select(s => new
                             {
                                 Id = s.Id,
                                 Name = s.Category.CategoryDetails.Select(c => c.Name).FirstOrDefault()
                             }).ToList(),
                         }
                     ).ToList(),
                         UnGroupedFormField = p.FormFields.SelectMany(s => s.FormFieldDetails.Where(p => p.CultureId == CultureId && !p.GroupId.HasValue)
                         .Select(d => new
                         {
                             Id = d.Id,
                             Name = d.Category.CategoryDetails.Where(c => c.CultureId == CultureId).Select(fc => fc.Name).FirstOrDefault(),
                         })).ToList()
                     },
                     FormItemDetails = p.FormItemDetails.Where(x => x.CultureId == CultureId)
                   .Select(x => new
                   {
                       Id = p.Id,
                       CreationDate = p.CreationDate,
                       Status = p.Status,
                       SiteId = p.SiteId,
                       ThumbnailUrl = p.ThumbnailId.HasValue ? p.Thumbnail.PublicUrl : "",
                       ThumbnailId = p.ThumbnailId,
                       Icon = p.Icon,
                       Name = x.Name,
                       Description = x.Description,
                       FormItemId = x.FormItemId,
                       CultureId = x.CultureId,
                       CultureCode = x.Culture == null ? "" : x.Culture.CultureId
                   }).FirstOrDefault(),


                 }).ToListAsync();

            return MatricesList;

        }
        public async Task<FormItemVM> GetFormItemsById(long FormItemId)
        {
            try
            {

                var FormItem = await _FormItemRepository.Query()
                    .Where(p => p.Id == FormItemId)
                    .Include(x => x.FormItemDetails)
                    .Include("FormItemDetails.Culture")
                    .Include(p => p.Thumbnail)
                    .Include(s => s.FormFieldGroups)
                    .Include(g => g.FormFields)
                    .ThenInclude(s => s.FormFieldDetails)
                    .ThenInclude(x => x.Category)
                    .ThenInclude(x => x.CategoryDetails)
                    .Select(p => new FormItemVM()
                    {
                        Id = p.Id,

                        Cultures = p.FormItemDetails.Select(x => new FormItemDetailsVM
                        {
                            CultureId = x.CultureId,
                            CultureCode = x.Culture == null ? "" : x.Culture.CultureId,
                            Description = x.Description,
                            FormItemId = x.FormItemId,
                            Name = x.Name
                        }).ToList(),
                        CreationDate = p.CreationDate,
                        Status = p.Status,
                        SiteId = p.SiteId,
                        ThumbnailUrl = p.ThumbnailId.HasValue ? p.Thumbnail.PublicUrl : "",
                        ThumbnailId = p.ThumbnailId,
                        FormFieldNames = p.FormFields.Where(s => s.Status == EStatus.Published).Select(s => s.FormFieldDetails.Select(x => x.Category).SelectMany(x => x.CategoryDetails).Select(x => x.Name).ToList()).FirstOrDefault() ?? new List<string>(),
                        FormField = p.FormFields.Where(s => s.Status == EStatus.Published).ToList(),
                        Icon = p.Icon,
                        AdditionalInfo = p.AdditionalInfo,
                        FormFieldGroups = p.FormFieldGroups,
                        Type = p.Type


                    })
                    .FirstOrDefaultAsync();

                return FormItem;


            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public async Task<FormItemVM> UpsertFormItems(UpsertFormItemVM Model, long UserId, long SiteId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var DBTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                    bool IsEditMode = Model.Id != 0;
                    var FormItem = IsEditMode
                              ? await _FormItemRepository.Query()
                       .Include(x => x.FormItemDetails)
                       .Where(p => p.Id == Model.Id)
                       .FirstOrDefaultAsync()
                              : new FormItem();

                    FormItem.ThumbnailId = Model.ThumbnailId;
                    FormItem.Icon = Model.Icon;
                    FormItem.Type = Model.Type;


                    if (IsEditMode)
                    {
                        FormItem.ModifiedById = UserId;
                        FormItem.ModificationDate = DateTime.Now;
                    }
                    else
                    {
                        FormItem.SiteId = SiteId;
                        FormItem.Status = EStatus.Published;
                        FormItem.CreatedById = UserId;
                        FormItem.CreationDate = DateTime.Now;

                        _FormItemRepository.Add(FormItem);
                    }

                    await _FormItemRepository.SaveChangesAsync();
                    IList<FormItemDetails> FormItemDetails = new List<FormItemDetails>();
                    if (IsEditMode)
                    {

                        var PicingDetails = await _FormItemDetailsRepository.Query().Where(x => x.FormItemId == Model.Id).ToListAsync();

                        _FormItemDetailsRepository.RemoveRange(PicingDetails);
                        await _FormItemDetailsRepository.SaveChangesAsync();



                    }

                    foreach (var PricingFormItem in Model.Cultures)
                    {
                        FormItemDetails.Add(new FormItemDetails()
                        {
                            Description = PricingFormItem.Description,
                            Name = PricingFormItem.Name,
                            FormItemId = FormItem.Id,
                            CultureId = PricingFormItem.CultureId
                        });


                    }
                    _FormItemDetailsRepository.AddRange(FormItemDetails);




                    await _FormItemDetailsRepository.SaveChangesAsync();
                    await DBTrans.CommitAsync();

                    return await GetFormItemsById(FormItem.Id);
                }

                catch (Exception e)
                {
                    await DBTrans.RollbackAsync();
                    throw e;
                }
            });
        }
        public async Task<bool> UpdateFormItemsStatus(long FormItemId, EStatus NewStatus, long UserId)
        {
            try
            {


                var FormItem = await _FormItemRepository.Query()
                 .Where(p => p.Id == FormItemId)
                 .FirstOrDefaultAsync();

                FormItem.ModificationDate = DateTime.Now;
                FormItem.ModifiedById = UserId;
                FormItem.Status = NewStatus;

                await _FormItemRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        #region helpers
        public async Task<bool> UpdateFormItemsAdditionalInfo(long FormItemId, string additionalInfo)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();

                var FormItem = await _FormItemRepository.Query()
                 .Where(p => p.Id == FormItemId && p.SiteId == siteId)
                 .FirstOrDefaultAsync();

                FormItem.ModificationDate = DateTime.Now;
                FormItem.AdditionalInfo = additionalInfo;
                FormItem.ModifiedById = userId;


                await _FormItemRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        private IList<FormFieldForWebVM> HandlerFormFields(List<FormField> pricingFormField, long cultureId)
        {
            try
            {
                var List = new List<FormFieldForWebVM>(pricingFormField.Count);

                foreach (var item in pricingFormField)
                {
                    var FormFieldDetails = item.FormFieldDetails.FirstOrDefault();

                    var categoryName = FormFieldDetails?.Category.CategoryDetails?.FirstOrDefault(x => x.CultureId == cultureId)?.Name ?? "";

                    List.Add(new FormFieldForWebVM
                    {
                        ChildrenCategories = item.FormFieldDetails.Any() ? HandlerCategory(item.FormFieldDetails
                            .Select(x => new ChildCategoriesVM
                            {
                                Id = x.Category.Id,
                                ParentId = x.Category.ParentId,
                                Status = x.Category.Status,
                                CategoryDetails = x.Category.CategoryDetails
                            })
                            .Where(x => x.Status != EStatus.Deleted)
                            .ToList(), cultureId) : new List<ChildCategoriesVM>(),
                        Status = item.Status,
                        Id = item.Id,
                        InputType = item.InputType,
                        FormItemId = item.FormItemId.HasValue ? item.FormItemId.Value : 0,
                        Name = categoryName,
                        Group = item.FormFieldDetails.Any() ? item.FormFieldDetails.Select(x => x.Group).FirstOrDefault() ?? new FormFieldGroup() : new FormFieldGroup(),
                        ClassesNames = FormFieldDetails?.ClassesNames ?? "",
                        Validations = FormFieldDetails?.Validations ?? "",
                        DataSourceId = FormFieldDetails?.DataSourceId,
                        DataSource = FormFieldDetails?.DataSource ?? new ApiMapper(),
                        ParentCategoryId = FormFieldDetails.CategoryId,
                        AdditionalInfo = FormFieldDetails?.AdditionalInfo ?? "",
                        PlaceHolder = FormFieldDetails?.PlaceHolder ?? "",
                        OneOfOptionsType = item.OneOfOptionsType,
                        StaticApiLink = FormFieldDetails?.StaticApiLink ?? ""
                    });
                }

                return List;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private List<ChildCategoriesVM> HandlerCategory(List<ChildCategoriesVM> categories, long cultureId)
        {
            try
            {

                var resultList = new List<ChildCategoriesVM>();

                var categoryIds = categories
                    .Where(x => EStatus.Deleted != x.Status)
                    .SelectMany(x => x.CategoryDetails)
                    .Where(x => x.CultureId == cultureId)
                    .Select(x => x.CategoryId)
                    .ToList();

                var getChildes = categoryRepository
                    .Query()
                    .Include(x => x.CategoryDetails)
                    .Where(x => categoryIds.Contains(x.ParentId ?? 0) &&
                                EStatus.Deleted != x.Status)
                    .SelectMany(x => x.CategoryDetails)
                    .Where(x => x.CultureId == cultureId)
                    .AsNoTracking()
                    .Select(x => new ChildCategoriesVM
                    {
                        Name = x.Name,
                        Id = x.CategoryId,
                        ParentId = x.Category.ParentId,
                        Status = x.Category.Status,
                        SiteId = x.Category.SiteId,
                        ThumbnailImageId = x.Category.ThumbnailImageId,
                        CategoryDetails = x.Category.CategoryDetails,
                        CategoryTypeId = x.Category.CategoryTypeId
                    })
                    .ToList();

                resultList.AddRange(getChildes);

                return resultList;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task AssignFormItemPermissions(RequestFormItemRoleViewModel model)
        {
            try
            {
                var UserId = await _workContext.GetCurrentUserId();

                var oldmediaRoleToBeDeleted = await formItemRoleRepository.Query().Where(x => x.FormItemId == model.FormItemId).ToListAsync();
                if (oldmediaRoleToBeDeleted != null && oldmediaRoleToBeDeleted.Count() > 0)
                {

                    formItemRoleRepository.RemoveRange(oldmediaRoleToBeDeleted);
                    await formItemRoleRepository.SaveChangesAsync();

                }
                var AddFormRole = model.RoleIds.ConvertAll(x => new FormItemRole
                {
                    RoleId = x,
                    CreationDate = DateTime.Now,
                    CreatedById = UserId,
                    FormItemId = model.FormItemId,
                });

                formItemRoleRepository.AddRange(AddFormRole);
                await formItemRoleRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<FormItemAssignedRolesVM> AssignedFormItemRoles(long id)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var FormItemAssignedRoles = await formItemRoleRepository.Query()
                                                         .Include(x => x.Role)
                                                         .Where(x => x.FormItemId == id && x.FormItem.SiteId == siteId)
                                                         .Include(x => x.Role)
                                                         .Select(x => new Role
                                                         {
                                                             Id = x.Role.Id,
                                                             Name = x.Role.Name,
                                                             Type = x.Role.Type,
                                                         })
                                                         .ToListAsync();

                var assignedRoleIds = FormItemAssignedRoles.Select(x => x.Id).ToList();

                var unassingedRoles = await roleManager.Roles.Where(x => (x.SiteId == siteId || x.SiteId == null) && !assignedRoleIds.Contains(x.Id)).Select(x =>
                      new Role
                      {
                          Id = x.Id,
                          Name = x.Name,
                          Type = x.Type,
                      }).ToListAsync();

                var result = new FormItemAssignedRolesVM { AssingedRoles = FormItemAssignedRoles, UnassingedRoles = unassingedRoles };

                return result;



            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

    }
}
