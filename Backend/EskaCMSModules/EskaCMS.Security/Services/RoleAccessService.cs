
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.Models;
using EskaCMS.Security.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Security;

namespace EskaCMS.Security.Services
{
    public class RoleAccessService : IRoleAccessService
    {
        private IRepository<RoleAccess> _RoleAccessRepository;
        private IRepositoryWithTypedId<ControllerInfo, string> _ControllerInfoRepository;
        private IRepositoryWithTypedId<ActionInfo, string> _ActionInfoRepository;
        public RoleAccessService(IRepository<RoleAccess> RoleAccessRepository,
            IRepositoryWithTypedId<ControllerInfo, string> ControllerInfoRepository,
            IRepositoryWithTypedId<ActionInfo, string> ActionInfoRepository)
        {
            _RoleAccessRepository = RoleAccessRepository;
            _ControllerInfoRepository = ControllerInfoRepository;
            _ActionInfoRepository = ActionInfoRepository;
        }

        public async Task<bool> AddRoleAccessAsync(RolesSecurityVM roleAccess)
        {
            try
            {
                roleAccess.CreationDate = DateTimeOffset.Now;
                var roleAccessObj = new RoleAccess
                {
                    CreationDate = DateTimeOffset.Now,
                    CreatedBy = roleAccess.CreatedBy,
                    RoleId = roleAccess.RoleId
                };
                _RoleAccessRepository.Add(roleAccessObj);
                await _RoleAccessRepository.SaveChangesAsync();
                foreach (var item in roleAccess.Controllers)
                {
                    var contInfoObj = new ControllerInfo()
                    {
                        AreaName = item.AreaName,
                        DisplayName = item.DisplayName,
                        Name = item.Name,
                        RoleAccessId = roleAccessObj.RoleId,

                    };
                    _ControllerInfoRepository.Add(contInfoObj);
                    if (item.Actions.Count() != 0)
                    {
                        foreach (var item2 in item.Actions)
                        {
                            if (item2.isSelected)
                            {
                                var actioninfoObj = new ActionInfo()
                                {
                                    DisplayName = item2.DisplayName,
                                    Name = item2.Name,
                                    ControllerId = contInfoObj.Id,

                                };
                                _ActionInfoRepository.Add(actioninfoObj);
                            }
                        }
                        await _ActionInfoRepository.SaveChangesAsync();
                    }

                }

                await _ControllerInfoRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception exc)

            {

                throw exc;
            }

        }

        public async Task<bool> EditRoleAccessAsync(RolesSecurityVM roleAccess)
        {
            var editroleaccess = await _RoleAccessRepository.Query().Where(x => x.RoleId == roleAccess.RoleId).ToListAsync();
            if (editroleaccess.Count == 0)
            {
                await AddRoleAccessAsync(roleAccess);
                return true;
            }
            else
            {
                await RemoveRoleAccessAsync(roleAccess.RoleId,roleAccess);
                return true;
            }
        }

        public async Task<bool> RemoveRoleAccessAsync(long roleId, RolesSecurityVM roleAccess)
        {
            var editroleaccess = await _RoleAccessRepository.Query().Where(x => x.RoleId == roleId).FirstOrDefaultAsync();
            foreach (var item in roleAccess.Controllers)
            {
                if(string.IsNullOrEmpty(item.Id))
                {
              
                
                var contInfoObj = new ControllerInfo()
                {
                    AreaName = item.AreaName,
                    DisplayName = item.DisplayName,
                    Name = item.Name,
                    RoleAccessId = roleId,

                };
                _ControllerInfoRepository.Add(contInfoObj);
                
                if (item.Actions.Count() != 0)
                {
                    foreach (var item2 in item.Actions)
                    {
                        if (item2.isSelected)
                        {
                            var actioninfoObj = new ActionInfo()
                            {
                                DisplayName = item2.DisplayName,
                                Name = item2.Name,
                                ControllerId = contInfoObj.Id,

                            };
                            _ActionInfoRepository.Add(actioninfoObj);
                        }
                    }
                    await _ActionInfoRepository.SaveChangesAsync();
                }
                }
                else
                {
                    if (item.Actions.Count() != 0)
                    {
                        foreach (var item2 in item.Actions)
                        {
                            if (item2.isSelected)
                            {
                                if(string.IsNullOrEmpty(item2.Id))
                                {
                                    var actioninfoObj = new ActionInfo()
                                    {
                                        DisplayName = item2.DisplayName,
                                        Name = item2.Name,
                                        ControllerId = item.Id,

                                    };
                                    _ActionInfoRepository.Add(actioninfoObj);
                                }
                                
                            }
                            else
                            {
                                var removeAction = await _ActionInfoRepository.Query().Where(x => x.Id == item2.Id).FirstOrDefaultAsync();
                                if(removeAction != null)
                                {
                                    _ActionInfoRepository.Remove(removeAction);
                                }
                            }
                        }
                        await _ActionInfoRepository.SaveChangesAsync();
                    }
                }
            }

            await _ControllerInfoRepository.SaveChangesAsync();
            return true;
           
        }

        public async Task<RoleAccess> GetRoleAccessAsync(long roleId)
        {
            var x = await _RoleAccessRepository.Query().Include(x => x.Controllers).Where(x => x.RoleId == roleId).Select(x => new RoleAccess
            {
                //Controllers = _ControllerInfoRepository.Query().Include(x=> x.Actions).Where(y => y.RoleAccessId == x.Id).ToList(),
                Controllers = x.Controllers.Select(y => new ControllerInfo
                {
                    Actions = _ActionInfoRepository.Query().Where(z => z.ControllerId == y.Id).ToList(),
                    AreaName = y.AreaName,
                    DisplayName = y.DisplayName,
                    RoleAccessId = y.RoleAccessId,
                    Name = y.Name,
                    Id = y.Id
                }).ToList(),
                CreatedBy = x.CreatedBy,
                CreationDate = x.CreationDate,
                RoleId = x.RoleId

            }).FirstOrDefaultAsync();

            return x;
        }

        public Task<bool> HasAccessToActionAsync(string actionId, long role)
        {
            var accessList = _RoleAccessRepository.Query().AsNoTracking()
                .AsQueryable()
                .Where(ra => ra.RoleId == role)
                .Include(x=>x.Controllers)
                .ThenInclude(ac=>ac.Actions)
                .SelectMany(ra => ra.Controllers)
                .ToList();
            var result = Task.FromResult(accessList.SelectMany(c => c.Actions).Any(a => a.Id == actionId));
            return result;
        }
    }
}