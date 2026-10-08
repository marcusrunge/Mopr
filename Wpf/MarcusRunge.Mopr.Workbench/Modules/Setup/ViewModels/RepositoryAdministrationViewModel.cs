using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
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

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels
{
    /// <summary>
    /// Provides read-only repository-location discovery and local path validation.
    /// </summary>
    public sealed class RepositoryAdministrationViewModel : BindableBase
    {
        private readonly IApplication _application;
        private readonly CancellationToken _applicationStopping;
        private readonly IRepositoryLocationAdministrationService _administrationService;
        private readonly IRepositoryLocationValidationService _validationService;
        private bool _isBusy;
        private string _repositoryPath = string.Empty;
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
        /// Gets a value indicating whether the user can interact with the view.
        /// </summary>
        public bool IsInteractionEnabled => !IsBusy;

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
                _selectFolderCommand?.RaiseCanExecuteChanged();
                _validateCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets the configured repository locations from the active database.
        /// </summary>
        public ObservableCollection<RepositoryLocationItemViewModel> Locations { get; } = [];

        /// <summary>
        /// Gets or sets the repository path being edited locally.
        /// </summary>
        public string RepositoryPath
        {
            get => _repositoryPath;
            set
            {
                if (!SetProperty(ref _repositoryPath, value)) return;
                StatusText = string.Empty;
                _validateCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets the selected configured repository location.
        /// </summary>
        public RepositoryLocationItemViewModel? SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (!SetProperty(ref _selectedLocation, value) || value is null) return;
                RepositoryPath = value.RootPath;
            }
        }

        /// <summary>
        /// Gets the current localized operation status.
        /// </summary>
        public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }

        /// <summary>
        /// Gets the command that opens the folder-selection dialog.
        /// </summary>
        public DelegateCommand SelectFolderCommand => _selectFolderCommand ??= new DelegateCommand(ExecuteSelectFolder, () => !IsBusy);

        /// <summary>
        /// Gets the command that validates the locally selected repository path.
        /// </summary>
        public DelegateCommand ValidateCommand => _validateCommand ??= new DelegateCommand(ExecuteValidate, () => !IsBusy && !string.IsNullOrWhiteSpace(RepositoryPath));

        private void ExecuteSelectFolder()
        {
            var fileDialogService = _application.DialogService?.FileDialogService ?? throw new InvalidOperationException("The WPF file dialog service has not been initialized.");
            var selectedPath = fileDialogService.SelectFolder(Resources.RepositoryAdministration_FolderDialogTitle, string.IsNullOrWhiteSpace(RepositoryPath) ? null : RepositoryPath);
            if (!string.IsNullOrWhiteSpace(selectedPath)) RepositoryPath = selectedPath;
        }

        private async void ExecuteValidate()
        {
            try
            {
                IsBusy = true;
                StatusText = Resources.RepositoryAdministration_Validating;
                var result = await _validationService.ValidateAsync(RepositoryPath, _applicationStopping).ConfigureAwait(true);
                if (result.IsValid)
                {
                    RepositoryPath = result.NormalizedPath ?? RepositoryPath;
                    StatusText = Resources.RepositoryAdministration_Valid;
                    return;
                }
                StatusText = !result.Exists ? Resources.RepositoryAdministration_Missing : !result.IsReadable ? Resources.RepositoryAdministration_NotReadable : !result.IsWritable ? Resources.RepositoryAdministration_NotWritable : Resources.RepositoryAdministration_Invalid;
            }
            catch (OperationCanceledException) when (_applicationStopping.IsCancellationRequested) { StatusText = Resources.RepositoryAdministration_Canceled; }
            catch { StatusText = Resources.RepositoryAdministration_Invalid; }
            finally { IsBusy = false; }
        }

        private async Task LoadLocationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsBusy = true;
                StatusText = Resources.RepositoryAdministration_Loading;
                var summaries = await _administrationService.GetAllAsync(cancellationToken).ConfigureAwait(true);
                Locations.Clear();
                foreach (var item in summaries.Select(summary => new RepositoryLocationItemViewModel(summary.Name, summary.RootPath, summary.IsDefault, summary.IsEnabled))) Locations.Add(item);
                SelectedLocation = Locations.FirstOrDefault(location => location.IsDefault) ?? Locations.FirstOrDefault();
                StatusText = Locations.Count == 0 ? Resources.RepositoryAdministration_NoLocations : string.Empty;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.RepositoryAdministration_Canceled; }
            catch { StatusText = Resources.RepositoryAdministration_LoadFailed; }
            finally { IsBusy = false; }
        }
    }
}
