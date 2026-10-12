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
/// Provides repository-location discovery, validation, creation, editing, and persistence.
/// </summary>
public sealed class RepositoryAdministrationViewModel : BindableBase
{
    private readonly IRepositoryLocationAdministrationService _administrationService;
    private readonly IApplication _application;
    private readonly CancellationToken _applicationStopping;
    private readonly IRepositoryLocationValidationService _validationService;
    private DelegateCommand? _cancelCreateCommand, _discardCommand, _newCommand, _saveCommand, _selectFolderCommand, _validateCommand;
    private DelegateCommand? _cancelDeleteCommand;
    private DelegateCommand? _confirmDeleteCommand;
    private bool _isBusy, _isCreating, _isDefaultRepository, _isPathValidated, _isRepositoryEnabled;
    private bool _isDeleteConfirmationVisible;
    private string _repositoryName = string.Empty, _repositoryPath = string.Empty, _statusText = string.Empty;
    private DelegateCommand? _requestDeleteCommand;
    private RepositoryLocationItemViewModel? _selectedLocation;

    /// <summary>Initializes a new instance of the <see cref="RepositoryAdministrationViewModel"/> class.</summary>
    public RepositoryAdministrationViewModel(IRepositoryLocationAdministrationService administrationService, IRepositoryLocationValidationService validationService, IApplication application, ILifetimeService lifetimeService)
    {
        _administrationService = administrationService ?? throw new ArgumentNullException(nameof(administrationService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _application = application ?? throw new ArgumentNullException(nameof(application));
        ArgumentNullException.ThrowIfNull(lifetimeService);
        _applicationStopping = lifetimeService.ApplicationStopping;
        _ = LoadLocationsAsync(_applicationStopping);
    }

    /// <summary>Gets the command that cancels creation.</summary>
    public DelegateCommand CancelCreateCommand => _cancelCreateCommand ??= new DelegateCommand(ExecuteCancelCreate, () => !IsBusy && IsCreating);

    /// <summary>
    /// Gets the command that cancels repository-location deletion.
    /// </summary>
    public DelegateCommand CancelDeleteCommand => _cancelDeleteCommand ??= new DelegateCommand(ExecuteCancelDelete, () => !IsBusy && IsDeleteConfirmationVisible);

    /// <summary>Gets a value indicating whether the enabled state may be changed.</summary>
    public bool CanChangeRepositoryEnabled => IsInteractionEnabled && (IsCreating || !IsDefaultRepository);

    /// <summary>
    /// Gets a value indicating whether the selected repository location may be removed.
    /// </summary>
    public bool CanDeleteSelectedLocation => !IsBusy && !IsCreating && !IsDirty && SelectedLocation is { IsDefault: false };

    /// <summary>Gets a value indicating whether another location may be selected.</summary>
    public bool CanSelectLocation => IsInteractionEnabled && !IsCreating && !IsDirty;

    /// <summary>
    /// Gets the command that confirms repository-location deletion.
    /// </summary>
    public DelegateCommand ConfirmDeleteCommand => _confirmDeleteCommand ??= new DelegateCommand(ExecuteConfirmDelete, () => CanDeleteSelectedLocation && IsDeleteConfirmationVisible);

    /// <summary>
    /// Gets the name of the repository location awaiting deletion confirmation.
    /// </summary>
    public string DeleteConfirmationRepositoryName => SelectedLocation?.Name ?? string.Empty;

    /// <summary>Gets the command that discards update changes.</summary>
    public DelegateCommand DiscardCommand => _discardCommand ??= new DelegateCommand(ExecuteDiscard, () => !IsBusy && !IsCreating && IsDirty);

    /// <summary>Gets a value indicating whether an operation is running.</summary>
    public bool IsBusy
    { get => _isBusy; private set { if (!SetProperty(ref _isBusy, value)) return; RaisePropertyChanged(nameof(IsInteractionEnabled)); RaiseEditorState(); } }

    /// <summary>Gets a value indicating whether a new repository location is being created.</summary>
    public bool IsCreating
    { get => _isCreating; private set { if (!SetProperty(ref _isCreating, value)) return; RaiseEditorState(); } }

    /// <summary>Gets or sets whether the edited location is the default import target.</summary>
    public bool IsDefaultRepository
    { get => _isDefaultRepository; set { if (!IsCreating && SelectedLocation?.IsDefault == true && !value || !SetProperty(ref _isDefaultRepository, value)) return; if (value && !IsRepositoryEnabled) { _isRepositoryEnabled = true; RaisePropertyChanged(nameof(IsRepositoryEnabled)); } OnEditorChanged(false); } }

    /// <summary>
    /// Gets a value indicating whether the deletion confirmation is visible.
    /// </summary>
    public bool IsDeleteConfirmationVisible
    {
        get => _isDeleteConfirmationVisible;
        private set
        {
            if (!SetProperty(ref _isDeleteConfirmationVisible, value)) return;

            RaisePropertyChanged(nameof(CanDeleteSelectedLocation));
            RaiseDeleteCommandStates();
        }
    }

    /// <summary>Gets a value indicating whether the editor contains unsaved values.</summary>
    public bool IsDirty => IsCreating ? !string.IsNullOrWhiteSpace(RepositoryName) || !string.IsNullOrWhiteSpace(RepositoryPath) || !IsRepositoryEnabled || IsDefaultRepository : SelectedLocation is not null && (!string.Equals(RepositoryName.Trim(), SelectedLocation.Name, StringComparison.Ordinal) || !string.Equals(RepositoryPath.Trim(), SelectedLocation.RootPath, StringComparison.OrdinalIgnoreCase) || IsRepositoryEnabled != SelectedLocation.IsEnabled || IsDefaultRepository != SelectedLocation.IsDefault);

    /// <summary>Gets a value indicating whether the user can interact with the view.</summary>
    public bool IsInteractionEnabled => !IsBusy;

    /// <summary>Gets or sets whether the edited location accepts new imports.</summary>
    public bool IsRepositoryEnabled
    { get => _isRepositoryEnabled; set { if (IsDefaultRepository && !value || !SetProperty(ref _isRepositoryEnabled, value)) return; OnEditorChanged(false); } }

    /// <summary>Gets every configured repository location.</summary>
    public ObservableCollection<RepositoryLocationItemViewModel> Locations { get; } = [];

    /// <summary>Gets the command that starts repository creation.</summary>
    public DelegateCommand NewCommand => _newCommand ??= new DelegateCommand(ExecuteNew, () => !IsBusy && !IsCreating && !IsDirty);

    /// <summary>Gets or sets the user-facing repository name.</summary>
    public string RepositoryName
    { get => _repositoryName; set { if (!SetProperty(ref _repositoryName, value)) return; OnEditorChanged(false); } }

    /// <summary>Gets or sets the repository root path.</summary>
    public string RepositoryPath
    { get => _repositoryPath; set { if (!SetProperty(ref _repositoryPath, value)) return; OnEditorChanged(true); } }

    /// <summary>
    /// Gets the command that opens repository-location deletion confirmation.
    /// </summary>
    public DelegateCommand RequestDeleteCommand => _requestDeleteCommand ??= new DelegateCommand(ExecuteRequestDelete, () => CanDeleteSelectedLocation && !IsDeleteConfirmationVisible);

    /// <summary>Gets the command that creates or updates the edited location.</summary>
    public DelegateCommand SaveCommand => _saveCommand ??= new DelegateCommand(ExecuteSave, CanSave);

    /// <summary>Gets or sets the selected persisted repository location.</summary>
    public RepositoryLocationItemViewModel? SelectedLocation
    {
        get => _selectedLocation;
        set
        {
            if (IsCreating || IsDirty || !SetProperty(ref _selectedLocation, value))
            {
                return;
            }

            IsDeleteConfirmationVisible = false;
            ApplySelection(value);
        }
    }

    /// <summary>Gets the command that opens the folder-selection dialog.</summary>
    public DelegateCommand SelectFolderCommand => _selectFolderCommand ??= new DelegateCommand(ExecuteSelectFolder, () => !IsBusy && (IsCreating || SelectedLocation is not null));

    /// <summary>Gets the current localized operation status.</summary>
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

    /// <summary>Gets the command that validates the edited root path.</summary>
    public DelegateCommand ValidateCommand => _validateCommand ??= new DelegateCommand(ExecuteValidate, () => !IsBusy && (IsCreating || SelectedLocation is not null) && !string.IsNullOrWhiteSpace(RepositoryPath));

    private void ApplySelection(RepositoryLocationItemViewModel? location)
    {
        _repositoryName = location?.Name ?? string.Empty;
        _repositoryPath = location?.RootPath ?? string.Empty;
        _isRepositoryEnabled = location?.IsEnabled ?? false;
        _isDefaultRepository = location?.IsDefault ?? false;
        _isPathValidated = location is not null;
        RaiseEditorProperties();
        StatusText = string.Empty;
    }

    private bool CanSave() => !IsBusy && (IsCreating || SelectedLocation is not null) && (IsCreating || IsDirty) && _isPathValidated && !string.IsNullOrWhiteSpace(RepositoryName) && !string.IsNullOrWhiteSpace(RepositoryPath) && (!IsDefaultRepository || IsRepositoryEnabled);

    private async Task DeleteSelectedLocationAsync(CancellationToken cancellationToken)
    {
        var selectedLocation = SelectedLocation;
        if (selectedLocation is null || selectedLocation.IsDefault)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusText = Resources.RepositoryAdministration_Deleting;

            var request = new RepositoryLocationDeleteRequest(
                selectedLocation.Id,
                [.. selectedLocation.RowVersion]);

            var result = await _administrationService.DeleteAsync(request, cancellationToken).ConfigureAwait(true);

            if (result.IsSuccess)
            {
                IsDeleteConfirmationVisible = false;

                await LoadLocationsAsync(cancellationToken)
                    .ConfigureAwait(true);

                StatusText = Resources.RepositoryAdministration_Deleted;
                return;
            }

            StatusText = result.IsConflict ? Resources.RepositoryAdministration_DeleteConflict : result.IsDefaultRepository ? Resources.RepositoryAdministration_DeleteDefaultRepository : result.IsInUse ? Resources.RepositoryAdministration_DeleteInUse : result.IsNotFound ? Resources.RepositoryAdministration_NotFound : result.IsUnauthorized ? Resources.RepositoryAdministration_Unauthorized : Resources.RepositoryAdministration_DeleteFailed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = Resources.RepositoryAdministration_Canceled;
        }
        catch
        {
            StatusText = Resources.RepositoryAdministration_DeleteFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExecuteCancelCreate()
    { IsCreating = false; ApplySelection(Locations.FirstOrDefault(location => location.IsDefault) ?? Locations.FirstOrDefault()); StatusText = Resources.RepositoryAdministration_CreationCanceled; }

    private void ExecuteCancelDelete()
    {
        IsDeleteConfirmationVisible = false;
        StatusText = string.Empty;
    }

    private async void ExecuteConfirmDelete() => await DeleteSelectedLocationAsync(_applicationStopping);

    private void ExecuteDiscard()
    {
        IsDeleteConfirmationVisible = false;
        ApplySelection(SelectedLocation);
        StatusText = Resources.RepositoryAdministration_ChangesDiscarded;
    }

    private void ExecuteNew()
    {
        IsDeleteConfirmationVisible = false;
        _selectedLocation = null;
        RaisePropertyChanged(nameof(SelectedLocation));
        IsCreating = true;
        _repositoryName = string.Empty;
        _repositoryPath = string.Empty;
        _isRepositoryEnabled = true;
        _isDefaultRepository = Locations.Count == 0;
        _isPathValidated = false;
        RaiseEditorProperties();
        StatusText = Resources.RepositoryAdministration_NewInstructions;
    }

    private void ExecuteRequestDelete()
    {
        if (!CanDeleteSelectedLocation)
        {
            return;
        }

        IsDeleteConfirmationVisible = true;
        StatusText = string.Empty;
    }

    private async void ExecuteSave() => await SaveAsync(_applicationStopping);

    private void ExecuteSelectFolder()
    {
        var service = _application.DialogService?.FileDialogService ?? throw new InvalidOperationException("The WPF file dialog service has not been initialized.");
        var selectedPath = service.SelectFolder(Resources.RepositoryAdministration_FolderDialogTitle, string.IsNullOrWhiteSpace(RepositoryPath) ? null : RepositoryPath);
        if (!string.IsNullOrWhiteSpace(selectedPath)) RepositoryPath = selectedPath;
    }

    private async void ExecuteValidate() => await ValidateAsync(_applicationStopping);

    private async Task LoadLocationsAsync(CancellationToken cancellationToken, int? selectedId = null)
    {
        try
        {
            IsBusy = true;
            StatusText = Resources.RepositoryAdministration_Loading;
            var summaries = await _administrationService.GetAllAsync(cancellationToken).ConfigureAwait(true);
            Locations.Clear();
            foreach (var summary in summaries) Locations.Add(new RepositoryLocationItemViewModel(summary.Id, summary.Name, summary.RootPath, summary.IsDefault, summary.IsEnabled, summary.RowVersion));
            _selectedLocation = selectedId.HasValue ? Locations.FirstOrDefault(location => location.Id == selectedId) : Locations.FirstOrDefault(location => location.IsDefault) ?? Locations.FirstOrDefault();
            RaisePropertyChanged(nameof(SelectedLocation));
            IsCreating = false;
            ApplySelection(SelectedLocation);
            StatusText = Locations.Count == 0 ? Resources.RepositoryAdministration_NoLocations : string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.RepositoryAdministration_Canceled; }
        catch { StatusText = Resources.RepositoryAdministration_LoadFailed; }
        finally { IsBusy = false; }
    }

    private void OnEditorChanged(bool resetPathValidation)
    { if (resetPathValidation) _isPathValidated = false; StatusText = string.Empty; RaisePropertyChanged(nameof(IsDirty)); RaiseEditorState(); }

    private void RaiseDeleteCommandStates()
    {
        _cancelDeleteCommand?.RaiseCanExecuteChanged();
        _confirmDeleteCommand?.RaiseCanExecuteChanged();
        _requestDeleteCommand?.RaiseCanExecuteChanged();
    }

    private void RaiseEditorProperties()
    {
        RaisePropertyChanged(nameof(RepositoryName));
        RaisePropertyChanged(nameof(RepositoryPath));
        RaisePropertyChanged(nameof(IsRepositoryEnabled));
        RaisePropertyChanged(nameof(IsDefaultRepository));
        RaisePropertyChanged(nameof(IsDirty));
        RaiseEditorState();
    }

    private void RaiseEditorState()
    {
        RaisePropertyChanged(nameof(CanChangeRepositoryEnabled));
        RaisePropertyChanged(nameof(CanDeleteSelectedLocation));
        RaisePropertyChanged(nameof(CanSelectLocation));
        RaisePropertyChanged(nameof(DeleteConfirmationRepositoryName));

        _cancelCreateCommand?.RaiseCanExecuteChanged();
        _discardCommand?.RaiseCanExecuteChanged();
        _newCommand?.RaiseCanExecuteChanged();
        _saveCommand?.RaiseCanExecuteChanged();
        _selectFolderCommand?.RaiseCanExecuteChanged();
        _validateCommand?.RaiseCanExecuteChanged();

        RaiseDeleteCommandStates();
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            StatusText = IsCreating ? Resources.RepositoryAdministration_Creating : Resources.RepositoryAdministration_Saving;
            if (IsCreating)
            {
                var createResult = await _administrationService.CreateAsync(new RepositoryLocationCreateRequest(RepositoryName.Trim(), RepositoryPath.Trim(), IsRepositoryEnabled, IsDefaultRepository), cancellationToken).ConfigureAwait(true);
                if (createResult.IsSuccess && createResult.Location is not null) { await LoadLocationsAsync(cancellationToken, createResult.Location.Id).ConfigureAwait(true); StatusText = Resources.RepositoryAdministration_Created; return; }
                StatusText = createResult.IsDuplicatePath ? Resources.RepositoryAdministration_DuplicatePath : createResult.IsUnauthorized ? Resources.RepositoryAdministration_Unauthorized : Resources.RepositoryAdministration_CreateFailed;
                return;
            }

            var selected = SelectedLocation;
            if (selected is null) return;
            var result = await _administrationService.UpdateAsync(new RepositoryLocationUpdateRequest(selected.Id, RepositoryName.Trim(), RepositoryPath.Trim(), IsRepositoryEnabled, IsDefaultRepository, [.. selected.RowVersion]), cancellationToken).ConfigureAwait(true);
            if (result.IsSuccess && result.Location is not null) { await LoadLocationsAsync(cancellationToken, result.Location.Id).ConfigureAwait(true); StatusText = Resources.RepositoryAdministration_Saved; return; }
            StatusText = result.IsConflict ? Resources.RepositoryAdministration_Conflict : result.IsDuplicatePath ? Resources.RepositoryAdministration_DuplicatePath : result.IsInvariantViolation ? Resources.RepositoryAdministration_DefaultInvariant : result.IsNotFound ? Resources.RepositoryAdministration_NotFound : result.IsUnauthorized ? Resources.RepositoryAdministration_Unauthorized : Resources.RepositoryAdministration_SaveFailed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.RepositoryAdministration_Canceled; }
        catch { StatusText = IsCreating ? Resources.RepositoryAdministration_CreateFailed : Resources.RepositoryAdministration_SaveFailed; }
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
            if (result.IsValid) { _repositoryPath = result.NormalizedPath ?? RepositoryPath; RaisePropertyChanged(nameof(RepositoryPath)); StatusText = Resources.RepositoryAdministration_Valid; RaisePropertyChanged(nameof(IsDirty)); return; }
            StatusText = !result.Exists ? Resources.RepositoryAdministration_Missing : !result.IsReadable ? Resources.RepositoryAdministration_NotReadable : !result.IsWritable ? Resources.RepositoryAdministration_NotWritable : Resources.RepositoryAdministration_Invalid;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.RepositoryAdministration_Canceled; }
        catch { _isPathValidated = false; StatusText = Resources.RepositoryAdministration_Invalid; }
        finally { IsBusy = false; RaiseEditorState(); }
    }
}