using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class CanAccessSlugSP
    {
        public static string CanAccessSlug = @"CREATE DEFINER=`root`@`%` PROCEDURE `Can_Access_Slug`(

IN TargetId INT,

IN SLUG_ENTITY INT(10),

IN P_USERID INT,

IN SiteId INT)
BEGIN

DECLARE Exist TINYINT(1);





IF SLUG_ENTITY = 1 THEN 

    

If  exists(

 select  * From  pages_pagesroles As PR

        LEFT JOIN

    core_userrole AS ur ON (PR.RoleId IS NULL 



					OR PR.RoleId = 0

					OR (ur.RoleId = PR.RoleId))

                            

                    WHERE

				    ((TargetId = PR.PageId )  and ((PR.RoleId IS NULL OR PR.RoleId = 0 )OR (ur.UserId = P_USERID AND ur.RoleId = PR.RoleId))) limit 1) or not exists(

                    select  * From  pages_pagesroles where PageId = TargetId  limit 1 )

                    Then

                    select 1 As Exist;

                    else

                    select 0 As Exist;

               

                    END IF;













ELSEIF SLUG_ENTITY = 2 THEN 

    

If  exists(

 select  * From blogarchive_postroles As PR 

        LEFT JOIN



            core_userrole AS ur ON (PR.RoleId IS NULL OR PR.RoleId = 0

                        

            OR (ur.RoleId = PR.RoleId))

            WHERE

            ((TargetId = PR.PostId) and ((PR.RoleId IS NULL OR PR.RoleId = 0 )OR (ur.UserId = P_USERID AND ur.RoleId = PR.RoleId))) limit 1) 

            or not exists(select  * From blogarchive_postroles where TargetId = PostId limit 1)

            Then

            select 1 As Exist;

            else

            select 0 As Exist;

               

            END IF;

            ELSE



            select 1 As Exist;



            END IF;











END";
    }
}



