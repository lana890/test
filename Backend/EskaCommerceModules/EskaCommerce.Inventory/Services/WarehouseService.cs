using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Inventory.Areas.Inventory.ViewModels;
using EskaCommerce.Inventory.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Inventory.Services
{
    public class WarehouseService : IWarehouseServices
    {
        private readonly IWorkContext _workContext;
        private readonly IRepository<WarehouseWorkingHours> _warehouseWorkingHoursRepository;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IRepository<WarehouseRoles> _warehouseRolesRepository;
        private readonly RoleManager<Role> _roleManager;
        public WarehouseService( RoleManager<Role> roleManager,IWorkContext workContext, IRepository<WarehouseWorkingHours> warehouseWorkingHoursRepository, IRepository<Warehouse> warehouseRepository, IRepository<WarehouseRoles> warehouseRolesRepository)
        {
            _workContext = workContext;
            _warehouseWorkingHoursRepository=warehouseWorkingHoursRepository;
            _warehouseRepository = warehouseRepository;
            _warehouseRolesRepository = warehouseRolesRepository;
            _roleManager = roleManager;
        }

        public async Task<bool> UpdateWarehouseRoles(long Id, List<long> rolesIdsList)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var userId = await _workContext.GetCurrentUserId();

            var warehouseRoleInDb = await _warehouseRolesRepository.Query()
                                                     .Where(x => x.WarehouseId == Id && x.Warehouse.SiteId == siteId)
                                                     .ToListAsync();

            var rolesIdtoBeAdded = rolesIdsList.Where(x => !warehouseRoleInDb.Any(y => y.RoleId == x)).ToList();

            if (rolesIdtoBeAdded.Any())
            {

                var warehouseRoles = rolesIdtoBeAdded.Select(x => new WarehouseRoles { RoleId = x, CreatedById = userId, WarehouseId = Id }).ToList();
                _warehouseRolesRepository.AddRange(warehouseRoles);
                await _warehouseRolesRepository.SaveChangesAsync();

            }

            var rolesIdtoBeDeleted = warehouseRoleInDb.Where(x => !rolesIdsList.Contains(x.RoleId)).ToList();

            if (rolesIdtoBeDeleted.Any())
            {


                _warehouseRolesRepository.RemoveRange(rolesIdtoBeDeleted);
                await _warehouseRolesRepository.SaveChangesAsync();

            }

            return true;
        }


        public async Task<WarehouseAssignedRolesVM> GetWareouseRoles(long Id)
        {
            var siteId = _workContext.GetCurrentSiteId();

          

            var warehouseAssignedRoles = await _warehouseRolesRepository.Query()
                                                     .Where(x => x.WarehouseId == Id && x.Warehouse.SiteId == siteId)
                                                     .Include(x=>x.Role)
                                                     .Select(x => new Role
                                                     {
                                                         Id = x.Role.Id,
                                                         Name = x.Role.Name,
                                                         Type = x.Role.Type,
                                                     })
                                                     .ToListAsync();

            var assignedRoleIds = warehouseAssignedRoles.Select(x => x.Id).ToList();

            var unassingedRoles = await _roleManager.Roles.Where(x => (x.SiteId == siteId || x.SiteId == null) && !assignedRoleIds.Contains(x.Id)).Select(x =>
                  new Role
                  {
                      Id = x.Id,
                      Name = x.Name,
                      Type = x.Type,
                  }).ToListAsync();

            var result = new WarehouseAssignedRolesVM { AssingedRoles = warehouseAssignedRoles, UnassingedRoles = unassingedRoles};

            return result;


        }


        public async Task<PaginationResult<WarehouseListVM>> GetAllWarehouses(SmartTableParam model)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var query = _warehouseRepository.Query().Where(s => s.SiteId == siteId)
                                                 .Include(x => x.Address)
                                                 .Include(x => x.Vendor)
                                                 .AsNoTracking();


            var result = await query.Select(x => new WarehouseListVM
            {
                Id = x.Id,
                Name = x.Name,
                ContactName = x.Address.ContactName,
                IsMain = x.IsMain,
                PhoneNumber = x.Address.Phone,
                Email=x.Address.Email,
            })
                              .PaginateAsync(model.Pagination.Number, model.Pagination.Start);

            return result;


        }

        public async Task<long> GetMainWareHouse(long SiteId)
        {
            try
            {

                var Warehouse = await _warehouseRepository
                                                      .Query()
                                                      .Where(x => x.IsMain && x.SiteId == SiteId)
                                                      .FirstOrDefaultAsync();

                if (Warehouse == null)
                {
                    throw new Exception("warehouse not Exist");
                }

                return Warehouse.Id;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        public async Task<Address> GetAddressByWarehouseId(long WarehouseId)
        {
            try
            {

                var Address = await _warehouseRepository
                                                      .Query()
                                                      .Where(x => WarehouseId == x.Id)
                                                      .Include(x => x.Address)
                                                      .Select(x => x.Address)
                                                      .FirstOrDefaultAsync();

                if (Address == null)
                {
                    throw new Exception("Warehouse not exist");
                }

                return Address;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }


        #region working hours
        public async Task<PaginationResult<WarehouseWorkingHoursViewModel>> GetWarehouseWorkingHours(long warehouseId, SmartTableParam param)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var query = _warehouseWorkingHoursRepository
                .Query()
                .Where(x => x.WarehouseId == warehouseId && x.Warehouse.SiteId == siteId)
                .AsNoTracking();

            if (param?.Search?.PredicateObject != null)
            {
                dynamic s = param.Search.PredicateObject;

                if (s.DayOfWeek != null)
                {
                    WarehouseWorkingDay day = (WarehouseWorkingDay)s.DayOfWeek;
                    query = query.Where(x => x.DayOfWeek == day);
                }

                if (s.IsClosed != null)
                {
                    bool closed = (bool)s.IsClosed;
                    query = query.Where(x => x.IsClosed == closed);
                }
            }


            var projected = query.Select(x => new WarehouseWorkingHoursViewModel
            {
                Id = x.Id,
                WarehouseId = x.WarehouseId,
                DayOfWeek = x.DayOfWeek,
                DayName = x.DayOfWeek.GetDisplayName(),
                OpenTime = x.OpenTime.HasValue ? x.OpenTime.Value : null,
                CloseTime = x.CloseTime.HasValue ? x.CloseTime.Value : null,
                IsClosed = x.IsClosed
            });

            return await projected.PaginateAsync(param.Pagination.Number, param.Pagination.Start);
        }

        // Get by Id
        public async Task<List<WarehouseWorkingHoursViewModel>> GetWarehouseWorkingHourById(long id)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var entity = await _warehouseWorkingHoursRepository
                .Query()
                .Include(x => x.Warehouse)
                .Where(x => x.WarehouseId == id && x.Warehouse.SiteId == siteId)
                .AsNoTracking()
                .ToListAsync();

            if (entity == null)
                return new List<WarehouseWorkingHoursViewModel>();
            return entity.Select(entity =>  new WarehouseWorkingHoursViewModel
            {
                Id = entity.Id,
                WarehouseId = entity.WarehouseId,
                DayOfWeek = entity.DayOfWeek,
                DayName = entity.DayOfWeek.GetDisplayName(),
                OpenTime = entity.OpenTime.HasValue ?entity.OpenTime.Value : null,
                CloseTime = entity.CloseTime.HasValue ? entity.CloseTime.Value : null,
                IsClosed = entity.IsClosed
            }).ToList();
        }

        // Create
        public async Task<bool> CreateWarehouseWorkingHour(List<CreateWarehouseWorkingHoursRequest> model)
        {
            try
            {

            var siteId = _workContext.GetCurrentSiteId();
            var warehouseId = model.Select(x => x.WarehouseId).FirstOrDefault();
            var warehouseWorkingHourstobeRemoved = await _warehouseWorkingHoursRepository.Query().Where(x => warehouseId == x.WarehouseId).ToListAsync();
            _warehouseWorkingHoursRepository.RemoveRange(warehouseWorkingHourstobeRemoved);
            await _warehouseWorkingHoursRepository.SaveChangesAsync();
            var entity = model.ConvertAll(c=> new WarehouseWorkingHours
            {
                WarehouseId = c.WarehouseId,
                DayOfWeek = c.DayOfWeek,
                OpenTime =c.OpenTime,
                CloseTime = c.CloseTime,
                IsClosed = c.IsClosed,
                SiteId= siteId
            });

            _warehouseWorkingHoursRepository.AddRange(entity);
            await _warehouseWorkingHoursRepository.SaveChangesAsync();

            return true;

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        // Update
    

        // Delete
        public async Task<bool> DeleteWarehouseWorkingHour(long id)
        {
            var siteId = _workContext.GetCurrentSiteId();

            var entity = await _warehouseWorkingHoursRepository
                .Query()
                .Include(x => x.Warehouse)
                .FirstOrDefaultAsync(x => x.Id == id && x.Warehouse.SiteId == siteId);

            if (entity == null)
                throw new Exception("Working hours not found.");

            _warehouseWorkingHoursRepository.Remove(entity);
            await _warehouseWorkingHoursRepository.SaveChangesAsync();
            return true;
        }
  
   
        #endregion


    }
}
