using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;

/// <summary>
/// Provides controlled local editing of the machine-wide database configuration.
/// </summary>
public sealed class DatabaseAdministrationViewModel : BindableBase
{
    private readonly CancellationToken _applicationStopping;
    private readonly IMachineConfigurationService _configurationService;
    private DelegateCommand? _cancelCommand, _resetCommand, _saveCommand, _testConnectionCommand;
    private string _connectionString = string.Empty, _originalConnectionString = string.Empty, _statusText = string.Empty, _testedConnectionString = string.Empty;
    private bool _isBusy, _isConnectionTestSuccessful, _isLoaded, _isRestartRequired;
    private CancellationTokenSource? _operationCancellation;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseAdministrationViewModel"/> class.
    /// </summary>
    public DatabaseAdministrationViewModel(IMachineConfigurationService configurationService, ILifetimeService lifetimeService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        ArgumentNullException.ThrowIfNull(lifetimeService);
        _applicationStopping = lifetimeService.ApplicationStopping;
        _ = LoadAsync(_applicationStopping);
    }

    public DelegateCommand CancelCommand => _cancelCommand ??= new DelegateCommand(ExecuteCancel, () => IsBusy && _operationCancellation is not null);

    public string ConnectionString
    {
        get => _connectionString;
        set
        {
            if (!SetProperty(ref _connectionString, value)) return;
            InvalidateConnectionTest();
            IsRestartRequired = false;
            StatusText = string.Empty;
            RaiseCommandStates();
        }
    }

    public bool HasChanges => !string.Equals(ConnectionString, _originalConnectionString, StringComparison.Ordinal);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            RaisePropertyChanged(nameof(IsInteractionEnabled));
            RaiseCommandStates();
        }
    }

    public bool IsConnectionTestSuccessful
    {
        get => _isConnectionTestSuccessful;
        private set
        {
            if (!SetProperty(ref _isConnectionTestSuccessful, value)) return;
            RaiseCommandStates();
        }
    }

    public bool IsInteractionEnabled => !IsBusy;
    public bool IsRestartRequired { get => _isRestartRequired; private set => SetProperty(ref _isRestartRequired, value); }
    public DelegateCommand ResetCommand => _resetCommand ??= new DelegateCommand(ExecuteReset, () => !IsBusy && HasChanges);
    public DelegateCommand SaveCommand => _saveCommand ??= new DelegateCommand(() => _ = SaveAsync(), CanSave);
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public DelegateCommand TestConnectionCommand => _testConnectionCommand ??= new DelegateCommand(() => _ = TestConnectionAsync(), CanTestConnection);

    private static string GetUpdateStatusText(DatabaseConfigurationUpdateStatus status) => status switch
    {
        DatabaseConfigurationUpdateStatus.Unauthorized => Resources.DatabaseAdministration_AccessDenied,
        DatabaseConfigurationUpdateStatus.ConfigurationUnavailable => Resources.DatabaseAdministration_ConfigurationUnavailable,
        DatabaseConfigurationUpdateStatus.ValidationFailed => Resources.DatabaseAdministration_ValidationFailed,
        DatabaseConfigurationUpdateStatus.Failed or DatabaseConfigurationUpdateStatus.NotStarted or DatabaseConfigurationUpdateStatus.Completed or _ => Resources.DatabaseAdministration_SaveFailed
    };

    private CancellationTokenSource BeginOperation()
    {
        CancelAndDisposeOperation();
        _operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(_applicationStopping);
        RaiseCommandStates();
        return _operationCancellation;
    }

    private void CancelAndDisposeOperation()
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = null;
    }

    private bool CanSave() => !IsBusy && _isLoaded && HasChanges && IsConnectionTestSuccessful && string.Equals(ConnectionString.Trim(), _testedConnectionString, StringComparison.Ordinal) && _configurationService.CanModify;

    private bool CanTestConnection() => !IsBusy && _isLoaded && !string.IsNullOrWhiteSpace(ConnectionString) && _configurationService.CanModify;

    private void CompleteOperation(CancellationTokenSource cancellation)
    {
        if (!ReferenceEquals(_operationCancellation, cancellation)) return;
        cancellation.Dispose();
        _operationCancellation = null;
        RaiseCommandStates();
    }

    private void ExecuteCancel() => _operationCancellation?.Cancel();

    private void ExecuteReset()
    {
        _connectionString = _originalConnectionString;
        RaisePropertyChanged(nameof(ConnectionString));
        InvalidateConnectionTest();
        IsRestartRequired = false;
        StatusText = Resources.DatabaseAdministration_ChangesDiscarded;
        RaiseCommandStates();
    }

    private void InvalidateConnectionTest()
    {
        IsConnectionTestSuccessful = false;
        _testedConnectionString = string.Empty;
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            StatusText = Resources.DatabaseAdministration_Loading;
            var configuration = await _configurationService.LoadAsync(cancellationToken).ConfigureAwait(true);
            _originalConnectionString = configuration.Database.ConnectionString;
            _connectionString = _originalConnectionString;
            _isLoaded = true;
            RaisePropertyChanged(nameof(ConnectionString));
            InvalidateConnectionTest();
            IsRestartRequired = false;
            StatusText = string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.DatabaseAdministration_Canceled; }
        catch { StatusText = Resources.DatabaseAdministration_LoadFailed; }
        finally { IsBusy = false; RaiseCommandStates(); }
    }

    private void RaiseCommandStates()
    {
        RaisePropertyChanged(nameof(HasChanges));
        _cancelCommand?.RaiseCanExecuteChanged();
        _resetCommand?.RaiseCanExecuteChanged();
        _saveCommand?.RaiseCanExecuteChanged();
        _testConnectionCommand?.RaiseCanExecuteChanged();
    }

    private async Task SaveAsync()
    {
        var cancellation = BeginOperation();
        try
        {
            IsBusy = true;
            StatusText = Resources.DatabaseAdministration_Saving;
            var normalizedConnectionString = ConnectionString.Trim();
            var result = await _configurationService.UpdateDatabaseAsync(new DatabaseConfigurationUpdateRequest(normalizedConnectionString), cancellation.Token).ConfigureAwait(true);
            if (!result.IsSuccess) { StatusText = GetUpdateStatusText(result.Status); return; }

            _originalConnectionString = normalizedConnectionString;
            _connectionString = normalizedConnectionString;
            RaisePropertyChanged(nameof(ConnectionString));
            InvalidateConnectionTest();
            IsRestartRequired = true;
            StatusText = Resources.DatabaseAdministration_Saved;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { StatusText = Resources.DatabaseAdministration_Canceled; }
        catch { StatusText = Resources.DatabaseAdministration_SaveFailed; }
        finally { IsBusy = false; CompleteOperation(cancellation); RaiseCommandStates(); }
    }

    private async Task TestConnectionAsync()
    {
        var cancellation = BeginOperation();
        try
        {
            IsBusy = true;
            InvalidateConnectionTest();
            StatusText = Resources.DatabaseAdministration_Testing;
            var normalizedConnectionString = ConnectionString.Trim();
            IsConnectionTestSuccessful = await _configurationService.TestDatabaseConnectionAsync(new DatabaseConfiguration(normalizedConnectionString), cancellation.Token).ConfigureAwait(true);
            if (IsConnectionTestSuccessful) _testedConnectionString = normalizedConnectionString;
            StatusText = IsConnectionTestSuccessful ? Resources.DatabaseAdministration_TestSucceeded : Resources.DatabaseAdministration_TestFailed;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { StatusText = Resources.DatabaseAdministration_Canceled; }
        catch { InvalidateConnectionTest(); StatusText = Resources.DatabaseAdministration_TestFailed; }
        finally { IsBusy = false; CompleteOperation(cancellation); RaiseCommandStates(); }
    }

    private sealed class DatabaseConfiguration(string connectionString) : IDatabaseConfiguration
    {
        public string ConnectionString { get; } = connectionString;
    }
}