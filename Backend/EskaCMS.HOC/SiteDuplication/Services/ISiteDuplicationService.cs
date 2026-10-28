using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.HOC.SiteDuplication.Services
{
    public interface ISiteDuplicationService
    {
        public Task<bool> DuplicatieSiteAsync(long DuplicateFromId);
    }
}
