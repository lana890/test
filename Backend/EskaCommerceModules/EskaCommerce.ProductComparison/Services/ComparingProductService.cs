using EskaCMS.Infrastructure.Data;
using System;
using System.Linq;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.ProductComparison;
using EskaCommerce.ProductComparison.Areas.ProductComparison.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Core;
using Microsoft.AspNetCore.Identity;
using EskaCMS.Media.Services;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.ProductComparison.Models;
using System.Collections.Generic;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Core.Services;
using EskaCMS.Core.Models;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;

namespace EskaCommerce.ProductComparison.Services
{
    public class ComparingProductService : IComparingProductService
    {
        private readonly IRepository<ComparingProduct> _comparingProductRepository;
        private readonly IRepository<User> userRepository;
        private readonly IProductPricingService productPricingService;
        private readonly IWorkContext workContext;
        private readonly IUserSiteService _userSiteService;
        private readonly ICurrencyService _currencyService;
        private readonly int MaxNumComparingProduct = 4;

        public ComparingProductService(IRepository<ComparingProduct> comparingProductRepository, IRepository<User> userRepository,
            IProductPricingService productPricingService,
            ICurrencyService currencyService,
             IUserSiteService userSiteService,
            IWorkContext workContext)
        {
            _comparingProductRepository = comparingProductRepository;
            this.userRepository = userRepository;
            this.productPricingService = productPricingService;
            this.workContext = workContext;
            _currencyService = currencyService;
            _userSiteService = userSiteService;
        }

        public async Task<AddToComparisonResult> AddToComparison([FromBody] AddToComparisonModel model)
        {
            try
            {
                var currentUserId = await workContext.GetCurrentUserId();
                var Result = new AddToComparisonResult();

                AddToComparison(currentUserId, model.ProductId);
                Result.Message = "The product has been added to comparison items";

                Result.ProductComparisons = await GetCompareProducts();

                return Result;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<ComparingProductVm>> GetCompareProducts()
        {
            try
            {
                var currentUserId = await workContext.GetCurrentUserId();

                var CultureId = workContext.GetCurrentCultureId();
                var IsLocalCountry = await _userSiteService.IsLocalCountry();
                var userCountry = await _userSiteService.GetUserCountry();
                var Currency = await _currencyService.GetUserCurrencyByCulture();
                var comparingProducts = _comparingProductRepository.Query()
                    .Include(x => x.Product)
                    .Include("Product.ThumbnailImage")
                    .Include("Product.ProductDetails")
                    .Include("Product.ProductDetails.SlugInfo")
                    .Include("Product.AttributeValues")
                    .Include("Product.AttributeValues.AttributeValueDetails")
                    .Include("Product.AttributeValues.Attribute")
                    .Include("Product.AttributeValues.Attribute.AttributeDetails")
                    .Include(x => x.Product)
                    .Include("Product.TaxClass")
                    .Include("Product.TaxClass.TaxRate")
                    .Include(x => x.Product)
                    .ThenInclude(x => x.ProductAvaliablity)
                    .Where(pc => pc.Product.ProductAvaliablity.Count() == 0  || pc.Product.ProductAvaliablity.Any(i => i.CountryId == userCountry.Id && i.Permission == EProductCountryPermission.Allowed))
                    .Where(x => x.UserId == currentUserId);

                if (!IsLocalCountry)
                {
                    comparingProducts = comparingProducts.Where(x => x.Product.ForeignPrice.HasValue);

                }


                var result = await comparingProducts.Select(x => new ComparingProductVm
                {
                    ProductName = x.Product.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                    Description = x.Product.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Description,
                    ShortDescription = x.Product.ProductDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().ShortDescription,
                    ProductImage = x.Product.ThumbnailImage == null ? "" : x.Product.ThumbnailImage.PublicUrl,
                    CalculatedProductPrice = productPricingService.CalculatedProductPrice(x.Product, userCountry.Id, Currency.Rate, Currency.Code, IsLocalCountry),
                    ProductId = x.ProductId,
                    Slug = x.Product.ProductDetails.Where(x => x.CultureId == CultureId).Select(x => x.SlugInfo == null ? "" : x.SlugInfo.Slug).FirstOrDefault(),
                    Attribute = x.Product.AttributeValues.Select(a => new AttributeValueVm
                    {
                        AttributeId = a.AttributeId,
                        Value = a.AttributeValueDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Value
                    ,
                        Names = a.Attribute.AttributeDetails == null ? new List<string>() : a.Attribute.AttributeDetails.Where(x => x.CultureId == CultureId).Select(x => x.Name).ToList()
                    }).ToList()
                }).ToListAsync();


                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public void MigrateComparingProduct(long fromUserId, long toUserId)
        {
            var comparingProducts = _comparingProductRepository.Query().Where(x => x.UserId == fromUserId);
            foreach (var item in comparingProducts)
            {
                item.UserId = toUserId;
            }

            _comparingProductRepository.SaveChanges();
        }

        public async Task<List<ComparingProductVm>> Remove(long Id)
        {
            try
            {
                var currentUserId = await workContext.GetCurrentUserId();

                var productComparison = await _comparingProductRepository.Query().Where(x => x.UserId == currentUserId && x.ProductId == Id).FirstOrDefaultAsync();

                if (productComparison == null)
                {
                    throw new Exception("Not Found Comparison Product");
                }

                _comparingProductRepository.Remove(productComparison)
                    ;
                await _comparingProductRepository.SaveChangesAsync();

                return await GetCompareProducts();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #region Helpers
        private void AddToComparison(long userId, long productId)
        {
            try
            {


                var isProductExisted = _comparingProductRepository.Query().Any(x => x.ProductId == productId && x.UserId == userId);
                if (!isProductExisted)
                {
                    var comparingProduct = new ComparingProduct
                    {
                        UserId = userId,
                        ProductId = productId,
                        CreatedOn = DateTimeOffset.Now
                    };

                    _comparingProductRepository.Add(comparingProduct);
                    _comparingProductRepository.SaveChanges();
                }


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion
    }
}
