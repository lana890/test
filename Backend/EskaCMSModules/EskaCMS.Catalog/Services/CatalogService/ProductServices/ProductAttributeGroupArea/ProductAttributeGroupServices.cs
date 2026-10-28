using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.ProductAttribute;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeGroupArea.Interfaces;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductAttributeGroupArea
{
    public class ProductAttributeGroupServices : IProductAttributeGroup
    {
        private readonly IWorkContext _workcontext;
        private readonly IRepository<ProductAttributeGroup> _productAttrGroupRepository;
        private readonly IRepository<ProductAttributeGroupDetails> productAttrGroupDetailsRepository;
        private readonly IRepository<ProductAttribute> _productAttrRepository;

        public ProductAttributeGroupServices(IWorkContext workcontext,
            IRepository<ProductAttributeGroup> productAttrGroupRepository,
            IRepository<ProductAttributeGroupDetails> productAttrGroupDetailsRepository,
            IRepository<ProductAttribute> productAttrRepository)
        {
            _workcontext = workcontext;
            _productAttrGroupRepository = productAttrGroupRepository;
            this.productAttrGroupDetailsRepository = productAttrGroupDetailsRepository;
            _productAttrRepository = productAttrRepository;
        }

        public async Task<bool> AddProductAttributeGroup(AtrtributeGroupViewModel model)
        {
            try
            {


                var SiteId = await _workcontext.GetCurrentSiteIdAsync();


                var groupAttribute = new ProductAttributeGroup();


                groupAttribute.SiteId = SiteId;
                groupAttribute.Status = model.Status;

                _productAttrGroupRepository.Add(groupAttribute);
                await _productAttrGroupRepository.SaveChangesAsync();

                var productAttributeGroupDetails = new List<ProductAttributeGroupDetails>();
                foreach (var item in model.atrtributeGroupDetails)
                {
                    productAttributeGroupDetails.Add(new ProductAttributeGroupDetails
                    {
                        CultureId = item.CultureId,
                        Name = item.Name,
                        ProductAttributeGroupId = groupAttribute.Id
                    });
                }


                productAttrGroupDetailsRepository.AddRange(productAttributeGroupDetails);

                await productAttrGroupDetailsRepository.SaveChangesAsync();





                return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> UpdateProductAttributeGroup(AtrtributeGroupViewModel model)
        {
            try
            {
                var SiteId = await _workcontext.GetCurrentSiteIdAsync();


                var groupAttribute = await _productAttrGroupRepository.Query().Include(x => x.productAttributeGroupDetails).Where(x => x.Id == model.Id).FirstOrDefaultAsync();

                groupAttribute.SiteId = SiteId;
                groupAttribute.Status = model.Status;





                foreach (var item in model.atrtributeGroupDetails)
                {

                    foreach (var attributeculture in groupAttribute.productAttributeGroupDetails)
                    {
                        if (attributeculture.CultureId == item.CultureId)
                        {
                            attributeculture.Name = item.Name;


                        }

                    }


                }

                await _productAttrGroupRepository.SaveChangesAsync();


                await productAttrGroupDetailsRepository.SaveChangesAsync();




                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }


        public async Task<PaginationResult<AtrtributeGroupViewModel>> GetProductAttributeGroups(AttributeParams model)
        {
            try
            {

                var SiteId = await _workcontext.GetCurrentSiteIdAsync();
                var attributeGroups = _productAttrGroupRepository
                    .Query()
                    .Include(x => x.productAttributeGroupDetails)
                    .ThenInclude(x => x.Culture)
                    .Where(s => s.SiteId == SiteId && s.Status != EStatus.Deleted);

                var Result = await attributeGroups.
                             Where(x => string.IsNullOrEmpty(model.SearchText)
                              || x.productAttributeGroupDetails.Any(y => y.Name.ToLower().
                              Contains(model.SearchText.ToLower())))
                             .Select(x => new AtrtributeGroupViewModel
                             {
                                 Id = x.Id,
                                 Status = x.Status,
                                 atrtributeGroupDetails = x.productAttributeGroupDetails.Select(y => new AtrtributeGroupDetailsViewModel
                                 {
                                     CultureId = y.CultureId,
                                     Id = y.Id,
                                     Name = y.Name,

                                 }).ToList()

                             }).PaginateAsync(model.PageSize, model.PageNumber);


                return Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<GetGroupsFlat>> GetProductAttributeGroupsFlat()
        {
            try
            {

                var SiteId = await _workcontext.GetCurrentSiteIdAsync();
                return await _productAttrGroupRepository
                    .Query()
                    .Where(s => s.SiteId == SiteId && s.Status != EStatus.Deleted)
                    .SelectMany(x => x.productAttributeGroupDetails)
                    .Select(x => new GetGroupsFlat()
                    {
                        CultureId = x.CultureId,
                        Id = x.ProductAttributeGroupId,
                        Name = x.Name
                    })
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();


            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<AtrtributeGroupViewModel> GetProductAttributeGroupById(long Id)
        {
            try
            {
                var productAttributeGroup = await _productAttrGroupRepository.Query()
                    .Where(x => x.Id == Id)
                     .Select(x => new AtrtributeGroupViewModel
                     {
                         Id = x.Id,
                         Status = x.Status,
                         atrtributeGroupDetails = x.productAttributeGroupDetails.Select(y => new AtrtributeGroupDetailsViewModel
                         {
                             CultureId = y.CultureId,
                             Id = y.Id,
                             Name = y.Name,

                         }).ToList()

                     }).FirstOrDefaultAsync();

                return productAttributeGroup;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        public async Task ChangeAttributeGroupStatus(long Id, EStatus NewStatus)
        {
            await _productAttrGroupRepository.ExecuteInTransactionAsync(async () =>
            {
                var Groups = await _productAttrGroupRepository.Query()
                    .Where(x => x.Id == Id)
                    .Include(x => x.Attributes)
                    .ToListAsync();

                foreach (var Group in Groups)
                {
                    if (Group.Attributes != null && Group.Attributes.Where(x => x.Status != EStatus.Deleted).Count() > 0 && NewStatus == EStatus.Deleted)
                    {
                        throw new Exception("Can not delete this group");
                    }

                    Group.Status = NewStatus;
                }
                await _productAttrGroupRepository.SaveChangesAsync();
            });


        }



        public async Task<List<AttributeWithGroupViewModel>> GetAttributeWithGroup(long CultureId)
        {
            try
            {
                var siteId =  _workcontext.GetCurrentSiteId();
                var Group = await _productAttrGroupRepository.Query()
                    .Include(x => x.productAttributeGroupDetails)
                    .Where(x => x.Status != EStatus.Deleted)
                    .Where(x => x.SiteId == siteId)
                    .Include(x => x.Attributes)
                    .Include("Attributes.AttributeDetails")
                    .Include("Attributes.Group")
                    .Select(x => new AttributeWithGroupViewModel
                    {


                        Id = x.Id,
                        Status = x.Status,
                        CultureId = x.productAttributeGroupDetails == null ? 0 : x.productAttributeGroupDetails.Where(x => x.CultureId == CultureId).FirstOrDefault() == null ? 0 : x.productAttributeGroupDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().CultureId,
                        Name = x.productAttributeGroupDetails == null ? null : x.productAttributeGroupDetails.Where(x => x.CultureId == CultureId).FirstOrDefault() == null ? null : x.productAttributeGroupDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,

                        productAttribute = x.Attributes == null ? new List<ProductAttraibuteDetailsGroup>() : x.Attributes.Select(a => new ProductAttraibuteDetailsGroup
                        {
                            Id = a.Id,
                            GroupId = a.Group == null ? 0 : a.Group.Id,
                            Status = a.Status,
                            IsVisible = a.IsVisible,
                            CultureId = CultureId,
                            Name = a.AttributeDetails == null ? null : a.AttributeDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                            DefaultGroupName = a.Group.productAttributeGroupDetails == null ? null : a.Group.productAttributeGroupDetails.Where(x => x.CultureId == CultureId).FirstOrDefault().Name,
                            SiteId = a.SiteId
                        }).ToList()
                    }).ToListAsync();








                return Group;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



    }
}
