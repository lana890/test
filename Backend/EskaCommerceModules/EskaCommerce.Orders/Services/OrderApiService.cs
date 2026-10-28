using EskaCMS.Addresses.Models;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.Events;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Orders;

namespace EskaCommerce.Orders.Services
{

    public class OrderApiService : IOrderApiService
    {

        private readonly IRepository<Order> _orderRepository;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly IRepository<UserAddress> _userAddressRepository;


        public OrderApiService(
            IRepository<Order> orderRepository,
            IWorkContext workContext,
            IMediator mediator, IRepository<UserAddress> userAddressRepository)
        {
            _orderRepository = orderRepository;

            _workContext = workContext;
            _mediator = mediator;
            _userAddressRepository = userAddressRepository;
        }
        public GridOutputVM<OrderListOutputVM> GetFilteredUserOrders(long? userId, SearchTableParam<OrderListSearchVM> param)
        {
            var cultureId=_workContext.GetCurrentCultureId();
            OrderListSearchVM search = param.Search;
            // Read-only query — no change tracking (lazy-loading proxies are NOT enabled
            // in this solution, so results are identical). Same filter + includes as before.
            var filtered = _orderRepository.Query()
                                      .AsNoTracking()
                                      .Where(order =>
                                          (
                                          string.IsNullOrEmpty(search.StringSearch) ||
                                          (
                                          order.Customer.FullName.ToUpper().Contains(search.StringSearch.ToUpper())
                                          || order.Customer.Email.ToUpper().Contains(search.StringSearch.ToUpper())
                                          || order.Customer.PhoneNumber.ToUpper().Contains(search.StringSearch.ToUpper())
                                          || order.BillingAddress.Country.CountryDetails.Any(p=>p.Name.ToUpper().Contains(search.StringSearch.ToUpper()))
                                          || order.BillingAddress.City.ToUpper().Contains(search.StringSearch.ToUpper())
                                          || order.BillingAddress.AddressLine1.ToUpper().Contains(search.StringSearch.ToUpper())
                                          )
                                          )
                                          && ((search.OrderStatus == null || search.OrderStatus == 0) || order.OrderStatus == search.OrderStatus)
                                          && (search.OrderDateFrom == null || order.CreatedOn >= search.OrderDateFrom)
                                          && (search.OrderDateTo == null || order.CreatedOn <= search.OrderDateTo)
                                          && (userId == null || order.CustomerId == userId)
                                          )
                                      .Include(x => x.Customer)
                                      .Include(x => x.BillingAddress)
                                      .ThenInclude(x => x.Country)
                                      .OrderByDescending(d => d.CreatedOn);

            // Count + page in SQL (was: materialize ALL matching orders, then page in memory).
            var totalRecord = filtered.Count();

            var pagedOrders = filtered
                                      .Skip(param.Pagination.Number * param.Pagination.Start)
                                      .Take(param.Pagination.Number)
                                      .ToList();

            GridOutputVM<OrderListOutputVM> orders = new GridOutputVM<OrderListOutputVM>
            {
                // Projection kept in memory and unchanged, so each row maps exactly as before.
                Items = pagedOrders.Select(o => new OrderListOutputVM
                {
                    OrderId = o.Id,
                    Amount = o.OrderTotal,
                    CustomerAddress = o.BillingAddress.AddressLine1,
                    CustomerCity = o.BillingAddress.City,
                    CustomerCountry = o.BillingAddress.Country.CountryDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                    CustomerEmail = o.Customer.Email,
                    CustomerName = o.Customer.FullName,
                    CustomerPhoneNumber = o.Customer.PhoneNumber,
                    OrderDate = o.CreatedOn,
                    OrderStatus = o.OrderStatus,
                    CurrencyCode = o.CurrencyCode
                }).ToList(),
                TotalRecord = totalRecord
            };

            return orders;

        }
        public async Task updateAddresses(long id, UpdateAddressVM addressesVM, long CurrentUserId)
        {
            try
            {
                var order = _orderRepository.Query().Include(x => x.BillingAddress)
                                .Include(x => x.ShippingAddress).FirstOrDefault(x => x.Id == id);
                if (order == null)
                {
                    throw new Exception("Order Not found");
                }

                order.ShippingAddress.AddressLine1 = addressesVM.ShippingAddress.AddressLine1;
                order.ShippingAddress.AddressLine2 = addressesVM.ShippingAddress.AddressLine2;
                order.ShippingAddress.ZipCode = addressesVM.ShippingAddress.ZipCode;
                order.ShippingAddress.City = addressesVM.ShippingAddress.City;
                order.ShippingAddress.CountryId = addressesVM.ShippingAddress.CountryId;
                if (addressesVM.ShippingAddress.StateOrProvinceId > 0)
                    order.ShippingAddress.StateOrProvinceId = addressesVM.ShippingAddress.StateOrProvinceId;
                else
                    order.ShippingAddress.StateOrProvinceId = null;

                if (!addressesVM.UseShippingAsBillingAddress)
                {
                    if (order.BillingAddress != null)
                    {
                        order.BillingAddress.AddressLine1 = addressesVM.BillingAddress.AddressLine1;
                        order.BillingAddress.AddressLine2 = addressesVM.BillingAddress.AddressLine2;
                        order.BillingAddress.ZipCode = addressesVM.BillingAddress.ZipCode;
                        order.BillingAddress.City = addressesVM.BillingAddress.City;
                        order.BillingAddress.CountryId = addressesVM.BillingAddress.CountryId;
                        if (addressesVM.BillingAddress.StateOrProvinceId > 0)
                            order.BillingAddress.StateOrProvinceId = addressesVM.BillingAddress.StateOrProvinceId;
                        else
                            order.BillingAddress.StateOrProvinceId = null;
                    }
                    else
                    {
                        var address = new Address
                        {
                            AddressLine1 = addressesVM.BillingAddress.AddressLine1,
                            AddressLine2 = addressesVM.BillingAddress.AddressLine2,
                            ContactName = addressesVM.BillingAddress.ContactName,
                            CountryId = addressesVM.BillingAddress.CountryId,
                            StateOrProvinceId = addressesVM.BillingAddress.StateOrProvinceId,
                            DistrictId = addressesVM.BillingAddress.DistrictId,
                            City = addressesVM.BillingAddress.City,
                            ZipCode = addressesVM.BillingAddress.ZipCode,
                            Phone = addressesVM.BillingAddress.Phone,
                            Email = addressesVM.BillingAddress.Email
                        };

                        var userAddress = new UserAddress
                        {
                            Address = address,
                            AddressType = EskaCMS.Entities.Enum.AddressType.Billing,
                            UserId = CurrentUserId
                        };

                        _userAddressRepository.Add(userAddress);

                        var billingAddress = address;
                        var orderAddress = new OrderAddress();

                        orderAddress.AddressLine1 = billingAddress.AddressLine1;
                        orderAddress.AddressLine2 = billingAddress.AddressLine2;
                        orderAddress.ContactName = billingAddress.ContactName;
                        orderAddress.CountryId = billingAddress.CountryId;
                        orderAddress.StateOrProvinceId = billingAddress.StateOrProvinceId;
                        orderAddress.DistrictId = billingAddress.DistrictId;
                        orderAddress.City = billingAddress.City;
                        orderAddress.ZipCode = billingAddress.ZipCode;
                        orderAddress.Phone = billingAddress.Phone;
                        orderAddress.Email = billingAddress.Email;

                        order.BillingAddress = orderAddress;
                    }
                }
                else
                {
                    order.BillingAddress.AddressLine1 = addressesVM.ShippingAddress.AddressLine1;
                    order.BillingAddress.AddressLine2 = addressesVM.ShippingAddress.AddressLine2;
                    order.BillingAddress.ZipCode = addressesVM.ShippingAddress.ZipCode;
                    order.BillingAddress.City = addressesVM.ShippingAddress.City;
                    order.BillingAddress.CountryId = addressesVM.ShippingAddress.CountryId;
                    order.BillingAddress.StateOrProvinceId = addressesVM.ShippingAddress.StateOrProvinceId;
                }

                order.LatestUpdatedById = CurrentUserId;
                order.LatestUpdatedOn = DateTimeOffset.Now;

                await _orderRepository.SaveChangesAsync();

                var orderStatusChanged = new OrderChanged
                {
                    OrderId = order.Id,
                    Order = order,
                    UserId = CurrentUserId,
                    NewStatus = order.OrderStatus,
                    Note = "Address Information has been Modified"
                };

                await _mediator.Publish(orderStatusChanged);

            }
            catch (Exception exc)
            {

                throw exc;
            }

        }

        public async Task updateAdditionalAddresses(long id, UpdateAddressVM addressesVM, long CurrentUserId)
        {
            try
            {
                var order = _orderRepository.Query().Include(x => x.OrderItems).FirstOrDefault(x => x.Id == id);
                if (order == null)
                {
                    throw new Exception("Order Not found");
                }

                var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == addressesVM.ItemId);
                if (orderItem == null)
                {
                    throw new Exception("Order Item Not found");
                }

                orderItem.AdditionalDetails = addressesVM.AdditionalDetails;
                order.LatestUpdatedById = CurrentUserId;
                order.LatestUpdatedOn = DateTimeOffset.Now;

                await _orderRepository.SaveChangesAsync();

                var orderStatusChanged = new OrderChanged
                {
                    OrderId = order.Id,
                    Order = order,
                    UserId = CurrentUserId,
                    NewStatus = order.OrderStatus,
                    Note = "Additional Addresses have been Modified"
                };

                await _mediator.Publish(orderStatusChanged);

            }
            catch (Exception exc)
            {

                throw exc;
            }

        }


    }

}
