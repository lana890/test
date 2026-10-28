using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.ShippingTableRate;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.ShippingTableRate.Areas.ShippingTableRate.ViewModels;
using EskaCommerce.ShippingTableRate.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.ShippingTableRate.Services
{
    public class ShippingTableRateServices : IShippingTableRateServices
    {
        private readonly IRepository<PriceAndDestination> priceAndDestinationRepository;
        private readonly IWorkContext workContext;

        public ShippingTableRateServices(IRepository<PriceAndDestination> priceAndDestinationRepository,IWorkContext workContext)
        {
            this.priceAndDestinationRepository = priceAndDestinationRepository;
            this.workContext = workContext;
        }
        public async Task<object> UpsertRate(List<RequestRateAndDistination> model)
        {
            try
            {

            if (model.Where(x => x.Id == 0).Count() > 0)
            {
                await CreateRate(model.Where(x => x.Id == 0).ToList());
            }
            if (model.Where(x => x.Id != 0).Count() > 0)
            {
                await EditRate(model.Where(x => x.Id != 0).ToList());
            }

            return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<bool> CreateRate(List<RequestRateAndDistination> model)
        {
            try
            {
                var priceAndDestination = model.ConvertAll(o=> new PriceAndDestination
                {
                    CountryId = o.CountryId,
                    StateOrProvinceId = o.StateOrProvinceId,
                    DistrictId = o.DistrictId,
                    ZipCode = o.ZipCode,
                    MinOrderSubtotal = o.MinOrderSubtotal,
                    ShippingPrice = o.ShippingPrice,
                    Note = o.Note,
                    NoOfItems = o.NoOfItems,
                    WarehouseId = o.WarehouseId,
                    SiteId = workContext.GetCurrentSiteId(),
                    EstimatedDeliveryDate = o.EstimatedDeliveryDate,    
                });

                priceAndDestinationRepository.AddRange(priceAndDestination);
                await priceAndDestinationRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> EditRate(List< RequestRateAndDistination> model)
        {
            try
            {
                var Ids = model.Select(x => x.Id).ToList();
                    var priceAndDestinationObjs = await priceAndDestinationRepository.Query().Where(x => Ids.Any(l=>l== x.Id)).ToListAsync();
                    if (priceAndDestinationObjs.Count()==0)
                    {
                       throw new Exception("Shipping Rate Not Exist");
                    }
                foreach (var priceAndDestinationObj in priceAndDestinationObjs)
                {
                    var RateModelObj = model.Where(x => x.Id == priceAndDestinationObj.Id).FirstOrDefault();

                    priceAndDestinationObj.CountryId = RateModelObj.CountryId;
                    priceAndDestinationObj.StateOrProvinceId = RateModelObj.StateOrProvinceId;
                    priceAndDestinationObj.DistrictId = RateModelObj.DistrictId;
                    priceAndDestinationObj.ZipCode = RateModelObj.ZipCode;
                    priceAndDestinationObj.ShippingPrice = RateModelObj.ShippingPrice;
                    priceAndDestinationObj.MinOrderSubtotal = RateModelObj.MinOrderSubtotal;
                    priceAndDestinationObj.Note = RateModelObj.Note;
                    priceAndDestinationObj.NoOfItems = RateModelObj.NoOfItems;
                    priceAndDestinationObj.EstimatedDeliveryDate = RateModelObj.EstimatedDeliveryDate;
                    priceAndDestinationObj.WarehouseId = RateModelObj.WarehouseId;
                }
                 await priceAndDestinationRepository.SaveChangesAsync();
                return true;
      
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async  Task<PaginationResult<PriceAndDestinationForm>> GetAll( SearchPriceAndDistinationViewModel model)
        {
            try
            {
                var query = await priceAndDestinationRepository.Query()
                     .AsNoTracking()
                     .Where(x=>x.Status != EStatus.Deleted && x.Status != EStatus.Unpublished)
                     .Include(x => x.Country)
                     .ThenInclude(x=>x.CountryDetails)
                     .Include(x => x.District)
                     .ThenInclude(x=>x.DistrictDetails)
                     .Where(x=> string.IsNullOrEmpty(model.SearchText) || x.District.DistrictDetails.Any(o=>o.Name.Contains(model.SearchText)))
                     .Include(x => x.StateOrProvince)
                     .ThenInclude(x=>x.StateOrProvinceDetails)
                     .Include(x => x.Warehouse)
                     .Include("Warehouse.Address")
                     .Include("Warehouse.Address.Country")
                     .Include("Warehouse.Address.Country.CountryDetails")
                     .Include("Warehouse.Address.StateOrProvince")
                     .Include("Warehouse.Address.StateOrProvince.StateOrProvinceDetails")
                     .Include("Warehouse.Address.District")
                     .Include("Warehouse.Address.District.DistrictDetails")
                     .Select(x => new PriceAndDestinationForm
                     {
                         Id = x.Id,
                         CountryId = x.CountryId,
                         CountryName = x.Country == null ? null : x.Country.CountryDetails.Select(x=>x.Name).FirstOrDefault(),
                         StateOrProvinceId = x.StateOrProvinceId,
                         StateOrProvinceName = x.StateOrProvince == null ? null : x.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                         DistrictId = x.DistrictId,
                         DistrictName = x.District == null ? null : x.District.DistrictDetails.Select(x => x.Name).FirstOrDefault(),
                         ZipCode = x.ZipCode,
                         MinOrderSubtotal = x.MinOrderSubtotal,
                         ShippingPrice = x.ShippingPrice,
                         Note = x.Note,
                         NoOfItems = x.NoOfItems,
                         Status = x.Status,
                         EstimatedDeliveryDate=x.EstimatedDeliveryDate,
                         WareHouseId=x.WarehouseId,
                         WareHouse = x.Warehouse == null ? new WareHouseDTO() : new WareHouseDTO
                         {

                             Id=x.Warehouse.Id,
                             CountryId = x.Warehouse.Address.CountryId,
                             CountryName = x.Warehouse.Address.Country == null ? null : x.Warehouse.Address.Country.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                             StateOrProvinceId = x.Warehouse.Address.StateOrProvinceId,
                             StateOrProvinceName = x.Warehouse.Address.StateOrProvince == null ? null : x.Warehouse.Address.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                             DistrictId = x.Warehouse.Address.DistrictId,
                             DistrictName = x.Warehouse.Address.District == null ? null : x.Warehouse.Address.District.DistrictDetails.Select(x => x.Name).FirstOrDefault(),
                             ZipCode = x.Warehouse.Address.ZipCode

                         }
                     }).PaginateAsync(model.PageSize,model.PageNumber);

                return query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<PriceAndDestinationForm> GetById(long id)
        {
            try
            {
                var query = await priceAndDestinationRepository
                                                       .Query()
                                                     .Where(x => x.Status != EStatus.Deleted && x.Status != EStatus.Unpublished)
                                                     .Include(x => x.Country)
                                                     .ThenInclude(x => x.CountryDetails)
                                                     .Include(x => x.District)
                                                     .ThenInclude(x => x.DistrictDetails)
                                                     .Include(x => x.StateOrProvince)
                                                     .ThenInclude(x => x.StateOrProvinceDetails)
                                                     .Include(x => x.Warehouse)
                                                     .Include("Warehouse.Address")
                                                     .Include("Warehouse.Address.Country")
                                                     .Include("Warehouse.Address.Country.CountryDetails")
                                                     .Include("Warehouse.Address.StateOrProvince")
                                                     .Include("Warehouse.Address.StateOrProvince.StateOrProvinceDetails")
                                                     .Include("Warehouse.Address.District")
                                                     .Include("Warehouse.Address.District.DistrictDetails")
                                                       .AsNoTrackingWithIdentityResolution()
                                                       .Where(x => x.Id == id).Select(x => new PriceAndDestinationForm
                                                       {
                                                           Id = x.Id,
                                                           CountryId = x.CountryId,
                                                           CountryName = x.Country.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                                                           StateOrProvinceId = x.StateOrProvinceId,
                                                           StateOrProvinceName = x.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                                                           DistrictId = x.DistrictId,
                                                           DistrictName = x.District.DistrictDetails.Select(x => x.Name).FirstOrDefault(),
                                                           ZipCode = x.ZipCode,
                                                           Status = x.Status,
                                                           MinOrderSubtotal = x.MinOrderSubtotal,
                                                           ShippingPrice = x.ShippingPrice,
                                                           Note = x.Note,
                                                           NoOfItems = x.NoOfItems,
                                                           EstimatedDeliveryDate = x.EstimatedDeliveryDate,
                                                           WareHouseId = x.WarehouseId,
                                                           WareHouse = x.Warehouse == null ? new WareHouseDTO() : new WareHouseDTO
                                                           {

                                                               Id=x.Warehouse.Id,
                                                               CountryId = x.Warehouse.Address.CountryId,
                                                               CountryName = x.Warehouse.Address.Country == null ? null : x.Warehouse.Address.Country.CountryDetails.Select(x => x.Name).FirstOrDefault(),
                                                               StateOrProvinceId = x.Warehouse.Address.StateOrProvinceId,
                                                               StateOrProvinceName = x.Warehouse.Address.StateOrProvince == null ? null : x.Warehouse.Address.StateOrProvince.StateOrProvinceDetails.Select(x => x.Name).FirstOrDefault(),
                                                               DistrictId = x.Warehouse.Address.DistrictId,
                                                               DistrictName = x.Warehouse.Address.District == null ? null : x.Warehouse.Address.District.DistrictDetails.Select(x => x.Name).FirstOrDefault(),
                                                               ZipCode = x.Warehouse.Address.ZipCode

                                                           }
                                                       }).FirstOrDefaultAsync();

                return query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<bool> ChangeStatus(long id, EStatus status)
        {
            try
            {
                var priceAndDestination = await priceAndDestinationRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (priceAndDestination == null)
                    throw new Exception("The Rate is not exist");

                var existingEntity = await priceAndDestinationRepository.Query()
                    .FirstOrDefaultAsync(x => x.Status == status
                                           && x.CountryId == priceAndDestination.CountryId
                                           && x.StateOrProvinceId == priceAndDestination.StateOrProvinceId
                                           && x.DistrictId == priceAndDestination.DistrictId);

                if (existingEntity != null)
                {
                    priceAndDestinationRepository.Remove(existingEntity);
                    await priceAndDestinationRepository.SaveChangesAsync();
                }

                priceAndDestination.Status = status;
                await priceAndDestinationRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw; 
            }
        }
        #region Helpers
        #endregion
    }
}
