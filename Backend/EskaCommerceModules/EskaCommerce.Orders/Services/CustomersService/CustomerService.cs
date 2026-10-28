using EskaCMS.Core.Areas.Core.Interfaces;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Account;
using EskaCMS.Core.Extensions;
using EskaCMS.EmailSender.SMTP.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.ModelVM;
using EskaCommerce.Orders.ModelVM.Request;
using EskaCommerce.Orders.Services.CustomersService.Interface;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EskaCMS.Infrastructure.Helpers;

using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using Hangfire.MemoryStorage.Database;
using EskaCommerce.Pricing.Services;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Models;
using EskaCommerce.Notifications.Notifiers;
using Elastic.Transport;

namespace EskaCommerce.Orders.Services.CustomersService
{
    public class CustomerService : ICustomerService
    {
        private readonly IRepository<User> _userrepository;
        private readonly IRepository<Order> _orderrepository;
        private readonly IRepository<EmailsSettings> _emailSettings;
        private readonly IEmailSender _emailSenderService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ICustomerGroup _customergroup;
        private readonly ICouponService _couponService;
        private readonly ITestNotifier _testNotifier;

        private readonly IWorkContext _IWorkContext;
        public CustomerService(
            IRepository<User> userrepository,
            IRepository<Order> orderrepository,
            IRepository<EmailsSettings> emailSettings,
            IEmailSender emailSenderService,
           IEmailTemplateService emailTemplateService,
           ICustomerGroup customergroup,
           ICouponService couponService,
           ITestNotifier testNotifier, 
        IWorkContext IWorkContext) 
        {
            _userrepository = userrepository;
            _orderrepository = orderrepository;
            _emailSettings = emailSettings;
            _emailSenderService = emailSenderService;
            _IWorkContext= IWorkContext;
            _emailTemplateService=emailTemplateService;
            _customergroup= customergroup;
            _couponService=couponService;
            _testNotifier= testNotifier;
        }

        public async Task<(List<CustomerDto> Data, int TotalCount)> GetCustomerList(string searchTerm, DateTime? dateFrom, DateTime? dateTo, EStatus? status, int page, int limit, string sortBy, ESortType sortOrder)
        {
            try
            {
                var query = _userrepository.Query()
                    .Include(x => x.UserRoles)
                    .AsNoTracking()
                    .Where(x => x.Status != EStatus.Deleted && x.Type != EUsersTypes.GuestClient);

                // Apply filters conditionally
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var lowerSearch = searchTerm.ToLower();
                    query = query.Where(x =>
                        x.Email.ToLower() == lowerSearch ||
                        x.UserName.ToLower() == lowerSearch ||
                        x.PhoneNumber == searchTerm);
                }

                if (dateFrom.HasValue)
                {
                    query = query.Where(x => x.CreationDate >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(x => x.CreationDate <= dateTo.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(x => x.Status == status);
                }

                // Count before pagination
                var totalCount = query.Count();

                // Sorting
                query = sortBy switch
                {
                    "Name" => sortOrder == ESortType.desc ? query.OrderByDescending(x => x.UserName) : query.OrderBy(x => x.UserName),
                    "CreationDate" => sortOrder == ESortType.desc ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate),
                    _ => query.OrderBy(x => x.Id) // default sort
                };
              
                    // Pagination
                    var users = await query
                        .Skip((page < 0 ?  1 : page - 1) * (limit < 0 ?1: limit))
                        .Take(limit < 0 ? 1 :limit)
                        .ToListAsync();
                 

                if (users.Any())
                {
                    var successfulOrders = await SuccessfullOrder();

                    var result = users.Select(user =>
                    {
                        var userOrders = successfulOrders.Where(o => o.CustomerId == user.Id).ToList();
                        return new CustomerDto
                        {
                            name = user.UserName,
                            email = user.Email,
                            mobile = user.PhoneNumber,
                            ordersCount = userOrders.Count,
                            totalAmount = userOrders.Sum(o => o.OrderTotal),
                            regitstrationDate = user.CreationDate,
                            status = user.Status
                        };
                    }).ToList();

                    return (result, totalCount);
                }

                return (new List<CustomerDto>(), 0);
            }
            catch (Exception)
            {
                throw;
            }
        }


            public async Task<bool> SendEmailToBulkUsers(CustomerActions customerAction)
        {
            try
            {
                var CurrentSiteId = _IWorkContext.GetCurrentSiteId();
                var payloadRequest = customerAction.Payload.ToString();
                
               var RequstDetails = JsonConvert.DeserializeObject<SendEmailRequest>(payloadRequest);
                var Emails = await _userrepository.Query().Where(x => customerAction.CustomerIds.Contains(x.Id)).Select(x => x.Email).ToListAsync();
                RequstDetails.data.SendValue = Emails;

                if (RequstDetails.EmailType == Enum.EmailType.Templete)
                {
                    var emailTemplateObj = await _emailTemplateService.GetEmailTemplateById(RequstDetails.EmailId);


                    if (emailTemplateObj != null)
                    {
                        RequstDetails.data.EmailTemplateId = emailTemplateObj.Id;

                    }
                }
                await _testNotifier.SendMessageAsync(RequstDetails.notificationType, RequstDetails.EmailSettings, RequstDetails.data, RequstDetails.siteId, RequstDetails.UserId);

                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<bool> CreateAndAssignGroup(CustomerActions customerAction)
        {
            try
            {
                var CurrentSiteId = _IWorkContext.GetCurrentSiteId();
                var userIds=customerAction.CustomerIds;
               
               var CustomerGroupForm = customerAction.Payload.ToString();
               var CustomerGroupFormDeserialized = JsonConvert.DeserializeObject<CustomerGroupForm>(CustomerGroupForm);

              var GroupId= await _customergroup.Post(CustomerGroupFormDeserialized);
                AddUserToGroupDto addUserToGroupDto = new AddUserToGroupDto
                {
                    CustomerGroupId = GroupId,
                    UserIds = userIds
                };
                var result=await _customergroup.AddBulkUserToGroup(addUserToGroupDto);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> addCouponToGroup(CustomerActions customerActions)
        {
            try
            {
                var couponRequst = customerActions.Payload.ToString();
                var couponRequstobj=JsonConvert.DeserializeObject<CouponRequest>(couponRequst);
                

               var result= await _couponService.addCouponToGroup(couponRequstobj.groupId, couponRequstobj.couponId);
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public async Task<List<User>> GetUsers(CustomerActions customerActions)

        {
            try
            {
                var usersIds = customerActions.CustomerIds;
                var users = await _userrepository.Query().Where(x => usersIds.Contains(x.Id)).ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private async Task<List<Order>> SuccessfullOrder()
        {
            try
            {
                var SuccessfullOrders = await _orderrepository.Query().Include(x=>x.Customer).Where(x =>
                   (x.OrderStatus == OrderStatus.CashOnDelivery
               || x.OrderStatus == OrderStatus.PaymentReceived
               || x.OrderStatus == OrderStatus.PaidByCoupon
               || x.OrderStatus == OrderStatus.Complete))
                  .ToListAsync();
                return SuccessfullOrders;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

       
    }
}
