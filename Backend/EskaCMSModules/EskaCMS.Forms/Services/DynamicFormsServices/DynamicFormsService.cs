using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Data;
using EskaCMS.DynamicForms.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Core.Data;
using EskaCMS.DynamicForms.ViewModels;
using EskaCMS.DynamicForms.HelperClasses.IHelperClasses;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using EskaCMS.Media.Services.Interfaces;
using EskaCMS.Infrastructure.Web.SmartTable;
using System.Dynamic;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.DynamicForms;
using System.Diagnostics;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.DataAccess.Data;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Forms.Services.FormsService.Interfaces;
using EskaCMS.Forms.Model.FormsViewModel;

namespace EskaCMS.DynamicForms.Services
{
    public class DynamicFormsService : IDynamicFormsService
    {
        private readonly IRepository<Classifications> _ClassificationsRepository;
        private readonly IRepository<Criteria> _CriteriasRepository;
        private readonly IRepository<FormItem> _FormItemRepository;
        private readonly EskaDCMSDbContext _context;
        private readonly IOperationsManager _OperationsManager;
        private readonly IUserSiteService userSiteService;
        private readonly IConditionsManager _ConditionsManager;
        private readonly IFormService _FormService;
        private readonly ISubmissionServices submissionServices;
        private readonly IRepository<CategoryDetails> _categoryDetailsRepository;
        private readonly IRepository<FormItemDetails> formItemDetailsRepository;
        private readonly IRepository<FormFieldGroup> _FormFieldGroupRepository;
        private readonly IRepository<FormField> _FormFieldRepository;
        private readonly IFormItemService _FormItemService;
        private readonly IRepository<CurrencyRates> currencyRates;
        private readonly Core.Services.IMediaService _MediaService;
        private readonly ICurrencyService currencyService;
        private readonly IRepositoryWithTypedId<SiteSettings, string> siteSettingsRepository;
        private readonly IRepository<FormFieldDetails> _FormFieldDetailsRpository;
        private readonly IRepository<Category> _categoriesRepository;
        private readonly IRepository<CategoryCriteria> _categoryCriteriaRepository;
        private readonly IWorkContext _WorkContext;

        public DynamicFormsService(
            IRepository<Classifications> ClassificationsRepository,
            IRepository<Criteria> CriteriasRepository,
            IRepository<FormItem> FormItemRepository,
            EskaDCMSDbContext context,
            IConditionsManager ConditionsManager,
            IOperationsManager OperationsManager,
            IUserSiteService userSiteService,
            IFormService FormService, ISubmissionServices submissionServices,
            IRepository<CategoryDetails> CategoryDetailsRepository,
            IRepository<FormItemDetails> FormItemDetailsRepository,
            IRepository<FormField> FormFieldRepository,
            IRepository<FormFieldGroup> FormFieldGroupRepository,
            IFormItemService FormItemService,
            IRepository<CurrencyRates> CurrencyRates,
            Core.Services.IMediaService MediaService,
            ICurrencyService CurrencyService,
                        IRepositoryWithTypedId<SiteSettings, string> SiteSettingsRepository,

            IRepository<FormFieldDetails> FormFieldDetailsRpository,

            IRepository<Category> categoriesRepository,
            IRepository<CategoryCriteria> CategoryCriteriaRepository,
            IWorkContext WorkContext
            )
        {
            _FormFieldGroupRepository = FormFieldGroupRepository;
            _FormFieldRepository = FormFieldRepository;
            _ClassificationsRepository = ClassificationsRepository;
            _CriteriasRepository = CriteriasRepository;
            _FormItemRepository = FormItemRepository;
            _context = context;
            _OperationsManager = OperationsManager;
            this.userSiteService = userSiteService;
            _ConditionsManager = ConditionsManager;
            _FormService = FormService;
            this.submissionServices = submissionServices;
            _categoryDetailsRepository = CategoryDetailsRepository;
            formItemDetailsRepository = FormItemDetailsRepository;
            _FormItemService = FormItemService;
            currencyRates = CurrencyRates;
            _MediaService = MediaService;
            currencyService = CurrencyService;
            siteSettingsRepository = SiteSettingsRepository;
            _FormFieldDetailsRpository = FormFieldDetailsRpository;
            _categoriesRepository = categoriesRepository;
            _categoryCriteriaRepository = CategoryCriteriaRepository;
            _WorkContext = WorkContext;
        }

        #region Criateria
        public async Task<bool> UpsertOperationCriateria(CriteriaVM Model, long UserId, long SiteId)
        {
            try
            {
                bool IsEditMode = Model.Id != 0;
                var OpreationCriateria = IsEditMode ? await _CriteriasRepository.Query()
                    .Where(c => c.Id == Model.Id && c.Type == EDynamicFormsCriteriaTypes.Operation)
                    .FirstOrDefaultAsync() : new Criteria();
                if (IsEditMode)
                {
                    OpreationCriateria.ModificationDate = DateTime.Now;
                    OpreationCriateria.ModifiedById = UserId;

                }
                else
                {

                    OpreationCriateria.CreationDate = DateTime.Now;
                    OpreationCriateria.SiteId = SiteId;
                    OpreationCriateria.CreatedById = UserId;
                    OpreationCriateria.ClassificationId = Model.ClassificationId;
                    OpreationCriateria.Status = EStatus.Published;
                    OpreationCriateria.Type = EDynamicFormsCriteriaTypes.Operation;
                    OpreationCriateria.Name = Model.Name;
                    OpreationCriateria.FormFieldId = Model.FormFieldId.HasValue && Model.FormFieldId != 0 ? Model.FormFieldId.Value : null;
                    //OpreationCriateria.SubCategoryId = Model.SubCategoryId.HasValue&&Model.SubCategoryId!=0 ?  Model.SubCategoryId.Value : null;
                    OpreationCriateria.CategoryId = Model.CategoryId.HasValue && Model.CategoryId != 0 ? Model.CategoryId.Value : null;

                }


                OpreationCriateria.Value = Model.Value;
                OpreationCriateria.OperationType = Model.OperationType;

                if (!IsEditMode)
                {
                    _CriteriasRepository.Add(OpreationCriateria);

                }

                await _CriteriasRepository.SaveChangesAsync();

                if (IsEditMode)
                {
                    var RemoveSubCategory = await _categoryCriteriaRepository.Query().Where(x => x.CriteriaId == OpreationCriateria.Id).ToListAsync();
                    _categoryCriteriaRepository.RemoveRange(RemoveSubCategory);
                    await _categoryCriteriaRepository.SaveChangesAsync();
                    await MappingCriteriaToSubCategory(Model.SubCategoryId, OpreationCriateria.Id);

                }
                else
                {

                    await MappingCriteriaToSubCategory(Model.SubCategoryId, OpreationCriateria.Id);



                }
                return true;




            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<PaginationResult<Criteria>> GetGeneralCriteria(SmartSearchTableParam<AllGeneralFormField> model)
        {
            try
            {
                var siteId = _WorkContext.GetCurrentSiteId();
                var ClassificationsItem = _CriteriasRepository.Query()
                                                              .Where(x => x.IsGeneral && x.Status != EStatus.Deleted && x.SiteId == siteId)
                                                              .Include(x => x.Category)
                                                              .Include(x => x.CategoryCriterias)
                                                              .Include("CategoryCriterias.SubCategory")
                                                              .AsNoTracking();


                if (!string.IsNullOrEmpty(model.Search.Name))
                {
                    string Name = model.Search.Name.ToUpper();
                    ClassificationsItem = ClassificationsItem.Where(x => x.Name.ToUpper().Contains(Name));
                }

                var result = await ClassificationsItem.Select(x => new Criteria()
                {
                    Id = x.Id,
                    Name = x.Name,
                    FormField = x.FormField,
                    CategoryCriterias = x.CategoryCriterias,

                    CategoryId = x.CategoryId,

                    Category = x.Category,
                    OperationType = x.OperationType,
                    Value = x.Value,
                    Type = x.Type,
                    FormFieldId = x.FormFieldId,
                    ConditionsType = x.ConditionsType

                }
                   ).PaginateAsync(model.Pagination.Number, model.Pagination.Start);

                return result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<bool> UpsertGeneralCriteria(CriteriaVM Model)
        {
            try
            {
                var siteId = _WorkContext.GetCurrentSiteId();
                var userId = await _WorkContext.GetCurrentUserId();
                bool IsEditMode = Model.Id != 0;

                var CriteriaItem = IsEditMode ? await _CriteriasRepository.Query()
                         .Where(c => c.Id == Model.Id)
                         .Include(c => c.CategoryCriterias)
                         .FirstOrDefaultAsync() : new Criteria();

                if (IsEditMode)
                {

                    CriteriaItem.ModificationDate = DateTime.Now;
                    CriteriaItem.ModifiedById = userId;

                }
                else
                {

                    CriteriaItem.CreatedById = userId;
                    CriteriaItem.CreationDate = DateTime.Now;
                    CriteriaItem.SiteId = siteId;
                    CriteriaItem.Status = EStatus.Published;
                    CriteriaItem.IsGeneral = true;

                }

                CriteriaItem.Name = Model.Name;
                CriteriaItem.Value = Model.Value;
                CriteriaItem.ConditionsType = Model.ConditionsType;
                CriteriaItem.OperationType = Model.OperationType;
                //CriteriaItem.SubCategoryId = Model.SubCategoryId == 0 ? null : Model.SubCategoryId;
                CriteriaItem.Type = Model.Type;
                CriteriaItem.CategoryId = Model.CategoryId.HasValue && Model.CategoryId != 0 ? Model.CategoryId.Value : null;
                CriteriaItem.FormFieldId = Model.FormFieldId.HasValue && Model.FormFieldId != 0 ? Model.FormFieldId.Value : null;

                if (!IsEditMode)
                {
                    _CriteriasRepository.Add(CriteriaItem);
                }
                await _CriteriasRepository.SaveChangesAsync();

                if (IsEditMode)
                {
                    var RemoveSubCategory = await _categoryCriteriaRepository.Query().Where(x => x.CriteriaId == CriteriaItem.Id).ToListAsync();
                    _categoryCriteriaRepository.RemoveRange(RemoveSubCategory);
                    await _categoryCriteriaRepository.SaveChangesAsync();
                    await MappingCriteriaToSubCategory(Model.SubCategoryId, CriteriaItem.Id);

                }
                else
                {
                    await MappingCriteriaToSubCategory(Model.SubCategoryId, CriteriaItem.Id);
                }


                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<bool> UpdateCriteriaStatus(long CriteriaId, EStatus NewStatus, long UserId)
        {
            try
            {
                var Criteria = await _CriteriasRepository.Query()
                 .Where(p => p.Id == CriteriaId)
                 .FirstOrDefaultAsync();

                Criteria.Status = NewStatus;
                Criteria.ModificationDate = DateTime.Now;
                Criteria.ModifiedById = UserId;

                await _CriteriasRepository.SaveChangesAsync();
                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        #endregion

        #region FormItem
        public async Task<FormItemVM> DuplicateFormItem(long FormItemId)
        {
            var UserId = await _WorkContext.GetCurrentUserId();

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var transactions = await _context.Database.BeginTransactionAsync();
            try
            {

                    var FormItem = await _FormItemRepository.Query().Where(m => m.Id == FormItemId)
                        .Include(x => x.FormItemDetails)
                      .Include(m => m.FormFieldGroups)
                      .Include(s => s.FormFields)
                      .Include("FormFields.FormFieldDetails")
                      .Include(m => m.Classifications)
                      .Include("Classifications.CriteriaList")
                      .Include("Classifications.CriteriaList.CategoryCriterias")
                      .Include("Classifications.CriteriaList.Category")
                      .Include("Classifications.CriteriaList.Category.Children")
                   .FirstOrDefaultAsync();

                    FormItem DuplicatedFormItem = new FormItem();

                    DuplicatedFormItem.CreatedById = UserId;
                    DuplicatedFormItem.CreationDate = DateTime.Now;
                    DuplicatedFormItem.SiteId = FormItem.SiteId;
                    DuplicatedFormItem.Status = FormItem.Status;
                    DuplicatedFormItem.ThumbnailId = FormItem.ThumbnailId;
                    DuplicatedFormItem.Icon = FormItem.Icon;
                    _FormItemRepository.Add(DuplicatedFormItem);

                    await _FormItemRepository.SaveChangesAsync();

                    List<FormItemDetails> FormItemDetails = new List<FormItemDetails>();

                    foreach (var FormItemDetail in FormItem.FormItemDetails)
                    {
                        FormItemDetails.Add(new FormItemDetails()
                        {
                            Description = FormItemDetail.Description,
                            Name = FormItemDetail.Name,
                            FormItemId = DuplicatedFormItem.Id,
                            CultureId = FormItemDetail.CultureId
                        });

                    }
                    formItemDetailsRepository.AddRange(FormItemDetails);

                    await formItemDetailsRepository.SaveChangesAsync();


                    var GroupsMapper = await DuplicateFormFieldGroups(DuplicatedFormItem.Id, FormItem.FormFieldGroups);

                    if (GroupsMapper.Count == 0 || FormItem.FormFields == null)
                    {
                        await transactions.CommitAsync();

                        return await _FormItemService.GetFormItemsById(DuplicatedFormItem.Id); ;
                    }



                    var Mappers = await DuplicateFormField(DuplicatedFormItem.Id,
                                                                           FormItem.FormFields,
                                                                           GroupsMapper);
                    var FormFieldMapper = Mappers[0];
                    var CategoryMapper = Mappers[1];


                    if (FormFieldMapper.Count == 0 || FormItem.Classifications == null)
                    {
                        await transactions.CommitAsync();
                        return await _FormItemService.GetFormItemsById(DuplicatedFormItem.Id); ;
                    }

                    await DuplicateClassification(DuplicatedFormItem.Id, FormItem.Classifications, FormFieldMapper, CategoryMapper, UserId);

                    await transactions.CommitAsync();
                    return await _FormItemService.GetFormItemsById(DuplicatedFormItem.Id);

                }
                catch (Exception ex)
                {
                    await transactions.RollbackAsync();
                    throw ex;
                }
            });

        }
        #endregion

        #region classification
        public async Task<Criteria> GetClassificationOperation(long ClassificationId)
        {
            try
            {


                var OperationCriteria = await _CriteriasRepository.Query()
                    .Where(p => p.Type == EDynamicFormsCriteriaTypes.Operation &&
                                p.ClassificationId == ClassificationId)
                    .FirstOrDefaultAsync();

                if (OperationCriteria == null)
                {
                    // fill default values
                    OperationCriteria = new Criteria()
                    {
                        Type = EDynamicFormsCriteriaTypes.Operation,
                        Id = 0,
                        Value = "",
                        OperationType = EDynamicCriteriaOperations.ShowValue
                    };
                }
                return OperationCriteria;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<Classifications> GetClassificationById(long ClassId)
        {
            try
            {
                var ClassificationsItem = await _ClassificationsRepository.Query()
                    .Where(c => c.Id == ClassId)
                    .Include(c => c.CriteriaList)
                    .Include("CriteriaList.Category")
                    .Include("CriteriaList.CategoryCriterias")
                    .Include("CriteriaList.CategoryCriterias.SubCategory")
                    .Select(c => new Classifications()
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        CriteriaList = c.CriteriaList.Where(x => x.Type == EDynamicFormsCriteriaTypes.Condition && x.Status != EStatus.Deleted)
                        .Select(x => new Criteria()
                        {
                            Id = x.Id,
                            Name = x.Name,
                            FormField = x.FormField,
                            CategoryCriterias = x.CategoryCriterias,

                            CategoryId = x.CategoryId,

                            Category = x.Category,
                            OperationType = x.OperationType,
                            Value = x.Value,
                            Type = x.Type,
                            FormFieldId = x.FormFieldId,
                            ConditionsType = x.ConditionsType

                        }).ToList()
                    })
                    .FirstOrDefaultAsync();
                return ClassificationsItem;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<List<Classifications>> GetClassifications(long FormItemId)
        {
            try
            {
                var ClassificationsItem = await _ClassificationsRepository.Query()
                    .Where(c => c.FormItemId == FormItemId && c.Status != EStatus.Deleted)
                    .Include("CriteriaList.CategoryCriterias")
                    .Include("CriteriaList.CategoryCriterias.SubCategory")
                    .ToListAsync();
                return ClassificationsItem;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<PaginationResult<Classifications>> GetGeneralClassifications(SmartSearchTableParam<AllGeneralFormField> model)
        {
            try
            {
                var siteId = _WorkContext.GetCurrentSiteId();
                var query = _ClassificationsRepository.Query()
                    .Where(c => c.IsGeneral && c.SiteId == siteId && c.Status != EStatus.Deleted)
                    .Include("CriteriaList.CategoryCriterias")
                    .Include("CriteriaList.CategoryCriterias.SubCategory")
                    .AsNoTracking();

                if (!string.IsNullOrEmpty(model.Search.Name))
                {
                    string Name = model.Search.Name.ToUpper();
                    query = query.Where(x => x.Name.ToUpper().Contains(Name));
                }

                var result = await query.Select(x => x).PaginateAsync(model.Pagination.Number, model.Pagination.Start);

                return result;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<Classifications> UpdateClassificationStatus(long ClassId, EStatus NewStatus, long UserId)
        {
            try
            {
                var ClassificationsItem = await _ClassificationsRepository.Query()
                   .Where(c => c.Id == ClassId)
                   .FirstOrDefaultAsync();
                ClassificationsItem.ModificationDate = DateTime.Now;
                ClassificationsItem.Status = NewStatus;
                ClassificationsItem.ModifiedById = UserId;

                await _ClassificationsRepository.SaveChangesAsync();

                return ClassificationsItem;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<Classifications> UpsertClassification(ClassificationVM Model, long UserId, long SiteId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
            await using var transactions = await _context.Database.BeginTransactionAsync();
            try
            {
                    bool IsEditMode = Model.Id != 0;
                    var ClassificationItem = IsEditMode ? await _ClassificationsRepository.Query()
                         .Where(c => c.Id == Model.Id)
                         .Include(c => c.CriteriaList)
                         .Include("CriteriaList.CategoryCriterias")
                         .FirstOrDefaultAsync() : new Classifications();

                    if (IsEditMode)
                    {

                        ClassificationItem.ModificationDate = DateTime.Now;
                        ClassificationItem.ModifiedById = UserId;

                    }
                    else
                    {
                        ClassificationItem.CreationDate = DateTime.Now;
                        ClassificationItem.Status = EStatus.Published;
                        ClassificationItem.CreatedById = UserId;


                        if (Model.IsGeneral.HasValue && Model.IsGeneral.Value && Model.FormItemId.HasValue)
                        {
                            throw new Exception("Please this is classification FormField you have to add it without any FormItem");
                        }

                        else if (!Model.IsGeneral.HasValue && !Model.FormItemId.HasValue)
                        {
                            throw new Exception("Please you have to add FormItem");
                        }

                        ClassificationItem.IsGeneral = Model.IsGeneral.HasValue ? Model.IsGeneral.Value : false;
                        ClassificationItem.FormItemId = Model.FormItemId;
                        ClassificationItem.SiteId = SiteId;

                    }
                    ClassificationItem.Name = Model.Name;
                    ClassificationItem.Description = Model.Description;
                    if (!IsEditMode)
                    {
                        _ClassificationsRepository.Add(ClassificationItem);
                    }
                    await _ClassificationsRepository.SaveChangesAsync();

                    if (!Model.IsGeneral.HasValue)
                    {
                        await UpsertCriteriasList(Model.CriteriaList, ClassificationItem.CriteriaList, ClassificationItem.Id, UserId, SiteId);

                    }


                    await transactions.CommitAsync();

                    return ClassificationItem;
                }
                catch (Exception Exc)
                {
                    await transactions.RollbackAsync();
                    throw Exc;
                }

            });

        }
        public async Task<ClassificationSearchVM> GetPaginatedClassifications(long FormItemId, SearchTableParam<string> Data)
        {
            try
            {

                var ClassificationsList = await _ClassificationsRepository.Query()
                    .Where(c => c.FormItemId == FormItemId
                                && c.Status != EStatus.Deleted
                                && (string.IsNullOrEmpty(Data.Search)
                                || c.Name.ToLower().Contains(Data.Search.ToLower())
                                || c.Description.ToLower().Contains(Data.Search.ToLower()))
                     )
                    .Include(c => c.CriteriaList)
                    .Include("CriteriaList.CategoryCriterias")
                    .Include("CriteriaList.CategoryCriterias.SubCategory")
                    .OrderByDescending(c => c.CreationDate)
                    .Select(c => new Classifications()
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        CreationDate = c.CreationDate,
                        CriteriaList = c.CriteriaList.Where(x => x.Type == EDynamicFormsCriteriaTypes.Condition && x.Status != EStatus.Deleted).ToList()
                    })
                    .ToListAsync();

                if (Data.Pagination != null)
                {
                    return new ClassificationSearchVM()
                    {
                        Classifications = ClassificationsList
                                        .Skip(Data.Pagination.Start * Data.Pagination.Number)
                                        .Take(Data.Pagination.Number)
                                        .ToList(),

                        Pagination = new Pagination()
                        {
                            Number = Data.Pagination.Number,
                            Start = Data.Pagination.Start,
                            TotalItemCount = ClassificationsList.Count()
                        }
                    };
                }

                return new ClassificationSearchVM()
                {
                    Classifications = ClassificationsList
                };


            }
            catch (Exception ex)
            {

                throw ex;
            }

        }
        #endregion

        #region web functions
        public async Task<string> Submitted(string FormData, DynamicFormsSettingsVM model, List<DynamicFormsWebVM> Data, long FormItemId, long SiteId, long UserId, long PageId, string ComponentSettingsId, string Culture, IFormFileCollection Files)
        {
            try
            {
                string Result = string.Empty;
                dynamic FormDataObj = new ExpandoObject();

                var FormItem = await _FormItemRepository.Query().Where(x => x.Id == FormItemId).AsNoTracking().FirstOrDefaultAsync();



                if (FormItem.Type == FormItemType.PricingQuotation)
                {
                    Data = Data.Where(input => input.InputType != EDynamicCriteriaInputTypes.Attachment).ToList();


                    var Classifications = await _ClassificationsRepository.Query()
                                        .Where(m => m.FormItemId == FormItemId && m.Status == EStatus.Published)
                                        .AsNoTrackingWithIdentityResolution()
                                        .AsSplitQuery()
                                        .Include("CriteriaList")
                                        .Include("CriteriaList.FormField")
                                        .Include("CriteriaList.CategoryCriterias")
                                        .Include("CriteriaList.CategoryCriterias.SubCategory")
                                        .OrderByDescending(s => s.CriteriaList.Count())
                                        .ToListAsync();

                    foreach (var Classification in Classifications)
                    {
                        var Conditions = Classification.CriteriaList
                                                        .Where(c => c.Type == EDynamicFormsCriteriaTypes.Condition &&
                                                        c.Status == EStatus.Published)
                                                        .ToList();

                        var Operation = Classification.CriteriaList
                                                       .Where(c => c.Type == EDynamicFormsCriteriaTypes.Operation &&
                                                       c.Status == EStatus.Published)
                                                       .FirstOrDefault();

                        if (await _ConditionsManager.CheckTheCriaterias(Conditions, Data))
                        {
                            Result = await _OperationsManager.AppliedOperation(Operation, Data);
                            break;
                        }
                    }




                    if (string.IsNullOrEmpty(Result))
                    {

                        Result = "No Classification Covers These Inputs";
                        FormDataObj = JsonConvert.DeserializeObject<dynamic>(FormData);
                        FormDataObj.Result = Result;
                        FormData = JsonConvert.SerializeObject(FormDataObj);
                        try
                        {
                           await _FormService.NewFormSave(PageId, ComponentSettingsId, Culture, FormData, Files, null, false);
                        }
                        catch (Exception)
                        {

                        }
                        throw new Exception(Result);
                    }


                }


                FormDataObj = JsonConvert.DeserializeObject<dynamic>(FormData);

                FormDataObj.Result = Result;
                try
                {
                    if (model!=null && model.allowDuplicateSurveyPerUser != null
                       && model.allowDuplicateSurveyPerUser.Value
                       && FormItem.Type == FormItemType.Survey)
                    {
                        await submissionServices.ValidateSurveyDuplication(UserId, FormItem.Type, PageId);
                    }

                }
                catch (Exception ex)
                {

                    throw ex;
                }
                await submissionServices.Create(Data, new Submission
                {
                    CreationDate = DateTimeOffset.Now,
                    CreatedById = UserId,
                    FormItemId = FormItemId,
                    PageId = PageId,
                    SiteId = SiteId,
                    Result = Result,
                    Type = FormItem.Type
                });

                FormData = JsonConvert.SerializeObject(FormDataObj);
                try
                {
                  await _FormService.NewFormSave(PageId, ComponentSettingsId, Culture, FormData, Files, null, false);

                }
                catch (Exception)
                {

                }
                return Result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        public async Task<string> Submitted(List<DynamicFormsWebVM> Data, long FormItemId, long SiteId, long UserId, string Culture, IFormFileCollection? Files)
        {
            try
            {
                string Result = string.Empty;
                string allowDuplicateSurveyPerUser =
                               await siteSettingsRepository.Query()
                                                           .Where(x => x.Id == "allowDuplicateSurveyPerUser")
                                                           .Select(x => x.Value)
                                                           .FirstOrDefaultAsync();

                var FormItem = await _FormItemRepository.Query().Where(x => x.Id == FormItemId).AsNoTracking().FirstOrDefaultAsync();
                try
                {
                    if (allowDuplicateSurveyPerUser != null
                       && (string.IsNullOrEmpty(allowDuplicateSurveyPerUser) || Convert.ToBoolean(allowDuplicateSurveyPerUser))
                       && FormItem.Type == FormItemType.Survey)
                    {
                        await submissionServices.ValidateSurveyDuplication(UserId, FormItem.Type, null);
                    }

                }
                catch (Exception ex)
                {

                    throw ex;
                }
                await submissionServices.Create(Data, new Submission
                {
                    CreationDate = DateTimeOffset.Now,
                    CreatedById = UserId,
                    FormItemId = FormItemId,
                    PageId = null,
                    SiteId = SiteId,
                    Result = Result,
                    Type = FormItem.Type
                });

                return Result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        #region helper Functions Criteria
        private async Task<bool> UpsertCriteriasList(IList<CriteriaVM> CriteriaList, IList<Criteria> OriginalCriteriaList, long ClassificationId, long UserId, long SiteId)
        {
            try
            {
                List<Criteria> Result = new List<Criteria>();
                foreach (var model in CriteriaList)
                {
                    Result.Add(await UpsertCriteria(model, OriginalCriteriaList, ClassificationId, UserId, SiteId));

                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }
        private async Task<Criteria> UpsertCriteria(CriteriaVM Model, IList<Criteria> OriginalCriteriaList, long ClassificationId, long UserId, long SiteId)
        {
            try
            {


                bool IsEditMode = Model.Id != 0;

                Criteria CriteriaItem = new Criteria();

                if (IsEditMode)
                {
                    CriteriaItem = OriginalCriteriaList.Where(c => c.Id == Model.Id).FirstOrDefault();
                    CriteriaItem.ModificationDate = DateTime.Now;
                    CriteriaItem.ModifiedById = UserId;

                }
                else
                {
                    CriteriaItem.ClassificationId = ClassificationId;
                    CriteriaItem.CreatedById = UserId;
                    CriteriaItem.CreationDate = DateTime.Now;
                    CriteriaItem.SiteId = SiteId;
                    CriteriaItem.Status = EStatus.Published;

                }

                CriteriaItem.Name = Model.Name;
                CriteriaItem.Value = Model.Value;
                CriteriaItem.ConditionsType = Model.ConditionsType;
                CriteriaItem.OperationType = Model.OperationType;
                //CriteriaItem.SubCategoryId = Model.SubCategoryId == 0 ? null : Model.SubCategoryId;
                CriteriaItem.Type = Model.Type;
                CriteriaItem.CategoryId = Model.CategoryId.HasValue && Model.CategoryId != 0 ? Model.CategoryId.Value : null;
                CriteriaItem.FormFieldId = Model.FormFieldId.HasValue && Model.FormFieldId != 0 ? Model.FormFieldId.Value : null;

                if (!IsEditMode)
                {
                    _CriteriasRepository.Add(CriteriaItem);
                }
                await _CriteriasRepository.SaveChangesAsync();

                if (IsEditMode)
                {
                    var RemoveSubCategory = await _categoryCriteriaRepository.Query().Where(x => x.CriteriaId == CriteriaItem.Id).ToListAsync();
                    _categoryCriteriaRepository.RemoveRange(RemoveSubCategory);
                    await _categoryCriteriaRepository.SaveChangesAsync();
                    await MappingCriteriaToSubCategory(Model.SubCategoryId, CriteriaItem.Id);

                }
                else
                {

                    await MappingCriteriaToSubCategory(Model.SubCategoryId, CriteriaItem.Id);



                }


                return CriteriaItem;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task<bool> MappingCriteriaToSubCategory(IList<long> subCategoryIds, long id)
        {
            try
            {
                var Model = new List<CategoryCriteria>();

                foreach (var CategoryId in subCategoryIds)
                {
                    Model.Add(new CategoryCriteria { CriteriaId = id, SubCategoryId = CategoryId });
                }

                _categoryCriteriaRepository.AddRange(Model);
                await _categoryCriteriaRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        #region helper Functions Form Field
        private async Task<Dictionary<long?, long?>> DuplicateFormFieldGroups(long DuplicatedFormItemId, IList<FormFieldGroup> Groups)
        {
            try
            {

                Dictionary<long?, long?> GroupsMapper = new Dictionary<long?, long?>(); // old , new 
                foreach (var group in Groups)
                {
                    var DuplicatedGroup = new FormFieldGroup()
                    {
                        FormItemId = DuplicatedFormItemId,
                        Name = group.Name,
                        Status = group.Status
                    };

                    _FormFieldGroupRepository.Add(DuplicatedGroup);
                    await _FormFieldGroupRepository.SaveChangesAsync();

                    GroupsMapper.Add(group.Id, DuplicatedGroup.Id);

                }
                return GroupsMapper;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task<List<Dictionary<long, long>>> DuplicateFormField(long DuplicatedFormItemId, IList<FormField> FormFields, Dictionary<long?, long?> GroupsMapper)
        {
            try
            {

                var result = new List<Dictionary<long, long>>();
                Dictionary<long, long> FormFieldMapper = new Dictionary<long, long>(); // old , new 
                Dictionary<long, long> CategoryMapper = new Dictionary<long, long>(); // old , new 
                foreach (var FormField in FormFields)
                {
                    var DuplicatedFormField = new FormField()
                    {
                        InputType = FormField.InputType,
                        SiteId = FormField.SiteId,
                        Status = FormField.Status,
                        OneOfOptionsType = FormField.OneOfOptionsType,
                        FormItemId = DuplicatedFormItemId,
                    };

                    _FormFieldRepository.Add(DuplicatedFormField);

                    await _FormFieldRepository.SaveChangesAsync();
                    FormFieldMapper.Add(FormField.Id, DuplicatedFormField.Id);

                    IList<FormFieldDetails> _FormFieldDetails = new List<FormFieldDetails>();

                    foreach (var FormFieldDetail in FormField.FormFieldDetails)
                    {
                        foreach (var item in await DuplicateCategoryWithChildren(FormFieldDetail.CategoryId))
                        {
                            CategoryMapper.Add(item.Key, item.Value);
                        }
                        var FormFieldDetails = new FormFieldDetails()
                        {

                            PlaceHolder = FormFieldDetail.PlaceHolder,
                            AdditionalInfo = FormFieldDetail.AdditionalInfo,
                            FormFieldId = FormFieldDetail.Id,
                            ClassesNames = FormFieldDetail.ClassesNames,
                            CultureId = FormFieldDetail.CultureId,
                            GroupId = FormFieldDetail.GroupId == null ? null : GroupsMapper[FormFieldDetail.GroupId.Value],
                            DataSourceId = FormFieldDetail.DataSourceId,
                            StaticApiLink = FormFieldDetail.StaticApiLink,
                            CategoryId = CategoryMapper[FormFieldDetail.CategoryId],
                            Validations = FormFieldDetail.Validations

                        };

                        _FormFieldDetails.Add(FormFieldDetails);
                    }
                    _FormFieldDetailsRpository.AddRange(_FormFieldDetails);

                    await formItemDetailsRepository.SaveChangesAsync();

                }
                result.Add(FormFieldMapper);
                result.Add(CategoryMapper);
                return result;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        # region helper Functions Category


        private async Task<Dictionary<long, long>> DuplicateCategoryWithChildren(long ParentCategoryId)
        {
            try
            {
                var Mapper = new Dictionary<long, long>();
                long UserId = await _WorkContext.GetCurrentUserId();
                var category = await _categoriesRepository.Query()
                     .Where(c => c.Id == ParentCategoryId)
                     .Include(c => c.Children)
                     .Include(x => x.ThumbnailImage)
                     .Include(x => x.CategoryDetails)

                     .ThenInclude(x => x.SlugInfo)
                     .FirstOrDefaultAsync();

                var ParentCategory = new Category()
                {
                    CreationDate = DateTimeOffset.Now,
                    CreatedById = UserId,
                    CategoryTypeId = (long)category.CategoryTypeId,
                    Status = category.Status,
                    StatusDate = DateTimeOffset.Now,
                    IncludeInMenu = category.IncludeInMenu,
                    ParentId = category.ParentId,
                    ThumbnailImageId = category.ThumbnailImageId,
                    SiteId = category.SiteId,
                    DisplayOrder = category.DisplayOrder,
                };

                _categoriesRepository.Add(ParentCategory);
                await _categoriesRepository.SaveChangesAsync();
                await InsertCategoryDetails(category.CategoryDetails, ParentCategory.Id);

                Mapper.Add(category.Id, ParentCategory.Id);
                foreach (var c in category.Children)
                {
                    var ChildrenItem = new Category()
                    {


                        CreationDate = DateTimeOffset.Now,
                        CreatedById = UserId,
                        CategoryTypeId = (long)c.CategoryTypeId,
                        Status = c.Status,
                        StatusDate = DateTimeOffset.Now,
                        IncludeInMenu = c.IncludeInMenu,
                        ParentId = ParentCategory.Id,
                        ThumbnailImageId = c.ThumbnailImageId,
                        SiteId = c.SiteId,
                        DisplayOrder = c.DisplayOrder,
                    };

                    _categoriesRepository.Add(ChildrenItem);
                    await _categoriesRepository.SaveChangesAsync();
                    await InsertCategoryDetails(c.CategoryDetails, ChildrenItem.Id);
                    Mapper.Add(c.Id, ChildrenItem.Id);


                }
                return Mapper;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private async Task<bool> InsertCategoryDetails(IList<CategoryDetails> categoryDetails, long id)
        {
            try
            {
                long SiteId = _WorkContext.GetCurrentSiteId();

                var CategoriesDetails = new List<CategoryDetails>();


                foreach (var category in categoryDetails)
                {
                    var Category = new CategoryDetails();




                    Category.CategoryId = id;
                    Category.Name = category.Name;
                    Category.Description = category.Description;
                    Category.MetaTitle = category.MetaTitle;
                    Category.MetaDescription = category.MetaDescription;
                    Category.Description = category.Description;
                    Category.CategoryTemplateId = category.CategoryTemplateId == 0 || category.CategoryTemplateId == null ? null : category.CategoryTemplateId;
                    Category.CultureId = category.CultureId;
                    Category.SlugInfoId = null;

                    if (Category.Id == 0)
                    {
                        _categoryDetailsRepository.Add(Category);
                    }
                    await _categoryDetailsRepository.SaveChangesAsync();



                }




                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        #region helper Functions classification
        private async Task<bool> DuplicateClassification(long DuplicatedFormItemId, IList<Classifications> Classifications, Dictionary<long, long> FormFieldsMapper, Dictionary<long, long> CategoryMapper, long UserId)
        {
            try
            {


                foreach (var Classification in Classifications)
                {
                    var DuplicatedClassification = new Classifications()
                    {
                        FormItemId = DuplicatedFormItemId,
                        Status = Classification.Status,
                        SiteId = Classification.SiteId,
                        Name = Classification.Name,
                        CreatedById = UserId,
                        CreationDate = DateTime.Now,
                        Description = Classification.Description

                    };


                    _ClassificationsRepository.Add(DuplicatedClassification);
                    await _ClassificationsRepository.SaveChangesAsync();


                    if (Classification.CriteriaList != null)
                    {


                        foreach (var c in Classification.CriteriaList)
                        {
                            var newCriteria = new Criteria()
                            {
                                Value = c.Value,
                                CategoryId = c.CategoryId,
                                ClassificationId = DuplicatedClassification.Id,
                                CreatedById = UserId,
                                CreationDate = DateTime.Now,
                                Name = c.Name,
                                OperationType = c.OperationType,
                                Type = c.Type,
                                ConditionsType = c.ConditionsType,
                                FormFieldId = null,
                                SiteId = c.SiteId,
                                Status = c.Status

                            };


                            if (c.FormFieldId != null)
                            {
                                newCriteria.FormFieldId = FormFieldsMapper[c.FormFieldId.Value];
                            }


                            _CriteriasRepository.Add(newCriteria);
                            await _CriteriasRepository.SaveChangesAsync();

                            if (c.CategoryCriterias?.Count > 0)
                            {

                                await MappingCriteriaToSubCategory(c.CategoryCriterias.Select(x => x.SubCategoryId).ToList(), newCriteria.Id);


                            }


                        }



                    }



                }

                return true;
            }




            catch (Exception ex)
            {

                throw ex;
            }

        }

        #endregion

        #region helper Functions Submitted Forms

        public async Task<PaginationResult<SubmittedFormItemViewModel>> GetAllFormItemForAdmin(FilterFormItemViewModel model)
        {
            try
            {
                var userId = await _WorkContext.GetCurrentUserId();
                var siteId = await _WorkContext.GetCurrentSiteIdAsync();
                if (!string.IsNullOrEmpty(model.Name))
                {
                    model.Name
                            = model.Name.ToLower();
                }
                var userRoles = await userSiteService.GetUserRoles(siteId, userId);
                var FormItemList = await _FormItemRepository.Query()
                    .Where(p => p.SiteId == siteId && p.Status == EStatus.Published)
                    .Include(p => p.Thumbnail)
                    .Include(x => x.Submissions)
                    .Include(x => x.FormItemRoles)
                    .Include(x => x.FormFields)
                    .Include(x => x.FormItemDetails)
                    .Where(x => x.FormItemRoles.Any() ? x.FormItemRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                    .Where(x => !model.Type.HasValue || x.Type == model.Type)
                    .Where(x => !string.IsNullOrEmpty(model.Name) || x.FormItemDetails.Any(u => u.Name.ToLower().Contains(model.Name)))
                    .Select(p => new SubmittedFormItemViewModel()
                    {
                        Id = p.Id,
                        CreationDate = p.CreationDate,
                        ThumbnailUrl = p.ThumbnailId.HasValue ? p.Thumbnail.PublicUrl : "",
                        ThumbnailId = p.ThumbnailId,
                        Icon = p.Icon,
                        Type = p.Type,
                        NumberOfResponses = p.Submissions != null ? p.Submissions.Count() : 0,
                        HasOneOfOption = p.FormFields.Any(x => x.InputType == EDynamicCriteriaInputTypes.Option),
                        Name = p.FormItemDetails.OrderByDescending(x => x.CultureId).Select(x => x.Name).FirstOrDefault(),
                        Description = p.FormItemDetails.OrderByDescending(x => x.CultureId).Select(x => x.Description).FirstOrDefault(),

                    }).OrderByDescending(x => x.Id)
                     .PaginateAsync(model.PageSize, model.PageNumber);

                return FormItemList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PaginationResult<SubmittedFormItemViewModel>> GetAllFormItemByIdForAdmin(FilterFormItemViewModel model)
        {
            try
            {
                var userId = await _WorkContext.GetCurrentUserId();
                var siteId = await _WorkContext.GetCurrentSiteIdAsync();
                if (!string.IsNullOrEmpty(model.Name))
                {
                    model.Name
                            = model.Name.ToLower();
                }
                var userRoles = await userSiteService.GetUserRoles(siteId, userId);
                var FormItemList = await _FormItemRepository.Query()
                    .Where(p => p.SiteId == siteId && p.Status == EStatus.Published)
                    .Include(p => p.Thumbnail)
                    .Include(x => x.Submissions)
                    .Include(x => x.FormItemRoles)
                    .Include(x => x.FormFields)
                    .Include(x => x.FormItemDetails)
                    .Where(x => x.FormItemRoles.Any() ? x.FormItemRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                    .Where(x => !model.Type.HasValue || x.Type == model.Type)
                    .Where(x => !string.IsNullOrEmpty(model.Name) || x.FormItemDetails.Any(u => u.Name.ToLower().Contains(model.Name)))
                    .Select(p => new SubmittedFormItemViewModel()
                    {
                        Id = p.Id,
                        CreationDate = p.CreationDate,
                        ThumbnailUrl = p.ThumbnailId.HasValue ? p.Thumbnail.PublicUrl : "",
                        ThumbnailId = p.ThumbnailId,
                        Icon = p.Icon,
                        Type = p.Type,
                        NumberOfResponses = p.Submissions != null ? p.Submissions.Count() : 0,
                        HasOneOfOption = p.FormFields.Any(x => x.InputType == EDynamicCriteriaInputTypes.Option),
                        Name = p.FormItemDetails.OrderByDescending(x => x.CultureId).Select(x => x.Name).FirstOrDefault(),
                        Description = p.FormItemDetails.OrderByDescending(x => x.CultureId).Select(x => x.Description).FirstOrDefault(),

                    }).OrderByDescending(x => x.Id)
                     .PaginateAsync(model.PageSize, model.PageNumber);

                return FormItemList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}
