using AutoMapper;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.UserCatalog.Dtos;
using EskaCMS.Entities.Entity;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;
using EskaCMS.Infrastructure.Pagination;
using System.Numerics;
using EskaCMS.Entities.Entity.Orders;
using EskaCommerce.UserCatalog.Dtos.CustomePlan;
using System.Linq.Expressions;
using EskaCMS.Entities.Entity.Catalog;

namespace EskaCommerce.UserCatalog.Services
{

    public class PlanService : IPlanService
    {

        private readonly IRepository<Plan> _planRepository;
        private readonly IRepository<PlanCategory> _planCategoryRepository;
        private readonly IRepository<PlanDetails> _planDetailsRepository;
        private readonly IPlansVersionsService _planVersionsService;
        private readonly IMapper _mapper;
        IRepository<UserPlans> _userplanRepository;
        private readonly IWorkContext _workContext;
        private readonly IPlanProductVersionService planProductVersionService;

        public PlanService(
            IRepository<Plan> planRepository,
            IWorkContext workContext,
            IPlanProductVersionService planProductVersionService,
            IRepository<UserPlans> userplanRepository,
            IPlansVersionsService planVersionsService,
            IRepository<PlanDetails> planDetailsRepository,
            IMapper mapper,
            IRepository<PlanCategory> planCategoryRepository
            )
        {
            _userplanRepository = userplanRepository;
            _planRepository = planRepository;
            _workContext = workContext;
            this.planProductVersionService = planProductVersionService;
            _planVersionsService = planVersionsService;
            _mapper = mapper;
            _planDetailsRepository = planDetailsRepository;
            _planCategoryRepository = planCategoryRepository;
        }
        public async Task<long> Create(PlanCreate planInput, long siteId)
        {
            Plan plan = _mapper.Map<Plan>(planInput);
            plan.CreatedById = await _workContext.GetCurrentUserId();
            plan.CreationDate = DateTimeOffset.Now;
            plan.Status = planInput.Status;
            plan.SiteId = siteId;
            _planRepository.Add(plan);
            await _planRepository.SaveChangesAsync();

            if (planInput.PlanDetails.Any())
            {
                await CreatePlanDetails(planInput.PlanDetails, plan.Id);
            }

            if (planInput.CategoryIds.Any())
            {
                IList<PlanCategory> planCategories = new List<PlanCategory>();

                foreach (var CategoryId in planInput.CategoryIds)
                {

                    planCategories.Add(new PlanCategory() { PlanId = plan.Id, CategoryId = CategoryId });


                }
                _planCategoryRepository.AddRange(planCategories);
                await _planCategoryRepository.SaveChangesAsync();
            }
            return plan.Id;
        }


        public async Task<PlanOutput> Get(long id, long siteId)
        {
            Plan plan = GetById(id);
            var userId = await _workContext.GetCurrentUserId();

            //   bool isParentPlan = !plan.ParentId.HasValue;
            PlanOutput planOutput = _mapper.Map<PlanOutput>(plan);
            //  planOutput.Parent = isParentPlan ? _mapper.Map<PlanOutput>(plan.Parent) : null;
            planOutput.Versions = _mapper.Map<IList<PlansVersionsOutput>>(plan.Versions);
            //  planOutput.ChildsPlans = await GetPlanChilds(plan.Id, siteId);
            planOutput.IsSubscribed = await IsSubscribedPlan(plan.Id, userId);
            return planOutput;
        }

        public async Task<PlanDetailsVM> GetPlanDetails(long id, long siteId)
        {
            Plan plan = GetById(id);



            // bool isParentPlan = !plan.ParentId.HasValue;
            PlanDetailsVM planOutput = new PlanDetailsVM
            {
                IsFeatured = plan.IsFeatured,
                Versions = _mapper.Map<List<PlansVersionsVM>>(plan.Versions),
                Id = plan.Id,
                Image = plan.Image,
                IsCustomPlan = plan.IsCustomPlan,
                IsTopPicked = plan.IsTopPicked,
                Order = plan.Order,
                PlanType = plan.PlanType,
                PlanCultureDetails = plan.PlanDetails.Select(x => new PlanCultureDetailsVm
                {
                    CultureCode = x.Culture.CultureId,
                    Name = x.Name,
                    Description = x.Description,
                    ShortDescription = x.ShortDescription,
                    CultureId = x.CultureId,

                }).ToList(),
                RecurringType = plan.RecurringType,
                Status = plan.Status,

                CaregoryIds = plan.Categories.Any() ? plan.Categories.Select(x => x.CategoryId).ToList() : new List<long>(),




            };







            return planOutput;
        }

        public async Task<PlanOutput> GetPlanWithProducts(long id, long siteId)
        {
            Plan plan = GetById(id);
            // bool isParentPlan = plan.ParentId.HasValue;
            PlanOutput planOutput = _mapper.Map<PlanOutput>(plan);
            //    planOutput.Parent = isParentPlan ? _mapper.Map<PlanOutput>(plan.Parent) : null;
            var latestVersionId = _planVersionsService.GetLatestVersionId(id);
            //   planOutput.ChildsPlans = await GetPlanChilds(plan.Id, siteId);
            //foreach (var child in planOutput.ChildsPlans)
            //{
            //    child.ChildsPlans = await GetPlanChilds(child.Id, siteId);
            //}
            var latestVersion = plan.Versions.Where(v => v.Id == latestVersionId).Select(x => new PlansVersionsOutput
            {
                Id = x.Id,
                CreatedById = x.CreatedById,
                CreationDate = x.CreationDate,
                RepeatAmount = x.RepeatAmount,
                RepeatFinal = x.RepeatFinal,
                RepeatInterval = x.RepeatInterval,
                RepeatPeriod = x.RepeatPeriod,
                RepeatStart = x.RepeatStart,
                RepeatTerm = x.RepeatTerm,
                VersionId = x.VersionId,
                DiscountId = x.DiscountId,
                DiscountValue = x.Discount != null ? x.Discount.DiscountValue : 0,
                PlanProducts = _mapper.Map<IList<PlanProductVersionOutput>>(x.PlanProductVersions)
            }).FirstOrDefault();
            if (latestVersion != null)
            {
                planOutput.LatestVersion = latestVersion;
                if (latestVersion.PlanProducts != null)
                {
                    planOutput.PlanProducts = latestVersion.PlanProducts.ToList();
                    planOutput.Price = latestVersion.PlanProducts.Select(price => price.Product.Price.HasValue ? price.Product.Price.Value : 0 * (price.Quantity != 0 ? price.Quantity : 1)).Sum() -
                        latestVersion.DiscountValue;

                }
            }
            //if (_workContext.GetCurrentUser().Result != null)
            //{
            //    planOutput.IsChildSubscribed = IsPlanChildSubscribed(planOutput.ChildsPlans, _workContext.GetCurrentUserIdValue());
            //}
            //else
            //{
            //    planOutput.IsChildSubscribed = false;
            //}
            return planOutput;
        }

        public async Task Update(PlanUpdate planInput)
        {
            Plan plan = GetById(planInput.Id);

            //plan.ParentId = planInput.ParentId;
            plan.IsFeatured = planInput.IsFeatured;
            plan.IsTopPicked = planInput.IsTopPicked;
            plan.IsCustomPlan = planInput.IsCustomPlan;
            plan.Status = planInput.Status;
            plan.Image = planInput.Image;
            plan.PlanType = planInput.PlanType;
            plan.RecurringType = planInput.RecurringType;
            plan.Order = planInput.Order;


            if (plan.PlanDetails.Any())
            {
                await UpdatePlanDetails(plan.PlanDetails, planInput.PlanDetails);

            }

            //update plan category 

            var planCategories = await _planCategoryRepository.Query().Where(x => x.PlanId == plan.Id).ToListAsync();
            if (planCategories.Count() > 0)
            {
                _planCategoryRepository.RemoveRange(planCategories);
            }


            IList<PlanCategory> addedPlanCategories = new List<PlanCategory>();

            foreach (var CategoryId in planInput.CategoryIds)
            {

                addedPlanCategories.Add(new PlanCategory() { PlanId = plan.Id, CategoryId = CategoryId });


            }
            _planCategoryRepository.AddRange(addedPlanCategories);
            await _planCategoryRepository.SaveChangesAsync();
        }

        public async Task<IList<PlanOutput>> GetPlansList(long siteId)
        {
            var userId = _workContext.GetCurrentUserIdValue();

            var cultureId = _workContext.GetCurrentCultureId();

            IList<PlanOutput> plans = await _planRepository.Query()
                .Where(x => x.Status != EStatus.Deleted && x.IsFeatured)
                .Include(x => x.CreatedBy)
                .Include(x => x.PlanDetails)
                .Include(x => x.Versions)
                .ThenInclude(x => x.PlanProductVersions).ThenInclude(y => y.Product)

                .Select(x => new PlanOutput
                {

                    CreatedById = x.CreatedById,
                    CreationDate = x.CreationDate,
                    Id = x.Id,
                    Description = x.PlanDetails.Count() > 0 ? x.PlanDetails.FirstOrDefault().Description : null,
                    Name = x.PlanDetails.Count() > 0 ? x.PlanDetails.FirstOrDefault().Name : null,
                    Image = x.Image,
                    IsFeatured = x.IsFeatured,
                    IsTopPicked = x.IsTopPicked,
                    IsCustomPlan = x.IsCustomPlan,
                    Price = x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price.HasValue ? p.Product.Price.Value : 0).AsQueryable().Sum(),
                    PlanType = x.PlanType,
                    SiteId = x.SiteId,
                    Status = x.Status,
                    Versions = _mapper.Map<IList<PlansVersionsOutput>>(x.Versions)
                })
                .ToListAsync();

            return plans;
        }
        public async Task<IList<PlanOutput>> GetPlansByIds(IList<long> ids, long siteId)
        {
            List<PlanOutput> plans = new List<PlanOutput>();
            foreach (var planId in ids)
            {
                var plan = await GetPlan(planId, siteId);
                plans.Add(plan);
            }
            return plans;
        }
        public async Task<IList<PlanOutput>> GetOneTimePlans(long siteId)
        {
            var cultureId = _workContext.GetCurrentCultureId();
            IList<PlanOutput> plans = await _planRepository.Query()
                           .Where(x => x.SiteId == siteId && x.Status != EStatus.Deleted && x.IsFeatured && x.RecurringType == RecurringTypes.OneTime && x.PlanType != PlanTypes.Trial)
                           .Include(x => x.PlanDetails)
                            .Select(x => new PlanOutput
                            {
                                Price = (
                                 x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount != null ?//first condition
                                 (
                                  x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountType == DiscountTypes.Percentage ? //second condition
                                  (
                                  (x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price.HasValue ? p.Product.Price.Value : 0 * (p.Quantity != 0 ? p.Quantity : 1)).AsQueryable().Sum() -
                                          (
                                          (x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price.HasValue ? p.Product.Price.Value : 0).AsQueryable().Sum() *
                                          (x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue != 0 ? x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue / 100 : 1)))
                                          ) / (x.PlanType == PlanTypes.ThreeMonthPlan ? 3 : 1)
                                  )
                                  :
                                   (
                                     (x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price.HasValue ? p.Product.Price.Value : 0 * (p.Quantity != 0 ? p.Quantity : 1)).AsQueryable().Sum() -
                                     (x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue)) / (x.PlanType == PlanTypes.ThreeMonthPlan ? 3 : 1)
                                     )
                                  )
                                  :
                                 (
                                 x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price.HasValue ? p.Product.Price.Value : 0 * (p.Quantity != 0 ? p.Quantity : 1)).AsQueryable().Sum()
                                 )
                                 ),
                                Id = x.Id,
                                IsFeatured = x.IsFeatured,
                                IsTopPicked = x.IsTopPicked,
                                IsCustomPlan = x.IsCustomPlan,
                                Name = x.PlanDetails.Where(x => x.CultureId == cultureId).FirstOrDefault().Name,
                                PlanType = x.PlanType,
                                ShortDescription = x.PlanDetails.Where(x => x.CultureId == cultureId).FirstOrDefault().ShortDescription,
                                Description = x.PlanDetails.Where(x => x.CultureId == cultureId).FirstOrDefault().Description,
                            })
                           .ToListAsync();

            return plans.OrderBy(x => x.Order).ToList();
        }
        public async Task<IList<PlanOutput>> GetRecurringPlans(long siteId)
        {
            var cultureId = _workContext.GetCurrentCultureId();
            var plans = await _planRepository.Query()
                              .Where(x => x.SiteId == siteId && x.Status != EStatus.Deleted && x.IsFeatured && x.RecurringType == RecurringTypes.Recurring)
                              .Include(x => x.PlanDetails)
                              .Select(x => new PlanOutput
                              {
                                  Id = x.Id,
                                  Name = x.PlanDetails.Where(x => x.CultureId == cultureId).FirstOrDefault().Name,
                                  PlanType = x.PlanType,
                                  RecurringType = x.RecurringType,
                                  IsCustomPlan = x.IsCustomPlan,
                                  IsFeatured = x.IsFeatured,
                                  IsTopPicked = x.IsTopPicked,
                                  Order = x.Order,
                                  Price = (
                                  x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount != null ?//first condition
                                  (
                                   x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountType == DiscountTypes.Percentage ? //second condition
                                   (
                                   (x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price.HasValue ? p.Product.Price.Value : 0 *(p.Quantity != 0 ? p.Quantity : 1)).AsQueryable().Sum() -
                                           (
                                           (x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price.HasValue ? p.Product.Price.Value : 0).AsQueryable().Sum() *
                                           (x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue != 0 ? x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue / 100 : 1)))
                                           ) / (x.PlanType == PlanTypes.ThreeMonthPlan ? 3 : 1)
                                   )
                                   :
                                    (
                                      (x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price.HasValue ? p.Product.Price.Value : 0 * (p.Quantity != 0 ? p.Quantity : 1)).AsQueryable().Sum() -
                                      (x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue)) / (x.PlanType == PlanTypes.ThreeMonthPlan ? 3 : 1)
                                      )
                                   )
                                   :
                                  (
                                  x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price.HasValue ? p.Product.Price.Value : 0 * (p.Quantity != 0 ? p.Quantity : 1)).AsQueryable().Sum()
                                  )
                                  )
                              })
                              .ToListAsync();

            //foreach (var item in plans)
            //{
            //    if (item.ChildsPlans.Count > 0)
            //    {
            //        foreach (var child in item.ChildsPlans)
            //        {
            //            child.ChildsPlans = await GetPlanChilds(child.Id, siteId);
            //            if (child.ChildsPlans.Count > 0)
            //            {
            //                foreach (var subChild in child.ChildsPlans)
            //                {
            //                    subChild.ChildsPlans = await GetPlanChilds(subChild.Id, siteId);
            //                }
            //            }

            //        }
            //    }

            //}
            return plans.OrderBy(x => x.Order).ToList();
        }

        public async Task<PlanOutput> GetPlanById(long id)
        {
            PlanOutput plan = await _userplanRepository.Query()
                .Include(x => x.PlanVersions)
                .ThenInclude(x => x.Plan)
                .Where(x => x.Id == id)
                .Select(x => new PlanOutput
                {
                    PlanType = x.PlanVersions.Plan.PlanType,
                    CreationDate = (x.PlanVersions.Plan.PlanType == PlanTypes.OneMonthPlan
                || x.PlanVersions.Plan.PlanType == PlanTypes.Trial) ?
                x.ActivationDate.AddMonths(1) : x.ActivationDate.AddMonths(3)
                }).FirstOrDefaultAsync();
            return plan;
        }

        public async Task<long> CreateCustomePlan(CustomePlanDto model)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();

                var PlanObj = await _planRepository.Query().Where(x => model.CustomePlanId == x.Id && x.IsCustomPlan).FirstOrDefaultAsync();

                if (PlanObj is null)
                {
                    throw new Exception("PLan Not Exist");

                }
                var PlanVersionObj = _planVersionsService.Get(model.PlansVersionsCustome.PlanVersionId);


                if (PlanVersionObj is null)
                {
                    throw new Exception("PlanVersionObj Not Exist");

                }

                var ObjVersion = new PlansVersionsCreate()
                {
                    DiscountId = PlanVersionObj.DiscountId,
                    CartRuleId = PlanVersionObj.CartRuleId,
                    PlanId = model.CustomePlanId,
                    RepeatStart = PlanVersionObj.RepeatStart,
                    RefundPeriod = PlanVersionObj.RefundPeriod,
                    RepeatAmount = PlanVersionObj.RepeatAmount,
                    RepeatFinal = PlanVersionObj.RepeatFinal,
                    RepeatInterval = PlanVersionObj.RepeatInterval,
                    RepeatPeriod = PlanVersionObj.RepeatPeriod.ToString(),
                    RepeatTerm = PlanVersionObj.RepeatTerm,
                    TaxClassId = PlanVersionObj.TaxClassId
                };

                var PlanVersionId = await _planVersionsService.Create(ObjVersion, siteId);

                var ProductPlanVersions = model.PlansVersionsCustome.planProductVersions.ConvertAll(y => new PlanProductVersionCreate
                {
                    PlanVersionId = PlanVersionId,
                    Status = EStatus.Published,
                    ProductId = y.ProductId.Value,
                    Quantity = y.Quantity.Value

                });
                foreach (var ProductPlanVersion in ProductPlanVersions)
                {

                    await planProductVersionService.Create(ProductPlanVersion);
                }

                return PlanVersionId;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #region Helpers
        private Plan GetById(long id)
        {
            return _planRepository.Query()
                .Where(x => x.Id == id)
                .Include(x => x.Categories)
                .Include(x => x.PlanDetails)
                .Include("PlanDetails.Culture")
                .Include(x => x.Versions)
                .ThenInclude(x => x.PlanProductVersions)
                .ThenInclude(x => x.Product)
                .Include("Versions.Discount")
                .Include("Versions.TaxClass")
                .Include("Versions.TaxClass.TaxRate")
                .FirstOrDefault();
        }
        public async Task<bool> UpdatePlanDetails(IList<PlanDetails> planDetailsList, List<PlanCultureDetailsVm> planInputDetails)
        {
            if (planDetailsList.Any())
            {

                foreach (var item in planInputDetails)
                {
                    var planDetails = planDetailsList.Where(x => x.CultureId == item.CultureId).FirstOrDefault();
                    if (planDetails != null)
                    {
                        planDetails.Name = item.Name;
                        planDetails.Description = item.Description;
                        planDetails.ShortDescription = item.ShortDescription;
                        planDetails.NormalizedName = item.Name.ToUpper();
                        _planDetailsRepository.Update(planDetails);
                    }
                    else
                    {
                        var newPlanDetails = new PlanDetails
                        {
                            Name = item.Name,
                            CultureId = item.CultureId,
                            Description = item.Description,
                            ShortDescription = item.ShortDescription,
                        };

                        _planDetailsRepository.Add(newPlanDetails);
                    }
                }
                _planDetailsRepository.SaveChanges();
                await _planRepository.SaveChangesAsync();

                return true;
            }

            return false;
        }

        private async Task<bool> IsSubscribedPlan(long PlanId, long userId)
        {
            if (PlanId > 0)
            {
                var planVersionsIds = (await _planVersionsService.GetPlanVersions(PlanId)).Items.Select(x => x.Id).ToList();
                return await _userplanRepository.Query()
                    .Include(x => x.PlanVersions)
                    .AnyAsync(x => x.UserId == userId && planVersionsIds.Contains(x.PlanVersions.PlanId));
            }
            return false;
        }
        public async Task<bool> CreatePlanDetails(List<PlanCultureDetailsVm> planDetails, long planId)
        {
            var planDetailsToBeAdded = planDetails.Select(x => new PlanDetails
            {
                CultureId = x.CultureId,
                Description = x.Description,
                ShortDescription = x.ShortDescription,
                Name = x.Name,
                NormalizedName = x.Name.ToUpper(),
                PlanId = planId,
            }).ToList();
            _planDetailsRepository.AddRange(planDetailsToBeAdded);
            await _planRepository.SaveChangesAsync();
            return true;
        }

        public async Task<PlanOutput> GetPlan(long id, long siteId)
        {
            Plan plan = _planRepository.Query().FirstOrDefault(x => x.Id == id);
            PlanOutput planOutput = _mapper.Map<PlanOutput>(plan);
            // planOutput.ChildsPlans = await GetPlanChilds(plan.Id, siteId);
            //foreach (var child in planOutput.ChildsPlans)
            //{
            //    child.ChildsPlans = await GetPlanChilds(child.Id, siteId);
            //}
            return planOutput;
        }

        public async Task<PaginationResult<PlanResponseDto>> GetAll(SmartTableParam param, bool CustomePlan)
        {
            try
            {
                var userId = await _workContext.GetCurrentUserId();

                var cultureId = _workContext.GetCurrentCultureId();

                var query = _planCategoryRepository.Query()
                            .Include(x => x.Category)
                            .ThenInclude(x => x.CategoryDetails)
                            .Include(x => x.Plan)
                            .Include("Plan.PlanDetails")
                            .Include("Plan.Versions")
                            .Include("Plan.Versions.PlanProductVersions")
                            .Include("Plan.Versions.PlanProductVersions.Product")
                            .Include("Plan.Versions.PlanProductVersions.Product.ProductDetails")
                            .Where(x => x.Plan.Status == EStatus.Published);

                if (CustomePlan)
                {
                    query = query.Where(x => x.Plan.IsCustomPlan);
                }
                else
                {
                    query = query.Where(x => !x.Plan.IsCustomPlan);
                }
                query = ApplyFiltersPLan(param, query);


                var Result = await query.Select(x => x.Plan).Select(x => new PlanResponseDto
                {
                    Id = x.Id,
                    Image = x.Image,
                    Name = x.PlanDetails.Where(x => x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                    Description = x.PlanDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),
                    ShortDescription = x.PlanDetails.Where(x => x.CultureId == cultureId).Select(x => x.ShortDescription).FirstOrDefault(),
                    IsFeatured = x.IsFeatured,
                    IsTopPicked = x.IsTopPicked,
                    IsCustomPlan = x.IsCustomPlan,
                    Price = CustomePlan
                      ? x.Versions.Where(v => v.CreatedById == userId).OrderByDescending(x => x.Id).FirstOrDefault() == null ? 0 : CalculatePLanPrice(x.Versions.Where(v => v.CreatedById == userId).OrderByDescending(x => x.Id).FirstOrDefault().PlanProductVersions.ToList())
                       : x.Versions.Where(v => v.Id == x.Versions.Max(i => (long?)i.Id)).FirstOrDefault() == null ? 0 : CalculatePLanPrice(x.Versions.Where(v => v.Id == x.Versions.Max(i => (long?)i.Id)).FirstOrDefault().PlanProductVersions.ToList()),
                    PlanType = x.PlanType,
                    Status = x.Status,
                    ExistCustomePlanByUser = CustomePlan ? x.Versions.Where(v => v.Id == userId).Count() > 0 : false,
                    Versions = x.Versions.Where(v => v.Id == ( CustomePlan ? x.Versions.Min(i => (long?)i.Id) : x.Versions.Max(i => (long?)i.Id))).Select(x => new PlansVersionsOutput
                                           {
                                               VersionId = x.VersionId,
                                               RepeatStart = x.RepeatStart,
                                               CartRuleId = x.CartRuleId,
                                               CreatedById = x.CreatedById,
                                               CreationDate = x.CreationDate,
                                               DiscountId = x.DiscountId,
                                               DiscountValue = x.Discount == null ? 0 : x.Discount.DiscountValue,
                                               Id = x.Id,
                                               RepeatTerm = x.RepeatTerm,
                                               RepeatPeriod = x.RepeatPeriod,
                                               TaxClassId = x.TaxClassId,
                                               RepeatInterval = x.RepeatInterval,
                                               RepeatFinal = x.RepeatFinal,
                                               RefundPeriod = x.RefundPeriod,
                                               RepeatAmount = x.RepeatAmount,
                                               PlanProducts = x.PlanProductVersions.Where(x => x.Status != EStatus.Deleted).Select(i => new PlanProductVersionOutput
                                               {

                                                   Status = i.Status,
                                                   ProductId = i.ProductId,
                                                   Quantity = i.Quantity,
                                                   ProductName = i.Product.ProductDetails.Where(x => x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                                                   Description = i.Product.ProductDetails.Where(x => x.CultureId == cultureId).Select(x => x.Description).FirstOrDefault(),



                                               }).ToList()

                                           }).FirstOrDefault()
                }).PaginateAsync(param.Pagination.Number, param.Pagination.Start);

                return Result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private static decimal CalculatePLanPrice(List<PlanProductVersion> planProductVersions)
        {
            try
            {
                return planProductVersions.Count() == 0 ? 0 : planProductVersions.Select(x => x.Product.Price.HasValue ? x.Product.Price.Value : 0 * x.Quantity).Sum();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private IQueryable<PlanCategory> ApplyFiltersPLan(dynamic param, IQueryable<PlanCategory> query)
        {
            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;


                if (search.CategoryIds != null)
                {

                    List<long> CategoryIds = new List<long>();

                    foreach (dynamic CategoryId in search.CategoryIds)
                    {
                        long Category = Convert.ToInt64(CategoryId);
                        CategoryIds.Add(Category);
                    }


                    query = query.Where(x => CategoryIds.Any(y => x.CategoryId == y));
                }

                if (search.IsFeatured != null)
                {

                    bool IsFeatured = Convert.ToBoolean(search.IsFeatured);
                    if (IsFeatured)
                        query = query.Where(x => x.Plan.IsFeatured);
                    else
                        query = query.Where(x => !x.Plan.IsFeatured);

                }

                if (search.Name != null)
                {
                    string name = search.Name;
                    query = query.Where(x => x.Plan.PlanDetails.Any(x => x.NormalizedName.Contains(name.ToUpper())));
                }

                if (search.IsTopPicked != null)
                {
                    bool isTopPicked = search.IsTopPicked;
                    query = query.Where(x => x.Plan.IsTopPicked == isTopPicked);
                }
                if (search.PlanType != null)
                {
                    var planType = (PlanTypes)search.PlanType;
                    query = query.Where(x => x.Plan.PlanType == planType);
                }
                if (search.RecurringType != null)
                {
                    var recurringType = (RecurringTypes)search.RecurringType;
                    query = query.Where(x => x.Plan.RecurringType == recurringType);
                }


                return query;
            }

            return query;

        }


        #endregion

        #region Commented Services Parents
        //            public async Task<SmartTableResult<PlanOutput>> GetAll(SmartTableParam input)
        //        {
        //            IList<Plan> plans = await _planRepository.Query()
        //                .Where(x => x.Status != EStatus.Deleted)
        //                .Include(x => x.CreatedBy)
        //                .Include(x => x.PlanProducts)
        //                .Include(x => x.Versions)
        //                .Skip((input.Pagination.Start - 1) * input.Pagination.Number)
        //                .Take(input.Pagination.Number)
        //                .ToListAsync();

        //            return new SmartTableResult<PlanOutput>
        //            {
        //                Items = _mapper.Map<IEnumerable<PlanOutput>>(plans),
        //                NumberOfPages = input.Pagination.Number,
        //                TotalRecord = _planRepository.Query().Count()
        //            };
        //}


        //private bool IsPlanChildSubscribed(IList<PlanOutput> planChilds, long userId)
        //{
        //    // var planChilds = _planRepository.Query().Where(c => c.ParentId == planId).ToList();
        //    if (planChilds.Count > 0)
        //    {
        //        bool isSubscribed;
        //        for (int i = 0; i < planChilds.Count; i++)
        //        {
        //            var userPlansChilds = _userplanRepository.Query()
        //            .Include(p => p.PlanVersions)
        //            .ThenInclude(x => x.Plan)
        //            .Where(up => planChilds[i].Id == up.PlanVersions.Plan.Id && up.UserId == userId && up.IsSubscribed)
        //            .ToList()
        //            .Count();
        //            isSubscribed = userPlansChilds > 0;
        //            planChilds[i].IsChildSubscribed = isSubscribed;
        //            for (int z = 0; z < planChilds[i].ChildsPlans.Count; z++)
        //            {
        //                var childs = _userplanRepository.Query()
        //            .Include(p => p.PlanVersions)
        //            .ThenInclude(x => x.Plan)
        //            .Where(up => planChilds[i].ChildsPlans[z].Id == up.PlanVersions.Plan.Id && up.UserId == userId && up.IsSubscribed)
        //            .ToList()
        //            .Count();
        //                isSubscribed = userPlansChilds > 0;
        //                planChilds[i].ChildsPlans[z].IsChildSubscribed = isSubscribed;
        //            }
        //        }
        //        if (planChilds.Any(x => x.IsChildSubscribed) || planChilds.Any(x => x.ChildsPlans.Any(c => c.IsChildSubscribed)))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        //private async Task<IList<PlanOutput>> GetPlanChilds(long planId, long SiteId)
        //{

        //    var cultureId = _workContext.GetCurrentCultureId();


        //    IList<PlanOutput> planChilds = await _planRepository.Query()
        //        .Where(x => x.ParentId == planId && x.Status != EStatus.Deleted && x.RecurringType == RecurringTypes.Recurring)
        //             .Include(x => x.Parent)
        //             .Include(x => x.CreatedBy)
        //             .Include(x => x.Versions)
        //             .ThenInclude(x => x.PlanProductVersions)
        //             .ThenInclude(y => y.Product)
        //             .Include("Versions.Discount")
        //             .Include("Versions.TaxRate")
        //             .Include("Versions.TaxRate.TaxClass")

        //         .Select(x => new PlanOutput
        //         {
        //             Id = x.Id,
        //             Image = x.Image,
        //             IsFeatured = x.IsFeatured,
        //             IsTopPicked = x.IsTopPicked,
        //             IsCustomPlan = x.IsCustomPlan,
        //             Name = x.PlanDetails.Where(x=>x.CultureId == cultureId).FirstOrDefault().Name,
        //             Price = (
        //             x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Disc
        //             ount != null ?//first condition
        //             (
        //              x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountType == DiscountTypes.Percentage ? //second condition
        //              (
        //              (x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price * (p.Quantity != 0 ? p.Quantity : 1)).AsQueryable().Sum() -
        //                      (
        //                      (x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price).AsQueryable().Sum() *
        //                      (x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue != 0 ? x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue / 100 : 1)))
        //                      ) / (x.PlanType == PlanTypes.ThreeMonthPlan ? 3 : 1)
        //              )
        //              :
        //               (
        //                 (x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price * (p.Quantity != 0 ? p.Quantity : 1)).AsQueryable().Sum() -
        //                 (x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue)) / (x.PlanType == PlanTypes.ThreeMonthPlan ? 3 : 1)
        //                 )
        //              )
        //              :
        //             (
        //             x.Versions.FirstOrDefault(pv => pv.Id == x.Versions.Max(i => i.Id)).PlanProductVersions.Select(p => p.Product.Price * (p.Quantity != 0 ? p.Quantity : 1)).AsQueryable().Sum()
        //             )
        //             ),
        //             ShortDescription = x.PlanDetails.Where(x => x.CultureId == cultureId).FirstOrDefault().Name,
        //             SiteId = x.SiteId,
        //             Status = x.Status,
        //             PlanType = x.PlanType,
        //             DiscountId = x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.Id,
        //             DicountValue = x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue,
        //             DiscountType = x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountType,
        //             TaxRate = x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).TaxRate,
        //             //IsSubscribed = IsSubscribedPlan(planId, _workContext.GetCurrentUser(SiteId).Result.Id)
        //             //ChildsPlans = GetPlanSubChilds(x.Id, SiteId)
        //         }).ToArrayAsync();
        //    return planChilds;
        //}

        //private IList<PlanOutput> GetPlanSubChilds(long planId, long SiteId)
        //{
        //    IList<PlanOutput> planChilds = _planRepository.Query().Where(x => x.ParentId == planId && x.Status == EStatus.Active)
        //             .Include(x => x.Parent)
        //             .Include(x => x.CreatedBy)
        //             .Include(x => x.Versions)
        //             .ThenInclude(x => x.PlanProductVersions)
        //             .ThenInclude(y => y.Product)
        //             .Include("Versions.Discount")
        //             .Include("Versions.TaxRate")
        //             .Include("Versions.TaxRate.TaxClass")

        //         .Select(x => new PlanOutput
        //         {
        //             Id = x.Id,
        //             Image = x.Image,
        //             IsFeatured = x.IsFeatured,
        //             IsTopPicked = x.IsTopPicked,
        //             IsCustomPlan = x.IsCustomPlan,
        //             //Name = x.Name,
        //             //ShortDescription = x.ShortDescription,
        //             SiteId = x.SiteId,
        //             Status = x.Status,
        //             PlanType = x.PlanType,
        //             DicountValue = x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue,
        //             DiscountType = x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountType,
        //             TaxRate = x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).TaxRate
        //         }).ToArray();
        //    return planChilds;
        //}

        //private IList<PlanOutput> Checkif(long planId, long SiteId)
        //{
        //    IList<PlanOutput> planChilds = _planRepository.Query().Where(x => x.ParentId == planId && x.Status == EStatus.Active)
        //             .Include(x => x.Parent)
        //             .Include(x => x.CreatedBy)
        //             .Include(x => x.Versions)
        //             .ThenInclude(x => x.PlanProductVersions)
        //             .ThenInclude(y => y.Product)
        //             .Include("Versions.Discount")
        //             .Include("Versions.TaxRate")
        //             .Include("Versions.TaxRate.TaxClass")

        //         .Select(x => new PlanOutput
        //         {
        //             Id = x.Id,
        //             Image = x.Image,
        //             IsFeatured = x.IsFeatured,
        //             IsTopPicked = x.IsTopPicked,
        //             IsCustomPlan = x.IsCustomPlan,
        //             //Name = x.Name,
        //             //ShortDescription = x.ShortDescription,
        //             SiteId = x.SiteId,
        //             Status = x.Status,
        //             PlanType = x.PlanType,
        //             DicountValue = x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountValue,
        //             DiscountType = x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).Discount.DiscountType,
        //             TaxRate = x.Versions.FirstOrDefault(v => v.Id == x.Versions.Max(i => i.Id)).TaxRate
        //         }).ToArray();
        //    return planChilds;
        //}
        #endregion

    }
}
