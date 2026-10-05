using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Models
{
    /// <summary>
    /// Represents the structured outcome of a repository-infrastructure recovery operation.
    /// </summary>
    public sealed class RepositoryInfrastructureRecoveryResult
    {
        private RepositoryInfrastructureRecoveryResult(RepositoryInfrastructureRecoveryStatus status, int? repositoryLocationId = null, int? systemUserId = null, string? normalizedRepositoryPath = null, Exception? technicalException = null)
        {
            Status = status;
            RepositoryLocationId = repositoryLocationId;
            SystemUserId = systemUserId;
            NormalizedRepositoryPath = normalizedRepositoryPath;
            TechnicalException = technicalException;
        }

        /// <summary>
        /// Gets a value indicating whether the recovery completed successfully.
        /// </summary>
        public bool IsSuccessful => Status == RepositoryInfrastructureRecoveryStatus.Completed;

        /// <summary>
        /// Gets the normalized repository path when it was determined safely.
        /// </summary>
        public string? NormalizedRepositoryPath { get; }

        /// <summary>
        /// Gets the identifier of the restored repository location.
        /// </summary>
        public int? RepositoryLocationId { get; }

        /// <summary>
        /// Gets the recovery status.
        /// </summary>
        public RepositoryInfrastructureRecoveryStatus Status { get; }

        /// <summary>
        /// Gets the identifier of the technical MOPR system user.
        /// </summary>
        public int? SystemUserId { get; }

        /// <summary>
        /// Gets the unexpected technical exception for diagnostics.
        /// </summary>
        /// <remarks>
        /// This exception must not be exposed directly as a user-facing message.
        /// </remarks>
        public Exception? TechnicalException { get; }

        /// <summary>
        /// Creates a successful recovery result.
        /// </summary>
        public static RepositoryInfrastructureRecoveryResult Completed(int repositoryLocationId, int systemUserId, string normalizedRepositoryPath)
        {
            if (repositoryLocationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repositoryLocationId), repositoryLocationId, "The repository-location identifier must be positive.");
            }

            if (systemUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(systemUserId), systemUserId, "The system-user identifier must be positive.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(normalizedRepositoryPath);

            return new RepositoryInfrastructureRecoveryResult(RepositoryInfrastructureRecoveryStatus.Completed, repositoryLocationId, systemUserId, normalizedRepositoryPath);
        }

        /// <summary>
        /// Creates a result indicating that elevated administrative authorization is required.
        /// </summary>
        public static RepositoryInfrastructureRecoveryResult AdministrativeAuthorizationRequired() => new(RepositoryInfrastructureRecoveryStatus.AdministrativeAuthorizationRequired);

        /// <summary>
        /// Creates a failed recovery result.
        /// </summary>
        public static RepositoryInfrastructureRecoveryResult Failed(Exception technicalException)
        {
            ArgumentNullException.ThrowIfNull(technicalException);
            return new RepositoryInfrastructureRecoveryResult(RepositoryInfrastructureRecoveryStatus.Failed, technicalException: technicalException);
        }

        /// <summary>
        /// Creates a result indicating that persistence services are unavailable.
        /// </summary>
        public static RepositoryInfrastructureRecoveryResult PersistenceUnavailable() => new(RepositoryInfrastructureRecoveryStatus.PersistenceUnavailable);

        /// <summary>
        /// Creates a result indicating that repository infrastructure already exists.
        /// </summary>
        public static RepositoryInfrastructureRecoveryResult RepositoryAlreadyConfigured() => new(RepositoryInfrastructureRecoveryStatus.RepositoryAlreadyConfigured);

        /// <summary>
        /// Creates a result indicating that the selected repository path could not be validated.
        /// </summary>
        public static RepositoryInfrastructureRecoveryResult RepositoryValidationFailed(string? normalizedRepositoryPath = null) => new(RepositoryInfrastructureRecoveryStatus.RepositoryValidationFailed, normalizedRepositoryPath: normalizedRepositoryPath);
    }
}