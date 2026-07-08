using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations
{
    internal class MeasurementConfiguration : EntityConfigurationBase<Measurement, MeasurementConfiguration>
    {
        public override void Configure(EntityTypeBuilder<Measurement> builder)
        {
            base.Configure(builder);
        }
    }
}