using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations
{
    internal abstract class AuditableEntityConfigurationBase<TEntity, TConfiguration> : EntityConfigurationBase<TEntity, TConfiguration> where TEntity : AuditableEntityBase where TConfiguration : AuditableEntityConfigurationBase<TEntity, TConfiguration>, new()
    {
        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.CreatedAtUtc)
                   .IsRequired();
            builder.Property(x => x.ModifiedAtUtc);
        }
    }
}