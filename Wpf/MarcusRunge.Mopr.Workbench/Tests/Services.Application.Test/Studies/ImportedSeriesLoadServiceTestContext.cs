using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Studies;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.Studies
{
    internal sealed class ImportedSeriesLoadServiceTestContext : IDisposable
    {
        public ImportedSeriesLoadServiceTestContext()
        {
            RepositoryRootPath = Path.Combine(Path.GetTempPath(), "MoprImportedSeriesLoadTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RepositoryRootPath);
            ApplicationBase = new Mock<IApplicationBase>(MockBehavior.Strict);
            Persistence = new Mock<IPersistence>(MockBehavior.Strict);
            Repository = new Mock<IImportedSeriesRepository>(MockBehavior.Strict);
            StudiesServiceBase = new Mock<IStudiesServiceBase>(MockBehavior.Strict);
            ApplicationBase.SetupGet(value => value.Persistence).Returns(Persistence.Object);
            StudiesServiceBase.SetupGet(value => value.ApplicationBase).Returns(ApplicationBase.Object);
            Persistence.SetupGet(value => value.ImportedSeries).Returns(Repository.Object);
            Service = ImportedSeriesLoadService.Create(StudiesServiceBase.Object, CreationLifetime.Scoped) ?? throw new InvalidOperationException("The imported-series load service could not be created.");
        }

        public Mock<IApplicationBase> ApplicationBase { get; }
        public Mock<IPersistence> Persistence { get; }
        public Mock<IImportedSeriesRepository> Repository { get; }
        public string RepositoryRootPath { get; }
        public IImportedSeriesLoadService Service { get; }
        public Mock<IStudiesServiceBase> StudiesServiceBase { get; }

        public void Dispose()
        {
            if (Directory.Exists(RepositoryRootPath)) Directory.Delete(RepositoryRootPath, recursive: true);
        }

        public string CreateFile(string relativePath, string content = "DICOM")
        {
            var absolutePath = Path.GetFullPath(Path.Combine(RepositoryRootPath, relativePath));
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(absolutePath, content);
            return absolutePath;
        }

        public PersistedImportedSeries CreateSeries(IReadOnlyList<PersistedImportedSeriesInstance>? instances = null, string studyInstanceUid = "1.2.3.4", int seriesId = 17, string seriesInstanceUid = "1.2.3.4.5") => new(seriesId, seriesInstanceUid, 7, studyInstanceUid, instances ?? Array.Empty<PersistedImportedSeriesInstance>());

        public PersistedImportedSeriesInstance CreateInstance(int instanceId, string relativeFilePath, int? instanceNumber = null, bool repositoryLocationIsEnabled = true, string? repositoryRootPath = null) => new(instanceId, $"1.2.840.{instanceId}", instanceNumber, 5, repositoryLocationIsEnabled, repositoryRootPath ?? RepositoryRootPath, relativeFilePath);

        public void SetupSeries(PersistedImportedSeries? series) => Repository.Setup(value => value.GetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(series);
    }
}