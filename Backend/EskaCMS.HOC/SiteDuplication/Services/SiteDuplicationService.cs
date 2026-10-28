using EskaCMS.Entities.Entity;
using EskaCMS.Core.Extensions;
using EskaCMS.HOC.SiteDuplication.Repositories;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Sites.Models;
using EskaCMS.Sites.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.HOC.SiteDuplication.Services
{
    public class SiteDuplicationService : ISiteDuplicationService
    {
        private readonly IWorkContext _WorkContext;
        private readonly IRepository<Site> _SitesRepository;
        private readonly ISiteDuplicationRepositories _siteDuplicationRepositories;

        public SiteDuplicationService(IWorkContext workContext, IRepository<Site> SitesRepository,ISiteDuplicationRepositories siteDuplicationRepositories)
        {
            _SitesRepository = SitesRepository;
            _siteDuplicationRepositories = siteDuplicationRepositories;
            _WorkContext = workContext;
        }
        public async Task<bool> DuplicatieSiteAsync(long DuplicateFromId)
        {
            try
            {

                long UserId =await  _WorkContext.GetCurrentUserId();
                var UserSites = new List<UsersSites>()
                    {
                        new UsersSites(){ UserId = 1 ,CultureId = null , CurrencyId=null}
                    };
                if(UserId != 1)
                {
                    UserSites.Add(new UsersSites() { UserId = UserId ,CultureId = null , CurrencyId=null });
                }

                var OriginalSite =await _SitesRepository.Query().FirstOrDefaultAsync(s=>s.Id == DuplicateFromId);

                var DublicateSite = new Site()
                {
                    CoreBranchId = OriginalSite.CoreBranchId,
                    CreatedById = UserId,
                    CreationDate = DateTime.Now,
                    Css = OriginalSite.Css,
                    DefaultParentPageId = 0,
                    Description = OriginalSite.Description,
                    IsADAuthRequired = OriginalSite.IsADAuthRequired,
                    Name = OriginalSite.Name + " - Duplicated",
                    Script = OriginalSite.Script,
                    ThumbnailId = null,
                    UserSites = UserSites,
                    OrganizationId = OriginalSite.OrganizationId,
                    SitesCultures = OriginalSite.SitesCultures.Select(s => new SitesCultures()
                    {
                        CultureId = s.CultureId,
                        CreatedById = UserId,
                        CulturePrefix = s.CulturePrefix,
                        Status = s.Status,
                        CultureType = s.CultureType,
                        CreationDate = DateTime.Now,

                    }).ToList(),
                    Status = OriginalSite.Status,
                    StatusDate = DateTime.Now,
                    SiteSettings = OriginalSite.SiteSettings
                    .Where(s => s.IsVisibleInCommonSettingPage)
                    .Select(x => new SiteSettings(x.Id)
                    {
                        IsVisibleInCommonSettingPage = x.IsVisibleInCommonSettingPage,
                        Module = x.Module,
                        Value = x.Value
                    })
                    .ToList(),
                };

                _SitesRepository.Add(DublicateSite);

                await _SitesRepository.SaveChangesAsync();

                await DublicateAllPiecesFromPreviousSite(OriginalSite.Id, DublicateSite.Id);

                return true;
            }
            catch (Exception e)
            {

                throw;
            }
        }

        private async Task<bool> DublicateAllPiecesFromPreviousSite(long OriginalSite, long DublicateSite)
        {
            try
            {
               // await   _siteDuplicationRepositories.DublicatePricingQuotations(OriginalSite,DublicateSite);
                await _siteDuplicationRepositories.DublicateAddress(OriginalSite, DublicateSite);
                await _siteDuplicationRepositories.DublicateBlogArchived(OriginalSite, DublicateSite);



                return true;
             
            }
            catch (Exception ex)
            {

                throw ex;
            }        }
    }
}
