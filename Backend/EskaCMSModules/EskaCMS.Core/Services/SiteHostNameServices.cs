using EskaCMS.Core.Services.Interfaces;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public class SiteHostNameServices : ISiteHostNameServices
    {
        private readonly IRepository<SitesHostNames> siteHostnamesRepository;

        public SiteHostNameServices(IRepository<SitesHostNames> siteHostnamesRepository)
        {
            this.siteHostnamesRepository = siteHostnamesRepository;
        }

        public async Task<string> GetSiteHostNamePrefix(long SiteId, long CultureId)
        {
            try
            {
                var Prefix = await siteHostnamesRepository.Query()
                                                         .Where(x => x.SiteId == SiteId && x.CultureId == CultureId)
                                                         .Select(x => x.Prefix)
                                                         .FirstOrDefaultAsync();
                return Prefix;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
