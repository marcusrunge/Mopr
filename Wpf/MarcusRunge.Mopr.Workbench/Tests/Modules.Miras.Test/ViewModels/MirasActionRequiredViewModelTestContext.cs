using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Modules.Miras.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;
using MarcusRunge.Mopr.Workbench.Test.Shared.Application;
using Moq;
using IDialogService = MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog.IDialogService;

namespace MarcusRunge.Mopr.Workbench.Modules.Miras.Test.ViewModels
{
    internal sealed class MirasActionRequiredViewModelTestContext : IDisposable
    {
        internal const string ExistingRepositoryPath = @"D:\MOPR\Existing";
        internal const string SelectedRepositoryPath = @"D:\MOPR\Repository";

        private bool _disposed;

        internal MirasActionRequiredViewModelTestContext(bool isElevatedAdministrator = true, bool fileDialogAvailable = true)
        {
            AdministrativeAuthorizationService.SetupGet(service => service.IsElevatedAdministrator).Returns(isElevatedAdministrator);
            Application.SetupGet(application => application.DialogService).Returns(DialogService.Object);
            Application.SetupGet(application => application.RepositoryAdministration).Returns(RepositoryLocationAdministrationService.Object);
            Application.SetupGet(application => application.RepositoryRecovery).Returns(RepositoryRecoveryActionService.Object);
            DialogService.SetupGet(service => service.FileDialogService).Returns(fileDialogAvailable ? FileDialogService.Object : null);
            Miras.SetupGet(miras => miras.RepositoryInfrastructureRecovery).Returns(RecoveryService.Object);
            Miras.SetupGet(miras => miras.Flow).Returns(Flow.Object);
            RegionNavigationService.SetupGet(service => service.Region).Returns(Region.Object);
            ViewModel = new MirasActionRequiredViewModel(Application.Object, Miras.Object, AdministrativeAuthorizationService.Object, LifetimeService, RegionManager.Object);
        }

        internal Mock<IAdministrativeAuthorizationService> AdministrativeAuthorizationService { get; } = new(MockBehavior.Strict);

        internal Mock<IApplication> Application { get; } = new(MockBehavior.Strict);

        internal Mock<IDialogService> DialogService { get; } = new(MockBehavior.Strict);

        internal Mock<IFileDialogService> FileDialogService { get; } = new(MockBehavior.Strict);

        internal Mock<IFlow> Flow { get; } = new(MockBehavior.Strict);

        internal TestApplicationLifetime LifetimeService { get; } = new();

        internal Mock<IMiras> Miras { get; } = new(MockBehavior.Strict);

        internal Mock<IRepositoryInfrastructureRecoveryService> RecoveryService { get; } = new(MockBehavior.Strict);

        internal Mock<IRegion> Region { get; } = new(MockBehavior.Strict);

        internal Mock<IRegionManager> RegionManager { get; } = new(MockBehavior.Strict);

        internal Mock<IRegionNavigationService> RegionNavigationService { get; } = new(MockBehavior.Strict);

        internal Mock<IRepositoryLocationAdministrationService> RepositoryLocationAdministrationService { get; } = new(MockBehavior.Strict);

        internal Mock<IRepositoryRecoveryActionService> RepositoryRecoveryActionService { get; } = new(MockBehavior.Strict);

        internal MirasActionRequiredViewModel ViewModel { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            LifetimeService.Dispose();
            GC.SuppressFinalize(this);
        }

        internal static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(condition);

            while (!condition())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken);
            }
        }

        internal void ConfigureCanceledFolderSelection() => FileDialogService.Setup(service => service.SelectFolder(It.IsAny<string>(), It.IsAny<string?>())).Returns((string?)null);

        internal void ConfigureFolderSelection(string selectedPath = SelectedRepositoryPath) => FileDialogService.Setup(service => service.SelectFolder(It.IsAny<string>(), It.IsAny<string?>())).Returns(selectedPath);

        internal void ConfigureRecoveryResult(RepositoryInfrastructureRecoveryResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            RecoveryService.Setup(service => service.RecoverAsync(It.IsAny<RepositoryInfrastructureRecoveryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        }

        internal void NavigateTo(bool isAdministrativeContext = false)
        {
            var navigationUri = isAdministrativeContext ? new Uri($"MirasActionRequired?{NavigationParameterNames.MirasAdministrativeContext}=true", UriKind.Relative) : new Uri("MirasActionRequired", UriKind.Relative);
            ViewModel.OnNavigatedTo(new NavigationContext(RegionNavigationService.Object, navigationUri));
        }

        internal void ExecuteRecover() => ViewModel.RecoverCommand.Execute();

        internal void ExecuteSelectRepository() => ViewModel.SelectRepositoryCommand.Execute();

        internal void VerifyFolderSelectionCalledOnce(string? initialDirectory) => FileDialogService.Verify(service => service.SelectFolder(It.IsAny<string>(), initialDirectory), Times.Once);

        internal void VerifyRecoveryCalledOnce(string repositoryPath) => RecoveryService.Verify(service => service.RecoverAsync(It.Is<RepositoryInfrastructureRecoveryRequest>(request => request.RepositoryPath == repositoryPath), It.IsAny<CancellationToken>()), Times.Once);

        internal void VerifyRecoveryNotCalled() => RecoveryService.Verify(service => service.RecoverAsync(It.IsAny<RepositoryInfrastructureRecoveryRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        internal void VerifyVerificationNotStarted() => Flow.Verify(flow => flow.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
