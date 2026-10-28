using AutoMapper;
using AutoMapper.QueryableExtensions;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.LoyaltyPoints;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.LoyaltyPoints.Models.Request;
using EskaCommerce.LoyaltyPoints.Models.Response;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Entity.LoyaltyPoints.Enums.LoyaltyPointsEnums;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.LoyaltyPoints.Services
{
    public class LoyaltySetupService : ILoyaltySetupService
    {
        private readonly IRepository<LoyaltyPointsSetup> _loyaltySetupRepository;
        private readonly IRepository<LoyaltyPointsSetupHistory> _loyaltySetupHistoryRepository;
        private readonly IWorkContext _workContext;
        private readonly IMapper _mapper;
       
        public LoyaltySetupService(IRepository<LoyaltyPointsSetup> loyaltySetupRepository,
                                    IWorkContext workContext,
                                    IMapper mapper,
                                    IRepository<LoyaltyPointsSetupHistory> loyaltySetupHistoryRepository)
        {
            _loyaltySetupRepository = loyaltySetupRepository;
            _loyaltySetupHistoryRepository = loyaltySetupHistoryRepository;
            _workContext = workContext;
            _mapper = mapper;
        }

        public async Task<bool> CreateLoyaltySetup(LoyaltySetupDto loyaltySetupDto){

            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var userId = await  _workContext.GetCurrentUserId();
                var loyaltySetup = _mapper.Map<LoyaltyPointsSetup>(loyaltySetupDto);

                loyaltySetup.CreatedById = userId;
                loyaltySetup.Status = EStatus.Active;
                loyaltySetup.SiteId=siteId;

                var validateSetup = await ValidateLoyaltySetup(loyaltySetup,siteId);

                if (!validateSetup)
                    return false;
                _loyaltySetupRepository.Add(loyaltySetup);
                 _loyaltySetupRepository.SaveChanges();

                await AddLoyaltySetupHistory(loyaltySetup,userId);


                    return true;

            }
            catch (Exception e)
            {

                throw e;
            }
            
        }

        public async Task<bool> DeleteLoyaltySetup(long loyaltySetupId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();
                var loyaltySetup = _loyaltySetupRepository.Query()
                                                          .Where(x => x.SiteId == siteId && x.Id == loyaltySetupId)
                                                          .FirstOrDefault();
                if (loyaltySetup == null)
                    throw new Exception("Loyalty Setup Not Exist");

                loyaltySetup.Status = EStatus.Deleted;
                await _loyaltySetupRepository.SaveChangesAsync();

                await AddLoyaltySetupHistory(loyaltySetup, userId);

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }
            
        }


        public async Task<LoyaltySetupDetailsDto>LoyaltySetupDetails(long loyaltySetupId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var loyaltySetupInDb = _loyaltySetupRepository.Query()
                                                        .Where(x => x.SiteId == siteId &&
                                                                x.Id == loyaltySetupId &&
                                                                x.Status != EStatus.Deleted)
                                                        .Include(x => x.CreatedBy)
                                                        .Include(x => x.ModifiedBy)
                                                        .Include(x => x.Product)
                                                        .Include("Product.ProductDetails")
                                                        .Include(x => x.LoyaltySetupHistory)
                                                        .Include("LoyaltySetupHistory.Product")
                                                        .Include("LoyaltySetupHistory.Product.ProductDetails")
                                                        .Include("LoyaltySetupHistory.CreatedBy")
                                                        .FirstOrDefault();

                if (loyaltySetupInDb == null)
                {
                    throw new Exception("Loyalty Setup Not Exist");

                }


                var result = _mapper.Map<LoyaltySetupDetailsDto>(loyaltySetupInDb);

                return result;
            }
            catch (Exception e)
            {

                throw e;
            }
           
                                                    
                                                   
                                                     

        }
        public async Task<bool> UpdateLoyaltySetup(LoyaltySetupDto loyaltySetupDto, long loyaltySetupId)
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();

                var loyaltySetupInDb  = _loyaltySetupRepository.Query()
                                                      .Where(x => x.SiteId == siteId && x.Id == loyaltySetupId && x.Status != EStatus.Deleted)
                                                      .FirstOrDefault();

                if (loyaltySetupInDb == null)
                {
                    throw new Exception("Loyalty Setup Not Exist");

                }

                loyaltySetupInDb.FromPrice = loyaltySetupDto.FromPrice;
                loyaltySetupInDb.ToPrice = loyaltySetupDto.ToPrice;
                loyaltySetupInDb.LoyalitySource = loyaltySetupDto.LoyalitySource;
                loyaltySetupInDb.ProductId = loyaltySetupDto.ProductId;
                loyaltySetupInDb.StartDate = loyaltySetupDto.StartDate;
                loyaltySetupInDb.EndDate = loyaltySetupDto.EndDate;
                loyaltySetupInDb.PointAmount = loyaltySetupDto.PointAmount;
                loyaltySetupInDb.PointPerUnit = loyaltySetupDto.PointPerUnit;
                loyaltySetupInDb.ModificationDate = DateTimeOffset.Now;
                loyaltySetupInDb.ModifiedById = userId;

                var validateSetup = await ValidateLoyaltySetup(loyaltySetupInDb, siteId);

                if (!validateSetup)
                    return false;

                await _loyaltySetupRepository.SaveChangesAsync();

                await AddLoyaltySetupHistory(loyaltySetupInDb, userId);

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

        }

        public async Task<bool> AddLoyaltySetupHistory(LoyaltyPointsSetup loyaltySetup , long userId)
        {
            try
            {
                var loyaltySetuphistory = _mapper.Map<LoyaltyPointsSetupHistory>(loyaltySetup);
                loyaltySetuphistory.CreatedById = userId;
                _loyaltySetupHistoryRepository.Add(loyaltySetuphistory);
                await _loyaltySetupHistoryRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception e)
            {

                throw e;
            }

            

        }

        public async Task<bool> DeactivateLoyaltySetup(long loyaltySetupId)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();
            var loyaltySetup = _loyaltySetupRepository.Query()
                                                      .Where(x => x.SiteId == siteId && x.Id == loyaltySetupId && x.Status != EStatus.Deleted)
                                                      .FirstOrDefault();
            if (loyaltySetup == null)
                throw new Exception("Loyalty Setup Not Exist");

            loyaltySetup.Status = EStatus.Inactive;
            await _loyaltySetupRepository.SaveChangesAsync();

            await AddLoyaltySetupHistory(loyaltySetup, userId);

            return true;
        }

        public async Task<bool> ActivateLoyaltySetup(long loyaltySetupId)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();
            var loyaltySetup = _loyaltySetupRepository.Query()
                                                      .Where(x => x.SiteId == siteId && x.Id == loyaltySetupId && x.Status != EStatus.Deleted)
                                                      .FirstOrDefault();
            if (loyaltySetup == null)
                throw new Exception("Loyalty Setup Not Exist");

            var validateSetup = await ValidateLoyaltySetup(loyaltySetup, siteId);

            if (!validateSetup)
                return false;

            loyaltySetup.Status = EStatus.Active;
            await _loyaltySetupRepository.SaveChangesAsync();

            await AddLoyaltySetupHistory(loyaltySetup, userId);

            return true;
        }

        public async Task<bool> ValidateLoyaltySetup(LoyaltyPointsSetup loyaltySetup ,long siteId)
        {
            try
            {
                var allloyaltySetups = _loyaltySetupRepository.Query().AsNoTracking()
                                                      .Where(x => x.SiteId == siteId && x.Status == EStatus.Active)
                                                      .ToList();

                if (loyaltySetup.StartDate >= loyaltySetup.EndDate)
                    throw new Exception("Please add valid date for this loyalty setup");

                if (loyaltySetup.LoyalitySource == ELoyalitySource.SpecificProduct && !loyaltySetup.ProductId.HasValue)
                    throw new Exception("Please select product for this loyalty setup");

                if (loyaltySetup.LoyalitySource == ELoyalitySource.ConsumeRange && loyaltySetup.ToPrice < loyaltySetup.FromPrice)
                    throw new Exception("Please Add valid range for this loyalty setup");
                if (loyaltySetup.LoyalitySource == ELoyalitySource.ConsumeRange && (loyaltySetup.FromPrice < 0 || loyaltySetup.ToPrice < 0))
                    throw new Exception("Please Add valid range for this loyalty setup");

                if (loyaltySetup.LoyalitySource == ELoyalitySource.ConsumeRate && loyaltySetup.PointPerUnit <= 0)
                    throw new Exception("Please Add valid value for Point Per Unit");

                if (loyaltySetup.LoyalitySource != ELoyalitySource.ConsumeRate && loyaltySetup.PointAmount <= 0)
                    throw new Exception("Please Add valid point amount for this loyalty setup");

                if (loyaltySetup.LoyalitySource != ELoyalitySource.SpecificProduct &&  allloyaltySetups.Any(x =>x.Id != loyaltySetup.Id && x.LoyalitySource == loyaltySetup.LoyalitySource && 
                                                        (x.StartDate <= loyaltySetup.EndDate && x.EndDate >= loyaltySetup.StartDate)))
                {
                    throw new Exception("Conflicting loyalty setup detected because another setup is active. Please adjust the date ranges or activate only one setup for a specific period.");
                }

                if (loyaltySetup.LoyalitySource == ELoyalitySource.SpecificProduct && allloyaltySetups.Any(x => x.LoyalitySource == loyaltySetup.LoyalitySource &&
                                                       (x.StartDate <= loyaltySetup.EndDate && x.EndDate >= loyaltySetup.StartDate) && loyaltySetup.ProductId == x.ProductId))
                {
                    throw new Exception("Conflicting loyalty setup detected because another setup is active for the product.activate only one setup for the same product.");
                }

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }
            
            
        }

        public async Task<PaginationResult<LoyaltySetupDetailsDto>> GetAllLoyaltySetups(SmartTableParam param)
        {
            try
            {


                var siteId = _workContext.GetCurrentSiteId();
                var query = _loyaltySetupRepository.Query()
                                                        .Where(x => x.SiteId == siteId &&
                                                                x.Status != EStatus.Deleted)
                                                        .AsNoTrackingWithIdentityResolution();





                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.From != null)
                    {
                        DateTimeOffset From = search.From;
                        query = query.Where(x => x.StartDate >= From);
                    }


                    if (search.To != null)
                    {
                        DateTimeOffset To = search.To;
                        query = query.Where(x => x.EndDate <= To);
                    }


                    if (search.Status != null)
                    {
                        var status = (EStatus)search.Status;
                        query = query.Where(x => x.Status == status);
                    }
                    
                    if (search.LoyalitySource != null)
                    {
                        var LoyalitySource = (ELoyalitySource)search.LoyalitySource;
                        query = query.Where(x => x.LoyalitySource == LoyalitySource);
                    }

                   
                }

                var result = await query.OrderByDescending(x=>x.Id).ProjectTo<LoyaltySetupDetailsDto>(_mapper.ConfigurationProvider)

                    .PaginateAsync(param.Pagination.Number, param.Pagination.Start);




                return result;


            }
            catch (Exception exc)
            {

                throw exc;
            }
        }

    }
}
