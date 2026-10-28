using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EmailSender.Services.Interfaces
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
