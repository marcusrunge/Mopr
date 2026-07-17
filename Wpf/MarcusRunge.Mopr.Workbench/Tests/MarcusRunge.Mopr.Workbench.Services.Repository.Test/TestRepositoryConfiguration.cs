using MarcusRunge.Mopr.Workbench.Contracts.Application;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    internal sealed class TestRepositoryConfiguration : IRepositoryConfiguration
    {
        public bool AutomaticallyRepairPaths { get; } = true;

        public string DicomRepositoryPath { get; } = Path.Combine(Path.GetTempPath(), "MoprRepositoryTests");

        public bool VerifyRepositoryOnStartup { get; } = true;
    }
}