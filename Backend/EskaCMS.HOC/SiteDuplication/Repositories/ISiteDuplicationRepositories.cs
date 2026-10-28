using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.HOC.SiteDuplication.Repositories
{
    public interface ISiteDuplicationRepositories
    {

  //      Task<bool?> DublicatePricingQuotations(long OriginalSite, long DublicateSite);
        Task<bool> DublicateAddress(long OriginalSite, long DublicateSite);
        Task<bool> DublicateBlogArchived(long originalSite, long dublicateSite);
    }
}
