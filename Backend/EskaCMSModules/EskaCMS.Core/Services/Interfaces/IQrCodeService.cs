using EskaCMS.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Services.Interfaces
{
    public interface IQrCodeService
    {
        Task<string> GenerateQrCode(string Url, long orderId);
        Task<string> GenerateQrCode(string Data, string FileName);
    }
}
