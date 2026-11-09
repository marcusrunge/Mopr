using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using Prism.Commands;
using Prism.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Studies.ViewModels
{
    /// <summary>
    /// Loads imported studies and returns one or more safely selectable series.
    /// </summary>
    public sealed class StudySelectionViewModel : ViewModelBase, IDialogAware
    {
        public const string SelectedSeriesIdParameterName = "SelectedSeriesId";
        public const string SelectedSeriesInstanceUidParameterName = "SelectedSeriesInstanceUid";
        public const string SelectedSeriesRequestsParameterName = "SelectedSeriesRequests";
        private readonly IStudyCatalogService _studyCatalogService;
        private CancellationTokenSource? _loadCancellation;
        private string _errorMessage = string.Empty;
        private bool _hasLoadError;
        private bool _isLoading;
        private StudyItemViewModel? _selectedStudy;

        public StudySelectionViewModel(IApplication application)
        {
            _studyCatalogService = application?.StudiesService?.StudyCatalog ?? throw new InvalidOperationException("The study catalog service has not been initialized.");
            CancelCommand = new DelegateCommand(Cancel, CanCancel);
            ClearSelectionCommand = new DelegateCommand(ClearSelection, CanChangeSelection);
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
            RetryCommand = new DelegateCommand(ExecuteLoad, CanRetry);
            SelectAllCommand = new DelegateCommand(SelectAll, CanChangeSelection);
            Studies = [];
        }

        public DelegateCommand CancelCommand { get; }
        public DelegateCommand ClearSelectionCommand { get; }
        public DelegateCommand ConfirmCommand { get; }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public bool HasLoadError
        {
            get => _hasLoadError;
            private set
            {
                if (!SetProperty(ref _hasLoadError, value)) return;
                RaisePropertyChanged(nameof(ShowEmptyState));
            }
        }

        public bool HasStudies => Studies.Count > 0;

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (!SetProperty(ref _isLoading, value)) return;
                RaisePropertyChanged(nameof(ShowEmptyState));
                RaiseCommandState();
            }
        }

        public DialogCloseListener RequestClose { get; }
        public DelegateCommand RetryCommand { get; }
        public DelegateCommand SelectAllCommand { get; }
        public bool ShowEmptyState => !IsLoading && !HasLoadError && !HasStudies;

        public StudyItemViewModel? SelectedStudy
        {
            get => _selectedStudy;
            set
            {
                _selectedStudy?.PropertyChanged -= OnSelectedStudyPropertyChanged;
                if (!SetProperty(ref _selectedStudy, value)) return;
                _selectedStudy?.PropertyChanged += OnSelectedStudyPropertyChanged;
                RaisePropertyChanged(nameof(SelectionSummary));
                RaiseCommandState();
            }
        }

        public string SelectionSummary => SelectedStudy?.SelectionSummary ?? string.Format(Properties.Resources.SeriesSelectionSummaryFormat, 0, 0);
        public ObservableCollection<StudyItemViewModel> Studies { get; }
        public static string Title => Properties.Resources.StudySelectionTitle;

        public bool CanCloseDialog() => !IsLoading;

        public override void Destroy()
        {
            ReleaseLoadCancellation(cancel: true);
            base.Destroy();
        }

        public void OnDialogClosed() => ReleaseLoadCancellation(cancel: true);

        public void OnDialogOpened(IDialogParameters parameters) => ExecuteLoad();

        private void Cancel()
        {
            if (!CanCancel()) return;
            RequestClose.Invoke(ButtonResult.Cancel);
        }

        private bool CanCancel() => !IsLoading;
        private bool CanChangeSelection() => !IsLoading && SelectedStudy is not null && SelectedStudy.AvailableSeriesCount > 0;
        private bool CanConfirm() => !IsLoading && SelectedStudy?.HasSelectedSeries == true;
        private bool CanRetry() => !IsLoading && HasLoadError;

        private void ClearSelection()
        {
            if (!CanChangeSelection()) return;
            SelectedStudy!.ClearSelection();
        }

        private void Confirm()
        {
            if (!CanConfirm()) return;

            ImportedSeriesLoadRequest[] requests = [.. SelectedStudy!.Series.Where(series => series.IsSelected).Select(series => new ImportedSeriesLoadRequest(series.SeriesId, series.SeriesInstanceUid))];
            var parameters = new DialogParameters
            {
                { SelectedSeriesRequestsParameterName, requests },
                /*
                 * The legacy single-series parameters remain available until every dialog
                 * consumer has moved to the atomic multi-series contract.
                 */
                { SelectedSeriesIdParameterName, requests[0].SeriesId },
                { SelectedSeriesInstanceUidParameterName, requests[0].SeriesInstanceUid }
            };
            RequestClose.Invoke(parameters, ButtonResult.OK);
        }

        private async void ExecuteLoad()
        {
            try
            {
                await LoadAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                HasLoadError = true;
                ErrorMessage = Properties.Resources.StudyCatalogLoadError;
                System.Diagnostics.Debug.WriteLine(exception);
            }
            finally
            {
                RaiseCommandState();
            }
        }

        private async Task LoadAsync()
        {
            ReleaseLoadCancellation(cancel: true);
            _loadCancellation = new CancellationTokenSource();
            HasLoadError = false;
            ErrorMessage = string.Empty;
            IsLoading = true;
            SelectedStudy = null;
            Studies.Clear();
            RaisePropertyChanged(nameof(HasStudies));
            RaisePropertyChanged(nameof(ShowEmptyState));

            try
            {
                var studies = await _studyCatalogService.GetStudiesAsync(_loadCancellation.Token);
                foreach (var study in studies) Studies.Add(new StudyItemViewModel(study));
                SelectedStudy = Studies.FirstOrDefault();
                RaisePropertyChanged(nameof(HasStudies));
                RaisePropertyChanged(nameof(ShowEmptyState));
            }
            finally
            {
                IsLoading = false;
                ReleaseLoadCancellation(cancel: false);
            }
        }

        private void OnSelectedStudyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not (nameof(StudyItemViewModel.HasSelectedSeries) or nameof(StudyItemViewModel.SelectedSeriesCount) or nameof(StudyItemViewModel.SelectionSummary))) return;
            RaisePropertyChanged(nameof(SelectionSummary));
            RaiseCommandState();
        }

        private void RaiseCommandState()
        {
            CancelCommand.RaiseCanExecuteChanged();
            ClearSelectionCommand.RaiseCanExecuteChanged();
            ConfirmCommand.RaiseCanExecuteChanged();
            RetryCommand.RaiseCanExecuteChanged();
            SelectAllCommand.RaiseCanExecuteChanged();
        }

        private void ReleaseLoadCancellation(bool cancel)
        {
            var cancellation = _loadCancellation;
            _loadCancellation = null;
            if (cancellation is null) return;
            if (cancel) cancellation.Cancel();
            cancellation.Dispose();
        }

        private void SelectAll()
        {
            if (!CanChangeSelection()) return;
            SelectedStudy!.SelectAllAvailable();
        }
    }
}