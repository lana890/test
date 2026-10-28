using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.DynamicForms.Services.IServices;
using EskaCMS.DynamicForms.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.DynamicForms.Services
{
    public class FormFieldService : IFormFieldService
    {
        private readonly IRepository<FormField> _FormFieldRepository;
        private readonly IRepository<FormItem> _FormItemRepository;
        private readonly IRepository<FormFieldGroup> _FormFieldGroupRepository;
        private readonly IRepository<FormFieldDetails> _FormFieldDetailsRepository;
        private readonly IRepository<Category> _CategoryRepository;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<CategoryDetails> categoryDetailsRepository;
        private readonly EskaDCMSDbContext _context;
        private readonly ISlugServices SlugServices;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Criteria> _CriteriasRepository;
        private readonly IRepository<Classifications> _ClassificationsRepo;

        public FormFieldService(
            IRepository<FormField> FormFieldRepository,
            IRepository<Criteria> CriteriasRepository,
            IRepository<Category> CategoryRepository,
            IUserSiteService userSiteService,
            IRepository<CategoryDetails> CategoryDetailsRepository,
            IRepository<FormFieldDetails> FormFieldDetailsRepository,
            EskaDCMSDbContext context,
                                    ISlugServices slugServices,
                                    IWorkContext workContext,

            IRepository<FormFieldGroup> FormFieldGroupRepository,
            IRepository<FormItem> FormItemRepository,
            IRepository<Classifications> ClassificationsRepo
            )
        {

            _FormFieldRepository = FormFieldRepository;
            _CategoryRepository = CategoryRepository;
            this.userSiteService = userSiteService;
            categoryDetailsRepository = CategoryDetailsRepository;
            _context = context;
            SlugServices = slugServices;
            _workContext = workContext;
            _FormFieldDetailsRepository = FormFieldDetailsRepository;
            _CriteriasRepository = CriteriasRepository;
            _FormFieldGroupRepository = FormFieldGroupRepository;
            _FormItemRepository = FormItemRepository;
            _ClassificationsRepo = ClassificationsRepo;
        }
        #region FormField Group
        public async Task<List<FormFieldGroup>> GetFormFieldGroups(long FormItemId)
        {
            return await _FormFieldGroupRepository.Query()
                .Where(g => g.FormItemId == FormItemId && g.Status != EStatus.Deleted)
                .Include(x => x.FormFieldDetails)
                .ThenInclude(x => x.Culture)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }
        public async Task<bool> DeleteGroup(long GroupId)
        {
            var Group = await _FormFieldGroupRepository
                                .Query()
                                .Where(g => g.Id == GroupId)
                                .Include(x => x.FormFieldDetails)
                                .ThenInclude(x => x.Culture)

                                .FirstOrDefaultAsync();


            foreach (var Specification in Group.FormFieldDetails)
            {

                Specification.GroupId = null;


            }

            Group.Status = EStatus.Deleted;

            await _FormFieldGroupRepository.SaveChangesAsync();

            await _FormFieldDetailsRepository.SaveChangesAsync();
            await _FormFieldRepository.SaveChangesAsync();


            return true;
        }
        public async Task<List<FormFieldGroup>> UpsertGroup(List<FormFieldGroupVM> model)
        {
            var Result = new List<FormFieldGroup>();
            foreach (var Model in model)
            {

                bool IsEditMode = Model.Id != 0;
                var FormFieldGroup = IsEditMode ? await _FormFieldGroupRepository
                                                    .Query()
                                                    .Where(g => g.Id == Model.Id)
                                                    .FirstOrDefaultAsync() : new FormFieldGroup();
                if (IsEditMode)
                {
                    if (FormFieldGroup == null)
                    {
                        throw new Exception("Not Found");
                    }
                }
                else
                {
                    FormFieldGroup.Status = EStatus.Published;
                }
                FormFieldGroup.FormItemId = Model.FormItemId;
                FormFieldGroup.Name = Model.Name;
                FormFieldGroup.cultureId = Model.cultureId;
                if (!IsEditMode)
                {
                    _FormFieldGroupRepository.Add(FormFieldGroup);
                    Result.Add(FormFieldGroup);
                }


                await _FormFieldGroupRepository.SaveChangesAsync();
            }
            return Result;
        }
        #endregion
        #region FormField
        public async Task<List<FormFieldVM>> GetFormFields(long FormItemId, long SiteId)
        {
            try
            {

                var FormFieldList = await _FormFieldRepository.Query()
                    .Include(x => x.FormItem)
                    .Include(x => x.FormFieldDetails)
                    .Include("FormFieldDetails.DataSource")
                    .Include("FormFieldDetails.Culture")
                    .Include("FormFieldDetails.Group")
                    .Include("FormFieldDetails.Category")
                    .Include("FormFieldDetails.Category.Children.CategoryDetails")
                    .Include("FormFieldDetails.Category.CategoryDetails")
                    .Include("FormFieldDetails.Category.CategoryDetails.SlugInfo")
                    .Include("FormFieldDetails.Category.CategoryDetails.Culture")
                    .Where(p => p.FormItemId == FormItemId && p.SiteId == SiteId && p.Status == EStatus.Published)
            .Select(s => new FormFieldVM()
            {
                Status = s.Status,
                Id = s.Id,
                InputType = s.InputType,
                FormItemId = FormItemId,
                Type = s.FormItem.Type,
                FormFieldDetailsVMs = s.FormFieldDetails.Select(x => new FormFieldDetailsVM
                {

                    FormFieldId = x.FormFieldId,
                    ChildrenCategories = x.Category == null ? new List<ChildDetailsVM>() :
                    x.Category.Children.Where(c => c.Status != EStatus.Deleted).SelectMany(b => b.CategoryDetails).Select(x => new ChildDetailsVM
                    {
                        Name = x.Name,
                        CultureId = x.CultureId.HasValue ? x.CultureId.Value : null,
                        Slug = x.Slug,
                        SlugInfoId = x.SlugInfoId,
                        Id = x.Id,
                        CategoryTemplateId = x.CategoryTemplateId.HasValue ? x.CategoryTemplateId.Value : null,
                        CategoryId = x.CategoryId

                    })
                   .ToList(),
                    GroupId = x.GroupId,
                    ClassesNames = x.ClassesNames,
                    Validations = x.Validations,
                    AdditionalInfo = x.AdditionalInfo,
                    PlaceHolder = x.PlaceHolder,
                    CultureId = x.CultureId,
                    Culture = x.Culture == null ? new SitesCultures() : new SitesCultures
                    {
                        CreatedById = x.Culture.CreatedById,
                        CultureId = x.Culture.CultureId,
                        CultureType = x.Culture.CultureType,
                        CulturePrefix = x.Culture.CulturePrefix,
                        ModificationDate = x.Culture.ModificationDate,
                        ModifiedById = x.Culture.ModifiedById,
                        Status = x.Culture.Status,
                        SiteId = x.Culture.SiteId,
                        CreationDate = x.Culture.CreationDate

                    },
                    ParentCategoryId = x.CategoryId,
                    ParentCategory = x.Category,
                    Name = x.Category == null ? "" : x.Category.CategoryDetails == null ? "" : x.Category.CategoryDetails.Where(y => y.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                    DataSource = x.DataSource,
                    Group = x.Group,
                    DataSourceId = x.DataSourceId,
                    StaticApiLink = x.StaticApiLink,
                    CategoryId = x.CategoryId
                }).ToList(),

                OneOfOptionsType = s.OneOfOptionsType,


            }).OrderByDescending(x => x.Id)
           .ToListAsync();
                return FormFieldList;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public async Task<List<FormFieldVM>> GetNumaricalFormFields(long FormItemId, long SiteId)
        {
            try
            {

                var FormFieldList = await _FormFieldRepository.Query()
                    .Include(x => x.FormFieldDetails)
                    .Include("FormFieldDetails.DataSource")
                   .Include(x => x.FormItem)
                    .Include("FormFieldDetails.Culture")
                    .Include("FormFieldDetails.Group")
                    .Include("FormFieldDetails.Category")
                    .Include("FormFieldDetails.Category.CategoryDetails")
                    .Include("FormFieldDetails.Category.CategoryDetails.SlugInfo")
                    .Include("FormFieldDetails.Category.CategoryDetails.Culture")
                    .Include("FormFieldDetails.Category.Children")
                    .Where(p => p.FormItemId == FormItemId
                                && p.SiteId == SiteId
                                && p.Status == EStatus.Published
                                && (p.InputType == EDynamicCriteriaInputTypes.Number || p.InputType == EDynamicCriteriaInputTypes.Currency))

                    .Select(s => new FormFieldVM()
                    {
                        Status = s.Status,

                        Id = s.Id,
                        InputType = s.InputType,
                        FormItemId = s.FormItemId,
                        Type = s.FormItem.Type,
                        FormFieldDetailsVMs = s.FormFieldDetails.Select(x => new FormFieldDetailsVM
                        {
                            GroupId = x.GroupId,
                            ClassesNames = x.ClassesNames,
                            Validations = x.Validations,
                            PlaceHolder = x.PlaceHolder,
                            CultureId = x.CultureId,
                            ParentCategoryId = x.CategoryId,
                            ParentCategory = x.Category,
                            DataSource = x.DataSource,
                            AdditionalInfo = x.AdditionalInfo,
                            Group = x.Group,
                            Culture = x.Culture,
                            Name = x.Category == null ? "" : x.Category.CategoryDetails == null ? "" : x.Category.CategoryDetails.Where(y => y.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                            ChildrenCategories = x.Category == null ? new List<ChildDetailsVM>() :
                          x.Category.Children.Where(c => c.Status != EStatus.Deleted).SelectMany(x => x.CategoryDetails).Select(x => new ChildDetailsVM
                          {
                              Name = x.Name,
                              CultureId = x.CultureId.HasValue ? x.CultureId.Value : null,
                              Slug = x.Slug,
                              SlugInfoId = x.SlugInfoId,
                              Id = x.Id,
                              CategoryTemplateId = x.CategoryTemplateId.HasValue ? x.CategoryTemplateId.Value : null,
                              CategoryId = x.CategoryId

                          })
                        .ToList(),
                        }).ToList(),


                    }).OrderByDescending(x => x.Id)
                      .ToListAsync();
                return FormFieldList;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<FormFieldVM> GetFormFieldById(long Id)
        {
            try
            {

                var Item = await _FormFieldRepository.Query()
                    .Where(s => s.Id == Id)
                    .Include(x => x.FormFieldDetails)
                    .Include("FormFieldDetails.DataSource")
                     .Include(x => x.FormItem)
                    .Include("FormFieldDetails.Culture")
                    .Include("FormFieldDetails.Group")
                    .Include("FormFieldDetails.Category")
                    .Include("FormFieldDetails.Category.Children.CategoryDetails")
                    .Include("FormFieldDetails.Category.CategoryDetails")
                   .Include("FormFieldDetails.Category.CategoryDetails.SlugInfo")
                   .Include("FormFieldDetails.Category.CategoryDetails.Culture")
                  .Include("FormFieldDetails.Category.Children")
               .Select(s => new FormFieldVM()
               {
                   Status = s.Status,
                   Id = s.Id,
                   InputType = s.InputType,
                   FormItemId = s.FormItemId.HasValue ? s.FormItemId.Value : 0,

                   FormFieldDetailsVMs = s.FormFieldDetails.Select(x => new FormFieldDetailsVM
                   {
                       GroupId = x.GroupId,
                       ClassesNames = x.ClassesNames,
                       Validations = x.Validations,
                       PlaceHolder = x.PlaceHolder,
                       CultureId = x.CultureId,
                       Culture = x.Culture == null ? new SitesCultures() : new SitesCultures
                       {
                           CreatedById = x.Culture.CreatedById,
                           CultureId = x.Culture.CultureId,
                           CultureType = x.Culture.CultureType,
                           CulturePrefix = x.Culture.CulturePrefix,
                           ModificationDate = x.Culture.ModificationDate,
                           ModifiedById = x.Culture.ModifiedById,
                           Status = x.Culture.Status,
                           SiteId = x.Culture.SiteId,
                           CreationDate = x.Culture.CreationDate

                       },
                       AdditionalInfo = x.AdditionalInfo,
                       ChildrenCategories = x.Category == null ? new List<ChildDetailsVM>() :
                  x.Category.Children.Where(c => c.Status != EStatus.Deleted).SelectMany(x => x.CategoryDetails).Select(x => new ChildDetailsVM
                  {
                      Name = x.Name,
                      CultureId = x.CultureId.HasValue ? x.CultureId.Value : null,
                      Slug = x.Slug,
                      SlugInfoId = x.SlugInfoId,
                      Id = x.Id,
                      CategoryTemplateId = x.CategoryTemplateId.HasValue ? x.CategoryTemplateId.Value : null,
                      CategoryId = x.CategoryId

                  })
                .ToList(),
                       Group = x.Group,
                       ParentCategoryId = x.CategoryId,
                       ParentCategory = x.Category,
                       Name = x.Category == null ? "" : x.Category.CategoryDetails == null ? "" : x.Category.CategoryDetails.Where(y => y.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                       DataSource = x.DataSource,
                       DataSourceId = x.DataSourceId,
                       StaticApiLink = x.StaticApiLink,
                       CategoryId = x.CategoryId
                   }).ToList(),

                   OneOfOptionsType = s.OneOfOptionsType,
               })
                    .FirstOrDefaultAsync();

                return Item;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<bool> UpdateFormFieldStatus(long Id, EStatus NewStatus, long UserId)
        {
            try
            {


                var FormField = await _FormFieldRepository.Query()
                 .Where(p => p.Id == Id)
                 .Include(x => x.FormFieldDetails)
                 .ThenInclude(p => p.Category)
                 .Include("FormFieldDetails.Category")
                 .Include("FormFieldDetails.Category.CategoryDetails")
                 .Include("FormFieldDetails.Category.CategoryDetails.SlugInfo")
                 .Include("FormFieldDetails.Category.Children")
                 .FirstOrDefaultAsync();

                FormField.Status = NewStatus;
                foreach (var formfieldDetails in FormField.FormFieldDetails)
                {
                    formfieldDetails.Category.Status = NewStatus;
                    formfieldDetails.Category.StatusDate = DateTime.Now;
                    formfieldDetails.Category.ModifiedById = UserId;

                }

                await _FormFieldRepository.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        public async Task<List<FormFieldVM>> UpsertFormField(List<FormFieldVMs> allFormField, long UserId, long SiteId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                    List<long> newIdsOrUpdatesIds = new List<long>();
                    foreach (var Model in allFormField)
                    {
                        bool IsEditMode = Model.Id != 0;
                        var FormField = IsEditMode
                               ? await _FormFieldRepository.Query()
                                        .Include(x => x.FormFieldDetails)
                                        .Where(p => p.Id == Model.Id)
                                        .FirstOrDefaultAsync()
                               : new FormField();


                        FormField.Status = EStatus.Published;

                        FormField.OneOfOptionsType = Model.OneOfOptionsType;
                        FormField.SiteId = SiteId;

                        if (Model.IsGeneral.HasValue && Model.IsGeneral.Value && Model.FormItemId.HasValue)
                        {
                            throw new Exception("Please this is general specification you have to add it without any matrix");
                        }

                        else if (!Model.IsGeneral.HasValue && !Model.FormItemId.HasValue)
                        {
                            throw new Exception("Please you have to add matrix");
                        }
                        else if (Model.IsGeneral.HasValue && !Model.IsGeneral.Value && !Model.FormItemId.HasValue)
                        {
                            throw new Exception("Please you have to add matrix");
                        }
                        FormField.FormItemId = Model.FormItemId;
                        FormField.IsGeneral = Model.IsGeneral.HasValue ? Model.IsGeneral.Value : false;


                        if (!IsEditMode)
                        {
                            FormField.InputType = Model.InputType;
                            _FormFieldRepository.Add(FormField);
                        }
                        else
                        {
                            if (FormField.InputType == Model.InputType || (FormField.InputType != Model.InputType && CanEditFormFieldType(Model.Id)))
                            {
                                FormField.InputType = Model.InputType;

                            }
                            else
                            {
                                throw new Exception("Delete Criateras That Linked To This Specification First");
                            }
                        }

                        await _FormFieldRepository.SaveChangesAsync();
                        newIdsOrUpdatesIds.Add(FormField.Id);
                        IList<FormFieldDetails> _FormFieldDetails = new List<FormFieldDetails>();

                        if (IsEditMode)
                        {
                            var FormFieldDetails = await _FormFieldDetailsRepository.Query().Where(x => x.FormFieldId == Model.Id).ToListAsync();


                            _FormFieldDetailsRepository.RemoveRange(FormFieldDetails);
                            await _FormFieldDetailsRepository.SaveChangesAsync();

                        }


                        foreach (var FormItem in Model.FormFields)
                        {
                            _FormFieldDetails.Add(new FormFieldDetails()
                            {
                                PlaceHolder = FormItem.PlaceHolder,
                                AdditionalInfo = FormItem.AdditionalInfo,
                                FormFieldId = FormField.Id,
                                ClassesNames = FormItem.ClassesNames,
                                CultureId = FormItem.CultureId,
                                GroupId = FormItem.GroupId,
                                DataSourceId = FormItem.DataSourceId,
                                StaticApiLink = FormItem.StaticApiLink,
                                CategoryId = await UpsertFormFieldCategories(FormItem, UserId, SiteId),
                                Validations = FormItem.Validations
                            });

                        }//end for(2nd)...
                        _FormFieldDetailsRepository.AddRange(_FormFieldDetails);

                        await _FormFieldDetailsRepository.SaveChangesAsync();
                    }//end for (1st)..

                    await transaction.CommitAsync();


                    var firstSpecification = allFormField.FirstOrDefault();
                    var result = allFormField.Any(x => x.IsGeneral != null && x.IsGeneral.Value) ? (await GetGeneralFormFields(new SmartSearchTableParam<AllGeneralFormField>() { Search = new AllGeneralFormField(), Pagination = new Infrastructure.Web.SmartTable.Pagination { Start = 0, Number = int.MaxValue } })).Data : await GetFormFieldByIds(FormItemId: firstSpecification.FormItemId.Value, SiteId: SiteId, newIdsOrUpdatesIds);//to get result after insert ...
                    result = result?.Where(x => newIdsOrUpdatesIds.Contains(x.Id))?.ToList();
                    return result;
                }
                catch (Exception E)
                {
                    await transaction.RollbackAsync();
                    throw E;
                }

            });

        }
        public async Task<PaginationResult<FormFieldVM>> GetGeneralFormFields(SmartSearchTableParam<AllGeneralFormField> model)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var FormFieldList = _FormFieldRepository.Query()
                   .Where(p => p.IsGeneral && !p.FormItemId.HasValue && p.SiteId == siteId && p.Status == EStatus.Published)
                   .Include(x => x.FormItem)
                   .Include(x => x.FormFieldDetails)
                   .Include("FormFieldDetails.DataSource")
                   .Include("FormFieldDetails.Culture")
                   .Include("FormFieldDetails.Group")
                   .Include("FormFieldDetails.Category")
                   .Include("FormFieldDetails.Category.Children.CategoryDetails")
                   .Include("FormFieldDetails.Category.CategoryDetails")
                   .Include("FormFieldDetails.Category.CategoryDetails.SlugInfo")
                   .Include("FormFieldDetails.Category.CategoryDetails.Culture")
                   .AsNoTracking();

                if (!string.IsNullOrEmpty(model.Search.Name))
                {
                    string Name = model.Search.Name.ToUpper();
                    FormFieldList = FormFieldList.Where(x => x.FormFieldDetails.Any(x => x.Category.CategoryDetails.Any(x => x.Name.ToUpper().Contains(Name))));
                }

                var result = await FormFieldList.Select(s => new FormFieldVM()
                {
                    Status = s.Status,
                    Id = s.Id,
                    InputType = s.InputType,
                    FormFieldDetailsVMs = s.FormFieldDetails.Select(x => new FormFieldDetailsVM
                    {
                        ChildrenCategories = x.Category == null ? new List<ChildDetailsVM>() :
                        x.Category.Children.Where(c => c.Status != EStatus.Deleted).SelectMany(b => b.CategoryDetails).Select(x => new ChildDetailsVM
                        {

                            Name = x.Name,
                            CultureId = x.CultureId.HasValue ? x.CultureId.Value : null,
                            Slug = x.Slug,
                            SlugInfoId = x.SlugInfoId,
                            Id = x.Id,
                            CategoryTemplateId = x.CategoryTemplateId.HasValue ? x.CategoryTemplateId.Value : null,
                            CategoryId = x.CategoryId

                        })
                       .ToList(),
                        GroupId = x.GroupId,
                        ClassesNames = x.ClassesNames,
                        Validations = x.Validations,
                        AdditionalInfo = x.AdditionalInfo,
                        PlaceHolder = x.PlaceHolder,
                        CultureId = x.CultureId,
                        Culture = x.Culture == null ? new SitesCultures() : new SitesCultures
                        {
                            CreatedById = x.Culture.CreatedById,
                            CultureId = x.Culture.CultureId,
                            CultureType = x.Culture.CultureType,
                            CulturePrefix = x.Culture.CulturePrefix,
                            ModificationDate = x.Culture.ModificationDate,
                            ModifiedById = x.Culture.ModifiedById,
                            Status = x.Culture.Status,
                            SiteId = x.Culture.SiteId,
                            CreationDate = x.Culture.CreationDate

                        },
                        ParentCategoryId = x.CategoryId,
                        ParentCategory = x.Category,
                        Name = x.Category == null ? "" : x.Category.CategoryDetails == null ? "" : x.Category.CategoryDetails.Where(y => y.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",
                        DataSource = x.DataSource,
                        Group = x.Group,
                        DataSourceId = x.DataSourceId,
                        StaticApiLink = x.StaticApiLink,

                        CategoryId = x.CategoryId
                    }).ToList(),
                    OneOfOptionsType = s.OneOfOptionsType,
                    FormFields = s.FormFieldDetails.Select(x => new FormFieldDetailsVMs
                    {
                        ChildrenCategories = x.Category == null ? new List<ChildDetailsVM>() :
                        x.Category.Children.Where(c => c.Status != EStatus.Deleted).SelectMany(b => b.CategoryDetails).Select(x => new ChildDetailsVM
                        {

                            Name = x.Name,
                            CultureId = x.CultureId.HasValue ? x.CultureId.Value : null,
                            Slug = x.Slug,
                            SlugInfoId = x.SlugInfoId,
                            Id = x.Id,
                            CategoryTemplateId = x.CategoryTemplateId.HasValue ? x.CategoryTemplateId.Value : null,
                            CategoryId = x.CategoryId

                        })
                       .ToList(),
                        GroupId = x.GroupId,
                        ClassesNames = x.ClassesNames,
                        Validations = x.Validations,
                        AdditionalInfo = x.AdditionalInfo,
                        PlaceHolder = x.PlaceHolder,
                        CultureId = x.CultureId,

                        ParentCategoryId = x.CategoryId,
                        Name = x.Category == null ? "" : x.Category.CategoryDetails == null ? "" : x.Category.CategoryDetails.Where(y => y.CultureId == x.CultureId).Select(x => x.Name).FirstOrDefault() ?? "",

                        DataSourceId = x.DataSourceId,
                        StaticApiLink = x.StaticApiLink,

                    }).ToList()

                }).OrderByDescending(x => x.Id).PaginateAsync(model.Pagination.Number, model.Pagination.Start);
                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        #endregion

        #region HelperFunctions 

        private async Task<List<FormFieldVM>> GetFormFieldByIds(long FormItemId, long SiteId, List<long> IDs)
        {
            List<FormFieldVM> FormFields = new List<FormFieldVM>();
            FormFields = await GetFormFields(FormItemId: FormItemId, SiteId: SiteId);//to get result after insert ...


            FormFields = FormFields?.Where(x => IDs.Contains(x.Id))?.ToList();
            return FormFields;

        }
        private async Task<long> UpsertFormFieldCategories(FormFieldDetailsVMs Model, long UserId, long SiteId)
        {
            try
            {
                var Category = await _CategoryRepository.Query().Include(x => x.CategoryDetails)
                .Where(x => x.Id == Model.ParentCategoryId).SelectMany(x => x.CategoryDetails).Select(x => new ChildDetailsVM
                {
                    CategoryId = x.CategoryId,
                    CategoryTemplateId = x.CategoryTemplateId,
                    CultureId = x.CultureId,
                    Id = x.Id,
                    Name = x.Name


                }).FirstOrDefaultAsync();

                if (Category == null)

                {


                    Category = new ChildDetailsVM();


                }

                long ParentId = await UpsertCategoryItem(Category, Model.Name, Model.CultureId, UserId, SiteId, null);


                if (Model.ChildrenCategories != null)
                {


                    await ChangeStatusForNotSend(Model.ChildrenCategories, ParentId, Model.CultureId);

                    foreach (var categoryItem in Model.ChildrenCategories)
                    {
                        await UpsertCategoryItem(categoryItem, categoryItem.Name, Model.CultureId, UserId, SiteId, ParentId);
                    }


                }
                else
                {

                    await ChangeStatusForNotSend(new List<ChildDetailsVM>(), ParentId, Model.CultureId);


                }
                return ParentId;

                return 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Category Already Exist Please Change The Name");

            }
        }
        private async Task ChangeStatusForNotSend(List<ChildDetailsVM> childrenCategories, long parentId, long cultureId)
        {
            try
            {
                var CategorybyParentId = await _CategoryRepository.Query().Include(x => x.CategoryDetails).Where(x => x.ParentId == parentId).ToListAsync();

                if (CategorybyParentId.Count() == 0)
                {

                    return;
                }
                var CategoryExceptSending = CategorybyParentId.Where(x => !childrenCategories.Any(y => y.CategoryId == x.Id)).ToList();

                if (CategoryExceptSending.Count() > 0)
                {
                    foreach (var cat in CategoryExceptSending)
                    {
                        cat.Status = EStatus.Deleted;

                    }


                }

                await _CategoryRepository.SaveChangesAsync();

                return;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task<long> UpsertCategoryItem(ChildDetailsVM Model, string Name, long cultureId, long UserId, long SiteId, long? ParentId)
        {
            try
            {


                bool IsEditMode = Model.CategoryId != 0;

                var CategoryItem = IsEditMode ? await _CategoryRepository.Query().Include(x => x.CategoryDetails).Where(c => c.Id == Model.CategoryId)
                                                       .FirstOrDefaultAsync() : new Category();

                if (IsEditMode)
                {


                    CategoryItem.ModifiedById = UserId;
                    CategoryItem.ModificationDate = DateTime.Now;
                    if (CategoryItem.CategoryDetails != null)
                    {
                        foreach (var cat in CategoryItem.CategoryDetails)
                        {
                            if (cat.CultureId == cultureId)
                            {
                                cat.Name = Name;


                            }

                        }


                    }

                }
                else
                {
                    CategoryItem.SiteId = SiteId;
                    CategoryItem.CategoryTypeId = (long)ECategoryTypes.PricingQuotation;
                    CategoryItem.CreatedById = UserId;
                    CategoryItem.CreationDate = DateTime.Now;
                    CategoryItem.Status = EStatus.Published;

                }
                CategoryItem.ParentId = ParentId;

                if (!IsEditMode)
                {
                    _CategoryRepository.Add(CategoryItem);
                }

                await _CategoryRepository.SaveChangesAsync();
                if (!IsEditMode)
                {
                    await InsertCategoryDetails(cultureId, Name, CategoryItem.Id);


                }
                return CategoryItem.Id;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        private bool CanEditFormFieldType(long Id)
        {
            try
            {

                var Criterias = _CriteriasRepository.Query()
                     .Where(c => c.FormFieldId == Id && c.Status != EStatus.Deleted)
                     .ToList();
                return Criterias.Count == 0;

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        private async Task<bool> InsertCategoryDetails(long cultureId, string Name, long id)
        {
            try
            {
                long SiteId = _workContext.GetCurrentSiteId();

                var CategoriesDetails = new List<CategoryDetails>();
                var Category = new CategoryDetails();
                Category.CategoryId = id;
                Category.Name = Name;
                Category.Description = "";
                Category.MetaTitle = "";
                Category.MetaDescription = "";
                Category.Description = "";
                Category.CategoryTemplateId = null;
                Category.CultureId = cultureId;
                Category.SlugInfoId = null;

                if (Category.Id == 0)
                {
                    categoryDetailsRepository.Add(Category);
                }
                await categoryDetailsRepository.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion
    }
}
