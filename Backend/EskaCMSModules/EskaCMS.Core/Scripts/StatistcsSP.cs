using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class StatistcsSP
    {
        public static string Statistics = @"CREATE DEFINER=`root`@`%` PROCEDURE `GetDataByKeyValue`(
    IN ConditonsearchKey LONGTEXT,
    IN ConditonsearchValue VARCHAR(255), 
    IN Conditions_comparison_operators INT(10),
    IN searchKey VARCHAR(255), 
    IN searchValue VARCHAR(255), 
    IN comparison_operators INT(10),
    IN Time_intervals INT(10),
    IN Type_mathematics INT(10),
    IN ComponentsettingsId VARCHAR(255),
    IN PageId INT(10)
)
BEGIN
    IF Type_mathematics = 1 THEN
        SELECT 
            CASE 
                WHEN Time_intervals = 1 THEN DATE_FORMAT(subForms.Creationdate, '%Y-%m-%d')
                WHEN Time_intervals = 2 THEN DATE_FORMAT(subForms.Creationdate - INTERVAL (WEEKDAY(subForms.Creationdate) + 1 - 6) DAY, '%Y-%m-%d')
                WHEN Time_intervals = 3 THEN DATE_FORMAT(subForms.Creationdate, '%Y-%m')
                WHEN Time_intervals = 4 THEN DATE_FORMAT(subForms.Creationdate, '%Y')
                WHEN Time_intervals = 5 THEN ''
            END AS TimeInterval,
            SUM(
                IFNULL(CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED), 0)
            ) AS Result
        FROM forms_submittedform As subForms
        Join forms_forms As Forms On Forms.ComponentSettingsId = ComponentsettingsId and Forms.PageId=PageId and subForms.FormId = Forms.Id 
        WHERE 
            subForms.SubmittedValue IS NOT NULL
            AND JSON_VALID(subForms.SubmittedValue)
            AND CASE comparison_operators
                WHEN 1 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) = searchValue
                WHEN 2 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) != searchValue
                WHEN 3 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) > CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 4 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) < CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 5 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) >= CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 6 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) <= CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                ELSE FALSE
            END
            AND CASE Conditions_comparison_operators
                WHEN 1 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) = ConditonsearchValue
                WHEN 2 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) != ConditonsearchValue
                WHEN 3 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) > CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 4 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) < CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 5 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) >= CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 6 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) <= CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                ELSE FALSE
            END
        GROUP BY TimeInterval;
    ELSEIF Type_mathematics = 2 THEN
        SELECT 
            CASE 
                WHEN Time_intervals = 1 THEN DATE_FORMAT(subForms.Creationdate, '%Y-%m-%d')
                WHEN Time_intervals = 2 THEN DATE_FORMAT(subForms.Creationdate - INTERVAL (WEEKDAY(subForms.Creationdate) + 1 - 6) DAY, '%Y-%m-%d')
                WHEN Time_intervals = 3 THEN DATE_FORMAT(subForms.Creationdate, '%Y-%m')
                WHEN Time_intervals = 4 THEN DATE_FORMAT(subForms.Creationdate, '%Y')
            END AS TimeInterval,
            COUNT(*) AS Result
        FROM forms_submittedform As subForms
		Join forms_forms As Forms On Forms.ComponentSettingsId = ComponentsettingsId and Forms.PageId=PageId and subForms.FormId = Forms.Id 

        WHERE 
            subForms.SubmittedValue IS NOT NULL
            AND JSON_VALID(subForms.SubmittedValue)
            AND CASE comparison_operators
                WHEN 1 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) = searchValue
                WHEN 2 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) != searchValue
                WHEN 3 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) > CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 4 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) < CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 5 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) >= CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 6 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) <= CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                ELSE FALSE
            END
            AND CASE Conditions_comparison_operators
                WHEN 1 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) = ConditonsearchValue
                WHEN 2 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) != ConditonsearchValue
                WHEN 3 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) > CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 4 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) < CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 5 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) >= CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 6 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) <= CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                ELSE FALSE
            END
        GROUP BY TimeInterval;
    ELSE
        SELECT 
            SubmittedValue AS Result
        FROM forms_submittedform As subForms
        Join forms_forms As Forms On Forms.ComponentSettingsId = ComponentsettingsId and Forms.PageId=PageId and subForms.FormId = Forms.Id 

        WHERE 
           subForms.SubmittedValue IS NOT NULL
            AND JSON_VALID(subForms.SubmittedValue)
            AND CASE comparison_operators
                WHEN 1 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) = searchValue
                WHEN 2 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) != searchValue
                WHEN 3 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) > CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 4 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) < CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 5 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) >= CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 6 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), searchKey)) AS SIGNED) <= CAST(searchValue AS SIGNED), 
                        FALSE
                    )
                ELSE FALSE
            END
            AND CASE Conditions_comparison_operators
                WHEN 1 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) = ConditonsearchValue
                WHEN 2 THEN JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) != ConditonsearchValue
                WHEN 3 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) > CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 4 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) < CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 5 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) >= CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                WHEN 6 THEN
                    IF(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) REGEXP '^[0-9]+$', 
                        CAST(JSON_UNQUOTE(JSON_EXTRACT(CAST(subForms.SubmittedValue AS JSON), ConditonsearchKey)) AS SIGNED) <= CAST(ConditonsearchValue AS SIGNED), 
                        FALSE
                    )
                ELSE FALSE
            END;
    END IF;
END";
    }
}
