using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Import.Test.ViewModels;

public sealed class ImportViewModelNetworkPathTests
{
    [Fact]
    public async Task CheckNetworkPathCommand_Should_Select_Available_Network_Source()
    {
        var context = new ImportViewModelTestContext();
        const string path = @"\\localhost\Verteiler";

        context.ResolveSource(new DicomImportSourceResolution
        {
            EffectiveSourcePath = path,
            Exists = true,
            IsReady = true,
            IsRepositoryImportSupported = true,
            OriginalSourcePath = path,
            SourceType = DicomImportSourceType.NetworkShare,
            TreatAsReadOnly = true
        });

        context.ViewModel.NetworkPath = path;

        context.ViewModel.CheckNetworkPathCommand.Execute();

        await ImportViewModelTestContext.WaitUntilAsync(() => !context.ViewModel.IsCheckingNetworkPath, TestContext.Current.CancellationToken);

        Assert.True(context.ViewModel.Source.HasSelection);
        Assert.Equal(path, context.ViewModel.Source.Path);
        Assert.Equal(DicomImportSourceType.AutoDetect, context.ViewModel.Source.SourceType);
        Assert.False(string.IsNullOrWhiteSpace(context.ViewModel.NetworkPathStatusText));

        context.ImportService.Verify(service => service.ResolveSourceAsync(It.Is<DicomImportRequest>(request => request.SourcePath == path && request.SourceType == DicomImportSourceType.NetworkShare), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckNetworkPathCommand_Should_Not_Select_Unavailable_Source()
    {
        var context = new ImportViewModelTestContext();
        const string path = @"\\localhost\NichtVorhanden";

        context.ResolveSource(new DicomImportSourceResolution
        {
            EffectiveSourcePath = path,
            Exists = false,
            IsReady = false,
            IsRepositoryImportSupported = false,
            OriginalSourcePath = path,
            SourceType = DicomImportSourceType.NetworkShare,
            TreatAsReadOnly = true
        });

        context.ViewModel.NetworkPath = path;

        context.ViewModel.CheckNetworkPathCommand.Execute();

        await ImportViewModelTestContext.WaitUntilAsync(() => !context.ViewModel.IsCheckingNetworkPath, TestContext.Current.CancellationToken);

        Assert.False(context.ViewModel.Source.HasSelection);
        Assert.False(string.IsNullOrWhiteSpace(context.ViewModel.NetworkPathStatusText));

        context.ImportService.Verify(service => service.ResolveSourceAsync(It.Is<DicomImportRequest>(request => request.SourcePath == path && request.SourceType == DicomImportSourceType.NetworkShare), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ChangingNetworkPath_Should_Clear_Previous_Status()
    {
        var context = new ImportViewModelTestContext();

        context.ViewModel.NetworkPath = @"\\localhost\A";
        context.ViewModel.NetworkPath = @"\\localhost\B";

        Assert.Equal(string.Empty, context.ViewModel.NetworkPathStatusText);
        Assert.True(context.ViewModel.CheckNetworkPathCommand.CanExecute());
    }
}