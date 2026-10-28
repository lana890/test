using EskaCMS.Entities.Entity.LoyaltyPoints;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.LoyaltyPoints.Models.Request;
using EskaCommerce.LoyaltyPoints.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.Services.Interface
{
    public interface ILoyaltySetupService
    {
        Task<bool> CreateLoyaltySetup(LoyaltySetupDto LoyaltySetup);
        Task<LoyaltySetupDetailsDto> LoyaltySetupDetails(long loyaltySetupId);
        Task<bool> UpdateLoyaltySetup(LoyaltySetupDto loyaltySetupDto, long loyaltySetupId);
        Task<bool> DeleteLoyaltySetup(long loyaltySetupId);
        Task<bool> ActivateLoyaltySetup(long loyaltySetupId);
        Task<bool> DeactivateLoyaltySetup(long loyaltySetupId);
        Task<PaginationResult<LoyaltySetupDetailsDto>> GetAllLoyaltySetups(SmartTableParam param);
    }
}
