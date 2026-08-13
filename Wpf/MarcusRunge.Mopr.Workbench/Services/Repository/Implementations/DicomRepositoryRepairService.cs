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
        private IRepositoryOperationsCoordinator OperationsCoordinator => Base.OperationsCoordinator ?? throw new InvalidOperationException("The repository operations coordinator has not been initialized.");
        private IPersistence Persistence => Base.Persistence ?? throw new InvalidOperationException("Persistence has not been initialized.");
        private IRepository Repository => Base as IRepository ?? throw new InvalidOperationException("The repository base does not implement IRepository.");
        private IRepositoryLocationRepository RepositoryLocationRepository => Persistence.RepositoryLocation ?? throw new InvalidOperationException("The repository-location repository has not been initialized.");
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

            IReadOnlyList<RepositoryLocation> repositoryLocations = await GetRepositoryLocationsAsync(request.RepositoryLocationId, result, cancellationToken);

            /*
             * A repair covering all enabled locations deliberately coordinates one
             * location at a time. The operation does not promise global atomicity
             * across independent repository roots.
             *
             * Releasing each lease before acquiring the next preserves parallel work
             * in other locations and prevents circular multi-location waits.
             */
            foreach (RepositoryLocation repositoryLocation in repositoryLocations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using IAsyncDisposable operationLease = await OperationsCoordinator.AcquireRepairAsync(
                    repositoryLocation.Id,
                    cancellationToken);

                await RepairLocationAsync(repositoryLocation, request, result, cancellationToken);
            }

            return result;
        }

        protected override void OnCreate(IRepositoryBase @base) => _base = @base;

        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task RepairLocationAsync(RepositoryLocation repositoryLocation, DicomRepositoryRepairRequest request, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            /*
             * The caller holds exclusive access to this repository location for the
             * complete method. No import can create temporary files, change a target
             * file, commit Persistence or compensate in this location while Repair
             * builds and evaluates its location-specific view.
             */
            DicomRepositoryLocationRepairContext? context = await CreateLocationContextAsync(repositoryLocation, result, cancellationToken);

            /*
             * An unavailable location is reported once at location level.
             * Its instances must not be classified as individually missing.
             */
            if (context is null)
            {
                return;
            }

            RegisterIncompleteImportFiles(context, result, cancellationToken);
            RegisterDuplicateFiles(context, result, cancellationToken);

            HashSet<string> persistedSopInstanceUids = new(StringComparer.Ordinal);

            /*
             * Persistence is read only after the exclusive location lease has been
             * acquired. This prevents a completed import from appearing in the file
             * index while its newly committed hierarchy is absent from Repair's view.
             */
            IList<Study> studies = await StudyRepository.GetAllAsync(cancellationToken);

            foreach (Study study in studies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IList<Series> seriesItems = await SeriesRepository.GetByStudyIdAsync(study.Id, cancellationToken);

                foreach (Series series in seriesItems)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    IList<Instance> instances = await InstanceRepository.GetBySeriesIdAsync(series.Id, cancellationToken);

                    foreach (Instance instance in instances.Where(item => item.RepositoryLocationId == repositoryLocation.Id))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!string.IsNullOrWhiteSpace(instance.SopInstanceUid))
                        {
                            persistedSopInstanceUids.Add(instance.SopInstanceUid);
                        }

                        await VerifyInstanceAsync(study, series, instance, context, request, result, cancellationToken);
                    }
                }
            }

            RegisterUnassociatedUnreadableFiles(context, result, cancellationToken);
            RegisterOrphanedFiles(context, persistedSopInstanceUids, result, cancellationToken);
        }

        private async Task<IReadOnlyList<RepositoryLocation>> GetRepositoryLocationsAsync(int? repositoryLocationId, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            if (repositoryLocationId is not int locationId)
            {
                return [.. await RepositoryLocationRepository.GetEnabledAsync(cancellationToken)];
            }

            if (locationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repositoryLocationId), "The repository-location ID must be a positive integer.");
            }

            RepositoryLocation? repositoryLocation = await RepositoryLocationRepository.GetByIdAsync(locationId, cancellationToken);

            if (repositoryLocation is not null)
            {
                return [repositoryLocation];
            }

            result.Errors.Add($"Repository location with ID '{locationId}' does not exist.");
            return [];
        }

        private async Task<DicomRepositoryLocationRepairContext?> CreateLocationContextAsync(RepositoryLocation repositoryLocation, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            try
            {
                string repositoryRootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryLocation.RootPath ?? throw new InvalidOperationException("The repository location has no root path.")));

                if (!Directory.Exists(repositoryRootPath))
                {
                    RegisterUnavailableRepositoryLocation(repositoryLocation, repositoryRootPath, "The configured repository root directory does not exist.", result);
                    return null;
                }

                DicomRepositoryLocationRepairContext context = new()
                {
                    RepositoryLocation = repositoryLocation,
                    RepositoryRootPath = repositoryRootPath
                };

                await PopulateRepositoryFileIndexAsync(context, result, cancellationToken);
                return context;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                RegisterUnavailableRepositoryLocation(repositoryLocation, repositoryLocation.RootPath ?? string.Empty, exception.Message, result, exception);
                return null;
            }
        }

        private void RegisterUnavailableRepositoryLocation(RepositoryLocation repositoryLocation, string repositoryRootPath, string reason, DicomRepositoryRepairResult result, Exception? exception = null)
        {
            result.UnavailableRepositoryLocations++;
            string technicalDetails = $"Repository location '{repositoryLocation.Id}' at '{repositoryRootPath}' could not be inspected: {reason} No persisted instance was classified as missing and no file was changed.";
            result.Issues.Add(new DicomRepositoryIssue
            {
                IssueType = DicomRepositoryIssueType.RepositoryLocationUnavailable,
                RepositoryLocationId = repositoryLocation.Id,
                ActualFilePath = repositoryRootPath,
                CanResolveAutomatically = false,
                AutomaticallyResolved = false,
                DetectedAtUtc = DateTime.UtcNow,
                TechnicalDetails = technicalDetails
            });
            AddError(result, technicalDetails, exception);
        }

        private static bool IsFileReadException(Exception exception)
        {
            if (exception is IOException or UnauthorizedAccessException)
            {
                return true;
            }

            return exception.InnerException is not null && IsFileReadException(exception.InnerException);
        }

        private static async Task<DicomRepositoryFileIdentity> ReadDicomIdentityAsync(string filePath, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                /*
                 * All hierarchy identifiers are read during the same physical file
                 * operation. This allows the repository to compare the DICOM file with
                 * the persisted Study-Series-Instance hierarchy without reopening it.
                 */
                DicomFile dicomFile = DicomFile.Open(filePath);

                return new DicomRepositoryFileIdentity
                {
                    StudyInstanceUid = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                    SeriesInstanceUid = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                    SopInstanceUid = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty)
                };
            }, cancellationToken);
        }

        private static void RegisterDicomHierarchyConflict(Study study, Series series, Instance instance, string filePath, DicomRepositoryFileIdentity actualIdentity, DicomRepositoryRepairResult result)
        {
            /*
             * The SOP identity identifies the expected instance, but the Study or
             * Series identity stored inside the physical DICOM file contradicts the
             * persisted hierarchy.
             *
             * Neither the database relationships nor the medical file may be changed
             * automatically because the repository cannot determine which hierarchy
             * represents the medically correct assignment.
             */
            result.RelationshipConflicts++;

            bool studyMatches = string.Equals(actualIdentity.StudyInstanceUid, study.StudyInstanceUid, StringComparison.Ordinal);
            bool seriesMatches = string.Equals(actualIdentity.SeriesInstanceUid, series.SeriesInstanceUid, StringComparison.Ordinal);

            string technicalDetails = $"Repository file '{filePath}' for persisted instance '{instance.Id}' has inconsistent DICOM hierarchy identifiers. Expected Study Instance UID '{study.StudyInstanceUid}' and Series Instance UID '{series.SeriesInstanceUid}', but the physical file contains Study Instance UID '{actualIdentity.StudyInstanceUid}' and Series Instance UID '{actualIdentity.SeriesInstanceUid}'. Study identity matches: {studyMatches}. Series identity matches: {seriesMatches}. No file or persistence relationship was changed.";

            result.Issues.Add(new DicomRepositoryIssue
            {
                IssueType = DicomRepositoryIssueType.RelationshipConflict,
                InstanceId = instance.Id,
                RepositoryLocationId = instance.RepositoryLocationId,
                ExpectedFilePath = filePath,
                ActualFilePath = filePath,
                ExpectedStudyInstanceUid = study.StudyInstanceUid!,
                ActualStudyInstanceUid = actualIdentity.StudyInstanceUid,
                ExpectedSeriesInstanceUid = series.SeriesInstanceUid!,
                ActualSeriesInstanceUid = actualIdentity.SeriesInstanceUid,
                ExpectedSopInstanceUid = instance.SopInstanceUid!,
                ActualSopInstanceUid = actualIdentity.SopInstanceUid,
                CanResolveAutomatically = false,
                AutomaticallyResolved = false,
                DetectedAtUtc = DateTime.UtcNow,
                TechnicalDetails = technicalDetails
            });

            result.Errors.Add(technicalDetails);
        }

        private static void RegisterDuplicateFiles(DicomRepositoryLocationRepairContext context, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            foreach (DicomRepositoryFileIndexEntry entry in context.RepositoryFiles.Values)
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
                    RepositoryLocationId = context.RepositoryLocation.Id,
                    ExpectedSopInstanceUid = entry.SopInstanceUid,
                    ActualSopInstanceUid = entry.SopInstanceUid,
                    CanResolveAutomatically = false,
                    AutomaticallyResolved = false,
                    DetectedAtUtc = DateTime.UtcNow,
                    TechnicalDetails = $"The repository contains {entry.FilePaths.Count} physical files with SOP instance UID '{entry.SopInstanceUid}'. The discovered locations are: {string.Join(", ", entry.FilePaths.Select(filePath => $"'{filePath}'"))}. No file was changed."
                });
            }
        }

        private static void RegisterIncompleteImportFiles(DicomRepositoryLocationRepairContext context, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            foreach (string filePath in context.IncompleteImportFilePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                result.IncompleteImportFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.IncompleteImport,
                    RepositoryLocationId = context.RepositoryLocation.Id,
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
                RepositoryLocationId = instance.RepositoryLocationId,
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

        private static void RegisterUnsafeRelativeFilePathConflict(Instance instance, string relativeFilePath, string reason, DicomRepositoryRepairResult result)
        {
            /*
             * A persisted path that is absolute, traverses directories or cannot be
             * normalized safely must never be passed to physical file operations.
             *
             * The repository reports the damaged Persistence relationship but does
             * not attempt to derive a replacement path or inspect the referenced
             * external location.
             */
            result.RelationshipConflicts++;

            string technicalDetails = $"Persisted instance '{instance.Id}' with SOP instance UID '{instance.SopInstanceUid}' contains unsafe relative repository path '{relativeFilePath}' for repository location '{instance.RepositoryLocationId}'. The path was rejected before any physical file operation: {reason} No file or persistence relationship was changed.";

            result.Issues.Add(new DicomRepositoryIssue
            {
                IssueType = DicomRepositoryIssueType.RelationshipConflict,
                InstanceId = instance.Id,
                RepositoryLocationId = instance.RepositoryLocationId,
                ExpectedSopInstanceUid = instance.SopInstanceUid ?? string.Empty,
                CanResolveAutomatically = false,
                AutomaticallyResolved = false,
                DetectedAtUtc = DateTime.UtcNow,
                TechnicalDetails = technicalDetails
            });

            result.Errors.Add(technicalDetails);
        }

        private static void RegisterOrphanedFiles(DicomRepositoryLocationRepairContext context, HashSet<string> persistedSopInstanceUids, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            foreach (KeyValuePair<string, DicomRepositoryFileIndexEntry> item in context.RepositoryFiles)
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
                IList<string> orphanedFilePaths = [.. indexEntry.FilePaths.Where(filePath => !context.AssociatedFilePaths.Contains(filePath))];

                if (orphanedFilePaths.Count == 0)
                {
                    continue;
                }

                result.OrphanedFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.OrphanedFile,
                    RepositoryLocationId = context.RepositoryLocation.Id,
                    ActualFilePath = orphanedFilePaths.Count == 1 ? orphanedFilePaths[0] : string.Empty,
                    ActualSopInstanceUid = sopInstanceUid,
                    CanResolveAutomatically = false,
                    AutomaticallyResolved = false,
                    DetectedAtUtc = DateTime.UtcNow,
                    TechnicalDetails = $"The repository contains DICOM data with SOP instance UID '{sopInstanceUid}', but no corresponding persisted instance exists for the unassociated file locations. The discovered orphaned locations are: {string.Join(", ", orphanedFilePaths.Select(filePath => $"'{filePath}'"))}. No file or persistence record was changed."
                });
            }
        }

        private static void RegisterUnassociatedUnreadableFiles(DicomRepositoryLocationRepairContext context, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            foreach (string filePath in context.UnreadableFilePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (context.AssociatedFilePaths.Contains(filePath))
                {
                    continue;
                }

                result.UnreadableFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.UnreadableFile,
                    RepositoryLocationId = context.RepositoryLocation.Id,
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
                RepositoryLocationId = instance.RepositoryLocationId,
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

        private async Task PopulateRepositoryFileIndexAsync(DicomRepositoryLocationRepairContext context, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Run(() =>
            {
                try
                {
                    foreach (string filePath in Directory.EnumerateFiles(context.RepositoryRootPath, "*", SearchOption.AllDirectories))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (filePath.EndsWith(".importing", StringComparison.OrdinalIgnoreCase))
                        {
                            context.IncompleteImportFilePaths.Add(filePath);
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

                            if (!context.RepositoryFiles.TryGetValue(sopInstanceUid, out DicomRepositoryFileIndexEntry? entry))
                            {
                                entry = new DicomRepositoryFileIndexEntry { SopInstanceUid = sopInstanceUid };
                                context.RepositoryFiles.Add(sopInstanceUid, entry);
                            }

                            entry.FilePaths.Add(filePath);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (DicomFileException exception) when (IsFileReadException(exception))
                        {
                            context.UnreadableFilePaths.Add(filePath);
                            AddError(result, $"Repository file '{filePath}' in repository location '{context.RepositoryLocation.Id}' could not be inspected: {exception.Message}", exception);
                        }
                        catch (DicomFileException)
                        {
                            // Invalid DICOM at an expected path is classified during instance verification.
                        }
                        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
                        {
                            context.UnreadableFilePaths.Add(filePath);
                            AddError(result, $"Repository file '{filePath}' in repository location '{context.RepositoryLocation.Id}' could not be inspected: {exception.Message}", exception);
                        }
                        catch (Exception exception)
                        {
                            AddError(result, $"Repository file '{filePath}' in repository location '{context.RepositoryLocation.Id}' could not be inspected: {exception.Message}", exception);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new IOException($"Repository location '{context.RepositoryLocation.Id}' at '{context.RepositoryRootPath}' could not be scanned.", exception);
                }
            }, cancellationToken);
        }

        private async Task VerifyInstanceAsync(Study study, Series series, Instance instance, DicomRepositoryLocationRepairContext context, DicomRepositoryRepairRequest request, DicomRepositoryRepairResult result, CancellationToken cancellationToken)
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
                try
                {
                    /*
                     * Path validation must complete before File.Exists or any other
                     * physical operation is allowed to observe the persisted value.
                     */
                    expectedAbsolutePath = RepositoryService.GetAbsolutePath(context.RepositoryLocation, instance.RelativeFilePath);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception) when (exception is ArgumentException or UnauthorizedAccessException or NotSupportedException or PathTooLongException)
                {
                    RegisterUnsafeRelativeFilePathConflict(instance, instance.RelativeFilePath, exception.Message, result);
                    return;
                }

                if (File.Exists(expectedAbsolutePath))
                {
                    /*
                     * The physical file occupies the expected location of a
                     * persisted instance and must not later be registered as
                     * an unrelated orphaned file.
                     */
                    context.AssociatedFilePaths.Add(expectedAbsolutePath);

                    /*
                     * The index operation already detected and reported the
                     * technical read error. Removing the path prevents a
                     * second, unassociated UnreadableFile issue.
                     */
                    if (context.UnreadableFilePaths.Remove(expectedAbsolutePath))
                    {
                        RegisterUnreadableFile(instance, expectedAbsolutePath, result);
                        return;
                    }

                    try
                    {
                        DicomRepositoryFileIdentity actualIdentity = await ReadDicomIdentityAsync(expectedAbsolutePath, cancellationToken);

                        if (string.Equals(actualIdentity.SopInstanceUid, persistedSopInstanceUid, StringComparison.Ordinal))
                        {
                            /*
                             * A matching SOP Instance UID identifies the expected instance, but the
                             * physical file must also belong to the persisted Study and Series.
                             */
                            bool studyMatches = string.Equals(actualIdentity.StudyInstanceUid, study.StudyInstanceUid, StringComparison.Ordinal);
                            bool seriesMatches = string.Equals(actualIdentity.SeriesInstanceUid, series.SeriesInstanceUid, StringComparison.Ordinal);

                            if (!studyMatches || !seriesMatches)
                            {
                                RegisterDicomHierarchyConflict(study, series, instance, expectedAbsolutePath, actualIdentity, result);
                            }

                            return;
                        }

                        result.IdentityMismatchFiles++;

                        string recoveryCandidateFilePath = string.Empty;

                        if (context.RepositoryFiles.TryGetValue(persistedSopInstanceUid, out DicomRepositoryFileIndexEntry? recoveryEntry) && recoveryEntry.FilePaths.Count == 1)
                        {
                            recoveryCandidateFilePath = recoveryEntry.FilePaths[0];
                        }

                        string technicalDetails = string.IsNullOrWhiteSpace(recoveryCandidateFilePath)
                            ? $"Repository file '{expectedAbsolutePath}' contains SOP instance UID '{actualIdentity.SopInstanceUid}', but instance '{instance.Id}' expects '{persistedSopInstanceUid}'. No unique recovery candidate was found."
                            : $"Repository file '{expectedAbsolutePath}' contains SOP instance UID '{actualIdentity.SopInstanceUid}', but instance '{instance.Id}' expects '{persistedSopInstanceUid}'. A unique recovery candidate was found at '{recoveryCandidateFilePath}'. No file was changed.";

                        result.Issues.Add(new DicomRepositoryIssue
                        {
                            IssueType = DicomRepositoryIssueType.IdentityMismatch,
                            InstanceId = instance.Id,
                RepositoryLocationId = instance.RepositoryLocationId,
                            ExpectedFilePath = expectedAbsolutePath,
                            ActualFilePath = expectedAbsolutePath,
                            RecoveryCandidateFilePath = recoveryCandidateFilePath,
                            ExpectedSopInstanceUid = persistedSopInstanceUid,
                            ActualSopInstanceUid = actualIdentity.SopInstanceUid,
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

                        if (context.RepositoryFiles.TryGetValue(persistedSopInstanceUid, out DicomRepositoryFileIndexEntry? recoveryEntry) && recoveryEntry.FilePaths.Count == 1)
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
                RepositoryLocationId = instance.RepositoryLocationId,
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

            if (!context.RepositoryFiles.TryGetValue(persistedSopInstanceUid, out DicomRepositoryFileIndexEntry? indexEntry) || indexEntry.FilePaths.Count == 0)
            {
                result.MissingFiles++;

                result.Issues.Add(new DicomRepositoryIssue
                {
                    IssueType = DicomRepositoryIssueType.MissingFile,
                    InstanceId = instance.Id,
                RepositoryLocationId = instance.RepositoryLocationId,
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
                RepositoryLocationId = instance.RepositoryLocationId,
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
                RepositoryLocationId = instance.RepositoryLocationId,
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