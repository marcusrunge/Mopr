using Moq;
using Prism.Navigation.Regions;

namespace MarcusRunge.Mopr.Workbench.Modules.ModuleName.Tests.ViewModels
{
    public class ImagingWorkbenchViewModelFixture
    {
        private readonly Mock<IRegionManager> _regionManagerMock;

        public ImagingWorkbenchViewModelFixture()
        {
            _regionManagerMock = new Mock<IRegionManager>();
        }
    }
}