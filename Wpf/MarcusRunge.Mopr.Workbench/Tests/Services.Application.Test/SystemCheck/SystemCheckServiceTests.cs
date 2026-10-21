using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.SystemCheck;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.SystemCheck;
using MarcusRunge.Mopr.Workbench.Services.Application.Properties;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.SystemCheck;

public sealed class SystemCheckServiceTests
{
    [Fact]
    public async Task RunAsync_WhenAllChecksPass_ReturnsPassedResult()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext();

        var result = await context.Service.RunAsync(cancellationToken);

        Assert.True(result.IsSuccessful);
        Assert.Equal(SystemCheckSectionStatus.Passed, result.Status);
        Assert.Equal(SystemCheckSectionStatus.Passed, result.Database.Status);
        Assert.Equal(SystemCheckSectionStatus.Passed, result.Persistence.Status);
        Assert.Equal(SystemCheckSectionStatus.Passed, result.Repository.Status);
        Assert.Equal(7, result.Persistence.ScannedItems);
        Assert.Equal(11, result.Repository.ScannedItems);
        Assert.True(result.CompletedAtUtc >= result.StartedAtUtc);
        context.VerifyCompleteRun(cancellationToken);
    }

    [Fact]
    public async Task RunAsync_WhenDatabaseConnectionFails_ReturnsActionRequired()
    {
        var context = new TestContext();
        context.MachineConfigurationService.Setup(service => service.TestDatabaseConnectionAsync(context.DatabaseConfiguration.Object, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await context.Service.RunAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(SystemCheckSectionStatus.ActionRequired, result.Status);
        Assert.Equal(SystemCheckSectionStatus.ActionRequired, result.Database.Status);
        Assert.Equal(Resources.SystemCheck_DatabaseActionRequired, result.Database.Summary);
    }

    [Fact]
    public async Task RunAsync_WhenDatabaseCheckThrows_ReturnsUnavailableAndContinues()
    {
        var context = new TestContext();
        context.MachineConfigurationService.Setup(service => service.TestDatabaseConnectionAsync(context.DatabaseConfiguration.Object, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database diagnostics."));

        var result = await context.Service.RunAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(SystemCheckSectionStatus.Unavailable, result.Status);
        Assert.Equal(SystemCheckSectionStatus.Unavailable, result.Database.Status);
        Assert.Equal(SystemCheckSectionStatus.Passed, result.Persistence.Status);
        Assert.Equal(SystemCheckSectionStatus.Passed, result.Repository.Status);
    }

    [Fact]
    public async Task RunAsync_WhenPersistenceIntegrityHasIssues_ReturnsActionRequiredWithoutTechnicalDetails()
    {
        var context = new TestContext();
        var persistenceResult = new PersistenceIntegrityResult { ScannedEntities = 5 };
        persistenceResult.Issues.Add(new PersistenceIntegrityIssue { TechnicalDetails = "Internal relationship detail." });
        context.Integrity.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(persistenceResult);

        var result = await context.Service.RunAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(SystemCheckSectionStatus.ActionRequired, result.Persistence.Status);
        Assert.Equal(5, result.Persistence.ScannedItems);
        Assert.DoesNotContain(result.Persistence.Details, detail => detail.Contains("Internal relationship detail", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_WhenPersistenceIntegrityHasTechnicalErrors_ReturnsFailedWithoutExposingErrors()
    {
        var context = new TestContext();
        var persistenceResult = new PersistenceIntegrityResult { ScannedEntities = 3 };
        persistenceResult.Errors.Add("Sensitive database diagnostic.");
        context.Integrity.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(persistenceResult);

        var result = await context.Service.RunAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(SystemCheckSectionStatus.Failed, result.Status);
        Assert.Equal(SystemCheckSectionStatus.Failed, result.Persistence.Status);
        Assert.DoesNotContain(result.Persistence.Details, detail => detail.Contains("Sensitive database diagnostic", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(MirasAlertLevel.Advisory, SystemCheckSectionStatus.Notice)]
    [InlineData(MirasAlertLevel.Caution, SystemCheckSectionStatus.Warning)]
    [InlineData(MirasAlertLevel.Warning, SystemCheckSectionStatus.ActionRequired)]
    public async Task RunAsync_WhenMirasReportsAlertLevel_MapsExpectedStatus(MirasAlertLevel alertLevel, SystemCheckSectionStatus expectedStatus)
    {
        var context = new TestContext();
        var mirasResult = CreateMirasResult(MirasOperationStatus.CompletedWithIssues, alertLevel);
        var issue = new MirasIssue { AlertLevel = alertLevel };

        if (alertLevel == MirasAlertLevel.Warning)
        {
            issue.IssueState = MirasIssueState.ActionRequired;
        }

        mirasResult.Issues.Add(issue);
        context.Operations.Setup(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mirasResult);

        var result = await context.Service.RunAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, result.Repository.Status);
        Assert.Equal(expectedStatus, result.Status);
    }

    [Theory]
    [InlineData(MirasOperationStatus.Blocked, SystemCheckSectionStatus.ActionRequired)]
    [InlineData(MirasOperationStatus.Incomplete, SystemCheckSectionStatus.Unavailable)]
    [InlineData(MirasOperationStatus.Failed, SystemCheckSectionStatus.Failed)]
    public async Task RunAsync_WhenMirasReportsOperationStatus_MapsExpectedStatus(MirasOperationStatus operationStatus, SystemCheckSectionStatus expectedStatus)
    {
        var context = new TestContext();
        context.Operations.Setup(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateMirasResult(operationStatus));

        var result = await context.Service.RunAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, result.Repository.Status);
        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public async Task RunAsync_WhenMirasProvidesUserMessage_UsesUserTextWithoutTechnicalDetails()
    {
        var context = new TestContext();
        var mirasResult = CreateMirasResult(MirasOperationStatus.CompletedWithIssues, MirasAlertLevel.Advisory);
        mirasResult.Messages.Add(new MirasUserMessage { Title = "Repository notice", Description = "One item was reviewed.", StatusText = "Review complete", RecommendedActionText = "No action required", TechnicalDetails = "Internal file-system diagnostic." });
        context.Operations.Setup(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mirasResult);

        var result = await context.Service.RunAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Contains("Repository notice", result.Repository.Details);
        Assert.Contains("One item was reviewed.", result.Repository.Details);
        Assert.Contains("Review complete", result.Repository.Details);
        Assert.Contains("No action required", result.Repository.Details);
        Assert.DoesNotContain(result.Repository.Details, detail => detail.Contains("Internal file-system diagnostic", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_WhenCanceled_PropagatesCancellationAndStopsFollowingChecks()
    {
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(Xunit.TestContext.Current.CancellationToken);
        var context = new TestContext();
        context.MachineConfigurationService.Setup(service => service.LoadAsync(cancellationSource.Token)).Returns(() =>
        {
            cancellationSource.Cancel();
            return Task.FromCanceled<IApplicationConfiguration>(cancellationSource.Token);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.RunAsync(cancellationSource.Token));

        context.Integrity.Verify(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        context.Operations.Verify(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static MirasOperationResult CreateMirasResult(MirasOperationStatus status = MirasOperationStatus.Completed, MirasAlertLevel alertLevel = MirasAlertLevel.Normal)
    {
        var result = new MirasOperationResult { Status = status, ScannedItems = 11 };

        if (alertLevel != MirasAlertLevel.Normal)
        {
            result.Issues.Add(new MirasIssue { AlertLevel = alertLevel });
        }

        return result;
    }

    private sealed class TestContext
    {
        internal TestContext()
        {
            Configuration.SetupGet(value => value.Database).Returns(DatabaseConfiguration.Object);
            MachineConfigurationService.Setup(service => service.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Configuration.Object);
            MachineConfigurationService.Setup(service => service.TestDatabaseConnectionAsync(DatabaseConfiguration.Object, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            Persistence.SetupGet(value => value.Initialization).Returns(Task.CompletedTask);
            Persistence.SetupGet(value => value.Integrity).Returns(Integrity.Object);
            Integrity.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PersistenceIntegrityResult { ScannedEntities = 7 });
            Miras.SetupGet(value => value.Operations).Returns(Operations.Object);
            Operations.Setup(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateMirasResult());
            ApplicationBase.SetupGet(value => value.MachineConfigurationService).Returns(MachineConfigurationService.Object);
            ApplicationBase.SetupGet(value => value.Persistence).Returns(Persistence.Object);
            ApplicationBase.SetupGet(value => value.Miras).Returns(Miras.Object);
            Service = SystemCheckService.Create(ApplicationBase.Object, CreationLifetime.Scoped);
        }

        internal Mock<IApplicationBase> ApplicationBase { get; } = new(MockBehavior.Strict);
        internal Mock<IApplicationConfiguration> Configuration { get; } = new(MockBehavior.Strict);
        internal Mock<IDatabaseConfiguration> DatabaseConfiguration { get; } = new(MockBehavior.Strict);
        internal Mock<IPersistenceIntegrityService> Integrity { get; } = new(MockBehavior.Strict);
        internal Mock<IMachineConfigurationService> MachineConfigurationService { get; } = new(MockBehavior.Strict);
        internal Mock<IMiras> Miras { get; } = new(MockBehavior.Strict);
        internal Mock<IOperations> Operations { get; } = new(MockBehavior.Strict);
        internal Mock<IPersistence> Persistence { get; } = new(MockBehavior.Strict);
        internal ISystemCheckService Service { get; }

        internal void VerifyCompleteRun(CancellationToken cancellationToken)
        {
            MachineConfigurationService.Verify(service => service.LoadAsync(cancellationToken), Times.Once);
            MachineConfigurationService.Verify(service => service.TestDatabaseConnectionAsync(DatabaseConfiguration.Object, cancellationToken), Times.Once);
            Integrity.Verify(service => service.VerifyAsync(It.Is<PersistenceIntegrityRequest>(request => request.VerifyAuditReferences && request.VerifyRelationships && request.VerifyRequiredValues && request.VerifyUniqueValues), cancellationToken), Times.Once);
            Operations.Verify(service => service.CheckRepositoryAsync(cancellationToken), Times.Once);
        }
    }
}
