using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.UserCatalog.Dtos;
using EskaCommerce.UserCatalog.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.UserCatalog;

namespace EskaCommerce.Catalog.Areas.UserCatalog.Controllers
{
    [Route("api/user-plan")]
    public class UserPlansApiController : Controller
    {
        private readonly IRepository<UserPlans> _userPlanRepository;
        // private readonly IRepository<Plan> _planRepository;
        // private readonly IUserPlansService _planServices;
        private readonly IUserPlansApiService _planApiService;
        public UserPlansApiController(
            /* IUserPlansService planServices, */ 
            IRepository<UserPlans> userPlanRepository, 
            /* IRepository<Plan> planRepository, */
            IUserPlansApiService planApiService)
        {
            // _planServices = planServices;
            _userPlanRepository = userPlanRepository;
            // _planRepository = planRepository;
            _planApiService = planApiService;
        }

        [HttpPost("grid")]
        public async Task<IActionResult> List([FromBody] SmartTableParam param)
        {
            try
            {

                var query = _userPlanRepository.Query();

                if (param.Search.PredicateObject != null)
                {
                    dynamic search = param.Search.PredicateObject;
                    if (search.CustomerName != null)
                    {
                        string name = search.CustomerName;
                        query = query.Where(x => x.User.FullName.Contains(name));
                    }
                    //ActivationDate
                    if (search.ActivationDate != null)
                    {
                        if (search.ActivationDate.before != null)
                        {
                            DateTimeOffset before = search.ActivationDate.before;
                            query = query.Where(x => x.ActivationDate <= before);
                        }

                        if (search.ActivationDate.after != null)
                        {
                            DateTimeOffset after = search.ActivationDate.after;
                            query = query.Where(x => x.ActivationDate >= after);
                        }
                    }
                    //CurrentStatusDate
                    if (search.CurrentStatusDate != null)
                    {
                        if (search.CurrentStatusDate.before != null)
                        {
                            DateTimeOffset before = search.CurrentStatusDate.before;
                            query = query.Where(x => x.CurrentStatusDate <= before);
                        }

                        if (search.CurrentStatusDate.after != null)
                        {
                            DateTimeOffset after = search.CurrentStatusDate.after;
                            query = query.Where(x => x.CurrentStatusDate >= after);
                        }
                    }
                    //NextStatusDate
                    if (search.NextStatusDate != null)
                    {
                        if (search.NextStatusDate.before != null)
                        {
                            DateTimeOffset before = search.NextStatusDate.before;
                            query = query.Where(x => x.NextStatusDate <= before);
                        }

                        if (search.NextStatusDate.after != null)
                        {
                            DateTimeOffset after = search.NextStatusDate.after;
                            query = query.Where(x => x.NextStatusDate >= after);
                        }
                    }

                    if (search.CurrentStatus != null)
                    {
                        var currentStatus = (EUserPlansStatus)search.CurrentStatus;
                        query = query.Where(x => x.CurrentStatus == currentStatus);
                    }
                    if (search.NextStatus != null)
                    {
                        var nextStatus = (EUserPlansStatus)search.NextStatus;
                        query = query.Where(x => x.NextStatus == nextStatus);
                    }
                }

                var userPlans = query.ToSmartTableResult(param, userPlan => new
                {
                    userPlan.Id,
                    userPlan.User,
                    userPlan.ActivationDate,
                    userPlan.CurrentStatus,
                    userPlan.CurrentStatusDate,
                    userPlan.NextStatus,
                    userPlan.NextStatusDate,
                    userPlan.Order.OrderTotal
                });
                return Json(userPlans);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("users-plans-list")]
        public IActionResult GetFilteredUserPlans([FromBody] SearchTableParam<SubscribersListSearchVM> param)
        {
            try
            {
                GridOutputVM<SubscriberListVM> plans = _planApiService.GetFilteredUserPlans(param);
                return Ok(plans);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("user-plans-by-id/{userId}")]
        public IActionResult GetUserPlansById(long userId,[FromBody] SearchTableParam<UserPlansListSearchVM> param)
        {
            try
            {
                var userPlans = _planApiService.GetUserPlansById(userId, param);
                return Ok(userPlans);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("user-plans-status")]
        public IActionResult UserPlansStatus()
        {
            var enumVals = new List<object>();

            foreach (var item in Enum.GetValues(typeof(EUserPlansStatus)))
            {

                enumVals.Add(new
                {
                    id = (int)item,
                    name = item.ToString()
                });
            }

            return Ok(enumVals);

        }


        [HttpPost("export")]
        public async Task<ActionResult<UserPlanExportVm>> Export([FromBody] SmartTableParam param)
        {

            var query = _userPlanRepository.Query();

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.CustomerName != null)
                {
                    string name = search.CustomerName;
                    query = query.Where(x => x.User.FullName.Contains(name));
                }
                //ActivationDate
                if (search.ActivationDate != null)
                {
                    if (search.ActivationDate.before != null)
                    {
                        DateTimeOffset before = search.ActivationDate.before;
                        query = query.Where(x => x.ActivationDate <= before);
                    }

                    if (search.ActivationDate.after != null)
                    {
                        DateTimeOffset after = search.ActivationDate.after;
                        query = query.Where(x => x.ActivationDate >= after);
                    }
                }
                //CurrentStatusDate
                if (search.CurrentStatusDate != null)
                {
                    if (search.CurrentStatusDate.before != null)
                    {
                        DateTimeOffset before = search.CurrentStatusDate.before;
                        query = query.Where(x => x.CurrentStatusDate <= before);
                    }

                    if (search.CurrentStatusDate.after != null)
                    {
                        DateTimeOffset after = search.CurrentStatusDate.after;
                        query = query.Where(x => x.CurrentStatusDate >= after);
                    }
                }
                //NextStatusDate
                if (search.NextStatusDate != null)
                {
                    if (search.NextStatusDate.before != null)
                    {
                        DateTimeOffset before = search.NextStatusDate.before;
                        query = query.Where(x => x.NextStatusDate <= before);
                    }

                    if (search.NextStatusDate.after != null)
                    {
                        DateTimeOffset after = search.NextStatusDate.after;
                        query = query.Where(x => x.NextStatusDate >= after);
                    }
                }

                if (search.CurrentStatus != null)
                {
                    var currentStatus = (EUserPlansStatus)search.CurrentStatus;
                    query = query.Where(x => x.CurrentStatus == currentStatus);
                }
                if (search.NextStatus != null)
                {
                    var nextStatus = (EUserPlansStatus)search.NextStatus;
                    query = query.Where(x => x.NextStatus == nextStatus);
                }
            }

            var userPlans = await query
                .Select(x => new UserPlanExportVm
                {
                    Id = x.Id,
                    CustomerName = x.User.FullName,
                    ActivationDate = x.ActivationDate,
                    CurrentStatus = x.CurrentStatus,
                    CurrentStatusDate = x.CurrentStatusDate,
                    NextStatus = x.NextStatus,
                    NextStatusDate = x.NextStatusDate,
                    OrderTotal = x.Order.OrderTotal.ToString()
                })
                .ToListAsync();


            var csvString = CsvConverter.ExportCsv(userPlans);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "user-plans-export.csv");
        }

        [HttpPost("export-by-userId/{userId}")]
        public async Task<ActionResult<UserPlanExportVm>> ExportByUserId(long userId, [FromBody] SmartTableParam param)
        {

            var query = _userPlanRepository.Query()
                               .Include(x => x.PlanVersions).ThenInclude(x => x.Plan)
                               .Include(x => x.Order).ThenInclude(x => x.OrderItems).ThenInclude(x => x.Product)
                               .Where(x => x.UserId == userId);

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;
                if (search.PlanName != null)
                {
                    string planName = search.PlanName;
                    query = query.Where(x => x.PlanVersions.Plan.PlanDetails.Any(x=>x.Name.Contains(planName)));
                }
                //ActivationDate
                if (search.ActivationDate != null)
                {
                    if (search.ActivationDate.before != null)
                    {
                        DateTimeOffset before = search.ActivationDate.before;
                        query = query.Where(x => x.ActivationDate <= before);
                    }

                    if (search.ActivationDate.after != null)
                    {
                        DateTimeOffset after = search.ActivationDate.after;
                        query = query.Where(x => x.ActivationDate >= after);
                    }
                }
                //CurrentStatusDate
                if (search.CurrentStatusDate != null)
                {
                    if (search.CurrentStatusDate.before != null)
                    {
                        DateTimeOffset before = search.CurrentStatusDate.before;
                        query = query.Where(x => x.CurrentStatusDate <= before);
                    }

                    if (search.CurrentStatusDate.after != null)
                    {
                        DateTimeOffset after = search.CurrentStatusDate.after;
                        query = query.Where(x => x.CurrentStatusDate >= after);
                    }
                }
                //NextStatusDate
                if (search.NextStatusDate != null)
                {
                    if (search.NextStatusDate.before != null)
                    {
                        DateTimeOffset before = search.NextStatusDate.before;
                        query = query.Where(x => x.NextStatusDate <= before);
                    }

                    if (search.NextStatusDate.after != null)
                    {
                        DateTimeOffset after = search.NextStatusDate.after;
                        query = query.Where(x => x.NextStatusDate >= after);
                    }
                }

                if (search.CurrentStatus != null)
                {
                    var currentStatus = (EUserPlansStatus)search.CurrentStatus;
                    query = query.Where(x => x.CurrentStatus == currentStatus);
                }
                if (search.NextStatus != null)
                {
                    var nextStatus = (EUserPlansStatus)search.NextStatus;
                    query = query.Where(x => x.NextStatus == nextStatus);
                }
            }

            var userPlans = await query
                .Select(x => new UserPlanExportVm
                {
                    Id = x.Id,
                    CustomerName = x.User.FullName,
                    ActivationDate = x.ActivationDate,
                    CurrentStatus = x.CurrentStatus,
                    CurrentStatusDate = x.CurrentStatusDate,
                    NextStatus = x.NextStatus,
                    NextStatusDate = x.NextStatusDate,
                    OrderTotal = x.Order.OrderTotal.ToString()
                })
                .ToListAsync();


            var csvString = CsvConverter.ExportCsv(userPlans);
            var csvBytes = Encoding.UTF8.GetBytes(csvString);
            // MS Excel need the BOM to display UTF8 Correctly
            var csvBytesWithUTF8BOM = Encoding.UTF8.GetPreamble().Concat(csvBytes).ToArray();
            return File(csvBytesWithUTF8BOM, "text/csv", "user-plans-export.csv");
        }


    }
}
