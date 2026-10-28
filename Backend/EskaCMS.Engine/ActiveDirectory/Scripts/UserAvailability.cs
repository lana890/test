using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Engine.Scripts
{
    public static class UserAvailability
    {


        public static string eventDetails = @" CREATE DEFINER=`root`@`%` PROCEDURE `GetEventDetails`(IN currentDateTime DATETIME(6))
BEGIN
    SELECT eo.CreatedById AS userId , up.ModificationDate
    FROM events_eventoccurrence eo
    join core_useroptions as up on up.UserId = eo.createdbyid and up.NormalizedPropertyName = 'AVAILABILITYSTATUS' and up.Normalizedvalue = '1'
    WHERE currentDateTime BETWEEN eo.FromTime AND eo.ToTime and(up.ModificationDate NOT BETWEEN eo.FromTime AND eo.ToTime OR up.ModificationDate IS NULL)

    UNION

    SELECT eventattendees.userId,up.ModificationDate
    FROM events_eventoccurrence eo
    JOIN events_eventattendees eventattendees ON eo.id = eventattendees.EventOccurrenceId
     join core_useroptions as up on up.UserId = eventattendees.userid and up.NormalizedPropertyName = 'AVAILABILITYSTATUS' and up.Normalizedvalue = '1'
    WHERE currentDateTime BETWEEN eo.FromTime AND eo.ToTime and (up.ModificationDate NOT BETWEEN eo.FromTime AND eo.ToTime OR up.ModificationDate IS NULL);
        END";
    }
}
