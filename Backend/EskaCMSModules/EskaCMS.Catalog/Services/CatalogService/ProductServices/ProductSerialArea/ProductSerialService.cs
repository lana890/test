using EskaCMS.Core.Data;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Services;
using EskaCMS.Core.Services.Interface;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity.Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EskaCMS.Infrastructure.Helpers;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using EskaCMS.Core.Scripts.ECommerce;
using static EskaCMS.Entities.Enum.GeneralEnums;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Search;
using EskaCMS.DataAccess.Data;
using EskaCMS.Entities.Entity.Pricing;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductSerialArea.Interfaces;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductSerialArea
{
    public class ProductSerialService : IProductSerialService
    {
        private readonly IRepository<ProductSerial> _productSerialRepository;
        private readonly EskaDCMSDbContext _context;
        private readonly IWorkContext _workContext;
        private readonly IRepository<ProductSerialHistory> _productSerialHistory;
        private readonly IRepository<ProductCategory> productCategoryRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductLink> _productLinkRepository;

        public ProductSerialService(IRepository<ProductSerial> productSerialRepository, EskaDCMSDbContext context,
        IWorkContext workContext,
        IRepository<ProductSerialHistory> ProductSerialHistory,
        IRepository<ProductCategory> ProductCategoryRepository,
        
        
        IRepository<Product> productRepository, IRepository<ProductLink> productLinkRepository)
        {
            _productSerialRepository = productSerialRepository;
            _context = context;
            _workContext = workContext;
            _productSerialHistory = ProductSerialHistory;
            productCategoryRepository = ProductCategoryRepository;
            _productRepository = productRepository;
            _productLinkRepository = productLinkRepository;
        }



        public async Task<SmartTableResult<ProductSerialViewModel>> GetAllProductSerials(SmartTableParam Param)
        {
            //PaginationResult<ProductSerialViewModel> Products;
            DateTimeOffset ConvertTime = DateTime.Now;

            var Query = _productSerialRepository.Query()
               .Include(x => x.Product)
               .ThenInclude(x => x.Categories)
               .Where(x => x.Product.SiteId == _workContext.GetCurrentSiteId());

            if (Param.Pagination.Number <= 0)
            {
                Param.Pagination.Number = 10;
            }

            if (Param.Search.PredicateObject != null)
            {
                dynamic search = Param.Search.PredicateObject;
                if (search.Serial != null)
                {
                    string Serial = search.Serial;
                    Query = Query.Where(x => x.Code.Contains(Serial));
                }
                if (search.Status != "")
                {
                    string Status = search.Status;
                    Query = Query.Where(x => x.Status == (EProductSerialStatus)Enum.Parse(typeof(EProductSerialStatus), Status));
                }
                if (!string.IsNullOrEmpty(search.CategoryId.ToString()))
                {
                    long CategoryId = Convert.ToInt64(search.CategoryId);

                    var ProductCategory = await productCategoryRepository.Query().Include(x => x.Product).ThenInclude(x => x.ProductLinks).Where(x => x.CategoryId == CategoryId).SelectMany(x => x.Product.ProductLinks).AsNoTracking().ToListAsync();
                    if (ProductCategory.Count > 0)
                    {
                        Query = Query.Where(x => ProductCategory.Select(x => x.LinkedProductId).Contains(x.ProductId) || ProductCategory.Select(x => x.ProductId).Contains(x.ProductId));

                    }
                }
                if (!string.IsNullOrEmpty(search.ProductId.ToString()))
                {
                    long ProductId = Convert.ToInt64(search.ProductId);

                    var ProductLikes = await _productLinkRepository.Query().Where(x => x.ProductId == ProductId).AsNoTracking().ToListAsync();
                    if (ProductLikes.Count > 0)
                    {
                        Query = Query.Where(x => ProductLikes.Select(x => x.LinkedProductId).Contains(x.ProductId) || ProductLikes.Select(x => x.ProductId).Contains(x.ProductId));
                    }
                }





                if (search.CreationDate != null)
                {
                    if (search.CreationDate.CreatedOn != null)
                    {
                        if (search.CreationDate.CreatedOn.after != null)
                        {
                            DateTimeOffset after = search.CreationDate.CreatedOn.after;
                            Query = Query.Where(x => x.CreationDate >= after);
                        }

                        if (search.CreationDate.CreatedOn.before != null)
                        {
                            DateTimeOffset before = search.CreationDate.CreatedOn.before;
                            Query = Query.Where(x => x.CreationDate <= before.AddHours(23).AddMinutes(59).AddSeconds(59));
                        }



                    }

                }
            }

            var Products = Query.ToSmartTableResult(
                Param,
                Products => new ProductSerialViewModel
                {
                    Id = Products.Id,
                    CardNo = Products.Code,
                    BatchNo = Products.BatchNumber,
                    Balance = Products.Balance,
                    ControlNo = Products.ControlNo,
                    Expiry = Products.Expiry,
                    Status = Products.Status.ToString(),
                    ProductId = Products.Product.Id,
                    OrderId = Products.OrderId,
                }); ;

            return Products;


        }



        public async Task<bool> ChangeProductSerialStatus(long serialId, EProductSerialStatus NewStatus)
        {

            try
            {
                long CurrentUserId = await _workContext.GetCurrentUserId();
                var result = _productSerialRepository.Query().Where(x => x.Id == serialId).FirstOrDefault();
                 EProductSerialStatus OldStatus = result.Status;
                if (result.Status != EProductSerialStatus.Deleted && NewStatus == EProductSerialStatus.Deleted)
                {
                    result.Code = result.Code + "_Deleted_" + result.Id;


                }
                else if (result.Status == EProductSerialStatus.Deleted && NewStatus != EProductSerialStatus.Deleted)
                {
                    // restore case
                    result.Code = result.Code.Split("_Deleted_")[0];
                }
                result.Status = NewStatus;
                await _productSerialRepository.SaveChangesAsync();


                ProductSerialHistory history = new ProductSerialHistory()
                {
                    ProductSerialId = serialId,
                    CreationDate = DateTimeOffset.Now,
                    CreatedById = CurrentUserId,
                    Order = result.Order,
                    OldStatus = OldStatus,
                    NewStatus = result.Status,
                    Note = "Serial Status Changed",
                    OrderId = result.OrderId
                };

                _productSerialHistory.Add(history);
                await _productSerialHistory.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<bool> Create(List<ProductSerialViewModel> products, long productId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var DBTrans = await _context.Database.BeginTransactionAsync();
                try
                {

                    long CurrentUserId = await _workContext.GetCurrentUserId();
                    ProductSerial productSerial;
                    foreach (var item in products)
                    {
                        productSerial = new ProductSerial(item.CardNo)
                        {
                            BatchNumber = item.BatchNo,
                            ControlNo = item.ControlNo,
                            Status = EProductSerialStatus.Available,
                            Balance = item.Balance,
                            Expiry = item.Expiry,
                            ProductId = productId,
                            CreatedById = await _workContext.GetCurrentUserId(),
                            CreationDate = DateTimeOffset.Now
                        };
                        _productSerialRepository.Add(productSerial);
                    }

                    await _productSerialRepository.SaveChangesAsync();

                    var Query = await _productRepository.Query().Where(x => x.Id == productId).FirstOrDefaultAsync();

                    Query.IsProductSerial = true;
                    await _productRepository.SaveChangesAsync();

                   // await _productRepository.SQLQuery(ManageStockQueries.AddToProduct_StockQuantity, products.Count, productId);
                    var stockEffected = await _productRepository.SQLQuery(ManageStockQueries.AddToInventoryStock_StockQuantity, products.Count, productId);

                    if (stockEffected <= 0)
                    {
                        throw new Exception("There Is No Stock For Product With Id " + productId);
                    }
                    await _productRepository.SQLQuery(ManageStockQueries.AddToInventoryStock_StockHistory, productId, 1, DateTimeOffset.Now.ToString("yyyy-MM-dd hh:mm:ss"), CurrentUserId, products.Count);

                    await DBTrans.CommitAsync();


                    return true;
                }
                catch (Exception e)
                {
                    await DBTrans.RollbackAsync();
                    throw e;
                }
            });



        }

        public async Task<List<ProductSerialViewModel>> ImportFile(IFormFile file)
        {
            var List = new List<ProductSerialViewModel>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    ExcelPackage.LicenseContext = LicenseContext.Commercial;
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var CardNo = worksheet.Cells[row, 1];
                        var BatchNo = worksheet.Cells[row, 2];
                        var ControlNo = worksheet.Cells[row, 3];

                        if (string.IsNullOrWhiteSpace(CardNo.Value?.ToString()) ||
                            string.IsNullOrWhiteSpace(BatchNo.Value?.ToString()) ||
                            string.IsNullOrWhiteSpace(ControlNo.Value?.ToString())

                            )
                        {
                            continue;
                        }
                        List.Add(new ProductSerialViewModel
                        {
                            CardNo = worksheet.Cells[row, 1].Value.ToString().Replace("'", "").Trim(),
                            BatchNo = worksheet.Cells[row, 2].Value.ToString().Replace("'", "").Trim(),
                            ControlNo = worksheet.Cells[row, 3].Value.ToString().Replace("'", "").Trim(),
                            Status = EProductSerialStatus.Available.ToString(),
                            Expiry = worksheet.Cells[row, 5].Value.ToString().Trim(),
                            Balance = worksheet.Cells[row, 6].Value.ToString().Trim(),



                        });
                    }
                }
            }
            return List;
        }

        private IQueryable<ProductSerial> SortProducts(ProductSerialSearchSortVM search, IQueryable<ProductSerial> query)
        {
            if (search.SortField == ProductSerialSearchSortFieldsEnum.CardNo)
            {
                if (search.SortType == ESortType.asc)
                {
                    return query.OrderBy(x => x.Id);
                }
                else
                {
                    return query.OrderByDescending(x => x.ControlNo);
                }
            }
            if (search.SortField == ProductSerialSearchSortFieldsEnum.ControlNo)
            {
                if (search.SortType == ESortType.asc)
                {
                    return query.OrderBy(x => x.ControlNo);
                }
                else
                {
                    return query.OrderByDescending(x => x.ControlNo);
                }
            }
            if (search.SortField == ProductSerialSearchSortFieldsEnum.BatchNo)
            {
                if (search.SortType == ESortType.asc)
                {
                    return query.OrderBy(x => x.BatchNumber);
                }
                else
                {
                    return query.OrderByDescending(x => x.BatchNumber);
                }
            }
            if (search.SortField == ProductSerialSearchSortFieldsEnum.Balance)
            {
                if (search.SortType == ESortType.asc)
                {
                    return query.OrderBy(x => x.Balance);
                }
                else
                {
                    return query.OrderByDescending(x => x.Balance);
                }
            }
            if (search.SortField == ProductSerialSearchSortFieldsEnum.Expiry)
            {
                if (search.SortType == ESortType.asc)
                {
                    return query.OrderBy(x => x.Expiry);
                }
                else
                {
                    return query.OrderByDescending(x => x.Expiry);
                }
            }
            if (search.SortField == ProductSerialSearchSortFieldsEnum.Status)
            {
                if (search.SortType == ESortType.asc)
                {
                    return query.OrderBy(x => x.Status);
                }
                else
                {
                    return query.OrderByDescending(x => x.Status);
                }
            }
            return query;

        }

        public async Task<List<ProductSerialHistoryViewModel>> GetProductSerialHistory(long productSerialId)
        {
            try
            {
                var Query = await _productSerialHistory.Query()
                                                    .Where(x => x.ProductSerialId == productSerialId)
                                                    .Include(h => h.ProductSerial)
                                                    .OrderBy(x => x.CreationDate)
                                                    .ToListAsync();
                if (Query.Count < 0)
                {
                    return new List<ProductSerialHistoryViewModel>();

                }

                return Query.Select(x => new ProductSerialHistoryViewModel
                {
                    ProductSerialId = x.ProductSerialId,
                    Code = x.ProductSerial.Code,
                    OldStatus = x.OldStatus.GetDisplayName(),
                    NewStatus = x.NewStatus.GetDisplayName(),
                    OrderId = x.OrderId,
                    Note = x.Note,
                }).ToList();



            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public async Task<List<SerialsExportVm>> GetSerialsToExport(SmartTableParam Param)
        {
            var Query = _productSerialRepository.Query()
                             .Include(x => x.Product)
                             .ThenInclude(x => x.Categories)
                             .ThenInclude(x => x.Category)
                             .ThenInclude(x => x.CategoryDetails)
                             .Include(x=>x.Product)
                             .ThenInclude(x=>x.ProductDetails)
                             .Include(x => x.Order)

              .Where(x => x.Product.SiteId == _workContext.GetCurrentSiteId());

            if (Param.Search.PredicateObject != null)
            {
                dynamic search = Param.Search.PredicateObject;
                if (search.Serial != null)
                {
                    string Serial = search.Serial;
                    Query = Query.Where(x => x.Code.Contains(Serial));
                }
                if (search.Status != "")
                {
                    string Status = search.Status;
                    Query = Query.Where(x => x.Status == (EProductSerialStatus)Enum.Parse(typeof(EProductSerialStatus), Status));
                }
                if (!string.IsNullOrEmpty(search.CategoryId.ToString()))
                {
                    long CategoryId = Convert.ToInt64(search.CategoryId);
                    Query = Query.Where(x => x.Product.Categories.Any(x => x.CategoryId == CategoryId));
                }
                if (!string.IsNullOrEmpty(search.ProductId.ToString()))
                {
                    long ProductId = Convert.ToInt64(search.ProductId);
                    Query = Query.Where(x => x.Product.Id == ProductId);
                }


                if (search.CreatedOn != null)
                {
                    if (search.CreatedOn.before != null)
                    {
                        DateTimeOffset before = search.CreatedOn.before;
                        Query = Query.Where(x => x.CreationDate <= before);
                    }

                    if (search.CreatedOn.after != null)
                    {
                        DateTimeOffset after = search.CreatedOn.after;
                        Query = Query.Where(x => x.CreationDate >= after);
                    }
                }

            }

            return await Query.Select(x => new SerialsExportVm
            {

                Balance = x.Balance,
                BatchNumber = x.BatchNumber,
                ControlNumber = x.ControlNo,
                Status = x.Status.GetDisplayName(),
                CategoryName = x.Product.Categories == null ? null : x.Product.Categories.SelectMany(x => x.Category.CategoryDetails).OrderByDescending(x=>x.CultureId).Select(x => x.Name).FirstOrDefault(),
                CategoryId = x.Product.Categories == null ? null : x.Product.Categories.Select(x => x.Category.Id).FirstOrDefault(),
                ProductName = x.Product.ProductDetails.OrderByDescending(x=>x.CultureId).FirstOrDefault().Name,
                ProductId = x.Product.Id,
                Expiry = x.Expiry,
                OrderId = x.OrderId

            }).ToListAsync();



        }


        public async Task<List<SerialsExportWithOwnersVM>> GetSerialsWithOwners(long ProductId)
        {

            var Variations = await _productLinkRepository.Query()
                                  .Where(x => x.Product.Id == ProductId
                                      && x.LinkType == ItemLinkType.Super)
                                  .Select(x => x.LinkedProductId)
                                  .ToListAsync();

            Variations.Add(ProductId);

            var result = await _productSerialRepository.Query()
                                    .Where(x => Variations.Any(v => v == x.ProductId) && x.OrderId != null)
                                    .Include(PathString => PathString.Order)
                                    .ThenInclude(o => o.Customer)
                                    .ThenInclude(c => c.UserOptions)
                                    .Select(c => new SerialsExportWithOwnersVM()
                                    {
                                        Bib = c.Code,
                                        FirstName = c.Order.Customer.UserOptions.FirstOrDefault(x => x.PropertyName.ToLower() == "firstname").Value,
                                        LastName = c.Order.Customer.UserOptions.FirstOrDefault(x => x.PropertyName.ToLower() == "lastname").Value,
                                        Club = c.Order.Customer.UserOptions.FirstOrDefault(x => x.PropertyName.ToLower() == "team").Value,
                                        Nation = c.Order.Customer.UserOptions.FirstOrDefault(x => x.PropertyName.ToLower() == "nationality").Value,
                                        Sex = c.Order.Customer.UserOptions.FirstOrDefault(x => x.PropertyName.ToLower() == "gender").Value,
                                        Year = c.Order.Customer.DateOfBirth.Value.ToString("dd/MM/yyyy"),

                                    }).ToListAsync();


            return result;
        }
        public async Task<List<Range<long>>> GetAllSerialsAsRanges(long ProductId)
        {
            var Product = _productRepository.Query().Where(p => p.Id == ProductId).FirstOrDefault();
            Product.IsProductSerial = true;
            await _productRepository.SaveChangesAsync();
            List<Range<long>> Result = new List<Range<long>>();
            var SerialsCodes = await _productSerialRepository
                                    .Query()
                                    .Where(ps => ps.Status != EProductSerialStatus.Deleted && ps.ProductId == ProductId)
                                    .Select(x => x.Code)
                                    .ToListAsync();
            var SerialNumbers = SerialsCodes.Select(x => ConvertSerialToLong(x)).OrderBy(x => x).ToList();
            long From = -1;
            long To = -1;
            bool RangeBroke = false;
            foreach (var SerialNumber in SerialNumbers)
            {
                if (From == -1)
                {
                    From = SerialNumber;
                    To = SerialNumber;
                }
                else if (To + 1 == SerialNumber)
                {
                    To = SerialNumber;
                }
                else
                {
                    RangeBroke = true;
                }

                if (SerialNumber != SerialNumbers.Last() && RangeBroke)
                {
                    Result.Add(new Range<long>(From, To));
                    From = SerialNumber;
                    To = SerialNumber;
                    RangeBroke = false;

                }
                else if (SerialNumber == SerialNumbers.Last() && RangeBroke)
                {
                    Result.Add(new Range<long>(From, To));
                    Result.Add(new Range<long>(SerialNumber, SerialNumber));

                }
                else if (SerialNumber == SerialNumbers.Last() && !RangeBroke)
                {
                    Result.Add(new Range<long>(From, To));
                }

            }

            return Result;

        }

        public async Task<bool> AddSerialsFromRanges(long ProductId, List<Range<long>> Ranges)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    long SerilasCount = 0;
                    long currentUserId = await _workContext.GetCurrentUserId();
                    foreach (var Range in Ranges)
                    {
                        for (long code = Range.From; code <= Range.To; code++)
                        {
                            SerilasCount++;
                            ProductSerial NewSerial = new ProductSerial()
                            {
                                Code = code.ToString(),
                                CreatedById = currentUserId,
                                CreationDate = DateTimeOffset.Now,
                                Expiry = DateTimeOffset.Now.AddYears(1).Date.ToString(),
                                ProductId = ProductId,
                                Status = EProductSerialStatus.Available

                            };
                            _productSerialRepository.Add(NewSerial);

                        }
                    }
                    await _productSerialRepository.SaveChangesAsync();


                    var Product = await _productRepository.Query().Where(p => p.Id == ProductId).FirstOrDefaultAsync();
                    Product.IsProductSerial = true;
                    await _productRepository.SaveChangesAsync();

                   // await _productRepository.SQLQuery(ManageStockQueries.AddToProduct_StockQuantity, SerilasCount, ProductId);
                    var stockEffected = await _productRepository.SQLQuery(ManageStockQueries.AddToInventoryStock_StockQuantity, SerilasCount, ProductId);
                    if (stockEffected <= 0)
                    {
                        throw new Exception("There Is No Stock For This Product");
                    }
                    await _productRepository.SQLQuery(ManageStockQueries.AddToInventoryStock_StockHistory, ProductId, 1, DateTimeOffset.Now.ToString("yyyy-MM-dd hh:mm:ss"), currentUserId, SerilasCount);


                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    e = ExceptionHelper.ThrowDeepException(e);
                    if (e.Message.Contains("IX_Catalog_ProductSerial_Code_ProductId"))
                    {
                        throw new Exception("Please make sure that the ranges do not intersect");
                    }
                    throw e;
                }
            });

        }



        public async Task<bool> DeleteRange(long ProductId, List<Range<long>> Ranges)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    long currentUserId = await _workContext.GetCurrentUserId();

                    var rangeItems = Ranges.SelectMany(r => Enumerable.Range((int)r.From, (int)r.To - (int)r.From + 1)).Select(x => x.ToString()).ToList();


                    var RangeSerials = await _productSerialRepository.Query()
                                            .Where(x => x.ProductId == ProductId &&
                                                        rangeItems.Any(ri => ri == x.Code) &&
                                                         x.Status == EProductSerialStatus.Available)
                                            .ToListAsync();

                    foreach (var RangeSerial in RangeSerials)
                    {
                        await ChangeProductSerialStatus(RangeSerial.Id, EProductSerialStatus.Deleted);
                    }


                    //await _productRepository.SQLQuery(ManageStockQueries.SubtractFromProduct_StockQuantity, RangeSerials.Count, ProductId);
                    await _productRepository.SQLQuery(ManageStockQueries.SubtractFromInventoryStock_StockQuantity, RangeSerials.Count, ProductId);
                    await _productRepository.SQLQuery(ManageStockQueries.AddToInventoryStock_StockHistory, ProductId, 1, DateTimeOffset.Now.ToString("yyyy-MM-dd hh:mm:ss"), currentUserId, -1 * RangeSerials.Count);


                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    throw e;
                }
            });

        }

        private long ConvertSerialToLong(string SerialCode)
        {
            try
            {
                return long.Parse(SerialCode);
            }
            catch (Exception)
            {

                throw new Exception("Serials Not In Rages Format");
            }
        }

        public async Task<bool> ReserveSerials(long productSerialId, long orderId)
        {
            try
            {
                var query =await  _productSerialRepository.Query().Where(x => x.Id == productSerialId).FirstOrDefaultAsync();

                var OldStatus = query.Status;
                if (query.Status != EProductSerialStatus.Available)
                {

                    throw new Exception("Serial Should be Available");
                
                }
               
                query.Status = EProductSerialStatus.Taken;
                query.OrderId = orderId;
 
               await  _productSerialRepository.SaveChangesAsync();

                ProductSerialHistory history = new ProductSerialHistory()
                {
                    ProductSerialId = productSerialId,
                    CreationDate = DateTimeOffset.Now,
                    CreatedById =await _workContext.GetCurrentUserId(),
                    OldStatus = OldStatus,
                    NewStatus = query.Status,
                    Note = "Serial Has Been Reserved Manually To Order "+ orderId,
                    OrderId = query.OrderId
                };

                _productSerialHistory.Add(history);
                await _productSerialHistory.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async  Task<bool> RestoreSerials(long productSerialId)
        {

           var query= await _productSerialRepository.Query().Where(x=>x.Id==productSerialId).FirstOrDefaultAsync();

            var OldStatus = query.Status;
            if (query == null)
            {

                throw new Exception("Serial Not Found ");
            
                
            }
            if (query.Status == EProductSerialStatus.Deleted)
            {
                var Code = query.Code.Split('_').ToList();
                query.Code = Code.FirstOrDefault();
            }

            query.Status= EProductSerialStatus.Available;   

            query.OrderId = null;

            await _productSerialRepository.SaveChangesAsync();


            ProductSerialHistory history = new ProductSerialHistory()
            {
                ProductSerialId = productSerialId,
                CreationDate = DateTimeOffset.Now,
                CreatedById = await _workContext.GetCurrentUserId(),
                OldStatus = OldStatus,
                NewStatus = EProductSerialStatus.Available,
                Note = "Serial Status Changed Manually To Avaliable",
                OrderId = query.OrderId
            };

            _productSerialHistory.Add(history);
            await _productSerialHistory.SaveChangesAsync();

            return true;
        }

    }
}

