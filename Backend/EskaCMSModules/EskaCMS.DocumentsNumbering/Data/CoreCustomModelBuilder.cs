using EskaCMS.Entities.Entity.DocumentsNumbering;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EskaCMS.DocumentsNumbering.Data
{
    public class NumberingCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<DocumentsNumbers>(e => e.Property(m => m.Id).HasMaxLength(265));
        }
    }
}
