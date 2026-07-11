using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal class MeasurementConfiguration : AuditableEntityConfigurationBase<Measurement, MeasurementConfiguration>
{
    public override void Configure(EntityTypeBuilder<Measurement> builder)
    {
        base.Configure(builder);


        builder.Property(x => x.MeasurementType)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(x => x.DataJson)
               .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Title)
               .HasMaxLength(256);

        builder.Property(x => x.Description)
               .HasMaxLength(4000);        

        builder.HasOne(x => x.Instance)
               .WithMany(x => x.Measurements)
               .HasForeignKey(x => x.InstanceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByUser)
               .WithMany(x => x.CreatedMeasurements)
               .HasForeignKey(x => x.CreatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ModifiedByUser)
               .WithMany(x => x.ModifiedMeasurements)
               .HasForeignKey(x => x.ModifiedByUserId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}