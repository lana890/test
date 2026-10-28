using EskaCMS.Core.Data;
using EskaCMS.Core.Data.Interfaces;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Infrastructure.Data;
using EskaCMS.DynamicForms.Repositories.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.DynamicForms.UnitOfWorks.Interfaces
{
    public interface IDynamicFormUnitOfWork : IUnitOfWork<EskaDCMSDbContext>
    {
        public IPricingMatrixRepository FormItemRepository { get; }
        public IRepository<FormField> FormFieldRepository { get; }
        public IRepository<FormFieldDetails> FormFieldDetailsRepository { get; }
        public IRepository<Classifications> PricingClassificationsRepository { get; }
        //  public IRepository<Criteria> PricingCriteriaRepository { get; }
    }
}
