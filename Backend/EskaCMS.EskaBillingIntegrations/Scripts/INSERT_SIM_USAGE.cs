using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Scripts
{
    internal static class INSERT_SIM_USAGE_SP
    {
        internal static string INSERT_SIM_USAGE  = @"CREATE DEFINER=`root`@`%` PROCEDURE `INSERT_SIM_USAGE`(
IN P_dataTotal double,
IN P_roamingDataConsumed double,
IN P_dataOut double,
IN P_cost double,
IN P_smsOutgoing bigint,
IN P_dataCharged double,
IN P_smsUsage double,
IN P_numberOfVoiceCalls bigint,
IN P_roamingDataOut double,
IN P_numberOfRoamingSms bigint,
IN P_roamingVoiceOutgoing double,
IN P_roamingSmsOutgoing bigint,

IN P_voiceIncoming double,
IN P_voiceOutgoing double,
IN P_numberOfRoamingCalls bigint,
IN P_roamingVoiceIncoming INT(100),

IN P_dataIn double,
IN P_smsIncoming bigint,
IN P_roamingSmsIncoming double,
IN P_roamingDataIn double,
IN P_SIM_ID bigint)
BEGIN

insert INTO integrations_simusage(
voiceIncoming,
voiceOutgoing,
numberOfRoamingCalls,
roamingVoiceIncoming,
dataIn,
sms_Incoming,
roamingSmsIncoming,
roamingDataIn,
dataTotal,
roamingDataConsumed,
dataOut,
cost,
sms_Outgoing,
dataCharged,
sms_Usage,
numberOfVoiceCalls,
roamingDataOut,
numberOfRoamingSms,
roamingVoiceOutgoing,
roamingSmsOutgoing,

CustomerSimsId,

CreatedById,
CreationDate,

StatusSnapshot,
ProductId
)
select 
	P_voiceIncoming -ifnull( Sum(VoiceIncoming),0) as voiceIncoming,
	P_voiceOutgoing -ifnull( Sum(voiceOutgoing),0) as voiceOutgoing,
	P_numberOfRoamingCalls -ifnull( Sum(numberOfRoamingCalls),0) as numberOfRoamingCalls,
	P_roamingVoiceIncoming -ifnull( Sum(roamingVoiceIncoming),0) as roamingVoiceIncoming,
	P_dataIn -ifnull( Sum(dataIn),0) as dataIn,
	P_smsIncoming -ifnull( Sum(sms_Incoming),0) as sms_Incoming,
	P_roamingSmsIncoming -ifnull( Sum(roamingSmsIncoming),0) as roamingSmsIncoming,
	P_roamingDataIn -ifnull( Sum(roamingDataIn),0) as roamingDataIn,
	P_dataTotal -ifnull( Sum(dataTotal),0) as dataTotal,
	P_roamingDataConsumed -ifnull( Sum(roamingDataConsumed),0) as roamingDataConsumed,
    P_dataOut -ifnull( Sum(dataOut),0) as dataOut,
    P_cost -ifnull( Sum(cost),0) as cost,
    P_smsOutgoing -ifnull( Sum(sms_Outgoing),0) as sms_Outgoing,
    P_dataCharged -ifnull( Sum(dataCharged),0) as dataCharged,
    P_smsUsage -ifnull( Sum(sms_Usage),0) as sms_Usage,
    P_numberOfVoiceCalls -ifnull( Sum(numberOfVoiceCalls),0) as numberOfVoiceCalls,
    P_roamingDataOut -ifnull( Sum(roamingDataOut),0) as roamingDataOut,
    P_numberOfRoamingSms -ifnull( Sum(numberOfRoamingSms),0) as numberOfRoamingSms,
    P_roamingVoiceOutgoing -ifnull( Sum(roamingVoiceOutgoing),0) as roamingVoiceOutgoing,
    P_roamingSmsOutgoing -ifnull( Sum(roamingSmsOutgoing),0) as roamingSmsOutgoing,
    P_SIM_ID as CustomerSimsId,

    1,
    CURRENT_TIMESTAMP(),
    s.Status as StatusSnapshot,
    s.ProductId as ProductId
    
    
    
from integrations_customersims as s left join integrations_simusage as u on
	u.CustomerSimsId = s.Id AND
    s.Id = P_SIM_ID and
	YEAR(u.CreationDate) = YEAR(CURRENT_DATE()) AND
	MONTH(u.CreationDate) = MONTH(CURRENT_DATE())
where 
	s.Id = P_SIM_ID;




END";
    }
}
