using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class FillSlugId
    {
        public static string FillSlugIdPages = @"update pages_page as p  set SlugInfoId = (
                                                        Select Id From core_SlugInfo
                                                        where core_SlugInfo.CultureId=p.CultureId 
					                                                        And core_SlugInfo.SiteId=p.SiteId
					                                                        And core_SlugInfo.Slug=concat(p.Slug,'-Page')
                                                                            limit 1)
                                                                            where Id > 0";



        public static string FillSlugIdPosts = @"update blogarchive_posts as p  set SlugInfoId = (
                                                        Select Id From core_SlugInfo
                                                        where core_SlugInfo.CultureId=p.CultureId 
					                                                        And core_SlugInfo.SiteId=p.SiteId
					                                                        And core_SlugInfo.Slug=concat(p.Slug,'-Post')
                                                                            limit 1)
                                                                            where Id > 0";

        public static string FillSlugIdProduct = @"update catalog_product as p  set SlugInfoId = (
                                                        Select Id From core_SlugInfo
                                                        where core_SlugInfo.CultureId=p.CultureId 
					                                                        And core_SlugInfo.SiteId=p.SiteId
					                                                        And core_SlugInfo.Slug=concat(p.Slug,'-Product')
                                                                            limit 1)
                                                                            where Id > 0";

        public static string FillSlugIdCategory = @"update core_category as p  set SlugInfoId = (
                                                        Select Id From core_SlugInfo
                                                        where core_SlugInfo.CultureId=p.CultureId 
					                                                        And core_SlugInfo.SiteId=p.SiteId
					                                                        And core_SlugInfo.Slug=concat(p.Slug,'-Category')
                                                                            limit 1)
                                                                            where Id > 0";


        public static string FillSlugIdBrand = @"update catalog_brand as b set b.SlugInfoId = (
	                                                Select Id From core_SlugInfo
	                                                where core_SlugInfo.CultureId=b.BrandsCultureId 
						                            And core_SlugInfo.SiteId=b.SiteId
						                            And core_SlugInfo.Slug=concat(b.Slug,'-Brand')
						                            limit 1)
						                            where Id > 0;";

    }
}
