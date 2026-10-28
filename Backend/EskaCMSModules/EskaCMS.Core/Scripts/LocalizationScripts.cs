using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public static class LocalizationScripts
    {

        public static string UpdateLocalizationKeys = @"update localization_resourceskeys  set IsShared  = 1 where SlugInfoId is null;";
        public static string UpdateLocalizationValues = @"UPDATE localization_resourcesvalue rv
                                                          JOIN localization_resourceskeys rk ON rv.KeyId = rk.Id
                                                          JOIN core_sluginfo sl ON rk.SlugInfoId = sl.Id
                                                          SET rv.SlugInfoId = rk.SlugInfoId
                                                          WHERE sl.CultureId = rv.CultureId;";

        public static string RemoveDuplicateLocalizationValues = @"DELETE t1
                                                                        FROM localization_resourcesvalue t1
                                                                        INNER JOIN (
                                                                        SELECT KeyId, CultureId, MIN(Id) AS MinId
                                                                        FROM localization_resourcesvalue
                                                                        GROUP BY KeyId, CultureId
                                                                        HAVING COUNT(*) > 1 ) t2
                                                                        ON t1.KeyId = t2.KeyId
                                                                        AND t1.CultureId = t2.CultureId
                                                                        AND t1.Id > t2.MinId;";
        
        public static string CreateIndexInLocalizationValues = @"CREATE UNIQUE INDEX `IX_Localization_ResourcesValue_CultureId_KeyId_SlugInfoId` ON `Localization_ResourcesValue` (`CultureId`, `KeyId`, `SlugInfoId`);";
    }
}
