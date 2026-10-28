using EskaCMS.Entities.Entity.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EskaCMS.Core.Data.Interfaces
{
      public interface IUnitOfWork<TContext> where TContext : DbContext, IDisposable
    {

         void Commit();
         public Task CommitAsync(); 


    }
}
