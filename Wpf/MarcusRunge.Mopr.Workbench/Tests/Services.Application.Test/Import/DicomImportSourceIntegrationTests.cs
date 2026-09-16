using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using Moq;
using RepositoryImportRequest = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportRequest;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.Import
{
    public sealed class DicomImportSourceIntegrationTests
    {
        [Theory]
        [InlineData(DicomImportSourceType.LocalDirectory, ImportSourceType.Directory)]
        [InlineData(DicomImportSourceType.UsbDrive, ImportSourceType.UsbDrive)]
        [InlineData(DicomImportSourceType.RemovableDrive, ImportSourceType.UsbDrive)]
        [InlineData(DicomImportSourceType.ExternalDrive, ImportSourceType.Directory)]
        [InlineData(DicomImportSourceType.SdCard, ImportSourceType.UsbDrive)]
        [InlineData(DicomImportSourceType.CdRom, ImportSourceType.CdRom)]
        [InlineData(DicomImportSourceType.Dvd, ImportSourceType.Dvd)]
        [InlineData(DicomImportSourceType.NetworkShare, ImportSourceType.NetworkShare)]
        [InlineData(DicomImportSourceType.MappedNetworkDrive, ImportSourceType.NetworkShare)]
        [InlineData(DicomImportSourceType.VirtualDrive, ImportSourceType.Directory)]
        public async Task ImportAsync_WhenDirectoryBasedSourceIsResolved_MapsExpectedRepositorySourceType(DicomImportSourceType sourceType, ImportSourceType expectedRepositorySourceType)
        {
            using var context = new DicomImportServiceTestContext();
            var request = new DicomImportRequest(context.SourceDirectoryPath, sourceType);

            var result = await context.Service.ImportAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.Completed, result.Status);
            context.RepositoryImportService.Verify(service => service.ImportAsync(
                It.Is<RepositoryImportRequest>(repositoryRequest =>
                    repositoryRequest.SourcePath == context.SourceDirectoryPath &&
                    repositoryRequest.SourceType == expectedRepositorySourceType),
                TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenSourceTypeIsAutoDetect_UsesResolvedSourceType()
        {
            using var context = new DicomImportServiceTestContext();

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.Completed, result.Status);
            context.RepositoryImportService.Verify(service => service.ImportAsync(
                It.Is<RepositoryImportRequest>(request => request.SourceType == ImportSourceType.Directory),
                TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenResolverNormalizesSelectedPath_ForwardsEffectiveSourcePath()
        {
            using var context = new DicomImportServiceTestContext();
            var selectedPath = $"  {context.SourceDirectoryPath}  ";
            var request = new DicomImportRequest(selectedPath, DicomImportSourceType.LocalDirectory);

            var result = await context.Service.ImportAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.Completed, result.Status);
            context.RepositoryImportService.Verify(service => service.ImportAsync(
                It.Is<RepositoryImportRequest>(repositoryRequest => repositoryRequest.SourcePath == context.SourceDirectoryPath),
                TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenIsoImageExists_StopsBeforeApplicationPrerequisites()
        {
            using var context = new DicomImportServiceTestContext();
            var isoPath = Path.Combine(context.SourceDirectoryPath, "Study.iso");
            await File.WriteAllBytesAsync(isoPath, [], TestContext.Current.CancellationToken);

            var result = await context.Service.ImportAsync(new DicomImportRequest(isoPath, DicomImportSourceType.IsoImage), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.SourceUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyNoPrerequisiteServicesCalled();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenSourceTypeIsUnknown_StopsBeforeApplicationPrerequisites()
        {
            using var context = new DicomImportServiceTestContext();

            var result = await context.Service.ImportAsync(new DicomImportRequest(context.SourceDirectoryPath, DicomImportSourceType.Unknown), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.SourceUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyNoPrerequisiteServicesCalled();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenSourceResolutionIsCanceled_ReturnsCanceledWithoutCallingApplicationPrerequisites()
        {
            using var context = new DicomImportServiceTestContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cancellationSource.Cancel();

            var result = await context.Service.ImportAsync(context.CreateRequest(), cancellationSource.Token);

            Assert.Equal(DicomImportStatus.Canceled, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyNoPrerequisiteServicesCalled();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenSeparateApplicationGraphsImportDifferentSources_UsesGraphOwnedServices()
        {
            using var firstContext = new DicomImportServiceTestContext();
            using var secondContext = new DicomImportServiceTestContext();
            firstContext.Reset(auditUserId: 71, repositoryLocationId: 81);
            secondContext.Reset(auditUserId: 72, repositoryLocationId: 82);

            await Task.WhenAll(
                firstContext.Service.ImportAsync(new DicomImportRequest(firstContext.SourceDirectoryPath, DicomImportSourceType.UsbDrive), TestContext.Current.CancellationToken),
                secondContext.Service.ImportAsync(new DicomImportRequest(secondContext.SourceDirectoryPath, DicomImportSourceType.NetworkShare), TestContext.Current.CancellationToken));

            firstContext.RepositoryImportService.Verify(service => service.ImportAsync(
                It.Is<RepositoryImportRequest>(request => request.CreatedByUserId == 71 && request.RepositoryLocationId == 81 && request.SourceType == ImportSourceType.UsbDrive),
                TestContext.Current.CancellationToken), Times.Once);
            secondContext.RepositoryImportService.Verify(service => service.ImportAsync(
                It.Is<RepositoryImportRequest>(request => request.CreatedByUserId == 72 && request.RepositoryLocationId == 82 && request.SourceType == ImportSourceType.NetworkShare),
                TestContext.Current.CancellationToken), Times.Once);
        }
    }
}
