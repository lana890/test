using EskaCMS.Core.Areas.Core.ViewModels;
using EskaCMS.Core.Areas.Core.ViewModels.AppSettingsModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public interface IEmailSettingsServices
    {
        Task<bool> UpsertEmailsSettigs(UpsertEmailSettingsVM appSettingsRequestVm);
        Task<List<GetEmailResponseVm>> GetAllEmails();
        Task<GetEmailResponseByIdVm> GetById(long Id);

        Task<bool> UpdateEmailSettingsStatus(long Id,EStatus NewStatus);
    }
}
