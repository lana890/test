using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts.ECommerce
{
    
    public static class AddressScripts
    {
        public static string InsertCountryDetails = @"INSERT INTO Address_CountryDetails
                                                            (`Id`,
                                                            `Name`,
                                                            `CultureId`,
                                                            `CountryId`)
                                                            SELECT null As Id, Name, 1 As CultureId, Id FROM addresses_country where Id!=null ;";
        public static string InsertstateDetails = @"INSERT INTO Address_StateOrProvinceDetails
                                                                    (`Id`,
                                                                    `Name`,
                                                                    `CultureId`,
                                                                    `StateOrProvinceId`)
                                                                    SELECT null As Id, Name, 1 As CultureId, Id FROM addresses_stateorprovince where Id>0;";
        public static string InsertdistrictDetails = @"INSERT INTO Address_DistrictDetails
                                                                        (`Id`,
                                                                        `Name`,
                                                                        `CultureId`,
                                                                        `DistrictId`)
                                                                        SELECT null As Id, Name, 1 As CultureId, Id FROM addresses_district where Id>0;";
    }
}


