using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.DynamicForms.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.DynamicForms.HelperClasses.IHelperClasses
{
    public interface IOperationsManager
    {
        Task<string> AppliedOperation(Criteria CriteriaItem, DynamicFormsWebVM CriteriaModel);
        Task<string> AppliedOperation(Criteria CriteriaItem, List<DynamicFormsWebVM> CriteriaModels);
    }
}
