using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class TransferDataToSlugsTable
    {
        public static string TransferDataFromPagesToSlugs = @"Insert Into core_SlugInfo(
                                                            core_SlugInfo.Slug
                                                            ,core_SlugInfo.SiteId
                                                            ,core_SlugInfo.CultureId,
                                                            core_SlugInfo.CreatedById,
                                                            core_SlugInfo.CreationDate,
                                                            core_SlugInfo.Entity)
                                                            Select concat(Slug,'-Page'),SiteId,CultureId,CreatedById,CreationDate,4 From pages_page as p
                                                            where  (select count(*) from core_SlugInfo as s where s.slug = p.slug) =0 and  p.slug is not null and p.slug <> ''";



        public static string TransferDataFromPostToSlugs = @"Insert Into core_SlugInfo(
                                                              core_SlugInfo.Slug
                                                            ,core_SlugInfo.SiteId
                                                            ,core_SlugInfo.CultureId,
                                                            core_SlugInfo.CreatedById,
                                                            core_SlugInfo.CreationDate,
                                                            core_SlugInfo.Entity)
                                                            Select concat(Slug,'-Post'),SiteId,CultureId,CreatedById,CreationDate,2 From blogarchive_posts as b
                                                            where  (select count(*) from core_SlugInfo as s where s.slug = b.slug) =0 and b.slug is not null and b.slug <> ''";

        public static string TransferDataFromProductToSlugs = @"Insert Into core_SlugInfo(
                                                                  core_SlugInfo.Slug
                                                                ,core_SlugInfo.SiteId
                                                                ,core_SlugInfo.CultureId,
                                                                core_SlugInfo.CreatedById,
                                                                core_SlugInfo.CreationDate,
                                                                core_SlugInfo.Entity)
                                                                Select concat(Slug,'-Product'),SiteId,CultureId,CreatedById,CreationDate,1 From catalog_product as Pro
                                                                where  (select count(*) from core_SlugInfo as s where s.slug = Pro.slug) =0 and  Pro.slug is not null and Pro.slug <> ''";

        public static string TransferDataFromCategoryToSlugs = @"Insert Into core_SlugInfo(
                                                                core_SlugInfo.Slug 
                                                                ,core_SlugInfo.SiteId
                                                                ,core_SlugInfo.CultureId,
                                                                core_SlugInfo.CreatedById,
                                                                core_SlugInfo.CreationDate,
                                                                core_SlugInfo.Entity)
                                                                Select concat(Slug,'-Category'),SiteId,CultureId,CreatedById,CreationDate,3 From core_category as Cat
                                                                where  (select count(*) from core_SlugInfo as s where s.slug = Cat.slug) =0 and Cat.slug is not null and Cat.slug <> ''";
        
        
        public static string TransferDataFromBrandsToSlugs = @"Insert Into core_SlugInfo(
                                                                core_SlugInfo.Slug 
                                                                ,core_SlugInfo.SiteId
                                                                ,core_SlugInfo.CultureId,
                                                                core_SlugInfo.CreatedById,
                                                                core_SlugInfo.CreationDate,
                                                                core_SlugInfo.Entity)
                                                                Select concat(b.Slug,'-Brand'),b.SiteId,b.BrandsCultureId,b.CreatedById,b.CreationDate,5 From catalog_brand as b
                                                                where  (select count(*) from core_SlugInfo as s where s.slug = b.slug) =0 and b.slug is not null and b.slug <> ''";
    }
}
