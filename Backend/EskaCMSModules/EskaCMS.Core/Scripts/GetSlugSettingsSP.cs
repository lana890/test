using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    // we don't use this any more
	public class GetSlugSettingsSP
	{
		public static string GET_SLUG_SETTINGS = @"CREATE DEFINER=`root`@`%` PROCEDURE `GET_SLUG_SETTINGS`(



IN P_SLUG VARCHAR(500),



IN P_CULTUREID INT(100),



IN P_SITEID INT(100),



IN P_STATUS INT(100) ,



IN P_POSTSLUGTYPE INT(100),



IN P_PAGESLUGTYPE INT(100) ,







IN P_PRODUCTSLUGTYPE INT(100) ,







IN P_BRANDSLUGTYPE INT(100) ,



IN P_CATEGORYSLUGTYPE INT(100) ,



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

DECLARE THUMBNAIL_WIDTH int;

DECLARE THUMBNAIL_HEIGHT int;



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



    select p.id , COALESCE(PRV.SETTINGSBODY, PV.SETTINGSBODY) , COALESCE(PRV.PAGECOMPONENTS, PV.PAGECOMPONENTS) , PM.PublicUrl , PM.Width , PM.Height  into TARGET_ID ,NEWSETTINGS_P , COMPONENTS , THUMBNAIL_PUBLICURL , THUMBNAIL_WIDTH , THUMBNAIL_HEIGHT 



   from  PAGES_PAGE AS P

      

            LEFT JOIN

            pages_pagesrolesversions AS PRV ON P.CURRENTVERSIONID = PRV.PageVersionId and PRV.RoleId = P_ROLEID

            LEFT JOIN

            core_media AS PM ON PM.Id = P.THUMBNAILID

            LEFT JOIN

            pages_pageversions AS PV ON P.CURRENTVERSIONID = PV.Id



           



WHERE



		SLUG_INFO_ID = p.slugInfoId



        AND P.CULTUREID = P_CULTUREID



        AND P.STATUS = P_STATUS



        AND P.SITEID = P_SITEID limit 1;


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



    P.ID AS Id,



    P.NAME AS Name,



    P.PARENTID AS ParentId,



    P.SITEID AS SiteId,



    P.STATUS AS Status,



    p_SLUG AS Slug,



    P_PAGESLUGTYPE AS SlugType,



    P.DESCRIPTION AS Description,



    P.KEYWORDS AS Keywords,



    P.PAGETEMPLATEID AS PageTemplateId,



    P.CURRENTVERSIONID AS PageCurrentVersionId,



    P.ADDITIONALCONTENT AS AdditionalContent,



    P.ISLOGINREQUIRED AS IsLoginRequired,



    P.THUMBNAILID AS ThumbnailId,



    THUMBNAIL_PUBLICURL AS ThumbnailUrl,

    THUMBNAIL_WIDTH AS ImageWidth,

    THUMBNAIL_HEIGHT AS ImageHeight,



    P.CULTUREID AS CultureId,



     COMPONENTS AS PageComponents,



    NEWSETTINGS_P AS PageSettingsBody,



    LSlug.Slug As LoginSlug







FROM



    PAGES_PAGE AS P



    left Join PAGES_PAGE as LPage



      on p.ParentLoginPageId = LPage.ParentId



        AND LPage.CULTUREID = P_CULTUREID



      left Join Core_SlugInfo as LSlug on  LSlug.Id = LPage.SlugInfoId







WHERE







        TARGET_ID = p.ID



        AND P.CULTUREID = P_CULTUREID



        AND P.STATUS = P_STATUS



        AND P.SITEID = P_SITEID;

        end if;



 IF SLUG_ENTITY = 1 then



    SELECT



    P.ID AS Id,



    P_PRODUCTSLUGTYPE AS SlugType,



    PRODE.NAME AS Name,



    p_SLUG AS Slug,



    P.SITEID AS SiteId,



    PRODE.DESCRIPTION AS Description,



    PRODE.SHORTDESCRIPTION AS ShortDescription,



    PRODE.ADDTIONALMETATAGS AS ProductAdditionalMetatags,


    CM.PUBLICURL AS ThumbnailImageUrl,

    CM.Width AS ImageWidth,

    CM.Height AS ImageHeight,



    SLU.Slug AS Slug,



    PRODE.PRODUCTTEMPLATEID AS TemplateId,



    PRODE.CULTUREID AS CultureId,



    CM.Id AS ThumbnailImageUrl







FROM



    CATALOG_PRODUCT AS P



    LEFT JOIN



    CATALOG_PRODUCTDETAILS AS PRODE ON PRODE.ProductId=P.Id and PRODE.CULTUREID=P_CULTUREID



        LEFT JOIN



   CORE_MEDIA AS CM ON P.THUMBNAILIMAGEID = CM.ID



     LEFT JOIN



   CATALOG_PRODUCTATTRIBUTEVALUE AS PAV ON PAV.PRODUCTID = P.ID



     LEFT JOIN



   CORE_SLUGINFO AS SLU ON SLU.ID = PRODE.SLUGINFOID











WHERE



        SLUG_INFO_ID = PRODE.SLUGINFOID



        AND P.SITEID = P_SITEID



        AND P.STATUS = P_STATUS



        AND PRODE.CULTUREID = P_CULTUREID;



        END IF;


 IF SLUG_ENTITY = 3 then

    SELECT



    CAT.ID AS CategoryId,
    CATDET.NAME AS CategoryName,
    CAT.SiteId AS SiteId,
    CATDET.DESCRIPTION AS Description,
    CAT.CATEGORYTYPEID AS CategoryTypeId,
    P_CATEGORYSLUGTYPE AS SlugType,
    CAT.DISPLAYORDER AS DisplayOrder,
    CAT.IncludeInMenu AS IncludedInMenu,
    CATDET.CATEGORYTEMPLATEID AS CategoryTemplateId,
    CATDET.METADESCRIPTION AS MetaDescription,
    CATDET.METATITLE AS MetaTitle,
    CATDET.METAKEYWORDS AS MetaKeywords,
    CAT.PARENTID AS ParentId,
    SLU.Slug AS Slug,
    CAT.STATUS AS Status,
    CAT.THUMBNAILIMAGEID AS ThumbnailImageId,
    P_SLUG AS P_SLUG,
    CM.ID AS MediaId,
    CM.PUBLICURL AS Imageurl,

    CM.Width AS ImageWidth,

    CM.Height AS ImageHeight,
    CM.FILENAME As FileName,
    CATDET.CULTUREID AS CultureId,
    PRO.ID AS ProductId,
    PRODE.NAME AS ProductName,
    INFO.SLUG AS ProductSlug,
    PRO.ISCALLFORPRICING AS IsCallForPricing,
    PRO.ISALLOWTOORDER AS IsAllowToOrder,

    PRO.SITEID AS SiteId,

    PRO.STOCKTRACKINGISENABLED AS StockTrackingIsEnabled,
    PRODE.DESCRIPTION AS ProductDescription,
    PRODE.SHORTDESCRIPTION AS ProductShortDescription,
    PRODE.ADDTIONALMETATAGS AS ProductAdditionalMetatags,
    PRO.SKU AS SKU,
    PRO.REVIEWSCOUNT AS ReviewsCount,
    PRO.RATINGAVERAGE AS RatingAverage,
    MED.ID As ThumbnailImageId,
    MED.PUBLICURL AS ThumbnailImageUrl
    FROM
    CORE_CATEGORY AS CAT
    LEFT JOIN
    CORE_CATEGORYDETAILS As CATDET ON CATDET.CULTUREID=P_CULTUREID and CAT.ID=CATDET.CATEGORYID
        LEFT JOIN
   CORE_MEDIA AS CM ON CAT.THUMBNAILIMAGEID = CM.ID
       LEFT JOIN
       CORE_SLUGINFO AS Slu ON Slu.ID = CATDET.SLUGINFOID
       LEFT JOIN
       CATALOG_PRODUCTCATEGORY AS PROCAT On PROCAT.CATEGORYID= CAT.ID
       LEFT JOIN
       CATALOG_PRODUCT AS PRO On PRO.ID= PROCAT.PRODUCTID
       LEFT JOIN

       CORE_MEDIA AS MED On MED.ID= PRO.THUMBNAILIMAGEID
	LEFT JOIN
    CATALOG_PRODUCTDETAILS AS PRODE ON PRODE.ProductId=PRO.Id and PRODE.CULTUREID=P_CULTUREID
       LEFT JOIN
       CORE_SLUGINFO AS INFO On INFO.ID= PRODE.SLUGINFOID
WHERE
        SLUG_INFO_ID = CATDET.SLUGINFOID
        AND CAT.SITEID = P_SITEID
        AND CAT.STATUS = P_STATUS
        AND CATDET.CULTUREID = P_CULTUREID;
        END IF;
 IF SLUG_ENTITY = 5 then
    SELECT
    BRAN.ID AS BrandId,
    BRANDDET.NAME AS BrandName,
    BRANDDET.DESCRIPTION AS Description,
    SLU.Slug AS Slug,
    BRAN.STATUS AS Status,
    P_BRANDSLUGTYPE AS SlugType,
    BRAN.THUMBNAILID AS ThumbnailImageId,
    P_SLUG AS P_SLUG,
    MED.PUBLICURL AS PublicUrl,

    MED.Width AS ImageWidth,

    MED.Height AS ImageHeight,
    BRANDDET.CULTUREID AS CultureId
FROM
    CATALOG_BRAND AS BRAN
    LEFT JOIN
    CATALOG_BRANDDETAILS As BRANDDET ON BRANDDET.BRANDID=BRAN.ID AND BRANDDET.CULTUREID=P_CULTUREID
        LEFT JOIN
   CORE_MEDIA AS MED ON BRAN.THUMBNAILID = MED.ID
       LEFT JOIN
       CORE_SLUGINFO AS Slu ON Slu.ID = BRANDDET.SLUGINFOID
WHERE
        SLUG_INFO_ID = BRANDDET.SLUGINFOID
        AND BRAN.SITEID = P_SITEID
        AND BRAN.STATUS = P_STATUS
        AND BRANDDET.CULTUREID=P_CULTUREID;
        END IF;
 IF SLUG_ENTITY = 2 then
   SELECT DISTINCT
    P.ID AS PostId,
    P.POSTTEMPLATEID AS PostTemplateId,
    p_SLUG AS PostSlug,
    P.CREATEDBYID AS CreatedById,
    P.TITLE AS PostTitle,
    P.KEYWORDS AS PostKeywords,
    P.SITEID AS SiteId,
    P.POSTCONTENT AS PostContent,
    P.DESCRIPTION AS PostDescription,
    P.SHORTDESCRIPTION AS PostShortDescription,
    P.METADESCRIPTION AS PostMetaDescription,
    P_POSTSLUGTYPE AS SlugType,
    P.RELATEDCATEGORIESSETTINGS AS PostRelatedCategoriesSettings,
    P.SETTINGS AS PostSettings,
    P.EXPIRYDATE AS POSTEXPIRYDATE,
    P.ADDTIONALMETATAGS AS POSTADDTIONALMETATAGS,
    P.ISLOGINREQUIRED AS ISLOGINREQUIRED,
    P.CULTUREID AS POSTCULTUREID,
    P.THUMBNAILID AS POSTTHUMBNAILID,
    P.PARENTID AS ParentPostId,
    P.PublishDate AS PublishDate,
    CM.PUBLICURL AS PostPublicUrl,
    CM.ID AS PostMediaId,
    CC.ID AS PostCategoryId,
    CATDET.NAME AS PostCategoryName,
    TumbnailM.PublicUrl AS PostImage,

    TumbnailM.Width AS ImageWidth,

    TumbnailM.Height AS ImageHeight,
    CM.Id AS MediaId,
    CM.PublicUrl AS FileUrl,
    CM.ContentType AS ContentType,

    CM.ContentType AS MediaContentType,

    MD.Title AS FileName,

    P_SLUG AS P_SLUG,

    LSlug.Slug As LoginSlug
FROM
    BLOGARCHIVE_POSTS AS P
        LEFT JOIN
    BLOGARCHIVE_POSTMEDIAS AS PM ON P.ID = PM.POSTID
        LEFT JOIN
    CORE_MEDIA AS CM ON PM.MEDIAID = CM.ID
        LEFT JOIN
    BLOGARCHIVE_POSTSCATEGORIES AS PC ON P.ID = PC.POSTID
        LEFT JOIN

    CORE_CATEGORY AS CC ON PC.CATEGORYID = CC.ID

        LEFT JOIN
    CORE_CATEGORYDETAILS As CATDET ON CATDET.CULTUREID=P_CULTUREID and CC.ID=CATDET.CATEGORYID
        LEFT JOIN
    core_media AS TumbnailM ON TumbnailM.Id = P.THUMBNAILID
LEFT JOIN
    core_mediadetails AS MD ON CM.id = MD.MediaId
        AND MD.CultureId = P_CULTUREID
left Join PAGES_PAGE as LPage
      on P.LoginPageId = LPage.ParentId
        AND LPage.CULTUREID = P_CULTUREID

      left Join Core_SlugInfo as LSlug on  LSlug.Id = LPage.SlugInfoId
WHERE
        SLUG_INFO_ID = P.SLUGINFOID
        AND P.SITEID = P_SITEID
        AND P.STATUS = P_STATUS
        AND P.CULTUREID = P_CULTUREID
        AND ((P.EXPIRYDATE IS NULL OR P.EXPIRYDATE > NOW()) OR P.STATUSDATE > NOW());
        END IF;
        end;
END";


	}
}



