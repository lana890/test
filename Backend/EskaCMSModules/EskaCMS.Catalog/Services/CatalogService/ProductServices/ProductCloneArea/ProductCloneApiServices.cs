using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Enum;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductCloneArea.Interfaces;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductCloneArea
{
    public class ProductCloneApiServices : IProductClone
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductDetails> productDetailsrepository;
        private readonly IWorkContext _workContext;
        private readonly IProductServices _productService;
        private readonly ISlugServices _slugServices;

        public ProductCloneApiServices(IRepository<Product> productRepository,
            IRepository<ProductDetails> ProductDetailsrepository,
            ISlugServices slugServices, IWorkContext workContext, IProductServices productService)
        {
            _productRepository = productRepository;
            productDetailsrepository = ProductDetailsrepository;
            _workContext = workContext;
            _productService = productService;
            _slugServices = slugServices;
        }

        public async Task<bool> AddProductClone(ItemCloneFormVm clone)
        {
            try
            {
                var SiteId = await _workContext.GetCurrentSiteIdAsync();
                var currentUser = await _workContext.GetCurrentUser();
                var product = await _productRepository.Query()
                    .Include(x=>x.ProductDetails)
                    .ThenInclude(x=>x.SlugInfo)
                    .Include(x => x.AttributeValues)
                    .Include(x => x.Categories)
                    .FirstOrDefaultAsync(x => x.Id == clone.Id);

                if (product == null)
                {
                    return false;
                }
              
                var newProduct = product.Clone();
                newProduct.CreatedById = currentUser.Id;
                newProduct.ModifiedById = currentUser.Id;
                newProduct.CreationDate = DateTimeOffset.Now;
                newProduct.ModificationDate = DateTimeOffset.Now;

                _productRepository.Add(newProduct);
                await _productRepository.SaveChangesAsync();
                


                    var ProductDetails = new ProductDetails();
                    ProductDetails.Name = clone.Name;
                    SlugManagementVm slugManagementVm = new()
                    {
                        CultureId = newProduct.ProductDetails.FirstOrDefault().CultureId,
                        SiteId = SiteId,
                        Slug = clone.Slug+"",
                        Entity = GeneralEnums.EEntity.Product,

                    };
                    ProductDetails.SlugInfoId = await _slugServices.UpsertAndGetId(slugManagementVm);
                    ProductDetails.Description ="";
                    ProductDetails.AddtionalMetaTags = "";
                
                    ProductDetails.ProductId = newProduct.Id;


                productDetailsrepository.Add(ProductDetails);
                await productDetailsrepository.SaveChangesAsync();

                var productPriceHistory = CreatePriceHistory(currentUser, newProduct);
                newProduct.PriceHistories.Add(productPriceHistory);

                return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ItemCloneFormVm> GetProductClone(long id)
        {
            try
            {
                var product = await _productRepository.
                                                    Query()
                                                    .Include(x=>x.ProductDetails)
                                                    .FirstOrDefaultAsync(x => x.Id == id);
                var model = new ItemCloneFormVm
                {
                    Id = product.Id,
                    Name = product.ProductDetails.OrderByDescending(x=>x.CultureId).FirstOrDefault().Name
                };
                return model;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }




        private static ProductPriceHistory CreatePriceHistory(User loginUser, Product product)
        {
            return new ProductPriceHistory
            {
                CreatedBy = loginUser,
                Product = product,
                Price = product.Price,
                SpecialForignPrice = product.SpecialForignPrice,
                SpecialPrice = product.SpecialPrice,
                SpecialPriceStart = product.SpecialPriceStart,
                SpecialPriceEnd = product.SpecialPriceEnd
            };
        }
    }
}


