using MarcusRunge.Mopr.Workbench.Application.Import;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RepositoryContract = MarcusRunge.Mopr.Workbench.Services.Repository.Contracts.IRepository;
using RepositoryImportRequest = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportRequest;
using RepositoryImportResult = MarcusRunge.Mopr.Workbench.Services.Repository.Models.DicomImportResult;
using RepositoryImportService = MarcusRunge.Mopr.Workbench.Services.Repository.Contracts.IDicomImportService;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Import
{
    public sealed class DicomImportServiceTests
    {
        [Fact]
        public async Task ImportDirectoryAsync_WhenImportCompletesSuccessfully_ReturnsCompletedResult()
        {
            using var context = new DicomImportServiceTestContext();
            context.SetImportResult(new RepositoryImportResult { ImportedFiles = 2 });

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.Completed, result.Status);
            Assert.True(result.IsSuccessful);
            Assert.Equal(2, result.ImportedFiles);
            context.VerifyRepositoryImporterCalledOnce();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenSourceIsEmpty_ReturnsSourceMissing()
        {
            using var context = new DicomImportServiceTestContext();

            var result = await context.Service.ImportDirectoryAsync(new DicomImportRequest(string.Empty), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.SourceMissing, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyNoPrerequisiteServicesCalled();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenSourceDoesNotExist_ReturnsSourceUnavailable()
        {
            using var context = new DicomImportServiceTestContext();
            var unavailableSourcePath = Path.Combine(context.SourceDirectoryPath, "Unavailable");

            var result = await context.Service.ImportDirectoryAsync(new DicomImportRequest(unavailableSourcePath), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.SourceUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyNoPrerequisiteServicesCalled();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryLocationRepositoryIsUnavailable_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext(repositoryLocationRepositoryAvailable: false);

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyAuditIdentityNotRequested();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenDefaultRepositoryIsMissing_ReturnsDefaultRepositoryMissing()
        {
            using var context = new DicomImportServiceTestContext();
            context.SetDefaultRepositoryLocation(null);

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.DefaultRepositoryMissing, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyDefaultRepositoryRequestedOnce(TestContext.Current.CancellationToken);
            context.VerifyAuditIdentityNotRequested();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenDefaultRepositoryHasInvalidId_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext(repositoryLocationId: 0);

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            context.VerifyAuditIdentityNotRequested();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenDefaultRepositoryIsDisabled_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext();
            context.SetDefaultRepositoryLocation(new RepositoryLocation
            {
                Id = context.RepositoryLocationId,
                IsDefault = true,
                IsEnabled = false,
                RootPath = context.RepositoryDirectoryPath
            });

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            context.VerifyAuditIdentityNotRequested();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenDefaultRepositoryDirectoryDoesNotExist_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext();
            context.SetDefaultRepositoryLocation(new RepositoryLocation
            {
                Id = context.RepositoryLocationId,
                IsDefault = true,
                IsEnabled = true,
                RootPath = Path.Combine(context.RepositoryDirectoryPath, "Unavailable")
            });

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            context.VerifyAuditIdentityNotRequested();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task ImportDirectoryAsync_WhenAuditIdentityIsInvalid_ReturnsAuditIdentityUnavailable(int? auditUserId)
        {
            using var context = new DicomImportServiceTestContext();
            context.SetAuditUserId(auditUserId);

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.AuditIdentityUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyAuditIdentityRequestedOnce(TestContext.Current.CancellationToken);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryImportServiceIsUnavailable_ReturnsRepositoryUnavailable()
        {
            using var context = new DicomImportServiceTestContext(repositoryImportServiceAvailable: false);

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.RepositoryUnavailable, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyAuditIdentityRequestedOnce(TestContext.Current.CancellationToken);
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenCanceledBeforeInvocation_ReturnsCanceledWithoutCallingDependencies()
        {
            using var context = new DicomImportServiceTestContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cancellationSource.Cancel();

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), cancellationSource.Token);

            Assert.Equal(DicomImportStatus.Canceled, result.Status);
            Assert.False(result.IsSuccessful);
            context.VerifyNoPrerequisiteServicesCalled();
            context.VerifyRepositoryImporterNotCalled();
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryImportIsCanceled_ReturnsCanceled()
        {
            using var context = new DicomImportServiceTestContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            context.RepositoryImportService                .Setup(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), cancellationSource.Token))                .Callback(cancellationSource.Cancel)                .ThrowsAsync(new OperationCanceledException(cancellationSource.Token));

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), cancellationSource.Token);

            Assert.Equal(DicomImportStatus.Canceled, result.Status);
            Assert.False(result.IsSuccessful);
            context.RepositoryImportService.Verify(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), cancellationSource.Token), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryImportCompletes_MapsAllCounters()
        {
            using var context = new DicomImportServiceTestContext();
            var repositoryResult = new RepositoryImportResult
            {
                FailedFiles = 6,
                ImportedFiles = 4,
                SkippedFiles = 5
            };

            repositoryResult.Files.Add(new Services.Repository.Models.DicomImportFileInfo
            {
                IsDicomFile = true,
                SeriesInstanceUid = "1.2.3.2",
                SopInstanceUid = "1.2.3.3",
                StudyInstanceUid = "1.2.3.1"
            });
            repositoryResult.Files.Add(new Services.Repository.Models.DicomImportFileInfo
            {
                IsDicomFile = true
            });
            repositoryResult.Files.Add(new Services.Repository.Models.DicomImportFileInfo());

            context.SetImportResult(repositoryResult);

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(3, result.DiscoveredFiles);
            Assert.Equal(2, result.ValidDicomFiles);
            Assert.Equal(1, result.ImportableFiles);
            Assert.Equal(4, result.ImportedFiles);
            Assert.Equal(5, result.SkippedFiles);
            Assert.Equal(6, result.FailedFiles);
            Assert.Equal(DicomImportStatus.CompletedWithErrors, result.Status);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenFilesWereSkipped_ReturnsCompletedWithSkippedFiles()
        {
            using var context = new DicomImportServiceTestContext();
            context.SetImportResult(new RepositoryImportResult
            {
                ImportedFiles = 3,
                SkippedFiles = 2
            });

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithSkippedFiles, result.Status);
            Assert.True(result.IsSuccessful);
            Assert.Equal(3, result.ImportedFiles);
            Assert.Equal(2, result.SkippedFiles);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenIndividualErrorsOccurred_ReturnsCompletedWithErrors()
        {
            using var context = new DicomImportServiceTestContext();
            var repositoryResult = new RepositoryImportResult
            {
                FailedFiles = 1,
                ImportedFiles = 2
            };
            repositoryResult.Errors.Add("Technical repository import detail.");
            context.SetImportResult(repositoryResult);

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithErrors, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Equal(1, result.FailedFiles);
            Assert.Equal(2, result.ImportedFiles);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenRepositoryReturnsTechnicalErrors_KeepsErrorsSeparateFromStatus()
        {
            using var context = new DicomImportServiceTestContext();
            const string technicalError = @"Access to C:\Sensitive\Repository\Instance.dcm was denied.";
            var repositoryResult = new RepositoryImportResult
            {
                FailedFiles = 1
            };
            repositoryResult.Errors.Add(technicalError);
            context.SetImportResult(repositoryResult);

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.CompletedWithErrors, result.Status);
            Assert.Equal(technicalError, Assert.Single(result.TechnicalErrors));
            Assert.DoesNotContain(technicalError, result.Status.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenUnexpectedExceptionOccurs_ReturnsFailedWithSeparatedTechnicalDetails()
        {
            using var context = new DicomImportServiceTestContext();
            const string technicalError = @"Access to C:\Sensitive\Repository was denied.";

            context.RepositoryImportService
                .Setup(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), TestContext.Current.CancellationToken))
                .ThrowsAsync(new IOException(technicalError));

            var result = await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportStatus.Failed, result.Status);
            Assert.False(result.IsSuccessful);
            Assert.Contains(technicalError, Assert.Single(result.TechnicalErrors), StringComparison.Ordinal);
            Assert.DoesNotContain(technicalError, result.Status.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenInvoked_UsesCurrentAuditUserAsCreatedByUserId()
        {
            using var context = new DicomImportServiceTestContext(auditUserId: 73);

            await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            context.RepositoryImportService.Verify(service => service.ImportAsync(It.Is<RepositoryImportRequest>(request => request.CreatedByUserId == 73), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenInvoked_UsesDefaultRepositoryLocationId()
        {
            using var context = new DicomImportServiceTestContext(repositoryLocationId: 91);

            await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            context.RepositoryImportService.Verify(service => service.ImportAsync(It.Is<RepositoryImportRequest>(request => request.RepositoryLocationId == 91), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenInvoked_MapsPublicRequestToRepositoryRequest()
        {
            using var context = new DicomImportServiceTestContext();

            await context.Service.ImportDirectoryAsync(context.CreateRequest(allowOverwrite: true), TestContext.Current.CancellationToken);

            context.RepositoryImportService.Verify(service => service.ImportAsync(
                It.Is<RepositoryImportRequest>(request =>
                    request.AllowOverwrite &&
                    request.CreatedByUserId == context.AuditUserId &&
                    request.RepositoryLocationId == context.RepositoryLocationId &&
                    request.SourcePath == context.SourceDirectoryPath &&
                    request.SourceType == ImportSourceType.Directory),
                TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenInvoked_ForwardsCancellationTokenToAllAsyncDependencies()
        {
            using var context = new DicomImportServiceTestContext();

            await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            context.VerifyDefaultRepositoryRequestedOnce(TestContext.Current.CancellationToken);
            context.VerifyAuditIdentityRequestedOnce(TestContext.Current.CancellationToken);
            context.RepositoryImportService.Verify(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), TestContext.Current.CancellationToken), Times.Once);
        }

        [Fact]
        public async Task ImportDirectoryAsync_WhenInvoked_CallsExistingRepositoryImporterExactlyOnce()
        {
            using var context = new DicomImportServiceTestContext();

            await context.Service.ImportDirectoryAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            context.VerifyRepositoryImporterCalledOnce();
        }

        private sealed class DicomImportServiceTestContext : IDisposable
        {
            public DicomImportServiceTestContext(bool repositoryLocationRepositoryAvailable = true, bool repositoryImportServiceAvailable = true, int auditUserId = 41, int repositoryLocationId = 23)
            {
                AuditUserId = auditUserId;
                RepositoryLocationId = repositoryLocationId;
                SourceDirectoryPath = Path.Combine(Path.GetTempPath(), $"mopr-import-source-{Guid.NewGuid():N}");
                RepositoryDirectoryPath = Path.Combine(Path.GetTempPath(), $"mopr-import-repository-{Guid.NewGuid():N}");

                Directory.CreateDirectory(SourceDirectoryPath);
                Directory.CreateDirectory(RepositoryDirectoryPath);

                var defaultRepositoryLocation = new RepositoryLocation
                {
                    Id = RepositoryLocationId,
                    IsDefault = true,
                    IsEnabled = true,
                    RootPath = RepositoryDirectoryPath
                };

                RepositoryLocationRepository.Setup(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(defaultRepositoryLocation);
                Persistence.SetupGet(persistence => persistence.RepositoryLocation).Returns(repositoryLocationRepositoryAvailable ? RepositoryLocationRepository.Object : null);
                AuditIdentityProvider.Setup(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(AuditUserId);
                Repository.SetupGet(repository => repository.ImportService).Returns(repositoryImportServiceAvailable ? RepositoryImportService.Object : null);
                SetImportResult(new RepositoryImportResult());

                Service = new DicomImportService(Persistence.Object, Repository.Object, AuditIdentityProvider.Object);
            }

            public int AuditUserId { get; }
            public Mock<IAuditIdentityProvider> AuditIdentityProvider { get; } = new(MockBehavior.Strict);
            public Mock<IPersistence> Persistence { get; } = new(MockBehavior.Strict);
            public Mock<RepositoryContract> Repository { get; } = new(MockBehavior.Strict);
            public string RepositoryDirectoryPath { get; }
            public Mock<RepositoryImportService> RepositoryImportService { get; } = new(MockBehavior.Strict);
            public int RepositoryLocationId { get; }
            public Mock<IRepositoryLocationRepository> RepositoryLocationRepository { get; } = new(MockBehavior.Strict);
            public DicomImportService Service { get; }
            public string SourceDirectoryPath { get; }

            public DicomImportRequest CreateRequest(bool allowOverwrite = false) => new(SourceDirectoryPath, allowOverwrite);

            public void Dispose()
            {
                TryDeleteDirectory(SourceDirectoryPath);
                TryDeleteDirectory(RepositoryDirectoryPath);
            }

            public void SetAuditUserId(int? auditUserId) => AuditIdentityProvider.Setup(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(auditUserId);

            public void SetDefaultRepositoryLocation(RepositoryLocation? repositoryLocation) => RepositoryLocationRepository.Setup(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(repositoryLocation);

            public void SetImportResult(RepositoryImportResult result) => RepositoryImportService.Setup(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);

            public void VerifyAuditIdentityNotRequested() => AuditIdentityProvider.Verify(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()), Times.Never);

            public void VerifyAuditIdentityRequestedOnce(CancellationToken cancellationToken) => AuditIdentityProvider.Verify(provider => provider.GetCurrentUserIdAsync(cancellationToken), Times.Once);

            public void VerifyDefaultRepositoryRequestedOnce(CancellationToken cancellationToken) => RepositoryLocationRepository.Verify(repository => repository.GetDefaultAsync(cancellationToken), Times.Once);

            public void VerifyNoPrerequisiteServicesCalled()
            {
                Persistence.VerifyGet(persistence => persistence.RepositoryLocation, Times.Never);
                VerifyAuditIdentityNotRequested();
            }

            public void VerifyRepositoryImporterCalledOnce() => RepositoryImportService.Verify(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), It.IsAny<CancellationToken>()), Times.Once);

            public void VerifyRepositoryImporterNotCalled() => RepositoryImportService.Verify(service => service.ImportAsync(It.IsAny<RepositoryImportRequest>(), It.IsAny<CancellationToken>()), Times.Never);

            private static void TryDeleteDirectory(string directoryPath)
            {
                try
                {
                    if (Directory.Exists(directoryPath))
                    {
                        Directory.Delete(directoryPath, recursive: true);
                    }
                }
                catch
                {
                    // Test cleanup must not hide the original test result.
                }
            }
        }
    }
}