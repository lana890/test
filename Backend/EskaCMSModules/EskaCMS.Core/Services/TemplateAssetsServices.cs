using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Core.Models;
using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Infrastructure.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.DataAccess.Data;


namespace EskaCMS.Core.Services
{
    public class TemplateAssetsServices : ITemplateAssetsServices
    {
        private readonly IWorkContext _workContext;
        private readonly EskaDCMSDbContext _context;
        private readonly IRepository<TemplateAssets> _templateAssets;

        public TemplateAssetsServices(IWorkContext workContext, EskaDCMSDbContext context, IRepository<TemplateAssets> templateAssets)
        {
            _workContext = workContext;
            _context = context;
            _templateAssets = templateAssets;
        }

        public async Task<bool> ChangeStatus(long Id, EStatus status)
        {
            try
            {
                var Query = await _templateAssets.Query().Where(x => x.Id == Id).FirstOrDefaultAsync();

                if (Query == null)
                {
                    throw new Exception("Could Not Find The Assset");
                }
                Query.Status = status;


                await _templateAssets.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<List<TemplateAssetsViewModel>> GetTemplatesAssets()
        {
            try
            {
                var SiteId = _workContext.GetCurrentSiteId();


                var Query = await _templateAssets.Query()
                    .Include(x => x.Asset)
                    .Include(x => x.Template)
                    .Where(x => x.Status != EStatus.Deleted && x.Asset.SiteId == SiteId)
                .Select(x => new TemplateAssetsViewModel
                {
                    Id = x.Id,
                    AssetsId = x.AssetId,
                    TemplateId = x.TemplateId,
                    AssetsName = x.Asset.Name,
                    TemplateName = x.Template.Name,
                    Status=x.Status,
                    Order=x.Order
                    
                }).ToListAsync();
                return Query;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> UpsertTemplateAssets(TemplateAssetsVM templateAssets)
        {

            try
            {
                if (templateAssets.Id == 0)
                {
                    return await AddTemplateAssets(templateAssets);
                }
                return await UpdateTemplateAssets(templateAssets);




            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private async Task<bool> AddTemplateAssets(TemplateAssetsVM templateAssets)
        {
            try
            {
                var TemplateAsset = new TemplateAssets
                {

                    AssetId = templateAssets.AssetsId,
                    TemplateId = templateAssets.PageTemplateId,
                    Order = templateAssets.Order,
                    Status = templateAssets.status
                };

                _templateAssets.Add(TemplateAsset);
                await _templateAssets.SaveChangesAsync();
                return true;


            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private async Task<bool> UpdateTemplateAssets(TemplateAssetsVM templateAssets)
        {
            try
            {
                var TemplateAssets = await _templateAssets.Query().Where(x => x.Id == templateAssets.Id).FirstOrDefaultAsync();

                TemplateAssets.AssetId = templateAssets.AssetsId;
                TemplateAssets.Status = templateAssets.status;
                TemplateAssets.Order = templateAssets.Order;
                TemplateAssets.TemplateId = templateAssets.PageTemplateId;
                await _templateAssets.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
