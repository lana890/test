using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.ProductAttribute;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Enum;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Infrastructure.Pagination;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Catalog.ProductsAttributes;
using EskaCMS.DataAccess.Data;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeArea.Interfaces;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeArea
{

    public class ProductAttributApiServices : IProductApiAttribute
    {
        private readonly IRepository<ProductAttribute> _productAttribute;
        private readonly IRepository<ProductAttributeValue> _productValue;
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<ProductTemplateAttribute> _producttempAttributeRepository;
        private readonly IRepository<ProductAttributeDetails> productAttributeDetailsRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepository<ProductAttributeValueDetails> productAttributeValueDetailsRepository;

        public ProductAttributApiServices(
            IRepository<ProductAttribute> productAttribute,
            IRepository<ProductAttributeValue> productValue,
            IWorkContext workContext,
            IRepository<ProductAttributeValueDetails> ProductAttributeValueDetailsRepository,
            EskaDCMSDbContext context,
            IRepository<ProductTemplateAttribute> producttempAttributeRepository,
            IRepository<ProductAttributeDetails> ProductAttributeDetailsRepository)
        {
            _productAttribute = productAttribute;
            _productValue = productValue;
            _workContext = workContext;
            productAttributeValueDetailsRepository = ProductAttributeValueDetailsRepository;
            _context = context;
            _producttempAttributeRepository = producttempAttributeRepository;
            productAttributeDetailsRepository = ProductAttributeDetailsRepository;
        }



        #region AttributeValue
        public async Task<List<GetProductValueVM>> GetAttributeValuebyProductId(long ProductId)
        {
            try
            {
                var Query = await _productValue
                                            .Query()
                                            .Include(x => x.AttributeValueDetails)
                                            .Include(x=>x.Attribute)
                                            .ThenInclude(x=>x.AttributeDetails)
                                            .Where(x => x.ProductId == ProductId)
                                            .AsNoTrackingWithIdentityResolution()
                                            .Select(p => new GetProductValueVM
                                            {
                                                ProductId = p.ProductId,
                                                AttributeId = p.AttributeId,
                                                AttributeName=p.Attribute.AttributeDetails.Count()>0? p.Attribute.AttributeDetails.Select(x=>x.Name).FirstOrDefault():"",
                                                AttributeValueCulture = p.AttributeValueDetails.Select(x => new AttributeValueCultureViewModel
                                                {
                                                    CultureId = x.CultureId,
                                                    Value = x.Value


                                                }).ToList(),
                                                AttributeValueId = p.Id,
                                            }).ToListAsync();

                return Query;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> AddProductAttributeValue(List<RequestProductValueVM> productAttributeVM,long ProductId)
        {
            try
            {
                
                    var Attributevalue = await _productValue.Query().Where(x => ProductId == x.ProductId).ToListAsync();
                
                    if (Attributevalue.Count > 0)
                    {
                        var AttributeValueIds = Attributevalue.ConvertAll(x => x.Id);

                        var AttributeValueDetails = await productAttributeValueDetailsRepository.Query()
                                                                                                .Where(x => AttributeValueIds
                                                                                                .Any(d => d == x.AttributeValueId))
                                                                                                .ToListAsync();
                        productAttributeValueDetailsRepository.RemoveRange(AttributeValueDetails);

                        await productAttributeValueDetailsRepository.SaveChangesAsync();

                        _productValue.RemoveRange(Attributevalue);

                        await _productValue.SaveChangesAsync();
                    }
                

                foreach (var attribute in productAttributeVM)
                {
                    var attributeValue = new ProductAttributeValue
                    {
                        AttributeId = attribute.AttributeId,
                        ProductId = ProductId,
                    };
                    _productValue.Add(attributeValue);
                    await _productValue.SaveChangesAsync();

                    if (attribute.AttributeValueCulture.Count > 0)
                    {
                        await AddAttributeValueDetails(attribute.AttributeValueCulture, attributeValue.Id);
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                throw ex;


            }
        }
        public async Task<bool> UpdateProductAttributeValue(List<GetProductValueVM> productAttributeVM)
        {
            try
            {
                var attributeValueIds = productAttributeVM.Select(item => item.AttributeValueId).ToList();

                var productAttributeValues = await _productValue.Query()
                    .Where(x => attributeValueIds.Contains(x.Id))
                    .Include(x => x.AttributeValueDetails)
                    .ToListAsync();

                foreach (var item in productAttributeVM)
                {
                    var productAttributeValue = productAttributeValues.FirstOrDefault(x => x.Id == item.AttributeValueId);

                    if (productAttributeValue != null)
                    {
                       // productAttributeValue.AttributeId = item.AttributeId;

                        foreach (var AttributeValuemodel in item.AttributeValueCulture)
                        {
                            foreach (var attributeValueDetails in productAttributeValue.AttributeValueDetails)
                            {
                                if (attributeValueDetails.CultureId == AttributeValuemodel.CultureId)
                                {
                                    attributeValueDetails.Value = AttributeValuemodel.Value;
                                }
                            }
                        }
                    }
                }

                await _productValue.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        public async Task<List<ProductAttributeGlobalVM>> GetProductAttributeValue(long ProductId)
        {
            try
            {

                var query = await _productValue.Query()
                            .Include(x => x.AttributeValueDetails)
                            .ThenInclude(x => x.Culture)
                            .Include(x => x.Attribute)
                            .ThenInclude(x => x.AttributeDetails)
                            .ThenInclude(x => x.Culture)
                            .Include(x => x.Product)
                            .ThenInclude(x => x.ProductDetails)
                            .ThenInclude(x => x.Culture)
                            .AsNoTrackingWithIdentityResolution()
                            .Where(x => x.ProductId == ProductId && x.Product.Status == EStatus.Published)
                            .ToListAsync();

                var result = query.Select(x => new ProductAttributeGlobalVM
                {
                    AttributeValue = x.AttributeValueDetails.Select(x => new ProductAttributeValueModel
                    {
                        CultureId = x.CultureId,
                        CultureName = x.Culture.CultureId,
                        ProductId = x.AttributeValue.ProductId,
                        Value = x.Value,
                        AttributeValueId = x.AttributeValueId
                    }).ToList(),
                    Attribute = x.Attribute.AttributeDetails.Select(f => new ProductAttributeModel
                    {
                        AttributeId = f.AttributeId,
                        CultureId = f.CultureId,
                        AttributeName = f.Name
                    }).ToList()


                }).ToList();




                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion


        #region Attribute
        public async Task<bool> AddProductAttribute(ProductAttribuateRequestViewModel model)
        {
            try
            {


                var SiteId = await _workContext.GetCurrentSiteIdAsync();




                var attributeobj = new ProductAttribute();


                attributeobj.GroupId = model.GroupId;
                attributeobj.IsVisible = model.IsVisible;
                attributeobj.SiteId = SiteId;
                attributeobj.Status = model.Status;


                _productAttribute.Add(attributeobj);


                await _productAttribute.SaveChangesAsync();

                var ProductAttributeDetails = new List<ProductAttributeDetails>();

                foreach (var item in model.productAttributeDetailsViewModels)
                {
                    ProductAttributeDetails.Add(new ProductAttributeDetails
                    {
                        CultureId = item.CultureId,
                        Name = item.Name,
                        AttributeId = attributeobj.Id

                    });
                }


                productAttributeDetailsRepository.AddRange(ProductAttributeDetails);

                await productAttributeDetailsRepository.SaveChangesAsync();




                return true;


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }
        }
        public async Task<bool> UpdateProductAttribute(ProductAttribuateRequestViewModel model)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();


                var attributeobj = await _productAttribute.Query().Include(x => x.AttributeDetails).Where(x => x.Id == model.Id).FirstOrDefaultAsync();


                attributeobj.GroupId = model.GroupId;
                attributeobj.IsVisible = model.IsVisible;
                attributeobj.SiteId = SiteId;
                attributeobj.Status = model.Status;


                foreach (var item in model.productAttributeDetailsViewModels)
                {

                    foreach (var attributeculture in attributeobj.AttributeDetails)
                    {
                        if (attributeculture.CultureId == item.CultureId)
                        {
                            attributeculture.Name = item.Name;


                        }

                    }


                }

                await _productAttribute.SaveChangesAsync();

                await productAttributeDetailsRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }
        public async Task<PaginationResult<ProductAttributeViewModel>> List(AttributeParams attributeParams)
        {
            var SiteId = _workContext.GetCurrentSiteId();
            var attributes = _productAttribute
                .Query()
                .Include(x => x.Group)
                .ThenInclude(x => x.productAttributeGroupDetails)
                .Include(x => x.AttributeDetails)
                .ThenInclude(x => x.Culture)
                .AsNoTrackingWithIdentityResolution()
                .Where(s => s.SiteId == SiteId && s.Status != EStatus.Deleted);


            var Result = await attributes.
                            Where(x => string.IsNullOrEmpty(attributeParams.SearchText)
                             || x.AttributeDetails.Any(y => y.Name.ToLower().
                             Contains(attributeParams.SearchText.ToLower())))
                            .Select(x => new ProductAttributeViewModel
                            {
                                Id = x.Id,
                                AttributeDetails = x.AttributeDetails,
                                DefaultGroupName = x.Group == null ? "" : x.Group.productAttributeGroupDetails.OrderByDescending(x => x.CultureId).FirstOrDefault().Name,
                                GroupId = x.GroupId,
                                SiteId = SiteId,
                                IsVisible = x.IsVisible,
                                ProductTemplates = x.ProductTemplates,
                                Status = x.Status,

                            }).PaginateAsync(attributeParams.PageSize, attributeParams.PageNumber);





            return Result;
        }
        public async Task<List<ProductAttribuateResponseViewModel>> GetAttributeByTemplateId(long TemplateId)
        {
            try
            {
                var AttributeItem = await _producttempAttributeRepository
                    .Query()
                    .Where(x => x.ProductTemplateId == TemplateId)
                    .Include(x => x.ProductAttribute)
                    .ThenInclude(c => c.AttributeDetails)
                    .Select(x => new ProductAttribuateResponseViewModel
                    {
                        Id = x.ProductAttribute.Id,
                        Status = x.ProductAttribute.Status,
                        GroupId = x.ProductAttribute.GroupId,
                        IsVisible = x.ProductAttribute.IsVisible,
                        CultureId = x.ProductAttribute.AttributeDetails.OrderByDescending(x => x.CultureId).Select(x => x.CultureId).FirstOrDefault(),
                        Name = x.ProductAttribute.AttributeDetails.OrderByDescending(x => x.CultureId).Select(x => x.Name).FirstOrDefault()
                    }).ToListAsync();
                return AttributeItem;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<ProductAttributeViewModel> GetAttributeById(long Id)
        {
            var SiteId = _workContext.GetCurrentSiteId();

            var productAttribute = await _productAttribute.Query()
                .Include(x => x.Group)
                .Include(x => x.AttributeDetails)
                .ThenInclude(x => x.Culture)
                .Where(s => s.SiteId == SiteId && s.Status != EStatus.Deleted && Id == s.Id)

                .Select(x => new ProductAttributeViewModel
                {
                    Id = x.Id,
                    AttributeDetails = x.AttributeDetails,
                    Group = x.Group,
                    SiteId = SiteId,
                    IsVisible = x.IsVisible,
                    ProductTemplates = x.ProductTemplates,
                    Status = x.Status,
                    GroupId = x.GroupId

                }).FirstOrDefaultAsync();


            return productAttribute;
        }
        public async Task<bool> ChangeProductAttributeStatus(long Id, EStatus status)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var dbTrans = await _context.Database.BeginTransactionAsync();
                try
                {
                    var productAttribute = await _productAttribute.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();

                    productAttribute.Status = status;

                    await _productAttribute.SaveChangesAsync();

                    await dbTrans.CommitAsync();


                    return true;

                }
                catch (Exception ex)
                {
                    await dbTrans.RollbackAsync();
                    throw ex;
                }
            });
        }
        #endregion


        #region Helpers 

        private async Task AddAttributeValueDetails(List<AttributeValueCultureViewModel> attributeValueCulture, long id)
        {
            try
            {
                var productAttributeValueDetails = new List<ProductAttributeValueDetails>();

                foreach (var item in attributeValueCulture)
                {
                    productAttributeValueDetails.Add(new ProductAttributeValueDetails
                    {
                        CultureId = item.CultureId,
                        AttributeValueId = id,
                        Value = item.Value
                    });
                }

                productAttributeValueDetailsRepository.AddRange(productAttributeValueDetails);

                await productAttributeValueDetailsRepository.SaveChangesAsync();

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        #endregion


    }
}

