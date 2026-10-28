using EskaCMS.Core.Data;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Module.Core.Data;
using EskaCMS.DynamicForms.Repositories.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.DataAccess.Data;

namespace EskaCMS.DynamicForms.Repositories
{
    public class PricingMatrixRepository : Repository<FormItem>, IPricingMatrixRepository
    {
        private readonly EskaDCMSDbContext _contex;

        public PricingMatrixRepository(EskaDCMSDbContext context) : base(context)
        {
            _contex = context;
        }
    }
}
