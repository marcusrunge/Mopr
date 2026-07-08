using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations
{
    internal class SeriesConfiguration : EntityConfigurationBase<Series, SeriesConfiguration>
    {
        public override void Configure(EntityTypeBuilder<Series> builder)
        {
            base.Configure(builder);
        }
    }
}