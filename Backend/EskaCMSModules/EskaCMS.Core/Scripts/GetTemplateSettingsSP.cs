using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class GetTemplateSettingsSP
    {
        public static string GET_TEMPLATE_SETTINGS = @"CREATE DEFINER=`root`@`%` PROCEDURE `GET_TEMPLATE_SETTINGS`(



IN P_SLUG VARCHAR(500),



IN P_CULTUREID INT(100),



IN P_SITEID INT(100),



IN P_STATUS INT(100) ,



IN P_POSTSLUGTYPE INT(100),



IN P_PAGESLUGTYPE INT(100),



IN P_ROLEID INT(100)



)
BEGIN



DECLARE DPP_ID INT;



DECLARE CNT INT(10);



DECLARE SLUG_ENTITY INT(10);















declare SLUG_INFO_ID INT;



DECLARE EXITCURSOR INT(10);







DECLARE REPLACEID LONGTEXT;



DECLARE REPLACE_SETTINGS LONGTEXT;







declare TARGET_ID INT;



DECLARE NEWSETTINGS_P LONGTEXT;



DECLARE COMPONENTS text;



DECLARE THUMBNAIL_PUBLICURL text;



declare P_ERROR_MSG varchar(1000);











DECLARE REPLACE_SETTINGS CURSOR  FOR 



SELECT concat(ifnull(IdentifierCode,'TaiChi'),'*__*',SETTINGS)  FROM ELEMENTS_SHAREABLEELEMENTS



WHERE 



SITEID = P_SITEID ;







DECLARE CONTINUE HANDLER FOR NOT FOUND BEGIN



    GET DIAGNOSTICS CONDITION 1 @SQLCODE = RETURNED_SQLSTATE, @SQLERRM = MESSAGE_TEXT;



         SET EXITCURSOR = 1;



  END;  



  



  begin



 



  SET EXITCURSOR = 0;











	IF P_SLUG IS NULL OR P_SLUG = '' THEN 



    



    set DPP_ID = (SELECT DefaultParentPageId FROM CORE_SITE WHERE ID = P_SITEID); 



    



     set DPP_ID = (SELECT ParentId  FROM pages_page WHERE Id = DPP_ID );



	SELECT SlugInfoId , 4 into SLUG_INFO_ID , SLUG_ENTITY FROM pages_page WHERE ParentId = DPP_ID and CultureId = P_CULTUREID;



     



     SELECT Id , Entity , Slug into  SLUG_INFO_ID , SLUG_ENTITY , P_SLUG   FROM Core_SlugInfo  where Id= SLUG_INFO_ID;



    



    else 



    select Id , Entity into  SLUG_INFO_ID , SLUG_ENTITY  



			from Core_SlugInfo AS SI where upper(SI.SLUG) = UPPER(P_SLUG) AND SI.CULTUREID = P_CULTUREID AND P_SITEID = SI.siteId limit 1;



            



    



    END IF;



    















	IF SLUG_ENTITY = 4 THEN 



    



    



  select  t.id ,COALESCE(trv.SETTINGSBODY, tv.SettingsBody)   into TARGET_ID ,NEWSETTINGS_P  from 







		PAGES_PAGE AS P







      left join pages_pagetemplates as t







		on 







        p.PageTemplateId = t.id

        

        left join

        



	  pages_templateroleversions AS trv ON t.CURRENTVERSIONID = trv.PageTemplateVersionId and trv.RoleId = P_ROLEID







      left join 







      pages_templateversions as tv on 







      t.CurrentVersionId = tv.id





    



           



WHERE



		 SLUG_INFO_ID = p.SlugInfoId



        AND P.CULTUREID = P_CULTUREID



        AND P.STATUS = P_STATUS



        AND P.SITEID = P_SITEID limit 1;



        























end if;











 IF SLUG_ENTITY = 2 then



        



        







	SELECT    PT.ID , PTV.SettingsBody  into TARGET_ID ,NEWSETTINGS_P 



    



FROM



    BLOGARCHIVE_POSTS AS P



        LEFT JOIN



    PAGES_PAGETEMPLATES AS PT ON P.POSTTEMPLATEID = PT.ID



        LEFT JOIN



    PAGES_TEMPLATEVERSIONS AS PTV ON PT.CURRENTVERSIONID = PTV.ID



 LEFT JOIN



    CORE_TEMPLATEASSETS As AssetsTemp ON AssetsTemp.TemplateId=PT.ID and AssetsTemp.STATUS=P_STATUS



	LEFT JOIN



    CORE_ASSETS As Assets ON AssetsTemp.AssetId=Assets.ID and Assets.STATUS=P_STATUS











WHERE



       SLUG_INFO_ID = P.SLUGINFOID



        and P.SITEID = P_SITEID



        AND P.STATUS = P_STATUS



        AND P.CULTUREID = P_CULTUREID



        AND ((P.EXPIRYDATE IS NULL



        OR P.EXPIRYDATE > NOW())



        OR P.STATUSDATE > NOW());







		END IF;



        



















 IF SLUG_ENTITY = 5 then



        



        







	SELECT    PT.ID , PTV.SettingsBody  into TARGET_ID ,NEWSETTINGS_P 



    



FROM



    catalog_brand AS b

   LEFT JOIN

    CATALOG_BRANDDETAILS As BRANDDET ON BRANDDET.BRANDID=b.ID AND BRANDDET.CULTUREID=P_CULTUREID

 

        LEFT JOIN



    PAGES_PAGETEMPLATES AS PT ON BRANDDET.BrandTemplateId = PT.ID



        LEFT JOIN



    PAGES_TEMPLATEVERSIONS AS PTV ON PT.CURRENTVERSIONID = PTV.ID



  LEFT JOIN



    CORE_TEMPLATEASSETS As AssetsTemp ON AssetsTemp.TemplateId=PT.ID and AssetsTemp.STATUS=P_STATUS



	LEFT JOIN



    CORE_ASSETS As Assets ON AssetsTemp.AssetId=Assets.ID and Assets.STATUS=P_STATUS











WHERE



       SLUG_INFO_ID = BRANDDET.SlugInfoId



        and b.SITEID = P_SITEID



        AND b.STATUS = P_STATUS



        AND BRANDDET.CultureId = P_CULTUREID;







		END IF;



        























 IF SLUG_ENTITY = 1 then



        



        







	SELECT    PT.ID , PTV.SettingsBody  into TARGET_ID ,NEWSETTINGS_P 



    



FROM



    catalog_product AS p

    LEFT JOIN

    CATALOG_PRODUCTDETAILS AS PRODE ON PRODE.ProductId=p.Id and PRODE.CULTUREID=P_CULTUREID

   

        LEFT JOIN



    PAGES_PAGETEMPLATES AS PT ON PRODE.ProductTemplateId = PT.ID



        LEFT JOIN



    PAGES_TEMPLATEVERSIONS AS PTV ON PT.CURRENTVERSIONID = PTV.ID



  LEFT JOIN



    CORE_TEMPLATEASSETS As AssetsTemp ON AssetsTemp.TemplateId=PT.ID and AssetsTemp.STATUS=P_STATUS



	LEFT JOIN



    CORE_ASSETS As Assets ON AssetsTemp.AssetId=Assets.ID and Assets.STATUS=P_STATUS











WHERE



       SLUG_INFO_ID = PRODE.SlugInfoId



        and p.SITEID = P_SITEID



        AND p.STATUS = P_STATUS



        AND PRODE.cultureId = P_CULTUREID;







		END IF;



        



















 IF SLUG_ENTITY = 3 then



        



        







	SELECT    PT.ID , PTV.SettingsBody  into TARGET_ID ,NEWSETTINGS_P 



    



FROM



    core_category AS c

     LEFT JOIN

    CORE_CATEGORYDETAILS As CATDET ON CATDET.CULTUREID=P_CULTUREID and c.ID=CATDET.CATEGORYID

   

        LEFT JOIN



    PAGES_PAGETEMPLATES AS PT ON CATDET.CategoryTemplateId = PT.ID



        LEFT JOIN



    PAGES_TEMPLATEVERSIONS AS PTV ON PT.CURRENTVERSIONID = PTV.ID



 LEFT JOIN



    CORE_TEMPLATEASSETS As AssetsTemp ON AssetsTemp.TemplateId=PT.ID and AssetsTemp.STATUS=P_STATUS



	LEFT JOIN



    CORE_ASSETS As Assets ON AssetsTemp.AssetId=Assets.ID and Assets.STATUS=P_STATUS











WHERE 



       SLUG_INFO_ID = CATDET.SlugInfoId



        and c.SITEID = P_SITEID



        AND c.STATUS = P_STATUS



        AND CATDET.CultureId = P_CULTUREID;







		END IF;



        



















LBL1: BEGIN



OPEN REPLACE_SETTINGS;



LOOP



FETCH REPLACE_SETTINGS INTO REPLACEID;







select substring_index(REPLACEID,'*__*',-1) into REPLACE_SETTINGS ; 



 select substring_index(REPLACEID,'*__*',1) into REPLACEID;



 



    



IF EXITCURSOR = 1 THEN







CLOSE REPLACE_SETTINGS;



LEAVE LBL1;



ELSE







   



    SELECT



        REPLACE(NEWSETTINGS_P, concat('""', REPLACEID ,'""'),



        IFNULL(REPLACE_SETTINGS,'{}') )



        INTO NEWSETTINGS_P;







        END IF;















        end loop;



        end;



















	SELECT



    PT.Id as TemplateId,



     PT.AddtionalMetaTags as AddtionalMetaTags,



    PT.DESCRIPTION AS TemplateDescription,



    PTV.CSS AS TemplateCss,



    NEWSETTINGS_P AS TemplateSettingsBody,



    PTV.SCRIPTS AS TemplateScripts,



    PTV.TEMPLATECOMPONENTS AS TemplateComponents,



    PT.Id AS SourceId,



    TIF.Id AS ImportedID,



    M.Id AS MediaId,



    M.PublicUrl AS FileUrl,



    M.ContentType AS ContentType,



    M.ContentType AS MediaContentType,



    MD.Title AS FileName,



    AssetsTemp.ORDER As AssettemplateOrder,



    AssetsTemp.Id As AssettemplateId,



    Assets.TYPE As Type,



    Assets.SITEID As SiteId,



    Assets.NAME As AssetsName



    







FROM











    PAGES_PAGETEMPLATES AS PT



        LEFT JOIN



    PAGES_TEMPLATEVERSIONS AS PTV ON PT.CURRENTVERSIONID = PTV.ID



        LEFT JOIN



    pages_templateimportedfiles AS TIF ON TIF.TemplateVersionId = PTV.ID



        LEFT JOIN



    core_media AS M ON M.Id = TIF.MediaId



        LEFT JOIN



    core_mediadetails AS MD ON M.Id = MD.MediaId



   LEFT JOIN



    CORE_TEMPLATEASSETS As AssetsTemp ON AssetsTemp.TemplateId=PT.ID and AssetsTemp.STATUS=P_STATUS



	LEFT JOIN



    CORE_ASSETS As Assets ON AssetsTemp.AssetId=Assets.ID and Assets.STATUS=P_STATUS



















WHERE



        TARGET_ID = PT.Id;



















        end;



END";
    }
}



