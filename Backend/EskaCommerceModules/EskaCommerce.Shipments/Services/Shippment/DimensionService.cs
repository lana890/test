using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Shipments;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Extensions;
using EskaCMS.Infrastructure.Pagination;
using EskaCommerce.Shipments.Models;
using EskaCommerce.Shipments.Services.Shippment.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Shipments.Services.Shippment
{
    public class DimensionService : IDimensionService
    {
        private readonly IRepository<Dimensions> dimensionsRepository;
        private readonly IRepository<ProductDimensions> productdimensionsRepository;
        private readonly IWorkContext workContext;

        public DimensionService(IRepository<Dimensions> DimensionsRepository,
            IRepository<ProductDimensions> ProductdimensionsRepository,
            IWorkContext workContext)
        {
            dimensionsRepository = DimensionsRepository;
            productdimensionsRepository = ProductdimensionsRepository;
            this.workContext = workContext;
        }

        public async Task<PaginationResult<DimensionViewModel>> GetAllAsync(DimensionFilterationViewModel model)
        {
            try
            {
                var siteId = await workContext.GetCurrentSiteIdAsync();

                return await dimensionsRepository.Query()
                    .Include(d => d.CreatedBy)
                    .Include(d => d.ModifiedBy)
                     .Include(x => x.ProductDimensions)
                    .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.ProductDetails)
                    .Where(x => x.SiteId == siteId && x.Status != EStatus.Deleted)
                    .Where(x => string.IsNullOrEmpty(model.Name) || x.Name.ToLower().Contains(model.Name.ToLower()))
                    .Select(d => new DimensionViewModel
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Weight = d.Weight,
                        Width = d.Width,
                        Height = d.Height,
                        Length = d.Length,
                        Status = d.Status,
                        CreatedByName = d.CreatedBy.FullName,
                        CreationDate = d.CreationDate,
                        DimensionType = d.DimensionType,
                        NumberOfPieces = d.NumberOfPieces,
                        ModifiedByName = d.ModifiedBy != null ? d.ModifiedBy.FullName : null,
                        ModificationDate = d.ModificationDate,
                        ProductIds = d.ProductDimensions.Select(x => x.ProductId
                    ).ToList()
                    }).PaginateAsync(model.PageSize, model.PageNumber);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<DimensionViewModel> GetByIdAsync(int id)
        {
            try
            {

                var query = await dimensionsRepository.Query()
                    .AsNoTracking()
                    .Include(x => x.CreatedBy)
                    .Include(x => x.ModifiedBy)
                    .Include(x=>x.ProductDimensions)
                    .ThenInclude(x=>x.Product)
                    .ThenInclude(x=>x.ProductDetails)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (query == null) return null;

                return new DimensionViewModel
                {
                    Id = query.Id,
                    Name = query.Name,
                    Weight = query.Weight,
                    Width = query.Width,
                    Height = query.Height,
                    Length = query.Length,
                    Status = query.Status,
                    CreatedByName = query.CreatedBy.FullName,
                    CreationDate = query.CreationDate,
                    ModifiedByName = query.ModifiedBy?.FullName,
                    ModificationDate = query.ModificationDate,
                    DimensionType = query.DimensionType,
                    NumberOfPieces=query.NumberOfPieces,
                    ProductIds=query.ProductDimensions.Select(x=>x.ProductId
                    ).ToList()

                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> CreateAsync(DimensionCreateUpdateViewModel model)
        {
            try
            {
                var userId = await workContext.GetCurrentUserId();
                var siteId = await workContext.GetCurrentSiteIdAsync();

                await ValidateProductDimensionConflicts(model.ProductIds, model.Id ?? 0);

                var dimension = new Dimensions
                {
                    Name = model.Name,
                    Weight = model.Weight,
                    Width = model.Width,
                    Height = model.Height,
                    Length = model.Length,
                    Status = model.Status,
                    CreatedById = userId,
                    CreationDate = DateTimeOffset.UtcNow,
                    SiteId = siteId,
                    DimensionType = model.DimensionType,
                    NumberOfPieces = model.NumberOfPieces
                };

                dimensionsRepository.Add(dimension);
                await dimensionsRepository.SaveChangesAsync();

                await AddProductDimensionsIfNeeded(model.ProductIds, model.DimensionType, dimension.Id);

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> UpdateAsync(DimensionCreateUpdateViewModel model)
        {
            try
            {
                var userId = await workContext.GetCurrentUserId();

                var dimension = await dimensionsRepository.Query()
                    .Include(d => d.ProductDimensions)
                    .FirstOrDefaultAsync(d => d.Id == model.Id);

                if (dimension == null)
                    throw new Exception("Dimension not found");

                await ValidateProductDimensionConflicts(model.ProductIds, model.Id.Value);

                // Update fields
                dimension.Name = model.Name;
                dimension.Weight = model.Weight;
                dimension.Width = model.Width;
                dimension.Height = model.Height;
                dimension.Length = model.Length;
                dimension.Status = model.Status;
                dimension.ModifiedById = userId;
                dimension.ModificationDate = DateTimeOffset.UtcNow;
                dimension.DimensionType = model.DimensionType;
                dimension.NumberOfPieces = model.NumberOfPieces;

                await dimensionsRepository.SaveChangesAsync();

                // Remove old relations and add new if needed
                productdimensionsRepository.RemoveRange(dimension.ProductDimensions);
                await productdimensionsRepository.SaveChangesAsync();

                await AddProductDimensionsIfNeeded(model.ProductIds, model.DimensionType, dimension.Id);

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var dimension = await dimensionsRepository.Query()
                    .Include(x => x.CreatedBy)
                    .Include(x => x.ModifiedBy)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (dimension == null)
                    return false;

                dimension.Status = EStatus.Deleted;
                await dimensionsRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task ValidateProductDimensionConflicts(List<long> productIds, long dimensionId)
        {
            if (productIds == null || !productIds.Any()) return;

            var conflicts = await productdimensionsRepository.Query()
                .AsNoTracking()
                .Where(x => x.Dimensions.Status != EStatus.Deleted)
                .Where(x => productIds.Contains(x.ProductId))
                .ToListAsync();

            if (dimensionId > 0)
            {
                conflicts = conflicts.Where(x => x.DimensionsId != dimensionId).ToList();
            }

            if (conflicts.Any())
            {
                throw new Exception("Some product(s) already assigned to another Dimension.");
            }
        }

        private async Task AddProductDimensionsIfNeeded(List<long> productIds, EDimensionType dimensionType, long dimensionId)
        {
            if ( productIds != null && productIds.Any())
            {
                var productDimensions = productIds.Select(productId => new ProductDimensions
                {
                    DimensionsId = dimensionId,
                    ProductId = productId
                }).ToList();

                productdimensionsRepository.AddRange(productDimensions);
                await productdimensionsRepository.SaveChangesAsync();
            }
        }

    }

}
