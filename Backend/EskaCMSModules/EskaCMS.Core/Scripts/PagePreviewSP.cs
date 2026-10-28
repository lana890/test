using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class PagePreviewSP
    {
        public static string GetPreviewPage = @"CREATE DEFINER=`root`@`%` PROCEDURE `GET_PREVIEWED_PAGE_SETTINGS`(
IN P_CODE VARCHAR(500),
IN P_CULTUREID INT(100),
IN P_SITEID INT(100),
IN P_STATUS INT(100) ,
IN P_PAGESLUGTYPE INT(100)
)
BEGIN




DECLARE EXITCURSOR INT(10);

DECLARE REPLACEID LONGTEXT;
DECLARE REPLACE_SETTINGS LONGTEXT;

declare TARGET_ID INT;
DECLARE NEWSETTINGS_P LONGTEXT;
DECLARE COMPONENTS text;
DECLARE THUMBNAIL_PUBLICURL text;


DECLARE REPLACE_SETTINGS CURSOR  FOR 
SELECT concat(ifnull(IdentifierCode,'TaiChi'),'*__*',SETTINGS)  FROM ELEMENTS_SHAREABLEELEMENTS
WHERE 
SITEID = P_SITEID ;-- AND CULTUREID = P_CULTUREID ;

DECLARE CONTINUE HANDLER FOR NOT FOUND BEGIN
    GET DIAGNOSTICS CONDITION 1 @SQLCODE = RETURNED_SQLSTATE, @SQLERRM = MESSAGE_TEXT;
         SET EXITCURSOR = 1;
  END;  
  /*
 DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN
    GET DIAGNOSTICS CONDITION 1 @SQLCODE = RETURNED_SQLSTATE, @SQLERRM = MESSAGE_TEXT;
         SET P_ERROR_MSG = @SQLERRM;
     --  insert into testbelal values ( P_ERROR_MSG);
  END; */
  begin
  
  SET EXITCURSOR = 0;
  
   select p.id , PV.SETTINGSBODY , PV.PAGECOMPONENTS , PM.PublicUrl  into TARGET_ID ,NEWSETTINGS_P , COMPONENTS , THUMBNAIL_PUBLICURL from 
   
	pages_pagepreviewcodes as P_Preview 
    LEFT JOIN
    PAGES_PAGEVERSIONS AS PV ON PV.Id = P_Preview.PageVersionId
    Left JOIN
     PAGES_PAGE AS P ON PV.PageId = P.Id
    LEFT JOIN
    core_media AS PM ON PM.Id = P.THUMBNAILID

	WHERE
		P_Preview.Code = P_CODE
        AND P.CULTUREID = P_CULTUREID
        AND P.STATUS = P_STATUS
        AND P.SITEID = P_SITEID limit 1;
        
-- ----------------- END OF GET PAGE ID ----------------------------------------------------------------------------

-- BELAL -----------------------------------------------------------

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

--  END OF BELAL -----------------------------------------------------------

	SELECT
    P.ID AS Id,
    P.NAME AS Name,
    P.PARENTID AS ParentId,
    P.SITEID AS SiteId,
    P.STATUS AS Status,

    P_PAGESLUGTYPE AS SlugType,
    P.DESCRIPTION AS Description,
    P.KEYWORDS AS Keywords,
    LSlug.Slug AS Slug,
    P.PAGETEMPLATEID AS PageTemplateId,
    P.CURRENTVERSIONID AS PageCurrentVersionId,
    P.ADDITIONALCONTENT AS AdditionalContent,
    P.ISLOGINREQUIRED AS IsLoginRequired,
    P.THUMBNAILID AS ThumbnailId,
    THUMBNAIL_PUBLICURL AS ThumbnailUrl,
    P.CULTUREID AS CultureId,
     COMPONENTS AS PageComponents,
    NEWSETTINGS_P AS PageSettingsBody,
    LSlug.Slug As LoginSlug

FROM
    PAGES_PAGE AS P
    left Join Core_SlugInfo as LSlug on  LSlug.Id = P.SlugInfoId

WHERE

        TARGET_ID = p.ID
        AND P.CULTUREID = P_CULTUREID
        AND P.STATUS = P_STATUS
        AND P.SITEID = P_SITEID;


        end;

END";
    }
}
