using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductFeesArea.Interfaces;
namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductFeesArea
{
    public class ProductFeesService : IProductFeesService
    {
        private ICurrencyService _currencyService;
        private readonly IRepository<ProductAdditionalFees> _productadditionalfees;
        private readonly IWorkContext _workContext;
        public ProductFeesService(ICurrencyService currencyService, IRepository<ProductAdditionalFees> productadditionalfees, IWorkContext workContext)
        {
            _currencyService = currencyService;
            _productadditionalfees = productadditionalfees;
            _workContext = workContext;
        }

        public async Task<bool> UpsertProductFees(ProductFeesModel prodfees, long SiteId)
        {
            try
            {


                foreach (var item in prodfees.productFeesVMs)
                {
                    if (item.Id != 0)
                    {

                        await EditProductFees(item.Id, new ProductFeesVM
                        {
                            FeesType = item.FeesType,
                            FeesValue = item.FeesValue,
                            Name = item.Name,
                            Note = item.Note,
                            Refundable = item.Refundable


                        }, _workContext.GetCurrentSiteId());


                    }
                    else
                    {
                        ProductAdditionalFees objprodfees = new ProductAdditionalFees();

                        objprodfees.ProductId = prodfees.ProductId;
                        objprodfees.CreatedById = _workContext.GetCurrentUserIdValue();
                        objprodfees.FeeValue = item.FeesValue;
                        objprodfees.FeeType = item.FeesType;
                        objprodfees.CreationDate = DateTimeOffset.Now;
                        objprodfees.Name = item.Name;
                        objprodfees.Note = item.Note;
                        objprodfees.Refundable = item.Refundable;
                        objprodfees.Status = EStatus.Published;
                        _productadditionalfees.Add(objprodfees);
                        await _productadditionalfees.SaveChangesAsync();
                    }
                }

                await _productadditionalfees.SaveChangesAsync();


                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<long> EditProductFees(long FeesId, ProductFeesVM prodfees, long SiteId)
        {
            try
            {


                var prod = await _productadditionalfees.Query().Where(x => x.Id == FeesId).FirstOrDefaultAsync();

                prod.FeeValue = prodfees.FeesValue;
                prod.FeeType = prodfees.FeesType;
                prod.CreationDate = DateTimeOffset.Now;
                prod.Name = prodfees.Name;
                prod.Note = prodfees.Note;
                prod.Refundable = prodfees.Refundable;
                prod.CreatedById = await _workContext.GetCurrentUserId();

                _productadditionalfees.Update(prod);
                await _productadditionalfees.SaveChangesAsync();
                return prod.Id;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<List<ProductFeesVM>> GetAdditionalFeesByProductId(long ProductId)
        {
            try
            {

                return await _productadditionalfees.Query().Where(x => x.ProductId == ProductId && x.Status == EStatus.Published).Select(x => new ProductFeesVM
                {
                    FeesType = x.FeeType,
                    FeesValue = x.FeeValue,
                    Name = x.Name,
                    ProductId = x.ProductId,
                    Refundable = x.Refundable,
                    Note = x.Note,
                    Id = x.Id

                }).ToListAsync();


            }
            catch (Exception exc)
            {

                throw exc;
            }
        }
        public async Task<ProductFeesVM> GetAdditionalFeesById(long Id)
        {
            try
            {

                return await _productadditionalfees.Query().Where(x => x.Id == Id && x.Status == EStatus.Published).Select(x => new ProductFeesVM
                {
                    FeesType = x.FeeType,
                    FeesValue = x.FeeValue,
                    Name = x.Name,
                    ProductId = x.ProductId,
                    Refundable = x.Refundable,
                    Note = x.Note,
                    Id = x.Id

                }).FirstOrDefaultAsync();


            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

        public async Task<bool> DeleteProductFees(long Id)
        {
            try
            {
                var prod = await _productadditionalfees.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
                prod.Status = EStatus.Deleted;

                await _productadditionalfees.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)
            {

                throw exc;
            }
        }


    }
}
