using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.DinkToPdf;
using System;
using DinkToPdf.Contracts;
using DinkToPdf;
using System.IO;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Orders;

namespace EskaCommerce.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]
    [Authorize]
    [Microsoft.AspNetCore.Mvc.Route("api/invoices")]
    public class InvoiceApiController : Controller
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IWorkContext _workContext;
        //private readonly IRazorViewRenderer _viewRender;
        //private readonly IPdfConverter _pdfConverter;
        private readonly ICurrencyService _currencyService;
        private IConverter _converter;
        public InvoiceApiController(IConverter converter, IRepository<Order> orderRepository, IWorkContext workContext, ICurrencyService currencyService)
        {
            _orderRepository = orderRepository;
            _workContext = workContext;
            //_viewRender = viewRender;
            //_pdfConverter = pdfConverter;
            _currencyService = currencyService;
            _converter = converter;
        }

        [HttpGet("print1/{id}")]
        public async Task<IActionResult> Print(long id)
        {
            var order = await _orderRepository
                .Query()
                //.Include(x => x.ShippingAddress).ThenInclude(x => x.District)
                //.Include(x => x.ShippingAddress).ThenInclude(x => x.StateOrProvince)
                //.Include(x => x.ShippingAddress).ThenInclude(x => x.Country)
                //.Include(x => x.BillingAddress).ThenInclude(x => x.District)
                //.Include(x => x.BillingAddress).ThenInclude(x => x.StateOrProvince)
                //.Include(x => x.BillingAddress).ThenInclude(x => x.Country)
                .AsNoTracking()
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x=>x.ProductDetails)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.OptionCombinations).ThenInclude(x => x.Option)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin") && order.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this order" });
            }

            var model = new OrderDetailVm(_currencyService)
            {
                Id = order.Id,
                CreatedOn = order.CreatedOn,
                OrderStatus = order.OrderStatus,
                OrderStatusString = order.OrderStatus.ToString(),
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,
                ShippingMethod = order.ShippingMethod,
                PaymentMethod = order.PaymentMethod,
                PaymentFeeAmount = order.PaymentFeeAmount,
                Subtotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                SubTotalWithDiscount = order.SubTotalWithDiscount,
                TaxAmount = order.TaxAmount,
                ShippingAmount = order.ShippingFeeAmount,
                OrderTotal = order.OrderTotal,
                OrderCurrencyCode = order.CurrencyCode,
                //ShippingAddress = new ShippingAddressVm
                //{
                //    AddressLine1 = order.ShippingAddress.AddressLine1,
                //    CityName = order.ShippingAddress.City,
                //    ZipCode = order.ShippingAddress.ZipCode,
                //    ContactName = order.ShippingAddress.ContactName,
                //    DistrictName = order.ShippingAddress.District?.Name,
                //    StateOrProvinceName = order.ShippingAddress.StateOrProvince.Name,
                //    Phone = order.ShippingAddress.Phone
                //},
                //BillingAddress = new ShippingAddressVm
                //{
                //    AddressLine1 = order.BillingAddress.AddressLine1,
                //    CityName = order.BillingAddress.City,
                //    ZipCode = order.BillingAddress.ZipCode,
                //    ContactName = order.BillingAddress.ContactName,
                //    DistrictName = order.BillingAddress.District?.Name,
                //    StateOrProvinceName = order.BillingAddress.StateOrProvince.Name,
                //    Phone = order.BillingAddress.Phone
                //},
                OrderItems = order.OrderItems.Select(x => new OrderItemVm(_currencyService)
                {
                    Id = x.Id,
                    ProductId = x.Product.Id,
                    ProductName = x.Product.ProductDetails.Select(f=>f.Name).FirstOrDefault(),
                    ProductPrice = x.ProductPrice,
                    Quantity = x.Quantity,
                    DiscountAmount = x.DiscountAmount,
                    TaxAmount = x.TaxAmount,
                    TaxPercent = x.TaxPercent,
                    OrderCurrencyCode = order.CurrencyCode,
                }).ToList()
            };

            //var invoiceHtml = await _viewRender.RenderViewToStringAsync("/Areas/Orders/Views/Shared/InvoicePdf.cshtml", model);
            // var invoiceHtml = @"";
            //byte[] pdf = _pdfConverter.Convert(invoiceHtml);
            return Ok(model);
        }
        [HttpGet("invoice/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Invoice(long id)
        {
            var order = await _orderRepository
                .Query()
                //.Include(x => x.ShippingAddress).ThenInclude(x => x.District)
                //.Include(x => x.ShippingAddress).ThenInclude(x => x.StateOrProvince)
                //.Include(x => x.ShippingAddress).ThenInclude(x => x.Country)
                //.Include(x => x.BillingAddress).ThenInclude(x => x.District)
                //.Include(x => x.BillingAddress).ThenInclude(x => x.StateOrProvince)
                //.Include(x => x.BillingAddress).ThenInclude(x => x.Country)
                .AsNoTracking()
                .Include(x => x.OrderItems).ThenInclude(x => x.Product)
                .Include(x => x.OrderItems).ThenInclude(x => x.Product).ThenInclude(x => x.OptionCombinations).ThenInclude(x => x.Option)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            //var currentUser = await _workContext.GetCurrentUser();
            //if (!User.IsInRole("admin") && order.VendorId != currentUser.VendorId)
            //{
            //    return BadRequest(new { error = "You don't have permission to manage this order" });
            //}

            var model = new OrderDetailVm(_currencyService)
            {
                Id = order.Id,
                CreatedOn = order.CreatedOn,
                OrderStatus = order.OrderStatus,
                OrderStatusString = order.OrderStatus.ToString(),
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,
                ShippingMethod = order.ShippingMethod,
                PaymentMethod = order.PaymentMethod,
                PaymentFeeAmount = order.PaymentFeeAmount,
                Subtotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                SubTotalWithDiscount = order.SubTotalWithDiscount,
                TaxAmount = order.TaxAmount,
                ShippingAmount = order.ShippingFeeAmount,
                OrderTotal = order.OrderTotal,
                OrderCurrencyCode = order.CurrencyCode,
                //ShippingAddress = new ShippingAddressVm
                //{
                //    AddressLine1 = order.ShippingAddress.AddressLine1,
                //    CityName = order.ShippingAddress.City,
                //    ZipCode = order.ShippingAddress.ZipCode,
                //    ContactName = order.ShippingAddress.ContactName,
                //    DistrictName = order.ShippingAddress.District?.Name,
                //    StateOrProvinceName = order.ShippingAddress.StateOrProvince.Name,
                //    Phone = order.ShippingAddress.Phone
                //},
                //BillingAddress = new ShippingAddressVm
                //{
                //    AddressLine1 = order.BillingAddress.AddressLine1,
                //    CityName = order.BillingAddress.City,
                //    ZipCode = order.BillingAddress.ZipCode,
                //    ContactName = order.BillingAddress.ContactName,
                //    DistrictName = order.BillingAddress.District?.Name,
                //    StateOrProvinceName = order.BillingAddress.StateOrProvince.Name,
                //    Phone = order.BillingAddress.Phone
                //},
                OrderItems = order.OrderItems.Select(x => new OrderItemVm(_currencyService)
                {
                    Id = x.Id,
                    ProductId = x.Product.Id,
                    ProductName = x.Product.ProductDetails.Select(f => f.Name).FirstOrDefault(),
                    ProductPrice = x.ProductPrice,
                    Quantity = x.Quantity,
                    DiscountAmount = x.DiscountAmount,
                    TaxAmount = x.TaxAmount,
                    TaxPercent = x.TaxPercent,
                    OrderCurrencyCode = order.CurrencyCode,
                }).ToList()
            };

            //var invoiceHtml = await _viewRender.RenderViewToStringAsync("/Areas/Orders/Views/Shared/InvoicePdf.cshtml", model);
            var invoiceHtml = "<p>Rami</p>";
            //byte[] pdf = _pdfConverter.Convert(invoiceHtml);
            //return File(pdf, "application/pdf", $"Invoice-{id}.pdf");
            return null;
        }




        [HttpPost("print")]
        public IActionResult CreatePDF([FromBody] InvoiceVM Invoice)
        {
            var output = _converter.Convert(new HtmlToPdfDocument()
            {
                Objects = {new ObjectSettings(){
                    PagesCount = true,
                    WebSettings={ DefaultEncoding="utf-8"},
                    HtmlContent = Invoice.HtmlContent } }
            });
            return new FileContentResult(output, "application/pdf");
        }

        [HttpPost("print-receipt")]
        public IActionResult PrintReceipt([FromBody] ReceiptVM Invoice)
        {
            var output = _converter.Convert(new HtmlToPdfDocument()
            {
                Objects = {new ObjectSettings(){
                    PagesCount = true,
                    WebSettings={ DefaultEncoding="utf-8"},
                    HtmlContent = Invoice.HtmlContent } }
            });
            return new FileContentResult(output, "application/pdf");
        }
    }

}
