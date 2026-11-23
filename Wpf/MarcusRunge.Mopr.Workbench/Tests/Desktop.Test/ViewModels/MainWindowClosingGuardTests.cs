using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.ViewModels;
using Moq;
using Prism.Dialogs;
using Prism.Navigation.Regions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.ViewModels
{
    public sealed class MainWindowClosingGuardTests
    {
        [Fact]
        public async Task CanCloseWindowAsync_Should_Close_Clean_Session_Without_Dialog()
        {
            var context = new TestContext(hasUnsavedChanges: false);

            bool result = await context.ViewModel.CanCloseWindowAsync(Xunit.TestContext.Current.CancellationToken);

            Assert.True(result);
            context.DialogService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CanCloseWindowAsync_Should_Save_And_Close_When_No_Changes_Remain()
        {
            var context = new TestContext(hasUnsavedChanges: true);
            context.ImagingSessionService.SetupSequence(service => service.HasUnsavedChanges).Returns(true).Returns(false);
            context.ImagingSessionApplicationService.Setup(service => service.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ImagingSessionSaveResult(1, 1));
            context.ConfigureDecision(UnsavedChangesDecision.Save);

            bool result = await context.ViewModel.CanCloseWindowAsync(Xunit.TestContext.Current.CancellationToken);

            Assert.True(result);
            context.ImagingSessionApplicationService.Verify(service => service.SaveAsync(Xunit.TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task CanCloseWindowAsync_Should_Keep_Window_Open_When_Newer_Changes_Remain()
        {
            var context = new TestContext(hasUnsavedChanges: true);
            context.ImagingSessionService.SetupGet(service => service.HasUnsavedChanges).Returns(true);
            context.ImagingSessionApplicationService.Setup(service => service.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ImagingSessionSaveResult(1, 0));
            context.ConfigureDecision(UnsavedChangesDecision.Save);

            bool result = await context.ViewModel.CanCloseWindowAsync(Xunit.TestContext.Current.CancellationToken);

            Assert.False(result);
        }

        [Fact]
        public async Task CanCloseWindowAsync_Should_Discard_And_Close()
        {
            var context = new TestContext(hasUnsavedChanges: true);
            context.ConfigureDecision(UnsavedChangesDecision.Discard);

            bool result = await context.ViewModel.CanCloseWindowAsync(Xunit.TestContext.Current.CancellationToken);

            Assert.True(result);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Once);
        }

        [Fact]
        public async Task CanCloseWindowAsync_Should_Keep_Window_Open_When_User_Cancels()
        {
            var context = new TestContext(hasUnsavedChanges: true);
            context.ConfigureDecision(UnsavedChangesDecision.Cancel);

            bool result = await context.ViewModel.CanCloseWindowAsync(Xunit.TestContext.Current.CancellationToken);

            Assert.False(result);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);
        }

        [Fact]
        public async Task CanCloseWindowAsync_Should_Report_Save_Error_And_Keep_Window_Open()
        {
            var context = new TestContext(hasUnsavedChanges: true);
            var exception = new InvalidOperationException("Save failed.");
            context.ImagingSessionApplicationService.Setup(service => service.SaveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(exception);
            context.Application.Setup(service => service.ReportException(exception));
            context.ConfigureDecision(UnsavedChangesDecision.Save);

            bool result = await context.ViewModel.CanCloseWindowAsync(Xunit.TestContext.Current.CancellationToken);

            Assert.False(result);
            context.Application.Verify(service => service.ReportException(exception), Times.Once);
        }

        [Fact]
        public async Task CanCloseWindowAsync_Should_Keep_Window_Open_When_Save_Is_Canceled()
        {
            var context = new TestContext(hasUnsavedChanges: true);
            using var cancellationTokenSource = new CancellationTokenSource();

            context.ImagingSessionApplicationService.Setup(service => service.SaveAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(async cancellationToken =>
            {
                cancellationTokenSource.Cancel();
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return new ImagingSessionSaveResult(0, 0);
            });

            context.ConfigureDecision(UnsavedChangesDecision.Save);

            bool result = await context.ViewModel.CanCloseWindowAsync(cancellationTokenSource.Token);

            Assert.False(result);

            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);

            context.Application.Verify(service => service.ReportException(It.IsAny<Exception>()), Times.Never);
        }

        private sealed class TestContext
        {
            internal TestContext(bool hasUnsavedChanges)
            {
                Application.SetupGet(service => service.ImagingSession).Returns(ImagingSessionApplicationService.Object);
                Core.SetupGet(service => service.ImagingService).Returns(ImagingService.Object);
                ImagingService.SetupGet(service => service.ImagingSessionService).Returns(ImagingSessionService.Object);
                ImagingSessionService.SetupGet(service => service.HasUnsavedChanges).Returns(hasUnsavedChanges);
                LifetimeService.SetupGet(service => service.ApplicationStopping).Returns(CancellationToken.None);
                ViewModel = new MainWindowViewModel(Application.Object, Core.Object, LifetimeService.Object, AdministrativeAuthorizationService.Object, RegionManager.Object, DialogService.Object);
            }

            internal Mock<IAdministrativeAuthorizationService> AdministrativeAuthorizationService { get; } = new();
            internal Mock<IApplication> Application { get; } = new();
            internal Mock<ICore> Core { get; } = new();
            internal Mock<IDialogService> DialogService { get; } = new();
            internal Mock<IImagingService> ImagingService { get; } = new();
            internal Mock<IImagingSessionApplicationService> ImagingSessionApplicationService { get; } = new();
            internal Mock<IImagingSessionService> ImagingSessionService { get; } = new();
            internal Mock<ILifetimeService> LifetimeService { get; } = new();
            internal Mock<IRegionManager> RegionManager { get; } = new();
            internal MainWindowViewModel ViewModel { get; }

            internal void ConfigureDecision(UnsavedChangesDecision decision)
            {
                DialogService.Setup(service => service.ShowDialog("UnsavedChangesDialogView", It.IsAny<IDialogParameters>(), It.IsAny<DialogCallback>())).Callback<string, IDialogParameters, DialogCallback>((_, _, callback) =>
                {
                    var parameters = new DialogParameters { { UnsavedChangesDialogViewModel.DecisionParameterName, decision } };
                    var result = new Mock<IDialogResult>();
                    result.SetupGet(value => value.Parameters).Returns(parameters);
                    result.SetupGet(value => value.Result).Returns(decision == UnsavedChangesDecision.Cancel ? ButtonResult.Cancel : ButtonResult.OK);
                    callback.Invoke(result.Object).GetAwaiter().GetResult();
                });
            }
        }
    }
}