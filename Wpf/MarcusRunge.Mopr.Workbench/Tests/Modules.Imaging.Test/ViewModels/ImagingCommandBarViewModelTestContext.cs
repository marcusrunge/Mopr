using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Moq;
using PrismDialogService = Prism.Dialogs.IDialogService;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    internal sealed class ImagingCommandBarViewModelTestContext
    {
        public ImagingCommandBarViewModelTestContext()
        {
            Application.SetupGet(value => value.StudiesService).Returns(StudiesService.Object);
            Core.SetupGet(value => value.ImagingService).Returns(ImagingService.Object);
            ImagingService.SetupGet(value => value.ImagingLayoutService).Returns(ImagingLayoutService.Object);
            ImagingService.SetupGet(value => value.ImagingStudyService).Returns(ImagingStudyService.Object);
            ImagingService.SetupGet(value => value.ImagingToolService).Returns(ImagingToolService.Object);
            ImagingService.SetupGet(value => value.ImagingViewportService).Returns(ImagingViewportService.Object);
            ImagingService.SetupGet(value => value.ImagingWindowLevelService).Returns(ImagingWindowLevelService.Object);
            ImagingLayoutService.SetupGet(value => value.CurrentLayout).Returns(ImagingLayout.Single);
            ImagingToolService.SetupGet(value => value.ActiveTool).Returns(ImagingTool.None);
            StudiesService.SetupGet(value => value.ImportedSeriesLoader).Returns(ImportedSeriesLoader.Object);
            ViewModel = new ImagingCommandBarViewModel(Core.Object, Application.Object, DialogService.Object);
        }

        public Mock<IApplication> Application { get; } = new();
        public Mock<ICore> Core { get; } = new();
        public Mock<PrismDialogService> DialogService { get; } = new();
        public Mock<IImagingLayoutService> ImagingLayoutService { get; } = new();
        public Mock<IImagingService> ImagingService { get; } = new();
        public Mock<IImagingStudyService> ImagingStudyService { get; } = new();
        public Mock<IImagingToolService> ImagingToolService { get; } = new();
        public Mock<IImagingViewportService> ImagingViewportService { get; } = new();
        public Mock<IImagingWindowLevelService> ImagingWindowLevelService { get; } = new();
        public Mock<IImportedSeriesLoadService> ImportedSeriesLoader { get; } = new();
        public Mock<IStudiesService> StudiesService { get; } = new();
        public ImagingCommandBarViewModel ViewModel { get; }

        public static Mock<IDialogResult> CreateDialogResult(IDialogParameters parameters, ButtonResult result = ButtonResult.OK)
        {
            var dialogResult = new Mock<IDialogResult>();
            dialogResult.SetupGet(value => value.Parameters).Returns(parameters);
            dialogResult.SetupGet(value => value.Result).Returns(result);
            return dialogResult;
        }
    }
}