using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Contracts.Miras;
using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Miras.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Implementations
{
    /// <summary>
    /// Orchestrates MIRAS integrity checks across persistence and repository services.
    /// </summary>
    internal sealed class MirasService : IMirasService
    {
        private readonly IMirasBase _base;

        private IApplicationLifetime? ApplicationLifetime => Base.ApplicationLifetime;

        private IMirasBase Base => _base;

        private IPersistence Persistence => Base.Persistence ?? throw new InvalidOperationException("Persistence has not been initialized.");

        private IRepository Repository => Base.Repository ?? throw new InvalidOperationException("Repository has not been initialized.");

        internal MirasService(IMirasBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        /// <inheritdoc/>
        public async Task<MirasOperationResult> CheckRepositoryAsync(CancellationToken cancellationToken = default)
        {
            var result = new MirasOperationResult
            {
                StartedAtUtc = DateTime.UtcNow
            };

            using var linkedCancellationSource = CreateLinkedCancellationSource(cancellationToken);
            var effectiveCancellationToken = linkedCancellationSource.Token;

            try
            {
                effectiveCancellationToken.ThrowIfCancellationRequested();

                var persistenceResult = await VerifyPersistenceAsync(effectiveCancellationToken).ConfigureAwait(false);
                effectiveCancellationToken.ThrowIfCancellationRequested();

                result.ScannedItems = persistenceResult.ScannedEntities;
                AddPersistenceIssues(result, persistenceResult.Issues);
                AddTechnicalErrors(result, persistenceResult.Errors);

                // Repository verification must not use persistence data unless the complete
                // persistence integrity assessment succeeded without findings.
                if (persistenceResult.Errors.Count > 0)
                {
                    CompleteIncompleteResult(result, Resources.MirasOperation_CheckIncomplete_Description);
                    return result;
                }

                if (persistenceResult.Issues.Count > 0)
                {
                    CompleteBlockedResult(result);
                    return result;
                }

                effectiveCancellationToken.ThrowIfCancellationRequested();

                var repositoryResult = await InspectRepositoryAsync(effectiveCancellationToken).ConfigureAwait(false);
                effectiveCancellationToken.ThrowIfCancellationRequested();

                result.ScannedItems += repositoryResult.ScannedFiles;
                AddRepositoryIssues(result, repositoryResult.Issues);
                AddTechnicalErrors(result, repositoryResult.Errors);

                if (repositoryResult.Errors.Count > 0)
                {
                    CompleteIncompleteResult(result, Resources.MirasOperation_CheckIncomplete_Description);
                    return result;
                }

                CompleteSuccessfulResult(result);
                return result;
            }
            catch (OperationCanceledException)
            {
                // Cancellation is a control-flow outcome and must remain observable by the caller.
                throw;
            }
            catch (Exception exception)
            {
                Base.OnExceptionThrown(exception);
                result.TechnicalErrors.Add(exception.ToString());
                result.Messages.Add(CreateTechnicalFailureMessage(Resources.MirasOperation_CheckFailed_Description));
                result.Status = MirasOperationStatus.Failed;
                result.CompletedAtUtc = DateTime.UtcNow;
                return result;
            }
        }

        private static void AddPersistenceIssues(MirasOperationResult result, IEnumerable<PersistenceIntegrityIssue> persistenceIssues)
        {
            foreach (var persistenceIssue in persistenceIssues)
            {
                var mirasIssue = MapPersistenceIssue(persistenceIssue);
                result.Issues.Add(mirasIssue);
                result.Messages.Add(CreateIssueMessage(mirasIssue));
            }
        }

        private static void AddRepositoryIssues(MirasOperationResult result, IEnumerable<DicomRepositoryIssue> repositoryIssues)
        {
            foreach (var repositoryIssue in repositoryIssues)
            {
                var mirasIssue = MapRepositoryIssue(repositoryIssue);
                result.Issues.Add(mirasIssue);
                result.Messages.Add(CreateIssueMessage(mirasIssue));
            }
        }

        private static void AddTechnicalErrors(MirasOperationResult result, IEnumerable<string> technicalErrors)
        {
            foreach (var technicalError in technicalErrors)
            {
                if (!string.IsNullOrWhiteSpace(technicalError))
                {
                    result.TechnicalErrors.Add(technicalError);
                }
            }
        }

        private static void CompleteBlockedResult(MirasOperationResult result)
        {
            result.Messages.Add(CreateTechnicalFailureMessage(Resources.MirasOperation_PersistenceBlocked_Description));
            result.Status = MirasOperationStatus.Blocked;
            result.CompletedAtUtc = DateTime.UtcNow;
        }

        private static void CompleteIncompleteResult(MirasOperationResult result, string description)
        {
            result.Messages.Add(CreateTechnicalFailureMessage(description));
            result.Status = MirasOperationStatus.Incomplete;
            result.CompletedAtUtc = DateTime.UtcNow;
        }

        private static void CompleteSuccessfulResult(MirasOperationResult result)
        {
            result.Status = result.Issues.Count == 0 ? MirasOperationStatus.Completed : MirasOperationStatus.CompletedWithIssues;
            result.CompletedAtUtc = DateTime.UtcNow;

            if (result.Issues.Count > 0)
            {
                return;
            }

            result.Messages.Add(new MirasUserMessage
            {
                AlertLevel = MirasAlertLevel.Normal,
                CanExecuteRecommendedAction = false,
                Description = Resources.MirasOperation_CheckCompleted_Description,
                IssueId = Guid.Empty,
                IssueState = MirasIssueState.Detected,
                RecommendedActionText = Resources.MirasOperation_NoActionRequired,
                StatusText = Resources.MirasOperation_NoActionRequired,
                TechnicalDetails = string.Empty,
                Title = Resources.MirasOperation_CheckCompleted_Title
            });
        }

        private static CancellationTokenSource CreateLinkedCancellationSource(CancellationToken cancellationToken, CancellationToken applicationStopping) =>
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, applicationStopping);

        private CancellationTokenSource CreateLinkedCancellationSource(CancellationToken cancellationToken) =>
            CreateLinkedCancellationSource(cancellationToken, ApplicationLifetime?.ApplicationStopping ?? CancellationToken.None);

        private static MirasIssue MapPersistenceIssue(PersistenceIntegrityIssue issue)
        {
            var issueType = issue.IssueType switch
            {
                PersistenceIntegrityIssueType.MissingRequiredValue => MirasIssueType.PersistenceRequiredValueMissing,
                PersistenceIntegrityIssueType.InvalidValue => MirasIssueType.PersistenceValueInvalid,
                PersistenceIntegrityIssueType.DuplicateUniqueValue => MirasIssueType.PersistenceUniqueValueConflict,
                PersistenceIntegrityIssueType.MissingParent => MirasIssueType.PersistenceRelationshipConflict,
                PersistenceIntegrityIssueType.InvalidAuditReference => MirasIssueType.PersistenceAuditReferenceInvalid,
                _ => MirasIssueType.Unknown
            };

            var alertLevel = issue.IssueType == PersistenceIntegrityIssueType.InvalidAuditReference
                ? MirasAlertLevel.Caution
                : MirasAlertLevel.Warning;

            var recommendedAction = issue.IssueType is PersistenceIntegrityIssueType.DuplicateUniqueValue or PersistenceIntegrityIssueType.MissingParent
                ? MirasRecommendedAction.ReviewConflict
                : MirasRecommendedAction.ContactAdministrator;

            return new MirasIssue
            {
                AlertLevel = alertLevel,
                CanResolveAutomatically = false,
                Id = issue.Id,
                InstanceId = issue.EntityType == PersistenceIntegrityEntityType.Instance ? issue.EntityId : null,
                IssueState = MirasIssueState.ActionRequired,
                IssueType = issueType,
                OccurredAtUtc = issue.DetectedAtUtc,
                RecommendedAction = recommendedAction,
                RepositoryLocationId = issue.EntityType == PersistenceIntegrityEntityType.RepositoryLocation ? issue.EntityId : null,
                TechnicalDetails = CreatePersistenceTechnicalDetails(issue)
            };
        }

        private static MirasIssue MapRepositoryIssue(DicomRepositoryIssue issue)
        {
            var issueType = issue.IssueType switch
            {
                DicomRepositoryIssueType.MissingFile => MirasIssueType.MissingFile,
                DicomRepositoryIssueType.MisplacedFile => MirasIssueType.MisplacedFile,
                DicomRepositoryIssueType.DuplicateFile => MirasIssueType.DuplicateFile,
                DicomRepositoryIssueType.IdentityMismatch => MirasIssueType.IdentityMismatch,
                DicomRepositoryIssueType.OrphanedFile => MirasIssueType.OrphanedFile,
                DicomRepositoryIssueType.InvalidDicomFile => MirasIssueType.InvalidDicomFile,
                DicomRepositoryIssueType.UnreadableFile => MirasIssueType.UnreadableFile,
                DicomRepositoryIssueType.IncompleteImport => MirasIssueType.IncompleteImport,
                DicomRepositoryIssueType.RepositoryLocationUnavailable => MirasIssueType.RepositoryUnavailable,
                DicomRepositoryIssueType.RelationshipConflict => MirasIssueType.RelationshipConflict,
                _ => MirasIssueType.Unknown
            };

            return new MirasIssue
            {
                ActualFilePath = issue.ActualFilePath,
                ActualSopInstanceUid = issue.ActualSopInstanceUid,
                AlertLevel = GetRepositoryAlertLevel(issue.IssueType),
                CanResolveAutomatically = issue.CanResolveAutomatically,
                ExpectedFilePath = issue.ExpectedFilePath,
                ExpectedSopInstanceUid = issue.ExpectedSopInstanceUid,
                Id = issue.Id,
                InstanceId = issue.InstanceId,
                IssueState = GetRepositoryIssueState(issue),
                IssueType = issueType,
                OccurredAtUtc = issue.DetectedAtUtc,
                RecommendedAction = GetRepositoryRecommendedAction(issue.IssueType),
                RepositoryLocationId = issue.RepositoryLocationId,
                ResolvedAtUtc = issue.ResolvedAtUtc,
                TechnicalDetails = issue.TechnicalDetails
            };
        }

        private static string CreatePersistenceTechnicalDetails(PersistenceIntegrityIssue issue) => $"EntityType={issue.EntityType}; EntityId={issue.EntityId?.ToString() ?? "none"}; IssueType={issue.IssueType}; PropertyName={issue.PropertyName}; ReferencedEntityType={issue.ReferencedEntityType}; ReferencedEntityId={issue.ReferencedEntityId?.ToString() ?? "none"}; Value={issue.Value}; Details={issue.TechnicalDetails}";

        private static MirasUserMessage CreateIssueMessage(MirasIssue issue) => new()
        {
            AlertLevel = issue.AlertLevel,
            CanExecuteRecommendedAction = issue.IssueState == MirasIssueState.ActionAvailable,
            Description = GetIssueDescription(issue.IssueType),
            IssueId = issue.Id,
            IssueState = issue.IssueState,
            RecommendedActionText = GetRecommendedActionText(issue.RecommendedAction),
            StatusText = GetIssueStatusText(issue.IssueState),
            TechnicalDetails = string.Empty,
            Title = GetIssueTitle(issue.IssueType)
        };

        private static MirasUserMessage CreateTechnicalFailureMessage(string description) => new()
        {
            AlertLevel = MirasAlertLevel.Warning,
            CanExecuteRecommendedAction = false,
            Description = description,
            IssueId = Guid.Empty,
            IssueState = MirasIssueState.ActionRequired,
            RecommendedActionText = Resources.MirasRecommendedAction_ContactAdministrator,
            StatusText = Resources.MirasStatus_ActionRequired,
            TechnicalDetails = string.Empty,
            Title = Resources.MirasOperation_TechnicalFailure_Title
        };

        private static MirasAlertLevel GetRepositoryAlertLevel(DicomRepositoryIssueType issueType) => issueType switch
        {
            DicomRepositoryIssueType.MissingFile => MirasAlertLevel.Caution,
            DicomRepositoryIssueType.MisplacedFile => MirasAlertLevel.Caution,
            DicomRepositoryIssueType.DuplicateFile => MirasAlertLevel.Caution,
            DicomRepositoryIssueType.OrphanedFile => MirasAlertLevel.Caution,
            DicomRepositoryIssueType.IdentityMismatch => MirasAlertLevel.Warning,
            DicomRepositoryIssueType.InvalidDicomFile => MirasAlertLevel.Warning,
            DicomRepositoryIssueType.UnreadableFile => MirasAlertLevel.Warning,
            DicomRepositoryIssueType.IncompleteImport => MirasAlertLevel.Warning,
            DicomRepositoryIssueType.RepositoryLocationUnavailable => MirasAlertLevel.Warning,
            DicomRepositoryIssueType.RelationshipConflict => MirasAlertLevel.Warning,
            _ => MirasAlertLevel.Warning
        };

        private static MirasIssueState GetRepositoryIssueState(DicomRepositoryIssue issue)
        {
            if (issue.AutomaticallyResolved)
            {
                return MirasIssueState.AutomaticallyResolved;
            }

            return issue.CanResolveAutomatically ? MirasIssueState.ActionAvailable : MirasIssueState.ActionRequired;
        }

        private static MirasRecommendedAction GetRepositoryRecommendedAction(DicomRepositoryIssueType issueType) => issueType switch
        {
            DicomRepositoryIssueType.MissingFile => MirasRecommendedAction.LocateFile,
            DicomRepositoryIssueType.MisplacedFile => MirasRecommendedAction.RestoreExpectedLocation,
            DicomRepositoryIssueType.DuplicateFile => MirasRecommendedAction.ReviewDuplicate,
            DicomRepositoryIssueType.IdentityMismatch => MirasRecommendedAction.ReviewConflict,
            DicomRepositoryIssueType.OrphanedFile => MirasRecommendedAction.RebuildPersistenceEntry,
            DicomRepositoryIssueType.InvalidDicomFile => MirasRecommendedAction.ReviewInvalidFile,
            DicomRepositoryIssueType.UnreadableFile => MirasRecommendedAction.ContactAdministrator,
            DicomRepositoryIssueType.IncompleteImport => MirasRecommendedAction.ReviewConflict,
            DicomRepositoryIssueType.RepositoryLocationUnavailable => MirasRecommendedAction.ReconnectRepository,
            DicomRepositoryIssueType.RelationshipConflict => MirasRecommendedAction.ReviewConflict,
            _ => MirasRecommendedAction.ContactAdministrator
        };

        private static string GetIssueDescription(MirasIssueType issueType) => issueType switch
        {
            MirasIssueType.MissingFile => Properties.Resources.MirasIssue_MissingFile_Description,
            MirasIssueType.MisplacedFile => Properties.Resources.MirasIssue_MisplacedFile_Description,
            MirasIssueType.DuplicateFile => Properties.Resources.MirasIssue_DuplicateFile_Description,
            MirasIssueType.IdentityMismatch => Properties.Resources.MirasIssue_IdentityMismatch_Description,
            MirasIssueType.OrphanedFile => Properties.Resources.MirasIssue_OrphanedFile_Description,
            MirasIssueType.InvalidDicomFile => Properties.Resources.MirasIssue_InvalidDicomFile_Description,
            MirasIssueType.UnreadableFile => Properties.Resources.MirasIssue_UnreadableFile_Description,
            MirasIssueType.IncompleteImport => Properties.Resources.MirasIssue_IncompleteImport_Description,
            MirasIssueType.RelationshipConflict => Properties.Resources.MirasIssue_RelationshipConflict_Description,
            MirasIssueType.RepositoryUnavailable => Properties.Resources.MirasIssue_RepositoryUnavailable_Description,
            MirasIssueType.PersistenceUnavailable => Properties.Resources.MirasIssue_PersistenceUnavailable_Description,
            MirasIssueType.PersistenceRequiredValueMissing => Properties.Resources.MirasIssue_PersistenceRequiredValueMissing_Description,
            MirasIssueType.PersistenceValueInvalid => Properties.Resources.MirasIssue_PersistenceValueInvalid_Description,
            MirasIssueType.PersistenceUniqueValueConflict => Properties.Resources.MirasIssue_PersistenceUniqueValueConflict_Description,
            MirasIssueType.PersistenceRelationshipConflict => Properties.Resources.MirasIssue_PersistenceRelationshipConflict_Description,
            MirasIssueType.PersistenceAuditReferenceInvalid => Properties.Resources.MirasIssue_PersistenceAuditReferenceInvalid_Description,
            _ => Properties.Resources.MirasIssue_Unknown_Description
        };

        private static string GetIssueStatusText(MirasIssueState issueState) => issueState switch
        {
            MirasIssueState.ActionAvailable => Resources.MirasStatus_ActionAvailable,
            MirasIssueState.ActionRequired => Resources.MirasStatus_ActionRequired,
            MirasIssueState.AutomaticallyResolved => Resources.MirasStatus_AutomaticallyResolved,
            _ => Resources.MirasStatus_Detected
        };

        private static string GetIssueTitle(MirasIssueType issueType) => issueType switch
        {
            MirasIssueType.MissingFile => Resources.MirasIssueType_MissingFile,
            MirasIssueType.MisplacedFile => Resources.MirasIssueType_MisplacedFile,
            MirasIssueType.DuplicateFile => Resources.MirasIssueType_DuplicateFile,
            MirasIssueType.IdentityMismatch => Resources.MirasIssueType_IdentityMismatch,
            MirasIssueType.OrphanedFile => Resources.MirasIssueType_OrphanedFile,
            MirasIssueType.InvalidDicomFile => Resources.MirasIssueType_InvalidDicomFile,
            MirasIssueType.UnreadableFile => Resources.MirasIssueType_UnreadableFile,
            MirasIssueType.IncompleteImport => Resources.MirasIssueType_IncompleteImport,
            MirasIssueType.RelationshipConflict => Resources.MirasIssueType_RelationshipConflict,
            MirasIssueType.RepositoryUnavailable => Resources.MirasIssueType_RepositoryUnavailable,
            MirasIssueType.PersistenceUnavailable => Resources.MirasIssueType_PersistenceUnavailable,
            MirasIssueType.PersistenceRequiredValueMissing => Resources.MirasIssueType_PersistenceRequiredValueMissing,
            MirasIssueType.PersistenceValueInvalid => Resources.MirasIssueType_PersistenceValueInvalid,
            MirasIssueType.PersistenceUniqueValueConflict => Resources.MirasIssueType_PersistenceUniqueValueConflict,
            MirasIssueType.PersistenceRelationshipConflict => Resources.MirasIssueType_PersistenceRelationshipConflict,
            MirasIssueType.PersistenceAuditReferenceInvalid => Resources.MirasIssueType_PersistenceAuditReferenceInvalid,
            _ => Resources.MirasIssueType_Unknown
        };

        private static string GetRecommendedActionText(MirasRecommendedAction action) => action switch
        {
            MirasRecommendedAction.LocateFile => Resources.MirasRecommendedAction_LocateFile,
            MirasRecommendedAction.RestoreExpectedLocation => Resources.MirasRecommendedAction_RestoreExpectedLocation,
            MirasRecommendedAction.RebuildPersistenceEntry => Resources.MirasRecommendedAction_RebuildPersistenceEntry,
            MirasRecommendedAction.RetryOperation => Resources.MirasRecommendedAction_RetryOperation,
            MirasRecommendedAction.ReviewConflict => Resources.MirasRecommendedAction_ReviewConflict,
            MirasRecommendedAction.ReviewDuplicate => Resources.MirasRecommendedAction_ReviewDuplicate,
            MirasRecommendedAction.ReviewInvalidFile => Resources.MirasRecommendedAction_ReviewInvalidFile,
            MirasRecommendedAction.ReconnectRepository => Resources.MirasRecommendedAction_ReconnectRepository,
            MirasRecommendedAction.ContactAdministrator => Resources.MirasRecommendedAction_ContactAdministrator,
            _ => Resources.MirasRecommendedAction_None
        };

        private async Task<DicomRepositoryRepairResult> InspectRepositoryAsync(CancellationToken cancellationToken)
        {
            var repairService = Repository.RepositoryRepairService
                ?? throw new InvalidOperationException("The repository repair service has not been initialized.");

            // CheckRepositoryAsync is strictly an inspection operation. The request-level
            // repair switch remains disabled regardless of application-wide repair settings.
            var request = new DicomRepositoryRepairRequest
            {
                RepairMissingFiles = false,
                RepositoryLocationId = null,
                VerifyFiles = true
            };

            return await repairService.RepairAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<PersistenceIntegrityResult> VerifyPersistenceAsync(CancellationToken cancellationToken)
        {
            var integrityService = Persistence.Integrity ?? throw new InvalidOperationException("The persistence integrity service has not been initialized.");

            var request = new PersistenceIntegrityRequest
            {
                VerifyAuditReferences = true,
                VerifyRelationships = true,
                VerifyRequiredValues = true,
                VerifyUniqueValues = true
            };

            return await integrityService.VerifyAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}