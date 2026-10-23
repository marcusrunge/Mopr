using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.SystemCheck;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.SystemCheck;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Navigation.Regions;
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
    private readonly IRegionManager _regionManager;
    private readonly ISystemCheckService _systemCheckService;
    private CancellationTokenSource? _runCancellationSource;
    private DelegateCommand? _cancelCommand, _openMirasActionsCommand, _runCommand;
    private bool _isMirasActionRequired, _isRunning;
    private string _overallStatusText = Resources.SystemCheck_InitialTitle, _statusText = Resources.SystemCheck_InitialDescription;
    private TimeSpan? _duration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemCheckViewModel"/> class.
    /// </summary>
    public SystemCheckViewModel(IApplication application, ILifetimeService lifetimeService, IRegionManager regionManager)
    {
        _systemCheckService = (application ?? throw new ArgumentNullException(nameof(application))).SystemCheck ?? throw new InvalidOperationException("The system-check service has not been initialized.");
        ArgumentNullException.ThrowIfNull(lifetimeService);
        _applicationStopping = lifetimeService.ApplicationStopping;
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
    }

    public DelegateCommand CancelCommand => _cancelCommand ??= new DelegateCommand(ExecuteCancel, () => IsRunning);

    public TimeSpan? Duration { get => _duration; private set => SetProperty(ref _duration, value); }

    public bool HasResult => Sections.Count > 0;

    public bool IsMirasActionRequired
    {
        get => _isMirasActionRequired;
        private set
        {
            if (!SetProperty(ref _isMirasActionRequired, value)) return;
            _openMirasActionsCommand?.RaiseCanExecuteChanged();
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!SetProperty(ref _isRunning, value)) return;
            _cancelCommand?.RaiseCanExecuteChanged();
            _openMirasActionsCommand?.RaiseCanExecuteChanged();
            _runCommand?.RaiseCanExecuteChanged();
        }
    }

    public string OverallStatusText { get => _overallStatusText; private set => SetProperty(ref _overallStatusText, value); }

    public DelegateCommand OpenMirasActionsCommand => _openMirasActionsCommand ??= new DelegateCommand(ExecuteOpenMirasActions, () => IsMirasActionRequired && !IsRunning);

    public DelegateCommand RunCommand => _runCommand ??= new DelegateCommand(() => _ = RunAsync(), () => !IsRunning);

    public ObservableCollection<SystemCheckSectionViewModel> Sections { get; } = [];

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    private void ApplyResult(SystemCheckResult result)
    {
        Sections.Clear();
        Sections.Add(new SystemCheckSectionViewModel(Resources.SystemCheck_Database, result.Database));
        Sections.Add(new SystemCheckSectionViewModel(Resources.SystemCheck_Persistence, result.Persistence));
        Sections.Add(new SystemCheckSectionViewModel(Resources.SystemCheck_Repository, result.Repository));
        IsMirasActionRequired = result.Repository.Status == SystemCheckSectionStatus.ActionRequired;
        OverallStatusText = GetOverallStatusText(result);
        Duration = result.Duration;
        RaisePropertyChanged(nameof(HasResult));
    }

    private void ExecuteCancel() => _runCancellationSource?.Cancel();

    private void ExecuteOpenMirasActions()
    {
        var parameters = new NavigationParameters { { NavigationParameterNames.MirasAdministrativeContext, true } };
        _regionManager.RequestNavigate(RegionNames.ContentRegion, NavigationNames.MirasActionRequired, parameters);
    }

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
        if (IsRunning) return;
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_applicationStopping);
        _runCancellationSource = cancellationSource;
        try
        {
            IsRunning = true;
            StatusText = Resources.SystemCheck_Running;
            OverallStatusText = string.Empty;
            Duration = null;
            IsMirasActionRequired = false;
            Sections.Clear();
            RaisePropertyChanged(nameof(HasResult));
            var result = await _systemCheckService.RunAsync(cancellationSource.Token).ConfigureAwait(true);
            ApplyResult(result);
            StatusText = Resources.SystemCheck_Completed;
        }
        catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
        {
            StatusText = Resources.SystemCheck_Canceled;
        }
        catch
        {
            StatusText = Resources.SystemCheck_RunFailed;
        }
        finally
        {
            if (ReferenceEquals(_runCancellationSource, cancellationSource)) _runCancellationSource = null;
            IsRunning = false;
        }
    }
}
