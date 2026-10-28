using EskaCMS.EskaBilling.Models;
using EskaCMS.EskaBilling.Models.response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Services.Interfaces
{
    public interface ICHARTServices
    {
        Task<List<ChartViewModel>> GetSimsStatuses(long AccountId , string SimTag);


        /// <summary>
        /// Getting SIM Usage By SIM Id 
        /// </summary>
        /// <param name="model"> Passes From Month To Month and SIM Id to filter on</param>
        /// <returns> A List Of SIM Usage Items Orderd By Date</returns>
        Task<List<GetSIMUsageResponseVM>> GetSIMUsage(GetSIMUsageFilterVM model);
        Task<List<GetAllSimUsageResponseVM>> GetAllSIMUsage(GetAllSIMUsageFilterVM model);
    }
}
