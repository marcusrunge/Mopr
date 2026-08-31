using MarcusRunge.Mopr.Workbench.Application.Configuration;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Configuration
{
    public sealed class ApplicationConfigurationTests
    {
        [Fact]
        public void NewConfiguration_HasSafeIncompleteDefaults()
        {
            var configuration = new ApplicationConfiguration();

            Assert.Equal(ApplicationConfiguration.CurrentSetupVersion, configuration.SetupVersion);
            Assert.False(configuration.IsSetupComplete);
            Assert.Empty(configuration.Database.ConnectionString);
            Assert.True(configuration.Repository.AutomaticallyRepairPaths);
            Assert.False(configuration.Security.AllowSelfDeletion);
            Assert.True(configuration.Security.AllowSelfModification);
            Assert.True(configuration.Security.HideOtherUsersFromRegularUsers);
        }

        [Fact]
        public void ContractProperties_ReturnSerializableConfigurationInstances()
        {
            var configuration = new ApplicationConfiguration
            {
                DatabaseConfiguration = new DatabaseConfiguration
                {
                    ConnectionString = "Server=(localdb)\\MSSQLLocalDB;"
                },
                RepositoryConfiguration = new RepositoryConfiguration
                {
                    AutomaticallyRepairPaths = false
                },
                SecurityConfiguration = new SecurityConfiguration
                {
                    AllowSelfDeletion = true,
                    AllowSelfModification = false,
                    HideOtherUsersFromRegularUsers = false
                }
            };

            Assert.Same(configuration.DatabaseConfiguration, configuration.Database);
            Assert.Same(configuration.RepositoryConfiguration, configuration.Repository);
            Assert.Same(configuration.SecurityConfiguration, configuration.Security);
            Assert.Equal("Server=(localdb)\\MSSQLLocalDB;", configuration.Database.ConnectionString);
            Assert.False(configuration.Repository.AutomaticallyRepairPaths);
            Assert.True(configuration.Security.AllowSelfDeletion);
            Assert.False(configuration.Security.AllowSelfModification);
            Assert.False(configuration.Security.HideOtherUsersFromRegularUsers);
        }
    }
}