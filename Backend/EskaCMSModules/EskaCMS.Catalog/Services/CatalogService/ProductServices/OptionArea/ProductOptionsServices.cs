using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Data;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;

using EskaCMS.Entities.Entity;
using EskaCommerce.Catalog.Areas.Catalog.Controllers;
using EskaCMS.DataAccess.Data;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.OptionArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.PricingProductArea.Interfaces;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.OptionArea
{
    public class ProductOptionsServices : IProductOptionsServices
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductOption> _productOptionsRepository;
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<ProductOptionValue> _productoptionsValueRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepository<ProductLink> _productLinkRepository;
        private readonly IRepository<ProductOptionDetails> productOptionDetails;
        private readonly IRepository<ProductOptionValueDetails> productOptionValueDetails;
        private readonly IRepository<ProductDetails> productDetailsRepository;
        private readonly IRepository<ProductOptionCombination> _productCombinationRepository;
        private readonly IRepository<ProductPriceHistory> _productPriceRepository;
        private readonly ISlugServices _slugServices;
        private readonly IProductServices _productService;
        private readonly IProductPricingService _productPricingService;


        public ProductOptionsServices(IRepository<Product> productRepository,
            IRepository<ProductOption> productOptionsRepository,
            EskaDCMSDbContext context, IRepository<ProductOptionValue> productoptionsRepository,
            IWorkContext workContext, ISlugServices slugServices
            , IRepository<ProductLink> productLinkRepository,
            IRepository<ProductOptionDetails> productOptionDetails,
            IRepository<ProductOptionValueDetails> ProductOptionValueDetails,
            IRepository<ProductDetails> ProductDetailsRepository,
            IRepository<ProductOptionCombination> productCombinationRepository,
            IRepository<ProductPriceHistory> productPriceRepository,
            IProductServices productService, IProductPricingService productPricingService)
        {
            _productRepository = productRepository;
            _productOptionsRepository = productOptionsRepository;
            _context = context;
            _productoptionsValueRepository = productoptionsRepository;
            _workContext = workContext;
            _productLinkRepository = productLinkRepository;
            this.productOptionDetails = productOptionDetails;
            productOptionValueDetails = ProductOptionValueDetails;
            productDetailsRepository = ProductDetailsRepository;
            _productCombinationRepository = productCombinationRepository;
            _productPriceRepository = productPriceRepository;
            _slugServices = slugServices;
            _productService = productService;
            _productPriceRepository = productPriceRepository;
            _productPricingService = productPricingService;

        }


        public async Task<List<OptionViewModel>> GetOptions()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();


                var Options = await _productOptionsRepository.Query().Where(x => x.SiteId == SiteId)
               .Select(x => new OptionViewModel
               {

                   Id = x.Id,
                   SiteId = x.SiteId,
                   optionDetails = x.optionDetails.Select(x => new OptionDetailsViewModel
                   {
                       CultureCode = x.Culture.CultureId,
                       Name = x.Name,
                       CultureId = x.CultureId,
                       Id = x.Id
                   }).ToList()
               }).ToListAsync();
                return Options;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<OptionViewModel> GetOptionsById(long id)
        {
            try
            {
                var Option = await _productOptionsRepository.Query().Include(d => d.optionDetails).ThenInclude(e => e.Culture).Where(x => x.Id == id).Select(x => new OptionViewModel
                {

                    Id = x.Id,
                    SiteId = x.SiteId,
                    optionDetails = x.optionDetails.Select(x => new OptionDetailsViewModel
                    {
                        CultureCode = x.Culture.CultureId,
                        Name = x.Name,
                        CultureId = x.CultureId,
                        Id = x.Id
                    }).ToList()
                }).FirstOrDefaultAsync();
                return Option;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<bool> AddOptions(List<OptionViewModel> optionsModels)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var SiteId = await _workContext.GetCurrentSiteIdAsync();
                    foreach (var item in optionsModels)
                    {
                        var productOption = new ProductOption
                        {

                            SiteId = SiteId
                        };
                        _productOptionsRepository.Add(productOption);
                        await _productOptionsRepository.SaveChangesAsync();
                        var List = new List<ProductOptionDetails>();
                        foreach (var opt in item.optionDetails)
                        {
                            var name = await _productOptionsRepository.Query()
                                                                 .Include(x => x.optionDetails)
                                                                 .Where(x => x.optionDetails.Any(p => opt.Name == p.Name) && x.SiteId == SiteId).FirstOrDefaultAsync();
                            if (name != null)
                            {
                                throw new Exception("Name Is Already Exist");
                            }
                            var one = new ProductOptionDetails();
                            one.Name = opt.Name;
                            one.CultureId = opt.CultureId;
                            one.ProductOptionId = productOption.Id;
                            List.Add(one);



                        }
                        productOptionDetails.AddRange(List);
                        await productDetailsRepository.SaveChangesAsync();

                    }


                    await transaction.CommitAsync();

                    return true;

                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception(ex.Message);
                }
            });
        }


        public async Task<bool> DeleteOptions(long id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var Option = await _productOptionsRepository.Query().Where(x => x.Id == id).FirstOrDefaultAsync();
                    if (Option == null)
                    {
                        throw new Exception("ProductOptions Not Exist");
                    }
                    else
                    {
                        // First Remove OptionDetails

                        var OptionDetails = await productOptionDetails.Query().Where(x => x.ProductOptionId == id).ToListAsync();
                        if (OptionDetails != null)
                        {
                            productOptionDetails.RemoveRange(OptionDetails);
                            await productDetailsRepository.SaveChangesAsync();
                        }

                        // End


                        // Second Remove OptionCompination
                        var OptionCompination = await _productCombinationRepository.Query().Where(x => x.OptionId == id).ToListAsync();
                        if (OptionCompination != null)
                        {
                            _productCombinationRepository.RemoveRange(OptionCompination);
                            await _productCombinationRepository.SaveChangesAsync();
                        }



                        //End


                        // OptionValues And Details
                        var OptionValues = await _productoptionsValueRepository.Query().Where(x => x.OptionId == id).ToListAsync();
                        if (OptionCompination != null)
                        {
                            if (OptionValues.Count > 0)
                            {
                                var SelectIds = OptionValues.Select(x => x.Id).ToList();
                                var OptionValueDetails = await productOptionValueDetails.Query().Where(x => SelectIds.Contains(x.OptionValueId)).ToListAsync();
                                productOptionValueDetails.RemoveRange(OptionValueDetails);
                                await productOptionValueDetails.SaveChangesAsync();

                                _productoptionsValueRepository.RemoveRange(OptionValues);
                                await _productoptionsValueRepository.SaveChangesAsync();
                            }
                        }



                        //End

                        _productOptionsRepository.Remove(Option);
                        await _productOptionsRepository.SaveChangesAsync();

                    }
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    throw new Exception(ex.Message);
                }
            });
        }

        public async Task<List<ProductOptionWithOptionValueViewModel>> GetOptionsForComponent()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                var Query = await _productOptionsRepository.Query()
                                                   .Include(x => x.productOptionValues)
                                                   .ThenInclude(x => x.productOptionValueDetails)
                                                   .Include(x => x.optionDetails)
                                                   .Where(x => x.SiteId == SiteId)
                                                   .Select(x => new ProductOptionWithOptionValueViewModel
                                                   {

                                                       optionId = x.Id,
                                                       productOptionNames = x.optionDetails.Select(x => new ProductOptionNameViewModel
                                                       {

                                                           CultureId = x.CultureId,
                                                           Name = x.Name

                                                       }).ToList(),
                                                       productOptionValues = x.productOptionValues.SelectMany(x => x.productOptionValueDetails).Select(x => new productOptionValueViewModel
                                                       {

                                                           CultureId = x.CultureId,
                                                           Value = x.Value,
                                                           ValueId = x.OptionValueId
                                                       }).ToList()
                                                   }).ToListAsync();

                return Query;


            }
            catch (Exception ex)
            {

                throw ex;
            }


        }

        public async Task<OptionViewModel> GetOptionsByName(string Name)
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();

                var Option = await _productOptionsRepository.Query().Include(d => d.optionDetails).ThenInclude(e => e.Culture).Where(x => x.optionDetails.Any(i => i.Name == Name) && x.SiteId == SiteId).Select(x => new OptionViewModel
                {

                    Id = x.Id,
                    SiteId = x.SiteId,
                    optionDetails = x.optionDetails.Select(x => new OptionDetailsViewModel
                    {
                        CultureCode = x.Culture.CultureId,
                        Name = x.Name,
                        CultureId = x.CultureId,
                        Id = x.Id
                    }).ToList()
                }).FirstOrDefaultAsync();
                return Option;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> UpdateOptions(List<OptionViewModel> optionsModels)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var SiteId = await _workContext.GetCurrentSiteIdAsync();

                    foreach (var item in optionsModels)
                    {
                        var optionObj = await _productOptionsRepository.Query().Include(p => p.optionDetails).ThenInclude(p => p.Culture).Where(o => o.Id == item.Id).FirstOrDefaultAsync();


                        foreach (var opt in item.optionDetails)
                        {
                            foreach (var pro in optionObj.optionDetails)
                            {
                                if (opt.CultureId == pro.CultureId)
                                {
                                    var name = await _productOptionsRepository.Query()
                                                               .Include(x => x.optionDetails)
                                                               .Where(x => x.optionDetails.Any(p => opt.Name == p.Name)).FirstOrDefaultAsync();
                                    if (name != null)
                                    {
                                        throw new Exception("Dublicate Entry");
                                    }
                                    pro.Name = opt.Name;
                                }
                            }




                        }
                        await _productOptionsRepository.SaveChangesAsync();


                    }
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

        #region OptionValues
        public async Task<List<long>> AddProductsOptionsValue(List<ProductOptionViewModel> models)
        {
            try
            {
                var optionIndex = 0;

                //from option  value table
                var optionValue = new ProductOptionValue();
                var ListOfValueId = new List<long>();
                foreach (var model in models)
                {

                    var ProductOptionValueobj = new ProductOptionValue
                    {
                        OptionId = model.OptionId,
                        DisplayType = model.DisplayType,
                        SortIndex = optionIndex,

                    };

                    _productoptionsValueRepository.Add(ProductOptionValueobj);
                    await _productoptionsValueRepository.SaveChangesAsync();

                    ListOfValueId.Add(ProductOptionValueobj.Id);
                    await AddProductOptionValueDetails(model.Values, ProductOptionValueobj.Id);

                    optionIndex++;

                }

                return ListOfValueId;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<bool> UpdateProductOptionsValueByValueId(List<ValueItemForAdmin> model)
        {
            try
            {

                var allOptionValueIds = model.Select(x => x.ValueId).ToList();

                var optionValues = await productOptionValueDetails.Query()
                                                                  .Where(x => allOptionValueIds.Contains(x.OptionValueId))
                                                                  .ToListAsync();


                foreach(var item in optionValues)
                {
                    var updatedValue = model.Where(x => x.ValueId == item.OptionValueId).SelectMany(x => x.ValueDetails).Where(x => x.CultureId == item.CultureId).FirstOrDefault();

                    if(updatedValue is not null && !string.IsNullOrEmpty(updatedValue.Value))
                    {
                        item.Value = updatedValue.Value;
                    }
                    
                }

                await productOptionValueDetails.SaveChangesAsync();

                return true;



            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public async Task<ItemDetailOptionForAdmin> GetProductOptionsValueByOptionId(long OptionId)
        {
            try
            {
                var Options = await _productOptionsRepository
                                                       .Query()
                                                       .Where(x => x.Id == OptionId)
                                                       .Include(x => x.optionDetails)
                                                       .Include(x => x.productOptionValues)
                                                       .ThenInclude(x => x.productOptionValueDetails)
                                                       .Select(x => new ItemDetailOptionForAdmin
                                                       {
                                                           OptionId = x.Id,
                                                           OptionDetails = x.optionDetails.Select(x => new OptionDetailForAdmin
                                                           {

                                                               CultureId = x.CultureId,
                                                               Name = x.Name,

                                                           }).ToList(),
                                                           Values = x.productOptionValues.Select(x => x)
                                                 .Select(x => new ValueItemForAdmin
                                                 {
                                                     ValueId = x.Id,
                                                     ValueDetails = x.productOptionValueDetails.Select(x => new ValueDetailForAdmin { Value = x.Value, CultureId = x.CultureId }).ToList()
                                                 }).ToList()

                                                       }).FirstOrDefaultAsync();
                return Options;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public async Task<List<ItemDetailOptionForAdmin>> GetProductOptionsDetailsWithOrdering(long Id)
        {
            try
            {

                var SiteId = _workContext.GetCurrentSiteId();



                var optionsWithDistinctValues = await _productCombinationRepository.Query()
                        .Where(x => x.ParentProductId == Id && x.ParentProduct.SiteId == SiteId)
                  .Include(x => x.Option)
                      .ThenInclude(x => x.optionDetails)
                  .Include(x => x.Value)
                      .ThenInclude(x => x.productOptionValueDetails)
                  .Select(x => new
                  {
                      OptionId = x.Option.Id,
                      OptionDetails = x.Option.optionDetails,
                      x.ValueId,
                      ValueDetails = x.Value.productOptionValueDetails,
                      x.SortIndex
                  })
                  .OrderBy(x => x.SortIndex)
                  .ToListAsync();

                var result = optionsWithDistinctValues
                    .GroupBy(x => x.OptionId)
                    .Select(group => new ItemDetailOptionForAdmin
                    {
                        OptionId = group.Key,
                        OptionDetails = group.First().OptionDetails.Select(x => new OptionDetailForAdmin
                        {

                            CultureId = x.CultureId,
                            Name = x.Name
                        }).ToList(),
                        Values = group.DistinctBy(x => x.ValueId).OrderBy(x => x.SortIndex).Select(x => x).Select(x => new ValueItemForAdmin { ValueId = x.ValueId, ValueDetails = x.ValueDetails.Select(x => new ValueDetailForAdmin { CultureId = x.CultureId, Value = x.Value }).ToList() }).ToList()
                    }).ToList();



                return result;







            }
            catch (Exception ex)
            {

                throw ex;
            }
        }




        private async Task<bool> AddProductOptionValueDetails(List<ProductoptionViewModelDetails> details, long Id)
        {
            try
            {
                List<ProductOptionValueDetails> ProductOptionValueDetailsObj = new();
                foreach (var detail in details)
                {
                    var ProductOptionValueDetails = new ProductOptionValueDetails();

                    ProductOptionValueDetails.CultureId = detail.CultureId;
                    ProductOptionValueDetails.Value = detail.Value;
                    ProductOptionValueDetails.OptionValueId = Id;

                    ProductOptionValueDetailsObj.Add(ProductOptionValueDetails);
                }

                productOptionValueDetails.AddRange(ProductOptionValueDetailsObj);

                await _productoptionsValueRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }




        #endregion;
    }
}

