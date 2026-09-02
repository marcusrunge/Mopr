using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration
{
    /// <summary>
    /// Represents the structured result of a machine-wide setup-completion operation.
    /// </summary>
    public sealed record SetupCompletionResult
    {
        /// <summary>
        /// Gets a value indicating whether the setup was completed successfully.
        /// </summary>
        public bool IsSuccessful => Status == SetupCompletionStatus.Completed;

        /// <summary>
        /// Gets a value indicating whether compensation of persistence changes was attempted.
        /// </summary>
        public bool RollbackAttempted { get; init; }

        /// <summary>
        /// Gets a value indicating whether an attempted compensation completed successfully.
        /// </summary>
        public bool RollbackSuccessful { get; init; }

        /// <summary>
        /// Gets the setup-completion status.
        /// </summary>
        public SetupCompletionStatus Status { get; init; } = SetupCompletionStatus.Failed;

        /// <summary>
        /// Gets technical diagnostic information that must not be displayed to users without filtering.
        /// </summary>
        public string TechnicalDetails { get; init; } = string.Empty;

        /// <summary>
        /// Creates a successful setup-completion result.
        /// </summary>
        /// <returns>A successful setup-completion result.</returns>
        public static SetupCompletionResult Completed() => new()
        {
            RollbackSuccessful = true,
            Status = SetupCompletionStatus.Completed
        };

        /// <summary>
        /// Creates a result for a failed database validation.
        /// </summary>
        /// <returns>A database-validation failure result.</returns>
        public static SetupCompletionResult DatabaseValidationFailed() => new()
        {
            RollbackSuccessful = true,
            Status = SetupCompletionStatus.DatabaseValidationFailed
        };

        /// <summary>
        /// Creates a result for a failed repository validation.
        /// </summary>
        /// <returns>A repository-validation failure result.</returns>
        public static SetupCompletionResult RepositoryValidationFailed() => new()
        {
            RollbackSuccessful = true,
            Status = SetupCompletionStatus.RepositoryValidationFailed
        };

        /// <summary>
        /// Creates a canceled setup-completion result.
        /// </summary>
        /// <param name="rollbackAttempted">Indicates whether compensation was attempted.</param>
        /// <param name="rollbackSuccessful">Indicates whether the attempted compensation succeeded.</param>
        /// <param name="technicalDetails">Technical diagnostic information.</param>
        /// <returns>A canceled setup-completion result.</returns>
        public static SetupCompletionResult Canceled(bool rollbackAttempted = false, bool rollbackSuccessful = true, string technicalDetails = "") => new()
        {
            RollbackAttempted = rollbackAttempted,
            RollbackSuccessful = rollbackSuccessful,
            Status = rollbackAttempted && !rollbackSuccessful ? SetupCompletionStatus.FailedAndRollbackFailed : SetupCompletionStatus.Canceled,
            TechnicalDetails = technicalDetails ?? string.Empty
        };

        /// <summary>
        /// Creates a failed setup-completion result.
        /// </summary>
        /// <param name="exception">The technical failure.</param>
        /// <param name="rollbackAttempted">Indicates whether compensation was attempted.</param>
        /// <param name="rollbackSuccessful">Indicates whether the attempted compensation succeeded.</param>
        /// <returns>A failed setup-completion result.</returns>
        public static SetupCompletionResult Failed(Exception exception, bool rollbackAttempted = false, bool rollbackSuccessful = true)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new SetupCompletionResult
            {
                RollbackAttempted = rollbackAttempted,
                RollbackSuccessful = rollbackSuccessful,
                Status = rollbackAttempted && !rollbackSuccessful ? SetupCompletionStatus.FailedAndRollbackFailed : SetupCompletionStatus.Failed,
                TechnicalDetails = exception.ToString()
            };
        }
    }
}