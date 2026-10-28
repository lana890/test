using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Forms;

namespace EskaCMS.Forms.Data.FormsData
{
    public class FormsCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {


            //modelBuilder.Entity<EskaCMS.Entities.Entity.Forms.Forms>().HasSeedData(
            //new EskaCMS.Entities.Entity.Forms.Forms { Id = 1L, AdditionalDate = "", Source = Entities.Enum.Source.FormBuilder, Settings = "", ComponentSettingsId = "", Keys = "", CreatedById = 1, PageId = 1 },
            //new EskaCMS.Entities.Entity.Forms.Forms { Id = 2L, AdditionalDate = "", Source = Entities.Enum.Source.FormBuilder, Settings = "", ComponentSettingsId = "", Keys = "", CreatedById = 1, PageId = 1 });

            //modelBuilder.Entity<SubmittedForm>().HasIndex(x => new { x.CreatedById, x.Code }).IsUnique(true);


        }
    }
}

