using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations
{
    internal class InstanceConfiguration : EntityConfigurationBase<Instance, InstanceConfiguration>
    {
        public override void Configure(EntityTypeBuilder<Instance> builder)
        {
            base.Configure(builder);
        }
    }
}