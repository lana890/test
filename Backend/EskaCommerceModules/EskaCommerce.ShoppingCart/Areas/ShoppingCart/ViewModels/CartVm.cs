using EskaCMS.Core.Areas.Core.ViewModels;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Services;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Metadata.Ecma335;

namespace EskaCommerce.ShoppingCart.Areas.ShoppingCart.ViewModels
{
    public class CartVm
    {
        private readonly ICurrencyService _currencyService;

        public CartVm(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public long Id { get; set; }
        public bool LockedOnCheckout { get; set; }
        public string ReferenceId {  get; set; }
        public string JoClaimId {  get; set; }
        public string CouponCode { get; set; }

        public decimal SubTotal { get; set; }

        public string SubTotalString { get { return _currencyService.FormatByUserCurrency(SubTotal); } }

        public decimal CouponDiscount { get; set; }
        public string CouponDiscountString { get { return _currencyService.FormatByUserCurrency(CouponDiscount); } }

        public decimal RedeemDiscount { get; set; }
        public string RedeemDiscountString { get { return _currencyService.FormatByUserCurrency(RedeemDiscount); } }

        public decimal Discount { get; set; }

        public string DiscountString { get { return _currencyService.FormatByUserCurrency(Discount); } }

        public string CouponValidationErrorMessage { get; set; }

        public bool IsProductPriceIncludeTax { get; set; }
        public string ShippingData { get; set; }

        public DeliveryInformationVM.ShippingAddressVm? ShippingAdress { get; set; }
        public decimal? TaxAmount { get; set; }
        public DeliveryOption DeliveryOption { get; set; }
        public decimal? TaxCalculations
        {
            get
            {

                return TaxAmount.HasValue ? _currencyService.DcimalRound(TaxAmount.Value * (SubTotal + TotalFees - Discount - CouponDiscount - RedeemDiscount)) : 0;
            }
        }
        public string OrderNote { get; set; }

        public decimal TotalFees { get; set; }

        public string TaxAmountString
        {
            get
            {

                return TaxAmount.HasValue ? _currencyService.FormatByUserCurrency(TaxAmount.Value) : "-";
            }
        }

        public decimal? ShippingAmount { get; set; }
        public decimal? ShippingDiscountAmount { get; set; }

        public decimal ShippingAfterDiscount =>
            Math.Max((ShippingAmount ?? 0) - (ShippingDiscountAmount ?? 0), 0);

        public string ShippingDiscountAmountString =>
            ShippingDiscountAmount.HasValue && ShippingDiscountAmount.Value > 0
                ? _currencyService.FormatByUserCurrency(ShippingDiscountAmount.Value)
                : string.Empty;

        public string ShippingAfterDiscountString =>
            _currencyService.FormatByUserCurrency(ShippingAfterDiscount);

        public string ShippingAmountString
        {
            get { return ShippingAmount.HasValue ? _currencyService.FormatByUserCurrency(ShippingAmount.Value) : "-"; }
        }

        public decimal SubTotalWithDiscount
        {
            get
            {

                return _currencyService.DcimalRound((SubTotal + TotalFees - Discount - CouponDiscount));
            }
        }

        public string SubTotalWithDiscountString
        {
            get
            {

                return _currencyService.FormatByUserCurrency(SubTotalWithDiscount);
            }
        }

        public decimal SubTotalWithDiscountWithoutTax
        {
            get
            {
                if (IsProductPriceIncludeTax)
                {
                    return _currencyService.DcimalRound(SubTotalWithDiscount - TaxAmount ?? 0);
                }

                return _currencyService.DcimalRound(SubTotalWithDiscount);
            }
        }

        public IList<ProductFeesVM> ProductFees { get; set; }


        public decimal OrderTotal
        {
            get
            {
                if (IsProductPriceIncludeTax)
                {
                    return _currencyService.DcimalRound(SubTotal + ShippingAfterDiscount - Discount - CouponDiscount - RedeemDiscount);
                }

                var result = _currencyService.DcimalRound(SubTotal + TotalFees + (TaxAmount ?? 0) + ShippingAfterDiscount- Discount - CouponDiscount - RedeemDiscount);

                return result < 0 ? 0 : result;
            }
        }
        public string OrderTotalString { get { return _currencyService.FormatByUserCurrency(OrderTotal); } }
        public long SiteId { get; set; }
        public IList<CartItemVm> Items { get; set; } = new List<CartItemVm>();
        public CustomerLoyalityObjectVm Loyality { get; set; } = new CustomerLoyalityObjectVm();
        public bool IsValid
        {
            get
            {
                foreach (var item in Items)
                {
                    if (!item.IsProductAvailabeToOrder)
                    {
                        return false;
                    }

                    if (item.ProductStockTrackingIsEnabled && item.ProductStockQuantity < item.Quantity)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        public string TotalFeesString { get { return _currencyService.FormatByUserCurrency(TotalFees); } }

        public string TaxPercentage { get; set; }
        public string ReOrderWarning {  get; set; }
        public PlansVersionOutput? Plan { get; set; }

    }


    public class RemoveCartItemVM
    {
        public long itemId { get; set; }
        public CartTypes CartType { get; set; }
    }
    //public class CartVm
    //{
    //    private readonly ICurrencyService _currencyService;

    //    public CartVm(ICurrencyService currencyService)
    //    {
    //        _currencyService = currencyService;
    //    }

    //    public long Id { get; set; }
    //    public bool LockedOnCheckout { get; set; }

    //    public string CouponCode { get; set; }

    //    public decimal SubTotal { get; set; }

    //    public string SubTotalString { get { return _currencyService.FormatCurrency(SubTotal); } }

    //    public decimal CouponDiscount { get; set; }
    //    public string CouponDiscountString { get { return _currencyService.FormatCurrency(CouponDiscount); } }

    //    public decimal RedeemDiscount { get; set; }
    //    public string RedeemDiscountString { get { return _currencyService.FormatCurrency(RedeemDiscount); } }

    //    public decimal Discount { get; set; }

    //    public string DiscountString { get { return _currencyService.FormatCurrency(Discount); } }

    //    public string CouponValidationErrorMessage { get; set; }

    //    public bool IsProductPriceIncludeTax { get; set; }


    //    public decimal? TaxAmount { get; set; }
    //    public DeliveryOption DeliveryOption { get; set; }
    //    public decimal? TaxCalculations
    //    {
    //        get
    //        {

    //            return TaxAmount.HasValue ? _currencyService.DcimalRound(TaxAmount.Value * (SubTotal + TotalFees - Discount - CouponDiscount - RedeemDiscount)) : 0;
    //        }
    //    }
    //    public string OrderNote { get; set; }

    //    public decimal TotalFees { get; set; }

    //    public string TaxAmountString
    //    {
    //        get
    //        {

    //            return TaxAmount.HasValue ? _currencyService.FormatCurrencyRound(TaxAmount.Value) : "-";
    //        }
    //    }

    //    public decimal? ShippingAmount { get; set; }

    //    public string ShippingAmountString
    //    {
    //        get { return ShippingAmount.HasValue ? _currencyService.FormatCurrency(ShippingAmount.Value) : "-"; }
    //    }

    //    public decimal SubTotalWithDiscount
    //    {
    //        get
    //        {

    //            return _currencyService.DcimalRound((SubTotal + TotalFees - Discount - CouponDiscount));
    //        }
    //    }

    //    public string SubTotalWithDiscountString
    //    {
    //        get
    //        {

    //            return _currencyService.FormatCurrencyRound(SubTotalWithDiscount);
    //        }
    //    }

    //    public decimal SubTotalWithDiscountWithoutTax
    //    {
    //        get
    //        {
    //            if (IsProductPriceIncludeTax)
    //            {
    //                return _currencyService.DcimalRound(SubTotalWithDiscount - TaxAmount ?? 0);
    //            }

    //            return _currencyService.DcimalRound(SubTotalWithDiscount);
    //        }
    //    }

    //    public IList<ProductFeesVM> ProductFees { get; set; }


    //    public decimal OrderTotal
    //    {
    //        get
    //        {

    //            if (IsProductPriceIncludeTax)
    //            {
    //                return _currencyService.DcimalRound(SubTotal + (ShippingAmount ?? 0) - Discount - CouponDiscount - RedeemDiscount);
    //            }

    //            return _currencyService.DcimalRound((SubTotal + TotalFees + (TaxAmount ?? 0) + (ShippingAmount ?? 0) - Discount - CouponDiscount - RedeemDiscount));
    //        }
    //    }

    //    public string OrderTotalString { get { return _currencyService.FormatCurrencyRound(OrderTotal); } }
    //    public long SiteId { get; set; }
    //    public IList<CartItemVm> Items { get; set; } = new List<CartItemVm>();
    //    public CustomerLoyalityObjectVm Loyality { get; set; } = new CustomerLoyalityObjectVm();
    //    public bool IsValid
    //    {
    //        get
    //        {
    //            foreach (var item in Items)
    //            {
    //                if (!item.IsProductAvailabeToOrder)
    //                {
    //                    return false;
    //                }

    //                if (item.ProductStockTrackingIsEnabled && item.ProductStockQuantity < item.Quantity)
    //                {
    //                    return false;
    //                }
    //            }

    //            return true;
    //        }
    //    }

    //    public string TaxPercentage { get; set; }
    //}

}
