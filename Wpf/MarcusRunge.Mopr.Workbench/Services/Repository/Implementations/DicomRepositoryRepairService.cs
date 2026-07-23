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

            IDictionary<string, DicomRepositoryFileIndexEntry> repositoryFiles = await CreateRepositoryFileIndexAsync(result, cancellationToken);

            RegisterDuplicateFiles(repositoryFiles, result, cancellationToken);

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

        private static void RegisterDuplicateFiles(IDictionary<string, DicomRepositoryFileIndexEntry> repositoryFiles, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            foreach (DicomRepositoryFileIndexEntry entry in repositoryFiles.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.FilePaths.Count <= 1)
                {
                    continue;
                }

                /*
                 * DuplicateFiles retains its existing meaning:
                 * it counts additional physical copies rather than affected
                 * SOP instance UIDs.
                 *
                 * Two files produce one duplicate.
                 * Three files produce two duplicates.
                 */
                result.DuplicateFiles += entry.FilePaths.Count - 1;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.DuplicateFile,
                    ExpectedSopInstanceUid = entry.SopInstanceUid,
                    ActualSopInstanceUid = entry.SopInstanceUid,
                    CanResolveAutomatically = false,
                    AutomaticallyResolved = false,
                    TechnicalDetails = $"The repository contains {entry.FilePaths.Count} physical files with SOP instance UID '{entry.SopInstanceUid}'. The discovered locations are: {string.Join(", ", entry.FilePaths.Select(filePath => $"'{filePath}'"))}. No file was changed."
                });
            }
        }

        private static void RegisterOrphanedFiles(IDictionary<string, DicomRepositoryFileIndexEntry> repositoryFiles, HashSet<string> persistedSopInstanceUids, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
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

        private async Task<Dictionary<string, DicomRepositoryFileIndexEntry>> CreateRepositoryFileIndexAsync(DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<string, DicomRepositoryFileIndexEntry> filesBySopInstanceUid = new(StringComparer.Ordinal);

            if (!Directory.Exists(RepositoryPath))
            {
                return filesBySopInstanceUid;
            }

            return await Task.Run(
                () =>
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
                                if (!filesBySopInstanceUid.TryGetValue(sopInstanceUid, out DicomRepositoryFileIndexEntry? entry))
                                {
                                    entry = new DicomRepositoryFileIndexEntry
                                    {
                                        SopInstanceUid = sopInstanceUid
                                    };

                                    filesBySopInstanceUid.Add(sopInstanceUid, entry);
                                }

                                entry.FilePaths.Add(filePath);
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (DicomFileException)
                            {
                                /*
                                 * Regular non-DICOM files and structurally invalid
                                 * DICOM files remain ignored by the general index
                                 * at this stage.
                                 */
                            }
                            catch (Exception exception)
                            {
                                AddError(result, $"Repository file '{filePath}' could not be inspected: {exception.Message}", exception);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        AddError(result, $"The DICOM repository '{RepositoryPath}' could not be scanned: {exception.Message}", exception);
                    }

                    return filesBySopInstanceUid;
                },
                cancellationToken);
        }

        private async Task VerifyInstanceAsync(Instance instance, IDictionary<string, DicomRepositoryFileIndexEntry> repositoryFiles, DicomRepositoryRepairRequest request, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
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
                            /*
                             * The expected file is valid and has the correct identity.
                             *
                             * A possible duplicate has already been registered from
                             * the complete repository index. No file is changed here.
                             */
                            return;
                        }

                        result.IdentityMismatchFiles++;
                        AddError(result, $"Repository file '{expectedAbsolutePath}' contains SOP instance UID '{actualSopInstanceUid}', but instance '{instance.Id}' expects '{instance.SopInstanceUid}'.");
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (DicomFileException)
                    {
                        result.IdentityMismatchFiles++;
                        AddError(result, $"Repository file '{expectedAbsolutePath}' for instance '{instance.SopInstanceUid}' is not a valid DICOM file.");
                        return;
                    }
                    catch (Exception exception)
                    {
                        AddError(result, $"Repository file '{expectedAbsolutePath}' for instance '{instance.SopInstanceUid}' could not be verified: {exception.Message}", exception);
                        return;
                    }
                }
            }

            if (!repositoryFiles.TryGetValue(instance.SopInstanceUid, out DicomRepositoryFileIndexEntry? indexEntry) || indexEntry.FilePaths.Count == 0)
            {
                result.MissingFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.MissingFile,
                    InstanceId = instance.Id,
                    ExpectedFilePath = expectedAbsolutePath ?? string.Empty,
                    ExpectedSopInstanceUid = instance.SopInstanceUid,
                    CanResolveAutomatically = false,
                    AutomaticallyResolved = false,
                    TechnicalDetails = $"No repository file was found for persisted instance '{instance.Id}' with SOP instance UID '{instance.SopInstanceUid}'."
                });

                return;
            }

            /*
             * Multiple candidates with the same SOP instance UID are ambiguous.
             * No candidate may be selected based on enumeration order.
             *
             * RegisterDuplicateFiles has already created a DuplicateFile issue.
             */
            if (indexEntry.FilePaths.Count > 1)
            {
                return;
            }

            /*
             * The count checks above guarantee exactly one physical candidate.
             */
            string actualFilePath = indexEntry.FilePaths[0];

            /*
             * The file was found, but not at its expected location.
             * MisplacedFiles counts every detected misplaced file, regardless
             * of whether it is repaired during this operation.
             */
            result.MisplacedFiles++;

            if (!request.RepairMissingFiles)
            {
                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.MisplacedFile,
                    InstanceId = instance.Id,
                    ExpectedFilePath = expectedAbsolutePath ?? string.Empty,
                    ActualFilePath = actualFilePath,
                    ExpectedSopInstanceUid = instance.SopInstanceUid,
                    ActualSopInstanceUid = instance.SopInstanceUid,
                    CanResolveAutomatically = !string.IsNullOrWhiteSpace(expectedAbsolutePath),
                    AutomaticallyResolved = false,
                    TechnicalDetails = $"Repository file for persisted instance '{instance.Id}' with SOP instance UID '{instance.SopInstanceUid}' was found at '{actualFilePath}' instead of the expected location '{expectedAbsolutePath ?? string.Empty}'."
                });

                return;
            }

            /*
             * Without a persisted relative path, no deterministic repair
             * destination exists.
             */
            if (string.IsNullOrWhiteSpace(expectedAbsolutePath))
            {
                AddError(result, $"Instance '{instance.SopInstanceUid}' has no expected repository path.");

                return;
            }

            string repairDestinationPath = expectedAbsolutePath;

            string? expectedDirectory = Path.GetDirectoryName(repairDestinationPath);

            if (string.IsNullOrWhiteSpace(expectedDirectory))
            {
                AddError(result, $"The expected repository directory for instance '{instance.SopInstanceUid}' could not be determined.");
                return;
            }

            /*
             * Race-condition protection:
             * The destination may have become occupied after the initial check.
             */
            if (File.Exists(repairDestinationPath))
            {
                AddError(result, $"The expected repository path '{repairDestinationPath}' for instance '{instance.SopInstanceUid}' is already occupied.");
                return;
            }

            DateTime detectedAtUtc = DateTime.UtcNow;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Run(
                    () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Directory.CreateDirectory(expectedDirectory);
                        cancellationToken.ThrowIfCancellationRequested();
                        File.Move(actualFilePath, repairDestinationPath);
                    },
                    cancellationToken);

                /*
                 * Keep the in-memory index consistent with the physical move.
                 * The dictionary entry itself remains unchanged; only its
                 * physical path collection is updated.
                 */
                indexEntry.FilePaths.Clear();
                indexEntry.FilePaths.Add(repairDestinationPath);

                result.RepairedFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.MisplacedFile,
                    InstanceId = instance.Id,
                    ExpectedFilePath = repairDestinationPath,
                    ActualFilePath = actualFilePath,
                    ExpectedSopInstanceUid = instance.SopInstanceUid,
                    ActualSopInstanceUid = instance.SopInstanceUid,
                    CanResolveAutomatically = true,
                    AutomaticallyResolved = true,
                    DetectedAtUtc = detectedAtUtc,
                    ResolvedAtUtc = DateTime.UtcNow,
                    TechnicalDetails = $"Repository file for persisted instance '{instance.Id}' with SOP instance UID '{instance.SopInstanceUid}' was moved from '{actualFilePath}' to its expected location '{repairDestinationPath}'."
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                AddError(result, $"File for instance '{instance.SopInstanceUid}' could not be repaired from '{actualFilePath}' to '{repairDestinationPath}': {exception.Message}", exception);
            }
        }
    }
}