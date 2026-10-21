using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.SystemCheck;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.SystemCheck;
using MarcusRunge.Mopr.Workbench.Services.Application.Properties;
using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.SystemCheck;

/// <summary>
/// Coordinates a read-only check of configured workstation services without duplicating MIRAS or Persistence integrity rules.
/// </summary>
internal sealed class SystemCheckService : CreateableBindableBase<ISystemCheckService, SystemCheckService, IApplicationBase>, ISystemCheckService
{
    private IApplicationBase? _base;

    private IApplicationBase Base => _base ?? throw new InvalidOperationException("The system-check service has not been initialized.");

    /// <inheritdoc/>
    public async Task<SystemCheckResult> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startedAtUtc = DateTimeOffset.UtcNow;

        var database = await CheckDatabaseAsync(cancellationToken).ConfigureAwait(false);
        var persistence = await CheckPersistenceAsync(cancellationToken).ConfigureAwait(false);
        var repository = await CheckRepositoryAsync(cancellationToken).ConfigureAwait(false);
        var completedAtUtc = DateTimeOffset.UtcNow;
        var status = AggregateStatus(database.Status, persistence.Status, repository.Status);

        return new SystemCheckResult(status, startedAtUtc, completedAtUtc, database, persistence, repository);
    }

    /// <inheritdoc/>
    protected override void OnCreate(IApplicationBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

    /// <inheritdoc/>
    protected override Task OnCreateAsync(IApplicationBase @base, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OnCreate(@base);
        return Task.CompletedTask;
    }

    private static SystemCheckSectionStatus AggregateStatus(params SystemCheckSectionStatus[] statuses)
    {
        if (statuses.Contains(SystemCheckSectionStatus.Failed)) return SystemCheckSectionStatus.Failed;
        if (statuses.Contains(SystemCheckSectionStatus.Canceled)) return SystemCheckSectionStatus.Canceled;
        if (statuses.Contains(SystemCheckSectionStatus.Unavailable)) return SystemCheckSectionStatus.Unavailable;
        if (statuses.Contains(SystemCheckSectionStatus.ActionRequired)) return SystemCheckSectionStatus.ActionRequired;
        if (statuses.Contains(SystemCheckSectionStatus.Warning)) return SystemCheckSectionStatus.Warning;
        if (statuses.Contains(SystemCheckSectionStatus.Notice)) return SystemCheckSectionStatus.Notice;
        return SystemCheckSectionStatus.Passed;
    }

    private async Task<SystemCheckSectionResult> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await Base.MachineConfigurationService.LoadAsync(cancellationToken).ConfigureAwait(false);
            var isSuccessful = await Base.MachineConfigurationService.TestDatabaseConnectionAsync(configuration.Database, cancellationToken).ConfigureAwait(false);
            return isSuccessful
                ? SystemCheckSectionResult.Create(SystemCheckSectionStatus.Passed, Resources.SystemCheck_DatabasePassed)
                : SystemCheckSectionResult.Create(SystemCheckSectionStatus.ActionRequired, Resources.SystemCheck_DatabaseActionRequired);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return SystemCheckSectionResult.Create(SystemCheckSectionStatus.Unavailable, Resources.SystemCheck_DatabaseUnavailable);
        }
    }

    private async Task<SystemCheckSectionResult> CheckPersistenceAsync(CancellationToken cancellationToken)
    {
        var persistence = Base.Persistence;
        var integrity = persistence?.Integrity;
        if (persistence is null || integrity is null) return SystemCheckSectionResult.Create(SystemCheckSectionStatus.Unavailable, Resources.SystemCheck_PersistenceUnavailable);

        try
        {
            await persistence.Initialization.WaitAsync(cancellationToken).ConfigureAwait(false);
            var result = await integrity.VerifyAsync(new PersistenceIntegrityRequest { VerifyAuditReferences = true, VerifyRelationships = true, VerifyRequiredValues = true, VerifyUniqueValues = true }, cancellationToken).ConfigureAwait(false);

            if (result.Errors.Count > 0) return SystemCheckSectionResult.Create(SystemCheckSectionStatus.Failed, Resources.SystemCheck_PersistenceFailed, result.ScannedEntities);
            if (result.Issues.Count > 0) return new SystemCheckSectionResult(SystemCheckSectionStatus.ActionRequired, Resources.SystemCheck_PersistenceActionRequired, [string.Format(Resources.SystemCheck_PersistenceIssueCount, result.Issues.Count)], result.ScannedEntities);
            return SystemCheckSectionResult.Create(SystemCheckSectionStatus.Passed, Resources.SystemCheck_PersistencePassed, result.ScannedEntities);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return SystemCheckSectionResult.Create(SystemCheckSectionStatus.Unavailable, Resources.SystemCheck_PersistenceUnavailable);
        }
    }

    private async Task<SystemCheckSectionResult> CheckRepositoryAsync(CancellationToken cancellationToken)
    {
        var operations = Base.Miras.Operations;
        if (operations is null) return SystemCheckSectionResult.Create(SystemCheckSectionStatus.Unavailable, Resources.SystemCheck_RepositoryUnavailable);

        try
        {
            var result = await operations.CheckRepositoryAsync(cancellationToken).ConfigureAwait(false);
            var status = MapRepositoryStatus(result);
            var details = result.Messages.SelectMany(ToUserDetails).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.CurrentCulture).ToArray();
            return new SystemCheckSectionResult(status, GetRepositorySummary(status), details, result.ScannedItems);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return SystemCheckSectionResult.Create(SystemCheckSectionStatus.Unavailable, Resources.SystemCheck_RepositoryUnavailable);
        }
    }

    private static string GetRepositorySummary(SystemCheckSectionStatus status) => status switch
    {
        SystemCheckSectionStatus.Passed => Resources.SystemCheck_RepositoryPassed,
        SystemCheckSectionStatus.Notice => Resources.SystemCheck_RepositoryNotice,
        SystemCheckSectionStatus.Warning => Resources.SystemCheck_RepositoryWarning,
        SystemCheckSectionStatus.ActionRequired => Resources.SystemCheck_RepositoryActionRequired,
        SystemCheckSectionStatus.Unavailable => Resources.SystemCheck_RepositoryUnavailable,
        _ => Resources.SystemCheck_RepositoryFailed
    };

    private static SystemCheckSectionStatus MapRepositoryStatus(MirasOperationResult result)
    {
        if (result.Status == MirasOperationStatus.Failed) return SystemCheckSectionStatus.Failed;
        if (result.Status == MirasOperationStatus.Incomplete || result.HasTechnicalErrors) return SystemCheckSectionStatus.Unavailable;
        if (result.Status == MirasOperationStatus.Blocked || result.HasActionRequired || result.HighestAlertLevel == MirasAlertLevel.Warning) return SystemCheckSectionStatus.ActionRequired;
        if (result.HighestAlertLevel == MirasAlertLevel.Caution) return SystemCheckSectionStatus.Warning;
        if (result.Status == MirasOperationStatus.CompletedWithIssues || result.HighestAlertLevel == MirasAlertLevel.Advisory) return SystemCheckSectionStatus.Notice;
        return SystemCheckSectionStatus.Passed;
    }

    private static IEnumerable<string> ToUserDetails(MirasUserMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.Title)) yield return message.Title;
        if (!string.IsNullOrWhiteSpace(message.Description)) yield return message.Description;
        if (!string.IsNullOrWhiteSpace(message.StatusText)) yield return message.StatusText;
        if (!string.IsNullOrWhiteSpace(message.RecommendedActionText)) yield return message.RecommendedActionText;
    }
}
