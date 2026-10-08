using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;
using System.Windows;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels
{
    /// <summary>
    /// Provides local section selection and navigation for the administrative Setup overview.
    /// </summary>
    public sealed class AdministrationOverviewViewModel : BindableBase
    {
        private const string DatabaseSection = "Database";
        private const string OverviewSection = "Overview";
        private const string RepositorySection = "Repository";
        private const string SecuritySection = "Security";
        private const string SystemCheckSection = "SystemCheck";
        private const string UsersSection = "Users";
        private readonly IRegionManager _regionManager;
        private DelegateCommand? _returnToImagingCommand;
        private DelegateCommand<string>? _selectSectionCommand;
        private string _selectedSection = OverviewSection;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdministrationOverviewViewModel"/> class.
        /// </summary>
        /// <param name="regionManager">The application region manager.</param>
        public AdministrationOverviewViewModel(IRegionManager regionManager) => _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

        public Visibility DatabaseVisibility => GetVisibility(DatabaseSection);
        public bool IsDatabaseSelected => IsSelected(DatabaseSection);
        public bool IsOverviewSelected => IsSelected(OverviewSection);
        public bool IsRepositorySelected => IsSelected(RepositorySection);
        public bool IsSecuritySelected => IsSelected(SecuritySection);
        public bool IsSystemCheckSelected => IsSelected(SystemCheckSection);
        public bool IsUsersSelected => IsSelected(UsersSection);
        public Visibility OverviewVisibility => GetVisibility(OverviewSection);
        public Visibility RepositoryVisibility => GetVisibility(RepositorySection);
        public DelegateCommand ReturnToImagingCommand => _returnToImagingCommand ??= new DelegateCommand(ExecuteReturnToImaging);
        public string ReturnToImagingText => Resources.Administration_ReturnToImaging;
        public Visibility SecurityVisibility => GetVisibility(SecuritySection);
        public DelegateCommand<string> SelectSectionCommand => _selectSectionCommand ??= new DelegateCommand<string>(ExecuteSelectSection, CanSelectSection);
        public Visibility SystemCheckVisibility => GetVisibility(SystemCheckSection);
        public Visibility UsersVisibility => GetVisibility(UsersSection);

        private static bool CanSelectSection(string? section) => section is OverviewSection or DatabaseSection or RepositorySection or SecuritySection or UsersSection or SystemCheckSection;
        private void ExecuteReturnToImaging() => _regionManager.RequestNavigate(RegionNames.ContentRegion, NavigationNames.Imaging);
        private void ExecuteSelectSection(string? section)
        {
            if (!CanSelectSection(section) || string.Equals(_selectedSection, section, StringComparison.Ordinal))
            {
                return;
            }

            _selectedSection = section!;
            RaiseSectionProperties();
        }
        private Visibility GetVisibility(string section) => IsSelected(section) ? Visibility.Visible : Visibility.Collapsed;
        private bool IsSelected(string section) => string.Equals(_selectedSection, section, StringComparison.Ordinal);
        private void RaiseSectionProperties()
        {
            RaisePropertyChanged(nameof(DatabaseVisibility));
            RaisePropertyChanged(nameof(IsDatabaseSelected));
            RaisePropertyChanged(nameof(IsOverviewSelected));
            RaisePropertyChanged(nameof(IsRepositorySelected));
            RaisePropertyChanged(nameof(IsSecuritySelected));
            RaisePropertyChanged(nameof(IsSystemCheckSelected));
            RaisePropertyChanged(nameof(IsUsersSelected));
            RaisePropertyChanged(nameof(OverviewVisibility));
            RaisePropertyChanged(nameof(RepositoryVisibility));
            RaisePropertyChanged(nameof(SecurityVisibility));
            RaisePropertyChanged(nameof(SystemCheckVisibility));
            RaisePropertyChanged(nameof(UsersVisibility));
        }
    }
}
