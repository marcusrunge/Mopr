using FellowOakDicom;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    [TestCaseOrderer(typeof(PriorityOrderer))]
    public sealed class RepositoryIntegrationTests(RepositoryFixture fixture) : IClassFixture<RepositoryFixture>
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
            DicomImportResult result = await _fixture.Repository!.ImportService!.ImportAsync(new DicomImportRequest
            {
                SourcePath = Guid.NewGuid().ToString(),
                SourceType = ImportSourceType.Directory
            }, TestContext.Current.CancellationToken);
            Assert.Equal(1, result.FailedFiles);
            Assert.NotEmpty(result.Errors);
        }

        [Fact, Priority(7)]
        public async Task Import_Should_Count_Files()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "A.dcm"), "Test");
                File.WriteAllText(Path.Combine(directory, "B.dcm"), "Test");
                File.WriteAllText(Path.Combine(directory, "C.dcm"), "Test");
                DicomImportResult result = await _fixture.Repository!.ImportService!.ImportAsync(new DicomImportRequest
                {
                    SourcePath = directory,
                    SourceType = ImportSourceType.Directory
                }, TestContext.Current.CancellationToken);
                Assert.Equal(3, result.DiscoveredFiles);
                Assert.Equal(0, result.FailedFiles);
                Assert.Empty(result.Errors);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Fact, Priority(8)]
        public async Task Import_Should_Count_Files_In_SubDirectories()
        {
            string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                string child = Path.Combine(root, "Child");
                Directory.CreateDirectory(child);
                File.WriteAllText(Path.Combine(root, "A.dcm"), "Test");
                File.WriteAllText(Path.Combine(child, "B.dcm"), "Test");
                DicomImportResult result = await _fixture.Repository!.ImportService!.ImportAsync(new DicomImportRequest
                {
                    SourcePath = root,
                    SourceType = ImportSourceType.Directory
                }, TestContext.Current.CancellationToken);
                Assert.Equal(2, result.DiscoveredFiles);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [Fact, Priority(9)]
        public async Task Import_Should_Find_No_Dicom_Files()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "A.txt"), "Test");
                File.WriteAllText(Path.Combine(directory, "B.txt"), "Test");
                File.WriteAllText(Path.Combine(directory, "C.txt"), "Test");
                DicomImportResult result = await _fixture.Repository!.ImportService!.ImportAsync(new DicomImportRequest
                {
                    SourcePath = directory,
                    SourceType = ImportSourceType.Directory
                }, TestContext.Current.CancellationToken);

                Assert.Equal(3, result.DiscoveredFiles);
                Assert.Equal(0, result.ValidDicomFiles);
                Assert.Equal(0, result.FailedFiles);
                Assert.Empty(result.Errors);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Fact, Priority(10)]
        public async Task Import_Should_Find_Valid_Dicom_File()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(directory);

                string filePath = Path.Combine(directory, "Image.dcm");

                DicomDataset dataset = new()
                {
                    {
                        DicomTag.SOPClassUID,
                        DicomUID.SecondaryCaptureImageStorage
                    },
                    {
                        DicomTag.SOPInstanceUID,
                        DicomUID.Generate()
                    }
                };

                DicomFile dicomFile = new(dataset);

                await dicomFile.SaveAsync(filePath);

                DicomImportResult result = await _fixture.Repository!.ImportService!.ImportAsync(new DicomImportRequest
                {
                    SourcePath = directory,
                    SourceType = ImportSourceType.Directory
                }, TestContext.Current.CancellationToken);

                Assert.Equal(1, result.DiscoveredFiles);
                Assert.Equal(1, result.ValidDicomFiles);
                Assert.Equal(0, result.FailedFiles);
                Assert.Equal(0, result.ImportableFiles);
                Assert.Empty(result.Errors);
                DicomImportFileInfo fileInfo = Assert.Single(result.Files);
                Assert.True(fileInfo.IsDicomFile);
                Assert.False(fileInfo.IsImportable);
                Assert.Equal("Image.dcm", fileInfo.FileName);
                Assert.Equal(filePath, fileInfo.FilePath);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Fact, Priority(11)]
        public async Task Import_Should_Read_Dicom_Instance_Uids()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(directory);
                string filePath = Path.Combine(directory, "Image.dcm");
                DicomUID studyInstanceUid = DicomUID.Generate();
                DicomUID seriesInstanceUid = DicomUID.Generate();
                DicomUID sopInstanceUid = DicomUID.Generate();

                DicomDataset dataset = new()
                {
                    {
                        DicomTag.SOPClassUID,
                        DicomUID.SecondaryCaptureImageStorage
                    },
                    {
                        DicomTag.StudyInstanceUID,
                        studyInstanceUid
                    },
                    {
                        DicomTag.SeriesInstanceUID,
                        seriesInstanceUid
                    },
                    {
                        DicomTag.SOPInstanceUID,
                        sopInstanceUid
                    }
                };

                DicomFile dicomFile = new(dataset);

                await dicomFile.SaveAsync(filePath);

                DicomImportResult result = await _fixture.Repository!.ImportService!.ImportAsync(new DicomImportRequest
                {
                    SourcePath = directory,
                    SourceType = ImportSourceType.Directory
                }, TestContext.Current.CancellationToken);

                Assert.Equal(1, result.DiscoveredFiles);
                Assert.Equal(1, result.ValidDicomFiles);
                Assert.Equal(1, result.ImportableFiles);
                DicomImportFileInfo fileInfo = Assert.Single(result.Files);
                Assert.True(fileInfo.IsDicomFile);
                Assert.True(fileInfo.IsImportable);
                Assert.Equal(studyInstanceUid.UID, fileInfo.StudyInstanceUid);
                Assert.Equal(seriesInstanceUid.UID, fileInfo.SeriesInstanceUid);
                Assert.Equal(sopInstanceUid.UID, fileInfo.SopInstanceUid);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Fact, Priority(12)]
        public async Task Import_Should_Distinguish_Dicom_And_Non_Dicom_Files()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(directory);

                string childDirectory = Path.Combine(directory, "Child");

                Directory.CreateDirectory(childDirectory);

                string dicomFilePath = Path.Combine(childDirectory, "Image.dcm");
                string textFilePath = Path.Combine(directory, "Readme.txt");

                DicomDataset dataset = new()        {
                    {
                        DicomTag.SOPClassUID,
                        DicomUID.SecondaryCaptureImageStorage
                    },
                    {
                        DicomTag.StudyInstanceUID,
                        DicomUID.Generate()
                    },
                    {
                        DicomTag.SeriesInstanceUID,
                        DicomUID.Generate()
                    },
                    {
                        DicomTag.SOPInstanceUID,
                        DicomUID.Generate()
                    }
                };

                DicomFile dicomFile = new(dataset);
                await dicomFile.SaveAsync(dicomFilePath);

                await File.WriteAllTextAsync(textFilePath, "This is not a DICOM file.", TestContext.Current.CancellationToken);

                DicomImportResult result = await _fixture.Repository!.ImportService!.ImportAsync(new DicomImportRequest
                {
                    SourcePath = directory,
                    SourceType = ImportSourceType.Directory
                },
                            TestContext.Current.CancellationToken);

                Assert.Equal(2, result.DiscoveredFiles);
                Assert.Equal(1, result.ValidDicomFiles);
                Assert.Equal(1, result.ImportableFiles);
                Assert.Equal(0, result.FailedFiles);
                Assert.Empty(result.Errors);
                Assert.Contains(result.Files, fileInfo => fileInfo.FilePath == dicomFilePath && fileInfo.IsDicomFile);
                Assert.Contains(result.Files, fileInfo => fileInfo.FilePath == textFilePath && !fileInfo.IsDicomFile);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(
                        directory,
                        true);
                }
            }
        }
    }
}