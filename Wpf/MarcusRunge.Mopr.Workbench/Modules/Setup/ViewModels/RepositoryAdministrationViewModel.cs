using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Services;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;

/// <summary>
/// Provides repository-location discovery, validation, editing, and persistence.
/// </summary>
public sealed class RepositoryAdministrationViewModel : BindableBase
{
    private readonly IRepositoryLocationAdministrationService _administrationService;
    private readonly IApplication _application;
    private readonly CancellationToken _applicationStopping;
    private readonly IRepositoryLocationValidationService _validationService;
    private DelegateCommand? _discardCommand;
    private bool _isBusy;
    private bool _isDefaultRepository;
    private bool _isPathValidated;
    private bool _isRepositoryEnabled;
    private string _repositoryName = string.Empty;
    private string _repositoryPath = string.Empty;
    private DelegateCommand? _saveCommand;
    private RepositoryLocationItemViewModel? _selectedLocation;
    private DelegateCommand? _selectFolderCommand;
    private string _statusText = string.Empty;
    private DelegateCommand? _validateCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryAdministrationViewModel"/> class.
    /// </summary>
    public RepositoryAdministrationViewModel(IRepositoryLocationAdministrationService administrationService, IRepositoryLocationValidationService validationService, IApplication application, ILifetimeService lifetimeService)
    {
        _administrationService = administrationService ?? throw new ArgumentNullException(nameof(administrationService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _application = application ?? throw new ArgumentNullException(nameof(application));
        ArgumentNullException.ThrowIfNull(lifetimeService);
        _applicationStopping = lifetimeService.ApplicationStopping;
        _ = LoadLocationsAsync(_applicationStopping);
    }

    /// <summary>
    /// Gets the command that discards unsaved editor changes.
    /// </summary>
    public DelegateCommand DiscardCommand => _discardCommand ??= new DelegateCommand(ExecuteDiscard, () => !IsBusy && IsDirty);

    /// <summary>
    /// Gets a value indicating whether a repository operation is running.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value indicating whether the edited location is the default import target.
    /// </summary>
    public bool IsDefaultRepository
    {
        get => _isDefaultRepository;
        set
        {
            if (!SetProperty(ref _isDefaultRepository, value)) return;
            if (value) IsRepositoryEnabled = true;
            OnEditorChanged(resetPathValidation: false);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the editor contains unsaved changes.
    /// </summary>
    public bool IsDirty => SelectedLocation is not null && (!string.Equals(RepositoryName.Trim(), SelectedLocation.Name, StringComparison.Ordinal) || !string.Equals(RepositoryPath.Trim(), SelectedLocation.RootPath, StringComparison.OrdinalIgnoreCase) || IsRepositoryEnabled != SelectedLocation.IsEnabled || IsDefaultRepository != SelectedLocation.IsDefault);

    /// <summary>
    /// Gets a value indicating whether the user can interact with the view.
    /// </summary>
    public bool IsInteractionEnabled => !IsBusy;

    /// <summary>
    /// Gets or sets a value indicating whether the edited location accepts new imports.
    /// </summary>
    public bool IsRepositoryEnabled
    {
        get => _isRepositoryEnabled;
        set
        {
            if (!SetProperty(ref _isRepositoryEnabled, value)) return;
            OnEditorChanged(resetPathValidation: false);
        }
    }

    /// <summary>
    /// Gets the configured repository locations from the active database.
    /// </summary>
    public ObservableCollection<RepositoryLocationItemViewModel> Locations { get; } = [];

    /// <summary>
    /// Gets or sets the user-facing repository name.
    /// </summary>
    public string RepositoryName
    {
        get => _repositoryName;
        set
        {
            if (!SetProperty(ref _repositoryName, value)) return;
            OnEditorChanged(resetPathValidation: false);
        }
    }

    /// <summary>
    /// Gets or sets the repository path being edited locally.
    /// </summary>
    public string RepositoryPath
    {
        get => _repositoryPath;
        set
        {
            if (!SetProperty(ref _repositoryPath, value)) return;
            OnEditorChanged(resetPathValidation: true);
        }
    }

    /// <summary>
    /// Gets the command that persists validated editor changes.
    /// </summary>
    public DelegateCommand SaveCommand => _saveCommand ??= new DelegateCommand(ExecuteSave, CanSave);

    /// <summary>
    /// Gets or sets the selected configured repository location.
    /// </summary>
    public RepositoryLocationItemViewModel? SelectedLocation
    {
        get => _selectedLocation;
        set
        {
            if (!SetProperty(ref _selectedLocation, value)) return;
            ApplySelection(value);
        }
    }

    /// <summary>
    /// Gets the command that opens the folder-selection dialog.
    /// </summary>
    public DelegateCommand SelectFolderCommand => _selectFolderCommand ??= new DelegateCommand(ExecuteSelectFolder, () => !IsBusy && SelectedLocation is not null);

    /// <summary>
    /// Gets the current localized operation status.
    /// </summary>
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    /// <summary>
    /// Gets the command that validates the locally selected repository path.
    /// </summary>
    public DelegateCommand ValidateCommand => _validateCommand ??= new DelegateCommand(ExecuteValidate, () => !IsBusy && SelectedLocation is not null && !string.IsNullOrWhiteSpace(RepositoryPath));

    private void ApplySelection(RepositoryLocationItemViewModel? location)
    {
        if (location is null)
        {
            RepositoryName = string.Empty;
            RepositoryPath = string.Empty;
            IsRepositoryEnabled = false;
            IsDefaultRepository = false;
            _isPathValidated = false;
            RaisePropertyChanged(nameof(IsDirty));
            RaiseCommandStates();
            return;
        }

        _repositoryName = location.Name;
        _repositoryPath = location.RootPath;
        _isRepositoryEnabled = location.IsEnabled;
        _isDefaultRepository = location.IsDefault;
        _isPathValidated = true;
        RaisePropertyChanged(nameof(RepositoryName));
        RaisePropertyChanged(nameof(RepositoryPath));
        RaisePropertyChanged(nameof(IsRepositoryEnabled));
        RaisePropertyChanged(nameof(IsDefaultRepository));
        RaisePropertyChanged(nameof(IsDirty));
        StatusText = string.Empty;
        RaiseCommandStates();
    }

    private bool CanSave() => !IsBusy && SelectedLocation is not null && IsDirty && _isPathValidated && !string.IsNullOrWhiteSpace(RepositoryName) && !string.IsNullOrWhiteSpace(RepositoryPath) && (!IsDefaultRepository || IsRepositoryEnabled);

    private void ExecuteDiscard()
    {
        ApplySelection(SelectedLocation);
        StatusText = Resources.RepositoryAdministration_ChangesDiscarded;
    }

    private async void ExecuteSave() => await SaveAsync(_applicationStopping);

    private void ExecuteSelectFolder()
    {
        var fileDialogService = _application.DialogService?.FileDialogService ?? throw new InvalidOperationException("The WPF file dialog service has not been initialized.");
        var selectedPath = fileDialogService.SelectFolder(Resources.RepositoryAdministration_FolderDialogTitle, string.IsNullOrWhiteSpace(RepositoryPath) ? null : RepositoryPath);
        if (!string.IsNullOrWhiteSpace(selectedPath)) RepositoryPath = selectedPath;
    }

    private async void ExecuteValidate() => await ValidateAsync(_applicationStopping);

    private async Task LoadLocationsAsync(CancellationToken cancellationToken, int? selectedLocationId = null)
    {
        try
        {
            IsBusy = true;
            StatusText = Resources.RepositoryAdministration_Loading;
            var summaries = await _administrationService.GetAllAsync(cancellationToken).ConfigureAwait(true);
            Locations.Clear();
            foreach (var item in summaries.Select(summary => new RepositoryLocationItemViewModel(summary.Id, summary.Name, summary.RootPath, summary.IsDefault, summary.IsEnabled, summary.RowVersion))) Locations.Add(item);
            SelectedLocation = selectedLocationId.HasValue ? Locations.FirstOrDefault(location => location.Id == selectedLocationId.Value) : Locations.FirstOrDefault(location => location.IsDefault) ?? Locations.FirstOrDefault();
            StatusText = Locations.Count == 0 ? Resources.RepositoryAdministration_NoLocations : string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.RepositoryAdministration_Canceled; }
        catch { StatusText = Resources.RepositoryAdministration_LoadFailed; }
        finally { IsBusy = false; }
    }

    private void OnEditorChanged(bool resetPathValidation)
    {
        if (resetPathValidation) _isPathValidated = false;
        StatusText = string.Empty;
        RaisePropertyChanged(nameof(IsDirty));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        _discardCommand?.RaiseCanExecuteChanged();
        _saveCommand?.RaiseCanExecuteChanged();
        _selectFolderCommand?.RaiseCanExecuteChanged();
        _validateCommand?.RaiseCanExecuteChanged();
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var selectedLocation = SelectedLocation;
        if (selectedLocation is null) return;

        try
        {
            IsBusy = true;
            StatusText = Resources.RepositoryAdministration_Saving;
            var request = new RepositoryLocationUpdateRequest(selectedLocation.Id, RepositoryName.Trim(), RepositoryPath.Trim(), IsRepositoryEnabled, IsDefaultRepository, [.. selectedLocation.RowVersion]);
            var result = await _administrationService.UpdateAsync(request, cancellationToken).ConfigureAwait(true);
            if (result.IsSuccess && result.Location is not null)
            {
                await LoadLocationsAsync(cancellationToken, result.Location.Id).ConfigureAwait(true);
                StatusText = Resources.RepositoryAdministration_Saved;
                return;
            }

            StatusText = result.IsConflict ? Resources.RepositoryAdministration_Conflict : result.IsDuplicatePath ? Resources.RepositoryAdministration_DuplicatePath : result.IsInvariantViolation ? Resources.RepositoryAdministration_DefaultInvariant : result.IsNotFound ? Resources.RepositoryAdministration_NotFound : result.IsUnauthorized ? Resources.RepositoryAdministration_Unauthorized : Resources.RepositoryAdministration_SaveFailed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.RepositoryAdministration_Canceled; }
        catch { StatusText = Resources.RepositoryAdministration_SaveFailed; }
        finally { IsBusy = false; }
    }

    private async Task ValidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            StatusText = Resources.RepositoryAdministration_Validating;
            var result = await _validationService.ValidateAsync(RepositoryPath, cancellationToken).ConfigureAwait(true);
            _isPathValidated = result.IsValid;
            if (result.IsValid)
            {
                _repositoryPath = result.NormalizedPath ?? RepositoryPath;
                RaisePropertyChanged(nameof(RepositoryPath));
                StatusText = Resources.RepositoryAdministration_Valid;
                RaisePropertyChanged(nameof(IsDirty));
                return;
            }

            StatusText = !result.Exists ? Resources.RepositoryAdministration_Missing : !result.IsReadable ? Resources.RepositoryAdministration_NotReadable : !result.IsWritable ? Resources.RepositoryAdministration_NotWritable : Resources.RepositoryAdministration_Invalid;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.RepositoryAdministration_Canceled; }
        catch { _isPathValidated = false; StatusText = Resources.RepositoryAdministration_Invalid; }
        finally { IsBusy = false; RaiseCommandStates(); }
    }
}