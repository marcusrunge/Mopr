using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    internal sealed class TestRepositoryConfiguration : IRepositoryConfiguration
    {
        /// <inheritdoc/>
        public bool AutomaticallyRepairPaths { get; } = true;
    }
}