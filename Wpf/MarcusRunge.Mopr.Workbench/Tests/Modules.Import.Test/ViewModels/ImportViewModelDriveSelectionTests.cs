using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Import.Test.ViewModels;

public sealed class ImportViewModelDriveSelectionTests
{
    [Fact]
    public async Task OnDialogOpened_Should_Load_Drives_And_Update_Count()
    {
        var context = new ImportViewModelTestContext();
        context.SetDrives(new DicomImportDriveInfo(@"C:\", DriveType.Fixed, true), new DicomImportDriveInfo(@"Z:\", DriveType.Network, false));

        context.ViewModel.OnDialogOpened(new DialogParameters());
        await ImportViewModelTestContext.WaitUntilAsync(() => !context.ViewModel.IsRefreshingDrives, TestContext.Current.CancellationToken);

        Assert.Equal(2, context.ViewModel.AvailableDrives.Count);
        Assert.Contains("2", context.ViewModel.DriveSearchFoundCountText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SelectDriveCommand_Should_Select_Ready_Drive()
    {
        var context = new ImportViewModelTestContext();
        context.SetDrives(new DicomImportDriveInfo(@"E:\", DriveType.Removable, true));
        context.ViewModel.OnDialogOpened(new DialogParameters());
        await ImportViewModelTestContext.WaitUntilAsync(() => context.ViewModel.AvailableDrives.Count == 1, TestContext.Current.CancellationToken);
        var drive = Assert.Single(context.ViewModel.AvailableDrives);

        context.ViewModel.SelectDriveCommand.Execute(drive);

        Assert.True(context.ViewModel.Source.HasSelection);
        Assert.Equal(@"E:\", context.ViewModel.Source.Path);
    }

    [Fact]
    public async Task SelectDriveCommand_Should_Not_Select_Unready_Drive()
    {
        var context = new ImportViewModelTestContext();
        context.SetDrives(new DicomImportDriveInfo(@"Z:\", DriveType.Network, false));
        context.ViewModel.OnDialogOpened(new DialogParameters());
        await ImportViewModelTestContext.WaitUntilAsync(() => context.ViewModel.AvailableDrives.Count == 1, TestContext.Current.CancellationToken);
        var drive = Assert.Single(context.ViewModel.AvailableDrives);

        Assert.False(context.ViewModel.SelectDriveCommand.CanExecute(drive));
        Assert.False(context.ViewModel.Source.HasSelection);
    }

    [Fact]
    public void ImportDriveItemViewModel_Should_Map_Drive_State()
    {
        var drive = new Modules.Import.ViewModels.ImportDriveItemViewModel(new DicomImportDriveInfo(@"D:\", DriveType.CDRom, false));

        Assert.Equal(@"D:\", drive.RootPath);
        Assert.Equal(DriveType.CDRom, drive.DriveType);
        Assert.False(drive.IsReady);
        Assert.Contains(@"D:\", drive.DisplayName, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(drive.StatusText));
    }
}