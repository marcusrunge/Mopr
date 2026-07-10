using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations
{
    internal class StudyConfiguration : AuditableEntityConfigurationBase<Study, StudyConfiguration>
    {
        public override void Configure(EntityTypeBuilder<Study> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.StudyInstanceUid)
                   .IsRequired()
                   .HasMaxLength(128);

            builder.HasIndex(x => x.StudyInstanceUid)
                   .IsUnique();

            builder.Property(x => x.AccessionNumber)
                   .HasMaxLength(64);

            builder.Property(x => x.Description)
                   .HasMaxLength(1024);

            builder.HasMany(x => x.Series)
                   .WithOne(x => x.Study)
                   .HasForeignKey(x => x.StudyId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.CreatedByUser)
                   .WithMany(x => x.CreatedStudies)
                   .HasForeignKey(x => x.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ModifiedByUser)
                   .WithMany(x => x.ModifiedStudies)
                   .HasForeignKey(x => x.ModifiedByUserId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
        }
    }
}