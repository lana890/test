using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Scripts
{
    internal static class INSERT_SIM_INVOICES
    {
        internal static string INSERT_SIM_Invoices = @"CREATE DEFINER=`root`@`%` PROCEDURE `INSERT_SIM_Invoices`(
IN P_customerSimsId bigint,
IN P_startDate datetime(6),
IN P_endDate datetime(6),
IN P_activationDate datetime(6),
IN P_dataUsage double,
IN P_remainingQuata double,
IN P_ICIID bigint,
IN P_MSISDN bigint,
IN P_IMSI bigint,
IN P_APNType longtext,
IN P_paymentStatus int,
IN P_subAmount decimal (18,2),
IN P_totalAmount decimal (18,2),
IN P_taxAmount decimal (18,2),
IN P_taxRate decimal (18,2),
IN P_productId bigint,
IN P_accountId bigint,
IN P_serviceId bigint,
IN P_customerId bigint,
IN P_suspendedPeriod time(6),
IN P_termenationPeriod time(6),
IN P_activationPeriod time(6),
IN P_watingPeriod time(6))
BEGIN

insert INTO integrations_customerinvoices(
CustomerSimsId,
StartDate,
EndDate,
ActivationDate,
DataUsage,
RemainingQuata,
ICCID,
MSISDN,
IMSI,
APNType,
PaymentStatus,
SubAmount,
TotalAmount,
TaxAmount,
TaxRate,
ProductId,
AccountId,
ServiceId,
CustomerId,
SuspendedPeriod,
TermenationPeriod,
ActivationPeriod,
WatingPeriod,
CreationDate)

values(P_customerSimsId ,
 P_startDate ,
 P_endDate ,
 P_activationDate ,
 P_dataUsage ,
 P_remainingQuata ,
 P_ICIID ,
 P_MSISDN ,
 P_IMSI ,
 P_APNType ,
 P_paymentStatus,
 P_subAmount ,
 P_totalAmount ,
 P_taxAmount ,
 P_taxRate ,
 P_productId ,
 P_accountId ,
 P_serviceId ,
 P_customerId ,
 P_suspendedPeriod,
 P_termenationPeriod,
 P_activationPeriod,
 P_watingPeriod,

 CURRENT_DATE());




END";

        
    }
}
