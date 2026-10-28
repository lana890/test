using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.EskaBilling.Scripts
{
    internal static class GET_SHECHDULED_PLANS
    {
        internal static string Get_Shechduled_Plans = @"CREATE DEFINER=`root`@`%` PROCEDURE `Get_Shechduled_Plans`()
BEGIN
    SELECT 
    
    cp.ProductId as ProductId,
    cp.Note,
    pv.value as TargetProfile,
    s.IMSI,
    s.ICCID,
    s.IdentifierType,
    s.Status,
    s.ProductId as SimProductId,
    s.AccountId,
	s.MSISDN,
    s.CustomerId,
    s.SIMName,
    s.SIMTag,
    s.AssignIP,
    s.APNType,
    s.City,
    s.Id
    
    
    
    FROM eskabilling_changeplanrequest cp
     inner JOIN integrations_customersims s ON s.Id = cp.CustomerSimsId and s.IsDeleted =0
    inner JOIN catalog_product p ON p.Id = cp.ProductId
    inner JOIN catalog_productattributevalue pv ON pv.ProductId =  p.Id 
    inner JOIN catalog_productattribute pa ON pa.Id = pv.AttributeId AND pa.Name = 'ServiceProfile'

   
    WHERE MONTH(cp.CreationDate) = (MONTH(CURRENT_DATE()) - 1) AND YEAR(cp.CreationDate) = YEAR(CURRENT_DATE()) And cp.Status =2;
    
END";
    }
}
