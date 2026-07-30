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
            DicomRepositoryPathInfo pathInfo = _fixture.Repository!.RepositoryService!.CreatePathInfo(_fixture.RepositoryLocation!, "1.2.3", "4.5.6", "7.8.9");

            Assert.Equal(_fixture.RepositoryLocation!.Id, pathInfo.RepositoryLocationId);
            Assert.Equal(_fixture.RepositoryLocation.RootPath, pathInfo.RepositoryRootPath);
            Assert.Equal("1.2.3", pathInfo.StudyInstanceUid);
            Assert.Equal("4.5.6", pathInfo.SeriesInstanceUid);
            Assert.Equal("7.8.9", pathInfo.SopInstanceUid);
            Assert.Equal(Path.Combine("1.2.3", "4.5.6", "7.8.9.dcm"), pathInfo.RelativePath);
            Assert.Equal(Path.Combine(_fixture.RepositoryLocation.RootPath!, pathInfo.RelativePath), pathInfo.AbsolutePath);
        }

        [Fact, Priority(4)]
        public void Exists_Should_Return_False_For_Missing_File()
        {
            bool exists = _fixture.Repository!.RepositoryService!.Exists(_fixture.RepositoryLocation!, Path.Combine("DoesNotExist", "File.dcm"));
            Assert.False(exists);
        }

        [Fact, Priority(5)]
        public void GetAbsolutePath_Should_Combine_RepositoryLocation_And_Relative_Path()
        {
            string relativePath = Path.Combine("1.2.3", "4.5.6", "7.8.9.dcm");
            string absolutePath = _fixture.Repository!.RepositoryService!.GetAbsolutePath(_fixture.RepositoryLocation!, relativePath);

            Assert.Equal(Path.Combine(_fixture.RepositoryLocation!.RootPath!, relativePath), absolutePath);
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
            Assert.Equal(_fixture.RepositoryLocation!.Id, instance.RepositoryLocationId);
            Assert.Equal(pathInfo.RelativePath, instance.RelativeFilePath);
            Assert.Equal(_fixture.RepositoryLocation.RootPath, pathInfo.RepositoryRootPath);
            Assert.Equal(_fixture.RepositoryLocation.Id, pathInfo.RepositoryLocationId);
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
            Assert.Equal(_fixture.RepositoryLocation!.Id, instance.RepositoryLocationId);
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
                RebuildRepositoryIndex = false,
                RepairMissingFiles = false,
                RepositoryLocationId = _fixture.RepositoryLocation!.Id,
                VerifyFiles = true
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
                RebuildRepositoryIndex = false,
                RepairMissingFiles = false,
                RepositoryLocationId = _fixture.RepositoryLocation!.Id,
                VerifyFiles = true
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
                RebuildRepositoryIndex = false,
                RepairMissingFiles = false,
                RepositoryLocationId = _fixture.RepositoryLocation!.Id,
                VerifyFiles = true
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
                RebuildRepositoryIndex = false,
                RepositoryLocationId = _fixture.RepositoryLocation!.Id
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
                RebuildRepositoryIndex = false,
                RepairMissingFiles = false,
                RepositoryLocationId = _fixture.RepositoryLocation!.Id,
                VerifyFiles = true
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
                RebuildRepositoryIndex = false,
                RepairMissingFiles = false,
                RepositoryLocationId = _fixture.RepositoryLocation!.Id,
                VerifyFiles = true
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
                RebuildRepositoryIndex = false,
                RepairMissingFiles = false,
                RepositoryLocationId = _fixture.RepositoryLocation!.Id,
                VerifyFiles = true
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
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync(repairMissingFiles: false);
            await scenario.ImportSuccessfullyAsync();
            Instance instance = await scenario.GetPersistedInstanceAsync();
            File.Delete(scenario.PathInfo.AbsolutePath);

            DicomRepositoryRepairResult result = await RepairAsync(repairMissingFiles: false);

            Assert.Equal(initial.MissingFiles + 1, result.MissingFiles);
            Assert.Equal(initial.Issues.Count + 1, result.Issues.Count);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.MissingFile && item.InstanceId == instance.Id);
            Assert.Equal(scenario.PathInfo.AbsolutePath, issue.ExpectedFilePath);
            Assert.Equal(scenario.SopInstanceUid.UID, issue.ExpectedSopInstanceUid);
            Assert.False(issue.CanResolveAutomatically);
            Assert.False(issue.AutomaticallyResolved);
        }

        [Fact, Priority(27)]
        public async Task Repair_Should_Create_MisplacedFile_Issue()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync(repairMissingFiles: false);
            await scenario.ImportSuccessfullyAsync();
            Instance instance = await scenario.GetPersistedInstanceAsync();
            string misplacedPath = await scenario.MoveRepositoryFileAsync();

            DicomRepositoryRepairResult result = await RepairAsync(repairMissingFiles: false);

            Assert.Equal(initial.MisplacedFiles + 1, result.MisplacedFiles);
            Assert.Equal(initial.RepairedFiles, result.RepairedFiles);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.MisplacedFile && item.InstanceId == instance.Id);
            Assert.Equal(scenario.PathInfo.AbsolutePath, issue.ExpectedFilePath);
            Assert.Equal(misplacedPath, issue.ActualFilePath);
            Assert.True(issue.CanResolveAutomatically);
            Assert.False(issue.AutomaticallyResolved);
            Assert.False(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.True(File.Exists(misplacedPath));
        }

        [Fact, Priority(28)]
        public async Task Repair_Should_Create_AutomaticallyResolved_MisplacedFile_Issue()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync(repairMissingFiles: false);
            await scenario.ImportSuccessfullyAsync();
            Instance instance = await scenario.GetPersistedInstanceAsync();
            string misplacedPath = await scenario.MoveRepositoryFileAsync();

            DicomRepositoryRepairResult result = await RepairAsync();

            Assert.Equal(initial.MisplacedFiles + 1, result.MisplacedFiles);
            Assert.Equal(initial.RepairedFiles + 1, result.RepairedFiles);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.MisplacedFile && item.InstanceId == instance.Id && item.AutomaticallyResolved);
            Assert.True(issue.CanResolveAutomatically);
            Assert.NotNull(issue.ResolvedAtUtc);
            Assert.True(issue.ResolvedAtUtc >= issue.DetectedAtUtc);
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.False(File.Exists(misplacedPath));
            await AssertFilesEqualAsync(scenario.SourceFilePath, scenario.PathInfo.AbsolutePath);
        }

        [Fact, Priority(29)]
        public async Task Repair_Should_Create_DuplicateFile_Issue()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync();
            await scenario.ImportSuccessfullyAsync();
            string duplicatePath = await scenario.CopyRepositoryFileAsync();
            byte[] canonicalBefore = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);
            byte[] duplicateBefore = await ReadAllBytesAsync(duplicatePath);

            DicomRepositoryRepairResult result = await RepairAsync();

            Assert.Equal(initial.DuplicateFiles + 1, result.DuplicateFiles);
            Assert.Equal(initial.RepairedFiles, result.RepairedFiles);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.DuplicateFile && item.ExpectedSopInstanceUid == scenario.SopInstanceUid.UID);
            Assert.False(issue.CanResolveAutomatically);
            Assert.False(issue.AutomaticallyResolved);
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.True(File.Exists(duplicatePath));
            Assert.Equal(canonicalBefore, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
            Assert.Equal(duplicateBefore, await ReadAllBytesAsync(duplicatePath));
        }

        [Fact, Priority(30)]
        public async Task Repair_Should_Create_IdentityMismatch_Issue()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync();
            await scenario.ImportSuccessfullyAsync();
            Instance instance = await scenario.GetPersistedInstanceAsync();
            DicomUID actualUid = DicomUID.Generate();
            await scenario.ReplaceRepositoryFileAsync(actualUid);

            DicomRepositoryRepairResult result = await RepairAsync();

            Assert.Equal(initial.IdentityMismatchFiles + 1, result.IdentityMismatchFiles);
            Assert.Equal(initial.RepairedFiles, result.RepairedFiles);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.IdentityMismatch && item.InstanceId == instance.Id);
            Assert.Equal(scenario.SopInstanceUid.UID, issue.ExpectedSopInstanceUid);
            Assert.Equal(actualUid.UID, issue.ActualSopInstanceUid);
            Assert.Empty(issue.RecoveryCandidateFilePath);
            Assert.False(issue.CanResolveAutomatically);
            Assert.Equal(actualUid.UID, await ReadSopInstanceUidAsync(scenario.PathInfo.AbsolutePath));
        }

        [Fact, Priority(31)]
        public async Task Repair_Should_Create_OrphanedFile_Issue()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            DicomRepositoryRepairResult initial = await RepairAsync();
            await scenario.CreateOrphanedRepositoryFileAsync();

            DicomRepositoryRepairResult result = await RepairAsync();

            Assert.Equal(initial.OrphanedFiles + 1, result.OrphanedFiles);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.OrphanedFile && item.ActualSopInstanceUid == scenario.SopInstanceUid.UID);
            Assert.Null(issue.InstanceId);
            Assert.Equal(scenario.PathInfo.AbsolutePath, issue.ActualFilePath);
            Assert.False(issue.CanResolveAutomatically);
            Assert.Null(await scenario.TryGetPersistedInstanceAsync());
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
        }

        [Fact, Priority(32)]
        public async Task Repair_Should_Create_InvalidDicomFile_Issue()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync();
            await scenario.ImportSuccessfullyAsync();
            Instance instance = await scenario.GetPersistedInstanceAsync();
            await scenario.ReplaceRepositoryFileWithInvalidContentAsync();
            string contentBefore = await File.ReadAllTextAsync(scenario.PathInfo.AbsolutePath, TestContext.Current.CancellationToken);

            DicomRepositoryRepairResult result = await RepairAsync();

            Assert.Equal(initial.InvalidDicomFiles + 1, result.InvalidDicomFiles);
            Assert.Equal(initial.IdentityMismatchFiles, result.IdentityMismatchFiles);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.InvalidDicomFile && item.InstanceId == instance.Id);
            Assert.Empty(issue.ActualSopInstanceUid);
            Assert.Empty(issue.RecoveryCandidateFilePath);
            Assert.False(issue.CanResolveAutomatically);
            Assert.Equal(contentBefore, await File.ReadAllTextAsync(scenario.PathInfo.AbsolutePath, TestContext.Current.CancellationToken));
        }

        [Fact, Priority(33)]
        public async Task Repair_Should_Create_UnreadableFile_Issue()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync();
            await scenario.ImportSuccessfullyAsync();
            Instance instance = await scenario.GetPersistedInstanceAsync();
            byte[] bytesBefore = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);
            DicomRepositoryRepairResult result;

            using (FileStream lockedFile = scenario.LockRepositoryFile())
            {
                Assert.False(lockedFile.SafeFileHandle.IsInvalid);
                result = await RepairAsync();
            }

            Assert.Equal(initial.UnreadableFiles + 1, result.UnreadableFiles);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.UnreadableFile && item.InstanceId == instance.Id);
            Assert.Equal(scenario.PathInfo.AbsolutePath, issue.ActualFilePath);
            Assert.False(issue.CanResolveAutomatically);
            Assert.Equal(bytesBefore, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
        }

        [Fact, Priority(34)]
        public async Task Repair_Should_Create_IncompleteImport_Issue()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(createDicomFile: false);
            DicomRepositoryRepairResult initial = await RepairAsync();
            string incompletePath = await scenario.CreateIncompleteImportFileAsync();
            byte[] bytesBefore = await ReadAllBytesAsync(incompletePath);

            DicomRepositoryRepairResult result = await RepairAsync();

            Assert.Equal(initial.IncompleteImportFiles + 1, result.IncompleteImportFiles);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.IncompleteImport && item.ActualFilePath == incompletePath);
            Assert.False(issue.CanResolveAutomatically);
            Assert.False(issue.AutomaticallyResolved);
            Assert.Equal(bytesBefore, await ReadAllBytesAsync(incompletePath));
        }

        [Fact, Priority(35)]
        public async Task Repair_Should_Register_Unique_Recovery_Candidate_For_IdentityMismatch()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync();
            await scenario.ImportSuccessfullyAsync();
            Instance instance = await scenario.GetPersistedInstanceAsync();
            string candidatePath = await scenario.MoveRepositoryFileAsync("RecoveryCandidate", "ExpectedImage.dcm");
            DicomUID actualUid = DicomUID.Generate();
            await scenario.ReplaceRepositoryFileAsync(actualUid);
            byte[] expectedBefore = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);
            byte[] candidateBefore = await ReadAllBytesAsync(candidatePath);

            DicomRepositoryRepairResult result = await RepairAsync();

            Assert.Equal(initial.IdentityMismatchFiles + 1, result.IdentityMismatchFiles);
            Assert.Equal(initial.MisplacedFiles, result.MisplacedFiles);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.IdentityMismatch && item.InstanceId == instance.Id);
            Assert.Equal(candidatePath, issue.RecoveryCandidateFilePath);
            Assert.False(issue.CanResolveAutomatically);
            Assert.Equal(expectedBefore, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
            Assert.Equal(candidateBefore, await ReadAllBytesAsync(candidatePath));
        }

        [Fact, Priority(36)]
        public async Task Repair_Should_Register_Unique_Recovery_Candidate_For_InvalidDicomFile()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync();
            await scenario.ImportSuccessfullyAsync();
            Instance instance = await scenario.GetPersistedInstanceAsync();
            string candidatePath = await scenario.MoveRepositoryFileAsync("RecoveryCandidate", "ExpectedImage.dcm");
            await scenario.ReplaceRepositoryFileWithInvalidContentAsync();
            string invalidContentBefore = await File.ReadAllTextAsync(scenario.PathInfo.AbsolutePath, TestContext.Current.CancellationToken);
            byte[] candidateBefore = await ReadAllBytesAsync(candidatePath);

            DicomRepositoryRepairResult result = await RepairAsync();

            Assert.Equal(initial.InvalidDicomFiles + 1, result.InvalidDicomFiles);
            Assert.Equal(initial.MisplacedFiles, result.MisplacedFiles);
            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.InvalidDicomFile && item.InstanceId == instance.Id);
            Assert.Equal(candidatePath, issue.RecoveryCandidateFilePath);
            Assert.False(issue.CanResolveAutomatically);
            Assert.Equal(invalidContentBefore, await File.ReadAllTextAsync(scenario.PathInfo.AbsolutePath, TestContext.Current.CancellationToken));
            Assert.Equal(candidateBefore, await ReadAllBytesAsync(candidatePath));
        }

        [Fact, Priority(37)]
        public async Task Repair_Should_Create_RelationshipConflict_When_RelativeFilePath_Is_Missing()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync();
            await scenario.ImportSuccessfullyAsync();
            Instance original = await scenario.GetPersistedInstanceAsync();
            Assert.False(string.IsNullOrWhiteSpace(original.RelativeFilePath));
            string originalRelativePath = original.RelativeFilePath;
            byte[] bytesBefore = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);

            try
            {
                Instance instance = await scenario.SetRelativeFilePathAsync(string.Empty);
                DicomRepositoryRepairResult result = await RepairAsync();

                Assert.Equal(initial.RelationshipConflicts + 1, result.RelationshipConflicts);
                Assert.Equal(initial.MisplacedFiles, result.MisplacedFiles);
                Assert.Equal(initial.RepairedFiles, result.RepairedFiles);
                DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.RelationshipConflict && item.InstanceId == instance.Id);
                Assert.Empty(issue.ExpectedFilePath);
                Assert.Equal(scenario.PathInfo.AbsolutePath, issue.ActualFilePath);
                Assert.Equal(scenario.PathInfo.AbsolutePath, issue.RecoveryCandidateFilePath);
                Assert.False(issue.CanResolveAutomatically);
                Assert.Equal(bytesBefore, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
                Assert.Empty((await scenario.GetPersistedInstanceAsync()).RelativeFilePath!);
            }
            finally
            {
                // The fixture shares one database; restore test-induced inconsistency.
                await scenario.SetRelativeFilePathAsync(originalRelativePath);
            }
        }

        [Fact, Priority(38)]
        public async Task Repair_Should_Create_RelationshipConflict_When_Dicom_Study_Differs()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync();

            await scenario.ImportSuccessfullyAsync();

            Instance instance = await scenario.GetPersistedInstanceAsync();
            DicomUID actualStudyInstanceUid = DicomUID.Generate();

            /*
             * The SOP and Series identities remain correct. Only the Study identity
             * inside the physical DICOM file contradicts the persisted hierarchy.
             */
            await scenario.ReplaceRepositoryFileHierarchyAsync(actualStudyInstanceUid, scenario.SeriesInstanceUid);

            byte[] bytesBefore = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);
            DicomRepositoryRepairResult result = await RepairAsync();

            Assert.Equal(initial.RelationshipConflicts + 1, result.RelationshipConflicts);
            Assert.Equal(initial.IdentityMismatchFiles, result.IdentityMismatchFiles);
            Assert.Equal(initial.MisplacedFiles, result.MisplacedFiles);
            Assert.Equal(initial.RepairedFiles, result.RepairedFiles);
            Assert.Equal(initial.Issues.Count + 1, result.Issues.Count);
            Assert.Equal(initial.Errors.Count + 1, result.Errors.Count);

            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.RelationshipConflict && item.InstanceId == instance.Id);

            Assert.Equal(scenario.StudyInstanceUid.UID, issue.ExpectedStudyInstanceUid);
            Assert.Equal(actualStudyInstanceUid.UID, issue.ActualStudyInstanceUid);
            Assert.Equal(scenario.SeriesInstanceUid.UID, issue.ExpectedSeriesInstanceUid);
            Assert.Equal(scenario.SeriesInstanceUid.UID, issue.ActualSeriesInstanceUid);
            Assert.Equal(scenario.SopInstanceUid.UID, issue.ExpectedSopInstanceUid);
            Assert.Equal(scenario.SopInstanceUid.UID, issue.ActualSopInstanceUid);
            Assert.False(issue.CanResolveAutomatically);
            Assert.False(issue.AutomaticallyResolved);
            Assert.Null(issue.ResolvedAtUtc);
            Assert.Contains("Study identity matches: False", issue.TechnicalDetails, StringComparison.Ordinal);
            Assert.Contains("Series identity matches: True", issue.TechnicalDetails, StringComparison.Ordinal);

            /*
             * A hierarchy conflict must not rewrite either the DICOM metadata or the
             * persisted database relationship.
             */
            Assert.Equal(bytesBefore, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));

            Instance persistedInstance = await scenario.GetPersistedInstanceAsync();
            Assert.Equal(instance.SeriesId, persistedInstance.SeriesId);
        }

        [Fact, Priority(39)]
        public async Task Repair_Should_Create_RelationshipConflict_When_Dicom_Series_Differs()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            DicomRepositoryRepairResult initial = await RepairAsync();

            await scenario.ImportSuccessfullyAsync();

            Instance instance = await scenario.GetPersistedInstanceAsync();
            DicomUID actualSeriesInstanceUid = DicomUID.Generate();

            /*
             * The SOP and Study identities remain correct. Only the Series identity
             * inside the physical DICOM file contradicts the persisted hierarchy.
             */
            await scenario.ReplaceRepositoryFileHierarchyAsync(scenario.StudyInstanceUid, actualSeriesInstanceUid);

            byte[] bytesBefore = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);
            DicomRepositoryRepairResult result = await RepairAsync();

            Assert.Equal(initial.RelationshipConflicts + 1, result.RelationshipConflicts);
            Assert.Equal(initial.IdentityMismatchFiles, result.IdentityMismatchFiles);
            Assert.Equal(initial.MisplacedFiles, result.MisplacedFiles);
            Assert.Equal(initial.RepairedFiles, result.RepairedFiles);
            Assert.Equal(initial.Issues.Count + 1, result.Issues.Count);
            Assert.Equal(initial.Errors.Count + 1, result.Errors.Count);

            DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.RelationshipConflict && item.InstanceId == instance.Id);

            Assert.Equal(scenario.StudyInstanceUid.UID, issue.ExpectedStudyInstanceUid);
            Assert.Equal(scenario.StudyInstanceUid.UID, issue.ActualStudyInstanceUid);
            Assert.Equal(scenario.SeriesInstanceUid.UID, issue.ExpectedSeriesInstanceUid);
            Assert.Equal(actualSeriesInstanceUid.UID, issue.ActualSeriesInstanceUid);
            Assert.Equal(scenario.SopInstanceUid.UID, issue.ExpectedSopInstanceUid);
            Assert.Equal(scenario.SopInstanceUid.UID, issue.ActualSopInstanceUid);
            Assert.False(issue.CanResolveAutomatically);
            Assert.False(issue.AutomaticallyResolved);
            Assert.Null(issue.ResolvedAtUtc);
            Assert.Contains("Study identity matches: True", issue.TechnicalDetails, StringComparison.Ordinal);
            Assert.Contains("Series identity matches: False", issue.TechnicalDetails, StringComparison.Ordinal);

            /*
             * The repository reports the conflict but must not move the instance to a
             * different persisted Series or rewrite the medical file.
             */
            Assert.Equal(bytesBefore, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));

            Instance persistedInstance = await scenario.GetPersistedInstanceAsync();
            Assert.Equal(instance.SeriesId, persistedInstance.SeriesId);
        }

        [Fact, Priority(40)]
        public async Task Import_Should_Store_Instance_In_Selected_Secondary_RepositoryLocation()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            await scenario.ImportSuccessfullyAsync();

            Instance instance = await scenario.GetPersistedInstanceAsync();

            Assert.Equal(_fixture.SecondaryRepositoryLocation!.Id, instance.RepositoryLocationId);
            Assert.Equal(scenario.PathInfo.RelativePath, instance.RelativeFilePath);
            Assert.Equal(_fixture.SecondaryRepositoryLocation.Id, scenario.PathInfo.RepositoryLocationId);
            Assert.Equal(_fixture.SecondaryRepositoryLocation.RootPath, scenario.PathInfo.RepositoryRootPath);
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));

            string primaryPath = _fixture.Repository!.RepositoryService!.GetAbsolutePath(_fixture.RepositoryLocation!, scenario.PathInfo.RelativePath);

            /*
             * Selecting the secondary repository must not create a physical copy in
             * the primary repository or silently fall back to its default location.
             */
            Assert.False(File.Exists(primaryPath));

            byte[] sourceBytes = await ReadAllBytesAsync(scenario.SourceFilePath);
            byte[] importedBytes = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);

            Assert.Equal(sourceBytes, importedBytes);
        }

        [Fact, Priority(41)]
        public async Task Repair_Should_Check_Only_Selected_RepositoryLocation()
        {
            using RepositoryTestScenario primaryScenario = await CreateRepositoryScenarioAsync();
            using RepositoryTestScenario secondaryScenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            await primaryScenario.ImportSuccessfullyAsync();
            await secondaryScenario.ImportSuccessfullyAsync();

            Instance primaryInstance = await primaryScenario.GetPersistedInstanceAsync();
            Instance secondaryInstance = await secondaryScenario.GetPersistedInstanceAsync();

            try
            {
                File.Delete(primaryScenario.PathInfo.AbsolutePath);
                File.Delete(secondaryScenario.PathInfo.AbsolutePath);

                DicomRepositoryRepairResult result = await RepairAsync(new DicomRepositoryRepairRequest
                {
                    VerifyFiles = true,
                    RepairMissingFiles = false,
                    RebuildRepositoryIndex = false,
                    RepositoryLocationId = _fixture.SecondaryRepositoryLocation!.Id
                });

                DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.MissingFile && item.InstanceId == secondaryInstance.Id);

                Assert.Equal(_fixture.SecondaryRepositoryLocation.Id, issue.RepositoryLocationId);
                Assert.Equal(secondaryScenario.PathInfo.AbsolutePath, issue.ExpectedFilePath);

                Assert.DoesNotContain(result.Issues, item => item.IssueType == DicomRepositoryIssueType.MissingFile && item.InstanceId == primaryInstance.Id);
            }
            finally
            {
                await CreateDicomFileAsync(primaryScenario.PathInfo.AbsolutePath, primaryScenario.StudyInstanceUid, primaryScenario.SeriesInstanceUid, primaryScenario.SopInstanceUid);
                await CreateDicomFileAsync(secondaryScenario.PathInfo.AbsolutePath, secondaryScenario.StudyInstanceUid, secondaryScenario.SeriesInstanceUid, secondaryScenario.SopInstanceUid);
            }
        }

        [Fact, Priority(42)]
        public async Task Repair_Should_Check_All_Enabled_RepositoryLocations_Independently()
        {
            using RepositoryTestScenario primaryScenario = await CreateRepositoryScenarioAsync();
            using RepositoryTestScenario secondaryScenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            await primaryScenario.ImportSuccessfullyAsync();
            await secondaryScenario.ImportSuccessfullyAsync();

            Instance primaryInstance = await primaryScenario.GetPersistedInstanceAsync();
            Instance secondaryInstance = await secondaryScenario.GetPersistedInstanceAsync();

            try
            {
                File.Delete(primaryScenario.PathInfo.AbsolutePath);
                File.Delete(secondaryScenario.PathInfo.AbsolutePath);

                DicomRepositoryRepairResult result = await RepairAsync(CreateAllLocationsRepairRequest(repairMissingFiles: false));

                DicomRepositoryIssue primaryIssue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.MissingFile && item.InstanceId == primaryInstance.Id);
                DicomRepositoryIssue secondaryIssue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.MissingFile && item.InstanceId == secondaryInstance.Id);

                Assert.Equal(_fixture.RepositoryLocation!.Id, primaryIssue.RepositoryLocationId);
                Assert.Equal(primaryScenario.PathInfo.AbsolutePath, primaryIssue.ExpectedFilePath);

                Assert.Equal(_fixture.SecondaryRepositoryLocation!.Id, secondaryIssue.RepositoryLocationId);
                Assert.Equal(secondaryScenario.PathInfo.AbsolutePath, secondaryIssue.ExpectedFilePath);
                Assert.NotEqual(primaryIssue.RepositoryLocationId, secondaryIssue.RepositoryLocationId);
            }
            finally
            {
                await CreateDicomFileAsync(primaryScenario.PathInfo.AbsolutePath, primaryScenario.StudyInstanceUid, primaryScenario.SeriesInstanceUid, primaryScenario.SopInstanceUid);
                await CreateDicomFileAsync(secondaryScenario.PathInfo.AbsolutePath, secondaryScenario.StudyInstanceUid, secondaryScenario.SeriesInstanceUid, secondaryScenario.SopInstanceUid);
            }
        }

        [Fact, Priority(43)]
        public async Task Import_Should_Reject_Disabled_RepositoryLocation()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            try
            {
                await SetSecondaryRepositoryLocationStateAsync(isEnabled: false);

                DicomImportResult result = await ImportAsync(scenario.SourceDirectory, repositoryLocationId: _fixture.SecondaryRepositoryLocation!.Id);

                Assert.Equal(0, result.ImportedFiles);
                Assert.Equal(1, result.FailedFiles);
                Assert.Single(result.Errors);
                Assert.Contains("disabled", result.Errors[0], StringComparison.OrdinalIgnoreCase);
                Assert.False(File.Exists(scenario.PathInfo.AbsolutePath));
                Assert.Null(await scenario.TryGetPersistedInstanceAsync());
            }
            finally
            {
                await SetSecondaryRepositoryLocationStateAsync(isEnabled: true);
            }
        }

        [Fact, Priority(44)]
        public async Task Repair_Should_Inspect_Explicitly_Selected_Disabled_RepositoryLocation()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            await scenario.ImportSuccessfullyAsync();

            Instance instance = await scenario.GetPersistedInstanceAsync();

            try
            {
                await SetSecondaryRepositoryLocationStateAsync(isEnabled: false);
                File.Delete(scenario.PathInfo.AbsolutePath);

                Assert.False(File.Exists(scenario.PathInfo.AbsolutePath));

                DicomRepositoryRepairResult result = await RepairAsync(new DicomRepositoryRepairRequest
                {
                    VerifyFiles = true,
                    RepairMissingFiles = false,
                    RebuildRepositoryIndex = false,
                    RepositoryLocationId = _fixture.SecondaryRepositoryLocation!.Id
                });

                DicomRepositoryIssue issue = Assert.Single(result.Issues, item =>
                    item.IssueType == DicomRepositoryIssueType.MissingFile
                    && item.InstanceId == instance.Id);

                Assert.Equal(_fixture.SecondaryRepositoryLocation.Id, issue.RepositoryLocationId);
                Assert.Equal(scenario.PathInfo.AbsolutePath, issue.ExpectedFilePath);
                Assert.Equal(scenario.SopInstanceUid.UID, issue.ExpectedSopInstanceUid);
            }
            finally
            {
                await SetSecondaryRepositoryLocationStateAsync(isEnabled: true);

                string? directory = Path.GetDirectoryName(scenario.PathInfo.AbsolutePath);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await CreateDicomFileAsync(scenario.PathInfo.AbsolutePath, scenario.StudyInstanceUid, scenario.SeriesInstanceUid, scenario.SopInstanceUid);
            }
        }

        [Fact, Priority(45)]
        public async Task Import_Should_Report_Missing_RepositoryLocation()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory, repositoryLocationId: int.MaxValue);

            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(1, result.FailedFiles);
            Assert.Single(result.Errors);
            Assert.Contains(int.MaxValue.ToString(), result.Errors[0], StringComparison.Ordinal);
            Assert.Contains("does not exist", result.Errors[0], StringComparison.OrdinalIgnoreCase);
            Assert.Null(await scenario.TryGetPersistedInstanceAsync());
            Assert.False(File.Exists(scenario.PathInfo.AbsolutePath));
        }

        [Fact, Priority(46)]
        public async Task Repair_Should_Report_Missing_RepositoryLocation()
        {
            DicomRepositoryRepairResult result = await RepairAsync(new DicomRepositoryRepairRequest
            {
                VerifyFiles = true,
                RepairMissingFiles = false,
                RebuildRepositoryIndex = false,
                RepositoryLocationId = int.MaxValue
            });

            Assert.Equal(0, result.ScannedFiles);
            Assert.Equal(0, result.MissingFiles);
            Assert.Equal(0, result.UnavailableRepositoryLocations);
            Assert.Empty(result.Issues);
            Assert.Single(result.Errors);
            Assert.Contains(int.MaxValue.ToString(), result.Errors[0], StringComparison.Ordinal);
            Assert.Contains("does not exist", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        }

        [Fact, Priority(47)]
        public async Task Repair_Should_Create_RepositoryLocationUnavailable_When_Root_Does_Not_Exist()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            await scenario.ImportSuccessfullyAsync();

            Instance instance = await scenario.GetPersistedInstanceAsync();
            string unavailableRootPath = Path.Combine(Path.GetTempPath(), "MoprUnavailableRepositoryTests", Guid.NewGuid().ToString("N"));

            Assert.False(Directory.Exists(unavailableRootPath));

            try
            {
                RepositoryLocation repositoryLocation = await SetSecondaryRepositoryLocationStateAsync(isEnabled: true, rootPath: unavailableRootPath);

                DicomRepositoryRepairResult result = await RepairAsync(new DicomRepositoryRepairRequest
                {
                    VerifyFiles = true,
                    RepairMissingFiles = false,
                    RebuildRepositoryIndex = false,
                    RepositoryLocationId = repositoryLocation.Id
                });

                Assert.Equal(1, result.UnavailableRepositoryLocations);
                Assert.Equal(0, result.ScannedFiles);
                Assert.Equal(0, result.MissingFiles);

                DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.RepositoryLocationUnavailable && item.RepositoryLocationId == repositoryLocation.Id);

                Assert.Equal(unavailableRootPath, issue.ActualFilePath);
                Assert.False(issue.CanResolveAutomatically);
                Assert.False(issue.AutomaticallyResolved);
                Assert.Contains("does not exist", issue.TechnicalDetails, StringComparison.OrdinalIgnoreCase);

                /*
                 * The unavailable repository is reported once at location level.
                 * Its persisted Instance must not be misclassified as a missing file.
                 */
                Assert.DoesNotContain(result.Issues, item => item.IssueType == DicomRepositoryIssueType.MissingFile && item.InstanceId == instance.Id);
            }
            finally
            {
                await SetSecondaryRepositoryLocationStateAsync(isEnabled: true);
            }
        }

        [Fact, Priority(48)]
        public async Task Repair_All_Should_Skip_Disabled_RepositoryLocation()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            await scenario.ImportSuccessfullyAsync();

            Instance instance = await scenario.GetPersistedInstanceAsync();

            try
            {
                await SetSecondaryRepositoryLocationStateAsync(isEnabled: false);
                File.Delete(scenario.PathInfo.AbsolutePath);

                DicomRepositoryRepairResult result = await RepairAsync(CreateAllLocationsRepairRequest(repairMissingFiles: false));

                Assert.DoesNotContain(result.Issues, item => item.RepositoryLocationId == _fixture.SecondaryRepositoryLocation!.Id && item.InstanceId == instance.Id);
                Assert.DoesNotContain(result.Issues, item => item.RepositoryLocationId == _fixture.SecondaryRepositoryLocation!.Id && item.IssueType == DicomRepositoryIssueType.RepositoryLocationUnavailable);
            }
            finally
            {
                await SetSecondaryRepositoryLocationStateAsync(isEnabled: true);

                string? directory = Path.GetDirectoryName(scenario.PathInfo.AbsolutePath);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await CreateDicomFileAsync(scenario.PathInfo.AbsolutePath, scenario.StudyInstanceUid, scenario.SeriesInstanceUid, scenario.SopInstanceUid);
            }
        }

        [Fact, Priority(49)]
        public async Task Repair_Should_Remain_Clean_After_RepositoryLocation_Status_Tests()
        {
            RepositoryLocation primaryLocation = await _fixture.Persistence!.RepositoryLocation!.GetByIdAsync(_fixture.RepositoryLocation!.Id, TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("The primary repository test location does not exist.");
            RepositoryLocation secondaryLocation = await _fixture.Persistence.RepositoryLocation.GetByIdAsync(_fixture.SecondaryRepositoryLocation!.Id, TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("The secondary repository test location does not exist.");

            Assert.True(primaryLocation.IsEnabled);
            Assert.True(primaryLocation.IsDefault);
            Assert.Equal(_fixture.RepositoryRootPath, primaryLocation.RootPath);

            Assert.True(secondaryLocation.IsEnabled);
            Assert.False(secondaryLocation.IsDefault);
            Assert.Equal(_fixture.SecondaryRepositoryRootPath, secondaryLocation.RootPath);

            DicomRepositoryRepairResult result = await RepairAsync(CreateAllLocationsRepairRequest(repairMissingFiles: false));

            Assert.Equal(0, result.UnavailableRepositoryLocations);
            Assert.DoesNotContain(result.Issues, item => item.IssueType == DicomRepositoryIssueType.RepositoryLocationUnavailable);
        }

        [Fact, Priority(50)]
        public async Task Repair_Should_Reject_Absolute_RelativeFilePath()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            await scenario.ImportSuccessfullyAsync();

            Instance original = await scenario.GetPersistedInstanceAsync();

            Assert.False(string.IsNullOrWhiteSpace(original.RelativeFilePath));

            string originalRelativeFilePath = original.RelativeFilePath;
            string externalDirectory = CreateTemporaryDirectory();
            string externalFilePath = Path.Combine(externalDirectory, "ExternalImage.dcm");

            await CreateDicomFileAsync(externalFilePath, scenario.StudyInstanceUid, scenario.SeriesInstanceUid, scenario.SopInstanceUid);

            byte[] repositoryBytesBefore = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);
            byte[] externalBytesBefore = await ReadAllBytesAsync(externalFilePath);

            try
            {
                Instance instance = await scenario.SetRelativeFilePathAsync(externalFilePath);

                DicomRepositoryRepairResult result = await RepairAsync();

                Assert.Equal(1, result.RelationshipConflicts);

                DicomRepositoryIssue issue = Assert.Single(result.Issues, item =>
                    item.IssueType == DicomRepositoryIssueType.RelationshipConflict
                    && item.InstanceId == instance.Id);

                Assert.Equal(_fixture.RepositoryLocation!.Id, issue.RepositoryLocationId);
                Assert.Empty(issue.ExpectedFilePath);
                Assert.Empty(issue.ActualFilePath);
                Assert.Empty(issue.RecoveryCandidateFilePath);
                Assert.Equal(scenario.SopInstanceUid.UID, issue.ExpectedSopInstanceUid);
                Assert.False(issue.CanResolveAutomatically);
                Assert.False(issue.AutomaticallyResolved);
                Assert.Contains("unsafe relative repository path", issue.TechnicalDetails, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(externalFilePath, issue.TechnicalDetails, StringComparison.Ordinal);

                /*
                 * Neither the canonical repository file nor the external file may be
                 * read as a repair candidate, moved, overwritten or deleted.
                 */
                Assert.Equal(repositoryBytesBefore, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
                Assert.Equal(externalBytesBefore, await ReadAllBytesAsync(externalFilePath));
                Assert.Equal(externalFilePath, (await scenario.GetPersistedInstanceAsync()).RelativeFilePath);
            }
            finally
            {
                await scenario.SetRelativeFilePathAsync(originalRelativeFilePath);
                DeleteDirectory(externalDirectory);
            }
        }

        [Fact, Priority(51)]
        public async Task Repair_Should_Reject_ParentDirectory_Traversal_In_RelativeFilePath()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            await scenario.ImportSuccessfullyAsync();

            Instance original = await scenario.GetPersistedInstanceAsync();

            Assert.False(string.IsNullOrWhiteSpace(original.RelativeFilePath));

            string originalRelativeFilePath = original.RelativeFilePath;
            string traversalPath = Path.Combine("..", "OutsideRepository", $"{scenario.SopInstanceUid.UID}.dcm");
            byte[] repositoryBytesBefore = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);

            /*
             * The shared fixture contains persisted records from preceding scenarios.
             * Capture the current location-wide baseline so this test measures only
             * the additional effect of its deliberately manipulated Instance.
             */
            DicomRepositoryRepairResult initial = await RepairAsync();

            try
            {
                Instance instance = await scenario.SetRelativeFilePathAsync(traversalPath);

                DicomRepositoryRepairResult result = await RepairAsync();

                Assert.Equal(initial.RelationshipConflicts + 1, result.RelationshipConflicts);
                Assert.Equal(initial.MissingFiles, result.MissingFiles);
                Assert.Equal(initial.MisplacedFiles, result.MisplacedFiles);
                Assert.Equal(initial.RepairedFiles, result.RepairedFiles);
                Assert.Equal(initial.IdentityMismatchFiles, result.IdentityMismatchFiles);
                Assert.Equal(initial.InvalidDicomFiles, result.InvalidDicomFiles);
                Assert.Equal(initial.UnreadableFiles, result.UnreadableFiles);
                Assert.Equal(initial.OrphanedFiles, result.OrphanedFiles);
                Assert.Equal(initial.DuplicateFiles, result.DuplicateFiles);
                Assert.Equal(initial.IncompleteImportFiles, result.IncompleteImportFiles);
                Assert.Equal(initial.UnavailableRepositoryLocations, result.UnavailableRepositoryLocations);
                Assert.Equal(initial.Issues.Count + 1, result.Issues.Count);
                Assert.Equal(initial.Errors.Count + 1, result.Errors.Count);

                DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.RelationshipConflict && item.InstanceId == instance.Id && item.RepositoryLocationId == _fixture.RepositoryLocation!.Id);

                Assert.Equal(_fixture.RepositoryLocation!.Id, issue.RepositoryLocationId);
                Assert.Empty(issue.ExpectedFilePath);
                Assert.Empty(issue.ActualFilePath);
                Assert.Empty(issue.RecoveryCandidateFilePath);
                Assert.Equal(scenario.SopInstanceUid.UID, issue.ExpectedSopInstanceUid);
                Assert.False(issue.CanResolveAutomatically);
                Assert.False(issue.AutomaticallyResolved);
                Assert.Null(issue.ResolvedAtUtc);
                Assert.Contains(traversalPath, issue.TechnicalDetails, StringComparison.Ordinal);
                Assert.Contains("parent-directory", issue.TechnicalDetails, StringComparison.OrdinalIgnoreCase);

                /*
                 * The canonical DICOM file remains untouched. The unsafe Persistence
                 * value is reported but is never normalized or used for file access.
                 */
                Assert.Equal(repositoryBytesBefore, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
                Assert.Equal(traversalPath, (await scenario.GetPersistedInstanceAsync()).RelativeFilePath);
            }
            finally
            {
                /*
                 * Restore the original Persistence relationship even if verification
                 * or an assertion fails.
                 */
                await scenario.SetRelativeFilePathAsync(originalRelativeFilePath);
            }
        }

        [Fact, Priority(52)]
        public async Task Repair_Should_Not_Use_RecoveryCandidate_From_Different_RepositoryLocation()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            await scenario.ImportSuccessfullyAsync();

            Instance instance = await scenario.GetPersistedInstanceAsync();
            byte[] primaryBytesBefore = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);

            DicomRepositoryPathInfo secondaryPathInfo = CreatePathInfo(_fixture.SecondaryRepositoryLocation!, scenario.StudyInstanceUid, scenario.SeriesInstanceUid, scenario.SopInstanceUid);
            string? secondaryDirectory = Path.GetDirectoryName(secondaryPathInfo.AbsolutePath);

            Assert.False(string.IsNullOrWhiteSpace(secondaryDirectory));

            try
            {
                Directory.CreateDirectory(secondaryDirectory!);

                /*
                 * The secondary repository contains a valid physical file with the
                 * expected SOP identity, but the persisted Instance belongs to the
                 * primary repository location.
                 */
                await CreateDicomFileAsync(secondaryPathInfo.AbsolutePath, scenario.StudyInstanceUid, scenario.SeriesInstanceUid, scenario.SopInstanceUid);

                await scenario.ReplaceRepositoryFileWithInvalidContentAsync();

                string invalidPrimaryContentBefore = await File.ReadAllTextAsync(scenario.PathInfo.AbsolutePath, TestContext.Current.CancellationToken);
                byte[] secondaryBytesBefore = await ReadAllBytesAsync(secondaryPathInfo.AbsolutePath);

                DicomRepositoryRepairResult result = await RepairAsync(new DicomRepositoryRepairRequest
                {
                    VerifyFiles = true,
                    RepairMissingFiles = true,
                    RebuildRepositoryIndex = false,
                    RepositoryLocationId = _fixture.RepositoryLocation!.Id
                });

                DicomRepositoryIssue issue = Assert.Single(result.Issues, item => item.IssueType == DicomRepositoryIssueType.InvalidDicomFile && item.InstanceId == instance.Id);

                Assert.Equal(_fixture.RepositoryLocation.Id, issue.RepositoryLocationId);
                Assert.Empty(issue.RecoveryCandidateFilePath);
                Assert.False(issue.CanResolveAutomatically);
                Assert.False(issue.AutomaticallyResolved);

                /*
                 * The primary and secondary locations are isolated. The valid file in
                 * the secondary repository must not replace or recover the invalid
                 * primary file.
                 */
                Assert.Equal(invalidPrimaryContentBefore, await File.ReadAllTextAsync(scenario.PathInfo.AbsolutePath, TestContext.Current.CancellationToken));
                Assert.Equal(secondaryBytesBefore, await ReadAllBytesAsync(secondaryPathInfo.AbsolutePath));
            }
            finally
            {
                await File.WriteAllBytesAsync(scenario.PathInfo.AbsolutePath, primaryBytesBefore, TestContext.Current.CancellationToken);

                if (File.Exists(secondaryPathInfo.AbsolutePath))
                {
                    File.Delete(secondaryPathInfo.AbsolutePath);
                }

                DeleteDirectory(GetRepositoryStudyDirectory(secondaryPathInfo));
            }
        }

        [Fact, Priority(53)]
        public async Task RepositoryService_Should_Reject_NonCanonical_Traversal_That_Resolves_Inside_Root()
        {
            string relativePath = Path.Combine("Study", "Temporary", "..", "Series", "Image.dcm");

            UnauthorizedAccessException exception = Assert.Throws<UnauthorizedAccessException>(() => _fixture.Repository!.RepositoryService!.GetAbsolutePath(_fixture.RepositoryLocation!, relativePath));

            Assert.Contains("parent-directory", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact, Priority(54)]
        public async Task Repair_Should_Remain_Operational_After_PathSecurity_Tests()
        {
            using RepositoryTestScenario primaryScenario = await CreateRepositoryScenarioAsync();
            using RepositoryTestScenario secondaryScenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            await primaryScenario.ImportSuccessfullyAsync();
            await secondaryScenario.ImportSuccessfullyAsync();

            Instance primaryInstance = await primaryScenario.GetPersistedInstanceAsync();
            Instance secondaryInstance = await secondaryScenario.GetPersistedInstanceAsync();

            DicomRepositoryRepairResult result = await RepairAsync(CreateAllLocationsRepairRequest(repairMissingFiles: false));

            Assert.DoesNotContain(result.Issues, item => item.InstanceId == primaryInstance.Id || item.InstanceId == secondaryInstance.Id);

            Assert.True(File.Exists(primaryScenario.PathInfo.AbsolutePath));
            Assert.True(File.Exists(secondaryScenario.PathInfo.AbsolutePath));
        }
        
        [Fact, Priority(55)]
        public async Task Import_Should_Remove_Created_File_When_Persistence_Fails()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory, createdByUserId: int.MaxValue);

            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(1, result.FailedFiles);
            Assert.Single(result.Errors);
            Assert.Contains(int.MaxValue.ToString(), result.Errors[0], StringComparison.Ordinal);
            Assert.False(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.Null(await scenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        [Fact, Priority(56)]
        public async Task Import_Should_Not_Delete_Existing_Identical_File_When_Persistence_Fails()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            string? destinationDirectory = Path.GetDirectoryName(scenario.PathInfo.AbsolutePath);

            Assert.False(string.IsNullOrWhiteSpace(destinationDirectory));

            Directory.CreateDirectory(destinationDirectory!);
            File.Copy(scenario.SourceFilePath, scenario.PathInfo.AbsolutePath);

            byte[] originalBytes = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory, createdByUserId: int.MaxValue);

            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(1, result.FailedFiles);
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.Equal(originalBytes, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
            Assert.Null(await scenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        [Fact, Priority(57)]
        public async Task Import_Should_Restore_Overwritten_File_When_Persistence_Fails()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            string? destinationDirectory = Path.GetDirectoryName(scenario.PathInfo.AbsolutePath);

            Assert.False(string.IsNullOrWhiteSpace(destinationDirectory));

            Directory.CreateDirectory(destinationDirectory!);

            DicomUID originalPhysicalSopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(
                scenario.PathInfo.AbsolutePath,
                scenario.StudyInstanceUid,
                scenario.SeriesInstanceUid,
                originalPhysicalSopInstanceUid);

            byte[] originalRepositoryBytes = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);
            byte[] incomingBytes = await ReadAllBytesAsync(scenario.SourceFilePath);

            Assert.NotEqual(originalRepositoryBytes, incomingBytes);

            DicomImportResult result = await ImportAsync(
                scenario.SourceDirectory,
                allowOverwrite: true,
                createdByUserId: int.MaxValue);

            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(1, result.FailedFiles);
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.Equal(originalRepositoryBytes, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
            Assert.Null(await scenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        [Fact, Priority(58)]
        public async Task Import_Should_Skip_Identical_File_Even_When_Overwrite_Is_Allowed()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            DicomImportResult firstResult = await ImportAsync(scenario.SourceDirectory);
            byte[] repositoryBytesBefore = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);
            DateTime lastWriteTimeUtcBefore = File.GetLastWriteTimeUtc(scenario.PathInfo.AbsolutePath);

            DicomImportResult secondResult = await ImportAsync(scenario.SourceDirectory, allowOverwrite: true);

            Assert.Equal(1, firstResult.ImportedFiles);
            Assert.Equal(0, firstResult.FailedFiles);
            Assert.Equal(0, secondResult.ImportedFiles);
            Assert.Equal(1, secondResult.SkippedFiles);
            Assert.Equal(0, secondResult.FailedFiles);
            Assert.Empty(secondResult.Errors);
            Assert.Equal(repositoryBytesBefore, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
            Assert.Equal(lastWriteTimeUtcBefore, File.GetLastWriteTimeUtc(scenario.PathInfo.AbsolutePath));
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        [Fact, Priority(59)]
        public async Task Import_Should_Remove_Temporary_And_Backup_Artifacts_After_Successful_Overwrite()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            DicomImportResult firstResult = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, firstResult.ImportedFiles);
            Assert.Equal(0, firstResult.FailedFiles);

            DicomFile changedDicomFile = DicomFile.Open(scenario.SourceFilePath);
            changedDicomFile.Dataset.AddOrUpdate(DicomTag.StudyDescription, $"Changed_{Guid.NewGuid():N}");
            await changedDicomFile.SaveAsync(scenario.SourceFilePath);

            byte[] changedSourceBytes = await ReadAllBytesAsync(scenario.SourceFilePath);

            DicomImportResult secondResult = await ImportAsync(scenario.SourceDirectory, allowOverwrite: true);

            Assert.Equal(1, secondResult.ImportedFiles);
            Assert.Equal(0, secondResult.SkippedFiles);
            Assert.Equal(0, secondResult.FailedFiles);
            Assert.Empty(secondResult.Errors);
            Assert.Equal(changedSourceBytes, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        [Fact, Priority(60)]
        public async Task Import_Should_Remain_Operational_After_Compensation_Tests()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.NotNull(await scenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        [Fact, Priority(61)]
        public async Task Import_Should_Allow_Concurrent_Identical_Imports_Without_Artifacts()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            Task<DicomImportResult> firstImport = ImportAsync(scenario.SourceDirectory);
            Task<DicomImportResult> secondImport = ImportAsync(scenario.SourceDirectory);

            DicomImportResult[] results = await Task.WhenAll(firstImport, secondImport);

            Assert.Equal(1, results.Sum(item => item.ImportedFiles));
            Assert.Equal(1, results.Sum(item => item.SkippedFiles));
            Assert.Equal(0, results.Sum(item => item.FailedFiles));
            Assert.All(results, item => Assert.Empty(item.Errors));
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.NotNull(await scenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        [Fact, Priority(62)]
        public async Task Import_Should_Not_Replace_Different_Concurrent_Destination_When_Overwrite_Is_Disallowed()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            string? destinationDirectory = Path.GetDirectoryName(scenario.PathInfo.AbsolutePath);

            Assert.False(string.IsNullOrWhiteSpace(destinationDirectory));

            Directory.CreateDirectory(destinationDirectory!);

            DicomUID existingSopInstanceUid = DicomUID.Generate();

            await CreateDicomFileAsync(
                scenario.PathInfo.AbsolutePath,
                scenario.StudyInstanceUid,
                scenario.SeriesInstanceUid,
                existingSopInstanceUid);

            byte[] existingBytes = await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath);

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory, allowOverwrite: false);

            Assert.Equal(0, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(1, result.FailedFiles);
            Assert.Single(result.Errors);
            Assert.Equal(existingBytes, await ReadAllBytesAsync(scenario.PathInfo.AbsolutePath));
            Assert.Null(await scenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        [Fact, Priority(63)]
        public async Task Import_Should_Remain_Operational_After_Concurrent_Import_Tests()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();

            DicomImportResult result = await ImportAsync(scenario.SourceDirectory);

            Assert.Equal(1, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.NotNull(await scenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        private static async Task AssertFilesEqualAsync(string expectedPath, string actualPath) => Assert.Equal(await ReadAllBytesAsync(expectedPath), await ReadAllBytesAsync(actualPath));

        private static async Task<byte[]> ReadAllBytesAsync(string filePath) => await File.ReadAllBytesAsync(filePath, TestContext.Current.CancellationToken);

        private static async Task<string> ReadSopInstanceUidAsync(string filePath)
        {
            DicomFile file = await Task.Run(() => DicomFile.Open(filePath), TestContext.Current.CancellationToken);
            return file.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
        }

        private DicomRepositoryPathInfo CreatePathInfo(DicomUID studyInstanceUid, DicomUID seriesInstanceUid, DicomUID sopInstanceUid) => CreatePathInfo(_fixture.RepositoryLocation!, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        private DicomRepositoryPathInfo CreatePathInfo(RepositoryLocation repositoryLocation, DicomUID studyInstanceUid, DicomUID seriesInstanceUid, DicomUID sopInstanceUid) => _fixture.Repository!.RepositoryService!.CreatePathInfo(repositoryLocation, studyInstanceUid.UID, seriesInstanceUid.UID, sopInstanceUid.UID);

        private async Task<DicomImportResult> ImportAsync(string sourcePath, bool allowOverwrite = false, int? repositoryLocationId = null, int? createdByUserId = null) => await _fixture.Repository!.ImportService!.ImportAsync(new DicomImportRequest
        {
            SourcePath = sourcePath,
            SourceType = ImportSourceType.Directory,
            RepositoryLocationId = repositoryLocationId ?? _fixture.RepositoryLocation!.Id,
            AllowOverwrite = allowOverwrite,
            CreatedByUserId = createdByUserId ?? _fixture.TestUser!.Id
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