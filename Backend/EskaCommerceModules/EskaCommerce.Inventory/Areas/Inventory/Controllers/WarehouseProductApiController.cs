using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Entities.Entity.Catalog;
using EskaCommerce.Inventory.Areas.Inventory.ViewModels;
using EskaCommerce.Inventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Inventory;

namespace EskaCommerce.Inventory.Areas.Inventory.Controllers
{
    [Area("Inventory")]
    [Authorize]
    [Route("api/warehouses")]
    public class WarehouseProductApiController : Controller
    {
        private readonly IRepository<Warehouse> _warehouseRepository;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Stock> _stockRepository;
        private readonly IStockService _stockService;

        public WarehouseProductApiController(IRepository<Warehouse> warehouseRepository, IWorkContext workContext, IRepository<Product> productRepository, IRepository<Stock> stockRepository, IStockService stockService)
        {
            _warehouseRepository = warehouseRepository;
            _workContext = workContext;
            _productRepository = productRepository;
            _stockRepository = stockRepository;
            _stockService = stockService;
        }

        [HttpPost("{warehouseId}/products")]
        public async Task<ActionResult<SmartTableResult<MangeWarehouseProductItemVm>>> GetProducts(long warehouseId, [FromBody] SmartTableParam param)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            var warehouse = _warehouseRepository.Query().AsNoTracking().Where(s => s.SiteId == SiteId).FirstOrDefault(x => x.Id == warehouseId);
            if (warehouse == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("admin") && warehouse.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this warehouse" });
            }

            var query = _productRepository
                .Query()
                .Include(x=>x.ProductDetails)
                .Where(x => !x.HasOptions && x.VendorId == warehouse.VendorId && x.SiteId == SiteId &&x.Status!=EStatus.Deleted);

            var joinedQuery = query.GroupJoin
                (
                    _stockRepository.Query().Where(x => x.WarehouseId == warehouseId),
                    product => product.Id, stock => stock.ProductId,
                    (product, stock) => new { product, stock }
                )
                .SelectMany(x => x.stock.DefaultIfEmpty(), (x, stock) => new MangeWarehouseProductItemVm
                {
                    Id = x.product.Id,
                    Name = x.product.ProductDetails.OrderByDescending(x=>x.CultureId).Select(e=>e.Name).FirstOrDefault(),
                    Sku = x.product.Sku,
                    Quantity = stock.Quantity
                });

            if (param.Search.PredicateObject != null)
            {
                dynamic search = param.Search.PredicateObject;

                if (search.ProductName != null)
                {
                    string productName = search.ProductName;
                    joinedQuery = joinedQuery.Where(x => x.Name.Contains(productName));
                }

                if (search.ProductSku != null)
                {
                    string productSku = search.productSku;
                    joinedQuery = joinedQuery.Where(x => x.Sku.Contains(productSku));
                }

                if (search.IsExistInWarehouse != null)
                {
                    bool isExistInWarehouse = search.IsExistInWarehouse;
                    if (isExistInWarehouse)
                    {
                        joinedQuery = joinedQuery.Where(x => x.Quantity != null);
                    }
                    else
                    {
                        joinedQuery = joinedQuery.Where(x => x.Quantity == null);
                    }
                }
            }

            var products = joinedQuery.ToSmartTableResult(param);
            return Ok(products);
        }

        [HttpPost("{warehouseId}/add-products")]
        public async Task<IActionResult> AddProducts(long warehouseId, [FromBody] IList<long> productIds)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            var warehouse = _warehouseRepository.Query().FirstOrDefault(x => x.Id == warehouseId);
            if (warehouse == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("admin") && warehouse.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this warehouse" });
            }
            // Fix for ef core 3.0
            var productIdsArray = productIds.ToArray();

            var existedProducIds = await _stockRepository.Query()
                .Where(x => x.WarehouseId == warehouseId && productIdsArray.Contains(x.ProductId))
                .Select(x => x.ProductId).ToListAsync();
            foreach (var id in existedProducIds)
            {
                productIds.Remove(id);
            }

            var stocks = productIds.Select(x => new Stock
            {
                ProductId = x,
                WarehouseId = warehouseId,
                Quantity = 0,
                Status=EStatus.Published
            });

            _stockRepository.AddRange(stocks);
            await _stockRepository.SaveChangesAsync();
            return Accepted();
        }

        [HttpPost("{warehouseId}/add-all-products")]
        public async Task<IActionResult> AddAllProducts(long warehouseId)
        {
            var SiteId = Convert.ToInt64(Request.Headers["SiteId"]);
            var currentUser = await _workContext.GetCurrentUser();
            var warehouse = _warehouseRepository.Query().FirstOrDefault(x => x.Id == warehouseId);
            if (warehouse == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("admin") && warehouse.VendorId != currentUser.VendorId)
            {
                return BadRequest(new { error = "You don't have permission to manage this warehouse" });
            }

            await _stockService.AddAllProduct(warehouse);
            return Accepted();
        }
    }
}


