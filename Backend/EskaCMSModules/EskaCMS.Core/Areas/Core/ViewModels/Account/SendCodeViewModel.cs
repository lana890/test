using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels.Account
{
    public class SendCodeViewModel
    {
        public SendCodeViewModel()
        {
            SMSProvider = SMSProviders.SMSZainJordan;
            WhatsAppProvider = WhatsAppProviders.Infoseed;
        }
        public E2FAProviders SelectedProvider { get; set; }
        public SMSProviders? SMSProvider { get; set; }
        public WhatsAppProviders? WhatsAppProvider { get; set; }

        public string Username { get; set; }

        public string Value { get; set; }
        public string Url { get; set; }
        public string EmailTemplateId { get; set; } = null;

        [Required(ErrorMessage = "The {0} field is required.")]
        public string Purpose { get; set; }
        public bool ValidateUser { get; set; } = false;

        public Dictionary<string, string>? AdditionalInfo { get; set; }



    }
}
