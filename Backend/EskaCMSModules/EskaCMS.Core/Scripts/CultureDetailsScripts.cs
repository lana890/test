using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public class CultureDetailsScripts
    {
        public static string CategoryDetailsScript = @"
                                        Insert Into core_CategoryDetails(
                                        core_CategoryDetails.Name,
                                        core_CategoryDetails.SlugInfoId,
                                        core_CategoryDetails.MetaTitle,
                                        core_CategoryDetails.MetaKeyWords,
                                        core_CategoryDetails.MetaDescription,
                                        core_CategoryDetails.Description,
                                        core_CategoryDetails.CategoryTemplateId,
                                        core_CategoryDetails.CultureId,
                                        core_CategoryDetails.CategoryId)
                                        Select Name ,SlugInfoId,MetaTitle,MetaKeyWords,MetaDescription,Description
                                        ,CategoryTemplateId,CultureId,CultureParentId From core_category Where Status <>3; ";


        public static string CategoryNotParentChangToDeleted = @"Update core_category Set Status =3 where Id<>CultureParentId; ";

        public static string EditCultureParentIdCategory = @"Update core_category Set CultureParentId=Id Where CultureParentId Is null;";
        public static string EditParentIdForProduct = @"Update catalog_product Set ParentId=Id Where ParentId Is null;";

        public static string ProductDetails= @"Insert Into catalog_ProductDetails(
                                        catalog_ProductDetails.Name,
                                        catalog_ProductDetails.Name2,
                                        catalog_ProductDetails.SlugInfoId,
                                        catalog_ProductDetails.MetaTitle,
                                        catalog_ProductDetails.MetaKeyWords,
                                        catalog_ProductDetails.MetaDescription,
                                        catalog_ProductDetails.Description,
                                        catalog_ProductDetails.ShortDescription,
                                        catalog_ProductDetails.ProductTemplateId,
                                        catalog_ProductDetails.CultureId,
                                        catalog_ProductDetails.NormalizedName,
                                        catalog_ProductDetails.Specification,
                                        catalog_ProductDetails.ProductId)
                                        Select Name ,Name2,SlugInfoId
                                       ,MetaTitle,MetaKeyWords,MetaDescription
                                       ,Description,ShortDescription,ProductTemplateId,CultureId
                                       ,NormalizedName,Specification,ParentId From catalog_product where Status<>3;";
        public static string ProductNotParentChangeToDeleted = @"Update catalog_product Set Status =3 where Id<>ParentId and Status<>3;";

        public static string EditParentIdForBrand = @"Update catalog_Brand Set ParentId=Id Where ParentId Is null;

 ";
        public static string BrandDetails = @"Insert Into catalog_BrandDetails(

                                        catalog_BrandDetails.Name,
                                        catalog_BrandDetails.SlugInfoId,
                                 
                                        catalog_BrandDetails.Description,
                                        catalog_BrandDetails.BrandTemplateId,
                                        catalog_BrandDetails.CultureId,

                                        catalog_BrandDetails.BrandId)
                                        Select Name ,SlugInfoId
                                    
                                         ,Description,BrandTemplateId,BrandsCultureId
                                         ,ParentId From catalog_Brand; 



 ";


        public static string ProductAttributeDetailsScipt = @" Insert Into catalog_productattributedetails(
                                        catalog_productattributedetails.Name,
                                        catalog_productattributedetails.CultureId,
                                        catalog_productattributedetails.AttributeId)
                                        Select Name ,CultureId,productAttributeParentId From catalog_productattribute; 

 ";


        public static string ProductAttributeGroupDetailsScript= @" Insert Into catalog_productattributegroupdetails(
                                        catalog_productattributegroupdetails.Name,
                                        catalog_productattributegroupdetails.CultureId,
                                        catalog_productattributegroupdetails.ProductAttributeGroupId)
                                        Select Name ,CultureId,ParentId From catalog_productattributegroup; ";


        public static string ProductTemplateDetailsScript = @"  Insert Into catalog_producttemplatedetails(
                                        catalog_producttemplatedetails.Name,
                                        catalog_producttemplatedetails.CultureId,
                                        catalog_producttemplatedetails.ProductTemplateId)
                                        Select Name ,CultureId,ParentId From catalog_producttemplate;  ";

        public static string ProductOptionDetailsScript = @"  Insert Into catalog_productoptiondetails(
                                        catalog_productoptiondetails.Name,
                                        catalog_productoptiondetails.CultureId,
                                        catalog_productoptiondetails.ProductOptionId)
                                        Select Name ,CultureId,Id From catalog_productoption;  ";

    }
}
