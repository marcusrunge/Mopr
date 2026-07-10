using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal class InstanceConfiguration : AuditableEntityConfigurationBase<Instance, InstanceConfiguration>
{
    public override void Configure(EntityTypeBuilder<Instance> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.SopInstanceUid)
               .IsRequired()
               .HasMaxLength(128);

        builder.HasIndex(x => x.SopInstanceUid)
               .IsUnique();

        builder.Property(x => x.FilePath)
               .HasMaxLength(2048);

        builder.HasMany(x => x.Measurements)
               .WithOne(x => x.Instance)
               .HasForeignKey(x => x.InstanceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.UnrealObjects)
               .WithOne(x => x.Instance)
               .HasForeignKey(x => x.InstanceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByUser)
               .WithMany(x => x.CreatedInstances)
               .HasForeignKey(x => x.CreatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModifiedByUser)
               .WithMany(x => x.ModifiedInstances)
               .HasForeignKey(x => x.ModifiedByUserId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}