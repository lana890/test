using AutoMapper;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.LoyaltyPoints;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.LoyaltyPoints.Models.Request;
using EskaCommerce.LoyaltyPoints.Models.Response;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Services
{
    public class PointsConversionService : IPointsConversionService
    {
        private readonly IRepository<PointsConversionConfig> _pointsConversionRepository;
        private readonly IWorkContext _workContext;
        private readonly IMapper _mapper;
        public PointsConversionService(IRepository<PointsConversionConfig> pointsConversionRepository,
                                           IWorkContext workContext,
                                          IMapper mapper)
        {
            _pointsConversionRepository = pointsConversionRepository;   
            _workContext = workContext;
            _mapper = mapper;

        }

        public async Task<bool> CreatePointsConversion(PointsConversionDto pointsConversionDto) {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();
                var userId = await _workContext.GetCurrentUserId();

                var pointsConversionConfig = new PointsConversionConfig
                {
                    PointsValue = pointsConversionDto.PointsValue,
                    CreatedById = userId,
                    SiteId = siteId,

                };

                if (_pointsConversionRepository.Query().Any(x => x.SiteId == siteId))
                    throw new Exception("This site Already Has Points Conversion Config");



                _pointsConversionRepository.Add(pointsConversionConfig);
                await _pointsConversionRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception e)
            {

                throw;
            }

           

        }

        public async Task<bool> UpdatePointsConversion(PointsConversionDto pointsConversionDto)
        {

            var siteId = _workContext.GetCurrentSiteId();
            var userId = await _workContext.GetCurrentUserId();

            var pointsConversionConfig = _pointsConversionRepository.Query()
                                                                    .Where(x=>x.SiteId == siteId)
                                                                    .FirstOrDefault();

            if (pointsConversionConfig == null)
                throw new Exception(" Points Conversion Config Not Exist");

            
           
            pointsConversionConfig.PointsValue=pointsConversionDto.PointsValue;
            pointsConversionConfig.ModifiedById=userId;
            pointsConversionConfig.ModificationDate=DateTimeOffset.Now;

            await _pointsConversionRepository.SaveChangesAsync();

            return true;

        }

        public async Task<PointsConfigDetailsDto> GetPointsConversion(long? SiteId=0)
        {
            var siteId = _workContext.GetCurrentSiteId();
            if (siteId == 0)
            {

                siteId = SiteId.Value;
            
            }
            var pointsConversionConfig = _pointsConversionRepository.Query()
                                                                    .Where(x => x.SiteId == siteId)
                                                                    .Include(x=>x.CreatedBy)
                                                                    .Include(x=>x.ModifiedBy)
                                                                    .FirstOrDefault();

            if (pointsConversionConfig == null)
                return new PointsConfigDetailsDto();

            var result = _mapper.Map<PointsConfigDetailsDto>(pointsConversionConfig);

            return result;

        }

        public async Task<bool> DeletePointsConversion()
        {
            try
            {
                var siteId = _workContext.GetCurrentSiteId();


                var pointsConversionConfig = _pointsConversionRepository.Query()
                                                                        .Where(x => x.SiteId == siteId)
                                                                        .FirstOrDefault();
                if (pointsConversionConfig == null)
                    throw new Exception(" Points Conversion Config Does Not Exist");

                _pointsConversionRepository.Remove(pointsConversionConfig);
                await _pointsConversionRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

            
        }



        }
}
