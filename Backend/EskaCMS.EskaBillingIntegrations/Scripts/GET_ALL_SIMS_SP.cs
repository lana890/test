using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Scripts
{
    internal static class GET_ALL_SIMS_SP
    {
        internal static string GetAllSIMS = @"CREATE DEFINER=`root`@`%` PROCEDURE `GetAllSIMS`()
BEGIN
    SELECT Id, IMSI, ICCID, IdentifierType
    FROM integrations_customersims
    where IsDeleted=0;
END";
    }
}
