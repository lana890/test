using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Infrastructure.Data;
using EskaCMS.Module.Core.Data;
using EskaCMS.DynamicForms.Repositories;
using EskaCMS.DynamicForms.Repositories.IRepositories;
using EskaCMS.DynamicForms.UnitOfWorks.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.DynamicForms.UnitOfWorks
{
    public class DynamicFormUnitOfWork : UnitOfWork<EskaDCMSDbContext>, IDynamicFormUnitOfWork
    {
        public IPricingMatrixRepository FormItemRepository { get; }

        public IRepository<FormField> FormFieldRepository { get; }
        public IRepository<FormFieldDetails> FormFieldDetailsRepository { get; }

        public IRepository<Classifications> PricingClassificationsRepository { get; }

        public DynamicFormUnitOfWork(EskaDCMSDbContext context) : base(context)
        {
            FormItemRepository = new PricingMatrixRepository(context);
            FormFieldRepository = new Repository<FormField>(context);
            FormFieldDetailsRepository = new Repository<FormFieldDetails>(context);
            PricingClassificationsRepository = new Repository<Classifications>(context);
        }

    }
}
