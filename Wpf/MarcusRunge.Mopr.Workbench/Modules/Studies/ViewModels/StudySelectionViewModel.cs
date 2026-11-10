using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using Prism.Commands;
using Prism.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Studies.ViewModels
{
    /// <summary>
    /// Loads imported studies, filters the visible study list and returns one or more safely selectable series.
    /// </summary>
    public sealed class StudySelectionViewModel : ViewModelBase, IDialogAware
    {
        public const string SelectedSeriesIdParameterName = "SelectedSeriesId";
        public const string SelectedSeriesInstanceUidParameterName = "SelectedSeriesInstanceUid";
        public const string SelectedSeriesRequestsParameterName = "SelectedSeriesRequests";
        private readonly List<StudyItemViewModel> _allStudies = [];
        private readonly IStudyCatalogService _studyCatalogService;
        private CancellationTokenSource? _loadCancellation;
        private string _errorMessage = string.Empty;
        private bool _hasLoadError;
        private bool _isLoading;
        private string _searchText = string.Empty;
        private StudyItemViewModel? _selectedStudy;

        public StudySelectionViewModel(IApplication application)
        {
            _studyCatalogService = application?.StudiesService?.StudyCatalog ?? throw new InvalidOperationException("The study catalog service has not been initialized.");
            CancelCommand = new DelegateCommand(Cancel, CanCancel);
            ClearSearchCommand = new DelegateCommand(ClearSearch, CanClearSearch);
            ClearSelectionCommand = new DelegateCommand(ClearSelection, CanChangeSelection);
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
            RetryCommand = new DelegateCommand(ExecuteLoad, CanRetry);
            SelectAllCommand = new DelegateCommand(SelectAll, CanChangeSelection);
            Studies = [];
        }

        public DelegateCommand CancelCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }
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

        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);
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

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetProperty(ref _searchText, value ?? string.Empty)) return;
                RaisePropertyChanged(nameof(HasSearchText));
                ApplyFilter();
                ClearSearchCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand SelectAllCommand { get; }
        public bool ShowEmptyState => !IsLoading && !HasLoadError && _allStudies.Count == 0;

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
        private bool CanClearSearch() => !IsLoading && HasSearchText;
        private bool CanConfirm() => !IsLoading && SelectedStudy?.HasSelectedSeries == true;
        private bool CanRetry() => !IsLoading && HasLoadError;

        private void ClearSearch()
        {
            if (!CanClearSearch()) return;
            SearchText = string.Empty;
        }

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
            _allStudies.Clear();
            Studies.Clear();
            RaiseFilterState();

            try
            {
                var studies = await _studyCatalogService.GetStudiesAsync(_loadCancellation.Token);
                foreach (var study in studies) _allStudies.Add(new StudyItemViewModel(study));
                ApplyFilter();
            }
            finally
            {
                IsLoading = false;
                ReleaseLoadCancellation(cancel: false);
            }
        }

        private void ApplyFilter()
        {
            var selectedStudyId = SelectedStudy?.StudyId;
            string[] searchTerms = SearchText.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            IEnumerable<StudyItemViewModel> filteredStudies = _allStudies;
            if (searchTerms.Length > 0) filteredStudies = filteredStudies.Where(study => searchTerms.All(term => Matches(study, term)));

            Studies.Clear();
            foreach (var study in filteredStudies) Studies.Add(study);
            SelectedStudy = selectedStudyId.HasValue ? Studies.FirstOrDefault(study => study.StudyId == selectedStudyId.Value) ?? Studies.FirstOrDefault() : Studies.FirstOrDefault();
            RaiseFilterState();
        }

        private static bool Contains(string? value, string term) => !string.IsNullOrWhiteSpace(value) && value.Contains(term, StringComparison.CurrentCultureIgnoreCase);
        private static bool Matches(StudyItemViewModel study, string term) => Contains(study.PatientName, term) || Contains(study.PatientId, term) || Contains(study.StudyDescription, term) || Contains(study.AccessionNumber, term) || Contains(study.StudyDateText, term) || study.Series.Any(series => Contains(series.Description, term) || Contains(series.Modality, term));

        private void OnSelectedStudyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not (nameof(StudyItemViewModel.HasSelectedSeries) or nameof(StudyItemViewModel.SelectedSeriesCount) or nameof(StudyItemViewModel.SelectionSummary))) return;
            RaisePropertyChanged(nameof(SelectionSummary));
            RaiseCommandState();
        }

        private void RaiseFilterState()
        {
            RaisePropertyChanged(nameof(HasStudies));
            RaisePropertyChanged(nameof(ShowEmptyState));
            RaiseCommandState();
        }

        private void RaiseCommandState()
        {
            CancelCommand.RaiseCanExecuteChanged();
            ClearSearchCommand.RaiseCanExecuteChanged();
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