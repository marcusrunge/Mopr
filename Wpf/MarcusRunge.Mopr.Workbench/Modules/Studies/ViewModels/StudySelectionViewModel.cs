using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;
using Prism.Commands;
using Prism.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Studies.ViewModels
{
    /// <summary>
    /// Loads imported studies and returns one safely selectable series.
    /// </summary>
    public sealed class StudySelectionViewModel : ViewModelBase, IDialogAware
    {
        public const string SelectedSeriesIdParameterName = "SelectedSeriesId";
        public const string SelectedSeriesInstanceUidParameterName = "SelectedSeriesInstanceUid";
        private readonly IStudyCatalogService _studyCatalogService;
        private CancellationTokenSource? _loadCancellation;
        private bool _isLoading;
        private StudyItemViewModel? _selectedStudy;

        public StudySelectionViewModel(IApplication application)
        {
            ArgumentNullException.ThrowIfNull(application);
            _studyCatalogService = application.StudyCatalog ?? throw new InvalidOperationException("The study catalog service has not been initialized.");
            CancelCommand = new DelegateCommand(Cancel, CanCancel);
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
            Studies = [];
        }

        public DelegateCommand CancelCommand { get; }
        public DelegateCommand ConfirmCommand { get; }
        public bool HasStudies => Studies.Count > 0;

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (!SetProperty(ref _isLoading, value)) return;
                RaiseCommandState();
            }
        }

        /// <inheritdoc/>
        public DialogCloseListener RequestClose { get; }

        public StudyItemViewModel? SelectedStudy
        {
            get => _selectedStudy;
            set
            {
                _selectedStudy?.PropertyChanged -= OnSelectedStudyPropertyChanged;
                if (!SetProperty(ref _selectedStudy, value)) return;
                _selectedStudy?.PropertyChanged += OnSelectedStudyPropertyChanged;
                RaiseCommandState();
            }
        }

        public ObservableCollection<StudyItemViewModel> Studies { get; }
        public static string Title => Properties.Resources.StudySelectionTitle;

        /// <inheritdoc/>
        public bool CanCloseDialog() => !IsLoading;

        /// <inheritdoc/>
        public override void Destroy()
        {
            ReleaseLoadCancellation(cancel: true);
            base.Destroy();
        }

        /// <inheritdoc/>
        public void OnDialogClosed() => ReleaseLoadCancellation(cancel: true);

        /// <inheritdoc/>
        public async void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                await LoadAsync();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void Cancel()
        {
            if (!CanCancel()) return;
            RequestClose.Invoke(ButtonResult.Cancel);
        }

        private bool CanCancel() => !IsLoading;
        private bool CanConfirm() => !IsLoading && SelectedStudy?.SelectedSeries?.IsAvailable == true;

        private void Confirm()
        {
            if (!CanConfirm()) return;
            var selectedSeries = SelectedStudy!.SelectedSeries!;
            var parameters = new DialogParameters
            {
                { SelectedSeriesIdParameterName, selectedSeries.SeriesId },
                { SelectedSeriesInstanceUidParameterName, selectedSeries.SeriesInstanceUid }
            };
            RequestClose.Invoke(parameters, ButtonResult.OK);
        }

        private async Task LoadAsync()
        {
            ReleaseLoadCancellation(cancel: true);
            _loadCancellation = new CancellationTokenSource();
            IsLoading = true;
            SelectedStudy = null;
            Studies.Clear();
            RaisePropertyChanged(nameof(HasStudies));

            try
            {
                var studies = await _studyCatalogService.GetStudiesAsync(_loadCancellation.Token);
                foreach (var study in studies) Studies.Add(new StudyItemViewModel(study));
                SelectedStudy = Studies.FirstOrDefault();
                RaisePropertyChanged(nameof(HasStudies));
            }
            finally
            {
                IsLoading = false;
                ReleaseLoadCancellation(cancel: false);
            }
        }

        private void OnSelectedStudyPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StudyItemViewModel.SelectedSeries)) RaiseCommandState();
        }

        private void RaiseCommandState()
        {
            CancelCommand.RaiseCanExecuteChanged();
            ConfirmCommand.RaiseCanExecuteChanged();
        }

        private void ReleaseLoadCancellation(bool cancel)
        {
            var cancellation = _loadCancellation;
            _loadCancellation = null;
            if (cancellation is null) return;
            if (cancel) cancellation.Cancel();
            cancellation.Dispose();
        }
    }
}