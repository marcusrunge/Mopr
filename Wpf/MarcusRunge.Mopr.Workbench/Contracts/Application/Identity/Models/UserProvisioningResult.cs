using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models
{
    /// <summary>
    /// Represents the structured result of creating a persistent MOPR user for the current operating-system identity.
    /// </summary>
    public sealed record UserProvisioningResult
    {
        private readonly IReadOnlyList<UserProvisioningValidationIssue> _validationIssues;

        private UserProvisioningResult(UserProvisioningStatus status, OperatingSystemIdentity? operatingSystemIdentity, CurrentUser? user, IEnumerable<UserProvisioningValidationIssue>? validationIssues = null)
        {
            Status = status;
            OperatingSystemIdentity = operatingSystemIdentity;
            User = user;
            _validationIssues = validationIssues?.Distinct().ToArray() ?? [];
            Validate();
        }

        /// <summary>
        /// Gets a value indicating whether the persistent MOPR user was created and signed in.
        /// </summary>
        public bool IsSuccessful => Status == UserProvisioningStatus.Completed;

        /// <summary>
        /// Gets the operating-system identity used for provisioning when available.
        /// </summary>
        public OperatingSystemIdentity? OperatingSystemIdentity { get; }

        /// <summary>
        /// Gets the provisioning status.
        /// </summary>
        public UserProvisioningStatus Status { get; }

        /// <summary>
        /// Gets the created or resolved persistent MOPR user when available.
        /// </summary>
        public CurrentUser? User { get; }

        /// <summary>
        /// Gets the detected validation issues.
        /// </summary>
        public IReadOnlyList<UserProvisioningValidationIssue> ValidationIssues => _validationIssues;

        /// <summary>
        /// Creates a successful provisioning result.
        /// </summary>
        public static UserProvisioningResult Completed(OperatingSystemIdentity operatingSystemIdentity, CurrentUser user)
        {
            ValidateRequiredArguments(operatingSystemIdentity, user);

            if (!user.IsActive)
            {
                throw new ArgumentException("A completed provisioning result requires an active persistent MOPR user.", nameof(user));
            }

            return new UserProvisioningResult(UserProvisioningStatus.Completed, operatingSystemIdentity, user);
        }

        /// <summary>
        /// Creates a validation-failure result.
        /// </summary>
        public static UserProvisioningResult ValidationFailed(IEnumerable<UserProvisioningValidationIssue> validationIssues)
        {
            if (validationIssues is null)
            {
                throw new ArgumentNullException(nameof(validationIssues));
            }

            return new UserProvisioningResult(UserProvisioningStatus.ValidationFailed, null, null, validationIssues);
        }

        /// <summary>
        /// Creates a result indicating that no usable operating-system identity is available.
        /// </summary>
        public static UserProvisioningResult OperatingSystemIdentityUnavailable() => new(UserProvisioningStatus.OperatingSystemIdentityUnavailable, null, null);

        /// <summary>
        /// Creates a result indicating that an active persistent user already exists.
        /// </summary>
        public static UserProvisioningResult UserAlreadyExists(OperatingSystemIdentity operatingSystemIdentity, CurrentUser user)
        {
            ValidateRequiredArguments(operatingSystemIdentity, user);

            if (!user.IsActive)
            {
                throw new ArgumentException("An existing-user result requires an active persistent MOPR user.", nameof(user));
            }

            return new UserProvisioningResult(UserProvisioningStatus.UserAlreadyExists, operatingSystemIdentity, user);
        }

        /// <summary>
        /// Creates a result indicating that a disabled persistent user exists.
        /// </summary>
        public static UserProvisioningResult UserDisabled(OperatingSystemIdentity operatingSystemIdentity, CurrentUser user)
        {
            ValidateRequiredArguments(operatingSystemIdentity, user);

            if (user.IsActive)
            {
                throw new ArgumentException("A disabled-user result requires an inactive persistent MOPR user.", nameof(user));
            }

            return new UserProvisioningResult(UserProvisioningStatus.UserDisabled, operatingSystemIdentity, user);
        }

        /// <summary>
        /// Creates a result indicating that Persistence is unavailable.
        /// </summary>
        public static UserProvisioningResult PersistenceUnavailable(OperatingSystemIdentity? operatingSystemIdentity = null) => new(UserProvisioningStatus.PersistenceUnavailable, operatingSystemIdentity, null);

        /// <summary>
        /// Creates a failed result without exposing technical details.
        /// </summary>
        public static UserProvisioningResult Failed(OperatingSystemIdentity? operatingSystemIdentity = null) => new(UserProvisioningStatus.Failed, operatingSystemIdentity, null);

        private static void ValidateRequiredArguments(OperatingSystemIdentity operatingSystemIdentity, CurrentUser user)
        {
            if (operatingSystemIdentity is null)
            {
                throw new ArgumentNullException(nameof(operatingSystemIdentity));
            }

            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
        }

        private void Validate()
        {
            if (Status == UserProvisioningStatus.Completed)
            {
                ValidateUserState(expectedActiveState: true);
                return;
            }

            if (Status == UserProvisioningStatus.UserAlreadyExists)
            {
                ValidateUserState(expectedActiveState: true);
                return;
            }

            if (Status == UserProvisioningStatus.UserDisabled)
            {
                ValidateUserState(expectedActiveState: false);
                return;
            }

            if (User is not null)
            {
                throw new InvalidOperationException("The provisioning result must not contain a persistent MOPR user for the selected status.");
            }

            if (Status == UserProvisioningStatus.ValidationFailed && _validationIssues.Count == 0)
            {
                throw new InvalidOperationException("A validation-failure result requires at least one validation issue.");
            }

            if (Status != UserProvisioningStatus.ValidationFailed && _validationIssues.Count != 0)
            {
                throw new InvalidOperationException("Validation issues are permitted only for a validation-failure result.");
            }
        }

        private void ValidateUserState(bool expectedActiveState)
        {
            if (OperatingSystemIdentity is null || User is null || User.IsActive != expectedActiveState)
            {
                throw new InvalidOperationException("The provisioning result contains an invalid persistent MOPR user state.");
            }

            if (!string.Equals(OperatingSystemIdentity.LoginName, User.LoginName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The operating-system identity does not match the persistent MOPR user.");
            }
        }
    }
}