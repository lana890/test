using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EskaCMS.Infrastructure.Data
{
    public static class EntityTypeBuilderSeedExtensions
    {
        public static EntityTypeBuilder HasSeedData(this EntityTypeBuilder builder, params object[] data)
        {
            if (!GlobalConfiguration.SkipModelSeedData)
            {
                builder.HasData(data);
            }

            return builder;
        }

        public static EntityTypeBuilder HasSeedData(this EntityTypeBuilder builder, IEnumerable<object> data)
        {
            if (!GlobalConfiguration.SkipModelSeedData)
            {
                builder.HasData(data);
            }

            return builder;
        }

        public static EntityTypeBuilder<TEntity> HasSeedData<TEntity>(this EntityTypeBuilder<TEntity> builder, params TEntity[] data)
            where TEntity : class
        {
            if (!GlobalConfiguration.SkipModelSeedData)
            {
                builder.HasData(data);
            }

            return builder;
        }

        public static EntityTypeBuilder<TEntity> HasSeedData<TEntity>(this EntityTypeBuilder<TEntity> builder, IEnumerable<TEntity> data)
            where TEntity : class
        {
            if (!GlobalConfiguration.SkipModelSeedData)
            {
                builder.HasData(data);
            }

            return builder;
        }

        public static OwnedNavigationBuilder HasSeedData(this OwnedNavigationBuilder builder, params object[] data)
        {
            if (!GlobalConfiguration.SkipModelSeedData)
            {
                builder.HasData(data);
            }

            return builder;
        }

        public static OwnedNavigationBuilder HasSeedData(this OwnedNavigationBuilder builder, IEnumerable<object> data)
        {
            if (!GlobalConfiguration.SkipModelSeedData)
            {
                builder.HasData(data);
            }

            return builder;
        }

        public static OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> HasSeedData<TOwnerEntity, TDependentEntity>(
            this OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> builder,
            params TDependentEntity[] data)
            where TOwnerEntity : class
            where TDependentEntity : class
        {
            if (!GlobalConfiguration.SkipModelSeedData)
            {
                builder.HasData(data);
            }

            return builder;
        }

        public static OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> HasSeedData<TOwnerEntity, TDependentEntity>(
            this OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> builder,
            IEnumerable<TDependentEntity> data)
            where TOwnerEntity : class
            where TDependentEntity : class
        {
            if (!GlobalConfiguration.SkipModelSeedData)
            {
                builder.HasData(data);
            }

            return builder;
        }
    }
}
