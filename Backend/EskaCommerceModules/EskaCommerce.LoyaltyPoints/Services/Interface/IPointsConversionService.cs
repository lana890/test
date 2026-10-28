using EskaCommerce.LoyaltyPoints.Models.Request;
using EskaCommerce.LoyaltyPoints.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Services.Interface
{
    public interface IPointsConversionService
    {
        Task<bool> CreatePointsConversion(PointsConversionDto pointsConversionDto);
        Task<bool> UpdatePointsConversion(PointsConversionDto pointsConversionDto);
        Task<PointsConfigDetailsDto> GetPointsConversion(long? SiteId=0);
        Task<bool> DeletePointsConversion();
    }
}
