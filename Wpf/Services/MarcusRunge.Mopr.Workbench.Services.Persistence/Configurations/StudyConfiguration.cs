using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations
{
    internal class StudyConfiguration : EntityConfigurationBase<Study, StudyConfiguration>
    {
        public override void Configure(EntityTypeBuilder<Study> builder)
        {
            base.Configure(builder);
        }
    }
}