using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Orders;
using EskaCMS.Core.Extensions;
using EskaCommerce.Orders.Areas.Orders.ViewModels;
using EskaCMS.Infrastructure.Helpers;

namespace EskaCommerce.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]
    [Authorize]
    public class OrderHistoryApiController : Controller
    {
        private readonly IRepository<OrderHistory> _orderHistoryRepository;
        private readonly IWorkContext _workContext;

        public OrderHistoryApiController(IRepository<OrderHistory> orderHistoryRepository, IWorkContext workContext)
        {
            _orderHistoryRepository = orderHistoryRepository;
            _workContext = workContext;
        }

        [HttpGet("api/orders/{orderId}/history")]
        public async Task<IActionResult> Get(long orderId)
        {
            var siteId = _workContext.GetCurrentSiteId();
            var histories = await _orderHistoryRepository.Query()
                .Where(x => x.OrderId == orderId && x.Order.SiteId == siteId)
                .Include(x=>x.CreatedBy)
                .Select(x => new
                {
                    OldStatus = x.OldStatus.ToString(),
                    NewStatus = x.NewStatus.ToString(),
                    CreatedByFullName = x.CreatedBy.FullName,
                    x.Note,
                    x.CreatedOn
                }).ToListAsync();

            return Ok(histories);
        }

        [HttpPost("api/orders/history")]
        public async Task<IActionResult> Get([FromBody] OrderIdVM Model)
        {
            string decryptedOrderId = EncryptDecryptHelper.Decrypt(Model.orderId);
            var orderId = Convert.ToInt64(decryptedOrderId);
            var siteId = _workContext.GetCurrentSiteId();
            var histories = await _orderHistoryRepository.Query()
                .Where(x => x.OrderId == orderId && x.Order.SiteId == siteId)
                .Include(x => x.CreatedBy)
                .Select(x => new
                {
                    OldStatus = x.OldStatus.ToString(),
                    NewStatus = x.NewStatus.ToString(),
                    CreatedByFullName = x.CreatedBy.FullName,
                    x.Note,
                    x.CreatedOn
                }).ToListAsync();

            return Ok(histories);
        }
    }

}
