using EskaCMS.Core.Areas.Core.Interfaces;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCommerce.LoyaltyPoints.Models.Request;
using EskaCommerce.LoyaltyPoints.Services.Interface;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCommerce.Orders.Enum;
using EskaCommerce.Orders.ModelVM;
using EskaCommerce.Orders.ModelVM.Request;
using EskaCommerce.Orders.Services.CustomersService;
using EskaCommerce.Orders.Services.CustomersService.Interface;
using EskaCommerce.Orders.Services.OrderAddressServices.Interfaces;
using Hangfire.MemoryStorage.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Orders.Areas.Orders.Controllers
{
    [Route("api/Customers")]
    [ApiController]
    [Authorize]

    public class CustomersApiController:ControllerBase
    {
       private ICustomerService _customerService;
        private ILoyaltyWalletService _loyalityService;
        // private ICustomerGroup _customergroup;

        public CustomersApiController(
            ICustomerService customerService,
            ILoyaltyWalletService loyalityService/* ,
            ICustomerGroup customergroup */)

        {
            // _customergroup= customergroup;
            _customerService = customerService;
            _loyalityService = loyalityService;
        }
        [HttpPost("Customers")]
        public async Task<IActionResult> GetCustomers(
       [FromBody] CustomerFilteration filter)
        {
            try
            {
                var (data, totalCount) = await _customerService.GetCustomerList(
                    filter.searchTerm,
                    filter.dateFrom,
                    filter.dateTo,
                    filter.status,
                    filter.page,
                    filter.limit,
                    filter.sortBy,
                    filter.sortOrder
                );

                return Ok(new
                {
                    TotalRecords = totalCount,
                    Page =filter.page,
                    Limit = filter.limit,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost("CustomerBulkAction")]
        public async Task<IActionResult> CustomerBulkActions([FromBody]CustomerActions customerAction)
        {
            bool result = false;
            try
            {
                switch (customerAction.action)
                {

                    case CustomerActionEnum.Create_Group:
                      result=await _customerService.CreateAndAssignGroup(customerAction);
                        break;

                  
                    case CustomerActionEnum.Send_Email:
                         result=await _customerService.SendEmailToBulkUsers(customerAction);
                        break;                       
                    case CustomerActionEnum.Assign_Coupon:
                        result = await _customerService.addCouponToGroup(customerAction);
                        break;
                    case CustomerActionEnum.Add_points:
                        {
                            string pointsRequest = customerAction.Payload.ToString();
                            var pointsRequestObj = JsonConvert.DeserializeObject<AddPointsDto>(pointsRequest);
                            var success=await _loyalityService.CreateLoyalityWallletToBulk(customerAction.CustomerIds, pointsRequestObj);
                            return Ok(success?"Point Were Added to Customers":"Faild to add Points");
                        }
                    

                }
                return Ok(result);

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromBody] CustomerActions customerAction)
        {
            try
            {
               
                    var users = await _customerService.GetUsers(customerAction);
                    var file = await ExcelHelper.ExportExcelFile(users, "UsersFile");
                    return File(file.FileBytes, file.ContentType, file.Name);
                
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
    }

// export

//body  related get list 