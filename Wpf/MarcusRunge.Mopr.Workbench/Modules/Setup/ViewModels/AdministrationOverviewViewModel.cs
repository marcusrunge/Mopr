using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels
{
    /// <summary>
    /// Provides navigation commands for the administrative Setup overview.
    /// </summary>
    public sealed class AdministrationOverviewViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private DelegateCommand? _returnToImagingCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdministrationOverviewViewModel"/> class.
        /// </summary>
        /// <param name="regionManager">The application region manager.</param>
        public AdministrationOverviewViewModel(IRegionManager regionManager) => _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

        /// <summary>
        /// Gets the command that returns to the imaging workbench.
        /// </summary>
        public DelegateCommand ReturnToImagingCommand => _returnToImagingCommand ??= new DelegateCommand(ExecuteReturnToImaging);

        /// <summary>
        /// Gets the localized return-navigation text.
        /// </summary>
        public string ReturnToImagingText => Resources.Administration_ReturnToImaging;

        private void ExecuteReturnToImaging() => _regionManager.RequestNavigate(RegionNames.ContentRegion, NavigationNames.Imaging);
    }
}
