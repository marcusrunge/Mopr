using MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal class SeriesConfiguration : AuditableEntityConfigurationBase<Series, SeriesConfiguration>
{
    public override void Configure(EntityTypeBuilder<Series> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.SeriesInstanceUid)
               .IsRequired()
               .HasMaxLength(128);

        builder.HasIndex(x => x.SeriesInstanceUid)
               .IsUnique();

        builder.Property(x => x.Modality)
               .HasMaxLength(16);

        builder.Property(x => x.Description)
               .HasMaxLength(1024);

        builder.HasMany(x => x.Instances)
               .WithOne(x => x.Series)
               .HasForeignKey(x => x.SeriesId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByUser)
               .WithMany(x => x.CreatedSeries)
               .HasForeignKey(x => x.CreatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModifiedByUser)
               .WithMany(x => x.ModifiedSeries)
               .HasForeignKey(x => x.ModifiedByUserId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}