using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Application.Startup;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Core;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Startup
{
    public sealed class ApplicationStartupRouteServiceTests
    {
        [Fact]
        public async Task GetInitialNavigationTargetAsync_WithValidConfiguration_ReturnsImaging()
        {
            var configuration = CreateConfiguration(isSetupComplete: true, connectionString: "Valid");
            var configurationService = CreateConfigurationService(configuration, MachineConfigurationValidationResult.Success);
            var service = new ApplicationStartupRouteService(configurationService.Object);

            var result = await service.GetInitialNavigationTargetAsync(TestContext.Current.CancellationToken);

            Assert.Equal(NavigationNames.Imaging, result);
            configurationService.Verify(candidate => candidate.LoadAsync(TestContext.Current.CancellationToken), Times.Once);
            configurationService.Verify(candidate => candidate.ValidateForSetupCompletion(configuration), Times.Once);
        }

        [Fact]
        public async Task GetInitialNavigationTargetAsync_WithIncompleteConfiguration_ReturnsSetup()
        {
            var configuration = CreateConfiguration(isSetupComplete: false, connectionString: string.Empty);
            var validationResult = new MachineConfigurationValidationResult([Contracts.Enums.MachineConfigurationIssue.DatabaseConnectionStringMissing, Contracts.Enums.MachineConfigurationIssue.SetupNotCompleted]);
            var configurationService = CreateConfigurationService(configuration, validationResult);
            var service = new ApplicationStartupRouteService(configurationService.Object);

            var result = await service.GetInitialNavigationTargetAsync(TestContext.Current.CancellationToken);

            Assert.Equal(NavigationNames.Setup, result);
        }

        [Fact]
        public async Task GetInitialNavigationTargetAsync_WhenLoadingIsCanceled_PropagatesCancellation()
        {
            var configurationService = new Mock<IMachineConfigurationService>(MockBehavior.Strict);

            configurationService.Setup(candidate => candidate.LoadAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());

            var service = new ApplicationStartupRouteService(configurationService.Object);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await service.GetInitialNavigationTargetAsync(TestContext.Current.CancellationToken));

            configurationService.Verify(candidate => candidate.ValidateForSetupCompletion(It.IsAny<IApplicationConfiguration>()), Times.Never);
        }

        [Fact]
        public async Task GetInitialNavigationTargetAsync_WhenLoadingFails_PropagatesException()
        {
            var expectedException = new InvalidDataException("Invalid machine configuration.");
            var configurationService = new Mock<IMachineConfigurationService>(MockBehavior.Strict);

            configurationService.Setup(candidate => candidate.LoadAsync(It.IsAny<CancellationToken>())).ThrowsAsync(expectedException);

            var service = new ApplicationStartupRouteService(configurationService.Object);
            var actualException = await Assert.ThrowsAsync<InvalidDataException>(async () => await service.GetInitialNavigationTargetAsync(TestContext.Current.CancellationToken));

            Assert.Same(expectedException, actualException);
            configurationService.Verify(candidate => candidate.ValidateForSetupCompletion(It.IsAny<IApplicationConfiguration>()), Times.Never);
        }

        private static ApplicationConfiguration CreateConfiguration(bool isSetupComplete, string connectionString) => new()
        {
            DatabaseConfiguration = new DatabaseConfiguration { ConnectionString = connectionString },
            IsSetupComplete = isSetupComplete,
            SetupVersion = ApplicationConfiguration.CurrentSetupVersion
        };

        private static Mock<IMachineConfigurationService> CreateConfigurationService(IApplicationConfiguration configuration, MachineConfigurationValidationResult validationResult)
        {
            var service = new Mock<IMachineConfigurationService>(MockBehavior.Strict);

            service.Setup(candidate => candidate.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(configuration);
            service.Setup(candidate => candidate.ValidateForSetupCompletion(configuration)).Returns(validationResult);

            return service;
        }
    }
}