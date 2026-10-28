using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.UserNavigationTracer.Services.IServices
{
    public interface IUserTracerService
    {
        Task<bool> ExecuteTakeNavigationSnapShot(string Slug, string SlugName, SlugsTypes SlugType, string IP, long PageId);
    }

}
