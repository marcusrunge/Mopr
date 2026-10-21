using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.SystemCheck;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.SystemCheck;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.Test.ViewModels;

public sealed class SystemCheckViewModelTests
{
    [Fact]
    public async Task RunCommand_WhenCheckCompletes_PublishesThreeSections()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateResult());

        context.ViewModel.RunCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsRunning && context.ViewModel.HasResult, cancellationToken);

        Assert.Equal(3, context.ViewModel.Sections.Count);
        Assert.Equal(Resources.SystemCheck_Database, context.ViewModel.Sections[0].Title);
        Assert.Equal(Resources.SystemCheck_Persistence, context.ViewModel.Sections[1].Title);
        Assert.Equal(Resources.SystemCheck_Repository, context.ViewModel.Sections[2].Title);
        Assert.Equal(Resources.SystemCheck_OverallPassed, context.ViewModel.OverallStatusText);
        Assert.Equal(Resources.SystemCheck_Completed, context.ViewModel.StatusText);
        Assert.NotNull(context.ViewModel.Duration);
    }

    [Fact]
    public async Task RunCommand_WhenActionIsRequired_ShowsActionRequiredOverallStatus()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var result = CreateResult(repositoryStatus: SystemCheckSectionStatus.ActionRequired, overallStatus: SystemCheckSectionStatus.ActionRequired);
        var context = new TestContext(result);

        context.ViewModel.RunCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsRunning && context.ViewModel.HasResult, cancellationToken);

        Assert.Equal(Resources.SystemCheck_OverallActionRequired, context.ViewModel.OverallStatusText);
        Assert.Equal(SystemCheckSectionStatus.ActionRequired, context.ViewModel.Sections[2].Status);
    }

    [Fact]
    public async Task CancelCommand_WhenRunIsActive_CancelsRun()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateResult(), waitForCancellation: true);

        context.ViewModel.RunCommand.Execute();
        await WaitUntilAsync(() => context.ViewModel.IsRunning, cancellationToken);
        context.ViewModel.CancelCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsRunning, cancellationToken);

        Assert.Equal(Resources.SystemCheck_Canceled, context.ViewModel.StatusText);
        Assert.False(context.ViewModel.HasResult);
        Assert.True(context.SystemCheckCancellationObserved);
    }

    [Fact]
    public async Task RunCommand_WhenServiceFails_ShowsFailureWithoutResult()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(new InvalidOperationException("Technical diagnostics."));

        context.ViewModel.RunCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsRunning && context.ViewModel.StatusText == Resources.SystemCheck_RunFailed, cancellationToken);

        Assert.False(context.ViewModel.HasResult);
        Assert.Empty(context.ViewModel.Sections);
    }

    [Fact]
    public async Task RunCommand_WhileRunning_IsDisabledAndCancelIsEnabled()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateResult(), waitForCancellation: true);

        context.ViewModel.RunCommand.Execute();
        await WaitUntilAsync(() => context.ViewModel.IsRunning, cancellationToken);

        Assert.False(context.ViewModel.RunCommand.CanExecute());
        Assert.True(context.ViewModel.CancelCommand.CanExecute());

        context.ViewModel.CancelCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsRunning, cancellationToken);
    }

    private static SystemCheckResult CreateResult(SystemCheckSectionStatus repositoryStatus = SystemCheckSectionStatus.Passed, SystemCheckSectionStatus overallStatus = SystemCheckSectionStatus.Passed)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        return new SystemCheckResult(overallStatus, startedAtUtc, startedAtUtc.AddSeconds(2), SystemCheckSectionResult.Create(SystemCheckSectionStatus.Passed, "Database passed"), SystemCheckSectionResult.Create(SystemCheckSectionStatus.Passed, "Persistence passed", 7), new SystemCheckSectionResult(repositoryStatus, "Repository result", ["User-oriented detail"], 11));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTime.UtcNow >= timeoutAt) throw new TimeoutException("The expected SystemCheckViewModel state was not reached.");
            await Task.Delay(10, cancellationToken);
        }
    }

    private sealed class TestContext
    {
        internal TestContext(SystemCheckResult result, bool waitForCancellation = false)
        {
            ConfigureApplication();
            if (waitForCancellation)
            {
                SystemCheck.Setup(service => service.RunAsync(It.IsAny<CancellationToken>())).Returns(async (CancellationToken token) =>
                {
                    try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
                    catch (OperationCanceledException) { SystemCheckCancellationObserved = true; throw; }
                    return result;
                });
            }
            else
            {
                SystemCheck.Setup(service => service.RunAsync(It.IsAny<CancellationToken>())).ReturnsAsync(result);
            }
            ViewModel = new SystemCheckViewModel(Application.Object, LifetimeService.Object);
        }

        internal TestContext(Exception exception)
        {
            ConfigureApplication();
            SystemCheck.Setup(service => service.RunAsync(It.IsAny<CancellationToken>())).ThrowsAsync(exception);
            ViewModel = new SystemCheckViewModel(Application.Object, LifetimeService.Object);
        }

        internal Mock<IApplication> Application { get; } = new(MockBehavior.Strict);
        internal Mock<ILifetimeService> LifetimeService { get; } = new(MockBehavior.Strict);
        internal Mock<ISystemCheckService> SystemCheck { get; } = new(MockBehavior.Strict);
        internal bool SystemCheckCancellationObserved { get; private set; }
        internal SystemCheckViewModel ViewModel { get; }

        private void ConfigureApplication()
        {
            Application.SetupGet(value => value.SystemCheck).Returns(SystemCheck.Object);
            LifetimeService.SetupGet(value => value.ApplicationStopping).Returns(CancellationToken.None);
        }
    }
}
