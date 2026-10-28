using EskaCMS.EskaBilling.Enums;
using EskaCMS.EskaBilling.Models;

namespace EskaCMS.EskaBilling.Services
{
    public interface ISIMsActionsService
    {

        Task<List<long>> GetSIMsThatCanNotBeSuspendedAnyMore(List<long> Ids);
        Task<bool> ChangeSIMsPlan(ChangeSIMsPlanVM Model);
        Task<bool> ChangeSimsStatus(List<long> Ids, IOTEnums.ESIMsAction Action, string Note);
        Task AddSIMsUsage(List<SIMWithIdentifiers> SIMs);
        
        Task<bool> RequestChangeSIMsPlan(ChangeSIMsPlanVM Model , long customerId );
        Task<bool> UpdateSTCIPAddress(SIMWithIdentifiers SIM, string IPAdress);
    }
}