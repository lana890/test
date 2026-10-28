using AutoMapper;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.UserCatalog.Dtos;
using EskaCMS.Entities.Entity;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.UserCatalog.Services
{
    public class ProviderTimeSlotsService : IProviderTimeSlotsService
    {
        private readonly DateTimeOffset TimeNow = DateTimeOffset.Now;

        private readonly IRepository<ProviderTimeSlot> _ProviderTimeSlotRepository;
        private readonly IMapper _mapper;
        private readonly IWorkContext _workContext;
        private readonly IRepository<UserBooking> _userBookingRepository;

        public ProviderTimeSlotsService(
            IRepository<ProviderTimeSlot> ProviderTimeSlotRepository,
            IWorkContext workContext,
            IMapper mapper,
            IRepository<UserBooking> userBookingRepository
            )
        {
            _ProviderTimeSlotRepository = ProviderTimeSlotRepository;
            _workContext = workContext;
            _mapper = mapper;
            _userBookingRepository = userBookingRepository;
        }
        public async Task<long> Create(ProviderTimeSlotCreateVM input)
        {
            ProviderTimeSlot timeSlot = _mapper.Map<ProviderTimeSlot>(input);
            timeSlot.CreatedById = _workContext.GetCurrentUserIdValue();
            timeSlot.ModifiedById = _workContext.GetCurrentUserIdValue();
            timeSlot.CreationDate = TimeNow;
            _ProviderTimeSlotRepository.Add(timeSlot);
            await _ProviderTimeSlotRepository.SaveChangesAsync();
            return timeSlot.Id;
        }

        public ProviderTimeSlotOutputVM Get(long id)
        {
            ProviderTimeSlot timeSlot = GetById(id);
            ProviderTimeSlotOutputVM timeSlotOutput = _mapper.Map<ProviderTimeSlotOutputVM>(timeSlot);
            timeSlotOutput.ProviderName = timeSlot.Provider.FullName;
            return timeSlotOutput;
        }
        public async Task<IList<ProviderTimeSlotOutputVM>> GetProviderTimeSlots(long providerId)
        {

            IList<ProviderTimeSlotOutputVM> timeSlots = await _ProviderTimeSlotRepository
                .Query()
                .Where(v => v.ProviderId == providerId)
                .Include(x => x.Provider)
                .Include(x => x.CreatedBy)
                .Include(x => x.ModifiedBy)
                .Select(x => new ProviderTimeSlotOutputVM
                {
                    Id = x.Id,
                    CreatedById = x.CreatedById,
                    CreationDate = x.CreationDate,
                    CreatedBy = x.CreatedBy.FullName,
                    Day = x.Day,
                    FromTime = x.FromTime,
                    ModifiedBy = x.ModifiedBy.FullName,
                    ModifiedById = x.ModifiedById,
                    ProviderId = x.ProviderId,
                    ProviderName = x.Provider.FullName,
                    ToTime = x.ToTime
                }).ToListAsync();

            return timeSlots;
        } 
        public async Task<bool> ShiftProviderTimeSlots(long providerId,TimeSpan shiftFactor)
        {
            try
            {
                shiftFactor= TimeSpan.Parse("00:"+shiftFactor.Minutes.ToString());
                //var user = await _workContext.GetCurrentUser(1);
                IList<long?> unvailableSlotsIds = _userBookingRepository.Query()
                    .Where(x => x.ProviderId == providerId)
                    .Select(c => c.ProviderTimeSlotId)
                    .ToList();

                IList<ProviderTimeSlot> timeSlots = await _ProviderTimeSlotRepository
                    .Query()
                    .Where(v => unvailableSlotsIds.Any(s => s == v.Id))
                    .ToListAsync();

                foreach (var timeSlot in timeSlots)
                {
                    var oldFrom = timeSlot.FromTime;
                    var oldTo = timeSlot.ToTime;
                    timeSlot.FromTime = oldFrom.Add(shiftFactor);
                    timeSlot.ToTime = oldTo.Add(shiftFactor);
                    _ProviderTimeSlotRepository.SaveChanges();
                }
                return true;
            }
            catch (Exception e)
            {

                throw;
            }
        }

        public async Task Update(ProviderTimeSlotUpdateVM timeSlotInput)
        {
            ProviderTimeSlot timeSlot = GetById(timeSlotInput.Id);
            if (timeSlotInput.FromTime.HasValue)
            {
                timeSlot.FromTime = (TimeSpan)timeSlotInput.FromTime;
            }
            if (timeSlotInput.ToTime.HasValue)
            {
                timeSlot.ToTime = (TimeSpan)timeSlotInput.ToTime;
            }
            await _ProviderTimeSlotRepository.SaveChangesAsync();
        }

        private ProviderTimeSlot GetById(long id)
        {
            return _ProviderTimeSlotRepository.Query()
                .Include(x => x.Provider)
                .Include(x => x.CreatedBy)
                .Include(x => x.ModifiedBy)
                .FirstOrDefault(x => x.Id == id);
        }

    }
}
