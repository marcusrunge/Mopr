using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.Test.ViewModels;

public sealed class DatabaseAdministrationViewModelTests
{
    private const string OriginalConnectionString = "Server=Original;Database=MoprDb;";
    private const string UpdatedConnectionString = "Server=Updated;Database=MoprDb;";

    [Fact]
    public async Task Constructor_WhenConfigurationLoads_AppliesConnectionString()
    {
        var context = new TestContext();

        await context.WaitUntilLoadedAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(OriginalConnectionString, context.ViewModel.ConnectionString);
        Assert.False(context.ViewModel.HasChanges);
        Assert.False(context.ViewModel.IsConnectionTestSuccessful);
        Assert.False(context.ViewModel.IsRestartRequired);
        Assert.Equal(string.Empty, context.ViewModel.StatusText);
    }

    [Fact]
    public async Task ConnectionString_WhenChanged_EnablesTestAndResetButNotSave()
    {
        var context = new TestContext();
        await context.WaitUntilLoadedAsync(Xunit.TestContext.Current.CancellationToken);

        context.ViewModel.ConnectionString = UpdatedConnectionString;

        Assert.True(context.ViewModel.HasChanges);
        Assert.True(context.ViewModel.TestConnectionCommand.CanExecute());
        Assert.True(context.ViewModel.ResetCommand.CanExecute());
        Assert.False(context.ViewModel.SaveCommand.CanExecute());
    }

    [Fact]
    public async Task TestConnectionCommand_WhenTestSucceeds_EnablesSaveForTestedValue()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.ConnectionString = $"  {UpdatedConnectionString}  ";
        context.ConfigurationService.Setup(service => service.TestDatabaseConnectionAsync(It.Is<IDatabaseConfiguration>(configuration => configuration.ConnectionString == UpdatedConnectionString), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        context.ViewModel.TestConnectionCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.IsConnectionTestSuccessful, cancellationToken);

        Assert.Equal(Resources.DatabaseAdministration_TestSucceeded, context.ViewModel.StatusText);
        Assert.True(context.ViewModel.SaveCommand.CanExecute());
    }

    [Fact]
    public async Task ConnectionString_WhenChangedAfterSuccessfulTest_InvalidatesTestAndDisablesSave()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.ConnectionString = UpdatedConnectionString;
        context.ConfigurationService.Setup(service => service.TestDatabaseConnectionAsync(It.IsAny<IDatabaseConfiguration>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        context.ViewModel.TestConnectionCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.IsConnectionTestSuccessful, cancellationToken);

        context.ViewModel.ConnectionString = "Server=Another;Database=MoprDb;";

        Assert.False(context.ViewModel.IsConnectionTestSuccessful);
        Assert.False(context.ViewModel.SaveCommand.CanExecute());
    }

    [Fact]
    public async Task ResetCommand_WhenExecuted_RestoresLoadedValue()
    {
        var context = new TestContext();
        await context.WaitUntilLoadedAsync(Xunit.TestContext.Current.CancellationToken);
        context.ViewModel.ConnectionString = UpdatedConnectionString;

        context.ViewModel.ResetCommand.Execute();

        Assert.Equal(OriginalConnectionString, context.ViewModel.ConnectionString);
        Assert.False(context.ViewModel.HasChanges);
        Assert.False(context.ViewModel.IsConnectionTestSuccessful);
        Assert.False(context.ViewModel.IsRestartRequired);
        Assert.Equal(Resources.DatabaseAdministration_ChangesDiscarded, context.ViewModel.StatusText);
    }

    [Fact]
    public async Task SaveCommand_WhenUpdateSucceeds_NormalizesValueAndRequiresRestart()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.ConnectionString = $"  {UpdatedConnectionString}  ";
        context.ConfigurationService.Setup(service => service.TestDatabaseConnectionAsync(It.IsAny<IDatabaseConfiguration>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        context.ConfigurationService.Setup(service => service.UpdateDatabaseAsync(It.IsAny<DatabaseConfigurationUpdateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(DatabaseConfigurationUpdateResult.Success());
        context.ViewModel.TestConnectionCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.IsConnectionTestSuccessful, cancellationToken);

        context.ViewModel.SaveCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.IsRestartRequired, cancellationToken);

        Assert.Equal(UpdatedConnectionString, context.ViewModel.ConnectionString);
        Assert.False(context.ViewModel.HasChanges);
        Assert.False(context.ViewModel.IsConnectionTestSuccessful);
        Assert.Equal(Resources.DatabaseAdministration_Saved, context.ViewModel.StatusText);
        context.ConfigurationService.Verify(service => service.UpdateDatabaseAsync(It.Is<DatabaseConfigurationUpdateRequest>(request => request.ConnectionString == UpdatedConnectionString), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_WhenConfigurationIsUnavailable_KeepsChangesAndShowsStatus()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.ConnectionString = UpdatedConnectionString;
        context.ConfigurationService.Setup(service => service.TestDatabaseConnectionAsync(It.IsAny<IDatabaseConfiguration>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        context.ConfigurationService.Setup(service => service.UpdateDatabaseAsync(It.IsAny<DatabaseConfigurationUpdateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(DatabaseConfigurationUpdateResult.ConfigurationUnavailable());
        context.ViewModel.TestConnectionCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.IsConnectionTestSuccessful, cancellationToken);

        context.ViewModel.SaveCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.DatabaseAdministration_ConfigurationUnavailable, cancellationToken);

        Assert.True(context.ViewModel.HasChanges);
        Assert.False(context.ViewModel.IsRestartRequired);
    }

    [Fact]
    public async Task CancelCommand_WhenConnectionTestIsRunning_CancelsOperation()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.ConnectionString = UpdatedConnectionString;
        context.ConfigurationService.Setup(service => service.TestDatabaseConnectionAsync(It.IsAny<IDatabaseConfiguration>(), It.IsAny<CancellationToken>())).Returns(async (IDatabaseConfiguration _, CancellationToken token) =>
        {
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) { context.CancellationObserved = true; throw; }
            return false;
        });

        context.ViewModel.TestConnectionCommand.Execute();
        await WaitUntilAsync(() => context.ViewModel.IsBusy && context.ViewModel.CancelCommand.CanExecute(), cancellationToken);
        context.ViewModel.CancelCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy, cancellationToken);

        Assert.True(context.CancellationObserved);
        Assert.Equal(Resources.DatabaseAdministration_Canceled, context.ViewModel.StatusText);
        Assert.False(context.ViewModel.IsConnectionTestSuccessful);
    }

    [Fact]
    public async Task Constructor_WhenLoadFails_ShowsLoadFailureAndKeepsCommandsDisabled()
    {
        var context = new TestContext(loadException: new InvalidOperationException("Load failed."));

        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.DatabaseAdministration_LoadFailed, Xunit.TestContext.Current.CancellationToken);

        Assert.False(context.ViewModel.TestConnectionCommand.CanExecute());
        Assert.False(context.ViewModel.SaveCommand.CanExecute());
        Assert.False(context.ViewModel.ResetCommand.CanExecute());
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTime.UtcNow >= timeoutAt) throw new TimeoutException("The expected DatabaseAdministrationViewModel state was not reached.");
            await Task.Delay(10, cancellationToken);
        }
    }

    private sealed class TestContext
    {
        internal TestContext(Exception? loadException = null)
        {
            var database = new Mock<IDatabaseConfiguration>(MockBehavior.Strict);
            database.SetupGet(configuration => configuration.ConnectionString).Returns(OriginalConnectionString);
            var configuration = new Mock<IApplicationConfiguration>(MockBehavior.Strict);
            configuration.SetupGet(value => value.Database).Returns(database.Object);
            ConfigurationService.SetupGet(service => service.CanModify).Returns(true);
            if (loadException is null) ConfigurationService.Setup(service => service.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(configuration.Object);
            else ConfigurationService.Setup(service => service.LoadAsync(It.IsAny<CancellationToken>())).ThrowsAsync(loadException);
            LifetimeService.SetupGet(service => service.ApplicationStopping).Returns(CancellationToken.None);
            ViewModel = new DatabaseAdministrationViewModel(ConfigurationService.Object, LifetimeService.Object);
        }

        internal bool CancellationObserved { get; set; }
        internal Mock<IMachineConfigurationService> ConfigurationService { get; } = new(MockBehavior.Strict);
        internal Mock<ILifetimeService> LifetimeService { get; } = new(MockBehavior.Strict);
        internal DatabaseAdministrationViewModel ViewModel { get; }
        internal async Task WaitUntilLoadedAsync(CancellationToken cancellationToken) => await WaitUntilAsync(() => !ViewModel.IsBusy && ViewModel.ConnectionString == OriginalConnectionString, cancellationToken);
    }
}
