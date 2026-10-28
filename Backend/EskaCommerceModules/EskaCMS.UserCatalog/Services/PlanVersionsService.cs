using AutoMapper;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Entities.Entity.Catalog;

using EskaCommerce.UserCatalog.Dtos;
using EskaCMS.Entities.Entity;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.UserCatalog.Services
{
    public class PlansVersionsService : IPlansVersionsService
    {
        private readonly DateTimeOffset TimeNow = DateTimeOffset.Now;

        private readonly IRepository<PlansVersions> _plansVersionsRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IMapper _mapper;
        private readonly IWorkContext _workContext;
        public PlansVersionsService(
            IRepository<PlansVersions> plansVersionsRepository,
            IRepository<Product> productRepository,
            IWorkContext workContext,
            IMapper mapper
            )
        {
            _plansVersionsRepository = plansVersionsRepository;
            _productRepository = productRepository;
            _workContext = workContext;
            _mapper = mapper;
        }
        public async Task<long> Create(PlansVersionsCreate plansVersionsInput, long siteId)
        {
            try
            {

                PlansVersions plansVersions = _mapper.Map<PlansVersions>(plansVersionsInput);
                plansVersions.CreatedById = _workContext.GetCurrentUserIdValue();
                plansVersions.CreationDate = TimeNow;
                plansVersions.VersionId = GetNextVersionId(plansVersionsInput.PlanId);
                _plansVersionsRepository.Add(plansVersions);
                await _plansVersionsRepository.SaveChangesAsync();
                return plansVersions.Id;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public PlansVersionsOutput Get(long id)
        {
            PlansVersions plansVersions = GetById(id);
            PlansVersionsOutput plansVersionsOutput = _mapper.Map<PlansVersionsOutput>(plansVersions);
            plansVersionsOutput.Plan = _mapper.Map<PlanOutput>(plansVersions.Plan);
            plansVersionsOutput.PlanProducts = _mapper.Map<IList<PlanProductVersionOutput>>(plansVersions.PlanProductVersions);

            return plansVersionsOutput;
        }

        //for admin
        public async Task<PlansVersionsVM> PlanVersionDetails(long id)
        {
            PlansVersions plansVersions = GetById(id);
            var plansVersionsOutput = _mapper.Map<PlansVersionsVM>(plansVersions);


            plansVersionsOutput.PlanProducts = plansVersions.PlanProductVersions
                                                            .Select(x => new PlanProductVersionVM
                                                            {
                                                                Id = x.Id,
                                                                Price = x.Product.Price.HasValue? x.Product.Price.Value:0,
                                                                Quantity = x.Quantity,
                                                                Status = x.Status,
                                                                PlanVersionId = x.PlanVersionId,
                                                                ProductCultureDetails = x.Product.ProductDetails.Select(x => new PlanProductVM
                                                                {
                                                                    Name = x.Name,

                                                                }).ToList()
                                                            }).ToList();

            return plansVersionsOutput;
        }
        public async Task<SmartTableResult<PlansVersionsOutput>> GetPlanVersions(long planId)
        {
            IList<PlansVersions> plansVersionss = await _plansVersionsRepository.Query()
                .Where(x => x.PlanId == planId)
                .ToListAsync();

            return new SmartTableResult<PlansVersionsOutput>
            {
                Items = _mapper.Map<IEnumerable<PlansVersionsOutput>>(plansVersionss),
                TotalRecord = _plansVersionsRepository.Query().Count()
            };
        }



        private PlansVersions GetById(long id)
        {
            return _plansVersionsRepository.Query()
                                           .Include(x => x.Plan)
                                           .ThenInclude(x => x.PlanDetails)
                                           .Include(x => x.PlanProductVersions)
                                           .ThenInclude(x => x.Product)
                                           .ThenInclude(x => x.ProductDetails).FirstOrDefault(x => x.Id == id);
        }
        public long GetLatestVersionId(long planId)
        {
            IList<PlansVersions> plansVersions = _plansVersionsRepository.Query().Where(x => x.PlanId == planId).ToList();

            return plansVersions.Last().Id;
        }
        private int GetNextVersionId(long planId)
        {
            IList<PlansVersions> plansVersions = _plansVersionsRepository.Query().Where(x => x.PlanId == planId).ToList();

            return plansVersions.Count > 0 ? plansVersions.Last().VersionId + 1 : 1;
        }

        public async Task<bool> Delete(long id)
        {
            try
            {
                var planVersion = _plansVersionsRepository
                                                     .Query()
                                                     .Include(x => x.UserPlans)
                                                     .FirstOrDefault(x => x.Id == id);
                if (planVersion.UserPlans.Count() > 0)
                {
                    throw new Exception("Can not remove this Version");
                }
                _plansVersionsRepository.Remove(planVersion);
                await _plansVersionsRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
