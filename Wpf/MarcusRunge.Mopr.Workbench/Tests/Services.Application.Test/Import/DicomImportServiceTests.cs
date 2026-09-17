using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using Moq;
using RepositoryImportFileInfo = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportFileInfo;
using RepositoryImportRequest = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportRequest;
using RepositoryImportResult = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportResult;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.Import
{
    public sealed class DicomImportServiceTests
    {

        [Fact]
        public async Task ImportAsync_WhenImportCompletesSuccessfully_ReturnsCompletedResult()
        {
            using var context = new DicomImportServiceTestContext();

            context.SetImportResult(new RepositoryImportResult { ImportedFiles = 2 });

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.Completed, result.Status);
            Assert.True(result.IsSuccessful);
            Assert.Equal(2, result.ImportedFiles);
            context.VerifyRepositoryImporterCalledOnce();
        }

        [Fact]
        public async Task ImportAsync_WhenSourceIsEmpty_ReturnsSourceMissing()
        {
            using var context = new DicomImportServiceTestContext();

            var result = await context.Service.ImportAsync(new DicomImportRequest(string.Empty), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.SourceMissing, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyNoPrerequisiteServicesCalled();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenSourceDoesNotExist_ReturnsSourceUnavailable()
        {
            using var context = new DicomImportServiceTestContext();

            var unavailableSourcePath = Path.Combine(context.SourceDirectoryPath, "Unavailable");

            var result = await context.Service.ImportAsync(new DicomImportRequest(unavailableSourcePath), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.SourceUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyNoPrerequisiteServicesCalled();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenRepositoryLocationRepositoryIsUnavailable_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext();

            context.Reset(repositoryLocationRepositoryAvailable: false);

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenDefaultRepositoryIsMissing_ReturnsDefaultRepositoryMissing()
        {
            using var context = new DicomImportServiceTestContext();

            context.SetDefaultRepositoryLocation(null);

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.DefaultRepositoryMissing, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyDefaultRepositoryRequestedOnce(TestContext.Current.CancellationToken);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenDefaultRepositoryHasInvalidId_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext();

            context.Reset(repositoryLocationId: 0);

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenDefaultRepositoryIsDisabled_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext();

            context.SetDefaultRepositoryLocation(new RepositoryLocation
            {
                Id = context.RepositoryLocationId,
                IsDefault = true,
                IsEnabled = false,
                RootPath = context.RepositoryDirectoryPath
            });

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenDefaultRepositoryRootPathIsEmpty_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext();

            context.SetDefaultRepositoryLocation(new RepositoryLocation
            {
                Id = context.RepositoryLocationId,
                IsDefault = true,
                IsEnabled = true,
                RootPath = string.Empty
            });

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenDefaultRepositoryDirectoryDoesNotExist_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext();

            context.SetDefaultRepositoryLocation(new RepositoryLocation
            {
                Id = context.RepositoryLocationId,
                IsDefault = true,
                IsEnabled = true,
                RootPath = Path.Combine(context.RepositoryDirectoryPath, "Unavailable")
            });

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task ImportAsync_WhenAuditIdentityIsInvalid_ReturnsAuditIdentityUnavailable(int? auditUserId)
        {
            using var context = new DicomImportServiceTestContext();

            context.SetAuditUserId(auditUserId);

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.AuditIdentityUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenRepositoryImportServiceIsUnavailable_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext();

            context.Reset(repositoryImportServiceAvailable: false);

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenCanceledBeforeInvocation_ReturnsCanceledWithoutCallingDependencies()
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
        public async Task ImportAsync_WhenRepositoryLocationLookupIsCanceled_ReturnsCanceled()
        {
            using var context = new DicomImportServiceTestContext();

            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            context.RepositoryLocationRepository.Setup(repository => repository.GetDefaultAsync(cancellationSource.Token)).Callback(cancellationSource.Cancel).ThrowsAsync(new OperationCanceledException(cancellationSource.Token));
            var result = await context.Service.ImportAsync(context.CreateRequest(), cancellationSource.Token);
            Assert.Equal(DicomImportStatus.Canceled, result.Status);
            Assert.False(result.IsSuccessful);
            context.RepositoryLocationRepository.Verify(repository => repository.GetDefaultAsync(cancellationSource.Token), Times.Once);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenCancellationIsRequestedAfterRepositoryLookup_ReturnsCanceled()
        {
            using var context = new DicomImportServiceTestContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            context.RepositoryLocationRepository
                .Setup(repository => repository.GetDefaultAsync(cancellationSource.Token))
                .Callback(cancellationSource.Cancel)
                .ReturnsAsync(new RepositoryLocation { Id = context.RepositoryLocationId, IsDefault = true, IsEnabled = true, RootPath = context.RepositoryDirectoryPath });

            var result = await context.Service.ImportAsync(context.CreateRequest(), cancellationSource.Token);

            Assert.Equal(DicomImportStatus.Canceled, result.Status);
            Assert.False(result.IsSuccessful);
            context.RepositoryLocationRepository.Verify(repository => repository.GetDefaultAsync(cancellationSource.Token), Times.Once);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportAsync_WhenRepositoryImportIsCanceled_ReturnsCanceled()
        {
            using var context = new DicomImportServiceTestContext();

            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            context.RepositoryImportService.Setup(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), cancellationSource.Token)).Callback(cancellationSource.Cancel).ThrowsAsync(new OperationCanceledException(cancellationSource.Token));
            var result = await context.Service.ImportAsync(context.CreateRequest(), cancellationSource.Token);
            Assert.Equal(DicomImportStatus.Canceled, result.Status);
            Assert.False(result.IsSuccessful);
            context.RepositoryImportService.Verify(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), cancellationSource.Token), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenRepositoryImportCompletes_MapsAllCounters()
        {
            using var context = new DicomImportServiceTestContext();

            var repositoryResult = new RepositoryImportResult
            {
                FailedFiles = 6,
                ImportedFiles = 4,
                SkippedFiles = 5
            };

            repositoryResult.Files.Add(new RepositoryImportFileInfo
            {
                IsDicomFile = true,
                SeriesInstanceUid = "1.2.3.2",
                SopInstanceUid = "1.2.3.3",
                StudyInstanceUid = "1.2.3.1"
            });

            repositoryResult.Files.Add(new RepositoryImportFileInfo
            {
                IsDicomFile = true
            });

            repositoryResult.Files.Add(new RepositoryImportFileInfo());

            context.SetImportResult(repositoryResult);

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(3, result.DiscoveredFiles);
            Assert.Equal(2, result.ValidDicomFiles);
            Assert.Equal(1, result.ImportableFiles);
            Assert.Equal(4, result.ImportedFiles);
            Assert.Equal(5, result.SkippedFiles);
            Assert.Equal(6, result.FailedFiles);
            Assert.Equal(DicomImportStatus.CompletedWithErrors, result.Status);
            Assert.False(result.IsSuccessful);
        }

        [Fact]
        public async Task ImportAsync_WhenFilesWereSkipped_ReturnsCompletedWithSkippedFiles()
        {
            using var context = new DicomImportServiceTestContext();

            context.SetImportResult(new RepositoryImportResult
            {
                ImportedFiles = 3,
                SkippedFiles = 2
            });

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithSkippedFiles, result.Status);
            Assert.True(result.IsSuccessful);
            Assert.Equal(3, result.ImportedFiles);
            Assert.Equal(2, result.SkippedFiles);
        }

        [Fact]
        public async Task ImportAsync_WhenFailedFilesAreReported_ReturnsCompletedWithErrors()
        {
            using var context = new DicomImportServiceTestContext();

            context.SetImportResult(new RepositoryImportResult
            {
                FailedFiles = 1,
                ImportedFiles = 2
            });

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithErrors, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Equal(1, result.FailedFiles);
            Assert.Equal(2, result.ImportedFiles);
        }

        [Fact]
        public async Task ImportAsync_WhenRepositoryReturnsTechnicalErrors_ReturnsCompletedWithErrors()
        {
            using var context = new DicomImportServiceTestContext();

            var repositoryResult = new RepositoryImportResult
            {
                ImportedFiles = 2
            };

            repositoryResult.Errors.Add("Technical repository import detail.");
            context.SetImportResult(repositoryResult);

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithErrors, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Equal(2, result.ImportedFiles);
            Assert.Equal("Technical repository import detail.", Assert.Single(result.TechnicalErrors));
        }

        [Fact]
        public async Task ImportAsync_WhenRepositoryReturnsTechnicalErrors_KeepsErrorsSeparateFromStatus()
        {
            using var context = new DicomImportServiceTestContext();

            const string technicalError = @"Access to C:\Sensitive\Repository\Instance.dcm was denied.";
            var repositoryResult = new RepositoryImportResult
            {
                FailedFiles = 1
            };

            repositoryResult.Errors.Add(technicalError);
            context.SetImportResult(repositoryResult);

            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithErrors, result.Status);
            Assert.Equal(technicalError, Assert.Single(result.TechnicalErrors));
            Assert.DoesNotContain(technicalError, result.Status.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task ImportAsync_WhenUnexpectedExceptionOccurs_ReturnsFailedWithSeparatedTechnicalDetails()
        {
            using var context = new DicomImportServiceTestContext();

            const string technicalError = @"Access to C:\Sensitive\Repository was denied.";
            context.RepositoryImportService.Setup(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), TestContext.Current.CancellationToken)).ThrowsAsync(new IOException(technicalError));
            var result = await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);
            Assert.Equal(DicomImportStatus.Failed, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Contains(technicalError, Assert.Single(result.TechnicalErrors), StringComparison.Ordinal);
            Assert.DoesNotContain(technicalError, result.Status.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task ImportAsync_WhenInvoked_UsesCurrentAuditUserAsCreatedByUserId()
        {
            using var context = new DicomImportServiceTestContext();

            context.Reset(auditUserId: 73);
            await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);
            context.RepositoryImportService.Verify(service => service.ImportAsync(It.Is<RepositoryImportRequest>(request => request.CreatedByUserId == 73), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenInvoked_UsesDefaultRepositoryLocationId()
        {
            using var context = new DicomImportServiceTestContext();

            context.Reset(repositoryLocationId: 91);
            await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);
            context.RepositoryImportService.Verify(service => service.ImportAsync(It.Is<RepositoryImportRequest>(request => request.RepositoryLocationId == 91), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenInvoked_MapsPublicRequestToRepositoryRequest()
        {
            using var context = new DicomImportServiceTestContext();

            await context.Service.ImportAsync(context.CreateRequest(allowOverwrite: true), TestContext.Current.CancellationToken);
            context.RepositoryImportService.Verify(service => service.ImportAsync(It.Is<RepositoryImportRequest>(request => request.AllowOverwrite && request.CreatedByUserId == context.AuditUserId && request.RepositoryLocationId == context.RepositoryLocationId && request.SourcePath == context.SourceDirectoryPath && request.SourceType == ImportSourceType.Directory), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenOverwriteIsDisabled_ForwardsDisabledOverwrite()
        {
            using var context = new DicomImportServiceTestContext();

            await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);
            context.RepositoryImportService.Verify(service => service.ImportAsync(It.Is<RepositoryImportRequest>(request => !request.AllowOverwrite), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenInvoked_ForwardsCancellationTokenToRepositoryDependencies()
        {
            using var context = new DicomImportServiceTestContext();

            await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            context.VerifyDefaultRepositoryRequestedOnce(TestContext.Current.CancellationToken);
            context.RepositoryImportService.Verify(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenInvoked_CallsExistingRepositoryImporterExactlyOnce()
        {
            using var context = new DicomImportServiceTestContext();

            await context.Service.ImportAsync(context.CreateRequest(), TestContext.Current.CancellationToken);
            context.VerifyRepositoryImporterCalledOnce();
        }

        [Fact]
        public void ApplicationFactories_WhenCreatedSeparately_ReturnIndependentImportServices()
        {
            using var firstContext = new DicomImportServiceTestContext();
            using var secondContext = new DicomImportServiceTestContext();

            Assert.NotSame(firstContext.Application, secondContext.Application);
            Assert.NotSame(firstContext.Application.ImportService, secondContext.Application.ImportService);
            Assert.NotSame(firstContext.Service, secondContext.Service);
        }

        [Fact]
        public async Task ImportAsync_WhenApplicationGraphsRunConcurrently_UsesOnlyGraphOwnedDependencies()
        {
            using var firstContext = new DicomImportServiceTestContext();
            using var secondContext = new DicomImportServiceTestContext();
            firstContext.Reset(auditUserId: 71, repositoryLocationId: 81);
            secondContext.Reset(auditUserId: 72, repositoryLocationId: 82);

            await Task.WhenAll(
                firstContext.Service.ImportAsync(firstContext.CreateRequest(), TestContext.Current.CancellationToken),
                secondContext.Service.ImportAsync(secondContext.CreateRequest(), TestContext.Current.CancellationToken));

            firstContext.RepositoryImportService.Verify(service => service.ImportAsync(
                It.Is<RepositoryImportRequest>(request => request.CreatedByUserId == 71 && request.RepositoryLocationId == 81),
                TestContext.Current.CancellationToken), Times.Once);
            secondContext.RepositoryImportService.Verify(service => service.ImportAsync(
                It.Is<RepositoryImportRequest>(request => request.CreatedByUserId == 72 && request.RepositoryLocationId == 82),
                TestContext.Current.CancellationToken), Times.Once);
            firstContext.RepositoryImportService.Verify(service => service.ImportAsync(
                It.Is<RepositoryImportRequest>(request => request.CreatedByUserId == 72 || request.RepositoryLocationId == 82),
                It.IsAny<CancellationToken>()), Times.Never);
            secondContext.RepositoryImportService.Verify(service => service.ImportAsync(
                It.Is<RepositoryImportRequest>(request => request.CreatedByUserId == 71 || request.RepositoryLocationId == 81),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void ApplicationFactory_WhenCreateIsCalledRepeatedly_ReturnsSameApplicationAndImportService()
        {
            using var context = new DicomImportServiceTestContext();

            var secondApplication = context.Factory.Create();

            Assert.Same(context.Application, secondApplication);
            Assert.Same(context.Service, secondApplication.ImportService?.DicomImportService);
        }
    }
}


