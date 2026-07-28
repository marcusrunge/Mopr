using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations
{
    internal class RepositoryLocationConfiguration : AuditableEntityConfigurationBase<RepositoryLocation, RepositoryLocationConfiguration>
    {
        public override void Configure(EntityTypeBuilder<RepositoryLocation> builder)
        {
            base.Configure(builder);

            builder.Property(item => item.Name)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(item => item.RootPath)
                .IsRequired()
                .HasMaxLength(2048);

            builder.Property(item => item.IsEnabled)
                .IsRequired();

            builder.Property(item => item.IsDefault)
                .IsRequired();

            /*
             * A physical root path identifies one configured repository
             * location. Duplicate paths would make import and repair ownership
             * ambiguous.
             */
            builder.HasIndex(item => item.RootPath)
                .IsUnique();

            builder.HasMany(item => item.Instances)
                .WithOne(item => item.RepositoryLocation)
                .HasForeignKey(item => item.RepositoryLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            /*
             * Repository locations may already contain medical data.
             * Deleting a location must therefore never cascade to Instances.
             */
            builder.HasOne(item => item.CreatedByUser)
                .WithMany(item => item.CreatedRepositoryLocations)
                .HasForeignKey(item => item.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(item => item.ModifiedByUser)
                .WithMany(item => item.ModifiedRepositoryLocations)
                .HasForeignKey(item => item.ModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}