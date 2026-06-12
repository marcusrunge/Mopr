using Mopr.Workbench.Modules.ModuleName.ViewModels;
using Mopr.Workbench.Services.Interfaces;
using Moq;
using Prism.Navigation.Regions;
using Xunit;

namespace Mopr.Workbench.Modules.ModuleName.Tests.ViewModels
{
    public class ViewAViewModelFixture
    {
        private readonly Mock<IMessageService> _messageServiceMock;
        private readonly Mock<IRegionManager> _regionManagerMock;
        private const string MessageServiceDefaultMessage = "Some Value";

        public ViewAViewModelFixture()
        {
            var messageService = new Mock<IMessageService>();
            messageService.Setup(x => x.GetMessage()).Returns(MessageServiceDefaultMessage);
            _messageServiceMock = messageService;

            _regionManagerMock = new Mock<IRegionManager>();
        }

        [Fact]
        public void MessagePropertyValueUpdated()
        {
            var vm = new ViewAViewModel(_regionManagerMock.Object, _messageServiceMock.Object);

            _messageServiceMock.Verify(x => x.GetMessage(), Times.Once);

            Assert.Equal(MessageServiceDefaultMessage, vm.Message);
        }

        [Fact]
        public void MessageINotifyPropertyChangedCalled()
        {
            var vm = new ViewAViewModel(_regionManagerMock.Object, _messageServiceMock.Object);
            Assert.PropertyChanged(vm, nameof(vm.Message), () => vm.Message = "Changed");
        }
    }
}
