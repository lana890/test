using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Core.Data.Interfaces;

namespace EskaCMS.Module.Core.Data
{
    public class UnitOfWork<TContext> : IUnitOfWork<TContext> where    TContext:DbContext
    {
        private readonly TContext _context;
 
    

        public UnitOfWork(TContext context )
        {
            _context = context;


        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        
        }

        public void Commit()
        {
              _context.SaveChanges();
        }
        public async Task CommitAsync()
        {
             await _context.SaveChangesAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            { 
                _context.Dispose();
            }
        }
    }
}
