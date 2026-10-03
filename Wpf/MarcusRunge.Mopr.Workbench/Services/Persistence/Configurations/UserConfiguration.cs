using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations
{
    /// <summary>
    /// Configures the persistent MOPR user entity.
    /// </summary>
    internal sealed class UserConfiguration : EntityConfigurationBase<User, UserConfiguration>
    {
        /// <inheritdoc/>
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);

            builder.Property(user => user.LoginName)
                .HasMaxLength(256)
                .IsRequired();

            // LoginName remains the authoritative identity mapping until existing
            // users have been migrated to persistent Windows security identifiers.
            builder.HasIndex(user => user.LoginName)
                .IsUnique();

            builder.Property(user => user.SecurityIdentifier)
                .HasMaxLength(184);

            // SQL Server unique indexes allow only one NULL value unless the index
            // is filtered. Multiple users may remain without a SID during migration.
            builder.HasIndex(user => user.SecurityIdentifier)
                .IsUnique()
                .HasFilter("[SecurityIdentifier] IS NOT NULL");

            builder.Property(user => user.PersonnelNumber)
                .HasMaxLength(64);

            // The personnel number is an optional organization-specific business
            // identifier, but every assigned value must identify exactly one user.
            builder.HasIndex(user => user.PersonnelNumber)
                .IsUnique()
                .HasFilter("[PersonnelNumber] IS NOT NULL");

            builder.Property(user => user.AcademicTitle)
                .HasMaxLength(128);

            builder.Property(user => user.FirstName)
                .HasMaxLength(256);

            builder.Property(user => user.MiddleName)
                .HasMaxLength(256);

            builder.Property(user => user.LastName)
                .HasMaxLength(256);

            builder.Property(user => user.ShortName)
                .HasMaxLength(64);

            builder.Property(user => user.Suffix)
                .HasMaxLength(128);

            // Existing users must remain usable after applying the migration.
            // Explicit deactivation is introduced only through controlled workflows.
            builder.Property(user => user.IsActive)
                .HasDefaultValue(true);
        }
    }
}