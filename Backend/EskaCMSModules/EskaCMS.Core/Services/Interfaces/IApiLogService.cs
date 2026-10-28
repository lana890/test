using EskaCMS.Core.Models;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services
{
    public interface IApiLogService
    {
        Task<bool> CreateLog(ApiLogVM log);
     
    }
}
