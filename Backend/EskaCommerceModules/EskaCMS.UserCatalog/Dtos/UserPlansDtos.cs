using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class UserPlansCreate
    {
        public long UserId { get; set; }
        public long PlanVersionsId { get; set; }
        public string AgreementId { get; set; }
    }
    public class UserPlansUpdate
    {
        public long Id { get; set; }
        public EUserPlansStatus CurrentStatus { get; set; }
        public EUserPlansStatus NextStatus { get; set; }
        public string Tran_Ref { get; set; }
        public string AgreementId { get; set; }
        public bool IsSubscribed { get; set; }
    }

    public class UserPlansCancellation
    {

        public string UserPlanId { get; set; }
    }

    public class UserPlansOutput
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public User User { get; set; }
        public PlansVersionsOutput PlanVersions { get; set; }
        public long PlanVersionsId { get; set; }
        public string AgreementId { get; set; }
        public string Tran_ref { get; set; }
        public string PlanName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerImage { get; set; }
        public IList<PlanProductVersionOutput> Products { get; set; }
        public long OrderId { get; set; }
        public long? ParentId { get; set; }
        public string ParentAmount { get; set; }
        public Order Order { get; set; }
        public EUserPlansStatus CurrentStatus { get; set; }
        public EUserPlansStatus NextStatus { get; set; }
        public DateTimeOffset ActivationDate { get; set; }
        public DateTimeOffset CurrentStatusDate { get; set; }
        public DateTimeOffset NextStatusDate { get; set; }
        public PlanTypes PlanType { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsSubscribed { get; set; }
        public bool IsUpgradable { get; set; }
    }
    public class UserPlansGridOutput
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long PlanVersionsId { get; set; }
        public List<PlanCultureDetailsVm> PlanCultureDetails { get; set; }
        public List<userplanhistoryDto> UserPlanHistories { get; set; }
        public long OrderId { get; set; }
        public EUserPlansStatus CurrentStatus { get; set; }
        public DateTimeOffset NextPaymentDate { get; set; }
        public DateTimeOffset LastPaymentDate { get; set; }
        public string Bookings { get; set; }
        public string CurrencyCode { get; set; }
        public decimal PricePerMonth { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public PlanTypes PlanType { get; set; }
    }
    public class UserInvoicesOutputVM
    {
        public long OrderId { get; set; }
        public string InvoiceName { get; set; }
        public DateTimeOffset SubscribeDate { get; set; }
        public decimal TotalPaid { get; set; }
        public UserInvoiceDetailsVM InvoiceDetails { get; set; }

    }

    public class userplanhistoryDto
    {
       
        public EUserPlansStatus CurrentStatus { get; set; }
        public EUserPlansStatus? OldStatus { get; set; }

        public DateTimeOffset TransDate { get; set; }
        public string AdditionalInfo { get; set; }


    }
    public class UserPlansRecuring
    {
        public long UserId { get; set; }
        public long PlanVersionsId { get; set; }
        public string AgreementId { get; set; }

        public string Tran_Ref { get; set; }
        public DateTimeOffset NextPaymentDate { get; set; }
        public long OrderId { get; set; }
        public long  SiteId { get; set; }
    }
    public class SubscriberListVM
    {
        public string CustomerName { get; set; }
        public string CustomerImage { get; set; }
        public SubscriberPlanDetailVM PlanDetails { get; set; }
        public int? PlansCount { get; set; }
        public long Id { get; set; }
        public long UserId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal OrderTotal { get; set; }

    }
    public class GridOutputVM<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }
    public class SubscriberPlanDetailVM
    {
        public EUserPlansStatus CurrentStatus { get; set; }
        public List<PlanCultureDetailsVm> PlanCultureDetails { get; set; }
        public DateTimeOffset NextStatusDate { get; set; }
        public decimal NextDuePayment { get; set; }
        public decimal TotalAmount { get; set; }

    }
    public class UserInvoiceDetailsVM
    {
        public long InvoiceNo { get; set; }
        public string InvoiceName { get; set; }
        public DateTimeOffset SubscribeDate { get; set; }
        public decimal TotalPaid { get; set; }

        public string ShippingAddress { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public List<InvoiceItemVM> InvoiceItems { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal OrderTotal { get; set; }
    }
    public class InvoiceItemVM
    {
        public string ItemName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal RowTotal { get; set; }


    }
    public class SubscribersListSearchVM
    {
        public EUserPlansStatus? CurrentStatus { get; set; }
        public DateTimeOffset? NextStatusDateFrom { get; set; }
        public DateTimeOffset? NextStatusDateTo { get; set; }
        public string Name { get; set; }
    }
    public class UserPlansListSearchVM
    {
        public EUserPlansStatus? CurrentStatus { get; set; }
        public DateTimeOffset? LastPaymentDateFrom { get; set; }
        public DateTimeOffset? LastPaymentDateTo { get; set; }
        public DateTimeOffset? NextPaymentDateFrom { get; set; }
        public DateTimeOffset? NextPaymentDateTo { get; set; }
        public DateTimeOffset? StartDateFrom { get; set; }
        public DateTimeOffset? StartDateTo { get; set; }
        public PlanTypes? PlanType { get; set; }
        public string PlanName { get; set; }
    }
    public class SearchTableParam<T>
    {
        public Pagination Pagination { get; set; }

        public T Search { get; set; }

    }
}
