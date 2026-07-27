using FellowOakDicom;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    public sealed partial class RepositoryIntegrationTests
    {
        private static DicomRepositoryRepairRequest CreateRepairRequest(bool repairMissingFiles = true) => new()
        {
            VerifyFiles = true,
            RepairMissingFiles = repairMissingFiles,
            RebuildRepositoryIndex = false
        };

        private async Task<RepositoryTestScenario> CreateRepositoryScenarioAsync(string fileName = "Image.dcm", bool createDicomFile = true, DicomUID? studyInstanceUid = null, DicomUID? seriesInstanceUid = null, DicomUID? sopInstanceUid = null)
        {
            RepositoryTestScenario scenario = new(this);
            await scenario.InitializeAsync(fileName, createDicomFile, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            return scenario;
        }

        private async Task<DicomRepositoryRepairResult> RepairAsync(bool repairMissingFiles = true) => await RepairAsync(CreateRepairRequest(repairMissingFiles));

        private async Task<DicomRepositoryRepairResult> RepairAsync(DicomRepositoryRepairRequest request) => await _fixture.Repository!.RepositoryRepairService!.RepairAsync(request, TestContext.Current.CancellationToken);

        private sealed class RepositoryTestScenario(RepositoryIntegrationTests owner) : IDisposable
        {
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
                DicomImportResult result = await owner.ImportAsync(SourceDirectory, allowOverwrite);
                Assert.Equal(1, result.ImportedFiles);
                Assert.Equal(0, result.FailedFiles);
                Assert.Empty(result.Errors);
                Assert.True(File.Exists(PathInfo.AbsolutePath));
                return result;
            }

            public async Task InitializeAsync(string fileName = "Image.dcm", bool createDicomFile = true, DicomUID? studyInstanceUid = null, DicomUID? seriesInstanceUid = null, DicomUID? sopInstanceUid = null)
            {
                StudyInstanceUid = studyInstanceUid ?? DicomUID.Generate();
                SeriesInstanceUid = seriesInstanceUid ?? DicomUID.Generate();
                SopInstanceUid = sopInstanceUid ?? DicomUID.Generate();
                SourceFilePath = Path.Combine(SourceDirectory, fileName);

                if (createDicomFile)
                {
                    await CreateDicomFileAsync(SourceFilePath, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid);
                }

                PathInfo = owner.CreatePathInfo(StudyInstanceUid, SeriesInstanceUid, SopInstanceUid);
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