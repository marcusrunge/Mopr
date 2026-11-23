using MarcusRunge.Mopr.Workbench.Modules.Import.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;
using Moq;
using System.Runtime.CompilerServices;
using IDialogService = MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog.IDialogService;

namespace MarcusRunge.Mopr.Workbench.Modules.Import.Test.ViewModels;

internal sealed class ImportViewModelTestContext
{
    private IReadOnlyList<DicomImportDriveInfo> _drives = [];

    public ImportViewModelTestContext()
    {
        Application.SetupGet(application => application.DialogService).Returns(DialogService.Object);
        Application.SetupGet(application => application.ImportService).Returns(ImportService.Object);
        DialogService.SetupGet(service => service.FileDialogService).Returns(FileDialogService.Object);
        ImportService.SetupGet(service => service.DicomImportDriveProvider).Returns(DriveProvider.Object);
        ImportService.SetupGet(service => service.DicomImportService).Returns(DicomImportService.Object);
        DriveProvider.Setup(provider => provider.GetDrivesAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken cancellationToken) => EnumerateDrivesAsync(_drives, cancellationToken));
        ResolveSource(new DicomImportSourceResolution { SourceType = DicomImportSourceType.NetworkShare });
        ViewModel = new ImportViewModel(Application.Object);
    }

    public Mock<IApplication> Application { get; } = new(MockBehavior.Strict);
    public Mock<IDialogService> DialogService { get; } = new(MockBehavior.Strict);
    public Mock<IDicomImportDriveProvider> DriveProvider { get; } = new(MockBehavior.Strict);
    public Mock<IDicomImportService> DicomImportService { get; } = new(MockBehavior.Strict);
    public Mock<IFileDialogService> FileDialogService { get; } = new(MockBehavior.Strict);
    public Mock<IImportService> ImportService { get; } = new(MockBehavior.Strict);
    public ImportViewModel ViewModel { get; }

    public void ResolveSource(DicomImportSourceResolution resolution) => ImportService.Setup(service => service.ResolveSourceAsync(It.IsAny<DicomImportRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(resolution);

    public void SelectFolder(string? path) => FileDialogService.Setup(service => service.SelectFolder(It.IsAny<string>(), It.IsAny<string?>())).Returns(path);

    public void SetDrives(params DicomImportDriveInfo[] drives) => _drives = drives;

    public static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(5);
        var startedAt = DateTime.UtcNow;
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTime.UtcNow - startedAt >= timeout) throw new TimeoutException("The expected view-model state was not reached within five seconds.");
            await Task.Delay(10, cancellationToken);
        }
    }

    private static async IAsyncEnumerable<DicomImportDriveInfo> EnumerateDrivesAsync(IEnumerable<DicomImportDriveInfo> drives, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var drive in drives)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return drive;
        }
    }
}