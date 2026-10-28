using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Scripts
{
    public static class TransferDataToDynamicObject
    {

        public static string InertIntoFormItem = @"INSERT INTO DynamicForms_FormItem(`Id`,`Icon`,`ThumbnailId`,`Status`,`CreatedById`,`CreationDate`,`ModifiedById`,`ModificationDate`,`SiteId`,`AdditionalInfo`,`Type`)
SELECT Id,Icon,ThumbnailId,Status,CreatedById,CreationDate,ModifiedById,ModificationDate,SiteId,AdditionalInfo,1 FROM pricingquotations_pricingmatrix;";


        public static string InertIntoFormItemDetails = @"INSERT INTO DynamicForms_FormItemDetails(`Id`,`Name`,`Description`,`CultureId`,`FormItemId`)
                                                          SELECT Id,Name,Description,CultureId,PricingMatrixId FROM pricingquotations_pricingmatrixdetails;";

        public static string InertIntoFormField = @"INSERT INTO DynamicForms_FormField(Id, FormItemId, Status, InputType, OneOfOptionsType, SiteId, IsGeneral)
SELECT Id, PricingMatrixId, Status, InputType, OneOfOptionsType, SiteId, IsGeneral FROM pricingquotations_pricingspecification;";


        public static string InertIntoFormFieldDetails = @"INSERT INTO DynamicForms_FormFieldDetails(Id, FormFieldId, ClassesNames, PlaceHolder, Validations, AdditionalInfo, GroupId, CultureId, CategoryId, DataSourceId, StaticApiLink)
SELECT Id, PricingSpecificationId, ClassesNames, PlaceHolder, Validations, AdditionalInfo, GroupId, CultureId, CategoryId, DataSourceId, StaticApiLink FROM pricingquotations_pricingspecificationdetails;";


        public static string InertIntoFormFieldGroup = @"INSERT INTO DynamicForms_FormFieldGroup(Id, Name, Status, FormItemId)
                      SELECT Id, Name, Status, MatrixId FROM pricingquotations_specificationgroup;";

        public static string InertIntoClassification = @"INSERT INTO DynamicForms_Classifications(Id, Name, Description, Status, FormItemId, CreatedById, CreationDate, ModifiedById, ModificationDate, SiteId, IsGeneral)
                      SELECT Id, Name, Description, Status, PricingMatrixId, CreatedById, CreationDate, ModifiedById, ModificationDate, SiteId, IsGeneral FROM pricingquotations_classifications;";

        public static string InertIntoCriteria = @"INSERT INTO DynamicForms_Criteria(Id, Name, Type, OperationType, ConditionsType, Status, ClassificationId, Value, CategoryId, FormFieldId, CreatedById, CreationDate, ModifiedById, ModificationDate, SiteId, IsGeneral)
                     SELECT Id, Name, Type, OperationType, ConditionsType, Status, ClassificationId, Value, CategoryId, SpecificationId, CreatedById, CreationDate, ModifiedById, ModificationDate, SiteId, IsGeneral FROM pricingquotations_criteria;";

        public static string InertIntoCriteriaCategory = @"INSERT INTO DynamicForms_CategoryCriteria(Id, CriteriaId, SubCategoryId)
                     SELECT Id, CriteriaId, SubCategoryId FROM pricingquotations_categorycriteria;";

    }
}
