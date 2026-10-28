using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Core.Extensions;
using EskaCommerce.Inventory.Services;
using EskaCommerce.Inventory.Areas.Inventory.ViewModels;
using System;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Inventory;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Core.Services.Interfaces;

namespace EskaCommerce.Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    [Authorize]
    [Route("api/stocks")]
    public class StockApiController : Controller
    {
        private readonly IRepository<Stock> _stockRepository;
        private readonly IStockService _stockService;
        // private readonly IRepository<SitesCultures> sitesCulture;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IUserSiteService userSiteService;
        private readonly IRepository<StockHistory> _stockHistoryRepository;

        public StockApiController(IRepository<Stock> stockRepository,
            IStockService stockService,
            /* IRepository<SitesCultures> SitesCulture, */
            IWorkContext workContext, IRepository<Warehouse> warehouseRepository,IUserSiteService userSiteService ,IRepository<StockHistory> stockHistoryRepository)
        {
            _stockRepository = stockRepository;
            _stockService = stockService;
            // sitesCulture = SitesCulture;
            _workContext = workContext;
            _warehouseRepository = warehouseRepository;
            this.userSiteService=userSiteService;
            _stockHistoryRepository = stockHistoryRepository;
        }

        [HttpPost("grid")]
        public async Task<IActionResult> List(long warehouseId, [FromBody] SmartTableParam param)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            var warehouse = await _warehouseRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == warehouseId);
            if (warehouse == null)
            {
                return NotFound();
            }
            var userRoles = await userSiteService.GetUserRoles(SiteId, currentUser.Id);

            var query = _stockRepository.Query().Include(x=>x.Warehouse)
                .ThenInclude(x=>x.WarehouseRoles)

                .Include(x=>x.Product).ThenInclude(x=>x.ProductDetails).Include(x=>x.Product).ThenInclude(x=>x.OptionCombinations).ThenInclude(x=>x.Value).ThenInclude(x=>x.productOptionValueDetails)
                .Where(x => x.Warehouse.WarehouseRoles.Any() ? x.Warehouse.WarehouseRoles.Any(y => userRoles.Contains(y.RoleId)) : true)
                .Where(x => x.WarehouseId == warehouseId
                && x.Product.Status != EskaCMS.Entities.Enum.GeneralEnums.EStatus.Deleted && !x.Product.HasOptions);

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;

                if (search.ProductName != null)
                {
                    string productName = search.ProductName;
                    query = query.Where(x => x.Product.ProductDetails.Any(c=>c.Name.Contains(productName)));
                }

                if (search.ProductSku != null)
                {
                    string productSku = search.productSku;
                    query = query.Where(x => x.Product.Sku.Contains(productSku));
                }
            }

            var products = query.ToSmartTableResult(
                param,
                x => new
                {
                    x.Id,
                    x.ProductId,
                    ProductName = x.Product.ProductDetails.OrderByDescending(x=>x.CultureId).Select(x=>x.Name).FirstOrDefault(),
                    ProductSku = x.Product.Sku,
                    x.Quantity,
                    AdjustedQuantity = 0,
                    x.Status
                });

            return Ok(products);
        }

        [HttpPut]
        public async Task<IActionResult> Put(long warehouseId, [FromBody] IList<StockVm> stockVms)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            var warehouse = await _warehouseRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == warehouseId);
            if (warehouse == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("admin") && warehouse.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this warehouse" });
            }

            foreach (var item in stockVms)
            {
                if (item.AdjustedQuantity == 0)
                {
                    continue;
                }

                var stockUpdateRequest = new StockUpdateRequest
                {
                    WarehouseId = warehouseId,
                    ProductId = item.ProductId,
                    AdjustedQuantity = item.AdjustedQuantity,
                    Note = item.Note,
                    UserId = currentUser.Id,
                };

                await _stockService.UpdateStock(stockUpdateRequest);
            }

            return Accepted();
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetStockHistory(int warehouseId, int productId)
        {
            var query = _stockHistoryRepository.Query();
            query = query.Where(x => x.WarehouseId == warehouseId && x.ProductId == productId);
            var stockHistory = await query.Select(x => new
            {
                x.Id,
                Name= x.Product.ProductDetails.OrderByDescending(x => x.CultureId).Select(c=>c.Name).FirstOrDefault()??"",
                x.CreatedOn,
                x.CreatedBy.FullName,
                x.AdjustedQuantity,
                x.Note
            }).ToListAsync();

            return Ok(stockHistory);
        }


        [HttpGet]
        [Route("GetAvailableStockByProductId")]
        public async Task<IActionResult> GetAvailableStockByProductId(long ProductId)
        {

            try
            {
                return Ok(await _stockService.GetAvailableStockByProductId(ProductId));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("UpsertProductStock")]
        public async Task<IActionResult> UpsertProductStock([FromBody] List<AddEditProductStockViewModel> model)
        {

            try
            {
                return Ok(await _stockService.UpsertProductStock(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("ChangeStockStatus")]
        public async Task<IActionResult> ChangeStockStatus([FromBody] StockStatusViewModel model)
        {

            try
            {
                return Ok(await _stockService.ChangeStockStatus(model));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }
        }
    }
}


