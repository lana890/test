using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services.Interfaces
{
    public interface ISiteHostNameServices
    {
        Task<string> GetSiteHostNamePrefix(long SiteId, long CultureId);
    }
}
