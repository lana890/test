using AutoMapper;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.UserCatalog.Dtos;
using EskaCMS.Entities.Entity;

using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.UserCatalog.Services
{
    public class PlanProductVersionService : IPlanProductVersionService
    {
        private readonly DateTimeOffset TimeNow = DateTimeOffset.Now;

        private readonly IRepository<PlanProductVersion> _planProductVersionRepository;
        private readonly IRepository<Plan> _planRepository;
        private readonly IRepository<PlansVersions> _planVersionsRepository;

        private readonly IMapper _mapper;
        private readonly IWorkContext _workContext;
        private readonly IRepository<UserPlans> userPlanRepository;

        public PlanProductVersionService(
            IRepository<PlanProductVersion> planProductVersionRepository,
            IWorkContext workContext,
            IRepository<UserPlans> UserPlanRepository,
            IMapper mapper,
            IRepository<Plan> planRepository,
            IRepository<PlansVersions> planVersionsRepository

            )
        {
            _planProductVersionRepository = planProductVersionRepository;
            _workContext = workContext;
            userPlanRepository = UserPlanRepository;
            _mapper = mapper;
            _planRepository = planRepository;
            _planVersionsRepository = planVersionsRepository;
            _planRepository = planRepository;
        }
        public async Task<IActionResult> Create(PlanProductVersionCreate planProductVersionInput)
        {
            var UserPlan = await userPlanRepository.Query().Where(x => x.PlanVersionsId == planProductVersionInput.PlanVersionId).ToListAsync();
            if (UserPlan != null && UserPlan.Count() > 0)
            {
                throw new Exception("Plan Version Already Used You Can Not Add Product");
            
            }

            PlanProductVersion oldExists = _planProductVersionRepository.Query().FirstOrDefault(x => x.ProductId == planProductVersionInput.ProductId && x.PlanVersionId == planProductVersionInput.PlanVersionId);
            if (oldExists == null)
            {
                PlanProductVersion planProductVersion = _mapper.Map<PlanProductVersion>(planProductVersionInput);
                planProductVersion.Status = planProductVersionInput.Status;
                _planProductVersionRepository.Add(planProductVersion);
                await _planProductVersionRepository.SaveChangesAsync();
                return new OkObjectResult(new { });
            }
            throw new Exception("This product already exist in this version");
        }

        public async Task<List<PlanProductVersionOutput>> GetProductsByPlan(long PlanId)
        {
            var PlanVersion = _planVersionsRepository.Query().Where(x => x.PlanId == PlanId).Max(p => p.Id);
            return _planProductVersionRepository.Query().Include(x => x.Product).Include(x => x.PlanVersion).Where(x => x.PlanVersionId == PlanVersion && x.Status == EStatus.Active).Select(x => new PlanProductVersionOutput
            {
                Id = x.Id,
                PlanVersionId = x.PlanVersionId,
                Price = x.Product.Price.HasValue ? x.Product.Price.Value : 0,
                ProductId = x.ProductId,
                Product = x.Product,
                Status = x.Status,
                Quantity = x.Quantity
            }).ToList();


        }

        public PlanProductVersionOutput Get(long id)
        {
            PlanProductVersion planProductVersion = GetById(id);
            PlanProductVersionOutput planProductVersionOutput = _mapper.Map<PlanProductVersionOutput>(planProductVersion);
            planProductVersionOutput.PlanVersion = _mapper.Map<PlansVersionsOutput>(planProductVersion.PlanVersion);
            planProductVersionOutput.Product = _mapper.Map<Product>(planProductVersion.Product);
            return planProductVersionOutput;
        }

        public async Task Update(PlanProductVersionUpdate planProductVersionInput)
        {
            PlanProductVersion planProductVersion = GetById(planProductVersionInput.Id);
            if (planProductVersionInput.PlanVersionId.HasValue)
            {
                planProductVersion.PlanVersionId = (long)planProductVersionInput.PlanVersionId;
            }
          
            if (planProductVersionInput.ProductId.HasValue)
            {
                planProductVersion.ProductId = (long)planProductVersionInput.ProductId;
            }
            if (planProductVersionInput.Quantity.HasValue)
            {
                planProductVersion.Quantity = (int)planProductVersionInput.Quantity;
            }
            if (planProductVersionInput.Status.HasValue)
            {
                planProductVersion.Status = (EStatus)planProductVersionInput.Status;
            }
            await _planProductVersionRepository.SaveChangesAsync();
        }
        public async Task<SmartTableResult<PlanProductVersionOutput>> GetAll(SmartTableParam input)
        {
            IList<PlanProductVersion> planProductsVersions = await _planProductVersionRepository.Query()
               .Where(x => x.Status != EStatus.Inactive)
               .Include(x => x.PlanVersion)
               .Include(x => x.Product)
               .Skip((input.Pagination.Start - 1) * input.Pagination.Number)
               .Take(input.Pagination.Number)
               .ToListAsync();

            return new SmartTableResult<PlanProductVersionOutput>
            {
                Items = _mapper.Map<IEnumerable<PlanProductVersionOutput>>(planProductsVersions),
                NumberOfPages = input.Pagination.Number,
                TotalRecord = _planProductVersionRepository.Query().Count()
            };
        }


        private PlanProductVersion GetById(long id)
        {
            return _planProductVersionRepository.Query().Where(x => x.Id == id).Include(x => x.Product).Include(x => x.PlanVersion).FirstOrDefault();
        }

    }
}
