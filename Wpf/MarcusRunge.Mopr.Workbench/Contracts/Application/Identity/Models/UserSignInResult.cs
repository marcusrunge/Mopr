using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models
{
    /// <summary>
    /// Represents the result of resolving the current operating-system identity to a persistent MOPR user.
    /// </summary>
    public sealed record UserSignInResult
    {
        private UserSignInResult(UserSignInStatus status, OperatingSystemIdentity? operatingSystemIdentity, CurrentUser? user)
        {
            Status = status;
            OperatingSystemIdentity = operatingSystemIdentity;
            User = user;

            Validate();
        }

        /// <summary>
        /// Gets a value indicating whether an active persistent MOPR user was signed in.
        /// </summary>
        public bool IsSignedIn => Status == UserSignInStatus.SignedIn;

        /// <summary>
        /// Gets the resolved operating-system identity when one is available.
        /// </summary>
        public OperatingSystemIdentity? OperatingSystemIdentity { get; }

        /// <summary>
        /// Gets the structured sign-in status.
        /// </summary>
        public UserSignInStatus Status { get; }

        /// <summary>
        /// Gets the persistent MOPR user when one was resolved.
        /// </summary>
        public CurrentUser? User { get; }

        /// <summary>
        /// Creates a result for a successful user sign-in.
        /// </summary>
        /// <param name="operatingSystemIdentity">The resolved operating-system identity.</param>
        /// <param name="user">The active persistent MOPR user.</param>
        /// <returns>The successful sign-in result.</returns>
        public static UserSignInResult SignedIn(OperatingSystemIdentity operatingSystemIdentity, CurrentUser user)
        {
            if (operatingSystemIdentity is null)
            {
                throw new ArgumentNullException(nameof(operatingSystemIdentity));
            }

            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!user.IsActive)
            {
                throw new ArgumentException("A successful sign-in requires an active persistent MOPR user.", nameof(user));
            }

            return new UserSignInResult(UserSignInStatus.SignedIn, operatingSystemIdentity, user);
        }

        /// <summary>
        /// Creates a result indicating that no usable operating-system identity is available.
        /// </summary>
        /// <returns>The unavailable operating-system identity result.</returns>
        public static UserSignInResult OperatingSystemIdentityUnavailable() => new(UserSignInStatus.OperatingSystemIdentityUnavailable, null, null);

        /// <summary>
        /// Creates a result indicating that no persistent MOPR user is assigned.
        /// </summary>
        /// <param name="operatingSystemIdentity">The resolved operating-system identity.</param>
        /// <returns>The unknown-user result.</returns>
        public static UserSignInResult UserUnknown(OperatingSystemIdentity operatingSystemIdentity) => CreateWithoutUser(UserSignInStatus.UserUnknown, operatingSystemIdentity);

        /// <summary>
        /// Creates a result for a disabled persistent MOPR user.
        /// </summary>
        /// <param name="operatingSystemIdentity">The resolved operating-system identity.</param>
        /// <param name="user">The disabled persistent MOPR user.</param>
        /// <returns>The disabled-user result.</returns>
        public static UserSignInResult UserDisabled(OperatingSystemIdentity operatingSystemIdentity, CurrentUser user)
        {
            if (operatingSystemIdentity is null)
            {
                throw new ArgumentNullException(nameof(operatingSystemIdentity));
            }

            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user.IsActive)
            {
                throw new ArgumentException("A disabled-user result requires an inactive persistent MOPR user.", nameof(user));
            }

            return new UserSignInResult(UserSignInStatus.UserDisabled, operatingSystemIdentity, user);
        }

        /// <summary>
        /// Creates a result for an assigned user with an invalid persistent identifier.
        /// </summary>
        /// <param name="operatingSystemIdentity">The resolved operating-system identity.</param>
        /// <returns>The invalid persistent identifier result.</returns>
        public static UserSignInResult InvalidPersistentUserId(OperatingSystemIdentity operatingSystemIdentity) => CreateWithoutUser(UserSignInStatus.InvalidPersistentUserId, operatingSystemIdentity);

        /// <summary>
        /// Creates a result indicating that Persistence is unavailable.
        /// </summary>
        /// <param name="operatingSystemIdentity">The resolved operating-system identity when available.</param>
        /// <returns>The unavailable Persistence result.</returns>
        public static UserSignInResult PersistenceUnavailable(OperatingSystemIdentity? operatingSystemIdentity = null) => new(UserSignInStatus.PersistenceUnavailable, operatingSystemIdentity, null);

        /// <summary>
        /// Creates a failed result without exposing technical diagnostic information.
        /// </summary>
        /// <param name="operatingSystemIdentity">The resolved operating-system identity when available.</param>
        /// <returns>The failed sign-in result.</returns>
        public static UserSignInResult Failed(OperatingSystemIdentity? operatingSystemIdentity = null) => new(UserSignInStatus.Failed, operatingSystemIdentity, null);

        private static UserSignInResult CreateWithoutUser(UserSignInStatus status, OperatingSystemIdentity operatingSystemIdentity)
        {
            if (operatingSystemIdentity is null)
            {
                throw new ArgumentNullException(nameof(operatingSystemIdentity));
            }

            return new UserSignInResult(status, operatingSystemIdentity, null);
        }

        private void Validate()
        {
            if (Status == UserSignInStatus.SignedIn)
            {
                if (OperatingSystemIdentity is null || User is null || !User.IsActive)
                {
                    throw new InvalidOperationException("A successful sign-in result requires an operating-system identity and an active persistent MOPR user.");
                }

                ValidateMatchingLoginNames();
                return;
            }

            if (Status == UserSignInStatus.UserDisabled)
            {
                if (OperatingSystemIdentity is null || User is null || User.IsActive)
                {
                    throw new InvalidOperationException("A disabled-user result requires an operating-system identity and an inactive persistent MOPR user.");
                }

                ValidateMatchingLoginNames();
                return;
            }

            if (User is not null)
            {
                throw new InvalidOperationException("The sign-in result must not contain a persistent MOPR user for the selected status.");
            }

            if (Status is UserSignInStatus.UserUnknown or UserSignInStatus.InvalidPersistentUserId && OperatingSystemIdentity is null)
            {
                throw new InvalidOperationException("The sign-in result requires an operating-system identity for the selected status.");
            }
        }

        private void ValidateMatchingLoginNames()
        {
            if (!string.Equals(OperatingSystemIdentity!.LoginName, User!.LoginName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The operating-system identity does not match the assigned persistent MOPR user.");
            }
        }
    }
}