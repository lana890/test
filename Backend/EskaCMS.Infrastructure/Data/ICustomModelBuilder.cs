using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Data
{
    public interface ICustomModelBuilder
    {
        void Build(ModelBuilder modelBuilder);
    }

    public interface IDataSeeder
    {
        Task Seed();
    }
}
