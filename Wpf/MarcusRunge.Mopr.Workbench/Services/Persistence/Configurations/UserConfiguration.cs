using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal class UserConfiguration : EntityConfigurationBase<User, UserConfiguration>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.LoginName)
               .HasMaxLength(256);

        // LoginName remains the single durable operating-system identity mapping.
        // Persistence-level uniqueness protects shared SQL databases against
        // concurrent provisioning attempts from different workstations.
        builder.HasIndex(x => x.LoginName)
               .IsUnique();

        builder.Property(x => x.FirstName)
               .HasMaxLength(256);

        builder.Property(x => x.MiddleName)
               .HasMaxLength(256);

        builder.Property(x => x.LastName)
               .HasMaxLength(256);

        builder.Property(x => x.ShortName)
               .HasMaxLength(64);

        builder.Property(x => x.Title)
               .HasMaxLength(128);

        builder.Property(x => x.Suffix)
               .HasMaxLength(128);

        // Existing users must remain usable after applying the migration.
        // Explicit deactivation is introduced only through controlled MOPR workflows.
        builder.Property(x => x.IsActive)
               .HasDefaultValue(true);
    }
}