using FellowOakDicom;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    public sealed partial class RepositoryIntegrationTests
    {
        private async Task<RepositoryTestScenario> CreateRepositoryScenarioAsync(string fileName = "Image.dcm", bool createDicomFile = true, DicomUID? studyInstanceUid = null, DicomUID? seriesInstanceUid = null, DicomUID? sopInstanceUid = null)
        {
            RepositoryTestScenario scenario = new(this);
            await scenario.InitializeAsync(fileName, createDicomFile, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            return scenario;
        }

        private sealed class RepositoryTestScenario(RepositoryIntegrationTests owner) : IDisposable
        {
            public DicomRepositoryPathInfo PathInfo { get; private set; } = default!;
            public string? RepositoryStudyDirectory { get; set; }
            public DicomUID SeriesInstanceUid { get; private set; } = DicomUID.Generate();
            public DicomUID SopInstanceUid { get; private set; } = DicomUID.Generate();
            public string SourceDirectory { get; } = CreateTemporaryDirectory();

            public string SourceFilePath { get; private set; } = string.Empty;

            public DicomUID StudyInstanceUid { get; private set; } = DicomUID.Generate();

            public void Dispose()
            {
                DeleteDirectory(SourceDirectory);
                DeleteDirectory(RepositoryStudyDirectory);
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
        }
    }
}