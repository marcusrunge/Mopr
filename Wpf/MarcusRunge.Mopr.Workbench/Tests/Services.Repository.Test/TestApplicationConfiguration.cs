using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    internal sealed class TestApplicationConfiguration : IApplicationConfiguration
    {
        public IDatabaseConfiguration Database { get; } = new TestDatabaseConfiguration();

        public bool IsSetupComplete => true;

        public IRepositoryConfiguration Repository { get; } = new TestRepositoryConfiguration();

        public ISecurityConfiguration Security { get; } = new TestSecurityConfiguration();

        public int SetupVersion => 1;

        private sealed class TestDatabaseConfiguration : IDatabaseConfiguration
        {
            public string ConnectionString => "RepositoryTests";
        }
    }
}