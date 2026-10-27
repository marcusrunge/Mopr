using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;
using Moq;
using System.Windows;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.Test.ViewModels
{
    public sealed class AdministrationOverviewViewModelTests
    {
        [Fact]
        public void AdministrationSectionRequestedEvent_WithSystemCheck_SelectsSystemCheck()
        {
            var eventAggregator = new EventAggregator();
            var viewModel = new AdministrationOverviewViewModel(new Mock<IRegionManager>(MockBehavior.Strict).Object, eventAggregator);

            eventAggregator.GetEvent<AdministrationSectionRequestedEvent>().Publish(SetupNavigation.AdministrationSystemCheckSection);

            Assert.True(viewModel.IsSystemCheckSelected);
            Assert.Equal(Visibility.Visible, viewModel.SystemCheckVisibility);
            Assert.False(viewModel.IsOverviewSelected);
            Assert.Equal(Visibility.Collapsed, viewModel.OverviewVisibility);
            Assert.Equal(Visibility.Collapsed, viewModel.DatabaseVisibility);
            Assert.Equal(Visibility.Collapsed, viewModel.RepositoryVisibility);
            Assert.Equal(Visibility.Collapsed, viewModel.SecurityVisibility);
            Assert.Equal(Visibility.Collapsed, viewModel.UsersVisibility);
        }

        [Fact]
        public void AdministrationSectionRequestedEvent_WithUnknownSection_PreservesOverview()
        {
            var eventAggregator = new EventAggregator();
            var viewModel = new AdministrationOverviewViewModel(new Mock<IRegionManager>(MockBehavior.Strict).Object, eventAggregator);

            eventAggregator.GetEvent<AdministrationSectionRequestedEvent>().Publish("UnknownSection");

            Assert.True(viewModel.IsOverviewSelected);
            Assert.Equal(Visibility.Visible, viewModel.OverviewVisibility);
            Assert.False(viewModel.IsSystemCheckSelected);
            Assert.Equal(Visibility.Collapsed, viewModel.SystemCheckVisibility);
        }
    }
}
