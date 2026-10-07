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
        private const int MaximumAcademicTitleLength = 128;
        private const int MaximumLoginNameLength = 256;
        private const int MaximumNameLength = 256;
        private const int MaximumPersonnelNumberLength = 64;
        private const int MaximumSecurityIdentifierLength = 184;
        private const int MaximumShortNameLength = 64;
        private const int MaximumSuffixLength = 128;

        /// <inheritdoc/>
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            base.Configure(builder);

            builder.Property(user => user.LoginName).HasMaxLength(MaximumLoginNameLength).IsRequired();

            /*
             * LoginName remains unique because it is displayed and acts as the
             * controlled migration fallback for users without a persisted SID.
             * The SID is authoritative after it has been assigned.
             */
            builder.HasIndex(user => user.LoginName).IsUnique();

            builder.Property(user => user.SecurityIdentifier).HasMaxLength(MaximumSecurityIdentifierLength);

            /*
             * Existing users may temporarily have no SID. The filtered unique
             * index permits those migration rows while ensuring every assigned
             * SID belongs to exactly one persistent MOPR user.
             */
            builder.HasIndex(user => user.SecurityIdentifier).IsUnique().HasFilter("[SecurityIdentifier] IS NOT NULL");

            builder.Property(user => user.PersonnelNumber).HasMaxLength(MaximumPersonnelNumberLength);

            /*
             * The personnel number is an optional organization-specific business
             * identifier. Every assigned value must identify exactly one user.
             */
            builder.HasIndex(user => user.PersonnelNumber).IsUnique().HasFilter("[PersonnelNumber] IS NOT NULL");

            builder.Property(user => user.AcademicTitle).HasMaxLength(MaximumAcademicTitleLength);
            builder.Property(user => user.FirstName).HasMaxLength(MaximumNameLength);
            builder.Property(user => user.MiddleName).HasMaxLength(MaximumNameLength);
            builder.Property(user => user.LastName).HasMaxLength(MaximumNameLength);
            builder.Property(user => user.ShortName).HasMaxLength(MaximumShortNameLength);
            builder.Property(user => user.Suffix).HasMaxLength(MaximumSuffixLength);

            /*
             * Existing users remain active when this property is introduced.
             * Deactivation occurs only through an explicit controlled workflow.
             */
            builder.Property(user => user.IsActive).HasDefaultValue(true);
        }
    }
}