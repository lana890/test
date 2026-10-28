using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Inventory.Areas.Inventory.ViewModels;
using EskaCommerce.Inventory.Services;
using EskaCommerce.Inventory.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    [Authorize]
    [Route("api/warehouses")]
    public class WarehouseApiController : Controller
    {
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IRepository<Address> _addressRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepository<WarehouseWorkingHours> warehouseWorkingDayRepository;
        private readonly IUserSiteService _userSiteService;
        private readonly IWarehouseServices _warehouseServices;


        public WarehouseApiController(IRepository<Warehouse> warehouseRepository,
                                      IWorkContext workContext,
                                      IRepository<WarehouseWorkingHours> warehouseWorkingDayRepository,
                                      IRepository<Address> addressRepository,
                                      IUserSiteService userSiteService,
                                      IWarehouseServices warehouseServices)
        {
            _warehouseRepository = warehouseRepository;
            _addressRepository = addressRepository;
            _workContext = workContext;
            this.warehouseWorkingDayRepository=warehouseWorkingDayRepository;
            _userSiteService = userSiteService;
            _warehouseServices = warehouseServices;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            var Source = _workContext.GetCurrentPlatform();
            var query = _warehouseRepository.Query().Where(x => x.SiteId == SiteId);
        
            if (!User.IsInRole("admin"))
            {
                query = query.Where(x => x.VendorId == currentUser.VendorId);
            }
            var warehouses = await query.Include(x=>x.Address).Select(x => new
            {
                x.Id,
                x.Name,
                x.IsVisible,
                x.Address
            }).ToListAsync();

            return Ok(warehouses);
        }

        [HttpPost("grid")]
        public async Task<IActionResult> List([FromBody] SmartTableParam param)
        {
            var userId = await _workContext.GetCurrentUserId();
            var SiteId = _workContext.GetCurrentSiteId();
            var userRoles = await _userSiteService.GetUserRoles(SiteId, userId);
            var query = _warehouseRepository.Query().Where(s => s.SiteId == SiteId)
                                                    .Where(s => s.WarehouseRoles.Any() ? s.WarehouseRoles.Any(x => userRoles.Contains(x.RoleId)) : true)
                                                    .Include(x => x.Address)
                                                    .Include(x => x.Vendor)
                                                    .AsNoTracking();

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;

                if (search.Name != null)
                {
                    string name = $"%{search.Name}%";
                    query = query.Where(x => EF.Functions.Like(x.Name, name));
                }
            }



            var warehouses = query.ToSmartTableResult(
                param,
                 sp => new
                 {
                     sp.Id,
                     sp.Name,
                     sp.IsVisible,
                     sp.IsMain,
                     VendorName = sp.VendorId.HasValue ? sp.Vendor.Name : null,
                     ContactName =sp.Address.ContactName,
                     sp.Address.Phone,
                     WarehouseRoleIds = sp.WarehouseRoles.Any() ? sp.WarehouseRoles.Select(x => x.RoleId).ToList() : new List<long>(),

                 });

            return Json(warehouses);
        }


        [HttpPost("GetAllWarehouses")]
        public async Task<IActionResult> GetAllWarehouses([FromBody] SmartTableParam model)
        {

            var result = await _warehouseServices.GetAllWarehouses(model);

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var warehouse = await _warehouseRepository.Query().AsNoTracking().Include(w => w.Address).FirstOrDefaultAsync(w => w.Id == id);
            if (warehouse == null)
            {
                return NotFound();
            }

            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();

            var address = warehouse.Address ?? new Address();
            var model = new WarehouseVm
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                AddressId = address.Id,
                ContactName = address.ContactName,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                Phone = address.Phone,
                StateOrProvinceId = address.StateOrProvinceId,
                CountryId = address.CountryId,
                City = address.City,
                DistrictId = address.DistrictId,
                ZipCode = address.ZipCode,
                IsMain = warehouse.IsMain,
                IsVisible = warehouse.IsVisible
            };

            return Json(model);
        }
        [HttpPost]
        [Route("UpsertWarehouseRoles")]
        public async Task<IActionResult> UpsertWarehouseRoles([FromBody] UpdateWareouseRoles model)
        {
            try
            {

                var result = await _warehouseServices.UpdateWarehouseRoles(model.WareHouseId, model.WarehouseRoleIds);
                if (result)
                    return Ok("Warehouse Roles Updated Sucessfuly");

                return BadRequest("Faild to update warehouse roles");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpGet]
        [Route("AssignedWarehouseRoles/{Id}")]
        public async Task<IActionResult> AssignedWarehouseRoles([FromRoute] long Id)
        {
            try
            {

                var result = await _warehouseServices.GetWareouseRoles(Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);

            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] WarehouseVm model)
        {
            if (ModelState.IsValid)
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
                var address = new Address
                {
                    ContactName = model.ContactName,
                    AddressLine1 = model.AddressLine1,
                    AddressLine2 = model.AddressLine2,
                    Phone = model.Phone,
                    StateOrProvinceId = model.StateOrProvinceId,
                    CountryId = model.CountryId,
                    City = model.City,
                    DistrictId = model.DistrictId,
                    ZipCode = model.ZipCode,
                    SiteId = SiteId,

                };

                var warehouse = new Warehouse
                {
                    Name = model.Name,
                    Address = address,
                    SiteId = SiteId,
                    IsMain = model.IsMain,
                    IsVisible = model.IsVisible,
                    VendorId = model.VendorId
                };


                var currentUser = await _workContext.GetCurrentUser();
                if (!User.IsInRole("admin"))
                {
                    warehouse.VendorId = currentUser.VendorId;
                }

                _warehouseRepository.Add(warehouse);
                await _warehouseRepository.SaveChangesAsync();
                return CreatedAtAction(nameof(Get), new { id = warehouse.Id }, null);
            }

            return BadRequest(ModelState);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] WarehouseVm model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var warehouse = await _warehouseRepository.Query().Include(x => x.Address).FirstOrDefaultAsync(x => x.Id == id);
            if (warehouse == null)
            {
                return NotFound();
            }

            var SiteId = _workContext.GetCurrentSiteId();
            var currentUser = await _workContext.GetCurrentUser();
            if (!User.IsInRole("admin") && warehouse.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this warehouse" });
            }

            warehouse.Name = model.Name;
            warehouse.IsMain = model.IsMain;
            warehouse.IsVisible = model.IsVisible;
            warehouse.VendorId = model.VendorId;

            if (warehouse.Address == null)
            {
                warehouse.Address = new Address();
                _addressRepository.Add(warehouse.Address);
            }

            warehouse.Address.ContactName = model.ContactName;
            warehouse.Address.Phone = model.Phone;
            warehouse.Address.ZipCode = model.ZipCode;
            warehouse.Address.StateOrProvinceId = model.StateOrProvinceId;
            warehouse.Address.CountryId = model.CountryId;
            warehouse.Address.DistrictId = model.DistrictId;
            warehouse.Address.AddressLine1 = model.AddressLine1;
            warehouse.Address.AddressLine2 = model.AddressLine2;
            warehouse.Address.City= model.City;

            warehouse.Address.SiteId = SiteId;


            await _warehouseRepository.SaveChangesAsync();
            return Accepted();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var warehouse = await _warehouseRepository.Query().Include(w => w.Address).FirstOrDefaultAsync(x => x.Id == id);
            if (warehouse == null)
            {
                return NotFound();
            }

            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            //if (!User.IsInRole("admin") && warehouse.VendorId != currentUser.VendorId)
            //{
            //    return BadRequest(new { error = "You don't have permission to manage this warehouse" });
            //}

            try
            {
                _warehouseRepository.Remove(warehouse);
                _addressRepository.Remove(warehouse.Address);

                await _warehouseRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { Error = $"The warehouse {warehouse.Name} can't not be deleted because it is referenced by other tables" });
            }

            return NoContent();
        }

        [HttpGet]
        [Route("get-warehouses")]
        public async Task<IActionResult> GetWarehousesForAdmin()
        {

            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            var userRoles = await _userSiteService.GetUserRoles(SiteId, currentUser.Id);

            var Source = _workContext.GetCurrentPlatform();
            var warehouses = await _warehouseRepository.Query().Include(x=>x.WarehouseRoles)
                                         .Where(x => x.WarehouseRoles.Any() ? x.WarehouseRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                                         .Where(x => x.SiteId == SiteId).Select(x => new
                                            {
                                                x.Id,
                                                x.Name,
                                                x.IsVisible
                                            }).ToListAsync();

          

            return Ok(warehouses);
        }


        [HttpPost("working-hours/list/{warehouseId}")]
        public async Task<IActionResult> GetWarehouseWorkingHours([FromRoute]long warehouseId, [FromBody] SmartTableParam param)
        {
            try
            {
                var result = await _warehouseServices.GetWarehouseWorkingHours(warehouseId, param);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("working-hours/{id}")]
        public async Task<IActionResult> GetWarehouseWorkingHourById([FromRoute] long id)
        {
            try
            {
                var result = await _warehouseServices.GetWarehouseWorkingHourById(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("working-hours/Create")]
        public async Task<IActionResult> CreateWarehouseWorkingHour([FromBody] List<CreateWarehouseWorkingHoursRequest> model)
        {
            try
            {
                var id = await _warehouseServices.CreateWarehouseWorkingHour(model);
                return Ok(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpDelete("working-hours/{id}")]
        public async Task<IActionResult> DeleteWarehouseWorkingHour([FromRoute] long id)
        {
            try
            {
                var result = await _warehouseServices.DeleteWarehouseWorkingHour(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("ValidateWorkingHours")]
        public async Task<IActionResult> ValidateWorkingHours()
        {
            try
            {

                return Ok(await ValidateWorkingHouseWarehouse());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<ValidateWarehouseWorkingHours?> ValidateWorkingHouseWarehouse()
        {
            try
            {
                ValidateWarehouseWorkingHours result = new ValidateWarehouseWorkingHours();

                result.IsClosed=false;
                var warehouseId = await _workContext.GetCurrentWarehouseId(); 

                if (warehouseId == 0)
                    return null;

                var today = (WarehouseWorkingDay)(((int)DateTime.Now.DayOfWeek + 1));

                var schedule = await warehouseWorkingDayRepository.Query().AsNoTracking()
                    .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && (int)x.DayOfWeek == (int)today);

                if (schedule == null)
                    return null;
             return   ValidateWorkingJourney(schedule.DayOfWeek, schedule.OpenTime, schedule.CloseTime, schedule.IsClosed, result);
            }
            catch (Exception)
            {

                throw;
            }
        }
        private ValidateWarehouseWorkingHours ValidateWorkingJourney(
          WarehouseWorkingDay dayOfWeek,
          TimeSpan? openTime,
          TimeSpan? closeTime,
          bool isClosed,
          ValidateWarehouseWorkingHours result)
        {
            var today = (WarehouseWorkingDay)(((int)DateTime.Now.DayOfWeek + 1));

            result.OpenTime = openTime.HasValue
                     ? DateTime.Today.Add(openTime.Value).ToString("hh:mm tt")
                     : null;

            result.CloseTime = closeTime.HasValue
                ? DateTime.Today.Add(closeTime.Value).ToString("hh:mm tt")
                : null;
            result.IsClosed = false;

            if (dayOfWeek != today)
            {
                result.IsClosed = true;
                return result;
            }

            if (isClosed)
            {
                result.IsClosed = true;
                return result;
            }

            if (!openTime.HasValue || !closeTime.HasValue)
            {
                result.IsClosed = true;
                return result;
            }

            var now = DateTime.Now.TimeOfDay;
            if (now < openTime.Value || now > closeTime.Value)
            {
                result.IsClosed = true;
                return result;
            }

            result.IsClosed = false;
            return result;
        }

    }
}
