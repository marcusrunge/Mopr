using FellowOakDicom;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    public sealed partial class RepositoryIntegrationTests
    {
        private DicomRepositoryRepairRequest CreateRepairRequest(bool repairMissingFiles = true) => new()
        {
            VerifyFiles = true,
            RepairMissingFiles = repairMissingFiles,
            RebuildRepositoryIndex = false,
            RepositoryLocationId = _fixture.RepositoryLocation!.Id
        };

        private static DicomRepositoryRepairRequest CreateAllLocationsRepairRequest(bool repairMissingFiles = true) => new()
        {
            VerifyFiles = true,
            RepairMissingFiles = repairMissingFiles,
            RebuildRepositoryIndex = false,
            RepositoryLocationId = null
        };

        private async Task<RepositoryTestScenario> CreateRepositoryScenarioAsync(string fileName = "Image.dcm", bool createDicomFile = true, DicomUID? studyInstanceUid = null, DicomUID? seriesInstanceUid = null, DicomUID? sopInstanceUid = null, RepositoryLocation? repositoryLocation = null)
        {
            RepositoryTestScenario scenario = new(this);
            await scenario.InitializeAsync(fileName, createDicomFile, studyInstanceUid, seriesInstanceUid, sopInstanceUid, repositoryLocation ?? _fixture.RepositoryLocation!);
            return scenario;
        }

        private async Task<DicomRepositoryRepairResult> RepairAsync(bool repairMissingFiles = true) => await RepairAsync(CreateRepairRequest(repairMissingFiles));

        private async Task<DicomRepositoryRepairResult> RepairAsync(DicomRepositoryRepairRequest request) => await _fixture.Repository!.RepositoryRepairService!.RepairAsync(request, TestContext.Current.CancellationToken);

        private async Task<RepositoryLocation> SetSecondaryRepositoryLocationStateAsync(bool isEnabled, string? rootPath = null)
        {
            RepositoryLocation repositoryLocation = await _fixture.Persistence!.RepositoryLocation!.GetByIdAsync(_fixture.SecondaryRepositoryLocation!.Id, TestContext.Current.CancellationToken)                ?? throw new InvalidOperationException("The secondary repository test location does not exist.");

            repositoryLocation.IsEnabled = isEnabled;
            repositoryLocation.RootPath = rootPath ?? _fixture.SecondaryRepositoryRootPath;

            /*
             * The secondary location is never the default. This allows activation and
             * deactivation tests without violating the invariant that a default
             * repository location must remain enabled.
             */
            Assert.False(repositoryLocation.IsDefault);

            await _fixture.Persistence.RepositoryLocation.UpdateAsync(repositoryLocation, TestContext.Current.CancellationToken);

            RepositoryLocation updated = await _fixture.Persistence.RepositoryLocation.GetByIdAsync(repositoryLocation.Id, TestContext.Current.CancellationToken)                ?? throw new InvalidOperationException("The updated secondary repository test location could not be loaded.");

            Assert.Equal(isEnabled, updated.IsEnabled);
            Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath ?? _fixture.SecondaryRepositoryRootPath)), updated.RootPath);

            return updated;
        }

        private sealed class RepositoryTestScenario(RepositoryIntegrationTests owner) : IDisposable
        {
            public RepositoryLocation RepositoryLocation { get; private set; } = default!;
            public DicomRepositoryPathInfo PathInfo { get; private set; } = default!;
            public string? RepositoryStudyDirectory { get; set; }
            public DicomUID SeriesInstanceUid { get; private set; } = DicomUID.Generate();
            public DicomUID SopInstanceUid { get; private set; } = DicomUID.Generate();
            public string SourceDirectory { get; } = CreateTemporaryDirectory();
            public string SourceFilePath { get; private set; } = string.Empty;
            public DicomUID StudyInstanceUid { get; private set; } = DicomUID.Generate();

            public async Task<string> CopyRepositoryFileAsync(string directoryName = "Duplicate", string fileName = "DuplicateImage.bin")
            {
                string directory = Path.Combine(RepositoryStudyDirectory!, directoryName);
                string filePath = Path.Combine(directory, fileName);
                await Task.Run(() =>
                {
                    Directory.CreateDirectory(directory);
                    File.Copy(PathInfo.AbsolutePath, filePath);
                }, TestContext.Current.CancellationToken);
                return filePath;
            }

            public async Task<string> CreateIncompleteImportFileAsync(string fileName = "Interrupted.dcm.importing", string content = "Incomplete import data")
            {
                string directory = Path.GetDirectoryName(PathInfo.AbsolutePath)!;
                string filePath = Path.Combine(directory, fileName);
                await Task.Run(() => Directory.CreateDirectory(directory), TestContext.Current.CancellationToken);
                await File.WriteAllTextAsync(filePath, content, TestContext.Current.CancellationToken);
                return filePath;
            }

            public async Task CreateOrphanedRepositoryFileAsync()
            {
                string directory = Path.GetDirectoryName(PathInfo.AbsolutePath)!;
                await Task.Run(() => Directory.CreateDirectory(directory), TestContext.Current.CancellationToken);
                await CreateDicomFileAsync(PathInfo.AbsolutePath, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid);
            }

            public void Dispose()
            {
                DeleteDirectory(SourceDirectory);
                DeleteDirectory(RepositoryStudyDirectory);
            }

            public async Task<Instance> GetPersistedInstanceAsync() => Assert.IsType<Instance>(await TryGetPersistedInstanceAsync());

            public async Task<DicomImportResult> ImportSuccessfullyAsync(bool allowOverwrite = false)
            {
                DicomImportResult result = await owner.ImportAsync(SourceDirectory, allowOverwrite, RepositoryLocation.Id);

                Assert.Equal(1, result.ImportedFiles);
                Assert.Equal(0, result.FailedFiles);
                Assert.Empty(result.Errors);
                Assert.True(File.Exists(PathInfo.AbsolutePath));

                return result;
            }

            public async Task InitializeAsync(string fileName, bool createDicomFile, DicomUID? studyInstanceUid, DicomUID? seriesInstanceUid, DicomUID? sopInstanceUid, RepositoryLocation repositoryLocation)
            {
                ArgumentNullException.ThrowIfNull(repositoryLocation);

                StudyInstanceUid = studyInstanceUid ?? DicomUID.Generate();
                SeriesInstanceUid = seriesInstanceUid ?? DicomUID.Generate();
                SopInstanceUid = sopInstanceUid ?? DicomUID.Generate();
                RepositoryLocation = repositoryLocation;
                SourceFilePath = Path.Combine(SourceDirectory, fileName);

                if (createDicomFile)
                {
                    await CreateDicomFileAsync(SourceFilePath, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid);
                }

                PathInfo = owner.CreatePathInfo(RepositoryLocation, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid);
                RepositoryStudyDirectory = GetRepositoryStudyDirectory(PathInfo);
            }

            public FileStream LockRepositoryFile() => new(PathInfo.AbsolutePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            public async Task<string> MoveRepositoryFileAsync(string directoryName = "Misplaced", string fileName = "RenamedImage.bin")
            {
                string directory = Path.Combine(RepositoryStudyDirectory!, directoryName);
                string filePath = Path.Combine(directory, fileName);
                await Task.Run(() =>
                {
                    Directory.CreateDirectory(directory);
                    File.Move(PathInfo.AbsolutePath, filePath);
                }, TestContext.Current.CancellationToken);
                return filePath;
            }

            public async Task ReplaceRepositoryFileAsync(DicomUID sopInstanceUid) => await CreateDicomFileAsync(PathInfo.AbsolutePath, StudyInstanceUid, SeriesInstanceUid, sopInstanceUid);

            public async Task ReplaceRepositoryFileHierarchyAsync(DicomUID studyInstanceUid, DicomUID seriesInstanceUid)
            {
                /*
                 * The SOP Instance UID remains unchanged so the repository still
                 * identifies the expected instance. Only the DICOM Study-Series hierarchy
                 * is changed to create a controlled relationship conflict.
                 */
                await CreateDicomFileAsync(PathInfo.AbsolutePath, studyInstanceUid, seriesInstanceUid, SopInstanceUid);
            }

            public async Task ReplaceRepositoryFileWithInvalidContentAsync(string content = "This is not a valid DICOM file.") => await File.WriteAllTextAsync(PathInfo.AbsolutePath, content, TestContext.Current.CancellationToken);

            public async Task<Instance> SetRelativeFilePathAsync(string relativeFilePath)
            {
                Instance instance = await GetPersistedInstanceAsync();
                instance.RelativeFilePath = relativeFilePath;

                // Persistence queries are detached; UpdateAsync persists the deliberate test state.
                await owner._fixture.Persistence!.Instance!.UpdateAsync(instance, TestContext.Current.CancellationToken);

                Instance updated = await GetPersistedInstanceAsync();
                Assert.Equal(relativeFilePath, updated.RelativeFilePath);
                return updated;
            }

            public async Task<Instance?> TryGetPersistedInstanceAsync() => await owner._fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(SopInstanceUid.UID, TestContext.Current.CancellationToken);
        }
    }
}