using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Import;
using Moq;
using System.IO;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.Import
{
    public sealed class DicomImportSourceResolverTests
    {
        [Fact]
        public async Task ResolveAsync_WhenLocalDirectoryIsSpecified_ReturnsSupportedLocalDirectory()
        {
            using var context = new DicomImportSourceResolverTestContext();
            var request = new DicomImportRequest(context.SourceDirectoryPath, DicomImportSourceType.LocalDirectory);

            var result = await context.Resolver.ResolveAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportSourceType.LocalDirectory, result.SourceType);
            Assert.Equal(context.SourceDirectoryPath, result.EffectiveSourcePath);
            Assert.Equal(context.SourceDirectoryPath, result.OriginalSourcePath);
            Assert.True(result.Exists);
            Assert.True(result.IsReady);
            Assert.True(result.IsRepositoryImportSupported);
            Assert.False(result.TreatAsReadOnly);
        }

        [Fact]
        public async Task ResolveAsync_WhenAutoDetectFindsFixedDrive_ReturnsLocalDirectory()
        {
            using var context = new DicomImportSourceResolverTestContext();
            context.SetDrive(DriveType.Fixed, isReady: true);

            var result = await context.Resolver.ResolveAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportSourceType.LocalDirectory, result.SourceType);
            Assert.True(result.Exists);
            Assert.True(result.IsReady);
            Assert.True(result.IsRepositoryImportSupported);
            Assert.False(result.TreatAsReadOnly);
        }

        [Theory]
        [InlineData(DriveType.Network, DicomImportSourceType.MappedNetworkDrive, true)]
        [InlineData(DriveType.CDRom, DicomImportSourceType.CdRom, true)]
        [InlineData(DriveType.Removable, DicomImportSourceType.RemovableDrive, false)]
        [InlineData(DriveType.Fixed, DicomImportSourceType.LocalDirectory, false)]
        [InlineData(DriveType.Ram, DicomImportSourceType.VirtualDrive, false)]
        public async Task ResolveAsync_WhenAutoDetectFindsKnownDriveType_ReturnsExpectedSourceType(DriveType driveType, DicomImportSourceType expectedSourceType, bool expectedReadOnly)
        {
            using var context = new DicomImportSourceResolverTestContext();
            context.SetDrive(driveType, isReady: true);

            var result = await context.Resolver.ResolveAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(expectedSourceType, result.SourceType);
            Assert.True(result.Exists);
            Assert.True(result.IsReady);
            Assert.True(result.IsRepositoryImportSupported);
            Assert.Equal(expectedReadOnly, result.TreatAsReadOnly);
        }

        [Fact]
        public async Task ResolveAsync_WhenSourceContainsDicomDirectoryIndex_ReportsDicomDirectoryIndex()
        {
            using var context = new DicomImportSourceResolverTestContext();
            await File.WriteAllTextAsync(Path.Combine(context.SourceDirectoryPath, "DICOMDIR"), string.Empty, TestContext.Current.CancellationToken);

            var result = await context.Resolver.ResolveAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.True(result.ContainsDicomDirectoryIndex);
        }

        [Fact]
        public async Task ResolveAsync_WhenSourceDoesNotContainDicomDirectoryIndex_DoesNotReportDicomDirectoryIndex()
        {
            using var context = new DicomImportSourceResolverTestContext();

            var result = await context.Resolver.ResolveAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.False(result.ContainsDicomDirectoryIndex);
        }

        [Fact]
        public async Task ResolveAsync_WhenIsoFileExists_ReturnsUnpreparedIsoImage()
        {
            using var context = new DicomImportSourceResolverTestContext();
            var isoPath = Path.Combine(context.SourceDirectoryPath, "Study.iso");
            await File.WriteAllBytesAsync(isoPath, [], TestContext.Current.CancellationToken);

            var result = await context.Resolver.ResolveAsync(new DicomImportRequest(isoPath), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportSourceType.IsoImage, result.SourceType);
            Assert.Equal(isoPath, result.OriginalSourcePath);
            Assert.Equal(isoPath, result.EffectiveSourcePath);
            Assert.True(result.Exists);
            Assert.False(result.IsReady);
            Assert.False(result.IsRepositoryImportSupported);
            Assert.True(result.TreatAsReadOnly);
            Assert.False(result.ContainsDicomDirectoryIndex);
        }

        [Fact]
        public async Task ResolveAsync_WhenExplicitIsoFileDoesNotExist_ReturnsUnavailableIsoImage()
        {
            using var context = new DicomImportSourceResolverTestContext();
            var isoPath = Path.Combine(context.SourceDirectoryPath, "Unavailable.iso");

            var result = await context.Resolver.ResolveAsync(new DicomImportRequest(isoPath, DicomImportSourceType.IsoImage), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportSourceType.IsoImage, result.SourceType);
            Assert.False(result.Exists);
            Assert.False(result.IsReady);
            Assert.False(result.IsRepositoryImportSupported);
            Assert.True(result.TreatAsReadOnly);
        }

        [Fact]
        public async Task ResolveAsync_WhenExplicitNetworkShareIsAvailable_ReturnsSupportedReadOnlySource()
        {
            using var context = new DicomImportSourceResolverTestContext();
            var request = new DicomImportRequest(context.SourceDirectoryPath, DicomImportSourceType.NetworkShare);

            var result = await context.Resolver.ResolveAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportSourceType.NetworkShare, result.SourceType);
            Assert.True(result.Exists);
            Assert.True(result.IsReady);
            Assert.True(result.IsRepositoryImportSupported);
            Assert.True(result.TreatAsReadOnly);
        }

        [Theory]
        [InlineData(DicomImportSourceType.UsbDrive, false)]
        [InlineData(DicomImportSourceType.RemovableDrive, false)]
        [InlineData(DicomImportSourceType.ExternalDrive, false)]
        [InlineData(DicomImportSourceType.SdCard, false)]
        [InlineData(DicomImportSourceType.CdRom, true)]
        [InlineData(DicomImportSourceType.Dvd, true)]
        [InlineData(DicomImportSourceType.NetworkShare, true)]
        [InlineData(DicomImportSourceType.MappedNetworkDrive, true)]
        [InlineData(DicomImportSourceType.VirtualDrive, false)]
        public async Task ResolveAsync_WhenExplicitDirectoryBasedSourceExists_ReturnsSupportedSource(DicomImportSourceType sourceType, bool expectedReadOnly)
        {
            using var context = new DicomImportSourceResolverTestContext();

            var result = await context.Resolver.ResolveAsync(new DicomImportRequest(context.SourceDirectoryPath, sourceType), TestContext.Current.CancellationToken);

            Assert.Equal(sourceType, result.SourceType);
            Assert.True(result.Exists);
            Assert.True(result.IsReady);
            Assert.True(result.IsRepositoryImportSupported);
            Assert.Equal(expectedReadOnly, result.TreatAsReadOnly);
        }

        [Fact]
        public async Task ResolveAsync_WhenSourceDoesNotExist_ReturnsUnavailableSource()
        {
            using var context = new DicomImportSourceResolverTestContext();
            var unavailablePath = Path.Combine(context.SourceDirectoryPath, "Unavailable");

            var result = await context.Resolver.ResolveAsync(new DicomImportRequest(unavailablePath, DicomImportSourceType.LocalDirectory), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportSourceType.LocalDirectory, result.SourceType);
            Assert.Equal(unavailablePath, result.EffectiveSourcePath);
            Assert.False(result.Exists);
            Assert.False(result.IsReady);
            Assert.False(result.IsRepositoryImportSupported);
        }

        [Fact]
        public async Task ResolveAsync_WhenSourcePathIsEmpty_ReturnsUnknownUnavailableSource()
        {
            using var context = new DicomImportSourceResolverTestContext();

            var result = await context.Resolver.ResolveAsync(new DicomImportRequest(string.Empty), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportSourceType.Unknown, result.SourceType);
            Assert.False(result.Exists);
            Assert.False(result.IsReady);
            Assert.False(result.IsRepositoryImportSupported);
        }

        [Fact]
        public async Task ResolveAsync_WhenExplicitUnknownSourceIsSpecified_ReturnsUnknownUnavailableSource()
        {
            using var context = new DicomImportSourceResolverTestContext();

            var result = await context.Resolver.ResolveAsync(new DicomImportRequest(context.SourceDirectoryPath, DicomImportSourceType.Unknown), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportSourceType.Unknown, result.SourceType);
            Assert.False(result.Exists);
            Assert.False(result.IsReady);
            Assert.False(result.IsRepositoryImportSupported);
        }

        [Fact]
        public async Task ResolveAsync_WhenCancellationWasRequested_ThrowsOperationCanceledException()
        {
            using var context = new DicomImportSourceResolverTestContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cancellationSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Resolver.ResolveAsync(context.CreateRequest(), cancellationSource.Token));
        }

        [Fact]
        public async Task ResolveAsync_WhenDriveIsNotReady_ReturnsUnavailableSource()
        {
            using var context = new DicomImportSourceResolverTestContext();
            context.SetDrive(DriveType.Removable, isReady: false);

            var result = await context.Resolver.ResolveAsync(context.CreateRequest(), TestContext.Current.CancellationToken);

            Assert.Equal(DicomImportSourceType.RemovableDrive, result.SourceType);
            Assert.True(result.Exists);
            Assert.False(result.IsReady);
            Assert.False(result.IsRepositoryImportSupported);
        }

        [Fact]
        public void Create_WhenCalledWithSameImportServiceBase_ReturnsSameScopedResolver()
        {
            using var context = new DicomImportSourceResolverTestContext();

            var secondResolver = DicomImportSourceResolver.Create(context.ImportServiceBase.Object, CreationLifetime.Scoped);

            Assert.Same(context.Resolver, secondResolver);
        }

        [Fact]
        public void Create_WhenCalledWithDifferentImportServiceBases_ReturnsIndependentResolvers()
        {
            using var firstContext = new DicomImportSourceResolverTestContext();
            using var secondContext = new DicomImportSourceResolverTestContext();

            Assert.NotSame(firstContext.ImportServiceBase.Object, secondContext.ImportServiceBase.Object);
            Assert.NotSame(firstContext.Resolver, secondContext.Resolver);
        }

        [Fact]
        public async Task ResolveAsync_WhenResolversBelongToDifferentImportGraphs_UsesGraphOwnedDriveProvider()
        {
            using var firstContext = new DicomImportSourceResolverTestContext();
            using var secondContext = new DicomImportSourceResolverTestContext();
            firstContext.SetDrive(DriveType.Removable, isReady: true);
            secondContext.SetDrive(DriveType.Network, isReady: true);

            var results = await Task.WhenAll(
                firstContext.Resolver.ResolveAsync(firstContext.CreateRequest(), TestContext.Current.CancellationToken),
                secondContext.Resolver.ResolveAsync(secondContext.CreateRequest(), TestContext.Current.CancellationToken));

            Assert.Equal(DicomImportSourceType.RemovableDrive, results[0].SourceType);
            Assert.Equal(DicomImportSourceType.MappedNetworkDrive, results[1].SourceType);
            firstContext.DriveProvider.Verify(provider => provider.GetDrives(), Times.Once);
            secondContext.DriveProvider.Verify(provider => provider.GetDrives(), Times.Once);
        }

        private sealed class DicomImportSourceResolverTestContext : IDisposable
        {
            private bool _disposed;

            public DicomImportSourceResolverTestContext()
            {
                SourceDirectoryPath = Path.Combine(Path.GetTempPath(), $"mopr-import-source-resolver-{Guid.NewGuid():N}");
                Directory.CreateDirectory(SourceDirectoryPath);

                SetDrive(DriveType.Fixed, isReady: true);

                ImportServiceBase
                    .SetupGet(service => service.DicomImportDriveProvider)
                    .Returns(DriveProvider.Object);

                Resolver = DicomImportSourceResolver.Create(ImportServiceBase.Object, CreationLifetime.Scoped);
            }

            public Mock<IDicomImportDriveProvider> DriveProvider { get; } = new(MockBehavior.Strict);

            public Mock<IImportServiceBase> ImportServiceBase { get; } = new(MockBehavior.Strict);

            public IDicomImportSourceResolver Resolver { get; }

            public string SourceDirectoryPath { get; }

            public DicomImportRequest CreateRequest() => new(SourceDirectoryPath);

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                TryDeleteDirectory(SourceDirectoryPath);
                GC.SuppressFinalize(this);
            }

            public void SetDrive(DriveType driveType, bool isReady)
            {
                var rootPath = Path.GetPathRoot(SourceDirectoryPath) ?? SourceDirectoryPath;

                DriveProvider
                    .Setup(provider => provider.GetDrives())
                    .Returns([new DicomImportDriveInfo(rootPath, driveType, isReady)]);
            }

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