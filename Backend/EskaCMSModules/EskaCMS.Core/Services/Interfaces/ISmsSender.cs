using System.Collections.Generic;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public interface ISmsSender
    {
       
        Task SendSmsAsync(SMSProviders SMSProvider, string PhoneNumber, string Message, long SiteId, string AdditionInfo = "");

     
    }
}