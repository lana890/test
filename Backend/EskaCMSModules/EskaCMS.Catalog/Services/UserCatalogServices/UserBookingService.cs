using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;
using EskaCMS.Entities.Entity.Addresses;

namespace EskaCommerce.UserCatalog.Services
{
    public class UserBookingService : IUserBookingService
    {
        private string EntityTypeId = "Booking";
        private readonly DateTimeOffset TimeNow = DateTimeOffset.Now;
        private readonly IEntityService _entityService;
        private readonly IRepository<UserBooking> _userBookingRepository;
        private readonly IRepository<UserAddress> _userAddressRepository;
        private readonly IRepository<UserPlans> _userPlansRepository;
        private readonly IRepository<ProviderTimeSlot> _userTimeSlotRepository;
        private readonly IRepository<ProviderTrip> _providerTripRepository;
        private readonly IRepository<ProviderTimeSlot> _providerTimeSlotsRepository;
        private readonly IRepository<ProductAttributeValue> _productAttributeRepository;
        private readonly IRepository<UserBookingHistory> _userBookingHistoryRepository;
        private readonly IProviderTimeSlotsService _providerTimeSlotsService;
        private readonly IWorkContext _workContext;
        public UserBookingService(
            IRepository<UserBooking> userBookingRepository,
            IRepository<UserAddress> userAddressRepository,
            IRepository<ProviderTimeSlot> userTimeSlotRepository,
            IWorkContext workContext,
            IRepository<UserPlans> userPlansRepository,
            IRepository<ProviderTrip> providerTripRepository,
            IEntityService entityService,
            IProviderTimeSlotsService providerTimeSlotsService,
            IRepository<ProviderTimeSlot> providerTimeSlotsRepository,
            IRepository<UserBookingHistory> userBookingHistoryRepository,
            IRepository<ProductAttributeValue> productAttributeRepository
            )
        {
            _userBookingRepository = userBookingRepository;
            _userAddressRepository = userAddressRepository;
            _userTimeSlotRepository = userTimeSlotRepository;
            _workContext = workContext;
            _userPlansRepository = userPlansRepository;
            _providerTripRepository = providerTripRepository;
            _entityService = entityService;
            _providerTimeSlotsService = providerTimeSlotsService;
            _providerTimeSlotsRepository = providerTimeSlotsRepository;
            _userBookingHistoryRepository = userBookingHistoryRepository;
            _productAttributeRepository = productAttributeRepository;
        }
        public async Task<long> Create(UserBookingCreate userBookingInput, long siteId)
        {
            long currentUserId = await _workContext.GetCurrentUserId();
            bool isProviderAvailable = _userBookingRepository
                .Query()
                .Where(b => b.ProviderId == userBookingInput.ProviderId &&
                b.ReservationDate == userBookingInput.ReservationDate &&
                b.ReservationTime == userBookingInput.ReservationTime &&
                b.ProviderTimeSlotId == userBookingInput.ProviderTimeSlotId)
                .Any() ? false : true;
            if (!isProviderAvailable)
            {
                throw new Exception("This provider is not available at this time");
            }

            bool isCustomerAvailable = _userBookingRepository
                .Query()
                .Where(b => b.UserId == currentUserId &&
                b.ReservationDate == userBookingInput.ReservationDate &&
                b.ReservationTime == userBookingInput.ReservationTime
               ).Any() ? false : true;
            if (!isCustomerAvailable)
            {
                throw new Exception("You are already booked another service at this time");
            }
            bool isValidateToBook = ValidateUserPlanToBooking(userBookingInput.UserPlanId, userBookingInput.ProductId, userBookingInput.ReservationDate);
            if (!isValidateToBook)
            {
                throw new Exception("You are excedded you allowed amount of this service");
            }
            UserBooking userBooking = new UserBooking
            {
                ProviderId = userBookingInput.ProviderId,
                ReservationDate = userBookingInput.ReservationDate,
                ReservationTime = userBookingInput.ReservationTime,
                ProviderTimeSlotId = userBookingInput.ProviderTimeSlotId,
                UserAddressId = userBookingInput.UserAddressId,
                CreatedById = currentUserId,
                CreationDate = TimeNow,
                Status = UserBookingStatus.Initiated,
                StatusDate = TimeNow,
                UserId = currentUserId,
                UserPlanId = userBookingInput.UserPlanId,
                ProductId = userBookingInput.ProductId
            };

            return await _userBookingRepository.ExecuteInTransactionAsync(async () =>
            {
                _userBookingRepository.Add(userBooking);
                await _userBookingRepository.SaveChangesAsync();

                var planName = _userBookingRepository.Query().Where(x => x.UserPlanId == userBooking.UserPlanId)
                     .Include(x => x.UserPlan).ThenInclude(x => x.PlanVersions).ThenInclude(x => x.Plan).ThenInclude(x=>x.PlanDetails).FirstOrDefault();

                _entityService.Add(planName.UserPlan.PlanVersions.Plan.PlanDetails.FirstOrDefault().Name, planName.UserPlan.PlanVersions.Plan.PlanDetails.FirstOrDefault().Name, userBooking.Id, EntityTypeId);
                await _userBookingRepository.SaveChangesAsync();
                await CreateBookingHistory(userBooking.Id, UserBookingStatus.Booked, UserBookingStatus.Booked);
                return userBooking.Id;
            });
        }

        public async Task<IActionResult> CreateUserTimeSlot(UserTimeSlotCreate timeSlotInput, long siteId)
        {
            //User user = await _workContext.GetCurrentUser(siteId);
            // IList<ProviderTimeSlot> userTimeSlots = _userTimeSlotRepository.Query().Where(x => x.UserId == user.Id).ToList();

            //ProviderTimeSlot userTimeSlot = new ProviderTimeSlot
            //{
            //    Day = timeSlotInput.Day,
            //    FromTime = timeSlotInput.FromTime,
            //    ToTime = timeSlotInput.ToTime,
            //    UserId = user.Id
            //};
            //_userTimeSlotRepository.Add(userTimeSlot);
            //await _userTimeSlotRepository.SaveChangesAsync();
            return new OkObjectResult("Added Successfully");

        }
        public async Task<IActionResult> StartProviderTrip(ProviderTripCreateVM input)
        {
            UserBooking userBooking = await _userBookingRepository.Query().Include(x => x.ProviderTimeSlot).FirstOrDefaultAsync(x => x.Id == input.BookingId);
            UserBookingStatus oldStatus = userBooking.Status;

            userBooking.Status = UserBookingStatus.OnTheWay;
            userBooking.StatusDate = DateTimeOffset.Now;
            await _userBookingRepository.SaveChangesAsync();
            await CreateBookingHistory(userBooking.Id, userBooking.Status, oldStatus);

            ProviderTrip providerTrip = new ProviderTrip
            {
                BookingId = input.BookingId,
                Distance = input.Distance,
                ProviderId = input.ProviderId
            };
            _providerTripRepository.Add(providerTrip);
            await _userTimeSlotRepository.SaveChangesAsync();
            var finalTime = TimeNow.TimeOfDay.Add(input.DistanceTime);
            if (userBooking.ProviderTimeSlot.FromTime > finalTime)
            {
                return new OkObjectResult("Added Successfully");
            }
            else
            {
                await _providerTimeSlotsService.ShiftProviderTimeSlots(input.ProviderId, finalTime);
                return new OkObjectResult("Added Successfully");
            }
        }
        public async Task UpdateUserConnectionId(LocationStremingVM stremingVM)
        {
            UserBooking userBooking = _userBookingRepository.Query().FirstOrDefault(x => x.Id == stremingVM.UserBookingId);
            userBooking.ConnectionId = stremingVM.ConnectionId;
            await _userBookingRepository.SaveChangesAsync();
        }

        public async Task<UserBookingListOutputVM> Get(long id)
        {

            UserBookingListOutputVM userBooking = await _userBookingRepository.Query()
                .Where(x => x.Id == id)
                .Include(x => x.Provider)
                .Include(x => x.UserAddress)
                .ThenInclude(x => x.Address)
                .Include(x => x.User)
                .Select(c => new UserBookingListOutputVM
                {
                    ProviderId = c.ProviderId,
                    ProviderName = c.Provider.FullName,
                    ProviderAddressId = c.Provider.DefaultShippingAddressId,
                    ReservationDate = c.ReservationDate,
                    ReservationTime = c.ReservationTime,
                    UserAddressId = c.UserAddressId,
                    CreatedById = c.CreatedById,
                    CreationDate = c.CreationDate,
                    Status = c.Status,
                    StatusDate = c.StatusDate,
                    UserId = c.UserId,
                    BookingId = c.Id,
                    Address = new UserAddressVM
                    {
                        Id = c.UserAddressId,
                        UserLatitude = c.UserAddress.Address.Latitude,
                        UserLongitude = c.UserAddress.Address.Longitude
                    }
                })
                .FirstOrDefaultAsync();
            var providerLocation = _userAddressRepository.Query().Where(x => x.Id == userBooking.ProviderAddressId).Select(x => x.Address).FirstOrDefault();
            userBooking.Address.ProviderLatitude = providerLocation.Latitude;
            userBooking.Address.ProviderLongitude = providerLocation.Longitude;
            return userBooking;
        }
        public async Task<UserBooking> GetByUserPlanId(long userPlanId)
        {
            UserBooking userBooking = await _userBookingRepository.Query()
                .Where(x => x.UserPlanId == userPlanId && x.Status == UserBookingStatus.Initiated)
                .FirstOrDefaultAsync();
            return userBooking;
        }
        public async Task<IList<UserBookingListOutputVM>> GetBookingByUserId(long userId)
        {
            var userBooking = await _userBookingRepository.Query()
                .Where(x => x.UserId == userId)
                .Include(x => x.UserPlan)
                .Include(x => x.Provider)
                .Include(x => x.ProviderTimeSlot)
                .Include(x => x.UserAddress)
                .ThenInclude(x => x.Address)
                .Include(x => x.User)
                .Select(c => new UserBookingListOutputVM
                {
                    ProviderId = c.ProviderId,
                    ProviderName = c.Provider.FullName,
                    ProviderAddressId = c.Provider.DefaultShippingAddressId,
                    ReservationDate = c.ReservationDate,
                    ReservationTime = c.ReservationTime,
                    FromTime = ConvertTimaSpanToString(c.ProviderTimeSlot.FromTime),
                    ToTime = ConvertTimaSpanToString(c.ProviderTimeSlot.ToTime),
                    UserAddressId = c.UserAddressId,
                    CreatedById = c.CreatedById,
                    CreationDate = c.CreationDate,
                    Status = c.Status,
                    StatusDate = c.StatusDate,
                    UserId = c.UserId,
                    BookingId = c.Id,
                    ServiceName = c.UserPlan.PlanVersions.Plan.PlanDetails.FirstOrDefault().Name,
                    Address = new UserAddressVM
                    {
                        City = c.UserAddress.Address.City,
                        AddressLine1 = c.UserAddress.Address.AddressLine1,
                        AddressLine2 = c.UserAddress.Address.AddressLine2
                    }
                })
                .ToListAsync();
            return userBooking;
        }
        public async Task<UserBookingDetailsVM> GetDetails(long id)
        {

            UserBookingDetailsVM userBooking = await _userBookingRepository.Query()
                .Where(x => x.Id == id)
                .Include(x => x.Product)
                .ThenInclude(x=>x.ProductDetails)
                .Include(x => x.Provider)
                .Include(x => x.UserAddress)
                .ThenInclude(x => x.Address)
                .Include(x => x.User)
                .Select(c => new UserBookingDetailsVM
                {
                    UserName = c.User.FullName,
                    UserImage = c.User.Image,
                    ProductDetails =  c.Product.ProductDetails.Select(x => new ProductCulture
                    {
                        CultureId = x.CultureId,
                        Name = x.Name,

                    }).ToList(),
                    ReservationDate = c.ReservationDate,
                    ReservationTime = ConvertTimaSpanToString(c.ReservationTime),
                    UserAddressId = c.UserAddressId,
                    UserAddress = c.UserAddress.Address.AddressLine1,
                    UserId = c.UserId,
                    BookingId = c.Id,
                    ProviderAddressId = c.Provider.DefaultShippingAddressId,
                    Address = new UserAddressVM
                    {
                        UserLatitude = c.UserAddress.Address.Latitude,
                        UserLongitude = c.UserAddress.Address.Longitude
                    },
                    ConnectionId = c.ConnectionId,
                    ProviderId = c.ProviderId,
                    Status = c.Status
                })
                .FirstOrDefaultAsync();
            var providerLocation = _userAddressRepository.Query().Where(x => x.Id == userBooking.ProviderAddressId).Select(x => x.Address).FirstOrDefault();
            userBooking.Address.ProviderLatitude = providerLocation.Latitude;
            userBooking.Address.ProviderLongitude = providerLocation.Longitude;

            return userBooking;
        }
        public async Task<IList<UserBookingListOutputVM>> GetPastServices(long siteId)
        {
            long userId = await _workContext.GetCurrentUserId();
            IList<UserBookingListOutputVM> pastServices = await _userBookingRepository.Query()
                 .Where(x =>
                 x.UserId == userId &&
                 (x.Status == UserBookingStatus.ServiceCompleted || x.Status == UserBookingStatus.Expired))
                 .Include(x => x.Provider)
                 .Include(x => x.UserAddress)
                 .ThenInclude(x => x.Address)
                 .Include(x => x.User)
                 .Select(c => new UserBookingListOutputVM
                 {
                     BookingId = c.Id,
                     ProviderId = c.ProviderId,
                     ProviderName = c.Provider.FullName,
                     ReservationDate = c.ReservationDate,
                     ReservationTime = c.ReservationTime,
                     FromTime = ConvertTimaSpanToString(c.ReservationTime),
                     UserAddressId = c.UserAddressId,
                     UserAddress = c.UserAddress.Address.AddressLine1,
                     Status = c.Status,
                     StatusDate = c.StatusDate,
                     ServiceName = c.Product.ProductDetails.Select(f=>f.Name).FirstOrDefault(),
                     ProviderImage = c.Provider.Image,
                     //Rating = _reviewRepository.Query().FirstOrDefault(x => x.EntityId == c.Id).Rating,
                     //IsRated = _reviewRepository.Query().Any(x => x.EntityId == c.Id),

                 })
                 .OrderByDescending(x => x.ReservationDate).ThenByDescending(x => x.ReservationTime)
                 .ToListAsync();



            return pastServices;
        }
        public async Task<UpComingServicesResponseVM<UserBookingListOutputVM>> GetUpcomingServices(long siteId)
        {
            long userId = await _workContext.GetCurrentUserId();
            var cultureId = _workContext.GetCurrentCultureId();

            IList<UserPlansOutput> userPlans = _userPlansRepository.Query()
                .Where(x => x.UserId == userId
                && (x.CurrentStatus == EUserPlansStatus.Subscribed
                || x.CurrentStatus == EUserPlansStatus.Canceled))
                .Include(x => x.PlanVersions)
                .ThenInclude(x => x.PlanProductVersions)
                .ThenInclude(x => x.Product)
                .Select(p => new UserPlansOutput
                {
                    Id = p.Id,
                    ActivationDate = p.ActivationDate,
                    OrderId = p.OrderId,
                    PlanName = p.PlanVersions.Plan.PlanDetails.FirstOrDefault().Name,
                    ExpiresAt = p.PlanVersions.Plan.PlanType == PlanTypes.OneMonthPlan ? p.ActivationDate.AddMonths(1) : p.ActivationDate.AddMonths(3),
                    Products = !p.PlanVersions.Plan.IsCustomPlan ? p.PlanVersions.PlanProductVersions
                       .Select(product => new PlanProductVersionOutput
                       {
                           Id = product.ProductId,
                           Quantity = product.Quantity - _userBookingRepository.Query().Where(b => b.UserPlanId == p.Id && b.ProductId == product.ProductId).Count(),//CalculateQuantity(p.Id,product.Id,product.Quantity),
                           ProductName = product.Product.ProductDetails.Select(f => f.Name).FirstOrDefault(),
                       }).ToList()
                       :
                       p.Order.OrderItems
                       .Select(product => new PlanProductVersionOutput
                       {
                           Id = product.ProductId,
                           Quantity = product.Quantity - _userBookingRepository.Query().Where(b => b.UserPlanId == p.Id && b.ProductId == product.ProductId).Count(),//CalculateQuantity(p.Id,product.Id,product.Quantity),
                           ProductName = product.Product.ProductDetails.Select(f => f.Name).FirstOrDefault(),
                       }).ToList()
                })
                .ToList();

            IList<UserBookingListOutputVM> upcomingServices = await _userBookingRepository.Query()
                 .Where(x => x.UserId == userId &&
                 (x.Status != UserBookingStatus.ServiceCompleted && x.Status != UserBookingStatus.Expired) &&
                    (x.UserPlan.CurrentStatus == EUserPlansStatus.Subscribed
                    || x.UserPlan.CurrentStatus == EUserPlansStatus.Canceled)
                  )
                 .Include(x => x.Provider)
                 .Include(x => x.UserAddress)
                 .ThenInclude(x => x.Address)
                 .Include(x => x.User)
                 .Select(c => new UserBookingListOutputVM
                 {
                     ProviderId = c.ProviderId,
                     ProviderName = c.Provider.FullName,
                     ReservationDate = c.ReservationDate,
                     ReservationTime = c.ReservationTime,
                     FromTime = ConvertTimaSpanToString(c.ProviderTimeSlot.FromTime),
                     UserAddressId = c.UserAddressId,
                     UserAddress = c.UserAddress.Address.AddressLine1,
                     Status = c.Status,
                     StatusDate = c.StatusDate,
                     ServiceName = c.Product.ProductDetails.Select(f => f.Name).FirstOrDefault(),
                     BookingId = c.Id,
                     PlanId = c.UserPlan.PlanVersions.PlanId,
                     UserPlanId = c.UserPlanId

                 })
                 .OrderByDescending(x => x.ReservationDate)
                 .ThenByDescending(x => x.ReservationTime)
                 .ToListAsync();
            UpComingServicesResponseVM<UserBookingListOutputVM> responseVM = new UpComingServicesResponseVM<UserBookingListOutputVM>
            {
                UserPlans = userPlans,
                UpComingServices = upcomingServices
            };
            return responseVM;
        }
        public async Task<UpComingServicesResponseVM<BarberBookingListOutputVM>> GetBarberUpcomingServices()
        {
            long userId = await _workContext.GetCurrentUserId();

            IList<BarberBookingListOutputVM> upcomingServices = await _userBookingRepository.Query()
                 .Where(x => x.ProviderId == userId && x.Status != UserBookingStatus.ServiceCompleted && x.Status != UserBookingStatus.Expired)
                 .Include(x => x.Provider)
                 .Include(x => x.UserAddress)
                 .ThenInclude(x => x.Address)
                 .Include(x => x.User)
                 .Select(c => new BarberBookingListOutputVM
                 {
                     UserId = c.UserId,
                     UserImage = c.User.FullName,
                     ReservationDate = c.ReservationDate,
                     ReservationTime = c.ReservationTime,
                     UserAddressId = c.UserAddressId,
                     UserAddress = c.UserAddress.Address.AddressLine1,
                     Status = c.Status,
                     StatusDate = c.StatusDate,
                     ServiceName = c.Product.ProductDetails.Select(f => f.Name).FirstOrDefault(),
                     BookingId = c.Id
                 })
                 .OrderBy(x => x.ReservationDate)
                 .ThenBy(x => x.ReservationTime)
                 .ToListAsync();
            UpComingServicesResponseVM<BarberBookingListOutputVM> responseVM = new UpComingServicesResponseVM<BarberBookingListOutputVM>
            {
                UpComingServices = upcomingServices
            };
            return responseVM;
        }
        public async Task Update(UserBookingUpdate userBookingInput)
        {
            UserBooking userBooking = GetById(userBookingInput.Id);
            UserBookingStatus oldStatus = userBooking.Status;
            if (userBookingInput.ProviderId.HasValue)
            {
                userBooking.ProviderId = (long)userBookingInput.ProviderId;
            }
            if (userBookingInput.ReservationDate.HasValue)
            {
                bool isProviderAvailable = _userBookingRepository
               .Query()
               .Where(b => b.ProviderId == userBookingInput.ProviderId &&
               b.ReservationDate == userBookingInput.ReservationDate &&
               b.ReservationTime == userBookingInput.ReservationTime &&
               b.ProviderTimeSlotId == userBookingInput.ProviderTimeSlotId)
               .Any() ? false : true;
                if (!isProviderAvailable)
                {
                    throw new Exception("This provider is not available at this time");
                }

                bool isCustomerAvailable = _userBookingRepository
                    .Query()
                    .Where(b => b.UserId == userBooking.UserId &&
                    b.ReservationDate == userBookingInput.ReservationDate &&
                    b.ReservationTime == userBookingInput.ReservationTime
                   ).Any() ? false : true;
                if (!isCustomerAvailable)
                {
                    throw new Exception("You are already booked another service at this time");
                }
                userBooking.ReservationDate = (DateTimeOffset)userBookingInput.ReservationDate;
            }
            if (userBookingInput.Status.HasValue)
            {
                userBooking.Status = (UserBookingStatus)userBookingInput.Status;
                userBooking.StatusDate = TimeNow;
            }
            if (userBookingInput.UserAddressId.HasValue)
            {
                userBooking.UserAddressId = (long)userBookingInput.UserAddressId;
            }
            if (userBookingInput.ProviderTimeSlotId.HasValue)
            {
                userBooking.ProviderTimeSlotId = (long)userBookingInput.ProviderTimeSlotId;
                ProviderTimeSlot providerTimeSlot = _providerTimeSlotsRepository.Query().FirstOrDefault(x => x.Id == userBookingInput.ProviderTimeSlotId);
                userBooking.ReservationTime = providerTimeSlot.FromTime;
            }
            await _userBookingRepository.SaveChangesAsync();
            await CreateBookingHistory(userBooking.Id, userBooking.Status, oldStatus);

        }

        public async Task Delete(long categoryId)
        {
            UserBooking userBooking = GetById(categoryId);
            _userBookingRepository.Remove(userBooking);
            await _userBookingRepository.SaveChangesAsync();
        }
        public async Task<IList<UserBookingListOutputVM>> GetAll(UserBookingListInput input)
        {
            IList<UserBookingListOutputVM> categories = await _userBookingRepository.Query()
                .Include(x => x.Provider)
                .Include(x => x.UserAddress)
                //.Include(x => x.UsersCategories)
                .Include(x => x.User)
                .Skip((input.PageNumber - 1) * input.PageSize)
                .Take(input.PageSize)
                .Select(c => new UserBookingListOutputVM
                {
                    ProviderId = c.ProviderId,
                    ReservationDate = c.ReservationDate,
                    ReservationTime = c.ReservationTime,
                    UserAddressId = c.UserAddressId,
                    // UserCategoryId = c.UserCategoryId,
                    CreatedById = c.CreatedById,
                    CreationDate = c.CreationDate,
                    Status = c.Status,
                    StatusDate = c.StatusDate,
                    UserId = c.UserId
                })
                .ToListAsync();

            return categories;
        }

        public IList<UserTimeSlotOutput> GetAvailableProviderSlots(long ProviderId, ProviderSlotInputVM data)
        {
            var unvailableSlotsIds = _userBookingRepository.Query()
                .Where(x => x.ProviderId == ProviderId && x.ReservationDate.Date == data.BookingDate.Date)
                .Select(c => new
                {
                    Id = c.Id,
                    FromTime = c.ReservationTime,
                    StringToTime = _productAttributeRepository.Query().Include(x=>x.Attribute).ThenInclude(x=>x.AttributeDetails).Where(a => a.Attribute.AttributeDetails.Any(o=>o.Name == "Period") && a.ProductId == c.UserPlan.PlanVersions.PlanProductVersions.OrderByDescending(p => p.Id).First().ProductId).ToList()
                })
                .ToList();
            IList<UserTimeSlotOutput> ProviderSlots;
            if (data.BookingDate.Date == TimeNow.Date)
            {
                ProviderSlots = _providerTimeSlotsRepository.Query()
               //.Where(x => !unvailableSlotsIds.Any(s => s == x.Id) && x.FromTime > TimeNow.TimeOfDay && x.ProviderId == ProviderId && x.Day == data.BookingDate.Date.DayOfWeek)
               .Select(c => new UserTimeSlotOutput
               {
                   Id = c.Id,
                   Day = c.Day,
                   FromTime = ConvertTimaSpanToString(c.FromTime),
                   ToTime = ConvertTimaSpanToString(c.ToTime),
                   TimeSlotDate = c.CreationDate
               })
               .ToList();
            }
            else
            {

                ProviderSlots = _providerTimeSlotsRepository.Query()
                    //.Where(x => !unvailableSlotsIds.Any(s => s == x.Id) && x.ProviderId == ProviderId && x.Day == data.BookingDate.Date.DayOfWeek)
                    .Select(c => new UserTimeSlotOutput
                    {
                        Id = c.Id,
                        Day = c.Day,
                        FromTime = ConvertTimaSpanToString(c.FromTime),
                        ToTime = ConvertTimaSpanToString(c.ToTime),
                        TimeSlotDate = c.CreationDate
                    })
                    .ToList();
            }
            return ProviderSlots;
        }

        private static string ConvertTimaSpanToString(TimeSpan timeSpan)
        {
            DateTime dateTime = DateTime.Today.Add(timeSpan);
            string displayTime = dateTime.ToString("hh:mm tt");
            return displayTime;
        }

        private bool ValidateUserPlanToBooking(long userPlanId, long productId, DateTimeOffset reservationDate)
        {
            UserPlans userPlan = _userPlansRepository.Query().AsNoTracking()
                .Where(u => u.Id == userPlanId)
                .Include(x => x.PlanVersions)
                .ThenInclude(x => x.PlanProductVersions)
               .Include("PlanVersions.Plan")
                  .Include(x => x.Order)
                  .ThenInclude(x => x.OrderItems)
                .FirstOrDefault();
            int userPlanProductQuantity;
            if (userPlan.PlanVersions.Plan.PlanType == PlanTypes.Trial)
                userPlanProductQuantity = userPlan.PlanVersions.PlanProductVersions.FirstOrDefault(pro => pro.ProductId == productId).Quantity;
            else
                userPlanProductQuantity = userPlan.Order.OrderItems.FirstOrDefault(x => x.ProductId == productId).Quantity;
            int bookedPlanQuantty = _userBookingRepository
                .Query().AsNoTracking()
                .Where(b => b.UserPlanId == userPlanId &&
                b.ProductId == productId &&
                b.ReservationDate.Month == reservationDate.Month).Count();   // Perf #15: was ToList().Count - counts in SQL instead of materialising every row
            return bookedPlanQuantty >= userPlanProductQuantity ? false : true;
        }
        private int CalculateQuantity(long userPlanId, long productId, int originalQuantity)
        {
            int bookedPlanQuantty = _userBookingRepository
                .Query().AsNoTracking()
                .Where(b => b.UserPlanId == userPlanId &&
                b.ProductId == productId &&
                b.ReservationDate < TimeNow).Count();   // Perf #15: was ToList().Count - counts in SQL instead of materialising every row
            return originalQuantity - bookedPlanQuantty;
        }
        private UserBooking GetById(long id)
        {
            return _userBookingRepository.Query().FirstOrDefault(x => x.Id == id);
        }

        private async Task<IActionResult> CreateBookingHistory(long UserBookingId, UserBookingStatus NewStatus, UserBookingStatus OldStatus)
        {
            UserBookingHistory userbookingHistory = new UserBookingHistory
            {
                StatusDate = DateTimeOffset.Now,
                OldStatus = OldStatus,
                Status = NewStatus,
                UserBookingId = UserBookingId
            };
            _userBookingHistoryRepository.Add(userbookingHistory);
            await _userBookingHistoryRepository.SaveChangesAsync();
            return new OkObjectResult("Success");
        }
    }
}

