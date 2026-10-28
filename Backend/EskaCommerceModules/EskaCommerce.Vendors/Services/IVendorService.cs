using EskaCMS.Entities.Entity.Vendors;
using System.Threading.Tasks;
 
namespace EskaCommerce.Vendors.Services
{
    public interface IVendorService
    {
        Task Create(Vendor brand);

        Task Update(Vendor brand);

        Task Delete(long id);

        Task Delete(Vendor brand);
    }
}
