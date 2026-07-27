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

            HashSet<string> incompleteImportFilePaths = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> unreadableRepositoryFilePaths = new(StringComparer.OrdinalIgnoreCase);
            IDictionary<string, DicomRepositoryFileIndexEntry> repositoryFiles = await CreateRepositoryFileIndexAsync(result, incompleteImportFilePaths, unreadableRepositoryFilePaths, cancellationToken);

            RegisterIncompleteImportFiles(incompleteImportFilePaths, result, cancellationToken);
            RegisterDuplicateFiles(repositoryFiles, result, cancellationToken);

            HashSet<string> persistedSopInstanceUids = new(StringComparer.Ordinal);
            HashSet<string> associatedRepositoryFilePaths = new(StringComparer.OrdinalIgnoreCase);
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

                        await VerifyInstanceAsync(instance, repositoryFiles, unreadableRepositoryFilePaths, associatedRepositoryFilePaths, request, result, cancellationToken);
                    }
                }
            }

            RegisterUnassociatedUnreadableFiles(unreadableRepositoryFilePaths, associatedRepositoryFilePaths, result, cancellationToken);
            RegisterOrphanedFiles(repositoryFiles, persistedSopInstanceUids, associatedRepositoryFilePaths, result, cancellationToken);

            return result;
        }

        protected override void OnCreate(IRepositoryBase @base) => _base = @base;

        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static bool IsFileReadException(Exception exception)
        {
            if (exception is IOException or UnauthorizedAccessException)
            {
                return true;
            }

            return exception.InnerException is not null && IsFileReadException(exception.InnerException);
        }

        private static async Task<string> ReadSopInstanceUidAsync(string filePath, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                DicomFile dicomFile = DicomFile.Open(filePath);

                return dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
            }, cancellationToken);
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
                 * DuplicateFiles counts additional physical copies.
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
                    DetectedAtUtc = DateTime.UtcNow,
                    TechnicalDetails = $"The repository contains {entry.FilePaths.Count} physical files with SOP instance UID '{entry.SopInstanceUid}'. The discovered locations are: {string.Join(", ", entry.FilePaths.Select(filePath => $"'{filePath}'"))}. No file was changed."
                });
            }
        }

        private static void RegisterIncompleteImportFiles(HashSet<string> incompleteImportFilePaths, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            foreach (string filePath in incompleteImportFilePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                result.IncompleteImportFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.IncompleteImport,
                    ActualFilePath = filePath,
                    CanResolveAutomatically = false,
                    AutomaticallyResolved = false,
                    DetectedAtUtc = DateTime.UtcNow,
                    TechnicalDetails = $"Temporary repository file '{filePath}' indicates an incomplete import operation. The file was not deleted, renamed or otherwise modified."
                });
            }
        }

        private static void RegisterMissingRelativeFilePathConflict(Instance instance, string actualFilePath, DicomRepositoryRepairResult result)
        {
            /*
             * The physical file can be identified, but Persistence does not define
             * its canonical repository destination. Deriving or changing that
             * relationship automatically could attach medical data incorrectly.
             */
            result.RelationshipConflicts++;

            string technicalDetails = $"Persisted instance '{instance.Id}' with SOP instance UID '{instance.SopInstanceUid}' has no relative repository file path. The matching physical file was found at '{actualFilePath}', but no canonical destination can be determined. No file or persistence relationship was changed.";

            result.Issues.Add(new DicomRepositoryIssue
            {
                IssueType = DicomRepositoryIssueType.RelationshipConflict,
                InstanceId = instance.Id,
                ActualFilePath = actualFilePath,
                RecoveryCandidateFilePath = actualFilePath,
                ExpectedSopInstanceUid = instance.SopInstanceUid!,
                ActualSopInstanceUid = instance.SopInstanceUid!,
                CanResolveAutomatically = false,
                AutomaticallyResolved = false,
                DetectedAtUtc = DateTime.UtcNow,
                TechnicalDetails = technicalDetails
            });

            result.Errors.Add(technicalDetails);
        }

        private static void RegisterOrphanedFiles(IDictionary<string, DicomRepositoryFileIndexEntry> repositoryFiles, HashSet<string> persistedSopInstanceUids, HashSet<string> associatedRepositoryFilePaths, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            foreach (KeyValuePair<string, DicomRepositoryFileIndexEntry> item in repositoryFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string sopInstanceUid = item.Key;
                DicomRepositoryFileIndexEntry indexEntry = item.Value;

                if (persistedSopInstanceUids.Contains(sopInstanceUid))
                {
                    continue;
                }

                /*
                 * Files associated with a persisted instance are excluded,
                 * including a valid DICOM file with the wrong identity at
                 * an expected location.
                 */
                IList<string> orphanedFilePaths = [.. indexEntry.FilePaths.Where(filePath => !associatedRepositoryFilePaths.Contains(filePath))];

                if (orphanedFilePaths.Count == 0)
                {
                    continue;
                }

                result.OrphanedFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.OrphanedFile,
                    ActualFilePath = orphanedFilePaths.Count == 1 ? orphanedFilePaths[0] : string.Empty,
                    ActualSopInstanceUid = sopInstanceUid,
                    CanResolveAutomatically = false,
                    AutomaticallyResolved = false,
                    DetectedAtUtc = DateTime.UtcNow,
                    TechnicalDetails = $"The repository contains DICOM data with SOP instance UID '{sopInstanceUid}', but no corresponding persisted instance exists for the unassociated file locations. The discovered orphaned locations are: {string.Join(", ", orphanedFilePaths.Select(filePath => $"'{filePath}'"))}. No file or persistence record was changed."
                });
            }
        }

        private static void RegisterUnassociatedUnreadableFiles(HashSet<string> unreadableRepositoryFilePaths, HashSet<string> associatedRepositoryFilePaths, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            foreach (string filePath in unreadableRepositoryFilePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (associatedRepositoryFilePaths.Contains(filePath))
                {
                    continue;
                }

                result.UnreadableFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.UnreadableFile,
                    ActualFilePath = filePath,
                    CanResolveAutomatically = false,
                    AutomaticallyResolved = false,
                    DetectedAtUtc = DateTime.UtcNow,
                    TechnicalDetails = $"Repository file '{filePath}' could not be read or assigned to a persisted instance. No file was changed."
                });
            }
        }

        private static void RegisterUnreadableFile(Instance instance, string filePath, DicomRepositoryRepairResult result)
        {
            result.UnreadableFiles++;

            result.Issues.Add(new DicomRepositoryIssue
            {
                IssueType = DicomRepositoryIssueType.UnreadableFile,
                InstanceId = instance.Id,
                ExpectedFilePath = filePath,
                ActualFilePath = filePath,
                ExpectedSopInstanceUid = instance.SopInstanceUid!,
                CanResolveAutomatically = false,
                AutomaticallyResolved = false,
                DetectedAtUtc = DateTime.UtcNow,
                TechnicalDetails = $"Repository file '{filePath}' for persisted instance '{instance.Id}' with SOP instance UID '{instance.SopInstanceUid}' could not be read. No file was changed."
            });
        }

        private void AddError(DicomRepositoryRepairResult result, string message, Exception? exception = null)
        {
            result.Errors.Add(message);

            if (exception is not null)
            {
                Base.OnExceptionThrown(exception);
            }
        }

        private async Task<Dictionary<string, DicomRepositoryFileIndexEntry>> CreateRepositoryFileIndexAsync(DicomRepositoryRepairResult result, HashSet<string> incompleteImportFilePaths, HashSet<string> unreadableRepositoryFilePaths, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<string, DicomRepositoryFileIndexEntry> filesBySopInstanceUid = new(StringComparer.Ordinal);

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
                            incompleteImportFilePaths.Add(filePath);
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
                        catch (DicomFileException exception) when (IsFileReadException(exception))
                        {
                            unreadableRepositoryFilePaths.Add(filePath);
                            AddError(result, $"Repository file '{filePath}' could not be inspected: {exception.Message}", exception);
                        }
                        catch (DicomFileException)
                        {
                            /*
                             * Regular non-DICOM files and structurally invalid
                             * DICOM files are ignored by the general index.
                             *
                             * An invalid file at a persisted expected location
                             * is classified later by VerifyInstanceAsync.
                             */
                        }
                        catch (UnauthorizedAccessException exception)
                        {
                            unreadableRepositoryFilePaths.Add(filePath);
                            AddError(result, $"Repository file '{filePath}' could not be inspected: {exception.Message}", exception);
                        }
                        catch (IOException exception)
                        {
                            unreadableRepositoryFilePaths.Add(filePath);
                            AddError(result, $"Repository file '{filePath}' could not be inspected: {exception.Message}", exception);
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
            }, cancellationToken);
        }

        private async Task VerifyInstanceAsync(Instance instance, IDictionary<string, DicomRepositoryFileIndexEntry> repositoryFiles, HashSet<string> unreadableRepositoryFilePaths, HashSet<string> associatedRepositoryFilePaths, DicomRepositoryRepairRequest request, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            result.ScannedFiles++;

            if (string.IsNullOrWhiteSpace(instance.SopInstanceUid))
            {
                result.MissingFiles++;
                AddError(result, $"Instance with ID '{instance.Id}' has no SOP instance UID.");
                return;
            }

            string persistedSopInstanceUid = instance.SopInstanceUid;
            string? expectedAbsolutePath = null;

            if (!string.IsNullOrWhiteSpace(instance.RelativeFilePath))
            {
                expectedAbsolutePath = RepositoryService.GetAbsolutePath(instance.RelativeFilePath);

                if (File.Exists(expectedAbsolutePath))
                {
                    /*
                     * The physical file occupies the expected location of a
                     * persisted instance and must not later be registered as
                     * an unrelated orphaned file.
                     */
                    associatedRepositoryFilePaths.Add(expectedAbsolutePath);

                    /*
                     * The index operation already detected and reported the
                     * technical read error. Removing the path prevents a
                     * second, unassociated UnreadableFile issue.
                     */
                    if (unreadableRepositoryFilePaths.Remove(expectedAbsolutePath))
                    {
                        RegisterUnreadableFile(instance, expectedAbsolutePath, result);
                        return;
                    }

                    try
                    {
                        string actualSopInstanceUid = await ReadSopInstanceUidAsync(expectedAbsolutePath, cancellationToken);

                        if (string.Equals(actualSopInstanceUid, persistedSopInstanceUid, StringComparison.Ordinal))
                        {
                            return;
                        }

                        result.IdentityMismatchFiles++;

                        string recoveryCandidateFilePath = string.Empty;

                        if (repositoryFiles.TryGetValue(persistedSopInstanceUid, out DicomRepositoryFileIndexEntry? recoveryEntry) && recoveryEntry.FilePaths.Count == 1)
                        {
                            recoveryCandidateFilePath = recoveryEntry.FilePaths[0];
                        }

                        string technicalDetails = string.IsNullOrWhiteSpace(recoveryCandidateFilePath)
                            ? $"Repository file '{expectedAbsolutePath}' contains SOP instance UID '{actualSopInstanceUid}', but instance '{instance.Id}' expects '{persistedSopInstanceUid}'. No unique recovery candidate was found."
                            : $"Repository file '{expectedAbsolutePath}' contains SOP instance UID '{actualSopInstanceUid}', but instance '{instance.Id}' expects '{persistedSopInstanceUid}'. A unique recovery candidate was found at '{recoveryCandidateFilePath}'. No file was changed.";

                        result.Issues.Add(new DicomRepositoryIssue
                        {
                            IssueType = DicomRepositoryIssueType.IdentityMismatch,
                            InstanceId = instance.Id,
                            ExpectedFilePath = expectedAbsolutePath,
                            ActualFilePath = expectedAbsolutePath,
                            RecoveryCandidateFilePath = recoveryCandidateFilePath,
                            ExpectedSopInstanceUid = persistedSopInstanceUid,
                            ActualSopInstanceUid = actualSopInstanceUid,
                            CanResolveAutomatically = false,
                            AutomaticallyResolved = false,
                            DetectedAtUtc = DateTime.UtcNow,
                            TechnicalDetails = technicalDetails
                        });

                        AddError(result, technicalDetails);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (DicomFileException exception) when (IsFileReadException(exception))
                    {
                        RegisterUnreadableFile(instance, expectedAbsolutePath, result);
                        AddError(result, $"Repository file '{expectedAbsolutePath}' for persisted instance '{instance.Id}' with SOP instance UID '{persistedSopInstanceUid}' could not be read: {exception.Message}", exception);
                        return;
                    }
                    catch (DicomFileException)
                    {
                        result.InvalidDicomFiles++;

                        /*
                         * A valid file with the expected SOP instance UID may still exist at
                         * another repository location. It is exposed as a recovery candidate
                         * only when exactly one physical candidate exists.
                         *
                         * Even an unambiguous candidate must not replace the invalid file
                         * automatically because the expected path is already occupied.
                         */
                        string recoveryCandidateFilePath = string.Empty;

                        if (repositoryFiles.TryGetValue(persistedSopInstanceUid, out DicomRepositoryFileIndexEntry? recoveryEntry) && recoveryEntry.FilePaths.Count == 1)
                        {
                            recoveryCandidateFilePath = recoveryEntry.FilePaths[0];
                        }

                        string technicalDetails = string.IsNullOrWhiteSpace(recoveryCandidateFilePath)
                            ? $"Repository file '{expectedAbsolutePath}' for persisted instance '{instance.Id}' with SOP instance UID '{persistedSopInstanceUid}' is not a valid DICOM file. No unique recovery candidate was found."
                            : $"Repository file '{expectedAbsolutePath}' for persisted instance '{instance.Id}' with SOP instance UID '{persistedSopInstanceUid}' is not a valid DICOM file. A unique recovery candidate was found at '{recoveryCandidateFilePath}'. No file was changed.";

                        result.Issues.Add(new DicomRepositoryIssue
                        {
                            IssueType = DicomRepositoryIssueType.InvalidDicomFile,
                            InstanceId = instance.Id,
                            ExpectedFilePath = expectedAbsolutePath,
                            ActualFilePath = expectedAbsolutePath,
                            RecoveryCandidateFilePath = recoveryCandidateFilePath,
                            ExpectedSopInstanceUid = persistedSopInstanceUid,
                            CanResolveAutomatically = false,
                            AutomaticallyResolved = false,
                            DetectedAtUtc = DateTime.UtcNow,
                            TechnicalDetails = technicalDetails
                        });

                        AddError(result, technicalDetails);
                        return;
                    }
                    catch (UnauthorizedAccessException exception)
                    {
                        RegisterUnreadableFile(instance, expectedAbsolutePath, result);
                        AddError(result, $"Repository file '{expectedAbsolutePath}' for persisted instance '{instance.Id}' with SOP instance UID '{persistedSopInstanceUid}' could not be read: {exception.Message}", exception);
                        return;
                    }
                    catch (IOException exception)
                    {
                        RegisterUnreadableFile(instance, expectedAbsolutePath, result);
                        AddError(result, $"Repository file '{expectedAbsolutePath}' for persisted instance '{instance.Id}' with SOP instance UID '{persistedSopInstanceUid}' could not be read: {exception.Message}", exception);
                        return;
                    }
                    catch (Exception exception)
                    {
                        AddError(result, $"Repository file '{expectedAbsolutePath}' for instance '{persistedSopInstanceUid}' could not be verified: {exception.Message}", exception);
                        return;
                    }
                }
            }

            if (!repositoryFiles.TryGetValue(persistedSopInstanceUid, out DicomRepositoryFileIndexEntry? indexEntry) || indexEntry.FilePaths.Count == 0)
            {
                result.MissingFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.MissingFile,
                    InstanceId = instance.Id,
                    ExpectedFilePath = expectedAbsolutePath ?? string.Empty,
                    ExpectedSopInstanceUid = persistedSopInstanceUid,
                    CanResolveAutomatically = false,
                    AutomaticallyResolved = false,
                    DetectedAtUtc = DateTime.UtcNow,
                    TechnicalDetails = $"No repository file was found for persisted instance '{instance.Id}' with SOP instance UID '{persistedSopInstanceUid}'."
                });

                return;
            }

            /*
             * Multiple candidates are ambiguous. No candidate may be selected
             * based on the file-system enumeration order.
             *
             * RegisterDuplicateFiles has already created the corresponding
             * DuplicateFile issue.
             */
            if (indexEntry.FilePaths.Count > 1)
            {
                return;
            }

            string actualFilePath = indexEntry.FilePaths[0];

            /*
             * A physical file was found unambiguously, but without RelativeFilePath
             * Persistence does not define where that file belongs canonically.
             *
             * This is a relationship conflict rather than a regular misplaced file,
             * and it must not depend on whether automatic repair was requested.
             */
            if (string.IsNullOrWhiteSpace(instance.RelativeFilePath))
            {
                RegisterMissingRelativeFilePathConflict(instance, actualFilePath, result);
                return;
            }

            /*
             * MisplacedFiles counts every detected misplaced file regardless
             * of whether the repair is performed successfully.
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
                    ExpectedSopInstanceUid = persistedSopInstanceUid,
                    ActualSopInstanceUid = persistedSopInstanceUid,
                    CanResolveAutomatically = !string.IsNullOrWhiteSpace(expectedAbsolutePath),
                    AutomaticallyResolved = false,
                    DetectedAtUtc = DateTime.UtcNow,
                    TechnicalDetails = $"Repository file for persisted instance '{instance.Id}' with SOP instance UID '{persistedSopInstanceUid}' was found at '{actualFilePath}' instead of the expected location '{expectedAbsolutePath ?? string.Empty}'."
                });

                return;
            }

            if (string.IsNullOrWhiteSpace(expectedAbsolutePath))
            {
                AddError(result, $"Instance '{persistedSopInstanceUid}' has no expected repository path.");
                return;
            }

            string repairDestinationPath = expectedAbsolutePath;
            string? expectedDirectory = Path.GetDirectoryName(repairDestinationPath);

            if (string.IsNullOrWhiteSpace(expectedDirectory))
            {
                AddError(result, $"The expected repository directory for instance '{persistedSopInstanceUid}' could not be determined.");
                return;
            }

            /*
             * Race-condition protection: the destination may have become
             * occupied after the initial check.
             */
            if (File.Exists(repairDestinationPath))
            {
                AddError(result, $"The expected repository path '{repairDestinationPath}' for instance '{persistedSopInstanceUid}' is already occupied.");
                return;
            }

            DateTime detectedAtUtc = DateTime.UtcNow;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Directory.CreateDirectory(expectedDirectory);
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Move(actualFilePath, repairDestinationPath);
                }, cancellationToken);

                indexEntry.FilePaths.Clear();
                indexEntry.FilePaths.Add(repairDestinationPath);

                result.RepairedFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.MisplacedFile,
                    InstanceId = instance.Id,
                    ExpectedFilePath = repairDestinationPath,
                    ActualFilePath = actualFilePath,
                    ExpectedSopInstanceUid = persistedSopInstanceUid,
                    ActualSopInstanceUid = persistedSopInstanceUid,
                    CanResolveAutomatically = true,
                    AutomaticallyResolved = true,
                    DetectedAtUtc = detectedAtUtc,
                    ResolvedAtUtc = DateTime.UtcNow,
                    TechnicalDetails = $"Repository file for persisted instance '{instance.Id}' with SOP instance UID '{persistedSopInstanceUid}' was moved from '{actualFilePath}' to its expected location '{repairDestinationPath}'."
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                AddError(result, $"File for instance '{persistedSopInstanceUid}' could not be repaired from '{actualFilePath}' to '{repairDestinationPath}': {exception.Message}", exception);
            }
        }
    }
}