using EskaCMS.Infrastructure;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels;
using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Pricing.Services;
using Microsoft.AspNetCore.Http;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.ShoppingCart;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Enum;
using EskaCMS.Entities.Entity.Orders.Models;

namespace EskaCommerce.Orders.Services
{
    public interface IOrderService
    {

        Task<Result<Order>> CreateOrder(long cartId, string paymentMethod, decimal paymentFeeAmount, long SiteId, OrderStatus orderStatus = OrderStatus.New);

        Task<Result<Order>> CreateOrder(long cartId, string paymentMethod, decimal paymentFeeAmount, DeliveryInformationVm AddressInfo, Address billingAddress, Address shippingAddress, OrderStatus orderStatus = OrderStatus.New, long SiteId = 0);

        Task<Result<Order>> CreateOrderPlan(long cartId, string paymentMethod, decimal paymentFeeAmount, string shippingMethod, Address billingAddress, Address shippingAddress, OrderStatus orderStatus = OrderStatus.New, long SiteId = 0);

        Task<List<long>> CancelOrder(Order order);

        Task<decimal> GetTax(long cartId, string countryId, long? stateOrProvinceId, string zipCode);
        Task<OrderTaxAndShippingPriceVm> UpdateTaxAndShippingPrices(long cartId, TaxAndShippingPriceRequestVm model, long SiteId);
        Task<Result<Cart>> ReActiveCart(long cartId);
        Task<Result<Cart>> DeActiveCart(long cartId);
        Task<Order> GetOrderById(long id);
        Task<OrderDetailVm> GetOrderDetailsById(long id);
        Task OrderChangeStatus(long id, OrderStatus StatusId, string Note, long SiteId,bool? FromApi=false);
        Task<CartVm> GetOrderSummery(long UserId, CartTypes cart);
        Task<bool> UpdateSerialProduct(List<long> list, long orderId);
        Task<bool> UpdateOrderCustomer(long CustomerId);
        Task<CouponValidationResult> ApplyCouponForOrder(ApplyCouponForOrderVM Model,bool FromApi);
        Task<PayCashResult> PayCash(OrderViewModel Model);
        Task AddOrdersForBulkUsers(IFormFile UsersSheet,int Quantity, OrderStatus orderStatus,long UserGroupId);

        Task<bool> ReserveSerials(long orderId);

        Task<CouponValidationResult> ApplyCouponPlanForOrder(ApplyCouponForOrderVM model);
        Task SendReceipt(long orderId, bool sendInBackground = false);
        Task<byte[]> CreateTemplate(long productId,long CultureId);
        Task SendOrderStatus(long orderId, OrderStatus oldStatus, OrderStatus newStatus);
        Task<bool> UpdateOrderReferanceId(long orderId, string orderReferanceId);
        Task<Order> GetOrderByReferanceId(string id);
        int? GetEstimatedDeliveryDate(Warehouse warehouse, OrderAddress address,DeliveryOptionEnum deliveryOption);
        Task UpdateCustomerCurrency(decimal orderTotal, string CurrencyCode,Order order);

        Task<EskaCMS.Infrastructure.Helpers.DinToPdf.PdfFile> PrintReceipt(long orderId, long? cultureId);

        Task<List<CartOrderChart>> GetCartOrderCharts(TimeIntervals time);
        Task<CartVm> ReOrder(long orderId, long siteId, long userId);
       // Task<bool> UpdateOrderStatus(long OrderId, OrderStatus status);

    }
}
