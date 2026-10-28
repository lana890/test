
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Helpers;
using EskaCMS.Infrastructure.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Core.Services
{
    public class AssetsService : IAssetsService
    {
        private readonly IRepository<Assets> _AssetsRepo;
        private readonly IWorkContext _WorkContext;
        public AssetsService(IRepository<Assets> AssetsRepo, IWorkContext workContext)
        {
            _AssetsRepo = AssetsRepo;
            _WorkContext = workContext;
        }
        public async Task<PaginationResult<GetAssetsVM>> GetAssets(GetAssertVM model)
        {
            long SiteId = await _WorkContext.GetCurrentSiteIdAsync();

            IQueryable<Assets> query = _AssetsRepo.Query().Where(a => a.Status != EStatus.Deleted && a.SiteId == SiteId);

            if (model.Type.HasValue && model.Type.Value != 0)
            {
                query = query.Where(a => a.Type == model.Type.Value);

            }
            if (!string.IsNullOrEmpty(model.SearchText))

            {
                model.SearchText=model.SearchText.Trim();

                query = query.Where(x => x.Name.Trim().Contains(model.SearchText)
                                 || x.Content.Contains(model.SearchText)
                                 || x.Description.Contains(model.SearchText));
            
            }


           return await query
                .Include(a => a.CreatedBy)
                .Include(a => a.ModifiedBy)
                .Select(a => new GetAssetsVM()
                {
                    Type = a.Type,
                    CreatedByName = a.CreatedBy.FullName,
                    ModifiedByName = a.ModifiedBy != null ? a.ModifiedBy.FullName : " - ",
                    CreationDate = a.CreationDate,
                    ModificationDate = a.ModificationDate,
                    Description = a.Description,
                    Name = a.Name,
                    Id = a.Id,
                    Status=a.Status
                    
                }).PaginateAsync(model.PageSize,model.PageNumber);


        }

        public async Task<bool> ChangeAssetsStatus(long AssetId, EStatus NewStatus)
        {
            Assets Asset = await _AssetsRepo.Query().Where(a => a.Id == AssetId).FirstOrDefaultAsync();

            if (Asset == null)
            {
                throw new Exception("Could Not Find The Assset");
            }
            Asset.Status = NewStatus;
            _AssetsRepo.SaveChanges();
            return true;

        }


        public async Task<bool> UpsertAssets(UpsertAssetVM Asset)
        {
            if (Asset.Id == 0)
            {
                return await AddAssets(Asset);
            }
            return await UpdateAssets(Asset);
        }

        #region Helper Functions
        private async Task<bool> AddAssets(UpsertAssetVM Asset)
        {
            try
            {
                long SiteId = await _WorkContext.GetCurrentSiteIdAsync();

                long UserId = await _WorkContext.GetCurrentUserId();

                Assets NewItem = new Assets()
                {
                    Description = Asset.Description,
                    Name = Asset.Name,
                    Status = EStatus.Published,
                    Type = Asset.Type,
                    CreatedById = UserId,
                    CreationDate = DateTimeOffset.Now,
                    SiteId = SiteId,
                    Content = Asset.Content
                };

                _AssetsRepo.Add(NewItem);

                await _AssetsRepo.SaveChangesAsync();
                var Extension = Enum.GetName(typeof(EAssetsType), NewItem.Type);
                FileHelper.UpsertFile(NewItem.Content, Asset.Name, "wwwroot/Resources/styles/siteId_" + SiteId +$"/Assest/{Extension}", Extension);

                return true;
            }
            catch (Exception e)
            {

                throw e;
            }

        }

        private async Task<bool> UpdateAssets(UpsertAssetVM Asset)
        {
            try
            {
                Assets OriginalAsset = await _AssetsRepo.Query().Where(a => a.Id == Asset.Id).FirstOrDefaultAsync();

                long SiteId = await _WorkContext.GetCurrentSiteIdAsync();

                long UserId = await _WorkContext.GetCurrentUserId();

                OriginalAsset.Description = Asset.Description;
                OriginalAsset.Name = Asset.Name;
                OriginalAsset.Type = Asset.Type;
                OriginalAsset.ModifiedById = UserId;
                OriginalAsset.ModificationDate = DateTimeOffset.Now;
                OriginalAsset.Content = Asset.Content;

                await _AssetsRepo.SaveChangesAsync();
                var Extension = Enum.GetName(typeof(EAssetsType), OriginalAsset.Type);
                FileHelper.UpsertFile(OriginalAsset.Content, Asset.Name, "wwwroot/Resources/styles/siteId_" + SiteId + $"/Assest/{Extension}", Extension);
                return true;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public async Task<GetAssetViewModel> GetAssetsById(long id)
        {
            try
            {
                long SiteId = await _WorkContext.GetCurrentSiteIdAsync();


                var query = await _AssetsRepo.Query().Where(x => x.Id == id)
                    .Select(x => new GetAssetViewModel
                    {
                        Id = x.Id,
                        Content = x.Content,
                        Description = x.Description,
                        Name = x.Name,
                        Type = x.Type,

                    }).FirstOrDefaultAsync();

                return query;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

    }
}
