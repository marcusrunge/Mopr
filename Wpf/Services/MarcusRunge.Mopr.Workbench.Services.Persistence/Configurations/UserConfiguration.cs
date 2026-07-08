using MarcusRunge.Base.EntityFramework;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Configurations
{
    internal class UserConfiguration : EntityConfigurationBase<User, UserConfiguration>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);
        }
    }
}