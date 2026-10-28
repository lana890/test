using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Enum;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.Areas.Orders.ViewModels
{
    public class OrderDetailVm
    {
        private readonly ICurrencyService _currencyService;

        public OrderDetailVm(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public long Id { get; set; }

        public string OrderCurrencyCode { get; set; }
     
        public long CustomerId { get; set; }
        public long SiteId { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        public string OldPriceWithTax { get; set; }
        public string PriceWithTax { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }

        public string CustomerEmail { get; set; }
		public string UserName { get; set; }
		public string OrderReferanceId { get; set; }
        public DateTimeOffset CreatedOn { get; set; }

        public string OrderStatusString { get; set; }
        public string EncryptedOrderId { get; set; }

        public EskaCMS.Entities.Entity.OrderStatus OrderStatus { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TotalFees { get; set; }

        public decimal DiscountAmount { get; set; }
        public ProductSerialViewModel ProductSerial { get; set; }

        public decimal SubTotalWithDiscount { get; set; }

        public decimal TaxAmount { get; set; }
        public decimal RedeemAmount { get; set; }

        public decimal ShippingAmount { get; set; }
        public decimal ShippingDiscountAmount {  get; set; }
        public decimal shippingAmountBeforeDiscount => ShippingAmount + ShippingDiscountAmount;

        public decimal OrderTotal { get; set; }

        public string ShippingMethod { get; set; }

        public string PaymentMethod { get; set; }
        public string AdditionalDetails { get; set; }

        public decimal PaymentFeeAmount { get; set; }

        public string SubtotalString { get { return _currencyService.FormatByUserCurrency(Subtotal); } }

        public string DiscountAmountString { get { return _currencyService.FormatByUserCurrency(DiscountAmount); } }

        public string SubtotalWithDiscountString { get { return _currencyService.FormatByUserCurrency(SubTotalWithDiscount); } }

        public string TaxAmountString { get { return _currencyService.FormatByUserCurrency(TaxAmount); } }

        public string ShippingAmountString { get { return _currencyService.FormatByUserCurrency(ShippingAmount); } }

        public string PaymentFeeAmountString { get { return _currencyService.FormatByUserCurrency(PaymentFeeAmount); } }

        public string OrderTotalString { get { return _currencyService.FormatByUserCurrency(OrderTotal); } }
        public string RedeemAmountString { get { return _currencyService.FormatByUserCurrency(RedeemAmount); } }
        public string TotalFeesٍString { get { return _currencyService.FormatByUserCurrency(RedeemAmount); } }

        public ShippingAddressVm ShippingAddress { get; set; }
        public ShippingAddressVm BillingAddress { get; set; }
        public IList<OrderAdditionalFeesVM> OrderAdditionalFees { get; set; } = new List<OrderAdditionalFeesVM>();

		public long? CartId { get; set; }

		public IList<OrderItemVm> OrderItems { get; set; } = new List<OrderItemVm>();

        public IList<long> SubOrderIds { get; set; }

        public bool IsMasterOrder { get; set; }

        public string OrderNote { get; set; }
        public string Currency { get { return _currencyService.GetUserCurrencyCodeForAdmin(); } }


        public IList<SelectListItem> ShipableContries { get; set; }

        public IList<SelectListItem> StateOrProvinces { get; set; }
        public string Tax { get; internal set; }
        public EPlatformSource Channel { get; internal set; }
        public string CouponCode { get;  set; }
        public int? EstimatedDeliveryDate { get; set; }
        public string WareHouseName { get; set; }
        public long ShipmentId { get; set; }
        public string TrackingNumber { get; internal set; }
        public DeliveryOptionEnum ShippingDelivery { get;  set; }
        public long? PickupId { get; internal set; }
        public string ShippingDeliveryString { get; set; }

    }
    public class OrderDto
    {
        internal List<string> serials;
        public long? UserId { get; set; }
        public long Id { get; set; }
        public string CustomerName { get; set; }
        public string OrderTotal { get; set; }
        public string OrderStatus { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public string Phone { get; set; }
        public EPlatformSource Channel { get; set; }
        public string Gender { get; set; }
        public string CategoryName { get; set; }
        public string Email { get; set; }
        public string WarehouseName { get; set; }
        public string WareHouseName { get;  set; }
        public string OrderTotalString { get;  set; }
        public OrderStatus OrderStatusId { get; set; }
        public string CategoryNames { get;  set; }
        public string ShipmentType { get;  set; }
    }

    //public class OrderDetailVm
    //{
    //    private readonly ICurrencyService _currencyService;

    //    public OrderDetailVm(ICurrencyService currencyService)
    //    {
    //        _currencyService = currencyService;
    //    }

    //    public long Id { get; set; }

    //    public string OrderCurrencyCode { get; set; }

    //    public long CustomerId { get; set; }
    //    public long SiteId { get; set; }

    //    public string OldPriceWithTax { get; set; }
    //    public string PriceWithTax { get; set; }
    //    public string CustomerName { get; set; }

    //    public string CustomerEmail { get; set; }

    //    public string OrderReferanceId { get; set; }
    //    public DateTimeOffset CreatedOn { get; set; }

    //    public string OrderStatusString { get; set; }

    //    public EskaCMS.Entities.Entity.OrderStatus OrderStatus { get; set; }

    //    public decimal Subtotal { get; set; }

    //    public decimal DiscountAmount { get; set; }
    //    public ProductSerialViewModel ProductSerial { get; set; }

    //    public decimal SubTotalWithDiscount { get; set; }

    //    public decimal TaxAmount { get; set; }

    //    public decimal ShippingAmount { get; set; }

    //    public decimal OrderTotal { get; set; }

    //    public string ShippingMethod { get; set; }

    //    public string PaymentMethod { get; set; }

    //    public decimal PaymentFeeAmount { get; set; }

    //    public string SubtotalString { get { return _currencyService.FormatCurrency(Subtotal, OrderCurrencyCode); } }

    //    public string DiscountAmountString { get { return _currencyService.FormatCurrency(DiscountAmount, OrderCurrencyCode); } }

    //    public string SubtotalWithDiscountString { get { return _currencyService.FormatCurrency(SubTotalWithDiscount, OrderCurrencyCode); } }

    //    public string TaxAmountString { get { return _currencyService.FormatCurrency(TaxAmount, OrderCurrencyCode); } }

    //    public string ShippingAmountString { get { return _currencyService.FormatCurrency(ShippingAmount, OrderCurrencyCode); } }

    //    public string PaymentFeeAmountString { get { return _currencyService.FormatCurrency(PaymentFeeAmount, OrderCurrencyCode); } }

    //    public string OrderTotalString { get { return _currencyService.FormatCurrency(OrderTotal, OrderCurrencyCode); } }

    //    public ShippingAddressVm ShippingAddress { get; set; }
    //    public ShippingAddressVm BillingAddress { get; set; }


    //    public IList<OrderItemVm> OrderItems { get; set; } = new List<OrderItemVm>();

    //    public IList<long> SubOrderIds { get; set; }

    //    public bool IsMasterOrder { get; set; }

    //    public string OrderNote { get; set; }
    //    public string Currency { get { return _currencyService.GetUserCurrencyCodeForAdmin(); } }

    //    public decimal TotalFees { get; set; }

    //    public IList<SelectListItem> ShipableContries { get; set; }

    //    public IList<SelectListItem> StateOrProvinces { get; set; }
    //    public string Tax { get; internal set; }
    //    public EPlatformSource Channel { get; internal set; }
    //    public string CouponCode { get; set; }
    //}

}
