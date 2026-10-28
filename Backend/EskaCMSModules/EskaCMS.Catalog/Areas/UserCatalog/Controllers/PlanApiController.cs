using EskaCMS.Infrastructure.Web.SmartTable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Entities.Entity;
using System.Linq;
using Newtonsoft.Json;
using static EskaCMS.Entities.Enum.GeneralEnums;
using System.Collections.Generic;
using EskaCMS.Core.Extensions;
using EskaCommerce.UserCatalog.Dtos;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Helpers;
using System.Text;
using EskaCommerce.UserCatalog.Services.Interfaces;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.Catalog.Areas.UserCatalog.Controllers
{
    [Authorize]
    [Route("api/PlanApi")]
    [ApiController]

    public class PlanApiController : ControllerBase
    {
        private readonly IRepository<Plan> _planRepository;
        private readonly IPlanService _planServices;
        // private readonly IWorkContext _workContext;

        public PlanApiController(IRepository<Plan> planRepository, /* IWorkContext workContext, */ IPlanService planServices)
        {
            _planRepository = planRepository;
            _planServices = planServices;
            // _workContext = workContext;
        }


        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details([FromRoute]int id)
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.GetPlanDetails(id, siteId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("grid")]
        public async Task<IActionResult> List([FromBody] SmartTableParam param)
        {
            try
            {
                var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);

                var query = _planRepository
                .Query().Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted);

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.Id != null)
                    {
                        long id = search.Id;
                        query = query.Where(x => x.Id == id);
                    }
                    if (search.Name != null)
                    {
                        string name = search.Name;
                        query = query.Where(x => x.PlanDetails.Any(x=> x.NormalizedName.Contains(name.ToUpper())));
                    }
                    if (search.IsFeatured != null)
                    {
                        bool isFeatured = search.IsFeatured;
                        query = query.Where(x => x.IsFeatured == isFeatured);
                    }
                    if (search.IsTopPicked != null)
                    {
                        bool isTopPicked = search.IsTopPicked;
                        query = query.Where(x => x.IsTopPicked == isTopPicked);
                    }
                    if (search.PlanType != null)
                    {
                        var planType = (PlanTypes)search.PlanType;
                        query = query.Where(x => x.PlanType == planType);
                    }
                    if (search.RecurringType != null)
                    {
                        var recurringType = (RecurringTypes)search.RecurringType;
                        query = query.Where(x => x.RecurringType == recurringType);
                    }
                }

                var plans = query.ToSmartTableResult(param, plan => new PlanListVM
                {
                   Id =  plan.Id,
                    IsFeatured =  plan.IsFeatured,
                    IsTopPicked =  plan.IsTopPicked,
                    Status =  plan.Status,
                    PlanDetails =plan.PlanDetails.Select(x=> new PlanCultureDetailsVm { Name = x.Name, Description = x.Description , ShortDescription=x.ShortDescription , CultureCode=x.Culture.CultureId}).ToList(),
                    PlanType= plan.PlanType,
                    RecurringType =   plan.RecurringType
                    ,IsCustomPlan=plan.IsCustomPlan
                });

                return Ok(plans);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var plan = _planRepository.Query().FirstOrDefault(x => x.Id == id);
                plan.Status = EStatus.Deleted;
                await _planRepository.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] PlanUpdate category)
        {
            try
            {
                await _planServices.Update(category);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] PlanCreate category)
        {
            try
            {
                var siteId = Convert.ToInt64(Request.Headers["SiteId"]);
                return Ok(await _planServices.Create(category, siteId));

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("plan-types")]
        public IActionResult PlanTypes()
        {
            var enumVals = new List<object>();

            foreach (var item in Enum.GetValues(typeof(PlanTypes)))
            {

                enumVals.Add(new
                {
                    id = (int)item,
                    name = ((PlanTypes)item).GetDisplayName()
                });
            }

            return Ok(enumVals);

        }
        [HttpGet("plan-recurring-types")]
        public IActionResult RecurringTypes()
        {
            var enumVals = new List<object>();

            foreach (var item in Enum.GetValues(typeof(RecurringTypes)))
            {

                enumVals.Add(new
                {
                    id = (int)item,
                    name = ((RecurringTypes)item).GetDisplayName()
                });
            }

            return Ok(enumVals);

        }

        [HttpPost("export")]
        public async Task<ActionResult<PlanExportVm>> Export([FromBody] SmartTableParam param)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);

            var query = _planRepository
            .Query().Where(x => x.SiteId == SiteId && x.Status != EStatus.Deleted);

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.Id != null)
                {
                    long id = search.Id;
                    query = query.Where(x => x.Id == id);
                }
                if (search.Name != null)
                {
                    string name = search.Name;
                   // query = query.Where(x => x.Name.Contains(name));
                }
                if (search.IsFeatured != null)
                {
                    bool isFeatured = search.IsFeatured;
                    query = query.Where(x => x.IsFeatured == isFeatured);
                }
                if (search.IsTopPicked != null)
                {
                    bool isTopPicked = search.IsTopPicked;
                    query = query.Where(x => x.IsTopPicked == isTopPicked);
                }
                if (search.PlanType != null)
                {
                    var planType = (PlanTypes)search.PlanType;
                    query = query.Where(x => x.PlanType == planType);
                }
            }


            var plans = await query
                .Select(x => new PlanExportVm
                {
                    Id = x.Id,
                   // Name = x.Name,
                    PlanType = x.PlanType,
                    //Description = x.Description,
                    //ShortDescription = x.ShortDescription,
                    IsFeatured = x.IsFeatured,
                    IsTopPicked = x.IsTopPicked,
                    IsCustomPlan = x.IsCustomPlan,
                    RecurringType = x.RecurringType,
                    CreatedBy = x.CreatedBy.FullName,
                    CreationDate = x.CreationDate
                })
                .ToListAsync();


            var csvString = CsvConverter.ExportCsv(plans);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "plans-export.csv");
        }

        //[HttpPost("GetAll")]
        //public async Task<IActionResult> GetAll([FromBody] SmartTableParam input)
        //{
        //    try
        //    {
        //        return Ok(await _planServices.GetAll(input));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

    }
}
