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
    public sealed class DicomImportServiceTests : IClassFixture<DicomImportServiceFixture>
    {
        private readonly DicomImportServiceFixture _context;

        public DicomImportServiceTests(DicomImportServiceFixture context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.Reset();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenImportCompletesSuccessfully_ReturnsCompletedResult()
        {
            _context.SetImportResult(new RepositoryImportResult { ImportedFiles = 2 });

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.Completed, result.Status);
            Assert.True(result.IsSuccessful);
            Assert.Equal(2, result.ImportedFiles);
            _context.VerifyRepositoryImporterCalledOnce();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenSourceIsEmpty_ReturnsSourceMissing()
        {
            var result = await _context.Service.ImportDirectoryAsync(new DicomImportRequest(string.Empty), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.SourceMissing, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyNoPrerequisiteServicesCalled();
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenSourceDoesNotExist_ReturnsSourceUnavailable()
        {
            var unavailableSourcePath = Path.Combine(_context.SourceDirectoryPath, "Unavailable");

            var result = await _context.Service.ImportDirectoryAsync(new DicomImportRequest(unavailableSourcePath), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.SourceUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyNoPrerequisiteServicesCalled();
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryLocationRepositoryIsUnavailable_ReturnsRepositoryUnavailable()
        {
            _context.Reset(repositoryLocationRepositoryAvailable: false);

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyAuditIdentityNotRequested();
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenDefaultRepositoryIsMissing_ReturnsDefaultRepositoryMissing()
        {
            _context.SetDefaultRepositoryLocation(null);

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.DefaultRepositoryMissing, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyDefaultRepositoryRequestedOnce(TestContext.Current.CancellationToken);
            _context.VerifyAuditIdentityNotRequested();
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenDefaultRepositoryHasInvalidId_ReturnsRepositoryUnavailable()
        {
            _context.Reset(repositoryLocationId: 0);

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyAuditIdentityNotRequested();
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenDefaultRepositoryIsDisabled_ReturnsRepositoryUnavailable()
        {
            _context.SetDefaultRepositoryLocation(new RepositoryLocation
            {
                Id = _context.RepositoryLocationId,
                IsDefault = true,
                IsEnabled = false,
                RootPath = _context.RepositoryDirectoryPath
            });

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyAuditIdentityNotRequested();
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenDefaultRepositoryRootPathIsEmpty_ReturnsRepositoryUnavailable()
        {
            _context.SetDefaultRepositoryLocation(new RepositoryLocation
            {
                Id = _context.RepositoryLocationId,
                IsDefault = true,
                IsEnabled = true,
                RootPath = string.Empty
            });

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyAuditIdentityNotRequested();
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenDefaultRepositoryDirectoryDoesNotExist_ReturnsRepositoryUnavailable()
        {
            _context.SetDefaultRepositoryLocation(new RepositoryLocation
            {
                Id = _context.RepositoryLocationId,
                IsDefault = true,
                IsEnabled = true,
                RootPath = Path.Combine(_context.RepositoryDirectoryPath, "Unavailable")
            });

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyAuditIdentityNotRequested();
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task ImportDirectoryAsync_WhenAuditIdentityIsInvalid_ReturnsAuditIdentityUnavailable(int? auditUserId)
        {
            _context.SetAuditUserId(auditUserId);

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.AuditIdentityUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyAuditIdentityRequestedOnce(TestContext.Current.CancellationToken);
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryImportServiceIsUnavailable_ReturnsRepositoryUnavailable()
        {
            _context.Reset(repositoryImportServiceAvailable: false);

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyAuditIdentityRequestedOnce(TestContext.Current.CancellationToken);
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenCanceledBeforeInvocation_ReturnsCanceledWithoutCallingDependencies()
        {
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cancellationSource.Cancel();

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), cancellationSource.Token);

            Assert.Equal(DicomImportStatus.Canceled, result.Status);
            Assert.False(result.IsSuccessful);
            _context.VerifyNoPrerequisiteServicesCalled();
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryLocationLookupIsCanceled_ReturnsCanceled()
        {
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            _context.RepositoryLocationRepository.Setup(repository => repository.GetDefaultAsync(cancellationSource.Token)).Callback(cancellationSource.Cancel).ThrowsAsync(new OperationCanceledException(cancellationSource.Token));
            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), cancellationSource.Token);
            Assert.Equal(DicomImportStatus.Canceled, result.Status);
            Assert.False(result.IsSuccessful);
            _context.RepositoryLocationRepository.Verify(repository => repository.GetDefaultAsync(cancellationSource.Token), Times.Once);
            _context.VerifyAuditIdentityNotRequested();
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenAuditIdentityLookupIsCanceled_ReturnsCanceled()
        {
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            _context.AuditIdentityProvider.Setup(provider => provider.GetCurrentUserIdAsync(cancellationSource.Token)).Callback(cancellationSource.Cancel).ThrowsAsync(new OperationCanceledException(cancellationSource.Token));
            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), cancellationSource.Token);
            Assert.Equal(DicomImportStatus.Canceled, result.Status);
            Assert.False(result.IsSuccessful);
            _context.AuditIdentityProvider.Verify(provider => provider.GetCurrentUserIdAsync(cancellationSource.Token), Times.Once);
            _context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryImportIsCanceled_ReturnsCanceled()
        {
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            _context.RepositoryImportService.Setup(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), cancellationSource.Token)).Callback(cancellationSource.Cancel).ThrowsAsync(new OperationCanceledException(cancellationSource.Token));
            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), cancellationSource.Token);
            Assert.Equal(DicomImportStatus.Canceled, result.Status);
            Assert.False(result.IsSuccessful);
            _context.RepositoryImportService.Verify(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), cancellationSource.Token), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryImportCompletes_MapsAllCounters()
        {
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

            _context.SetImportResult(repositoryResult);

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

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
        public async Task ImportDirectoryAsync_WhenFilesWereSkipped_ReturnsCompletedWithSkippedFiles()
        {
            _context.SetImportResult(new RepositoryImportResult
            {
                ImportedFiles = 3,
                SkippedFiles = 2
            });

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithSkippedFiles, result.Status);
            Assert.True(result.IsSuccessful);
            Assert.Equal(3, result.ImportedFiles);
            Assert.Equal(2, result.SkippedFiles);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenFailedFilesAreReported_ReturnsCompletedWithErrors()
        {
            _context.SetImportResult(new RepositoryImportResult
            {
                FailedFiles = 1,
                ImportedFiles = 2
            });

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithErrors, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Equal(1, result.FailedFiles);
            Assert.Equal(2, result.ImportedFiles);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryReturnsTechnicalErrors_ReturnsCompletedWithErrors()
        {
            var repositoryResult = new RepositoryImportResult
            {
                ImportedFiles = 2
            };

            repositoryResult.Errors.Add("Technical repository import detail.");
            _context.SetImportResult(repositoryResult);

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithErrors, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Equal(2, result.ImportedFiles);
            Assert.Equal("Technical repository import detail.", Assert.Single(result.TechnicalErrors));
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryReturnsTechnicalErrors_KeepsErrorsSeparateFromStatus()
        {
            const string technicalError = @"Access to C:\Sensitive\Repository\Instance.dcm was denied.";
            var repositoryResult = new RepositoryImportResult
            {
                FailedFiles = 1
            };

            repositoryResult.Errors.Add(technicalError);
            _context.SetImportResult(repositoryResult);

            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithErrors, result.Status);
            Assert.Equal(technicalError, Assert.Single(result.TechnicalErrors));
            Assert.DoesNotContain(technicalError, result.Status.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenUnexpectedExceptionOccurs_ReturnsFailedWithSeparatedTechnicalDetails()
        {
            const string technicalError = @"Access to C:\Sensitive\Repository was denied.";
            _context.RepositoryImportService.Setup(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), TestContext.Current.CancellationToken)).ThrowsAsync(new IOException(technicalError));
            var result = await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);
            Assert.Equal(DicomImportStatus.Failed, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Contains(technicalError, Assert.Single(result.TechnicalErrors), StringComparison.Ordinal);
            Assert.DoesNotContain(technicalError, result.Status.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenInvoked_UsesCurrentAuditUserAsCreatedByUserId()
        {
            _context.Reset(auditUserId: 73);
            await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);
            _context.RepositoryImportService.Verify(service => service.ImportAsync(It.Is<RepositoryImportRequest>(request => request.CreatedByUserId == 73), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenInvoked_UsesDefaultRepositoryLocationId()
        {
            _context.Reset(repositoryLocationId: 91);
            await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);
            _context.RepositoryImportService.Verify(service => service.ImportAsync(It.Is<RepositoryImportRequest>(request => request.RepositoryLocationId == 91), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenInvoked_MapsPublicRequestToRepositoryRequest()
        {
            await _context.Service.ImportDirectoryAsync(_context.CreateRequest(allowOverwrite: true), TestContext.Current.CancellationToken);
            _context.RepositoryImportService.Verify(service => service.ImportAsync(It.Is<RepositoryImportRequest>(request => request.AllowOverwrite && request.CreatedByUserId == _context.AuditUserId && request.RepositoryLocationId == _context.RepositoryLocationId && request.SourcePath == _context.SourceDirectoryPath && request.SourceType == ImportSourceType.Directory), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenOverwriteIsDisabled_ForwardsDisabledOverwrite()
        {
            await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);
            _context.RepositoryImportService.Verify(service => service.ImportAsync(It.Is<RepositoryImportRequest>(request => !request.AllowOverwrite), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenInvoked_ForwardsCancellationTokenToAllAsyncDependencies()
        {
            await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);

            _context.VerifyDefaultRepositoryRequestedOnce(TestContext.Current.CancellationToken);
            _context.VerifyAuditIdentityRequestedOnce(TestContext.Current.CancellationToken);
            _context.RepositoryImportService.Verify(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenInvoked_CallsExistingRepositoryImporterExactlyOnce()
        {
            await _context.Service.ImportDirectoryAsync(_context.CreateRequest(), TestContext.Current.CancellationToken);
            _context.VerifyRepositoryImporterCalledOnce();
        }

        [Fact]
        public void ApplicationFactory_WhenCreateIsCalledRepeatedly_ReturnsSameApplicationAndImportService()
        {
            var secondApplication = _context.Factory.Create();

            Assert.Same(_context.Application, secondApplication);
            Assert.Same(_context.Service, secondApplication.ImportService?.DicomImportService);
        }
    }
}