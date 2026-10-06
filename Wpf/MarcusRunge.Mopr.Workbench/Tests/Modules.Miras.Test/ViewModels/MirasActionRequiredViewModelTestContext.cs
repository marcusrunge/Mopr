using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Modules.Miras.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;
using MarcusRunge.Mopr.Workbench.Test.Shared.Application;
using Moq;
using Prism.Navigation.Regions;
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
            AdministrativeAuthorizationService = new Mock<IAdministrativeAuthorizationService>(MockBehavior.Strict);
            Application = new Mock<IApplication>(MockBehavior.Strict);
            DialogService = new Mock<IDialogService>(MockBehavior.Strict);
            FileDialogService = new Mock<IFileDialogService>(MockBehavior.Strict);
            Flow = new Mock<IFlow>(MockBehavior.Strict);
            Miras = new Mock<IMiras>(MockBehavior.Strict);
            RecoveryService = new Mock<IRepositoryInfrastructureRecoveryService>(MockBehavior.Strict);
            RegionManager = new Mock<IRegionManager>(MockBehavior.Strict);
            LifetimeService = new TestApplicationLifetime();

            AdministrativeAuthorizationService.SetupGet(service => service.IsElevatedAdministrator).Returns(isElevatedAdministrator);
            Application.SetupGet(application => application.DialogService).Returns(DialogService.Object);
            DialogService.SetupGet(service => service.FileDialogService).Returns(fileDialogAvailable ? FileDialogService.Object : null);
            Miras.SetupGet(miras => miras.RepositoryInfrastructureRecovery).Returns(RecoveryService.Object);
            Miras.SetupGet(miras => miras.Flow).Returns(Flow.Object);

            ViewModel = new MirasActionRequiredViewModel(Application.Object, Miras.Object, AdministrativeAuthorizationService.Object, LifetimeService, RegionManager.Object);
        }

        internal Mock<IAdministrativeAuthorizationService> AdministrativeAuthorizationService { get; }

        internal Mock<IApplication> Application { get; }

        internal Mock<IDialogService> DialogService { get; }

        internal Mock<IFileDialogService> FileDialogService { get; }

        internal Mock<IFlow> Flow { get; }

        internal TestApplicationLifetime LifetimeService { get; }

        internal Mock<IMiras> Miras { get; }

        internal Mock<IRepositoryInfrastructureRecoveryService> RecoveryService { get; }

        internal Mock<IRegionManager> RegionManager { get; }

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

        internal void ConfigureCanceledFolderSelection() => FileDialogService.Setup(service => service.SelectFolder(It.IsAny<string>(), It.IsAny<string?>())).Returns((string?)null);

        internal void ConfigureFolderSelection(string selectedPath = SelectedRepositoryPath) => FileDialogService.Setup(service => service.SelectFolder(It.IsAny<string>(), It.IsAny<string?>())).Returns(selectedPath);

        internal void ConfigureRecoveryResult(RepositoryInfrastructureRecoveryResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            RecoveryService.Setup(service => service.RecoverAsync(It.IsAny<RepositoryInfrastructureRecoveryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        }

        internal void ExecuteRecover() => ViewModel.RecoverCommand.Execute();

        internal void ExecuteSelectRepository() => ViewModel.SelectRepositoryCommand.Execute();

        internal void VerifyFolderSelectionCalledOnce(string? initialDirectory) => FileDialogService.Verify(service => service.SelectFolder(It.IsAny<string>(), initialDirectory), Times.Once);

        internal void VerifyRecoveryCalledOnce(string repositoryPath) => RecoveryService.Verify(service => service.RecoverAsync(It.Is<RepositoryInfrastructureRecoveryRequest>(request => request.RepositoryPath == repositoryPath), It.IsAny<CancellationToken>()), Times.Once);

        internal void VerifyRecoveryNotCalled() => RecoveryService.Verify(service => service.RecoverAsync(It.IsAny<RepositoryInfrastructureRecoveryRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        internal void VerifyVerificationNotStarted() => Flow.Verify(flow => flow.StartAsync(It.IsAny<CancellationToken>()), Times.Never);

        internal static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(condition);

            while (!condition())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken);
            }
        }
    }
}