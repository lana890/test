using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using MediatR;
using System;
using System.Collections.Generic;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;
using EskaCMS.Entities.Entity.Core;

namespace EskaCommerce.UserCatalog.Dtos
{
    public class UserBookingCreate
    {

        public long ProviderId { get; set; }
        public long ProviderTimeSlotId { get; set; }
        public long UserAddressId { get; set; }
        public DateTimeOffset ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public int Status { get; set; }
        public long ProductId { get; set; }
        public long UserPlanId { get; set; }

    }
    public class UserBookingUpdate
    {

        public long Id { get; set; }
        public long? ProviderId { get; set; }
        public long? UserAddressId { get; set; }
        public DateTimeOffset? ReservationDate { get; set; }
        public TimeSpan? ReservationTime { get; set; }
        public UserBookingStatus? Status { get; set; }
        public long? ProviderTimeSlotId { get; set; }

    }
    public class UserBookingListInput
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }
    public class ProviderSlotInputVM
    {
        public long ProviderId { get; set; }
        public DateTimeOffset BookingDate { get; set; }
    }
    public class UserBookingListOutputVM
    {
        public long UserId { get; set; }
        public long Id { get; set; }
        public long BookingId { get; set; }
        public long ProviderId { get; set; }
        public long? ProviderAddressId { get; set; }
        public string ProviderName { get; set; }
        public string CustomerName { get; set; }
        public string ProviderImage { get; set; }
        public long UserAddressId { get; set; }
        public string UserAddress { get; set; }
        public UserAddressVM Address { get; set; }
        public string ServiceName { get; set; }
        public string PlanName { get; set; }
        public DateTimeOffset ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }

        public TimeSpan FromTimeSpan { get; set; }
        public TimeSpan ToTimeSpan { get; set; }


        public UserBookingStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public User CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public bool IsRated { get; set; }
        public int Rating { get; set; }
        public long PlanId { get; set; }
        public long UserPlanId { get; set; }
    }
    public class UserBookingDetailsOutputVM
    {
        public long UserId { get; set; }
        public long Id { get; set; }
        public long BookingId { get; set; }
        public long ProviderId { get; set; }
        public long? ProviderAddressId { get; set; }
        public long UserAddressId { get; set; }
        public string ProviderName { get; set; }
        public string ProviderPhoneNumber { get; set; }
        public string CustomerName { get; set; }
        public UserAddressVM ProviderAddress { get; set; }
        public UserAddressVM CustomerAddress { get; set; }
        public IList<ProductCulture> ProductDetails { get; set; }
        public string PlanName { get; set; }
        public DateTimeOffset ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public string Duration { get; set; }
        public UserBookingStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public List<UserBookingHistoryDetailsOutputVM> BookingHistory { get; set; }
    }
    public class ProductCulture
    {
        public string Name { get; set; }
        public long CultureId { get; set; }

    }
    public class UserBookingHistoryDetailsOutputVM
    {
        public DateTimeOffset EventDate { get; set; }
        public UserBookingStatus Status { get; set; }
        public string Duration { get; set; }
    }
    public class UserBookingDetailsVM
    {
        public long UserId { get; set; }
        public long ProviderId { get; set; }
        public long? ProviderAddressId { get; set; }
        public long BookingId { get; set; }
        public string UserName { get; set; }
        public string UserImage { get; set; }
        public long UserAddressId { get; set; }
        public string UserAddress { get; set; }
        public UserAddressVM Address { get; set; }
        //public ProviderAddressVM ProviderAddress { get; set;
        //}
        public IList<ProductCulture> ProductDetails { get; set; }
        public DateTimeOffset ReservationDate { get; set; }
        public string ReservationTime { get; set; }
        public string ConnectionId { get; set; }
        public UserBookingStatus Status { get; set; }
    }
    public class BarberBookingListOutputVM
    {
        public long UserId { get; set; }
        public long BookingId { get; set; }
        public long? UserAddressId { get; set; }
        public string UserName { get; set; }
        public string UserImage { get; set; }
        public string UserAddress { get; set; }
        public UserAddressVM Address { get; set; }
        public string ServiceName { get; set; }
        public DateTimeOffset ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public UserBookingStatus Status { get; set; }
        public DateTimeOffset StatusDate { get; set; }
        public User CreatedBy { get; set; }
        public long CreatedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }

    }
    public class UserBookingListSearchVM
    {
        public string Name { get; set; }
        public UserBookingStatus? CurrentStatus { get; set; }
        public DateTimeOffset? ReservationDateFrom { get; set; }
        public DateTimeOffset? ReservationDateTo { get; set; }
    }

    public class UserAddressVM
    {
        public long Id { get; set; }
        public string ProviderLatitude { get; set; }
        public string ProviderLongitude { get; set; }
        public string UserLatitude { get; set; }
        public string UserLongitude { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
    }
    public class ProviderAddressVM
    {
        public long Id { get; set; }
        public string ProviderLatitude { get; set; }
        public string ProviderLongitude { get; set; }

    }
    public class UserTimeSlotOutput
    {
        public long Id { get; set; }
        public DateTimeOffset? TimeSlotDate { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public DayOfWeek Day { get; set; }

    }
    public class UserTimeSlotCreate
    {

        public TimeSpan FromTime { get; set; }
        public TimeSpan ToTime { get; set; }
        public DayOfWeek Day { get; set; }

    }
    public class UserUnavailableTimeSlotsOutput
    {
        public IList<UserTimeSlotOutput> TimeSlots { get; set; }
        public IList<UserTimeSlotOutput> UnavailableTimeSlots { get; set; }
    }
    public class UpComingServicesResponseVM<T>
    {
        public IList<UserPlansOutput> UserPlans { get; set; }
        public IList<T> UpComingServices { get; set; }
    }
    public class UserBookingChanged : INotification
    {
        public long UserBookingId { get; set; }

        public UserBooking UserBooking { get; set; }

        public UserBookingStatus? OldStatus { get; set; }

        public UserBookingStatus NewStatus { get; set; }

        public long UserId { get; set; }

        public string Note { get; set; }
    }
}
