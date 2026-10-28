using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Engine.Scripts
{
    public static class ActiveDirectorySP
    {

        public static string AddUpdateUserOptionsSP = @"CREATE DEFINER=`root`@`%` PROCEDURE `AddUpdateUserOptionsSP`(
    IN Name varchar(255),
    IN Value varchar(255),
    IN UserId INT(100),
    IN currentDateTime DATETIME(6)
    
)
BEGIN
    DECLARE userOption_id_param bigint;
    
    SELECT Id INTO userOption_id_param
    FROM core_useroptions As userOp
    WHERE UserId = userOp.UserId && upper( userOp.PropertyName)=upper(Name)
    LIMIT 1;
    
 IF userOption_id_param IS NOT NULL THEN
        UPDATE core_useroptions As op
        SET op.Value = Value, op.NormalizedValue =upper(Value) , op.ModificationDate = currentDateTime
        WHERE Id = userOption_id_param;
    ELSE
        INSERT INTO core_useroptions (
`PropertyName`,
`ControlType`,
`Value`,
`UserId`,
`IsVisible`,
`IsEditable`,
`NormalizedPropertyName`,
`NormalizedValue`,
`CreationDate`
        ) VALUES (
            Name, 
            0, 
            Value,
            UserId, 
            True, 
            True, 
            upper(Name), 
            upper(Value),
            currentDateTime
        );
        SET userOption_id_param = LAST_INSERT_ID();
    END IF;

    COMMIT;
END";
        public static string AddUserGroupSP = @"CREATE DEFINER=`root`@`%` PROCEDURE `AddUserGroupSP`(
    IN InGroupId INT(100),
    IN InUserId INT(100)
)
BEGIN
    DECLARE group_id_param bigint;
    
    SELECT CustomerGroupId INTO group_id_param
    FROM core_customergroupuser As UsGroup
    WHERE InUserId = UsGroup.UserId
    LIMIT 1;

    IF group_id_param IS not NULL THEN
		Delete From core_customergroupuser where UserId=InUserId;
        INSERT INTO core_customergroupuser (UserId, CustomerGroupId)
                               VALUES (InUserId, InGroupId);
        SET group_id_param = LAST_INSERT_ID();
    END IF;
    
    COMMIT;
END";
        public static string AddUserRolesSP = @"CREATE DEFINER=`root`@`%` PROCEDURE `AddUserRolesSP`(
    IN InRoleId INT(100),
    IN InUserId INT(100)
)
BEGIN
    DECLARE username_param VARCHAR(255);
    DECLARE role_id_param bigint;
    
    SELECT usRole.RoleId INTO role_id_param
    FROM core_userrole As usRole
    WHERE InUserId = usRole.UserId
    LIMIT 1;

    IF role_id_param IS NULL THEN
		Delete From core_userrole where UserId=InUserId;
        INSERT INTO core_userrole (UserId, RoleId)
                               VALUES (InUserId, InRoleId);
        SET role_id_param = LAST_INSERT_ID();
    END IF;
    
    COMMIT;
END";
        public static string CreateOrGetCustomerGroup = @"CREATE DEFINER=`root`@`localhost` PROCEDURE `CreateOrGetCustomerGroup`(
    IN DepartmentEn VARCHAR(255),
    IN SiteId INT(100),
    IN DepartmentCultureIdEn INT(100),
    IN DepartmentAr VARCHAR(255),
    IN DepartmentCultureIdAr VARCHAR(255),
    IN AdditionalInfo VARCHAR(255)
    
)
BEGIN
    DECLARE username_param VARCHAR(255);
    DECLARE emp_id_param bigint;
    DECLARE cus_group_type_Id bigint;
    
    DECLARE DepartmentsGroupTypeId bigint;
    SELECT CAST(Value AS SIGNED) INTO DepartmentsGroupTypeId
    FROM core_appsetting
    WHERE Module='DocumentNumber' AND Id='DepartmentsGroupTypeId' AND SiteId=SiteId
    LIMIT 1;
    
    
    
    IF DepartmentAr Is not null or  DepartmentAr!=""  Then
    SELECT CustomerGroupId INTO emp_id_param
    FROM usersandrolesmanagement_customergroupdetails
    WHERE  lower(Name) = lower(DepartmentAr)  
    LIMIT 1;
        END IF;

	IF DepartmentEn Is not  null or  DepartmentEn!="" Then
    SELECT CustomerGroupId INTO emp_id_param
    FROM usersandrolesmanagement_customergroupdetails
    WHERE  lower(Name) = lower(DepartmentAr)  
    LIMIT 1;
        END IF;

   IF emp_id_param IS NOT NULL  THEN
        UPDATE core_customergroup
        SET AdditionalInfo = AdditionalInfo
        WHERE Id = emp_id_param;
    ELSE

    
        INSERT INTO core_customergroup (
            
            `AdditionalInfo`,
            `IsActive`,
            `IsDeleted`,
            `CreatedOn`,
            `LatestUpdatedOn`,
            `FireBaseTopic`,
            `SiteId`,
            `MaxSessionsNumber`,
            `PasswordLoginAttempts`,
            `Email`,
            `PasswordExpiryInDays`,
            `EnableTwoFactorAuthentication`,
            `GroupTypeId`,
            `ThumbnailId`
        ) VALUES (
           AdditionalInfo, 
            TRUE, 
            FALSE, 
            NOW(), 
            NOW(), 
            'ADDepartment', 
            SiteId, 
            1000000, 
            100000, 
            null, 
            0, 
            false, 
            DepartmentsGroupTypeId, 
            NULL
        );
        SET emp_id_param = LAST_INSERT_ID();
        INSERT INTO usersandrolesmanagement_customergroupdetails
						(
						`Name`,
						`Description`,
						`CultureId`,
						`CustomerGroupId`)
						VALUES(
						CASE
        WHEN DepartmentEn IS NOT NULL or DepartmentEn!=""  THEN DepartmentEn
        ELSE DepartmentAr
    END,
    CASE
        WHEN DepartmentEn IS NOT NULL or  DepartmentEn!=""  THEN DepartmentEn
        ELSE DepartmentAr
    END,
						DepartmentCultureIdEn,
						emp_id_param);
   INSERT INTO usersandrolesmanagement_customergroupdetails
						(
						`Name`,
						`Description`,
						`CultureId`,
						`CustomerGroupId`)
						VALUES(
					CASE
                       WHEN DepartmentAr IS NOT NULL or  DepartmentAr!="" THEN DepartmentAr
                      ELSE DepartmentEn
                      END,
                     CASE
                     WHEN DepartmentAr IS NOT NULL or  DepartmentAr!="" THEN DepartmentAr
                      ELSE DepartmentEn
                       END,
						DepartmentCultureIdAr,
						emp_id_param);


    END IF;
    
    COMMIT;
    Select emp_id_param;
END";
        public static string CreateOrGetRole = @"CREATE DEFINER=`root`@`localhost` PROCEDURE `CreateOrGetRole`(
    IN Department VARCHAR(255),
    IN SiteId INT(100),
    IN RoleType INT(100)
)
BEGIN
    DECLARE username_param VARCHAR(255);
    DECLARE emp_id_param bigint;
    
    SELECT Id INTO emp_id_param
    FROM core_role
    WHERE (Department Is null and  Name is null) or (lower(Name) = lower( Department))
    LIMIT 1;

    IF emp_id_param IS NULL THEN
        INSERT INTO core_role (
           `SiteId`,
`Type`,
`Name`,
`NormalizedName`,
`ConcurrencyStamp`,
`IsSystemAdmin`
        ) VALUES (
            SiteId, 
            RoleType, 
           Department, 
            upper(Department), 
             UUID(),
            FALSE
        );
        SET emp_id_param = LAST_INSERT_ID();
    END IF;
    
    COMMIT;
    Select emp_id_param;

END";
        public static string GetCurrentCulturesSP = @"CREATE DEFINER=`root`@`%` PROCEDURE `GetCurrentCulturesSP`(
    IN SiteId_param INT
)
BEGIN
    SELECT US.Id AS CultureId,
           US.CultureId AS CultureCode
    FROM core_sitescultures AS US
    WHERE US.SiteId = SiteId_param;
END";
        public static string GetUserDetailsByUsername = @"CREATE DEFINER=`root`@`%` PROCEDURE `GetUserDetailsByUsername`(
    IN P_UserName VARCHAR(256)
)
BEGIN
    SELECT US.Id AS Id,
           US.FullName AS FullName,
           US.Status AS Status,
           US.ParentId AS ParentId,
           US.UserName AS UserName,
           US.NormalizedUserName AS NormalizedUserName,
           US.Email AS Email,
           US.NormalizedEmail AS NormalizedEmail,
           CGU.CustomerGroupId AS CustomerGroupId,
           UR.RoleId AS RoleId
    FROM core_user AS US
    LEFT JOIN core_customergroupuser AS CGU ON CGU.UserId = US.Id
    LEFT JOIN core_UserRole AS UR ON UR.UserId = US.Id
    WHERE US.UserName = P_UserName;
END";
        public static string UpdateParentId = @"CREATE DEFINER=`root`@`%` PROCEDURE `UpdateParentId`(IN username_param VARCHAR(255), IN user_id_param INT)
BEGIN
    DECLARE emp_id INT;

    SELECT Id INTO emp_id
    FROM core_user
    WHERE  lower( UserName) =lower( username_param)
    LIMIT 1;

    IF emp_id IS NOT NULL THEN
        UPDATE core_user
        SET ParentId = user_id_param
        WHERE Id = emp_id;
    END IF;
    
    COMMIT;
END";

        public static string UpdateUserGeneralInfoSP = @"CREATE DEFINER=`root`@`localhost` PROCEDURE `UpdateUserGeneralInfoSP`(
    IN Name varchar(255),
    IN UserId INT(100)
)
BEGIN
    DECLARE User_id_param bigint;
    
    SELECT Id INTO User_id_param
    FROM core_user As user1
    WHERE user1.Id = UserId
    LIMIT 1;

    IF User_id_param IS not NULL THEN
      UPDATE core_user As user2
        SET user2.FullName = Name
        WHERE user2.Id = UserId;
    END IF;
    
    COMMIT;
END";

    }
}
