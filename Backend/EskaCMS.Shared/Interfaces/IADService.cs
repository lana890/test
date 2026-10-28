using EskaCMS.Addresses.Models;
using EskaCMS.ADIntegration.ViewModels;
using System.Threading.Tasks;

namespace EskaCMS.ADIntegration.Interfaces
{
    public interface IADService
    {
        AddressVM GetAddressById(long id);
        Task<GetADUserViewModel> ActiveDirectoryLogin();
    }
}
