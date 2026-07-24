using FellowOakDicom;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    [TestCaseOrderer(typeof(PriorityOrderer))]
    public sealed partial class RepositoryIntegrationTests(RepositoryFixture fixture) : IClassFixture<RepositoryFixture>
    {
        private readonly RepositoryFixture _fixture = fixture;

        [Fact, Priority(1)]
        public void Services_Should_Be_Available()
        {
            Assert.NotNull(_fixture.Repository);
            Assert.NotNull(_fixture.Repository!.RepositoryService);
            Assert.NotNull(_fixture.Repository.ImportService);
            Assert.NotNull(_fixture.Repository.RepositoryRepairService);
        }

        [Fact, Priority(2)]
        public void CreateRelativePath_Should_Create_Expected_Path()
        {
            string path = _fixture.Repository!.RepositoryService!.CreateRelativePath("1.2.3", "4.5.6", "7.8.9");
            string expected = Path.Combine("1.2.3", "4.5.6", "7.8.9.dcm");

            Assert.Equal(expected, path);
        }

        [Fact, Priority(3)]
        public void CreatePathInfo_Should_Create_PathInformation()
        {
            DicomRepositoryPathInfo pathInfo = _fixture.Repository!.RepositoryService!.CreatePathInfo("1.2.3", "4.5.6", "7.8.9");

            Assert.Equal("1.2.3", pathInfo.StudyInstanceUid);
            Assert.Equal("4.5.6", pathInfo.SeriesInstanceUid);
            Assert.Equal("7.8.9", pathInfo.SopInstanceUid);
            Assert.Equal(Path.Combine("1.2.3", "4.5.6", "7.8.9.dcm"), pathInfo.RelativePath);
            Assert.NotEmpty(pathInfo.AbsolutePath);
        }

        [Fact, Priority(4)]
        public void Exists_Should_Return_False_For_Missing_File()
        {
            bool exists = _fixture.Repository!.RepositoryService!.Exists(Path.Combine("DoesNotExist", "File.dcm"));

            Assert.False(exists);
        }

        [Fact, Priority(5)]
        public void GetAbsolutePath_Should_Combine_Repository_And_Relative_Path()
        {
            string relativePath = Path.Combine("1.2.3", "4.5.6", "7.8.9.dcm");
            string absolutePath = _fixture.Repository!.RepositoryService!.GetAbsolutePath(relativePath);

            Assert.Contains(relativePath, absolutePath);
        }

        [Fact, Priority(6)]
        public async Task Import_Should_Return_Error_For_Missing_Directory()
        {
            DicomImportResult result = await ImportAsync(Guid.NewGuid().ToString("N"));

            Assert.Equal(0, result.DiscoveredFiles);
            Assert.Equal(0, result.ValidDicomFiles);
            Assert.Equal(0, result.ImportableFiles);
            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(1, result.FailedFiles);
            Assert.NotEmpty(result.Errors);
        }

        [Fact, Priority(7)]
        public async Task Import_Should_Count_Files()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);

            File.WriteAllText(Path.Combine(scenario.SourceDirectory, "A.dcm"), "Test");
            File.WriteAllText(Path.Combine(scenario.SourceDirectory, "B.dcm"), "Test");
            File.WriteAllText(Path.Combine(scenario.SourceDirectory, "C.dcm"), "Test");

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(3, result.DiscoveredFiles);
            Assert.Equal(0, result.ValidDicomFiles);
            Assert.Equal(0, result.ImportableFiles);
            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(3, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);
        }

        [Fact, Priority(8)]
        public async Task Import_Should_Count_Files_In_SubDirectories()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string childDirectory = Path.Combine(scenario.SourceDirectory, "Child");

            Directory.CreateDirectory(childDirectory);
            File.WriteAllText(Path.Combine(scenario.SourceDirectory, "A.dcm"), "Test");
            File.WriteAllText(Path.Combine(childDirectory, "B.dcm"), "Test");

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(2, result.DiscoveredFiles);
            Assert.Equal(0, result.ValidDicomFiles);
            Assert.Equal(0, result.ImportableFiles);
            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(2, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);
        }

        [Fact, Priority(9)]
        public async Task Import_Should_Find_No_Dicom_Files()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);

            File.WriteAllText(Path.Combine(scenario.SourceDirectory, "A.txt"), "Test");
            File.WriteAllText(Path.Combine(scenario.SourceDirectory, "B.txt"), "Test");
            File.WriteAllText(Path.Combine(scenario.SourceDirectory, "C.txt"), "Test");

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(3, result.DiscoveredFiles);
            Assert.Equal(0, result.ValidDicomFiles);
            Assert.Equal(0, result.ImportableFiles);
            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(3, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);
        }

        [Fact, Priority(10)]
        public async Task Import_Should_Find_Valid_Dicom_File()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string filePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(filePath, null, null, sopInstanceUid);

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, result.DiscoveredFiles);
            Assert.Equal(1, result.ValidDicomFiles);
            Assert.Equal(0, result.ImportableFiles);
            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(1, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);

            DicomImportFileInfo fileInfo = Assert.Single(result.Files);

            Assert.True(fileInfo.IsDicomFile);
            Assert.False(fileInfo.IsImportable);
            Assert.Equal("Image.dcm", fileInfo.FileName);
            Assert.Equal(filePath, fileInfo.FilePath);
            Assert.Equal(sopInstanceUid.UID, fileInfo.SopInstanceUid);
            Assert.Empty(fileInfo.StudyInstanceUid);
            Assert.Empty(fileInfo.SeriesInstanceUid);
            Assert.Empty(fileInfo.RelativeRepositoryPath);
        }

        [Fact, Priority(11)]
        public async Task Import_Should_Read_Dicom_Instance_Uids()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, result.DiscoveredFiles);
            Assert.Equal(1, result.ValidDicomFiles);
            Assert.Equal(1, result.ImportableFiles);
            Assert.Equal(1, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);

            DicomImportFileInfo fileInfo = Assert.Single(result.Files);

            Assert.True(fileInfo.IsDicomFile);
            Assert.True(fileInfo.IsImportable);
            Assert.Equal(studyInstanceUid.UID, fileInfo.StudyInstanceUid);
            Assert.Equal(seriesInstanceUid.UID, fileInfo.SeriesInstanceUid);
            Assert.Equal(sopInstanceUid.UID, fileInfo.SopInstanceUid);
            Assert.Equal(pathInfo.RelativePath, fileInfo.RelativeRepositoryPath);
            Assert.True(File.Exists(pathInfo.AbsolutePath));
        }

        [Fact, Priority(12)]
        public async Task Import_Should_Distinguish_Dicom_And_Non_Dicom_Files()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string childDirectory = Path.Combine(scenario.SourceDirectory, "Child");
            string dicomFilePath = Path.Combine(childDirectory, "Image.dcm");
            string textFilePath = Path.Combine(scenario.SourceDirectory, "Readme.txt");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            Directory.CreateDirectory(childDirectory);

            await CreateDicomFileAsync(dicomFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            await File.WriteAllTextAsync(textFilePath, "This is not a DICOM file.", TestContext.Current.CancellationToken);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(2, result.DiscoveredFiles);
            Assert.Equal(1, result.ValidDicomFiles);
            Assert.Equal(1, result.ImportableFiles);
            Assert.Equal(1, result.ImportedFiles);
            Assert.Equal(1, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);
            DicomImportFileInfo importedFile = Assert.Single(result.Files, fileInfo => fileInfo.FilePath == dicomFilePath);
            DicomImportFileInfo skippedFile = Assert.Single(result.Files, fileInfo => fileInfo.FilePath == textFilePath);
            Assert.True(importedFile.IsDicomFile);
            Assert.True(importedFile.IsImportable);
            Assert.Equal(pathInfo.RelativePath, importedFile.RelativeRepositoryPath);
            Assert.True(File.Exists(pathInfo.AbsolutePath));
            Assert.False(skippedFile.IsDicomFile);
            Assert.False(skippedFile.IsImportable);
            Assert.Empty(skippedFile.RelativeRepositoryPath);
        }

        [Fact, Priority(13)]
        public async Task Import_Should_Not_Mark_Dicom_File_As_Importable_When_Series_Uid_Is_Missing()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string filePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(filePath, studyInstanceUid, null, sopInstanceUid);

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, result.DiscoveredFiles);
            Assert.Equal(1, result.ValidDicomFiles);
            Assert.Equal(0, result.ImportableFiles);
            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(1, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);

            DicomImportFileInfo fileInfo = Assert.Single(result.Files);

            Assert.True(fileInfo.IsDicomFile);
            Assert.False(fileInfo.IsImportable);
            Assert.Equal(studyInstanceUid.UID, fileInfo.StudyInstanceUid);
            Assert.Empty(fileInfo.SeriesInstanceUid);
            Assert.Equal(sopInstanceUid.UID, fileInfo.SopInstanceUid);
            Assert.Empty(fileInfo.RelativeRepositoryPath);
        }

        [Fact, Priority(14)]
        public async Task Import_Should_Copy_Importable_Dicom_File_To_Repository()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, result.DiscoveredFiles);
            Assert.Equal(1, result.ValidDicomFiles);
            Assert.Equal(1, result.ImportableFiles);
            Assert.Equal(1, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);

            DicomImportFileInfo fileInfo = Assert.Single(result.Files);

            Assert.Equal(pathInfo.RelativePath, fileInfo.RelativeRepositoryPath);
            Assert.True(File.Exists(pathInfo.AbsolutePath));

            byte[] sourceBytes = await File.ReadAllBytesAsync(sourceFilePath, TestContext.Current.CancellationToken);
            byte[] destinationBytes = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);

            Assert.Equal(sourceBytes, destinationBytes);
        }

        [Fact, Priority(15)]
        public async Task Import_Should_Skip_Existing_Identical_Dicom_File()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult firstResult = await ImportAsync(scenario.SourceDirectory);
            DicomImportResult secondResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, firstResult.ImportedFiles);
            Assert.Equal(0, firstResult.SkippedFiles);
            Assert.Equal(0, firstResult.FailedFiles);
            Assert.Empty(firstResult.Errors);

            Assert.Equal(1, secondResult.DiscoveredFiles);
            Assert.Equal(1, secondResult.ValidDicomFiles);
            Assert.Equal(1, secondResult.ImportableFiles);
            Assert.Equal(0, secondResult.ImportedFiles);
            Assert.Equal(1, secondResult.SkippedFiles);
            Assert.Equal(0, secondResult.FailedFiles);
            Assert.Empty(secondResult.Errors);

            DicomImportFileInfo fileInfo = Assert.Single(secondResult.Files);

            Assert.Equal(pathInfo.RelativePath, fileInfo.RelativeRepositoryPath);
            Assert.True(File.Exists(pathInfo.AbsolutePath));

            byte[] sourceBytes = await File.ReadAllBytesAsync(sourceFilePath, TestContext.Current.CancellationToken);
            byte[] destinationBytes = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);

            Assert.Equal(sourceBytes, destinationBytes);
        }

        [Fact, Priority(16)]
        public async Task Import_Should_Report_Conflict_When_Existing_Dicom_File_Is_Different()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult firstResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, firstResult.ImportedFiles);
            Assert.Equal(0, firstResult.SkippedFiles);
            Assert.Equal(0, firstResult.FailedFiles);
            Assert.Empty(firstResult.Errors);
            Assert.True(File.Exists(pathInfo.AbsolutePath));

            byte[] originalRepositoryBytes = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);

            DicomFile changedDicomFile = await DicomFile.OpenAsync(sourceFilePath);
            changedDicomFile.Dataset.AddOrUpdate(DicomTag.StudyDescription, "Changed study description");
            await changedDicomFile.SaveAsync(sourceFilePath);

            DicomImportResult secondResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, secondResult.DiscoveredFiles);
            Assert.Equal(1, secondResult.ValidDicomFiles);
            Assert.Equal(1, secondResult.ImportableFiles);
            Assert.Equal(0, secondResult.ImportedFiles);
            Assert.Equal(0, secondResult.SkippedFiles);
            Assert.Equal(1, secondResult.FailedFiles);
            Assert.Single(secondResult.Errors);

            DicomImportFileInfo fileInfo = Assert.Single(secondResult.Files);

            Assert.True(fileInfo.IsDicomFile);
            Assert.True(fileInfo.IsImportable);
            Assert.Empty(fileInfo.RelativeRepositoryPath);

            byte[] currentRepositoryBytes = await File.ReadAllBytesAsync(
                pathInfo.AbsolutePath,
                TestContext.Current.CancellationToken);

            Assert.Equal(originalRepositoryBytes, currentRepositoryBytes);
        }

        [Fact, Priority(17)]
        public async Task Import_Should_Overwrite_Different_Dicom_File_When_Allowed()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult firstResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, firstResult.ImportedFiles);
            Assert.Equal(0, firstResult.SkippedFiles);
            Assert.Equal(0, firstResult.FailedFiles);
            Assert.Empty(firstResult.Errors);
            Assert.True(File.Exists(pathInfo.AbsolutePath));

            byte[] originalRepositoryBytes = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);

            DicomFile changedDicomFile = DicomFile.Open(sourceFilePath);
            changedDicomFile.Dataset.AddOrUpdate(DicomTag.StudyDescription, "Changed study description");

            await changedDicomFile.SaveAsync(sourceFilePath);

            byte[] changedSourceBytes = await File.ReadAllBytesAsync(sourceFilePath, TestContext.Current.CancellationToken);

            Assert.NotEqual(originalRepositoryBytes, changedSourceBytes);

            DicomImportResult secondResult = await ImportAsync(scenario.SourceDirectory, allowOverwrite: true);

            Assert.Equal(1, secondResult.DiscoveredFiles);
            Assert.Equal(1, secondResult.ValidDicomFiles);
            Assert.Equal(1, secondResult.ImportableFiles);
            Assert.Equal(1, secondResult.ImportedFiles);
            Assert.Equal(0, secondResult.SkippedFiles);
            Assert.Equal(0, secondResult.FailedFiles);
            Assert.Empty(secondResult.Errors);

            DicomImportFileInfo fileInfo = Assert.Single(secondResult.Files);

            Assert.Equal(pathInfo.RelativePath, fileInfo.RelativeRepositoryPath);
            Assert.True(File.Exists(pathInfo.AbsolutePath));

            byte[] currentRepositoryBytes = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);

            Assert.Equal(changedSourceBytes, currentRepositoryBytes);
            Assert.NotEqual(originalRepositoryBytes, currentRepositoryBytes);
        }

        [Fact, Priority(18)]
        public async Task Import_Should_Persist_Study_Series_And_Instance()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);

            Assert.True(result.FailedFiles == 0, string.Join(Environment.NewLine, result.Errors));

            Assert.Empty(result.Errors);
            Assert.NotNull(_fixture.Persistence);
            Assert.NotNull(_fixture.TestUser);

            Study? study = await _fixture.Persistence!.Study!.GetByStudyInstanceUidAsync(studyInstanceUid.UID, TestContext.Current.CancellationToken);

            Assert.NotNull(study);
            Assert.True(study.Id > 0);
            Assert.Equal(studyInstanceUid.UID, study.StudyInstanceUid);
            Assert.Equal(_fixture.TestUser!.Id, study.CreatedByUserId);

            Series? series = await _fixture.Persistence.Series!.GetBySeriesInstanceUidAsync(seriesInstanceUid.UID, TestContext.Current.CancellationToken);

            Assert.NotNull(series);
            Assert.True(series.Id > 0);
            Assert.Equal(seriesInstanceUid.UID, series.SeriesInstanceUid);
            Assert.Equal(study.Id, series.StudyId);
            Assert.Equal(_fixture.TestUser.Id, series.CreatedByUserId);

            Instance? instance = await _fixture.Persistence.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);

            Assert.NotNull(instance);
            Assert.True(instance.Id > 0);
            Assert.Equal(sopInstanceUid.UID, instance.SopInstanceUid);
            Assert.Equal(series.Id, instance.SeriesId);
            Assert.Equal(pathInfo.RelativePath, instance.RelativeFilePath);
            Assert.Equal(_fixture.TestUser.Id, instance.CreatedByUserId);

            Assert.True(File.Exists(pathInfo.AbsolutePath));
        }

        [Fact, Priority(19)]
        public async Task Import_Should_Not_Create_Duplicate_Persistence_Entities()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult firstResult = await ImportAsync(scenario.SourceDirectory);
            DicomImportResult secondResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, firstResult.ImportedFiles);
            Assert.Equal(0, firstResult.SkippedFiles);
            Assert.Equal(0, firstResult.FailedFiles);
            Assert.Empty(firstResult.Errors);

            Assert.Equal(0, secondResult.ImportedFiles);
            Assert.Equal(1, secondResult.SkippedFiles);
            Assert.Equal(0, secondResult.FailedFiles);
            Assert.Empty(secondResult.Errors);

            Study? study = await _fixture.Persistence!.Study!.GetByStudyInstanceUidAsync(studyInstanceUid.UID, TestContext.Current.CancellationToken);

            Assert.NotNull(study);

            Series? series = await _fixture.Persistence.Series!.GetBySeriesInstanceUidAsync(seriesInstanceUid.UID, TestContext.Current.CancellationToken);

            Assert.NotNull(series);
            Assert.Equal(study.Id, series.StudyId);

            Instance? instance = await _fixture.Persistence.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);

            Assert.NotNull(instance);
            Assert.Equal(series.Id, instance.SeriesId);
            Assert.Equal(pathInfo.RelativePath, instance.RelativeFilePath);

            IList<Series> studySeries = await _fixture.Persistence.Series.GetByStudyIdAsync(study.Id, TestContext.Current.CancellationToken);

            Assert.Single(studySeries, item => item.SeriesInstanceUid == seriesInstanceUid.UID);

            IList<Instance> seriesInstances = await _fixture.Persistence.Instance.GetBySeriesIdAsync(series.Id, TestContext.Current.CancellationToken);

            Assert.Single(seriesInstances, item => item.SopInstanceUid == sopInstanceUid.UID);

            Assert.True(File.Exists(pathInfo.AbsolutePath));
        }

        [Fact, Priority(20)]
        public async Task Repair_Should_Detect_Missing_Repository_File()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = false,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult importResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, importResult.ImportedFiles);
            Assert.Equal(0, importResult.FailedFiles);
            Assert.Empty(importResult.Errors);
            Assert.True(File.Exists(pathInfo.AbsolutePath));
            Instance? instance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);
            Assert.NotNull(instance);
            Assert.Equal(pathInfo.RelativePath, instance.RelativeFilePath);
            File.Delete(pathInfo.AbsolutePath);
            Assert.False(File.Exists(pathInfo.AbsolutePath));
            DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
            Assert.Equal(initialRepairResult.ScannedFiles + 1, repairResult.ScannedFiles);
            Assert.Equal(initialRepairResult.MissingFiles + 1, repairResult.MissingFiles);
            Assert.Equal(initialRepairResult.RepairedFiles, repairResult.RepairedFiles);
            Assert.Empty(repairResult.Errors);
        }

        [Fact, Priority(21)]
        public async Task Repair_Should_Detect_Misplaced_Repository_File()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = false,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult importResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, importResult.ImportedFiles);
            Assert.Equal(0, importResult.FailedFiles);
            Assert.Empty(importResult.Errors);
            Assert.True(File.Exists(pathInfo.AbsolutePath));
            string misplacedDirectory = Path.Combine(scenario.RepositoryStudyDirectory!, "Misplaced");
            Directory.CreateDirectory(misplacedDirectory);
            string? misplacedFilePath = Path.Combine(misplacedDirectory, "RenamedImage.bin"); File.Move(pathInfo.AbsolutePath, misplacedFilePath);
            Assert.False(File.Exists(pathInfo.AbsolutePath));
            Assert.True(File.Exists(misplacedFilePath));
            DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
            Assert.Equal(initialRepairResult.ScannedFiles + 1, repairResult.ScannedFiles);
            Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);
            Assert.Equal(initialRepairResult.MisplacedFiles + 1, repairResult.MisplacedFiles);
            Assert.Equal(initialRepairResult.RepairedFiles, repairResult.RepairedFiles);
            Assert.Empty(repairResult.Errors);
            Assert.False(File.Exists(pathInfo.AbsolutePath));
            Assert.True(File.Exists(misplacedFilePath));
        }

        [Fact, Priority(22)]
        public async Task Repair_Should_Move_Misplaced_File_To_Expected_Path()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            DicomRepositoryRepairRequest detectionRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = false,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(detectionRequest, TestContext.Current.CancellationToken);

            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult importResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, importResult.ImportedFiles);
            Assert.Equal(0, importResult.FailedFiles);
            Assert.Empty(importResult.Errors);
            Assert.True(File.Exists(pathInfo.AbsolutePath));
            string misplacedDirectory = Path.Combine(scenario.RepositoryStudyDirectory!, "Misplaced");
            Directory.CreateDirectory(misplacedDirectory);
            string misplacedFilePath = Path.Combine(misplacedDirectory, "RenamedImage.bin");
            File.Move(pathInfo.AbsolutePath, misplacedFilePath);
            Assert.False(File.Exists(pathInfo.AbsolutePath));
            Assert.True(File.Exists(misplacedFilePath));
            DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(new DicomRepositoryRepairRequest
            {
                VerifyFiles = true,
                RepairMissingFiles = true,
                RebuildRepositoryIndex = false
            }, TestContext.Current.CancellationToken);
            Assert.Equal(initialRepairResult.ScannedFiles + 1, repairResult.ScannedFiles);
            Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);
            Assert.Equal(initialRepairResult.RepairedFiles + 1, repairResult.RepairedFiles);
            Assert.Equal(initialRepairResult.MisplacedFiles + 1, repairResult.MisplacedFiles);
            Assert.Empty(repairResult.Errors);
            Assert.True(File.Exists(pathInfo.AbsolutePath));
            Assert.False(File.Exists(misplacedFilePath));

            byte[] sourceBytes = await File.ReadAllBytesAsync(sourceFilePath, TestContext.Current.CancellationToken);

            byte[] repairedBytes = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);

            Assert.Equal(sourceBytes, repairedBytes);
        }

        [Fact, Priority(23)]
        public async Task Repair_Should_Detect_Duplicate_Sop_Instance_File()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = false,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult importResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, importResult.ImportedFiles);
            Assert.Equal(0, importResult.FailedFiles);
            Assert.Empty(importResult.Errors);
            Assert.True(File.Exists(pathInfo.AbsolutePath));

            string duplicateDirectory = Path.Combine(scenario.RepositoryStudyDirectory!, "Duplicate");
            string duplicateFilePath = Path.Combine(duplicateDirectory, "DuplicateImage.bin");

            await Task.Run(() =>
            {
                Directory.CreateDirectory(duplicateDirectory);

                File.Copy(pathInfo.AbsolutePath, duplicateFilePath);
            }, TestContext.Current.CancellationToken);

            Assert.True(File.Exists(pathInfo.AbsolutePath));
            Assert.True(File.Exists(duplicateFilePath));
            DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
            Assert.Equal(initialRepairResult.ScannedFiles + 1, repairResult.ScannedFiles);
            Assert.Equal(initialRepairResult.DuplicateFiles + 1, repairResult.DuplicateFiles);
            Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);
            Assert.Equal(initialRepairResult.MisplacedFiles, repairResult.MisplacedFiles);
            Assert.Equal(initialRepairResult.RepairedFiles, repairResult.RepairedFiles);
            Assert.Empty(repairResult.Errors);
            Assert.True(File.Exists(pathInfo.AbsolutePath));
            Assert.True(File.Exists(duplicateFilePath));
            byte[] canonicalBytes = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);
            byte[] duplicateBytes = await File.ReadAllBytesAsync(duplicateFilePath, TestContext.Current.CancellationToken);
            Assert.Equal(canonicalBytes, duplicateBytes);
        }

        [Fact, Priority(24)]
        public async Task Repair_Should_Detect_Identity_Mismatch_At_Expected_Path()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);

            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = false,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            string sourceFilePath = Path.Combine(scenario.SourceDirectory, "Image.dcm");
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            scenario.RepositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomImportResult importResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, importResult.ImportedFiles);
            Assert.Equal(0, importResult.FailedFiles);
            Assert.Empty(importResult.Errors);
            Assert.True(File.Exists(pathInfo.AbsolutePath));

            DicomUID differentSopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(pathInfo.AbsolutePath, studyInstanceUid, seriesInstanceUid, differentSopInstanceUid);

            DicomFile wrongFile = await Task.Run(() => DicomFile.Open(pathInfo.AbsolutePath), TestContext.Current.CancellationToken);

            string actualSopInstanceUid = wrongFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);

            Assert.Equal(differentSopInstanceUid.UID, actualSopInstanceUid);
            DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
            Assert.Equal(initialRepairResult.ScannedFiles + 1, repairResult.ScannedFiles);
            Assert.Equal(initialRepairResult.IdentityMismatchFiles + 1, repairResult.IdentityMismatchFiles);
            Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);
            Assert.Equal(initialRepairResult.MisplacedFiles, repairResult.MisplacedFiles);
            Assert.Equal(initialRepairResult.RepairedFiles, repairResult.RepairedFiles);
            Assert.Equal(initialRepairResult.DuplicateFiles, repairResult.DuplicateFiles);
            Assert.Equal(initialRepairResult.Errors.Count + 1, repairResult.Errors.Count);
            Assert.Single(repairResult.Errors, error => error.Contains(sopInstanceUid.UID, StringComparison.Ordinal) && error.Contains(differentSopInstanceUid.UID, StringComparison.Ordinal));
            Assert.True(File.Exists(pathInfo.AbsolutePath));
            DicomFile repositoryFile = await Task.Run(() => DicomFile.Open(pathInfo.AbsolutePath), TestContext.Current.CancellationToken);
            string repositorySopInstanceUid = repositoryFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
            Assert.Equal(differentSopInstanceUid.UID, repositorySopInstanceUid);
        }

        [Fact, Priority(25)]
        public async Task Repair_Should_Detect_Orphaned_Dicom_File()
        {
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();

            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            string? repositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = false,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            try
            {
                string? repositorySeriesDirectory = Path.GetDirectoryName(pathInfo.AbsolutePath);
                Assert.False(string.IsNullOrWhiteSpace(repositorySeriesDirectory));
                await Task.Run(() => Directory.CreateDirectory(repositorySeriesDirectory!), TestContext.Current.CancellationToken);
                await CreateDicomFileAsync(pathInfo.AbsolutePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                Instance? persistedInstance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);
                Assert.Null(persistedInstance);
                DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
                Assert.Equal(initialRepairResult.ScannedFiles, repairResult.ScannedFiles);
                Assert.Equal(initialRepairResult.OrphanedFiles + 1, repairResult.OrphanedFiles);
                Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);
                Assert.Equal(initialRepairResult.MisplacedFiles, repairResult.MisplacedFiles);
                Assert.Equal(initialRepairResult.IdentityMismatchFiles, repairResult.IdentityMismatchFiles);
                Assert.Equal(initialRepairResult.DuplicateFiles, repairResult.DuplicateFiles);
                Assert.Equal(initialRepairResult.RepairedFiles, repairResult.RepairedFiles);
                Assert.Equal(initialRepairResult.Errors.Count, repairResult.Errors.Count);
                Assert.True(File.Exists(pathInfo.AbsolutePath));
            }
            finally
            {
                DeleteDirectory(repositoryStudyDirectory);
            }
        }

        [Fact, Priority(26)]
        public async Task Repair_Should_Create_MissingFile_Issue()
        {
            string sourceDirectory = CreateTemporaryDirectory();
            string? repositoryStudyDirectory = null;

            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = false,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            try
            {
                string sourceFilePath = Path.Combine(sourceDirectory, "Image.dcm");

                DicomUID studyInstanceUid = DicomUID.Generate();
                DicomUID seriesInstanceUid = DicomUID.Generate();
                DicomUID sopInstanceUid = DicomUID.Generate();

                await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

                DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

                repositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

                DicomImportResult importResult = await ImportAsync(sourceDirectory);

                Assert.Equal(1, importResult.ImportedFiles);
                Assert.Equal(0, importResult.FailedFiles);
                Assert.Empty(importResult.Errors);
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                Instance? instance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);
                Assert.NotNull(instance);
                File.Delete(pathInfo.AbsolutePath);
                Assert.False(File.Exists(pathInfo.AbsolutePath));
                DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
                Assert.Equal(initialRepairResult.MissingFiles + 1, repairResult.MissingFiles);
                Assert.Equal(initialRepairResult.Issues.Count + 1, repairResult.Issues.Count);
                DicomRepositoryIssue issue = Assert.Single(repairResult.Issues, item => item.IssueType == DicomRepositoryIssueType.MissingFile && item.InstanceId == instance.Id && item.ExpectedSopInstanceUid == sopInstanceUid.UID);
                Assert.NotEqual(Guid.Empty, issue.Id);
                Assert.Equal(pathInfo.AbsolutePath, issue.ExpectedFilePath);
                Assert.Empty(issue.ActualFilePath);
                Assert.Empty(issue.ActualSopInstanceUid);
                Assert.False(issue.CanResolveAutomatically);
                Assert.False(issue.AutomaticallyResolved);
                Assert.Null(issue.ResolvedAtUtc);
                Assert.NotEqual(default, issue.DetectedAtUtc);
                Assert.Contains(instance.Id.ToString(), issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(sopInstanceUid.UID, issue.TechnicalDetails, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectory(sourceDirectory);
                DeleteDirectory(repositoryStudyDirectory);
            }
        }

        [Fact, Priority(27)]
        public async Task Repair_Should_Create_MisplacedFile_Issue()
        {
            string sourceDirectory = CreateTemporaryDirectory();
            string? repositoryStudyDirectory = null;

            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = false,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            try
            {
                string sourceFilePath = Path.Combine(sourceDirectory, "Image.dcm");

                DicomUID studyInstanceUid = DicomUID.Generate();
                DicomUID seriesInstanceUid = DicomUID.Generate();
                DicomUID sopInstanceUid = DicomUID.Generate();

                await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
                DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

                repositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

                DicomImportResult importResult = await ImportAsync(sourceDirectory);

                Assert.Equal(1, importResult.ImportedFiles);
                Assert.Equal(0, importResult.FailedFiles);
                Assert.Empty(importResult.Errors);
                Assert.True(File.Exists(pathInfo.AbsolutePath));

                Instance? instance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);

                Assert.NotNull(instance);

                string misplacedDirectory = Path.Combine(repositoryStudyDirectory!, "Misplaced");

                string misplacedFilePath = Path.Combine(misplacedDirectory, "RenamedImage.bin");

                await Task.Run(() =>
                {
                    Directory.CreateDirectory(misplacedDirectory);
                    File.Move(pathInfo.AbsolutePath, misplacedFilePath);
                },
                    TestContext.Current.CancellationToken);

                Assert.False(File.Exists(pathInfo.AbsolutePath));
                Assert.True(File.Exists(misplacedFilePath));

                DateTime checkStartedAtUtc = DateTime.UtcNow;

                DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

                DateTime checkCompletedAtUtc = DateTime.UtcNow;

                Assert.Equal(initialRepairResult.ScannedFiles + 1, repairResult.ScannedFiles);
                Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);
                Assert.Equal(initialRepairResult.MisplacedFiles + 1, repairResult.MisplacedFiles);
                Assert.Equal(initialRepairResult.RepairedFiles, repairResult.RepairedFiles);
                Assert.Equal(initialRepairResult.Issues.Count + 1, repairResult.Issues.Count);
                Assert.Empty(repairResult.Errors);

                DicomRepositoryIssue issue = Assert.Single(repairResult.Issues, item => item.IssueType == DicomRepositoryIssueType.MisplacedFile && item.InstanceId == instance.Id && item.ExpectedSopInstanceUid == sopInstanceUid.UID);

                Assert.NotEqual(Guid.Empty, issue.Id);
                Assert.Equal(pathInfo.AbsolutePath, issue.ExpectedFilePath);
                Assert.Equal(misplacedFilePath, issue.ActualFilePath);
                Assert.Equal(sopInstanceUid.UID, issue.ExpectedSopInstanceUid);
                Assert.Equal(sopInstanceUid.UID, issue.ActualSopInstanceUid);
                Assert.True(issue.CanResolveAutomatically);
                Assert.False(issue.AutomaticallyResolved);
                Assert.Null(issue.ResolvedAtUtc);
                Assert.InRange(issue.DetectedAtUtc, checkStartedAtUtc, checkCompletedAtUtc);
                Assert.Contains(instance.Id.ToString(), issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(sopInstanceUid.UID, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(misplacedFilePath, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(pathInfo.AbsolutePath, issue.TechnicalDetails, StringComparison.Ordinal);
                // Detection-only mode must not modify the repository.
                Assert.False(File.Exists(pathInfo.AbsolutePath));
                Assert.True(File.Exists(misplacedFilePath));
            }
            finally
            {
                DeleteDirectory(sourceDirectory);
                DeleteDirectory(repositoryStudyDirectory);
            }
        }

        [Fact, Priority(28)]
        public async Task Repair_Should_Create_AutomaticallyResolved_MisplacedFile_Issue()
        {
            string sourceDirectory = CreateTemporaryDirectory();
            string? repositoryStudyDirectory = null;

            DicomRepositoryRepairRequest initialRepairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = false,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(initialRepairRequest, TestContext.Current.CancellationToken);

            try
            {
                string sourceFilePath = Path.Combine(sourceDirectory, "Image.dcm");

                DicomUID studyInstanceUid = DicomUID.Generate();
                DicomUID seriesInstanceUid = DicomUID.Generate();
                DicomUID sopInstanceUid = DicomUID.Generate();

                await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

                DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

                repositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

                DicomImportResult importResult = await ImportAsync(sourceDirectory);

                Assert.Equal(1, importResult.ImportedFiles);
                Assert.Equal(0, importResult.FailedFiles);
                Assert.Empty(importResult.Errors);
                Assert.True(File.Exists(pathInfo.AbsolutePath));

                Instance? instance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);

                Assert.NotNull(instance);

                string misplacedDirectory = Path.Combine(repositoryStudyDirectory!, "Misplaced");

                string misplacedFilePath = Path.Combine(misplacedDirectory, "RenamedImage.bin");

                await Task.Run(() =>
                {
                    Directory.CreateDirectory(misplacedDirectory);
                    File.Move(pathInfo.AbsolutePath, misplacedFilePath);
                }, TestContext.Current.CancellationToken);

                Assert.False(File.Exists(pathInfo.AbsolutePath));
                Assert.True(File.Exists(misplacedFilePath));

                DicomRepositoryRepairRequest repairRequest = new()
                {
                    VerifyFiles = true,
                    RepairMissingFiles = true,
                    RebuildRepositoryIndex = false
                };

                DateTime repairStartedAtUtc = DateTime.UtcNow;
                DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
                DateTime repairCompletedAtUtc = DateTime.UtcNow;

                Assert.Equal(initialRepairResult.ScannedFiles + 1, repairResult.ScannedFiles);
                Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);

                /*
                 * MisplacedFiles counts every detected misplaced file, while
                 * RepairedFiles counts successfully completed repairs.
                */
                Assert.Equal(initialRepairResult.MisplacedFiles + 1, repairResult.MisplacedFiles);
                Assert.Equal(initialRepairResult.RepairedFiles + 1, repairResult.RepairedFiles);
                Assert.Equal(initialRepairResult.Issues.Count + 1, repairResult.Issues.Count);
                Assert.Empty(repairResult.Errors);
                DicomRepositoryIssue issue = Assert.Single(repairResult.Issues, item => item.IssueType == DicomRepositoryIssueType.MisplacedFile && item.InstanceId == instance.Id && item.ExpectedSopInstanceUid == sopInstanceUid.UID && item.AutomaticallyResolved);
                Assert.NotEqual(Guid.Empty, issue.Id);
                Assert.Equal(pathInfo.AbsolutePath, issue.ExpectedFilePath);

                /*
                 * ActualFilePath records the location at which the file was found.
                 * The file no longer exists there after successful repair.
                 */
                Assert.Equal(misplacedFilePath, issue.ActualFilePath);
                Assert.Equal(sopInstanceUid.UID, issue.ExpectedSopInstanceUid);
                Assert.Equal(sopInstanceUid.UID, issue.ActualSopInstanceUid);
                Assert.True(issue.CanResolveAutomatically);
                Assert.True(issue.AutomaticallyResolved);
                Assert.NotNull(issue.ResolvedAtUtc);
                Assert.InRange(issue.DetectedAtUtc, repairStartedAtUtc, repairCompletedAtUtc); Assert.InRange(issue.ResolvedAtUtc.Value, repairStartedAtUtc, repairCompletedAtUtc);
                Assert.True(issue.ResolvedAtUtc.Value >= issue.DetectedAtUtc);
                Assert.Contains(instance.Id.ToString(), issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(sopInstanceUid.UID, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(misplacedFilePath, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(pathInfo.AbsolutePath, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                Assert.False(File.Exists(misplacedFilePath));
                byte[] sourceBytes = await File.ReadAllBytesAsync(sourceFilePath, TestContext.Current.CancellationToken);
                byte[] repairedBytes = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);
                Assert.Equal(sourceBytes, repairedBytes);
            }
            finally
            {
                DeleteDirectory(sourceDirectory);
                DeleteDirectory(repositoryStudyDirectory);
            }
        }

        [Fact, Priority(29)]
        public async Task Repair_Should_Create_DuplicateFile_Issue()
        {
            string sourceDirectory = CreateTemporaryDirectory();
            string? repositoryStudyDirectory = null;

            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = true,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            try
            {
                string sourceFilePath = Path.Combine(sourceDirectory, "Image.dcm");

                DicomUID studyInstanceUid = DicomUID.Generate();
                DicomUID seriesInstanceUid = DicomUID.Generate();
                DicomUID sopInstanceUid = DicomUID.Generate();

                await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

                DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

                repositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

                DicomImportResult importResult = await ImportAsync(sourceDirectory);

                Assert.Equal(1, importResult.ImportedFiles);
                Assert.Equal(0, importResult.FailedFiles);
                Assert.Empty(importResult.Errors);
                Assert.True(File.Exists(pathInfo.AbsolutePath));

                Instance? instance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);

                Assert.NotNull(instance);

                string duplicateDirectory = Path.Combine(repositoryStudyDirectory!, "Duplicate");
                string duplicateFilePath = Path.Combine(duplicateDirectory, "DuplicateImage.bin");

                await Task.Run(() =>
                {
                    Directory.CreateDirectory(duplicateDirectory);
                    File.Copy(pathInfo.AbsolutePath, duplicateFilePath);
                }, TestContext.Current.CancellationToken);

                Assert.True(File.Exists(pathInfo.AbsolutePath));
                Assert.True(File.Exists(duplicateFilePath));

                byte[] canonicalBytesBeforeRepair = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);
                byte[] duplicateBytesBeforeRepair = await File.ReadAllBytesAsync(duplicateFilePath, TestContext.Current.CancellationToken);

                Assert.Equal(canonicalBytesBeforeRepair, duplicateBytesBeforeRepair);

                DateTime checkStartedAtUtc = DateTime.UtcNow;
                DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
                DateTime checkCompletedAtUtc = DateTime.UtcNow;

                Assert.Equal(initialRepairResult.ScannedFiles + 1, repairResult.ScannedFiles);
                Assert.Equal(initialRepairResult.DuplicateFiles + 1, repairResult.DuplicateFiles);
                Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);
                Assert.Equal(initialRepairResult.MisplacedFiles, repairResult.MisplacedFiles);
                Assert.Equal(initialRepairResult.IdentityMismatchFiles, repairResult.IdentityMismatchFiles);
                Assert.Equal(initialRepairResult.RepairedFiles, repairResult.RepairedFiles);
                Assert.Equal(initialRepairResult.Issues.Count + 1, repairResult.Issues.Count);
                Assert.Equal(initialRepairResult.Errors.Count, repairResult.Errors.Count);

                DicomRepositoryIssue issue = Assert.Single(repairResult.Issues, item => item.IssueType == DicomRepositoryIssueType.DuplicateFile && item.ExpectedSopInstanceUid == sopInstanceUid.UID);

                Assert.NotEqual(Guid.Empty, issue.Id);
                Assert.Null(issue.InstanceId);
                Assert.Equal(sopInstanceUid.UID, issue.ExpectedSopInstanceUid);
                Assert.Equal(sopInstanceUid.UID, issue.ActualSopInstanceUid);
                Assert.Empty(issue.ExpectedFilePath);
                Assert.Empty(issue.ActualFilePath);
                Assert.False(issue.CanResolveAutomatically);
                Assert.False(issue.AutomaticallyResolved);
                Assert.Null(issue.ResolvedAtUtc);
                Assert.InRange(issue.DetectedAtUtc, checkStartedAtUtc, checkCompletedAtUtc);
                Assert.Contains(sopInstanceUid.UID, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(pathInfo.AbsolutePath, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(duplicateFilePath, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains("2 physical files", issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains("No file was changed", issue.TechnicalDetails, StringComparison.Ordinal);

                /*
                 * Duplicate files must never be removed or overwritten
                 * automatically.
                 */
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                Assert.True(File.Exists(duplicateFilePath));

                byte[] canonicalBytesAfterRepair = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);
                byte[] duplicateBytesAfterRepair = await File.ReadAllBytesAsync(duplicateFilePath, TestContext.Current.CancellationToken);

                Assert.Equal(canonicalBytesBeforeRepair, canonicalBytesAfterRepair);
                Assert.Equal(duplicateBytesBeforeRepair, duplicateBytesAfterRepair);
                Assert.Equal(canonicalBytesAfterRepair, duplicateBytesAfterRepair);
            }
            finally
            {
                DeleteDirectory(sourceDirectory);
                DeleteDirectory(repositoryStudyDirectory);
            }
        }

        [Fact, Priority(30)]
        public async Task Repair_Should_Create_IdentityMismatch_Issue()
        {
            string sourceDirectory = CreateTemporaryDirectory();
            string? repositoryStudyDirectory = null;

            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = true,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            try
            {
                string sourceFilePath = Path.Combine(sourceDirectory, "Image.dcm");

                DicomUID studyInstanceUid = DicomUID.Generate();
                DicomUID seriesInstanceUid = DicomUID.Generate();
                DicomUID expectedSopInstanceUid = DicomUID.Generate();

                await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, expectedSopInstanceUid);

                DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, expectedSopInstanceUid);

                repositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);
                DicomImportResult importResult = await ImportAsync(sourceDirectory);
                Assert.Equal(1, importResult.ImportedFiles);
                Assert.Equal(0, importResult.FailedFiles);
                Assert.Empty(importResult.Errors);
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                Instance? instance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(expectedSopInstanceUid.UID, TestContext.Current.CancellationToken);
                Assert.NotNull(instance);
                DicomUID actualSopInstanceUid = DicomUID.Generate();

                /*
                 * Replace the repository file with a valid DICOM file that carries
                 * a different SOP instance UID.
                 */
                await CreateDicomFileAsync(pathInfo.AbsolutePath, studyInstanceUid, seriesInstanceUid, actualSopInstanceUid);
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                DicomFile mismatchingFile = await Task.Run(() => DicomFile.Open(pathInfo.AbsolutePath), TestContext.Current.CancellationToken);
                string sopInstanceUidInFile = mismatchingFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
                Assert.Equal(actualSopInstanceUid.UID, sopInstanceUidInFile);
                DateTime checkStartedAtUtc = DateTime.UtcNow;
                DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
                DateTime checkCompletedAtUtc = DateTime.UtcNow;
                Assert.Equal(initialRepairResult.ScannedFiles + 1, repairResult.ScannedFiles);
                Assert.Equal(initialRepairResult.IdentityMismatchFiles + 1, repairResult.IdentityMismatchFiles); Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);
                Assert.Equal(initialRepairResult.MisplacedFiles, repairResult.MisplacedFiles);
                Assert.Equal(initialRepairResult.RepairedFiles, repairResult.RepairedFiles);
                Assert.Equal(initialRepairResult.DuplicateFiles, repairResult.DuplicateFiles);
                Assert.Equal(initialRepairResult.Issues.Count + 1, repairResult.Issues.Count);

                /*
                 * The existing technical error behavior remains unchanged.
                 */
                Assert.Equal(initialRepairResult.Errors.Count + 1, repairResult.Errors.Count);
                DicomRepositoryIssue issue = Assert.Single(repairResult.Issues, item => item.IssueType == DicomRepositoryIssueType.IdentityMismatch && item.InstanceId == instance.Id && item.ExpectedSopInstanceUid == expectedSopInstanceUid.UID && item.ActualSopInstanceUid == actualSopInstanceUid.UID);
                Assert.NotEqual(Guid.Empty, issue.Id);
                Assert.Equal(pathInfo.AbsolutePath, issue.ExpectedFilePath);
                Assert.Equal(pathInfo.AbsolutePath, issue.ActualFilePath);
                Assert.Equal(expectedSopInstanceUid.UID, issue.ExpectedSopInstanceUid);
                Assert.Equal(actualSopInstanceUid.UID, issue.ActualSopInstanceUid);
                Assert.False(issue.CanResolveAutomatically);
                Assert.False(issue.AutomaticallyResolved);
                Assert.Null(issue.ResolvedAtUtc);
                Assert.InRange(issue.DetectedAtUtc, checkStartedAtUtc, checkCompletedAtUtc);
                Assert.Contains(instance.Id.ToString(), issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(expectedSopInstanceUid.UID, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(actualSopInstanceUid.UID, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(pathInfo.AbsolutePath, issue.TechnicalDetails, StringComparison.Ordinal);

                /*
                 * Identity conflicts must never trigger an automatic file change.
                 */
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                DicomFile repositoryFileAfterRepair = await Task.Run(() => DicomFile.Open(pathInfo.AbsolutePath), TestContext.Current.CancellationToken);
                string sopInstanceUidAfterRepair = repositoryFileAfterRepair.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
                Assert.Equal(actualSopInstanceUid.UID, sopInstanceUidAfterRepair);
            }
            finally
            {
                DeleteDirectory(sourceDirectory);
                DeleteDirectory(repositoryStudyDirectory);
            }
        }

        [Fact, Priority(31)]
        public async Task Repair_Should_Create_OrphanedFile_Issue()
        {
            DicomUID studyInstanceUid = DicomUID.Generate();
            DicomUID seriesInstanceUid = DicomUID.Generate();
            DicomUID sopInstanceUid = DicomUID.Generate();
            DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            string? repositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);

            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = true,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            try
            {
                string? repositorySeriesDirectory = Path.GetDirectoryName(pathInfo.AbsolutePath);
                Assert.False(string.IsNullOrWhiteSpace(repositorySeriesDirectory));
                await Task.Run(() => Directory.CreateDirectory(repositorySeriesDirectory!), TestContext.Current.CancellationToken);
                await CreateDicomFileAsync(pathInfo.AbsolutePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                Instance? persistedInstance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);
                Assert.Null(persistedInstance);
                byte[] fileBytesBeforeCheck = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);
                DateTime checkStartedAtUtc = DateTime.UtcNow;
                DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
                DateTime checkCompletedAtUtc = DateTime.UtcNow;

                /*
                 * Orphaned files have no persisted instance and therefore do not
                 * increase the number of scanned persistence instances.
                 */
                Assert.Equal(initialRepairResult.ScannedFiles, repairResult.ScannedFiles);
                Assert.Equal(initialRepairResult.OrphanedFiles + 1, repairResult.OrphanedFiles);
                Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);
                Assert.Equal(initialRepairResult.MisplacedFiles, repairResult.MisplacedFiles);
                Assert.Equal(initialRepairResult.IdentityMismatchFiles, repairResult.IdentityMismatchFiles);
                Assert.Equal(initialRepairResult.DuplicateFiles, repairResult.DuplicateFiles);
                Assert.Equal(initialRepairResult.RepairedFiles, repairResult.RepairedFiles);
                Assert.Equal(initialRepairResult.Issues.Count + 1, repairResult.Issues.Count);
                Assert.Equal(initialRepairResult.Errors.Count, repairResult.Errors.Count);
                DicomRepositoryIssue issue = Assert.Single(repairResult.Issues, item => item.IssueType == DicomRepositoryIssueType.OrphanedFile && item.ActualSopInstanceUid == sopInstanceUid.UID);
                Assert.NotEqual(Guid.Empty, issue.Id);
                Assert.Null(issue.InstanceId);
                Assert.Empty(issue.ExpectedFilePath);
                Assert.Equal(pathInfo.AbsolutePath, issue.ActualFilePath);
                Assert.Empty(issue.ExpectedSopInstanceUid);
                Assert.Equal(sopInstanceUid.UID, issue.ActualSopInstanceUid);
                Assert.False(issue.CanResolveAutomatically);
                Assert.False(issue.AutomaticallyResolved);
                Assert.Null(issue.ResolvedAtUtc);
                Assert.InRange(issue.DetectedAtUtc, checkStartedAtUtc, checkCompletedAtUtc);
                Assert.Contains(sopInstanceUid.UID, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(pathInfo.AbsolutePath, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains("no corresponding persisted instance", issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains("No file or persistence record was changed", issue.TechnicalDetails, StringComparison.Ordinal);

                /*
                 * A repository check must not register an orphan automatically.
                 */
                persistedInstance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);
                Assert.Null(persistedInstance);

                /*
                 * The physical file must remain unchanged.
                 */
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                byte[] fileBytesAfterCheck = await File.ReadAllBytesAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);
                Assert.Equal(fileBytesBeforeCheck, fileBytesAfterCheck);
            }
            finally
            {
                DeleteDirectory(repositoryStudyDirectory);
            }
        }

        [Fact, Priority(32)]
        public async Task Repair_Should_Create_InvalidDicomFile_Issue()
        {
            string sourceDirectory = CreateTemporaryDirectory();
            string? repositoryStudyDirectory = null;

            DicomRepositoryRepairRequest repairRequest = new()
            {
                VerifyFiles = true,
                RepairMissingFiles = true,
                RebuildRepositoryIndex = false
            };

            DicomRepositoryRepairResult initialRepairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);

            try
            {
                string sourceFilePath = Path.Combine(sourceDirectory, "Image.dcm");
                DicomUID studyInstanceUid = DicomUID.Generate();
                DicomUID seriesInstanceUid = DicomUID.Generate();
                DicomUID sopInstanceUid = DicomUID.Generate();
                await CreateDicomFileAsync(sourceFilePath, studyInstanceUid, seriesInstanceUid, sopInstanceUid);
                DicomRepositoryPathInfo pathInfo = CreatePathInfo(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
                repositoryStudyDirectory = GetRepositoryStudyDirectory(pathInfo);
                DicomImportResult importResult = await ImportAsync(sourceDirectory);
                Assert.Equal(1, importResult.ImportedFiles);
                Assert.Equal(0, importResult.FailedFiles);
                Assert.Empty(importResult.Errors);
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                Instance? instance = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(sopInstanceUid.UID, TestContext.Current.CancellationToken);
                Assert.NotNull(instance);

                /*
                 * Replace the valid repository file with invalid DICOM content.
                 */
                await File.WriteAllTextAsync(pathInfo.AbsolutePath, "This is not a valid DICOM file.", TestContext.Current.CancellationToken);
                Assert.True(File.Exists(pathInfo.AbsolutePath));
                string fileContentBeforeCheck = await File.ReadAllTextAsync(pathInfo.AbsolutePath, TestContext.Current.CancellationToken);
                DateTime checkStartedAtUtc = DateTime.UtcNow;
                DicomRepositoryRepairResult repairResult = await _fixture.Repository!.RepositoryRepairService!.RepairAsync(repairRequest, TestContext.Current.CancellationToken);
                DateTime checkCompletedAtUtc = DateTime.UtcNow;
                Assert.Equal(initialRepairResult.ScannedFiles + 1, repairResult.ScannedFiles);
                Assert.Equal(initialRepairResult.InvalidDicomFiles + 1, repairResult.InvalidDicomFiles);

                /*
                 * An invalid DICOM file is not an identity mismatch because no
                 * valid DICOM identity could be read.
                 */
                Assert.Equal(initialRepairResult.IdentityMismatchFiles, repairResult.IdentityMismatchFiles);
                Assert.Equal(initialRepairResult.MissingFiles, repairResult.MissingFiles);
                Assert.Equal(initialRepairResult.MisplacedFiles, repairResult.MisplacedFiles);
                Assert.Equal(initialRepairResult.DuplicateFiles, repairResult.DuplicateFiles);
                Assert.Equal(initialRepairResult.OrphanedFiles, repairResult.OrphanedFiles);
                Assert.Equal(initialRepairResult.RepairedFiles, repairResult.RepairedFiles);
                Assert.Equal(initialRepairResult.Issues.Count + 1, repairResult.Issues.Count);
                Assert.Equal(initialRepairResult.Errors.Count + 1, repairResult.Errors.Count);
                DicomRepositoryIssue issue = Assert.Single(repairResult.Issues, item => item.IssueType == DicomRepositoryIssueType.InvalidDicomFile && item.InstanceId == instance.Id && item.ExpectedSopInstanceUid == sopInstanceUid.UID);
                Assert.NotEqual(Guid.Empty, issue.Id);
                Assert.Equal(instance.Id, issue.InstanceId);
                Assert.Equal(pathInfo.AbsolutePath, issue.ExpectedFilePath);
                Assert.Equal(pathInfo.AbsolutePath, issue.ActualFilePath);
                Assert.Equal(sopInstanceUid.UID, issue.ExpectedSopInstanceUid);
                Assert.Empty(issue.ActualSopInstanceUid);
                Assert.False(issue.CanResolveAutomatically);
                Assert.False(issue.AutomaticallyResolved);
                Assert.Null(issue.ResolvedAtUtc);
                Assert.InRange(issue.DetectedAtUtc, checkStartedAtUtc, checkCompletedAtUtc);
                Assert.Contains(instance.Id.ToString(), issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(sopInstanceUid.UID, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains(pathInfo.AbsolutePath, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains("not a valid DICOM file", issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Single(repairResult.Errors, error => error.Contains(pathInfo.AbsolutePath, StringComparison.Ordinal) && error.Contains(sopInstanceUid.UID, StringComparison.Ordinal));

                /*
                 * MIRAS must not delete, replace, move or otherwise modify the
                 * invalid medical repository file automatically.
                 */
                Assert.True(
                    File.Exists(pathInfo.AbsolutePath));

                string fileContentAfterCheck =
                    await File.ReadAllTextAsync(
                        pathInfo.AbsolutePath,
                        TestContext.Current.CancellationToken);

                Assert.Equal(
                    fileContentBeforeCheck,
                    fileContentAfterCheck);
            }
            finally
            {
                DeleteDirectory(sourceDirectory);
                DeleteDirectory(repositoryStudyDirectory);
            }
        }

        private DicomRepositoryPathInfo CreatePathInfo(DicomUID studyInstanceUid, DicomUID seriesInstanceUid, DicomUID sopInstanceUid) => _fixture.Repository!.RepositoryService!.CreatePathInfo(studyInstanceUid.UID, seriesInstanceUid.UID, sopInstanceUid.UID);

        private async Task<DicomImportResult> ImportAsync(string sourcePath, bool allowOverwrite = false) => await _fixture.Repository!.ImportService!.ImportAsync(new DicomImportRequest
        {
            SourcePath = sourcePath,
            SourceType = ImportSourceType.Directory,
            AllowOverwrite = allowOverwrite,
            ExecuteRepositoryRepair = false,
            CreatedByUserId = _fixture.TestUser!.Id
        }, TestContext.Current.CancellationToken);

        private static async Task CreateDicomFileAsync(string filePath, DicomUID? studyInstanceUid, DicomUID? seriesInstanceUid, DicomUID sopInstanceUid)
        {
            DicomDataset dataset = new()
            {
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage },
                { DicomTag.SOPInstanceUID, sopInstanceUid }
            };

            if (studyInstanceUid is not null)
            {
                dataset.Add(DicomTag.StudyInstanceUID, studyInstanceUid);
            }

            if (seriesInstanceUid is not null)
            {
                dataset.Add(DicomTag.SeriesInstanceUID, seriesInstanceUid);
            }

            DicomFile dicomFile = new(dataset);

            await dicomFile.SaveAsync(filePath);
        }

        private static string CreateTemporaryDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(directory);

            return directory;
        }

        private static void DeleteDirectory(string? directory)
        {
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        private static string? GetRepositoryStudyDirectory(DicomRepositoryPathInfo pathInfo)
        {
            string? seriesDirectory = Path.GetDirectoryName(pathInfo.AbsolutePath);

            return seriesDirectory is null ? null : Path.GetDirectoryName(seriesDirectory);
        }
    }
}