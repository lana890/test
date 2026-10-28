using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class StoredProcedure
    {

        public static string FuncSplitNonDublicate = @"CREATE DEFINER=`root`@`%` FUNCTION `FuncSplitNonDublicate`(
                                                    Input_SiteId int
                                                    ) RETURNS int
                                                    DETERMINISTIC
                                                    BEGIN

                                                    DECLARE done INT DEFAULT FALSE;

                                                    declare TargetSlugId int;

                                                    declare Db cursor for (Select Id From core_SlugInfo where (SiteId =Input_SiteId or SiteId is null) 
                                                    and  Slug is not null
                                                    and (Slug like '%-Brand' or Slug like '%-Page' or Slug like '%-Post' or Slug like '%-Category' or Slug like '%-Product') 
                                                    group by  CultureId,SiteId ,Left(Slug,CHAR_LENGTH(Slug)-LOCATE('-',REVERSE(Slug)))
                                                    having 
                                                    count(Left(Slug,CHAR_LENGTH(Slug)-LOCATE('-',REVERSE(Slug)))) = 1 and count(CultureId)=1 and
                                                    count(SiteId)=1    Order By Slug 
                                                    );

                                                    DECLARE CONTINUE HANDLER FOR NOT FOUND set done=true;


                                                    open Db;
                                                    --  start transaction;
                                                    loop_label :loop 
                                                    Fetch Db Into TargetSlugId;

                                                    IF done THEN LEAVE loop_label;
                                                    END IF;
                                                    update core_SlugInfo set Slug=Left(Slug,CHAR_LENGTH(Slug)-LOCATE('-',REVERSE(Slug))) Where Id=TargetSlugId;
                                                    end loop;
                                                    -- Commit;
                                                    Close Db;  
                                                    RETURN 1;
                                                    END";








        public static string Email_Setting = @"CREATE DEFINER=`root`@`%` PROCEDURE `EmailSetting`()
            BEGIN
            declare TargetSiteId bigint;
            declare SmtpUsernameVar varchar(200);
            declare SmtpSenderEmailAdminVar varchar(200);
            declare SmtpPasswordVar varchar(200);
            declare SmtpServerVar varchar(200);
            declare SmtpSenderEmailVar varchar(200);
            declare SmtpPort bigint;
            declare SmtpRequiresAuthentication bool; 
             loop_label :loop 


                set TargetSiteId =( SELECT Id FROM core_site where Id not in (SELECT SiteId FROM core_emailssettings) limit 1 );
	
    
                if(TargetSiteId is null)then 
                leave loop_label;
                end if;
    
               select TargetSiteId;
   

    
                set SmtpUsernameVar = (Select Value From core_appsetting Where Id='SmtpUsername' And SiteId=TargetSiteId  );
                set SmtpSenderEmailAdminVar = (Select Value From core_appsetting Where Id='SmtpSenderEmailAdmin' And SiteId=TargetSiteId);
                set SmtpPasswordVar = (Select Value From core_appsetting Where Id='SmtpPassword' And SiteId=TargetSiteId  );
	            set SmtpServerVar= (Select Value From core_appsetting Where Id='SmtpServer' And SiteId=TargetSiteId  );
                set SmtpSenderEmailVar=(Select Value From core_appsetting Where Id='SmtpSenderEmail' And SiteId=TargetSiteId );
                set SmtpPort=(Select Value From core_appsetting Where Id='SmtpPort' And SiteId=TargetSiteId  );
	            set SmtpRequiresAuthentication=(Select IF( Value = 'true',1,0) From core_appsetting Where Id='SmtpRequiresAuthentication' And SiteId=TargetSiteId );
   
               Insert IGNORE Into core_emailssettings( SmtpUsername, SmtpPassword, SmtpServer, SmtpSenderEmail, SmtpPort, RequiresAuthentication, IsDefault, Status, SiteId, SmtpSenderEmailAdmin, Name)
	               values (
		            ifnull(SmtpUsernameVar,'RADTime@eskadenia.com'),
	                ifnull(SmtpPasswordVar,'R@DT!me#123'),
                    ifnull(SmtpServerVar,'mail@eskadenia.com'),
                    ifnull(SmtpSenderEmailVar,'mail@eskadenia.com'),
                    ifnull(SmtpPort,25),
                    ifnull(SmtpRequiresAuthentication,true),
	               (true),
	               (2),
                   TargetSiteId,
	               ifnull(SmtpSenderEmailAdminVar,'mail@eskadenia.com'),
                   ('Default Email Settings')
	               );
             end loop;

            END";


        public static string Rename_Slug_Category = @"CREATE DEFINER=`root`@`%` PROCEDURE `RenameSlugCategory`()
                                BEGIN

                                declare TargetId bigint;

                                loop_label:loop

                                set TargetId=(Select Id From core_category Where Slug is not null and Slug <> '' group by Slug having count(*) > 1 Order By Slug limit 1);


                                if(TargetId Is null)then

                                leave loop_label;

                                end if;

                                update core_category set Slug=CONCAT(Slug,TargetId) Where Id=TargetId ;


                                End loop;

                                END";



        public static string InvokeStoredProcedure = @"CREATE DEFINER=`root`@`%` PROCEDURE `InvokeStoredProcedure`()
                BEGIN

                 declare SiteId Int;

                declare done Int Default 0;

                declare Db cursor for Select Id From core_Site order by Id;

                declare continue Handler For Not Found Set done=1;

                open Db;
                 start transaction;


                 loop_One:Loop 
                 Fetch db Into SiteId ;
                if(done=1)then

                leave loop_One;

                end if;

		       select  FuncSplitNonDublicate(SiteId);
                end loop;

                Close Db;  

           

                END";


    }
}
