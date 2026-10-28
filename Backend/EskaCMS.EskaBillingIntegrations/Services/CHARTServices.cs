using AutoMapper;
using AutoMapper.QueryableExtensions;
using EskaCMS.Entities.Entity;
using EskaCMS.EskaBilling.Models;
using EskaCMS.EskaBilling.Models.response;
using EskaCMS.EskaBilling.Services.Interfaces;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Services
{
    public class CHARTServices : ICHARTServices
    {
        private readonly IRepository<CustomerSims> customerSimsRepository;
        private readonly IRepository<SIMUsage> _SIMUsageRepo;
        private readonly IMapper mapper;


        public CHARTServices(IRepository<CustomerSims> CustomerSimsRepository
                             , IRepository<SIMUsage> SIMUsageRepo
                             , IMapper mapper)
        {
            customerSimsRepository = CustomerSimsRepository;
            _SIMUsageRepo = SIMUsageRepo;
            this.mapper = mapper;
        }

        public async Task<List<ChartViewModel>> GetSimsStatuses(long AccountId , string SimTag)
        {
            try
            {
                
                var query =  customerSimsRepository
                                                .Query()
                                                .Where(x => x.AccountId == AccountId && !x.IsDeleted);
                                                



                if(!string.IsNullOrEmpty(SimTag))
                {
                    query = query.Where(x => x.SIMTag.Contains(SimTag));
                }



                var result = query.GroupBy(x => x.Status)
                               .Select(x => new ChartViewModel
                               {
                                   status = x.Key,
                                   RateChanges = x.Count()
                               }).ToList();




                return result;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        public async Task<List<GetSIMUsageResponseVM>> GetSIMUsage(GetSIMUsageFilterVM model)
        {
            try
            {
                return await _SIMUsageRepo
                                .Query()
                                .Where(x => x.CustomerSimsId == model.SIMId &&
                                            x.CreationDate.Date <= model.To.Date &&
                                            x.CreationDate.Date >= model.From.Date)
                                .AsNoTrackingWithIdentityResolution()
                                .ProjectTo<GetSIMUsageResponseVM>(mapper.ConfigurationProvider)
                                .OrderBy(x=>x.CreationDate)
                                .ToListAsync();


            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<List<GetAllSimUsageResponseVM>> GetAllSIMUsage(GetAllSIMUsageFilterVM model)
        {
            try
            {
                var simsUsage =  await _SIMUsageRepo
                                .Query()
                                .Where(x => !x.CustomerSims.IsDeleted &&
                                            x.CustomerSims.AccountId == model.AccountId && 
                                            x.CreationDate.Date <= model.To.Date &&
                                            x.CreationDate.Date >= model.From.Date && (!string.IsNullOrEmpty(model.SimTag) ? x.CustomerSims.SIMTag.Contains(model.SimTag) : true))
                                .Include(x=>x.CustomerSims)
                                .ThenInclude(x => x.Product)
                                .Include("Product.AttributeValues")
                                .Include("Product.AttributeValues.Attribute")
                                .AsNoTrackingWithIdentityResolution()
                                .ToListAsync();

                

                
                var groupedUsage =  simsUsage
                                .GroupBy(x=>x.CreationDate.Date)
                                .Select(x=> new GetAllSimUsageResponseVM { 
                                     CreationDate=x.Key,
                                     SimsUsage =mapper.Map<List<GetSIMUsageResponseVM>>(x.ToList()),
                                })
                                .OrderBy(x => x.CreationDate)
                                .ToList();


                return groupedUsage;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

    }
}
