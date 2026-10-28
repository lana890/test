

using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Pricing;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Module.Core.Services;
using EskaCommerce.Module.Notifications.Notifiers;
using EskaCommerce.Notifications.Areas.Notifications.ViewModels;
using EskaCommerce.Notifications.Notifiers;
using EskaCommerce.Pricing.Areas.Pricing.ViewModels;
using EskaCommerce.Pricing.Model;
using EskaCommerce.Pricing.Services;
using EskaCommerce.Pricing.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Pricing.Areas.Pricing.Controllers
{
    [Area("Pricing")]
    [Authorize]
    [Route("api/cart-rules")]
    public class CartRuleApiController : Controller
    {
        private readonly IRepository<CartRule> _cartRuleRepository;
        private readonly IRepository<CartRuleCustomerGroup> cartRuleCustomerGroupRepository;
        // private readonly IRepository<CustomerGroup> customerGroupRepository;
        private readonly IWorkContext workContext;
        // private readonly ISendRuleSms sendSMs;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ISiteSettingsService settingservice;
        private readonly ITestNotifier testNotifier;
        private readonly ICouponService _ICouponService;
        public CartRuleApiController(
            IRepository<CartRule> cartRuleRepository,
            IRepository<CartRuleCustomerGroup> CartRuleCustomerGroupRepository,
            /* IRepository<CustomerGroup> CustomerGroupRepository, */
            IWorkContext workContext,
                                    /* ISendRuleSms SendSMs, */

            IServiceScopeFactory serviceScopeFactory,
                                                ISiteSettingsService settingservice,

            ITestNotifier testNotifier,

            ICouponService couponService)
        {
            _cartRuleRepository = cartRuleRepository;
            cartRuleCustomerGroupRepository = CartRuleCustomerGroupRepository;
            // customerGroupRepository = CustomerGroupRepository;
            this.workContext = workContext;
            // sendSMs = SendSMs;
            this.serviceScopeFactory = serviceScopeFactory;
            this.settingservice = settingservice;
            this.testNotifier = testNotifier;
            _ICouponService = couponService;
        }

        [HttpPost("grid")]
        public IActionResult List([FromBody] SmartTableParam param)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            IQueryable<CartRule> query = _cartRuleRepository
           .Query().Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted);

            var cartRules = query.ToSmartTableResult(
                param,
                cartRule => new
                {
                    cartRule.Id,
                    cartRule.Name,
                    cartRule.StartOn,
                    cartRule.EndOn,
                    cartRule.IsActive
                });

            return Json(cartRules);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);

            var cartRule = await _cartRuleRepository.Query().Where(x => x.SiteId == SiteId)
                .Include(x => x.Coupons)
                .Include(x => x.Products)
                .ThenInclude(p => p.Product)
                .ThenInclude(x => x.ProductDetails)
                .Include(x => x.CustomerGroups)
                .Include("CustomerGroups.CustomerGroup")
                .Include("CustomerGroups.CustomerGroup.customerGroupDetails")
                .AsNoTrackingWithIdentityResolution()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (cartRule != null)
            {
                var model = new CartRuleForm
                {
                    Id = cartRule.Id,
                    Name = cartRule.Name,
                    Description = cartRule.Description,
                    IsActive = cartRule.IsActive,
                    StartOn = cartRule.StartOn,
                    EndOn = cartRule.EndOn,
                    IsCouponRequired = cartRule.IsCouponRequired,
                    RuleToApply = cartRule.RuleToApply,
                    DiscountAmount = cartRule.DiscountAmount,
                    DiscountStep = cartRule.DiscountStep,
                    MaxDiscountAmount = cartRule.MaxDiscountAmount,
                    UsageLimitPerCoupon = cartRule.UsageLimitPerCoupon,
                    UsageLimitPerCustomer = cartRule.UsageLimitPerCustomer,
                    IsAutoDiscount = cartRule.IsAutoDiscount,
                    IsCustomerGroup = cartRule.IsCustomerGroup,
                    Quantity = cartRule.Quantity,
                    Products = cartRule.Products.Select(x => x.Product)
                                  .Select(x => new CartRuleProductVm
                                  {
                                      Id = x.Id,
                                      Name = x.ProductDetails.OrderByDescending(x => x.CultureId).Select(x => x.Name).FirstOrDefault(),
                                      IsPublished = x.Status == EskaCMS.Entities.Enum.GeneralEnums.EStatus.Published ? true : false
                                  }).ToList(),
                    CustomerGroups = cartRule.CustomerGroups.Select(x => x.CustomerGroup).Select(x => new CustomerGroupsForm
                    {

                        Id = x.Id,
                        Name = x.customerGroupDetails.Select(x => x.Name).FirstOrDefault(),
                        IsActive = x.IsActive

                    }).ToList()
                };
                return Json(model);
            }
            else
            {
                return BadRequest(new { Error = $"The cart rule is not found!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CartRuleForm model)
        {
            if (ModelState.IsValid)
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var cartRule = new CartRule
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    StartOn = model.StartOn,
                    EndOn = model.EndOn,
                    IsCouponRequired = model.IsCouponRequired,
                    RuleToApply = model.RuleToApply,
                    DiscountAmount = model.DiscountAmount,
                    DiscountStep = model.DiscountStep,
                    MaxDiscountAmount = model.MaxDiscountAmount,
                    UsageLimitPerCoupon = model.UsageLimitPerCoupon,
                    UsageLimitPerCustomer = model.UsageLimitPerCustomer,
                    IsAutoDiscount = model.IsAutoDiscount,
                    Quantity = model.Quantity,
                    SiteId = SiteId,
                    IsCustomerGroup = model.IsCustomerGroup,
                    Status = EStatus.Active,


                };

                foreach (var item in model.Products)
                {
                    var cartRuleProduct = new CartRuleProduct
                    {
                        CartRule = cartRule,
                        ProductId = item.Id
                    };
                    cartRule.Products.Add(cartRuleProduct);
                }

                _cartRuleRepository.Add(cartRule);
                await _cartRuleRepository.SaveChangesAsync();

                return Ok(new { id = cartRule.Id });
            }
            return BadRequest(ModelState);
        }

        [HttpPost("UpsertCartCustomerGroup")]
        public async Task<IActionResult> UpsertCartCustomerGroup([FromBody] CartCustomerRuleForm model)
        {
            try
            {
                var SiteId = workContext.GetCurrentSiteId();
                var Query = await _cartRuleRepository.Query().Where(x => x.Id == model.CartId).FirstOrDefaultAsync();
                if (Query == null)
                    return NotFound();
                var OriginalCartGroups = await cartRuleCustomerGroupRepository.Query().Where(x => x.CartRuleId == model.CartId).ToListAsync();


                var CartRuleCustomerGroup = new List<CartRuleCustomerGroup>();
                if (Query.IsCustomerGroup)
                {
                    foreach (var GroupId in model.CustomerGroupIds)
                    {
                        if (!OriginalCartGroups.Any(x => x.CustomerGroupId == GroupId))
                        {
                            CartRuleCustomerGroup.Add(new CartRuleCustomerGroup { CartRuleId = model.CartId, CustomerGroupId = GroupId });

                        }

                    }
                }
                var ToBeDeleted = Query.IsCustomerGroup ? OriginalCartGroups.Where(x => !model.CustomerGroupIds.Any(c => c == x.CustomerGroupId)).ToList()
                                                        : OriginalCartGroups;
                if (ToBeDeleted.Count > 0)
                {

                    cartRuleCustomerGroupRepository.RemoveRange(ToBeDeleted);

                    await cartRuleCustomerGroupRepository.SaveChangesAsync();
                }

                if (CartRuleCustomerGroup.Count > 0)
                {
                    cartRuleCustomerGroupRepository.AddRange(CartRuleCustomerGroup);

                    await cartRuleCustomerGroupRepository.SaveChangesAsync();

                }


                return Ok();
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] CartRuleForm model)
        {
            if (ModelState.IsValid)
            {
                var cartRule = await _cartRuleRepository.Query()
                    .Include(x => x.Coupons)
                    .Include(x => x.Products)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (cartRule == null)
                {
                    return NotFound();
                }

                cartRule.Name = model.Name;
                cartRule.Description = model.Description;
                cartRule.StartOn = model.StartOn;
                cartRule.EndOn = model.EndOn;
                cartRule.IsActive = model.IsActive;
                cartRule.IsCouponRequired = model.IsCouponRequired;
                cartRule.RuleToApply = model.RuleToApply;
                cartRule.DiscountAmount = model.DiscountAmount;
                cartRule.DiscountStep = model.DiscountStep;
                cartRule.MaxDiscountAmount = model.MaxDiscountAmount;
                cartRule.UsageLimitPerCoupon = model.UsageLimitPerCoupon;
                cartRule.UsageLimitPerCustomer = model.UsageLimitPerCustomer;
                cartRule.UsageLimitPerCustomer = model.UsageLimitPerCustomer;
                cartRule.IsAutoDiscount = model.IsAutoDiscount;
                cartRule.Quantity = model.Quantity;
                cartRule.IsCustomerGroup = model.IsCustomerGroup;


                if (model.IsCouponRequired && !string.IsNullOrWhiteSpace(model.CouponCode))
                {
                    var coupon = cartRule.Coupons.FirstOrDefault();
                    if (coupon == null)
                    {
                        coupon = new Coupon
                        {
                            CartRule = cartRule,
                            Code = model.CouponCode
                        };

                        cartRule.Coupons.Add(coupon);
                    }
                    else
                    {
                        coupon.Code = model.CouponCode;
                    }
                }

                foreach (var item in model.Products)
                {
                    var cartRuleProduct = cartRule.Products.FirstOrDefault(x => x.ProductId == item.Id);
                    if (cartRuleProduct == null)
                    {
                        cartRuleProduct = new CartRuleProduct
                        {
                            CartRule = cartRule,
                            ProductId = item.Id
                        };
                        cartRule.Products.Add(cartRuleProduct);
                    }
                }

                var modelProductIds = model.Products.Select(x => x.Id);
                var deletedProducts = cartRule.Products.Where(x => !modelProductIds.Contains(x.ProductId)).ToList();
                foreach (var item in deletedProducts)
                {
                    item.CartRule = null;
                    cartRule.Products.Remove(item);
                }

                await _cartRuleRepository.SaveChangesAsync();
                return Accepted();
            }
            return BadRequest(ModelState);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var cartRule = await _cartRuleRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (cartRule == null)
            {
                return NotFound();
            }

            try
            {
                cartRule.Status = EStatus.Deleted;
                await _cartRuleRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { Error = $"The cart rule {cartRule.Name} can't not be deleted because it has been used" });
            }

            return NoContent();
        }

        [HttpPost("coupongrid")]
        public IActionResult CouponList([FromBody] SmartTableParam param)
        {

            return Json(_ICouponService.CouponList(param));
        }
        [HttpPost("createcoupon")]
        public async Task<IActionResult> CreateCoupon([FromBody] CouponFormVM model)
        {
            try
            {

                return Ok(new { id = await _ICouponService.AddCoupon(model) });

            }
            catch (Exception ex)
            {

                ex = ExceptionHelper.ThrowDeepException(ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getcoupon/{id}")]
        public async Task<IActionResult> GetCoupon(long Id)
        {

            return Json(await _ICouponService.GetCouponById(Id));
        }

        [HttpPut("editcoupon/{id}")]
        public async Task<IActionResult> EditCoupon(long id, [FromBody] CouponFormVM model)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    await _ICouponService.EditCoupon(id, model);
                    return Accepted();
                }
                return BadRequest(model);



            }
            catch (Exception ex)
            {

                ex = ExceptionHelper.ThrowDeepException(ex);
                        return BadRequest(ex.Message);    
            
            }
            }

            [HttpDelete("deletecoupon/{id}")]
        public async Task<IActionResult> DeleteCoupon(long id)
        {
            try
            {
                await _ICouponService.DeleteCoupon(id);
            }
            catch (System.Exception exc)
            {

                return BadRequest(exc.Message);
            }


            return NoContent();
        }


        [HttpGet]
        [Route("GetTemplateOfBulkCouponAddition")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTemplateOfBulkCouponAddition()
        {
            try
            {
                ExcelFile Template = await ExcelHelper.ExportExcelTemplate<CouponVM>("Coupons Template");
                return File(Template.FileBytes, Template.ContentType, Template.Name);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpGet("GetCustomerGroupByRuleId/{CartRuleId}")]

        public async Task<GetRuleCustomerGroupViewModel> GetCustomerGroupByRuleId([FromRoute] long CartRuleId)
        {
            try
            {
                var Query = await _cartRuleRepository.Query().Include(x => x.CustomerGroups).ThenInclude(x => x.CustomerGroup).ThenInclude(x => x.customerGroupDetails).Where(x => x.Id == CartRuleId)
                                                                       .Select(x => new GetRuleCustomerGroupViewModel
                                                                       {
                                                                           CartRuleId = x.Id,
                                                                           CustomerGroup = x.CustomerGroups.Select(x => new DetailsRuleCustomerGroupViewModel
                                                                           {

                                                                               Id = x.CustomerGroupId,
                                                                               Name = x.CustomerGroup.customerGroupDetails.Select(x => x.Name).FirstOrDefault()

                                                                           }).ToList()

                                                                       }).FirstOrDefaultAsync();

                return Query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        [HttpGet("RemoveCustomerGroup/{CartId}/{CustomerGroupId}")]


        public async Task<bool> RemoveCustomerGroup([FromRoute] long CartId, [FromRoute] long CustomerGroupId)
        {
            try
            {
                var Query = await cartRuleCustomerGroupRepository.Query().Where(x => x.CartRuleId == CartId && x.CustomerGroupId == CustomerGroupId).FirstOrDefaultAsync();


                if (Query == null)
                {
                    throw new Exception("NotFound This Cart Rule With Specific Group");

                }
                cartRuleCustomerGroupRepository.Remove(Query);

                await cartRuleCustomerGroupRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpPost]
        [Route("AddBulkCoupons/{CartRoleId}")]
        [AllowAnonymous]

   
        public async Task<IActionResult> AddBulkCoupons([FromForm] IFormFile CouponSheet, [FromRoute] long CartRoleId)
        {
            try
            {
                List<CouponVM> Coupons = await ExcelHelper.ImportExcel<CouponVM>(CouponSheet);
                await _ICouponService.BulkAddition(CartRoleId, Coupons);

                return Ok("Coupons are added successfully");
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


    }

}

