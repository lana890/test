using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Data;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.Services.OrderAddressServices.Interfaces;
using Microsoft.EntityFrameworkCore;
using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.Services.OrderAddressServices
{
    public class UserAddressService : IUserAddressService
    {
        private readonly IRepository<Address> addressRepository;
        private readonly IRepository<UserAddress> userAddressrepository;
        private readonly IWorkContext workContext;

        public UserAddressService(IRepository<Address> AddressRepository,
            IRepository<UserAddress> UserAddressrepository,
            IWorkContext workContext)
        {
            addressRepository = AddressRepository;
            userAddressrepository = UserAddressrepository;
            this.workContext = workContext;
        }

        public async Task<bool> Add(OrderAddressViewModel model)
        {
            try
            {
                var query = new Address();

                query.Status = EskaCMS.Entities.Enum.GeneralEnums.EStatus.Published;
                query.AddressLine1 = model.AddressLine1;
                query.AddressLine2 = model.AddressLine2;
                query.City = model.City;
                query.Phone = model.Phone;
                query.ContactName = model.ContactName;
                query.Email = model.Email;
                query.StateOrProvinceId=model.StateOrProvinceId;
                query.ZipCode = model.ZipCode;  
                query.DistrictId = model.DistrictId == 0 ? null : model.DistrictId;
                query.CountryId = model.CountryId;
                query.SiteId =workContext.GetCurrentSiteId();
                query.Longitude =model.Longitude;
                query.Latitude =model.Latitude;

                addressRepository.Add(query);
                await addressRepository.SaveChangesAsync();

                //Add To User Address

                var AddToUserAddress = new UserAddress();
                AddToUserAddress.UserId= await workContext.GetCurrentUserId();
                AddToUserAddress.AddressId = query.Id;

                userAddressrepository.Add(AddToUserAddress);
                await userAddressrepository.SaveChangesAsync();

                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> Delete(long Id)
        {
            try
            {
                var query = await addressRepository.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();

                query.Status = EskaCMS.Entities.Enum.GeneralEnums.EStatus.Deleted;
               await addressRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<OrderAddresssResponseViewModel>> GetAddress(long? userId , string countryId)
        {
            try
            {
                var cultureId=workContext.GetCurrentCultureId();
                var UserId= userId.HasValue ? userId.Value : await workContext.GetCurrentUserId();
                var query =await userAddressrepository.Query()
                                                 .Where(x => x.UserId == UserId && x.Address.Status!= EStatus.Deleted && (!string.IsNullOrEmpty(countryId) ? x.Address.CountryId == countryId :true))
                                                 .Include(x => x.User)
                                                 .Include(x => x.Address)
                                                 .Include("Address.StateOrProvince")
                                                 .Include("Address.StateOrProvince.StateOrProvinceDetails")
                                                 .Include("Address.District")
                                                 .Include("Address.District.DistrictDetails")
                                                 .Include("Address.Country")
                                                 .Include("Address.Country.CountryDetails")
                                                 .Select(x => new OrderAddresssResponseViewModel
                                                 {

                                                     District = x.Address.District == null ? "" : x.Address.District.DistrictDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                                                     StateOrProvince = x.Address.StateOrProvince == null ? "" : x.Address.StateOrProvince.StateOrProvinceDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                                                     Country = x.Address.Country == null ? "" : x.Address.Country.CountryDetails.Where(x => cultureId == 0 || cultureId == x.CultureId).Select(x => x.Name).FirstOrDefault(),
                                                     AddressLine1 = x.Address.AddressLine1,
                                                     AddressLine2 = x.Address.AddressLine2,
                                                     City = x.Address.City,
                                                     ContactName = x.Address.ContactName,
                                                     Email = x.Address.Email,
                                                     Id = x.Id,
                                                     Phone = x.Address.Phone,
                                                     ZipCode = x.Address.ZipCode,
                                                     AddressId=x.AddressId,
                                                     StateOrProvinceId=x.Address.StateOrProvince == null ? 0 : x.Address.StateOrProvince.Id,
                                                     CountryId=x.Address.Country == null ? "" : x.Address.Country.Id,
                                                     DistrictId=x.Address.District == null ? 0 : x.Address.District.Id,
                                                     Latitude = x.Address.Latitude, 
                                                     Longitude=x.Address.Longitude,
                                                     Regionphonenumber= GetRegionCode(x.Address.Phone)

                                                 }).ToListAsync();

                return query;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> Update(OrderAddressViewModel model)
        {
            try
            {
                var query = await addressRepository.Query().Where(x => x.Id == model.Id).FirstOrDefaultAsync();

                query.AddressLine1 = model.AddressLine1;
                query.AddressLine2 = model.AddressLine2;
                query.City = model.City;
                query.Phone = model.Phone;
                query.ContactName = model.ContactName;
                query.Email = model.Email;
                query.StateOrProvinceId = model.StateOrProvinceId == 0 ? null : model.StateOrProvinceId;
                query.ZipCode = model.ZipCode;
                query.DistrictId =model.DistrictId==0?null: model.DistrictId;
                query.CountryId = model.CountryId;
                query.Longitude = model.Longitude;
                query.Latitude = model.Latitude;
                await addressRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public static string GetRegionCode(string phoneNumber)
        {
            var phoneUtil = PhoneNumberUtil.GetInstance();

            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return "";
                }

                if (!phoneNumber.StartsWith("+"))
                {
                    phoneNumber = "+" + phoneNumber;
                }

                var parsedNumber = phoneUtil.Parse(phoneNumber, null);
                string regionCode = phoneUtil.GetRegionCodeForNumber(parsedNumber);

                return regionCode;
            }
            catch (NumberParseException e)
            {
                return "";
            }
        }
    }
}
