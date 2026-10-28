using AutoMapper;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.UserCatalog.Dtos;
using EskaCMS.Entities.Entity;
using EskaCommerce.UserCatalog.Helpers;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;
using EskaCMS.Entities.Enum;
using EskaCMS.Entities.Entity.Orders;

namespace EskaCommerce.UserCatalog.Services
{

    public class UserPlansService : IUserPlansService
    {
        private readonly DateTimeOffset TimeNow = DateTimeOffset.Now;

        private readonly IRepository<UserPlans> _userplanRepository;
        private readonly IRepository<UserPlansHistory> _userplanHistoryRepository;
        private readonly IRepository<PlansVersions> _plansVersionsRepository;
        private readonly IRepository<Order> _orderRepository; // Firas to be removed from here and use the service instead
        private readonly IRepository<Plan> _planRepository;
        private readonly IRepository<UserPlanProducts> _userplanProductsRepository;
        private readonly IMapper _mapper;
        private readonly IWorkContext _workContext;
        public UserPlansService(
            IRepository<UserPlans> userplanRepository,
            IWorkContext workContext,
            IRepository<PlansVersions> plansVersionsRepository,
            IMapper mapper,
            IRepository<UserPlansHistory> userplanHistoryRepository,
            IRepository<Plan> planRepository,
            IRepository<Order> orderRepository,
            IRepository<UserPlanProducts> userplanProductsRepository
            )
        {
            _userplanRepository = userplanRepository;
            _workContext = workContext;
            _plansVersionsRepository = plansVersionsRepository;
            _mapper = mapper;
            _userplanHistoryRepository = userplanHistoryRepository;
            _planRepository = planRepository;
            _orderRepository = orderRepository;
            _userplanProductsRepository = userplanProductsRepository;
        }

        public async Task<long> Create(long planId, long siteId, long OrderId, string AdditionInfo, EUserPlansStatus CurrentStatus, EUserPlansStatus NextStatus, bool? IsOneTimeService = null, long? parentId = null)
        {
            try
            {

                var userId = await _workContext.GetCurrentUserId();
                PlansVersionsOutput plansVersion = await GetPlanLatestVersionId(planId);
                bool isSubscribed = await IsUserSubscribedVersion(plansVersion.Id, userId);
                if (isSubscribed && IsOneTimeService.HasValue && (bool)!IsOneTimeService)
                {
                    throw new Exception("You are already subscribed in this version");
                }
                else
                {
                    UserPlans userplan = new UserPlans
                    {
                        PlanVersionsId = plansVersion.Id,
                        UserId = userId,
                        AgreementId = AdditionInfo,
                        OrderId = OrderId,
                        ActivationDate = DateTimeOffset.Now,
                        CurrentStatus = CurrentStatus,
                        NextStatus = NextStatus,
                        NextStatusDate = await GetNextStatusDate(planId),
                        CurrentStatusDate = DateTimeOffset.Now,
                        ParentId = parentId
                    };
                    _userplanRepository.Add(userplan);
                    await _userplanRepository.SaveChangesAsync();
                    await AddUserPlanProducts(userplan.Id, OrderId);
                    await CreatePlanHistory(userplan.Id, null, EUserPlansStatus.WaitingPayment, AdditionInfo);
                    return userplan.Id;
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }


        public async Task AddUserPlanProducts(long UserPlanId, long OrderId)
        {
            UserPlanProducts userplanprod = new UserPlanProducts();
            //var Order = await _orderService.GetOrderById(OrderId);
            var Order = await _orderRepository.Query().Where(x => x.Id == OrderId).Include(x => x.OrderItems).ThenInclude(x => x.Product).Include(x => x.ShippingAddress).ThenInclude(x => x.StateOrProvince).FirstOrDefaultAsync();
            userplanprod.UserPlanId = UserPlanId;
            foreach (var item in Order.OrderItems)
            {
                userplanprod.ProductId = item.ProductId;
                userplanprod.Quantity = item.Quantity;
                userplanprod.ProductPrice = item.ProductPrice;

            }
            _userplanProductsRepository.Add(userplanprod);
            await _userplanRepository.SaveChangesAsync();
        }

        public UserPlansOutput Get(long id)
        {
            UserPlans plan = GetById(id);
            UserPlansOutput planOutput = _mapper.Map<UserPlansOutput>(plan);
            planOutput.PlanVersions = _mapper.Map<PlansVersionsOutput>(plan.PlanVersions);
            return planOutput;
        }
        public async Task<PlansVersionsOutput> GetPlanLatestVersion(long planId)
        {
            return await GetPlanLatestVersionId(planId);
        }
        public async Task<IEnumerable<UserPlansOutput>> GetUserPlans(long siteId)
        {
            long userId = await _workContext.GetCurrentUserId();
            var cultureId = _workContext.GetCurrentCultureId();
            IEnumerable<UserPlansOutput> plans = (from userPlan in _userplanRepository.Query()
                                                  .Include(x => x.PlanVersions)
                                                  .ThenInclude(C => C.Plan)
                                                  .ThenInclude(y=>y.PlanDetails)

                                                  .AsEnumerable()
                                                  where userPlan.UserId == userId &&
                                                  (userPlan.CurrentStatus == EUserPlansStatus.Subscribed || userPlan.CurrentStatus == EUserPlansStatus.Scheduled)
                                                  && userPlan.PlanVersions.Plan.RecurringType == RecurringTypes.Recurring
                                                  select new { plan = userPlan })

                                          .Select(x => new UserPlansOutput
                                          {
                                              //added by Mahmoud To get the name and id of the user plan start
                                              Id = x.plan.Id,
                                              PlanName = x.plan.PlanVersions.Plan.PlanDetails.Where(x=>x.CultureId == cultureId).FirstOrDefault().Name,
                                              //end
                                              ActivationDate = x.plan.ActivationDate,
                                              IsSubscribed = x.plan.IsSubscribed,
                                              AgreementId = x.plan.AgreementId,
                                              PlanVersions = _mapper.Map<PlansVersionsOutput>(x.plan.PlanVersions),
                                              UserId = x.plan.UserId,
                                              PlanVersionsId = x.plan.PlanVersionsId,
                                              OrderId = x.plan.OrderId,
                                              CurrentStatus = x.plan.CurrentStatus,
                                              CurrentStatusDate = x.plan.CurrentStatusDate,
                                              NextStatusDate = x.plan.NextStatusDate,
                                              NextStatus = x.plan.NextStatus,
                                              IsUpgradable = x.plan.PlanVersions.Plan.PlanType == PlanTypes.OneMonthPlan,
                                              PlanType = x.plan.PlanVersions.Plan.PlanType
                                          });

            return plans.OrderByDescending(x => x.ActivationDate);

        }
        public async Task<IEnumerable<UserPlansOutput>> GetUserProfileServices(long siteId)
        {
            long userId = await _workContext.GetCurrentUserId();
            var cultureId = _workContext.GetCurrentCultureId();
            IEnumerable<UserPlansOutput> plans = (from userPlan in _userplanRepository.Query()
                                                  .Include(x => x.PlanVersions)
                                                  .ThenInclude(C => C.Plan)
                                                  .AsEnumerable()
                                                  where userPlan.UserId == userId && userPlan.IsSubscribed && userPlan.PlanVersions.Plan.RecurringType == RecurringTypes.Recurring
                                                  select new { plan = userPlan })

                                          .Select(x => new UserPlansOutput
                                          {
                                              //added by Mahmoud To get the name and id of the user plan start
                                              Id = x.plan.Id,
                                              PlanName = x.plan.PlanVersions.Plan.PlanDetails.Where(x=>x.CultureId == cultureId).FirstOrDefault()?.Name,
                                              //end
                                              IsSubscribed = x.plan.IsSubscribed,
                                              AgreementId = x.plan.AgreementId,
                                              PlanVersions = _mapper.Map<PlansVersionsOutput>(x.plan.PlanVersions),
                                              UserId = x.plan.UserId,
                                              PlanVersionsId = x.plan.PlanVersionsId,
                                              OrderId = x.plan.OrderId,
                                              CurrentStatus = x.plan.CurrentStatus,
                                              NextStatus = x.plan.NextStatus,
                                              IsUpgradable = x.plan.PlanVersions.Plan.PlanType == PlanTypes.OneMonthPlan,
                                              PlanType = x.plan.PlanVersions.Plan.PlanType
                                          });

            return plans;

        }
        public async Task<IList<UserPlansOutput>> GetUserPlansById(long siteId, long UserId)
        {
            //User user = await _workContext.GetCurrentUser(siteId);
            IList<UserPlansOutput> plans = await _userplanRepository.Query()
                .Where(x => x.UserId == UserId)
                .Include(x => x.Order)
               .Include(x => x.PlanVersions)
               .ThenInclude(x => x.Plan)
               .Select(x => new UserPlansOutput
               {
                   //added by Mahmoud To get the name and id of the user plan start
                  // PlanName = x.PlanVersions.Plan.Name, /*_planRepository.Query().Where(y => y.Id == x.PlanVersions.PlanId).Select(x => x.Name).FirstOrDefault(),*/
                   //end
                   ActivationDate = x.ActivationDate,
                   CurrentStatusDate = x.CurrentStatusDate,
                   NextStatusDate = x.NextStatusDate,
                   CurrentStatus = x.CurrentStatus,
                   NextStatus = x.NextStatus,
                   AgreementId = x.AgreementId,
                   PlanVersions = _mapper.Map<PlansVersionsOutput>(x.PlanVersions),
                   UserId = x.UserId,
                   PlanVersionsId = x.PlanVersionsId,
                   Order = x.Order,
                   OrderId = x.OrderId
               })
               .ToListAsync();
            return plans;

        }

        public async Task<IEnumerable<UserInvoicesOutputVM>> GetUserInvoices(long siteId)
        {
            var user = await _workContext.GetCurrentUser();
            var cultureId = _workContext.GetCurrentCultureId();

            var InvoiceObj = (from u in _userplanRepository.Query().AsNoTracking()
                              .Where(x => x.UserId == user.Id
                              && x.CurrentStatus != EUserPlansStatus.Canceled
                              && x.Order.OrderStatus == OrderStatus.PaymentReceived)
                              .Include(x => x.Order)
                              .ThenInclude(x => x.OrderItems)
                              .ThenInclude(x => x.Product)
                              .ThenInclude(x=>x.ProductDetails)
                              .Include("Order.ShippingAddress")
                              .Include(x => x.PlanVersions.Discount)
                              .Include(x => x.PlanVersions)
                              .ThenInclude(x => x.Plan)
                             // .ThenInclude(x => x.Parent)
                              .ThenInclude(x=>x.PlanDetails)
                               .ToList()
                              join o in _orderRepository.Query()
                              .Where(x => x.CustomerId == user.Id && x.OrderStatus == OrderStatus.PaymentReceived)
                              .Include(x => x.OrderItems)
                              .ThenInclude(x => x.Product)
                              .Include(x => x.ShippingAddress) on u.OrderId equals o.Id
                              where o.Id == u.OrderId
                              select new { o, u }).ToList();

            var InvoiceData = InvoiceObj.Select(x => new UserInvoicesOutputVM
            {
                //InvoiceName = x.u.PlanVersions.Plan.Parent.PlanDetails.Where(x=>x.CultureId == cultureId).FirstOrDefault().Name,
                SubscribeDate = x.o.LatestUpdatedOn,
                TotalPaid = x.o.OrderTotal,
                OrderId = x.o.Id,
                InvoiceDetails = new UserInvoiceDetailsVM
                {
                    CustomerEmail = user.Email,
                    CustomerName = user.FullName,
                    Discount = x.o.DiscountAmount,
                    InvoiceNo = x.o.Id,
                    OrderTotal = x.o.OrderTotal,
                    Shipping = x.o.ShippingFeeAmount,
                    ShippingAddress = x.o.ShippingAddress==null?null: x.o.ShippingAddress.AddressLine1,
                    SubscribeDate = x.o.LatestUpdatedOn,
                    Subtotal = x.o.SubTotal,
                    Tax = x.o.TaxAmount,
                    TotalPaid = x.o.OrderTotal,
                    InvoiceItems = x.o.OrderItems.Select(i => new InvoiceItemVM
                    {
                        DiscountAmount = x.o.DiscountAmount,
                        ItemName = i.Product.ProductDetails.Select(x=>x.Name).FirstOrDefault(),
                        Price = i.ProductPrice,
                        Quantity = i.Quantity,
                        TaxAmount = i.TaxAmount,
                        TaxPercent = i.TaxPercent,
                        Total = i.ProductPrice,
                        RowTotal = (i.Quantity * i.ProductPrice) + i.TaxAmount
                    }).ToList()
                }
            }).ToList();

            IList<UserInvoicesOutputVM> productsInvoices = await _orderRepository.Query()
                .Where(x => x.CustomerId == user.Id && x.OrderStatus == OrderStatus.PaymentReceived)
                .Include(x => x.OrderItems)
               .ThenInclude(x => x.Product)

                              .ThenInclude(x => x.ProductDetails)
               .Include(x => x.ShippingAddress)
               .Select(x => new UserInvoicesOutputVM
               {
                   //InvoiceName = _userplanRepository.Query().Where(u => u.OrderId == x.Id)
                   //.Include(x => x.PlanVersions)
                   //.ThenInclude(x => x.Plan)
                   ////ThenInclude(x => x.Parent)
                   //.ThenInclude(x=>x.PlanDetails)
                   //.Select(x => x.PlanVersions.Plan.Parent.PlanDetails.Where(x => x.CultureId == cultureId).FirstOrDefault().Name)
                   //.FirstOrDefault() ?? "Invoice #" + x.Id,
                   SubscribeDate = x.LatestUpdatedOn,
                   TotalPaid = x.OrderTotal,
                   OrderId = x.Id,
                   InvoiceDetails = new UserInvoiceDetailsVM
                   {
                       CustomerEmail = user.Email,
                       CustomerName = user.FullName,
                       Discount = x.DiscountAmount,
                       InvoiceName = x.OrderItems.First().Product.ProductDetails.Select(x=>x.Name).FirstOrDefault() + (x.OrderItems.Count - 1).ToString() + " more products ",
                       InvoiceNo = x.Id,
                       OrderTotal = x.OrderTotal,
                       Shipping = x.ShippingFeeAmount,
                       ShippingAddress = x.ShippingAddress==null?null: x.ShippingAddress.AddressLine1,
                       SubscribeDate = x.LatestUpdatedOn,
                       Subtotal = x.SubTotal,
                       Tax = x.TaxAmount,
                       TotalPaid = x.OrderTotal,
                       InvoiceItems = x.OrderItems.Select(i => new InvoiceItemVM
                       {
                           DiscountAmount = i.DiscountAmount,
                           ItemName = i.Product.ProductDetails.Select(x => x.Name).FirstOrDefault(),
                           Price = i.ProductPrice,
                           Quantity = i.Quantity,
                           TaxAmount = i.TaxAmount,
                           TaxPercent = i.TaxPercent,
                           Total = i.ProductPrice,
                           RowTotal = i.ProductPrice + i.TaxAmount + i.DiscountAmount
                       }).ToList()
                   }
               })
               .ToListAsync();
            var response = InvoiceData.Concat(productsInvoices).Distinct(new InvoiceComparer()).OrderByDescending(x => x.SubscribeDate).ToList();

            //var response = InvoiceData.OrderByDescending(x => x.SubscribeDate);
            return response;

        }

        private async Task<DateTimeOffset> GetNextStatusDate(long PlanId)
        {
            try
            {
                var PlanVersionDetials = await GetPlanLatestVersionId(PlanId);
                ///   var PlanVersionDetials = _plansVersionsRepository.Query().Where(X => X.PlanId == PlanId).FirstOrDefault();
                ///   

                DateTimeOffset NextStatusDate = DateTimeOffset.Now;
                switch (PlanVersionDetials.RepeatPeriod)
                {
                    case PlanEnums.RepeatPeriod.M:
                        return NextStatusDate = NextStatusDate.AddMonths(PlanVersionDetials.RepeatInterval);

                    case PlanEnums.RepeatPeriod.W:
                        return NextStatusDate = NextStatusDate.AddDays(PlanVersionDetials.RepeatInterval * 7);


                    default:
                        return NextStatusDate;

                }
            }
            catch (Exception exc)
            {

                throw;
            }
        }
        public async Task Update(UserPlansUpdate planInput)
        {
            await _userplanRepository.ExecuteInTransactionAsync(async () =>
            {
                // Insert into table user plan history
                UserPlans plan = GetById(planInput.Id);

                if (!string.IsNullOrEmpty(planInput.AgreementId))
                {
                    plan.AgreementId = planInput.AgreementId;
                }
                plan.CurrentStatus = planInput.CurrentStatus;
                plan.NextStatus = planInput.NextStatus;
                plan.CurrentStatusDate = TimeNow;
                plan.NextStatusDate = await GetNextStatusDate(plan.PlanVersions.PlanId);
                plan.IsSubscribed = planInput.IsSubscribed;
                plan.Tran_ref = planInput.Tran_Ref;
                plan.IsSubscribed = planInput.IsSubscribed;
                _userplanRepository.Update(plan);
                await _userplanRepository.SaveChangesAsync();
                await CreatePlanHistory(plan.Id, plan.CurrentStatus, planInput.NextStatus, planInput.AgreementId);
            });
        }
        public async Task<SmartTableResult<UserPlansOutput>> GetAll(SmartTableParam input)
        {
            IList<UserPlans> userPlans = await _userplanRepository.Query()
               .Include(x => x.PlanVersions)
               .Include(x => x.User)
               .Include(x => x.Order)
               .Skip((input.Pagination.Start - 1) * input.Pagination.Number)
               .Take(input.Pagination.Number)
               .ToListAsync();

            return new SmartTableResult<UserPlansOutput>
            {
                Items = _mapper.Map<IEnumerable<UserPlansOutput>>(userPlans).GroupBy(x => x.UserId).Select(x => x.First()),
                NumberOfPages = input.Pagination.Number,
                TotalRecord = _userplanRepository.Query().Count()
            };
        }

        private UserPlans GetById(long id)
        {
            return _userplanRepository.Query().Include(x => x.PlanVersions).FirstOrDefault(x => x.Id == id);
        }
        public async Task<IList<UserPlansOutput>> GetUserPlanByOrderId(long OrderId)
        {
            var Plan = await _userplanRepository
                 .Query()
                 .Where(x => x.OrderId == OrderId)
                 .Include(x => x.PlanVersions)
                 .ThenInclude(x => x.Plan)
                 .ThenInclude(x=>x.PlanDetails)
                 .Select(y => new UserPlansOutput
                 {
                     Id = y.Id,
                     AgreementId = y.AgreementId,
                     Tran_ref = y.Tran_ref,
                     PlanName = y.PlanVersions.Plan.PlanDetails.FirstOrDefault().Name,
                     PlanVersions = new PlansVersionsOutput
                     {
                         Plan = new PlanOutput
                         {
                             Name = y.PlanVersions.Plan.PlanDetails.FirstOrDefault().Name,
                             PlanType = y.PlanVersions.Plan.PlanType

                         },
                     },

                     UserId = y.UserId,
                     CurrentStatus = y.CurrentStatus,
                     NextStatus = y.NextStatus,
                     CurrentStatusDate = y.CurrentStatusDate,
                     NextStatusDate = y.NextStatusDate,
                     IsSubscribed = y.IsSubscribed

                 })
                 .ToListAsync();

            return Plan;
        }

        public async Task<UserPlansOutput> GetUserPlanByUserPlanId(long UserPlanId)
        {
            var cultureId = _workContext.GetCurrentCultureId();
            var Plan = await _userplanRepository
                 .Query()
                 .Where(x => x.Id == UserPlanId)
                 .Include(x => x.PlanVersions)
                 .ThenInclude(x => x.Plan)
                 .ThenInclude(x=>x.PlanDetails)
                 .Include(x => x.Order)
                 .Include(x => x.Parent)
                 .ThenInclude(x => x.Order)
                 .ThenInclude(x => x.OrderItems)
                 .ThenInclude(x => x.Product)
                 .ThenInclude(x=>x.ProductDetails)
                 .Select(y => new UserPlansOutput
                 {
                     Id = y.Id,
                     AgreementId = y.AgreementId,
                     PlanName = y.PlanVersions.Plan.PlanDetails.Where(x => x.CultureId == cultureId).FirstOrDefault().Name,
                     PlanVersions = new PlansVersionsOutput
                     {
                         Plan = new PlanOutput
                         {
                             Name = y.PlanVersions.Plan.PlanDetails.Where(x => x.CultureId == cultureId).FirstOrDefault().Name,
                             PlanType = y.PlanVersions.Plan.PlanType,
                         },
                         RefundPeriod = y.PlanVersions.RefundPeriod
                     },
                     Order = y.Order,
                     UserId = y.UserId,
                     CurrentStatus = y.CurrentStatus,
                     NextStatus = y.NextStatus,
                     CurrentStatusDate = y.CurrentStatusDate,
                     ActivationDate = y.ActivationDate,
                     NextStatusDate = y.NextStatusDate,
                     IsSubscribed = y.IsSubscribed,
                     ParentId = y.ParentId,
                     ParentAmount = y.ParentId.HasValue ? "1" : null
                 })
                 .FirstOrDefaultAsync();

            return Plan;
        }


        private async Task<PlansVersionsOutput> GetPlanLatestVersionId(long planId)
        {
            IList<PlansVersions> plansVersions = await _plansVersionsRepository
                .Query().AsNoTracking().Include(x => x.Plan)
                .Where(x => x.PlanId == planId)
                .ToArrayAsync();
            PlansVersionsOutput planVersion = _mapper.Map<PlansVersionsOutput>(plansVersions.Last());
            return planVersion;
        }
        public async Task<PlansVersionsOutput> GetLatestVersion(long PlanVersionId)
        {
            PlansVersions plansVersions = await _plansVersionsRepository
                .Query().Include(x => x.Plan)
                .Where(x => x.Id == PlanVersionId)
                .FirstOrDefaultAsync();
            PlansVersionsOutput planVersion = _mapper.Map<PlansVersionsOutput>(plansVersions);
            return planVersion;
        }
        private async Task<IActionResult> CreatePlanHistory(long UserplanId, EUserPlansStatus? oldStatus, EUserPlansStatus newStatus, string AdditionInfo)
        {
            UserPlansHistory userplanHis = new UserPlansHistory
            {
                TransDate = DateTimeOffset.Now,
                AdditionalInfo = AdditionInfo,
                OldStatus = oldStatus,
                CurrentStatus = newStatus,
                UserPlanId = UserplanId
            };
            _userplanHistoryRepository.Add(userplanHis);
            await _userplanHistoryRepository.SaveChangesAsync();
            return new OkObjectResult("Success");
        }


        public async Task<List<UserPlansHistoryVM>> GetUserPlanHistoryByUserPlanId(long UserPlanId)
        {
            var Plan = await _userplanHistoryRepository
                 .Query()
                 .Where(x => x.UserPlanId == UserPlanId).Include(P => P.UserPlan).ThenInclude(PV => PV.PlanVersions).ThenInclude(P => P.Plan).Select(y => new UserPlansHistoryVM
                 {

                     UserPlan = y.UserPlan,
                     AdditionalInfo = y.AdditionalInfo,

                     OldStatus = y.OldStatus,
                     CurrentStatus = y.CurrentStatus,
                     TransDate = y.TransDate,
                     UserPlanId = y.UserPlanId

                 })
                 .ToListAsync();

            return Plan;
        }


        private async Task<bool> IsUserSubscribedVersion(long versionId, long userId)
        {
            UserPlans userPlan = await _userplanRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.PlanVersionsId == versionId && x.UserId == userId && x.IsSubscribed == true);
            return userPlan != null;
        }

     
    }

}

