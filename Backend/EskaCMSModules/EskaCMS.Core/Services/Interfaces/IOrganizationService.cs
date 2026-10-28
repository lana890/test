using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Core.ViewModels;

namespace EskaCMS.Core.Services
{
    public interface IOrganizationService
    {

        Task<long> Create(OrganizationAddVM organizationReq);
        Task<long> Update(OrganizationUpdateVM organizationReq);
        Task<OrganizationVM> GetById(long organizationId);
        Task<OrganizationsGridOutputVM<OrganizationVM>> GetList(OrganizationsSearchTableParam<OrganizationsListSearchVM> param);
        Task<long> Delete(long id);
        Task<List<OrganizationGetAllVM>> GetAll();
    }
}
