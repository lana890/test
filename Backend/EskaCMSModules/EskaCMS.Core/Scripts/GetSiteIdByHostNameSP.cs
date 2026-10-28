using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class GetSiteIdByHostNameSP
    {
        public static string GET_SITE_ID_BY_HOST_NAME = @"CREATE DEFINER=`root`@`%` PROCEDURE `GET_SITE_ID_BY_HOST_NAME`(

IN P_HOSTNAME TEXT

)
BEGIN

	declare HostFlag varchar(4000);

    declare HostId int(255);





    set HostFlag = left(lower(P_HOSTNAME), 4000);

    set P_HOSTNAME = lower(P_HOSTNAME);

    loop_label :loop

    

    set HostId = (SELECT Id FROM core_siteshostnames 

					where (concat(lower(HostName) ,'/', lower(  if((Prefix is null ),'', Prefix)))   = lower(HostFlag) ) 

						or  (concat(lower(HostName) , lower(  if((Prefix is null ),'', Prefix)))   = lower(HostFlag) ) );

  

		if (HostFlag is null or HostFlag = '' or HostId is not null or position('/' in HostFlag) =0 ) then

			leave loop_label;

        end if;

        

        

        

        set HostFlag =(select substr(HostFlag ,1,length(HostFlag) - position('/' in reverse(HostFlag))));

        

    end loop;

    



    

	select 

		HN.Id as HostNameId,

        HN.Prefix as Prefix,

		HN.HostName as HostName,

        HN.CultureId as HostCultureId,

        

        HN.SiteId as SiteId,

        S.Name as SiteName,

        S.Description,

        S.Css,

        S.OrganizationId,

        S.CoreBranchId,

        S.IsADAuthRequired,

        S.ThumbnailId as SiteThumbnailId,

        S.Script as SiteScripts,

        S.DefaultParentPageId as SiteDefaultParentPageId,

        SC.CultureId as CultureName,

     

        Sl.Slug as DefaultSlug,

    

	

    

     

        

     

		TH.Id as ThemeId,

        TH.Name as ThemeName,

        TH.Colors as ThemeColors,

        TH.Classes as ThemeClasses,

        TH.Fonts as ThemeFonts,

        TH.Styles as ThemeStyles,

        TH.Scripts as ThemeScripts,

		THIF.Id as ImportedFileId,

        M.Id as FileId,

        M.PublicUrl as FileUrl,

        M.ContentType as ContentType,

        ss.Value as LoginType,

        

        Logo.PublicUrl as LogoUrl

      

    from 

		core_siteshostnames HN

	

    left join 

		core_site as S

		on S.Id = HN.SiteId

    

    left join 

		core_appsetting as SS

		on SS.SiteId = HN.SiteId and SS.Id like 'LoginType'

	 

	left join 

		pages_page as P

		on S.DefaultParentPageId = P.ParentId  and 

			 P.CultureId = HN.CultureId

            

	





    left join

        core_sluginfo as Sl

         On  P.SlugInfoId = Sl.Id

    

	

    left join 

		core_theme as TH

		on TH.SiteId = HN.SiteId and TH.IsDefault = true

    

     left join 

		core_themeimportedfiles as THIF

		on THIF.ThemeId = TH.Id 

        

    

    left join 

		core_media as M

		on M.Id = THIF.MediaId 

        

    

    

    left join 

		core_media as Logo

		on Logo.Id = S.ThumbnailId

        

    

   left join 

		core_sitescultures as SC

		on SC.Id = HN.CultureId 

	

	

	

     where HN.Id = HostId; 

END";

    }
}
