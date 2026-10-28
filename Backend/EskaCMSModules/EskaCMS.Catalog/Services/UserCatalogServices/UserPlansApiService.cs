using AutoMapper;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Helpers;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.UserCatalog.Services
{

    public class UserPlansApiService : IUserPlansApiService
    {
        private readonly DateTimeOffset TimeNow = DateTimeOffset.Now;

        private readonly IRepository<UserPlans> _userplanRepository;
        private readonly IRepository<UserPlansHistory> _userplanHistoryRepository;
        private readonly IRepository<PlansVersions> _plansVersionsRepository;
        private readonly IRepository<UserBooking> _userBookingRepository;
        private readonly IRepository<Order> _orderRepository; // Firas to be removed from here and use the service instead
        private readonly IRepository<Plan> _planRepository;
        private readonly IRepository<UserPlanProducts> _userplanProductsRepository;
        private readonly IMapper _mapper;
        private readonly IWorkContext _workContext;
        public UserPlansApiService(
            IRepository<UserPlans> userplanRepository,
            IWorkContext workContext,
            IRepository<PlansVersions> plansVersionsRepository,
            IRepository<UserBooking> userBookingRepository,
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
            _userBookingRepository = userBookingRepository;
            _mapper = mapper;
            _userplanHistoryRepository = userplanHistoryRepository;
            _planRepository = planRepository;
            _orderRepository = orderRepository;
            _userplanProductsRepository = userplanProductsRepository;
        }
        public GridOutputVM<SubscriberListVM> GetFilteredUserPlans(SearchTableParam<SubscribersListSearchVM> param)
        {
            SubscribersListSearchVM search = param.Search;
            var userPlanQuery = _userplanRepository.Query()
                                      .Where(userPlan =>
                                          (string.IsNullOrEmpty(search.Name) || (userPlan.User.FullName.ToUpper().Contains(search.Name.ToUpper())|| userPlan.PlanVersions.Plan.PlanDetails.Any(x=>x.NormalizedName.Contains(search.Name.ToUpper()))))
                                          && ((search.CurrentStatus == null || search.CurrentStatus == 0) || userPlan.CurrentStatus == search.CurrentStatus)
                                          && (search.NextStatusDateFrom == null || userPlan.NextStatusDate >= search.NextStatusDateFrom)
                                          && (search.NextStatusDateTo == null || userPlan.NextStatusDate <= search.NextStatusDateTo))
                                      .Include(x => x.PlanVersions)
                                      .ThenInclude(C => C.Plan)        
                                      .ThenInclude(x=>x.PlanDetails)
                                      .Include(x => x.User)
                                      .Include(x=>x.Order)
                                      .OrderByDescending(d => d.ActivationDate)
                                      .ToList();
            GridOutputVM<SubscriberListVM> plans = new GridOutputVM<SubscriberListVM>
            {
                Items = userPlanQuery.Select(x => new SubscriberListVM
                                            {
                                                Id = x.Id,
                                                PlanDetails = userPlanQuery.Where(pl => pl.UserId == x.UserId).Select(d => new SubscriberPlanDetailVM
                                                {
                                                    CurrentStatus = d.CurrentStatus,
                                                    PlanCultureDetails = d.PlanVersions.Plan.PlanDetails
                                                                                            .Select(x=>new PlanCultureDetailsVm {
                                                                                                Name = x.Name ,
                                                                                                CultureId=x.CultureId,
                                                                                                Description = x.Description ,
                                                                                                ShortDescription = x.ShortDescription})
                                                                                            .ToList(),
                                                    NextStatusDate = d.NextStatusDate,
                                                    NextDuePayment = d.Order.OrderTotal,
                                                }).FirstOrDefault(),
                                                CustomerName = x.User.FullName,
                                                CustomerImage = x.User.Image,
                                                PlansCount = userPlanQuery.Where(p => p.UserId == x.UserId).Count(),
                                                UserId = x.UserId,
                                                CurrencyCode=x.Order.CurrencyCode,
                                                OrderTotal=x.Order.OrderTotal
                                            }).Distinct(new UserPlanComparer()).Skip(param.Pagination.Number * param.Pagination.Start).Take(param.Pagination.Number).ToList(),
                TotalRecord = userPlanQuery.Count()
            };
            
            return plans;

        }

        public GridOutputVM<UserPlansGridOutput> GetUserPlansById(long userId, SearchTableParam<UserPlansListSearchVM> param)
        {
            UserPlansListSearchVM search = param.Search;
            var userPlanQuery = _userplanRepository.Query().AsNoTracking()
                                      .Include(x => x.PlanVersions)
                                      .ThenInclude(C => C.Plan)
                                      .ThenInclude(x=>x.PlanDetails)
                                      .Include(x => x.User)
                                      .Include(x => x.Order)
                                      .Include(x=>x.userPlansHistories)
                                      .Where(userPlan =>
                                          (userPlan.UserId == userId)
                                          && (string.IsNullOrEmpty(search.PlanName) || userPlan.PlanVersions.Plan.PlanDetails.Any(x=>x.NormalizedName.Contains(search.PlanName.ToUpper())))
                                          && ((search.CurrentStatus == null || search.CurrentStatus == 0) || userPlan.CurrentStatus == search.CurrentStatus)
                                          && (search.LastPaymentDateFrom == null || userPlan.CurrentStatusDate >= search.LastPaymentDateFrom)
                                          && (search.LastPaymentDateTo == null || userPlan.CurrentStatusDate <= search.LastPaymentDateTo)
                                          && (search.NextPaymentDateFrom == null || userPlan.NextStatusDate >= search.NextPaymentDateFrom)
                                          && (search.NextPaymentDateTo == null || userPlan.NextStatusDate <= search.NextPaymentDateTo)
                                          && (search.StartDateFrom == null || userPlan.ActivationDate >= search.StartDateFrom)
                                          && (search.StartDateTo == null || userPlan.ActivationDate <= search.StartDateTo)
                                          && (search.PlanType == null || userPlan.PlanVersions.Plan.PlanType == search.PlanType))
                                      .ToList();
                                     var userPlans=userPlanQuery
                                      .Select(x => new UserPlansGridOutput
                                      {
                                          Id = x.Id,
                                          CurrentStatus = x.CurrentStatus,
                                          PlanCultureDetails = x.PlanVersions.Plan.PlanDetails.Select(x => new PlanCultureDetailsVm
                                          {
                                              Name = x.Name,
                                              CultureId = x.CultureId,
                                              Description = x.Description,
                                              ShortDescription = x.ShortDescription
                                          })
                                            .ToList(),
                                          UserPlanHistories=x.userPlansHistories!=null? x.userPlansHistories.Select(x=>new userplanhistoryDto { 
                                          
                                                  TransDate = x.TransDate,
                                                  AdditionalInfo = x.AdditionalInfo,
                                                  CurrentStatus=x.CurrentStatus,
                                                  OldStatus = x.OldStatus
                                          
                                          }).ToList():new List<userplanhistoryDto>(),
                                          Bookings=_userBookingRepository.Query().Where(b=>b.UserPlanId==x.Id).Count().ToString() +" of " + x.Order.OrderItems.Count.ToString(),
                                          OrderId=x.OrderId,
                                          LastPaymentDate=x.CurrentStatusDate,
                                          NextPaymentDate=x.NextStatusDate,
                                          PlanVersionsId=x.PlanVersionsId,
                                          PricePerMonth=x.Order.OrderTotal,
                                          StartDate=x.ActivationDate,
                                          PlanType=x.PlanVersions.Plan.PlanType,
                                          UserId=x.UserId,
                                          CurrencyCode = x.Order.CurrencyCodeCustomer                                      }).Skip(param.Pagination.Number * param.Pagination.Start).Take(param.Pagination.Number).ToList();
            GridOutputVM<UserPlansGridOutput> plans = new GridOutputVM<UserPlansGridOutput>
            {
                Items = userPlans,                                            
                TotalRecord = userPlanQuery.Count()
            };

            return plans;
        }


    }

}
