using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.Shared;
using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;

using EskaCommerce.Catalog.Areas.Catalog.ViewModels.ProductAttribute;
using EskaCommerce.Catalog.Data;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.DataAccess.Data;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductTemplateArea.TemplateInterfaces;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductTemplateArea
{
    public class ProductTemplateServices : IProductTemplateServices
    {
        private readonly IRepository<ProductTemplate> _productTemplateRepository;
        private readonly IRepository<ProductTemplateDetails> productTemplateDetailsRepository;
        private readonly IRepository<ProductAttribute> _productAttributeRepository;
        private readonly IRepository<ProductTemplateAttribute> ProductTemplateAttributeRepository;
        private readonly EskaDCMSDbContext _EskaDCMSDbContext;
        private readonly IWorkContext _workContext;

        public ProductTemplateServices(IRepository<ProductTemplate> productTemplateRepository,

            IRepository<ProductTemplateDetails> ProductTemplateDetailsRepository,
            IRepository<ProductAttribute> productAttributeRepository,
            IRepository<ProductTemplateAttribute> ProductTemplateAttributeRepository, IWorkContext workContext, EskaDCMSDbContext eskaDCMSDbContext)
        {
            _productTemplateRepository = productTemplateRepository;
            productTemplateDetailsRepository = ProductTemplateDetailsRepository;
            _productAttributeRepository = productAttributeRepository;
            this.ProductTemplateAttributeRepository = ProductTemplateAttributeRepository;
            _workContext = workContext;
            _EskaDCMSDbContext = eskaDCMSDbContext;
        }
        public async Task<PaginationResult<ProductTemplateResponseViewModel>> GetProductTemplate(long SiteId, AttributeParams attributeParams)
        {
            try
            {

                var productTemplates = _productTemplateRepository

                                    .Query()
                                    .Include(c => c.ProductTemplateDetails)
                                    .ThenInclude(x => x.Culture)
                                    .Where(s => s.SiteId == SiteId && s.Status != EStatus.Deleted);

                var Result = await productTemplates.Where(x => string.IsNullOrEmpty(attributeParams.SearchText) || x.ProductTemplateDetails.Any(x => x.Name.Contains(attributeParams.SearchText)))
                     .Select(x => new ProductTemplateResponseViewModel
                     {
                         Status = x.Status,
                         Id = x.Id,
                         productTemplateDetails = x.ProductTemplateDetails.Select(x => new ProductTemplateDetailsResponseViewModel
                         {


                             CultureCode = x.Culture.CultureId,
                             CultureId = x.CultureId,
                             Id = x.Id,
                             Name = x.Name,
                             TemplateId = x.ProductTemplateId
                         }).ToList()



                     }).PaginateAsync(attributeParams.PageSize, attributeParams.PageNumber);

                return Result;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<List<TemplateModel>> GetProductTemplateList()
        {
            try
            {
                var templateList = await _productTemplateRepository.Query().Include(x => x.ProductTemplateDetails)
                    .SelectMany(x => x.ProductTemplateDetails)
                    .Select(x => new TemplateModel
                    {
                        Id = x.ProductTemplateId,
                        Name = x.Name,
                        Status = x.ProductTemplate.Status
                    }).ToListAsync();
                return templateList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<TemplateAttributeViewModel>> GetProductTemplateById(long Id)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var productTemplate = await _productTemplateRepository
                .Query()
                .Include(x => x.TemplateAttributes)
                .Include(x => x.ProductTemplateDetails)
                .ThenInclude(x => x.Culture)
                .Where(x => x.Id == Id && x.SiteId == SiteId && x.Status != EStatus.Deleted)
                .Select(t => new TemplateAttributeViewModel
                {
                    Id = t.Id,
                    templateDetails = t.ProductTemplateDetails.Select(x => new TemplateDetails
                    {

                        Name = x.Name,
                        CultureId = x.CultureId,



                    }).ToList(),
                    templateAttributes = t.TemplateAttributes.Select(
                       x => new TemplateAttributeDetailsViewModel()
                       {
                           Id = x.ProductAttributeId,
                           Name = x.ProductAttribute.AttributeDetails.FirstOrDefault().Name,
                           GroupName = x.ProductAttribute.Group.productAttributeGroupDetails.FirstOrDefault().Name,
                           GroupId = x.ProductAttribute.GroupId
                       }).ToList(),
                    Status = t.Status

                }).ToListAsync();



                return productTemplate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<ProductTemplatebyCultureViewModel> GetTemplateByCultureId(long CultureId)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();
                var productTemplate = await _productTemplateRepository
                .Query()
                .Include(x => x.TemplateAttributes)
                .Include(c => c.ProductTemplateDetails)
                .ThenInclude(x => x.Culture)
                .Where(x => x.ProductTemplateDetails.Any(y => y.CultureId == CultureId) && x.SiteId == SiteId && x.Status != EStatus.Deleted)
                .Select(t => new ProductTemplatebyCultureViewModel
                {
                    Id = t.Id,
                    Name = t.ProductTemplateDetails.FirstOrDefault().Name,
                    CultureId = t.ProductTemplateDetails.FirstOrDefault().CultureId,
                    templateAttributes = t.TemplateAttributes.Select(
                       x => new TemplateAttributeDetailsViewModel()
                       {
                           Id = x.ProductAttributeId,
                           Name = x.ProductAttribute.AttributeDetails.FirstOrDefault().Name,
                           GroupName = x.ProductAttribute.Group.productAttributeGroupDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                           GroupId = x.ProductAttribute.GroupId
                       }).ToList(),
                    estatus = t.Status

                }).FirstOrDefaultAsync();



                return productTemplate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> AddProductTemplate(ProductTemplateViewModel model, long SiteId)
        {
            var strategy = _EskaDCMSDbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _EskaDCMSDbContext.Database.BeginTransactionAsync();
                try
                {


                    var itemTemplate = new ProductTemplate();

                    itemTemplate.SiteId = SiteId;
                    itemTemplate.Status = model.Status;


                    var productattributeId = 0;

                    foreach (var attributeId in model.AttributesItemId)
                    {
                        itemTemplate.AddAttribute(attributeId);
                        await _productAttributeRepository.SaveChangesAsync();
                    }

                    _productTemplateRepository.Add(itemTemplate);
                    await _productTemplateRepository.SaveChangesAsync();

                    var productTemplateDetails = new List<ProductTemplateDetails>();
                    foreach (var item in model.productTemplateDetails)
                    {
                        productTemplateDetails.Add(new ProductTemplateDetails
                        {

                            CultureId = item.CultureId,
                            Name = item.Name,
                            ProductTemplateId = itemTemplate.Id


                        });
                    }
                    productTemplateDetailsRepository.AddRange(productTemplateDetails);
                    await productTemplateDetailsRepository.SaveChangesAsync();






                    await transaction.CommitAsync();
                    return true;

                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw ex;
                }
            });
        }

        public async Task<bool> UpdateProductTemplate(ProductTemplateViewModel model)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                var productTemplate = await _productTemplateRepository.Query()
                                                              .Include(x => x.ProductTemplateDetails)
                                                              .Include(x => x.TemplateAttributes)
                                                              .Where(x => x.Id == model.Id)
                                                              .FirstOrDefaultAsync();

                if (productTemplate.TemplateAttributes.Count()>0)
                {
                    var ProdcutProductTemplate = await ProductTemplateAttributeRepository.Query().Where(x => x.ProductTemplateId == model.Id).ToListAsync();

                    ProductTemplateAttributeRepository.RemoveRange(ProdcutProductTemplate);
                    await ProductTemplateAttributeRepository.SaveChangesAsync();


                }

                foreach (var attributeId in model.AttributesItemId)
                {

                    productTemplate.AddAttribute(attributeId);
                    await _productTemplateRepository.SaveChangesAsync();

                }

                await _productAttributeRepository.SaveChangesAsync();
                foreach (var item in model.productTemplateDetails)
                {

                    foreach (var temp in productTemplate.ProductTemplateDetails)
                    {
                        if (temp.CultureId == item.CultureId)
                        {
                            temp.Name = item.Name;


                        }

                    }


                }
                await productTemplateDetailsRepository.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ChangeProductTemplateStatus(long Id, EStatus NewStatus)
        {
            return await _productTemplateRepository.ExecuteInTransactionAsync(async () =>
            {
                var productTemplates = await _productTemplateRepository.Query().Where(x => x.Id == Id).Include(x => x.TemplateAttributes).ToListAsync();
                if (productTemplates == null)
                {
                    throw new Exception("Product Template Not Found");

                }

                foreach (var productTemplate in productTemplates)
                {
                    if (productTemplate.Status == EStatus.Deleted && productTemplate.TemplateAttributes.Count > 0)
                    {
                        throw new Exception("Could not Delete This Template");
                    }

                    productTemplate.Status = NewStatus;


                }
                _productAttributeRepository.SaveChanges();

                return true;
            });

        }



    }
}

