

using AutoMapper;
using EskaCMS.EskaBilling.Models;
using EskaCMS.EskaBilling.Models.response;
using EskaCMS.Infrastructure.Helpers.DinToPdf;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.Infrastructure.Web.SmartTable;
using EskaCMS.Integrations.EskaBilling.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static EskaCMS.EskaBilling.Enums.IOTEnums;

namespace EskaCMS.EskaBilling.Services
{
    public interface IBillingServices
    {
        Task<bool> CheckUserGroup(List<BillingGroupVm> billingGroupVm);

    }
}

