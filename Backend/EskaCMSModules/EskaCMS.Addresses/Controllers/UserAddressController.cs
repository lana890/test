using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Addresses.Models;
using EskaCMS.Entities.Enum;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Addresses.Controllers
{
    [Area("Core")]
    [Route("api/UserAdress/[action]")]
    [Authorize]
    public class UserAddressController : Controller
    {
        private readonly IRepository<UserAddress> _userAddressRepository;
        private readonly IRepository<Address> _addressRepository;
        private readonly IRepositoryWithTypedId<Country, string> _countryRepository;
        private readonly IRepository<StateOrProvince> _stateOrProvinceRepository;
        private readonly IRepository<District> _districtRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IWorkContext _workContext;

        public UserAddressController(IRepository<UserAddress> userAddressRepository, IRepositoryWithTypedId<Country, string> countryRepository, IRepository<StateOrProvince> stateOrProvinceRepository,
            IRepository<District> districtRepository,
            IRepository<User> userRepository,
            IRepository<Address> addressRepository,
            IWorkContext workContext)
        {
            _userAddressRepository = userAddressRepository;
            _countryRepository = countryRepository;
            _stateOrProvinceRepository = stateOrProvinceRepository;
            _districtRepository = districtRepository;
            _userRepository = userRepository;
            _workContext = workContext;
            _addressRepository = addressRepository;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            var CultureId =  _workContext.GetCurrentCultureId();
            var model = _userAddressRepository
                .Query()
                .Where(x => x.AddressType == AddressType.Shipping && x.UserId == currentUser.Id)
                .Select(x => new UserAddressListItem
                {
                    AddressId = x.AddressId,
                    UserAddressId = x.Id,
                    ContactName = x.Address.ContactName,
                    Phone = x.Address.Phone,
                    AddressLine1 = x.Address.AddressLine1,
                    AddressLine2 = x.Address.AddressLine2,
                    DistrictName = x.Address.District.DistrictDetails.Where(x=>CultureId==0||x.CultureId== CultureId).Select(x=>x.Name).FirstOrDefault(),
                    StateOrProvinceName = x.Address.StateOrProvince.StateOrProvinceDetails.Where(x => CultureId == 0 || x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault() ,
                    CountryName = x.Address.Country.CountryDetails.Where(x => CultureId == 0 || x.CultureId == CultureId).Select(x => x.Name).FirstOrDefault(),
                    DisplayCity = x.Address.Country.IsCityEnabled,
                    DisplayZipCode = x.Address.Country.IsZipCodeEnabled,
                    DisplayDistrict = x.Address.Country.IsDistrictEnabled,
                    IsDefaultShippingAddress = x.Id == currentUser.DefaultShippingAddressId,
                    City = x.Address.City,
                    Latitude = x.Address.Latitude,
                    Longitude = x.Address.Longitude,
                    StateOrProvinceId = x.Address.StateOrProvinceId,
                    CountryId = x.Address.CountryId,
                }).ToList();


            return Ok(model);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllAddresses()
        {
            var SiteId = _workContext.GetCurrentSiteId();

            var model = _addressRepository
                .Query().Where(x => x.SiteId == SiteId)
                .Select(x => new AddressVM
                {
                    Id = x.Id,
                    ContactName = x.ContactName,
                    Phone = x.Phone,
                    AddressLine1 = x.AddressLine1,
                    AddressLine2 = x.AddressLine2,
                    City = x.City,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    StateOrProvinceId = x.StateOrProvinceId,
                    CountryId = x.CountryId,
                }).ToList();


            return Ok(model);
        }

        [HttpGet]
        public async Task<IActionResult> Get(string countryId)
        {
            var country = await _countryRepository.Query().AsNoTracking().Include(x=>x.CountryDetails).Include(x => x.StatesOrProvinces).ThenInclude(x=>x.StateOrProvinceDetails).FirstOrDefaultAsync(x => x.Id == countryId);
            var cultureId=_workContext.GetCurrentCultureId();
            if (country == null)
            {
                return NotFound();
            }

            var model = new
            {
                CountryId = country.Id,
                CountryName = country.CountryDetails.Where(x=> cultureId==0||x.CultureId== cultureId).Select(x=>x.Name).FirstOrDefault(),
                country.IsBillingEnabled,
                country.IsShippingEnabled,
                country.IsCityEnabled,
                country.IsZipCodeEnabled,
                country.IsDistrictEnabled,
                StatesOrProvinces = country.StatesOrProvinces.Select(x => new { x.Id,Name= x.StateOrProvinceDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault() })
            };

            return Ok(model);
        }
        [HttpGet]
        public async Task<IActionResult> GetByUserId(long userAddressId)
        {
            UserAddressFormViewModel userAddress = await _userAddressRepository.Query()
                .Include(x => x.Address)
                .Where(x => x.Id == userAddressId)
                .Select(x => new UserAddressFormViewModel
                {
                    AddressLine1 = x.Address.AddressLine1,
                    Latitude = x.Address.Latitude,
                    Longitude = x.Address.Longitude
                }).FirstOrDefaultAsync();
            return Ok(userAddress);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserAddressFormViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                  
                    var currentUser = await _workContext.GetCurrentUser();
                    bool hasAddresses = currentUser.DefaultShippingAddressId != null;

                    var address = new Address
                    {
                        AddressLine1 = model.AddressLine1,
                        AddressLine2 = model.AddressLine2,
                        ContactName = model.ContactName,
                        CountryId = model.CountryId,
                        StateOrProvinceId = model.StateOrProvinceId,
                        DistrictId = model.DistrictId,
                        City = model.City,
                        ZipCode = model.ZipCode,
                        Phone = model.Phone,
                        Latitude = model.Latitude,
                        Longitude = model.Longitude,

                    };

                    var userAddress = new UserAddress
                    {
                        Address = address,
                        AddressType = AddressType.Shipping,
                        UserId = currentUser.Id
                    };

                    _userAddressRepository.Add(userAddress);
                    _userAddressRepository.SaveChanges();
                    if (!hasAddresses)
                    {
                        currentUser.DefaultShippingAddressId = userAddress.Id;
                        _userRepository.SaveChanges();
                    }
                    return Ok(userAddress.Id);
                }
                return Ok();
            }
            catch (Exception e)
            {

                return BadRequest(e.Message);
            }
        }

        //[HttpGet]
        //public async Task<IActionResult> UpdateUserShipableAddress(long id)
        //{
        //    try
        //    {
        //        var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
        //        var currentUser = await _workContext.GetCurrentUser(SiteId);
        //        currentUser.DefaultShippingAddressId = id;
        //        _userRepository.SaveChanges();
        //        return Ok();
        //    }
        //    catch (Exception e)
        //    {

        //        return BadRequest(e.Message);
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> EditAddress(long id, [FromBody] UserAddressFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var currentUserId = await _workContext.GetCurrentUserId();
                var userAddress = _userAddressRepository.Query()
                    .Include(x => x.Address)
                    .FirstOrDefault(x => x.Id == id && x.UserId == currentUserId);

                if (userAddress == null)
                {
                    return NotFound();
                }

                userAddress.Address.AddressLine1 = model.AddressLine1;
                userAddress.Address.AddressLine2 = model.AddressLine2;
                userAddress.Address.ContactName = model.ContactName;
                userAddress.Address.CountryId = model.CountryId;
                userAddress.Address.StateOrProvinceId = model.StateOrProvinceId;
                userAddress.Address.DistrictId = model.DistrictId;
                userAddress.Address.City = model.City;
                userAddress.Address.ZipCode = model.ZipCode;
                userAddress.Address.Phone = model.Phone;

                _userAddressRepository.SaveChanges();
                return RedirectToAction("List");
            }

            PopulateAddressFormData(model);
            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetAsDefault(long id)
        {
            var currentUser = await _workContext.GetCurrentUser();
            var userAddress = _userAddressRepository.Query()
                .FirstOrDefault(x => x.Id == id && x.UserId == currentUser.Id);

            if (userAddress == null)
            {
                return NotFound();
            }

            currentUser.DefaultShippingAddressId = userAddress.Id;
            _userRepository.SaveChanges();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            var userAddress = _userAddressRepository.Query()
                .FirstOrDefault(x => x.Id == id && x.UserId == currentUser.Id);

            if (userAddress == null)
            {
                return NotFound();
            }

            if (currentUser.DefaultShippingAddressId == userAddress.Id)
            {
                currentUser.DefaultShippingAddressId = null;
            }

            _userAddressRepository.Remove(userAddress);
            _userAddressRepository.SaveChanges();

            return Ok();
        }

        private void PopulateAddressFormData(UserAddressFormViewModel model)
        {
            var shippableCountries = _countryRepository.Query()
                .Include(x=>x.CountryDetails)
                .Where(x => x.IsShippingEnabled);

            if (!shippableCountries.Any())
            {
                return;
            }

            model.Countries = shippableCountries
                .SelectMany(x=>x.CountryDetails)
                .OrderBy(x=>x.Name)
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.CountryId.ToString()
                }).ToList();

            var selectedShipableCountryId = !string.IsNullOrEmpty(model.CountryId) ? model.CountryId : model.Countries.First().Value;
            var selectedCountry = shippableCountries.FirstOrDefault(c => c.Id == selectedShipableCountryId);
            if (selectedCountry != null)
            {
                model.DisplayCity = selectedCountry.IsCityEnabled;
                model.DisplayDistrict = selectedCountry.IsDistrictEnabled;
                model.DisplayZipCode = selectedCountry.IsZipCodeEnabled;
            }

            model.StateOrProvinces = _stateOrProvinceRepository
                .Query()
                .Include(x=>x.StateOrProvinceDetails)
                .Where(x => x.CountryId == selectedShipableCountryId)
                .SelectMany(x=>x.StateOrProvinceDetails)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                }).ToList();

            if (model.StateOrProvinceId > 0)
            {
                model.Districts = _districtRepository
                    .Query()
                    .Include(x=>x.DistrictDetails)
                    .Where(x => x.StateOrProvinceId == model.StateOrProvinceId)
                    .SelectMany(x=>x.DistrictDetails)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.DistrictId.ToString()
                    }).ToList();
            }
        }
    }
}
