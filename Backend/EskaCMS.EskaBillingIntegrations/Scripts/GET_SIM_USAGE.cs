using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Scripts
{
    internal static class GET_SIM_USAGE
    {
        internal static string GetSIMUsage = @"CREATE DEFINER=`root`@`%` PROCEDURE `GetSIMUsage`()
BEGIN
    SELECT su.CustomerSimsId as CustomerSimId,
    su.DataTotal, 
    su.ProductId,
    su.StatusSnapshot as StatusSnapshot,
    su.CreationDate as CreationDate,
    p.Price,
    Customer.Id AS CustomerId,
    cs.AccountId AS AccountId,
    cs.FirstActivationDate As ActivationDate,
    cs.ICCID ,
    cs.MSISDN,
    cs.IMSI,
    cs.APNType,
	DATE_FORMAT(DATE_ADD(su.CreationDate, INTERVAL (1 - DAY(su.CreationDate)) DAY), '%Y-%m-%d 00:00:00') AS StartDate,
	DATE_FORMAT(LAST_DAY(su.CreationDate), '%Y-%m-%d 00:00:00') AS EndDate,
    pv.value as ServiceId,
    pv2.value as TotalQuota,
    trate.Rate as TaxRate
    
    
    
   
    
    
    
    FROM integrations_simusage su
    inner JOIN catalog_product p ON p.Id = su.ProductId
    inner JOIN catalog_productattributevalue pv ON pv.ProductId =  p.Id 
    inner JOIN catalog_productattribute pa ON pa.Id = pv.AttributeId AND pa.Name = 'ServiceProfile'
    inner JOIN catalog_productattributevalue pv2 ON pv2.ProductId = p.Id 
    inner JOIN catalog_productattribute pa2 ON pa2.Id = pv2.AttributeId AND pa2.Name = 'TotalQuota' 
    left JOIN  tax_taxclass tclass on  p.TaxClassId = tclass.Id
    left JOIN tax_taxrate trate on trate.TaxClassId = tclass.Id
    inner JOIN integrations_customersims cs ON cs.Id = su.CustomerSimsId and  Not cs.isDeleted = 1
    inner JOIN core_user Customer ON  Customer.Id=cs.CustomerId 
   
    WHERE MONTH(su.CreationDate) = (MONTH(CURRENT_DATE()) - 1) AND YEAR(su.CreationDate) = YEAR(CURRENT_DATE());
END";
    }
}
