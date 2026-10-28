using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.DynamicForms.Data
{
    public class DynamicFormsCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<FormItem>().HasSeedData(
            //    new FormItem { Id = 1, Name = "Test Matrix", Description = "test From Seeding data 1", ThumbnailId = null, Status = GeneralEnums.EStatus.Published, CreatedById = 1, CreationDate = DateTimeOffset.Now, SiteId = 1, Icon = "" }

            //    );

            //modelBuilder.Entity<FormField>().HasSeedData(
            //   new FormField { Id = 1, Status = GeneralEnums.EStatus.Published, SiteId = 1, CategoryId = 1, InputType = EDynamicCriteriaInputTypes.Text, PricingMatrixId = 1, AdditionalInfo = "", ClassesNames = "", PlaceHolder = "", StaticApiLink = "", Validations = "" }

            //   );

            //modelBuilder.Entity<Classifications>().HasSeedData(
            //  new Classifications { Id = 1, Name = "Test Classifications", Description = "test From Seeding data 1", Status = EStatus.Published, CreatedById = 1, CreationDate = DateTime.Now, SiteId = 1, PricingMatrixId = 1 }

            //  );

            //modelBuilder.Entity<Criteria>().HasSeedData(
            //  new Criteria { Id = 1, Name = "Test Criteria", Status = EStatus.Published, CreatedById = 1, CreationDate = DateTime.Now, SiteId = 1, ClassificationId = 1, CategoryId = 1, Type = EDynamicFormsCriteriaTypes.Condition, SubCategoryId = 2, Value = "", ConditionsType = EDynamicCriteriaConditions.Equals }

            //  );

        }

    }
}

