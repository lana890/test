using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services.Interfaces
{
    public interface IWhatsAppServices
    {
        Task SendMessageAsync(WhatsAppProviders SMSProvider, string PhoneNumber, string ReciverName, bool IsPDF, string PdfLink, string Message, long SiteId, string AdditionInfo = "");
    }
}
