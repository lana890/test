using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.General.Services.Interfaces;
using EskaCMS.General.ViewModels;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity.General;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Security.Services.Interfaces;

namespace EskaCMS.General.Services
{
    public class LookupSevices : ILookupServices
    {
        private readonly IRepository<Lookup> _LookUp;
        private readonly IRepository<LookupValues> _LookupValues;
        private readonly IWorkContext _WorkContext;
        private readonly IUserRoleService _UserRoleService;
        private readonly IRepository<LookupvalueRoles> _LookupValuesRoles;
        private readonly RoleManager<Role> _roleManager;

        public LookupSevices(IWorkContext WorkContext, IRepository<LookupValues> LookupValues, IRepository<Lookup> Lookup,
          IUserRoleService UserRoleService, IRepository<LookupvalueRoles> LookupValuesRoles, RoleManager<Role> roleManager)
        {

            _LookUp = Lookup;
            _LookupValues = LookupValues;
            _WorkContext = WorkContext;
            _UserRoleService = UserRoleService;
            _LookupValuesRoles = LookupValuesRoles;
            _roleManager = roleManager;

        }
        public async Task<List<LookupValuesVM>> GetLookupValues(long lookupId, long? ParentId)
        {
            var lookup = await _LookupValues.Query().Where(x => x.lookupId == lookupId && ParentId == x.ParentId && x.Status == EStatus.Published).Select(x => new LookupValuesVM
            {
                Id = x.Id,
                Name = x.Name,
                Value = x.Value,
                ParentId = x.ParentId,
                Lookup_Order = x.Lookup_Order,
                LookupId = x.lookupId
            }).ToListAsync();
            return lookup;

        }

        public async Task<LookupVM> GetLookupById(long lookupId)

        {
            long SiteId = _WorkContext.GetCurrentSiteId();

            var lookup = await _LookUp.Query().Where(x => x.Id == lookupId && x.SiteId == SiteId).Select(x => new LookupVM
            {
                Name = x.Name,
                Code = x.Code,
                Description = x.Description,
                DefultValue = x.DefultValue,
                EnableDataSecurity = x.EnableDataSecurity,
                IsEditable = x.IsEditable,
                SiteId = x.SiteId
            }).FirstOrDefaultAsync();
            return lookup;

        }
        public async Task<LookupValuesWithLookupId> GetLookupValues()
        {
            try
            {

                var result = new LookupValuesWithLookupId();
                var lookup = await _LookupValues.Query()

                .Select(x => new LookupValuesVM
                {
                    Name = x.Name,
                    Value = x.Value,
                    Lookup_Order = x.Lookup_Order,
                    Id = x.Id,
                    LookupId = x.lookupId
                }).ToListAsync();
                result.lookupId = lookup.FirstOrDefault().LookupId;
                //result.LookupValuesList = lookup.ToList();
                return result;


            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<List<LookupVM>> GetLookup()
        {
            try
            {

                var lookup = await _LookUp.Query()/*.Include(c => c.Children)*/
                    .Where(x => x.Estatus != Entities.Enum.GeneralEnums.EStatus.Deleted)


                .Select(x => new LookupVM
                {
                    id = x.Id,
                    Name = x.Name,
                    Code = x.Code

                }).ToListAsync();


                return lookup;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<LookupValuesWithLookupId> GetLookupValuesbylookupId(long lookupId, long? ParentId = null)
        {
            try
            {


                var result = new LookupValuesWithLookupId();
                var UserId = await _WorkContext.GetCurrentUserId();

                var UserRoles = await _UserRoleService.GetRoleByUserId(UserId);
                List<long> RoleIds = UserRoles.Select(x => x.RoleId).ToList();


                result.LookupValuesList = await _LookupValues.Query()
                        .Include(x => x.LookupvalueRoles)
                        .Where(x => x.lookupId == lookupId && x.ParentId == ParentId)
                        .Where(x => x.LookupvalueRoles.Count() == 0 || x.LookupvalueRoles.Any(lr => RoleIds.Any(ri => ri == lr.RoleId)))
                        .Select(x => new LookupValues
                        {
                            Name = x.Name,
                            Value = x.Value,
                            Lookup_Order = x.Lookup_Order,
                            Id = x.Id
                        })
                        .ToListAsync();


                result.lookupId = lookupId;
                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public async Task<LookupValuesWithLookupId> GetLookupValueList(long LookupId, LookupSearchVM lookupSearchVM)
        {
            try
            {
                var LookupValue = await _LookupValues.Query().AsNoTracking().Include(l => l.LookUp).Where(l => l.lookupId == LookupId).OrderByDescending(i => i.Id).ToListAsync();
                var SearchQuery = LookupValue.Where((L => L.Name.ToUpper().Contains(lookupSearchVM.SearchValue.ToUpper())
                  || (string.IsNullOrEmpty(lookupSearchVM.SearchValue)))).ToList();
                var LookupValueTree = await BuildLookupValueTree(SearchQuery, lookupSearchVM);
                return new LookupValuesWithLookupId()
                {
                    LookupValuesList = LookupValueTree.ToList(),
                    lookupId = LookupValue.FirstOrDefault().lookupId,
                    TotalCount = SearchQuery.Count
                };




            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<List<LookupValues>> BuildLookupValueTree(List<LookupValues> SearchQuery, LookupSearchVM lookupSearchVM)
        {


            List<LookupValues> LookupList = new List<LookupValues>();
            LookupList = SearchQuery.Select(p => new LookupValues
            {
                Id = p.Id,
                Name = p.Name,
                Value = p.Value,
                Lookup_Order = p.Lookup_Order,
                lookupId = p.lookupId
            })
            .Skip(lookupSearchVM.PageNumber * lookupSearchVM.PageSize)
            .Take(lookupSearchVM.PageSize)
            .ToList();
            return LookupList;

        }



        #region AdminApis
        public async Task<long> AddNewLookups(LookupModel lookupModel, long SiteId)
        {
            try
            {
                var lookup = new Lookup();

                lookup.Name = lookupModel.Name;
                lookup.Code = lookupModel.Code;
                lookup.IsEditable = lookupModel.IsEditable;
                lookup.SiteId = SiteId;
                lookup.CultureId = lookupModel.CultureId;
                lookup.DefultValue = lookupModel.DefultValue;
                lookup.EnableDataSecurity = lookupModel.EnableDataSecurity;
                lookup.Estatus = lookupModel.Estatus;
                lookup.Description = lookupModel.Description;

                _LookUp.Add(lookup);
                await _LookUp.SaveChangesAsync();

                return lookup.Id;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> EditLookups(LookupModel lookupModel, long Id)
        {
            try
            {
                var lookupItem = await _LookUp.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
                //var SiteId = _WorkContext.GetCurrentSiteId();
                if (lookupItem != null)
                {
                    lookupItem.Name = lookupModel.Name;
                    lookupItem.Code = lookupModel.Code;
                    lookupItem.IsEditable = lookupModel.IsEditable;
                    lookupItem.DefultValue = lookupModel.DefultValue;
                    lookupItem.EnableDataSecurity = lookupModel.EnableDataSecurity;
                    lookupItem.Estatus = lookupModel.Estatus;
                    lookupItem.Description = lookupModel.Description;
                    lookupItem.CultureId = lookupModel.CultureId;
                    _LookUp.Update(lookupItem);
                    await _LookUp.SaveChangesAsync();
                }
                return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> DeleteLookup(long Id)
        {
            try
            {
                var lookupItem = await _LookUp.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
                lookupItem.Estatus = Entities.Enum.GeneralEnums.EStatus.Deleted;
                _LookUp.Update(lookupItem);
                await _LookUp.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<long> AddLookupValues(LookupValueVM lookupValuesVM)
        {
            try
            {
                LookupValues lookupValues = new LookupValues();

                lookupValues.Name = lookupValuesVM.Name;
                lookupValues.Lookup_Order = lookupValuesVM.Lookup_Order;
                lookupValues.ParentId = lookupValuesVM.ParentId.HasValue ? lookupValuesVM.ParentId.Value : null;
                lookupValues.lookupId = lookupValuesVM.LookupId;
                lookupValues.Value = lookupValuesVM.Value;
                lookupValues.Status = lookupValuesVM.Status;
                _LookupValues.Add(lookupValues);
                await _LookupValues.SaveChangesAsync();

                return lookupValues.Id;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<bool> EditLookupValues(LookupValuesWithoutOrder lookupValuesVM, long Id)
        {
            try
            {
                var lookupvaluesItem = await _LookupValues.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();
                if (lookupvaluesItem != null)
                {
                    lookupvaluesItem.Name = lookupValuesVM.Name;
                    lookupvaluesItem.ParentId = lookupValuesVM.ParentId;
                    lookupvaluesItem.lookupId = lookupValuesVM.LookupId;
                    lookupvaluesItem.Value = lookupValuesVM.Value;
                    lookupvaluesItem.Status = lookupValuesVM.Status;
                    _LookupValues.Update(lookupvaluesItem);
                    await _LookupValues.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public async Task<bool> ChangeLookupValuesStatus(long Id, Entities.Enum.GeneralEnums.EStatus eStatus)
        {
            try
            {
                var lookupvaluesItem = await _LookupValues.Query().Where(x => x.Id == Id || x.ParentId == Id).ToListAsync();
                if (lookupvaluesItem != null)
                {
                    foreach (var item in lookupvaluesItem)
                    {
                        item.Status = eStatus;
                        _LookupValues.Update(item);
                        await _LookupValues.SaveChangesAsync();
                    }

                }

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public async Task<List<LookupValuesVM>> GetLookupValuesByRoles(List<long> RoleIds)
        {
            try
            {
                var userId = await _WorkContext.GetCurrentUserId();
                var RoleObj = _UserRoleService.GetRoleByUserId(userId).Result;



                //var result = await _LookupValues.Query().Include(r => r.lookupValuesRoles).Where(x => x.lookupValuesRoles
                // .Any(y => RoleIds.Contains(y.RoleId))
                //).Select(x => new LookupValuesVM
                //{
                //    Id = x.Id,
                //    Name = x.Name,
                //    LookupId = x.lookupId,
                //    Lookup_Order = x.Lookup_Order,
                //    ParentId = x.ParentId.Value,
                //    Value = x.Value
                //}).ToListAsync();

                return new List<LookupValuesVM>();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<bool> UpsertLookupValuesRoles(LookupValuesRolesVM lookupRolesVM)
        {
            try
            {
                var oldLookups = await _LookupValuesRoles.Query().Where(x => x.LookupvalueId == lookupRolesVM.LookupValueId).ToListAsync();
                foreach (var val in oldLookups)
                {
                    _LookupValuesRoles.Remove(val);
                }
                await _LookupValuesRoles.SaveChangesAsync();

                foreach (var item in lookupRolesVM.assingedRolesIds)
                {
                    LookupvalueRoles newRole = new LookupvalueRoles();
                    newRole.LookupvalueId = lookupRolesVM.LookupValueId;
                    newRole.RoleId = item;

                    _LookupValuesRoles.Add(newRole);
                }
                await _LookupValuesRoles.SaveChangesAsync();
                return true;


            }

            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<LookupValuesVM> getLookupValuesbyLookupIdAndvalueId(long lookupId, long ValueId)
        {
            try
            {
                var result = await _LookupValues.Query()
                    .Where(x => x.lookupId == lookupId && x.Id == ValueId)
                    .Select(l => new LookupValuesVM
                    {
                        Value = l.Value,
                        Name = l.Name,
                        LookupId = l.lookupId,
                        Lookup_Order = l.Lookup_Order,
                        ParentId = l.ParentId
                    }).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<List<LookupModel>> GetLookupsList(LookupSearchVM searchParamsVM, long SiteId)
        {
            try
            {
                var LookupList = await _LookUp.Query().Where(x => x.SiteId == SiteId
                  && x.Estatus != Entities.Enum.GeneralEnums.EStatus.Deleted
                  && (string.IsNullOrEmpty(searchParamsVM.SearchValue)
                  || x.Name.ToLower().Contains(searchParamsVM.SearchValue.ToLower()))
                  && (string.IsNullOrEmpty(searchParamsVM.SearchValue)
                  || x.Code.ToLower().Contains(searchParamsVM.SearchValue.ToLower())))

                .Select(x => new LookupModel
                {
                    Code = x.Code,
                    Id = x.Id,
                    Name = x.Name,
                    IsEditable = x.IsEditable,
                    CultureId = x.CultureId,
                    DefultValue = x.DefultValue,
                    Estatus = x.Estatus,
                    Description = x.Description,
                    EnableDataSecurity = x.EnableDataSecurity
                }).ToListAsync();

                return LookupList.Skip(searchParamsVM.PageNumber * searchParamsVM.PageSize)
                        .Take(searchParamsVM.PageSize).ToList();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ChangeStatus(long LookupId, Entities.Enum.GeneralEnums.EStatus eStatus)
        {
            try
            {
                var lookupItem = await _LookUp.Query().Where(x => x.Id == LookupId).FirstOrDefaultAsync();
                lookupItem.Estatus = eStatus;
                _LookUp.Update(lookupItem);
                await _LookUp.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<LookupValuesVM> GetAllLookupValues(long LookupId)
        {

            try
            {
                var SiteId = _WorkContext.GetCurrentSiteId();
                var RootValue = await _LookupValues.Query().Where(x => x.lookupId == LookupId
                                 && x.LookUp.Estatus != Entities.Enum.GeneralEnums.EStatus.Deleted
                                 && x.LookUp.SiteId == SiteId)
                                                    .Include(l => l.LookUp)
                                                    .Select(x => new LookupValuesVM
                                                    {
                                                        Id = x.LookUp.Id,
                                                        Name = x.LookUp.Name,
                                                        Status = x.LookUp.Estatus


                                                    }).FirstOrDefaultAsync();
                var ParentId = 0;
                RootValue.Children = getnodes(ParentId, RootValue.Id);



                return RootValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public List<LookupValuesVM> getnodes(long? parentId, long LookupId)
        {
            try
            {
                if (parentId == 0)
                {
                    parentId = null;
                }
                var Child = new List<LookupValuesVM>();
                var node = _LookupValues.Query().Where(x => x.ParentId == parentId && x.lookupId == LookupId)
                 .Select(x => new LookupValuesVM
                 {
                     Id = x.Id,
                     LookupId = x.lookupId,
                     ParentId = x.ParentId,
                     Name = x.Name,
                     Value = x.Value,
                     Status = x.Status


                 }).OrderBy(x => x.Lookup_Order).ToList();

                if (node.Count > 0)
                {
                    foreach (var item in node)
                    {
                        Child.Add(item);
                        item.Children = getnodes(item.Id, item.LookupId);
                    }
                }



                return Child;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<LookupValues>> ChangeLookupOrder(LookupValuesWithOrder Model)
        {
            try
            {
                var LookupValuesList = await _LookupValues.Query()
                                    .Where(x => x.lookupId == Model.LookupId
                                    && x.Status != Entities.Enum.GeneralEnums.EStatus.Deleted).ToListAsync();

                foreach (var lookupvalues in LookupValuesList)
                {
                    var selectedLookup = Model.SortedList.Where(i => i.Id == lookupvalues.Id).FirstOrDefault();
                    if (selectedLookup == null)
                    {
                        continue;
                    }
                    lookupvalues.Lookup_Order = selectedLookup.Lookup_Order;
                    lookupvalues.ParentId = selectedLookup.ParentId == 0 ? null : selectedLookup.ParentId;
                }


                await _LookupValues.SaveChangesAsync();



                return LookupValuesList.OrderBy(l => l.Lookup_Order).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<LookupValuesDetails> GetAllLookupValuesByLookupId(long LookupId)
        {
            try
            {
                var LookupValuesList = await _LookupValues.Query()
                                      .Include(l => l.LookUp)
                                         .Where(x => x.lookupId == LookupId
                                          && x.Status != Entities.Enum.GeneralEnums.EStatus.Deleted)
                                          .ToListAsync();
                var result = new LookupValuesDetails();
                if (LookupValuesList.Count > 0)
                {
                    result = new LookupValuesDetails
                    {
                        Id = LookupValuesList.FirstOrDefault().lookupId,
                        Name = LookupValuesList.FirstOrDefault().LookUp.Name,
                        LookupValuesList = LookupValuesList.Select(x => new LookupValuesVM
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Value = x.Value,
                            ParentId = x.ParentId,
                            Status = x.Status,
                            Lookup_Order = x.Lookup_Order,
                            LookupId = x.lookupId
                        }).ToList()

                    };
                }
                else
                {
                    var looupValuesWithoutList = await _LookUp.Query().AsNoTracking()
                                                      .Where(x => x.Id == LookupId).FirstOrDefaultAsync();

                    result.Id = looupValuesWithoutList.Id;
                    result.Name = looupValuesWithoutList.Name;
                    result.LookupValuesList = new List<LookupValuesVM>();
                }

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<LookupValuesWithRole> GetRoleForLookupValues(long LookupvalueId)
        {
            try
            {
                var RolesValues = new LookupValuesWithRole();
                var SiteId = _WorkContext.GetCurrentSiteId();
                RolesValues.AssignedRoles = await _LookupValuesRoles
                                     .Query()
                                     .Include(l => l.Role)
                                     .Where(x => x.LookupvalueId == LookupvalueId)
                                     .Select(x => new Role
                                     {
                                         Id = x.Role.Id,
                                         Name = x.Role.Name,
                                         Type = x.Role.Type,
                                     }).ToListAsync();

                var unassingedRoles = await _roleManager.Roles.Where(x => x.SiteId == SiteId || x.SiteId == null).Select(x =>
                 new Role
                 {
                     Id = x.Id,
                     Name = x.Name,
                     Type = x.Type,
                 }).ToListAsync();


                RolesValues.UnAssignedRoles = new List<Role>();
                var found = false;
                foreach (var item in unassingedRoles)
                {
                    found = false;
                    foreach (var item2 in RolesValues.AssignedRoles)
                    {
                        if (item.Id == item2.Id)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        RolesValues.UnAssignedRoles.Add(item);
                    }

                }
                RolesValues.LookupValueId = LookupvalueId;
                RolesValues.LookupValueName = await _LookupValues.Query().Where(x => x.Id == LookupvalueId).Select(x => x.Name).FirstAsync();
                return RolesValues;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<LookupValuesDetails> GetAllLookupValuesByCode(string code)
        {
            try
            {
                var lowerCode = code.ToLower();
                var siteId = _WorkContext.GetCurrentSiteId();
                var LookupValuesList = await _LookupValues.Query()
                                                          .Include(l => l.LookUp)
                                                          .Where(x => x.LookUp.Code.ToLower() == lowerCode && x.LookUp.SiteId == siteId && x.Status != EStatus.Deleted)
                                                          .ToListAsync();

                var result = new LookupValuesDetails();
                if (LookupValuesList.Count > 0)
                {
                    result = new LookupValuesDetails
                    {
                        Id = LookupValuesList.FirstOrDefault().lookupId,
                        Name = LookupValuesList.FirstOrDefault().LookUp.Name,
                        LookupValuesList = LookupValuesList.Select(x => new LookupValuesVM
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Value = x.Value,
                            ParentId = x.ParentId,
                            Status = x.Status,
                            Lookup_Order = x.Lookup_Order,
                            LookupId = x.lookupId
                        }).ToList()

                    };
                }
                else
                {
                    var looupValuesWithoutList = await _LookUp.Query().AsNoTracking()
                                                      .Where(x => x.Code.ToLower() == lowerCode).FirstOrDefaultAsync();

                    if (looupValuesWithoutList is null)
                    {
                        throw new Exception($"There is no lookup has this code {code}");
                    }

                    result.Id = looupValuesWithoutList.Id;
                    result.Name = looupValuesWithoutList.Name;
                    result.LookupValuesList = new List<LookupValuesVM>();
                }

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<LookupValuesWithLookupId> GetLookupByLookupIdAndName(long lookupId, string name)
        {
            try
            {
               
                var result = new LookupValuesWithLookupId();
             
                var UserId = await _WorkContext.GetCurrentUserId();

                var UserRoles = await _UserRoleService.GetRoleByUserId(UserId);
               
                List<long> RoleIds = UserRoles.Select(x => x.RoleId).ToList();

                var Nametolower = string.IsNullOrEmpty(name) ? null: name.ToLower();

                var ParentId= await _LookupValues.Query()
                        .Include(x => x.LookupvalueRoles)
                        .Where(x => x.lookupId == lookupId)
                        .Where(x=>string.IsNullOrEmpty(name)||x.Name.ToLower().Equals(Nametolower))
                        .Select(x=>x.Id)
                        .FirstOrDefaultAsync();

                     result.LookupValuesList = await _LookupValues.Query()
                        .Include(x => x.LookupvalueRoles)
                        .Where(x => x.lookupId == lookupId && x.ParentId == ParentId)
                        .Where(x => x.LookupvalueRoles.Count() == 0 || x.LookupvalueRoles.Any(lr => RoleIds.Any(ri => ri == lr.RoleId)))
                        .Select(x => new LookupValues
                        {
                            Name = x.Name,
                            Value = x.Value,
                            Lookup_Order = x.Lookup_Order,
                            Id = x.Id
                        })
                        .ToListAsync();




                if (result.LookupValuesList.Count>0)
                {
                    result.TotalCount = result.LookupValuesList.Count;


                }


                result.lookupId = lookupId;
                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion

    }
}
