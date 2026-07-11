using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal class UnrealObjectConfiguration : AuditableEntityConfigurationBase<UnrealObject, UnrealObjectConfiguration>
{
    public override void Configure(EntityTypeBuilder<UnrealObject> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name)
               .HasMaxLength(256);

        builder.Property(x => x.ClassName)
               .HasMaxLength(256);

        builder.Property(x => x.AssetPath)
               .HasMaxLength(2048);

        builder.Property(x => x.MetadataJson)
               .HasColumnType("nvarchar(max)");

        builder.HasOne(x => x.Instance)
               .WithMany(x => x.UnrealObjects)
               .HasForeignKey(x => x.InstanceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByUser)
               .WithMany(x => x.CreatedUnrealObjects)
               .HasForeignKey(x => x.CreatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModifiedByUser)
               .WithMany(x => x.ModifiedUnrealObjects)
               .HasForeignKey(x => x.ModifiedByUserId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}