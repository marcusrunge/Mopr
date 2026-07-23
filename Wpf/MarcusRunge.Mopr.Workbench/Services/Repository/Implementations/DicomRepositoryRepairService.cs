using FellowOakDicom;
using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    internal class DicomRepositoryRepairService : CreateableBindableBase<IDicomRepositoryRepairService, DicomRepositoryRepairService, IRepositoryBase>, IDicomRepositoryRepairService
    {
        private IRepositoryBase? _base;
        private IRepositoryBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");
        private IInstanceRepository InstanceRepository => Persistence.Instance ?? throw new InvalidOperationException("The instance repository has not been initialized.");
        private IPersistence Persistence => Base.Persistence ?? throw new InvalidOperationException("Persistence has not been initialized.");
        private IRepository Repository => Base as IRepository ?? throw new InvalidOperationException("The repository base does not implement IRepository.");
        private string RepositoryPath => Base.ApplicationConfiguration?.Repository?.DicomRepositoryPath ?? throw new InvalidOperationException("The DICOM repository path has not been configured.");
        private IDicomRepositoryService RepositoryService => Repository.RepositoryService ?? throw new InvalidOperationException("The repository service has not been initialized.");
        private ISeriesRepository SeriesRepository => Persistence.Series ?? throw new InvalidOperationException("The series repository has not been initialized.");
        private IStudyRepository StudyRepository => Persistence.Study ?? throw new InvalidOperationException("The study repository has not been initialized.");

        /// <inheritdoc/>
        public async Task<DicomRepositoryRepairResult> RepairAsync(DicomRepositoryRepairRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            DicomRepositoryRepairResult result = new();

            if (!request.VerifyFiles)
            {
                return result;
            }

            IDictionary<string, string> repositoryFiles = await CreateRepositoryFileIndexAsync(result, cancellationToken);

            HashSet<string> persistedSopInstanceUids = new(StringComparer.Ordinal);

            IList<Study> studies = await StudyRepository.GetAllAsync(cancellationToken);

            foreach (Study study in studies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                IList<Series> seriesItems = await SeriesRepository.GetByStudyIdAsync(study.Id, cancellationToken);

                foreach (Series series in seriesItems)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IList<Instance> instances = await InstanceRepository.GetBySeriesIdAsync(series.Id, cancellationToken);

                    foreach (Instance instance in instances)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!string.IsNullOrWhiteSpace(instance.SopInstanceUid))
                        {
                            persistedSopInstanceUids.Add(instance.SopInstanceUid);
                        }

                        await VerifyInstanceAsync(instance, repositoryFiles, request, result, cancellationToken);
                    }
                }
            }

            RegisterOrphanedFiles(repositoryFiles, persistedSopInstanceUids, result, cancellationToken);

            return result;
        }

        protected override void OnCreate(IRepositoryBase @base) => _base = @base;

        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task<string> ReadSopInstanceUidAsync(string filePath, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                DicomFile dicomFile = DicomFile.Open(filePath);

                return dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
            },
                cancellationToken);
        }

        private static void RegisterOrphanedFiles(IDictionary<string, string> repositoryFiles, ISet<string> persistedSopInstanceUids, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            foreach (string sopInstanceUid in repositoryFiles.Keys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!persistedSopInstanceUids.Contains(sopInstanceUid))
                {
                    result.OrphanedFiles++;
                }
            }
        }

        private void AddError(DicomRepositoryRepairResult result, string message, Exception? exception = null)
        {
            result.Errors.Add(message);

            if (exception is not null)
            {
                Base.OnExceptionThrown(exception);
            }
        }

        private async Task<Dictionary<string, string>> CreateRepositoryFileIndexAsync(DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<string, string> filesBySopInstanceUid = new(StringComparer.Ordinal);

            if (!Directory.Exists(RepositoryPath))
            {
                return filesBySopInstanceUid;
            }

            return await Task.Run(() =>
            {
                try
                {
                    foreach (string filePath in Directory.EnumerateFiles(RepositoryPath, "*", SearchOption.AllDirectories))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (filePath.EndsWith(".importing", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        try
                        {
                            DicomFile dicomFile = DicomFile.Open(filePath);

                            string sopInstanceUid = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);

                            if (string.IsNullOrWhiteSpace(sopInstanceUid))
                            {
                                continue;
                            }

                            if (!filesBySopInstanceUid.TryAdd(sopInstanceUid, filePath))
                            {
                                result.DuplicateFiles++;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (DicomFileException)
                        {
                            // A regular non-DICOM or structurally invalid DICOM file
                            // is ignored during repository indexing.
                        }
                        catch (Exception exception)
                        {
                            AddError(result, $"Repository file '{filePath}' could not be inspected: " + exception.Message, exception);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    AddError(result, $"The DICOM repository '{RepositoryPath}' could not be scanned: " + exception.Message, exception);
                }

                return filesBySopInstanceUid;
            },
                cancellationToken);
        }

        private async Task VerifyInstanceAsync(Instance instance, IDictionary<string, string> repositoryFiles, DicomRepositoryRepairRequest request, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            result.ScannedFiles++;

            if (string.IsNullOrWhiteSpace(instance.SopInstanceUid))
            {
                result.MissingFiles++;
                AddError(result, $"Instance with ID '{instance.Id}' has no SOP instance UID.");
                return;
            }

            string? expectedAbsolutePath = null;

            if (!string.IsNullOrWhiteSpace(instance.RelativeFilePath))
            {
                expectedAbsolutePath = RepositoryService.GetAbsolutePath(instance.RelativeFilePath);

                if (File.Exists(expectedAbsolutePath))
                {
                    try
                    {
                        string actualSopInstanceUid = await ReadSopInstanceUidAsync(expectedAbsolutePath, cancellationToken);

                        if (string.Equals(actualSopInstanceUid, instance.SopInstanceUid, StringComparison.Ordinal))
                        {
                            return;
                        }

                        result.IdentityMismatchFiles++;
                        AddError(result, $"Repository file '{expectedAbsolutePath}' contains SOP instance UID " + $"'{actualSopInstanceUid}', but instance '{instance.Id}' expects " + $"'{instance.SopInstanceUid}'.");
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (DicomFileException)
                    {
                        result.IdentityMismatchFiles++;
                        AddError(result, $"Repository file '{expectedAbsolutePath}' for instance " + $"'{instance.SopInstanceUid}' is not a valid DICOM file.");
                        return;
                    }
                    catch (Exception exception)
                    {
                        AddError(result, $"Repository file '{expectedAbsolutePath}' for instance " + $"'{instance.SopInstanceUid}' could not be verified: " + exception.Message, exception);
                        return;
                    }
                }
            }

            if (!repositoryFiles.TryGetValue(instance.SopInstanceUid, out string? actualFilePath))
            {
                result.MissingFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.MissingFile,
                    InstanceId = instance.Id,
                    ExpectedFilePath = expectedAbsolutePath ?? string.Empty,
                    ExpectedSopInstanceUid = instance.SopInstanceUid,
                    CanResolveAutomatically = false,
                    TechnicalDetails = $"No repository file was found for persisted instance '{instance.Id}' with SOP instance UID '{instance.SopInstanceUid}'."
                });

                return;
            }

            if (!request.RepairMissingFiles)
            {
                result.MisplacedFiles++;
                return;
            }

            if (string.IsNullOrWhiteSpace(expectedAbsolutePath))
            {
                result.MissingFiles++;
                AddError(result, $"Instance '{instance.SopInstanceUid}' has no expected repository path.");
                return;
            }

            string? expectedDirectory = Path.GetDirectoryName(expectedAbsolutePath);

            if (string.IsNullOrWhiteSpace(expectedDirectory))
            {
                AddError(result, $"The expected repository directory for instance " + $"'{instance.SopInstanceUid}' could not be determined.");
                return;
            }

            // Race-condition protection:
            // The destination may have been occupied after the initial check.
            if (File.Exists(expectedAbsolutePath))
            {
                AddError(result, $"The expected repository path '{expectedAbsolutePath}' " + $"for instance '{instance.SopInstanceUid}' is already occupied.");
                return;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Run(() =>
                {
                    Directory.CreateDirectory(expectedDirectory);
                    File.Move(actualFilePath, expectedAbsolutePath);
                }, cancellationToken);

                repositoryFiles[instance.SopInstanceUid] = expectedAbsolutePath;
                result.RepairedFiles++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                AddError(result, $"File for instance '{instance.SopInstanceUid}' could not be repaired " + $"from '{actualFilePath}' to '{expectedAbsolutePath}': " + exception.Message, exception);
            }
        }
    }
}