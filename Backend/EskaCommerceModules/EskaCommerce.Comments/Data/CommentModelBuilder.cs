using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace EskaCommerce.Comments.Data
{
    public class CommentCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<SiteSettings>().HasSeedData(
            //        new SiteSettings("Catalog.IsCommentsRequireApproval") { Module = "Catalog", IsVisibleInCommonSettingPage = true, Value = "true", SiteId = 1L }
            //);
        }
    }
}

