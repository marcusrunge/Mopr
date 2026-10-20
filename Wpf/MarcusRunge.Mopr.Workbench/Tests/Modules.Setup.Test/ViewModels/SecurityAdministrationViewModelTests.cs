using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.Test.ViewModels;

public sealed class SecurityAdministrationViewModelTests
{
    [Fact]
    public async Task Constructor_WhenConfigurationLoads_AppliesSecurityValues()
    {
        var context = new TestContext();
        await context.WaitUntilLoadedAsync(Xunit.TestContext.Current.CancellationToken);
        Assert.False(context.ViewModel.AllowSelfDeletion);
        Assert.True(context.ViewModel.AllowSelfModification);
        Assert.True(context.ViewModel.HideOtherUsersFromRegularUsers);
        Assert.False(context.ViewModel.IsDirty);
    }

    [Fact]
    public async Task Setting_WhenChanged_EnablesSaveAndDiscard()
    {
        var context = new TestContext();
        await context.WaitUntilLoadedAsync(Xunit.TestContext.Current.CancellationToken);
        context.ViewModel.AllowSelfDeletion = true;
        Assert.True(context.ViewModel.IsDirty);
        Assert.True(context.ViewModel.SaveCommand.CanExecute());
        Assert.True(context.ViewModel.DiscardCommand.CanExecute());
        Assert.False(context.ViewModel.ReloadCommand.CanExecute());
    }

    [Fact]
    public async Task DiscardCommand_WhenExecuted_RestoresLoadedValues()
    {
        var context = new TestContext();
        await context.WaitUntilLoadedAsync(Xunit.TestContext.Current.CancellationToken);
        context.ViewModel.AllowSelfDeletion = true;
        context.ViewModel.AllowSelfModification = false;
        context.ViewModel.DiscardCommand.Execute();
        Assert.False(context.ViewModel.AllowSelfDeletion);
        Assert.True(context.ViewModel.AllowSelfModification);
        Assert.False(context.ViewModel.IsDirty);
        Assert.Equal(Resources.SecurityAdministration_ChangesDiscarded, context.ViewModel.StatusText);
    }

    [Fact]
    public async Task SaveCommand_WhenUpdateSucceeds_SavesCurrentValuesAndClearsDirtyState()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext();
        context.ConfigurationService.Setup(service => service.UpdateSecurityAsync(It.IsAny<SecurityConfigurationUpdateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SecurityConfigurationUpdateResult.Success());
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.AllowSelfDeletion = true;
        context.ViewModel.HideOtherUsersFromRegularUsers = false;
        context.ViewModel.SaveCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.SecurityAdministration_Saved, cancellationToken);
        Assert.False(context.ViewModel.IsDirty);
        context.ConfigurationService.Verify(service => service.UpdateSecurityAsync(It.Is<SecurityConfigurationUpdateRequest>(request => request.AllowSelfDeletion && request.AllowSelfModification && !request.HideOtherUsersFromRegularUsers), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_WhenUnauthorized_ShowsLocalizedStatusAndKeepsChanges()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext();
        context.ConfigurationService.Setup(service => service.UpdateSecurityAsync(It.IsAny<SecurityConfigurationUpdateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(SecurityConfigurationUpdateResult.Unauthorized());
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.AllowSelfDeletion = true;
        context.ViewModel.SaveCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.SecurityAdministration_Unauthorized, cancellationToken);
        Assert.True(context.ViewModel.IsDirty);
    }

    [Fact]
    public async Task Constructor_WhenModificationIsNotAllowed_DisablesInteraction()
    {
        var context = new TestContext(canModify: false);
        await context.WaitUntilLoadedAsync(Xunit.TestContext.Current.CancellationToken);
        Assert.False(context.ViewModel.IsInteractionEnabled);
        Assert.False(context.ViewModel.SaveCommand.CanExecute());
        Assert.Equal(Resources.SecurityAdministration_ReadOnly, context.ViewModel.StatusText);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTime.UtcNow >= timeoutAt) throw new TimeoutException("The expected SecurityAdministrationViewModel state was not reached.");
            await Task.Delay(10, cancellationToken);
        }
    }

    private sealed class TestContext
    {
        internal TestContext(bool canModify = true)
        {
            var security = new Mock<ISecurityConfiguration>(MockBehavior.Strict);
            security.SetupGet(value => value.AllowSelfDeletion).Returns(false);
            security.SetupGet(value => value.AllowSelfModification).Returns(true);
            security.SetupGet(value => value.HideOtherUsersFromRegularUsers).Returns(true);
            var configuration = new Mock<IApplicationConfiguration>(MockBehavior.Strict);
            configuration.SetupGet(value => value.Security).Returns(security.Object);
            ConfigurationService.SetupGet(service => service.CanModify).Returns(canModify);
            ConfigurationService.Setup(service => service.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(configuration.Object);
            LifetimeService.SetupGet(service => service.ApplicationStopping).Returns(CancellationToken.None);
            ViewModel = new SecurityAdministrationViewModel(ConfigurationService.Object, LifetimeService.Object);
        }

        internal Mock<IMachineConfigurationService> ConfigurationService { get; } = new(MockBehavior.Strict);
        internal Mock<ILifetimeService> LifetimeService { get; } = new(MockBehavior.Strict);
        internal SecurityAdministrationViewModel ViewModel { get; }
        internal async Task WaitUntilLoadedAsync(CancellationToken cancellationToken) => await WaitUntilAsync(() => !ViewModel.IsBusy, cancellationToken);
    }
}
