using AutoMapper;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;

using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;
using EskaCMS.Entities.Entity.Addresses;

namespace EskaCommerce.UserCatalog.Services
{

    public class UserBookingApiService : IUserBookingApiService
    {

        private readonly IRepository<UserBookingHistory> _userBookingHistoryRepository;
        private readonly IRepository<UserBooking> _userBookingRepository;
        private readonly IRepository<UserAddress> _userAddressRepository;
        public UserBookingApiService(
            IRepository<UserBooking> userBookingRepository,
            IRepository<UserBookingHistory> userBookingHistoryRepository,
            IRepository<UserAddress> userAddressRepository
            )
        {
            _userBookingRepository = userBookingRepository;
            _userBookingHistoryRepository = userBookingHistoryRepository;
            _userAddressRepository = userAddressRepository;
        }
        public GridOutputVM<UserBookingListOutputVM> GetFilteredUserBookings(long? userId, SearchTableParam<UserBookingListSearchVM> param)
        {
            UserBookingListSearchVM search = param.Search;
            var userBookingQuery = _userBookingRepository.Query()
                .Include(x=>x.Product)
                .ThenInclude(x=>x.ProductDetails)
                                      .Where(userBooking =>
                                          (string.IsNullOrEmpty(search.Name) || (userBooking.Provider.FullName.ToUpper().Contains(search.Name.ToUpper()) || userBooking.Product.ProductDetails.Any(u=>u.Name.ToUpper().Contains(search.Name.ToUpper())))
                                          && ((search.CurrentStatus == null || search.CurrentStatus == 0) || userBooking.Status == search.CurrentStatus)
                                          && (search.ReservationDateFrom == null || userBooking.ReservationDate >= search.ReservationDateFrom)
                                          && (search.ReservationDateTo == null || userBooking.ReservationDate <= search.ReservationDateTo)
                                          && ((userId == null || userId == 0) || userBooking.UserId == userId)))
                                      .Include(x => x.User)
                                      .Include(x => x.Provider)
                                      .Include(x => x.ProviderTimeSlot)
                                      .Include(x => x.UserAddress)
                                      .ThenInclude(x => x.Address)
                                      .Include(x => x.UserPlan)
                                      .ThenInclude(x => x.PlanVersions)
                                      .ThenInclude(x => x.Plan)
                                      .ThenInclude(x=>x.PlanDetails)
                                      .OrderByDescending(d => d.ReservationDate);

            // Paginate in SQL (Skip/Take) so only the requested page is materialized,
            // instead of loading every matching booking + its full include graph into
            // memory and paging afterwards. Same filter, same ordering, same page.
            var pagedBookings = userBookingQuery
                                      .Skip(param.Pagination.Number * param.Pagination.Start)
                                      .Take(param.Pagination.Number)
                                      .ToList();
            GridOutputVM<UserBookingListOutputVM> bookings = new GridOutputVM<UserBookingListOutputVM>
            {
                Items = pagedBookings.Select(c => new UserBookingListOutputVM
                {
                    Id = c.Id,
                    ProviderId = c.ProviderId,
                    ProviderName = c.Provider.FullName,
                    ProviderImage = c.Provider.Image,
                    CustomerName = c.User.FullName,
                    ProviderAddressId = c.Provider.DefaultShippingAddressId,
                    ReservationDate = c.ReservationDate,
                    ReservationTime = c.ReservationTime,
                    FromTime = ConvertTimaSpanToString(c.ProviderTimeSlot.FromTime),
                    ToTime = ConvertTimaSpanToString(c.ProviderTimeSlot.ToTime),
                    FromTimeSpan = c.ProviderTimeSlot.FromTime,
                    ToTimeSpan = c.ProviderTimeSlot.ToTime,
                    UserAddressId = c.UserAddressId,
                    CreatedById = c.CreatedById,
                    CreationDate = c.CreationDate,
                    Status = c.Status,
                    StatusDate = c.StatusDate,
                    UserId = c.UserId,
                    BookingId = c.Id,
                    PlanName = c.UserPlan.PlanVersions.Plan.PlanDetails.FirstOrDefault().Name,
                    ServiceName = c.Product.ProductDetails.Select(x => x.Name).FirstOrDefault(),

                    Address = new UserAddressVM
                    {
                        City = c.UserAddress.Address.City,
                        AddressLine1 = c.UserAddress.Address.AddressLine1,
                        AddressLine2 = c.UserAddress.Address.AddressLine2
                    }
                }).ToList(),
                TotalRecord = userBookingQuery.Count()
            };

            return bookings;

        }
        public UserBookingDetailsOutputVM GetUserBookingById(long id)
        {
            UserBookingDetailsOutputVM userBooking = _userBookingRepository.Query()
                                      .Where(userBooking => userBooking.Id == id)
                                      .Include(x => x.Product)
                                      .Include(x => x.User)
                                      .Include(x => x.Provider)
                                      .Include(x => x.ProviderTimeSlot)
                                      .Include(x => x.UserAddress)
                                      .ThenInclude(x => x.Address)
                                      .Include(x => x.UserPlan)
                                      .ThenInclude(x => x.PlanVersions)
                                      .ThenInclude(x => x.Plan)
                                      .ThenInclude(x=>x.PlanDetails)
                                      .OrderByDescending(d => d.ReservationDate)
                                      .Select(c => new UserBookingDetailsOutputVM
                                      {
                                          Id = c.Id,
                                          ProviderId = c.ProviderId,
                                          ProviderName = c.Provider.FullName,
                                          ProviderPhoneNumber = c.Provider.PhoneNumber,
                                          CustomerName = c.User.FullName,
                                          ProviderAddressId = c.Provider.DefaultShippingAddressId,
                                          ReservationDate = c.ReservationDate,
                                          ReservationTime = c.ReservationTime,
                                          FromTime = ConvertTimaSpanToString(c.ProviderTimeSlot.FromTime),
                                          ToTime = ConvertTimaSpanToString(c.ProviderTimeSlot.ToTime),
                                          UserAddressId = c.UserAddressId,
                                          CreationDate = c.CreationDate,
                                          Status = c.Status,
                                          StatusDate = c.StatusDate,
                                          UserId = c.UserId,
                                          BookingId = c.Id,
                                          PlanName = c.UserPlan.PlanVersions.Plan.PlanDetails.FirstOrDefault().Name,
                                          ProductDetails = c.Product.ProductDetails.Select(x=> new ProductCulture { 
                                          CultureId= x.CultureId,   
                                          Name= x.Name, 
                                          
                                          }).ToList(),
                                          CustomerAddress = new UserAddressVM
                                          {
                                              City = c.UserAddress.Address.City,
                                              AddressLine1 = c.UserAddress.Address.AddressLine1,
                                              AddressLine2 = c.UserAddress.Address.AddressLine2,
                                              UserLatitude = c.UserAddress.Address.Latitude,
                                              UserLongitude = c.UserAddress.Address.Longitude
                                          },
                                          BookingHistory = _userBookingHistoryRepository.Query().Where(h => h.UserBookingId == id).OrderBy(x => x.StatusDate).Select(bh => new UserBookingHistoryDetailsOutputVM
                                          {
                                              EventDate = bh.StatusDate,
                                              Status = bh.Status,
                                          }).ToList(),
                                          Duration = (c.ProviderTimeSlot.ToTime - c.ProviderTimeSlot.FromTime).ToString()
                                      })
                                      .FirstOrDefault();
            var providerLocation = _userAddressRepository.Query().Where(x => x.Id == userBooking.ProviderAddressId).Select(x => x.Address).FirstOrDefault();
            if (providerLocation != null)
            {
                userBooking.ProviderAddress = new UserAddressVM
                {
                    AddressLine1 = providerLocation.AddressLine1,
                    City = providerLocation.City,
                    Id = providerLocation.Id,
                    ProviderLatitude = providerLocation.Latitude,
                    ProviderLongitude = providerLocation.Longitude
                };
            }


            return userBooking;

        }
        public async Task<IList<UserBookingHistoryDetailsOutputVM>> GetUserBookingHistoryById(long bookingId)
        {
            var duration = await _userBookingHistoryRepository.Query().AsNoTracking()
                                .Where(x => x.Status == UserBookingStatus.ServiceCompleted && x.UserBookingId == bookingId)
                                .ToListAsync();

            var bookingHistory = await _userBookingHistoryRepository.Query()
                .Where(h => h.UserBookingId == bookingId)
                .Select(bh => new UserBookingHistoryDetailsOutputVM
                {
                    EventDate = bh.StatusDate,
                    Status = bh.Status,
                    Duration = (duration != null || duration.Count != 0) ?
                                CalculateDuration(_userBookingHistoryRepository.Query().Where(u => (u.Status == UserBookingStatus.ServiceStarted || u.Status == UserBookingStatus.ServiceCompleted) && u.UserBookingId == bh.UserBookingId).ToList(), UserBookingStatus.ServiceStarted, UserBookingStatus.ServiceCompleted) : ""

                }).ToListAsync();


            return bookingHistory;
        }
        private static string ConvertTimaSpanToString(TimeSpan timeSpan)
        {
            DateTime dateTime = DateTime.Today.Add(timeSpan);
            string displayTime = dateTime.ToString("hh:mm tt");
            return displayTime;
        }
        private static string CalculateDuration(IList<UserBookingHistory> statuses, UserBookingStatus status1, UserBookingStatus status2)
        {
            if (statuses.Count >= 2)
            {
                var statusTwo = statuses.FirstOrDefault(x => x.Status == status2);
                var statusOne = statuses.FirstOrDefault(x => x.Status == status1);
                TimeSpan duration = statusTwo.StatusDate - statusOne.StatusDate;

                return duration.ToString(@"hh\:mm\:ss");
            }
            return "";

        }
    }

}

