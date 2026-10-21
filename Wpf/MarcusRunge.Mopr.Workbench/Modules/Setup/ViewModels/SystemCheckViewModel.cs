using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.SystemCheck;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.SystemCheck;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;

/// <summary>
/// Provides the read-only MOPR workstation system check.
/// </summary>
public sealed class SystemCheckViewModel : BindableBase
{
    private readonly CancellationToken _applicationStopping;
    private readonly ISystemCheckService _systemCheckService;
    private CancellationTokenSource? _runCancellationSource;
    private DelegateCommand? _cancelCommand, _runCommand;
    private bool _isRunning;
    private string _overallStatusText = Resources.SystemCheck_InitialTitle, _statusText = Resources.SystemCheck_InitialDescription;
    private TimeSpan? _duration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemCheckViewModel"/> class.
    /// </summary>
    public SystemCheckViewModel(IApplication application, ILifetimeService lifetimeService)
    {
        _systemCheckService = (application ?? throw new ArgumentNullException(nameof(application))).SystemCheck ?? throw new InvalidOperationException("The system-check service has not been initialized.");
        ArgumentNullException.ThrowIfNull(lifetimeService);
        _applicationStopping = lifetimeService.ApplicationStopping;
    }

    public DelegateCommand CancelCommand => _cancelCommand ??= new DelegateCommand(ExecuteCancel, () => IsRunning);
    public TimeSpan? Duration { get => _duration; private set => SetProperty(ref _duration, value); }
    public bool HasResult => Sections.Count > 0;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!SetProperty(ref _isRunning, value)) return;
            _cancelCommand?.RaiseCanExecuteChanged();
            _runCommand?.RaiseCanExecuteChanged();
        }
    }

    public string OverallStatusText { get => _overallStatusText; private set => SetProperty(ref _overallStatusText, value); }
    public DelegateCommand RunCommand => _runCommand ??= new DelegateCommand(() => _ = RunAsync(), () => !IsRunning);
    public ObservableCollection<SystemCheckSectionViewModel> Sections { get; } = [];
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    private void ApplyResult(SystemCheckResult result)
    {
        Sections.Clear();
        Sections.Add(new SystemCheckSectionViewModel(Resources.SystemCheck_Database, result.Database));
        Sections.Add(new SystemCheckSectionViewModel(Resources.SystemCheck_Persistence, result.Persistence));
        Sections.Add(new SystemCheckSectionViewModel(Resources.SystemCheck_Repository, result.Repository));
        OverallStatusText = GetOverallStatusText(result);
        Duration = result.Duration;
        RaisePropertyChanged(nameof(HasResult));
    }

    private void ExecuteCancel() => _runCancellationSource?.Cancel();

    private static string GetOverallStatusText(SystemCheckResult result)
    {
        if (result.IsSuccessful) return Resources.SystemCheck_OverallPassed;
        return result.Status switch
        {
            SystemCheckSectionStatus.ActionRequired => Resources.SystemCheck_OverallActionRequired,
            SystemCheckSectionStatus.Warning => Resources.SystemCheck_OverallWarning,
            SystemCheckSectionStatus.Notice => Resources.SystemCheck_OverallNotice,
            SystemCheckSectionStatus.Unavailable => Resources.SystemCheck_OverallUnavailable,
            SystemCheckSectionStatus.Canceled => Resources.SystemCheck_Canceled,
            _ => Resources.SystemCheck_OverallFailed
        };
    }

    private async Task RunAsync()
    {
        _runCancellationSource?.Dispose();
        _runCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_applicationStopping);

        try
        {
            IsRunning = true;
            StatusText = Resources.SystemCheck_Running;
            OverallStatusText = string.Empty;
            Duration = null;
            Sections.Clear();
            RaisePropertyChanged(nameof(HasResult));
            var result = await _systemCheckService.RunAsync(_runCancellationSource.Token).ConfigureAwait(true);
            ApplyResult(result);
            StatusText = Resources.SystemCheck_Completed;
        }
        catch (OperationCanceledException) when (_runCancellationSource.IsCancellationRequested)
        {
            StatusText = Resources.SystemCheck_Canceled;
        }
        catch
        {
            StatusText = Resources.SystemCheck_RunFailed;
        }
        finally
        {
            IsRunning = false;
        }
    }
}
