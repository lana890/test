using EskaCMS.Addresses.Services.Interfaces;
using EskaCMS.Addresses.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Address;
using EskaCMS.Entities.Entity.Addresses;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Addresses.Services
{
    public class DistrictApiService : IDistrictApiService
    {
        private readonly IRepository<District> _DistrictRepository;
        private readonly IRepository<DistrictDetails> districtDetailsRepository;
        private readonly IWorkContext workContext;

        public DistrictApiService(
            IRepository<District> DistrictRepository,
            IRepository<DistrictDetails> DistrictDetailsRepository, IWorkContext workContext

            )
        {
            _DistrictRepository = DistrictRepository;
            districtDetailsRepository = DistrictDetailsRepository;
            this.workContext = workContext;
        }
        public DistrictSmartGridOutputVM<DistrictVM> GetDistrictsByStateId(DistrictSmartSearchTableParam<DistrictSearchListVM> param, long stateId, long siteId)
        {
            var cultureId = workContext.GetCurrentCultureId();
            DistrictSearchListVM search = param.Search;
            var DistrictQuery = _DistrictRepository.Query().AsNoTracking().Include(x => x.StateOrProvince).ThenInclude(x => x.StateOrProvinceDetails).Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published)
                .Include(x => x.DistrictDetails)
                                      .Where(x => x.StateOrProvinceId == stateId &&
                                      (string.IsNullOrEmpty(search.Name) || x.DistrictDetails.Any(g => g.Name.ToUpper().Contains(search.Name.ToUpper())) ||
                                      (string.IsNullOrEmpty(search.Location) || x.Location.ToUpper().Contains(search.Location.ToUpper()))))
                                      .ToList();
            DistrictSmartGridOutputVM<DistrictVM> Districts = new DistrictSmartGridOutputVM<DistrictVM>
            {
                Items = DistrictQuery.Select(s => new DistrictVM
                {
                    Id = s.Id,
                    Name = s.DistrictDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault(),
                    Type = s.Type,
                    Location = s.Location,
                    StateOrProvinceId = s.StateOrProvinceId,
                    StateOrProvinceName = s.StateOrProvince.StateOrProvinceDetails.Where(x => cultureId == 0 || x.CultureId == cultureId).Select(x => x.Name).FirstOrDefault()
                }).Skip(param.Pagination.Number * param.Pagination.Start).Take(param.Pagination.Number).OrderByDescending(x => x.Id).ToList(),
                TotalRecord = DistrictQuery.Count()
            };

            return Districts;
        }

        public async Task<DistrictRequestResponseVM> GetDistrictById(long id)
        {
            var Districts = await _DistrictRepository.Query().Include(x => x.DistrictDetails).Where(x => x.Id == id).Select(d => new DistrictRequestResponseVM
            {
                Id = d.Id,
                DistrictDetails = d.DistrictDetails.Select(x => new DistrictDetailsVM
                {
                    CultureId = x.CultureId,
                    Name = x.Name,

                }).ToList(),
                Type = d.Type,
                Location = d.Location,
                StateOrProvinceId = d.StateOrProvinceId
            }).FirstOrDefaultAsync();


            return Districts;
        }
        public async Task<bool> EditDistrict(long Id, DistrictRequestResponseVM model)
        {
            var state = await _DistrictRepository.Query().Where(x => x.Status == Entities.Enum.GeneralEnums.EStatus.Published).FirstOrDefaultAsync(x => x.Id == Id);
            state.Location = model.Location;
            state.Type = model.Type;

            await _DistrictRepository.SaveChangesAsync();
            var DistrictDetails = await districtDetailsRepository.Query().Where(x => x.DistrictId == Id).ToListAsync();

            foreach (var DistrictDetail in DistrictDetails)
            {
                foreach (var modelcountrydetails in model.DistrictDetails)
                {
                    if (DistrictDetail.CultureId == modelcountrydetails.CultureId)
                    {

                        DistrictDetail.Name = modelcountrydetails.Name;
                    }

                }
            }
            await districtDetailsRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateDistrict(DistrictRequestResponseVM model, long SiteId)
        {
            try
            {


                var state = new District
                {
                    Id = model.Id,
                    Location = model.Location,
                    StateOrProvinceId = model.StateOrProvinceId,
                    Type = model.Type,
                    Status = Entities.Enum.GeneralEnums.EStatus.Published

                };

                _DistrictRepository.Add(state);
                await _DistrictRepository.SaveChangesAsync();
                var DistrictDetail = new List<DistrictDetails>();

                if (model.DistrictDetails.Count() > 0)
                {
                    DistrictDetail = model.DistrictDetails.Select(x => new DistrictDetails
                    {
                        DistrictId = state.Id,
                        Name = x.Name,
                        CultureId = x.CultureId,


                    }).ToList();
                    districtDetailsRepository.AddRange(DistrictDetail);
                    await districtDetailsRepository.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> DeleteDistrict(long Id)
        {
            try
            {

                var district = await _DistrictRepository.Query().FirstOrDefaultAsync(x => x.Id == Id);

                district.Status = Entities.Enum.GeneralEnums.EStatus.Deleted;
                await _DistrictRepository.SaveChangesAsync();

                return true;

            }
            catch (Exception)
            {

                throw;
            }
        }


    }
}
