using EskaCommerce.UserCatalog.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EskaCommerce.UserCatalog.Services.Interfaces
{
    public interface IProviderTimeSlotsService
    {
        Task<long> Create(ProviderTimeSlotCreateVM input);
        ProviderTimeSlotOutputVM Get(long id);
        Task<IList<ProviderTimeSlotOutputVM>> GetProviderTimeSlots(long providerId);
        Task Update(ProviderTimeSlotUpdateVM timeSlotInput);
        Task<bool> ShiftProviderTimeSlots(long providerId, TimeSpan shiftFactor);
    }

}
