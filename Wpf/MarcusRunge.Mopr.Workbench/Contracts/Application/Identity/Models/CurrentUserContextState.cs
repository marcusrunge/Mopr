using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models
{
    /// <summary>
    /// Represents the current application-wide MOPR user assignment state.
    /// </summary>
    public sealed record CurrentUserContextState
    {
        private CurrentUserContextState(UserAssignmentStatus status, string? loginName, CurrentUserIdentity? user)
        {
            Status = status;
            LoginName = NormalizeOptionalValue(loginName);
            User = user;

            Validate();
        }

        /// <summary>
        /// Gets the initial state before identity resolution has started.
        /// </summary>
        public static CurrentUserContextState NotResolved { get; } = new(UserAssignmentStatus.NotResolved, null, null);

        /// <summary>
        /// Gets a value indicating whether an active persistent MOPR user is available.
        /// </summary>
        public bool IsAuthenticated => Status == UserAssignmentStatus.UserActive && User is { IsActive: true };

        /// <summary>
        /// Gets the operating-system login name when one was resolved.
        /// </summary>
        public string? LoginName { get; }

        /// <summary>
        /// Gets the current user assignment status.
        /// </summary>
        public UserAssignmentStatus Status { get; }

        /// <summary>
        /// Gets the assigned persistent MOPR user when one is available.
        /// </summary>
        public CurrentUserIdentity? User { get; }

        /// <summary>
        /// Creates a state for an active persistent MOPR user.
        /// </summary>
        /// <param name="user">The active persistent MOPR user.</param>
        /// <returns>The active user state.</returns>
        public static CurrentUserContextState Active(CurrentUserIdentity user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!user.IsActive)
            {
                throw new ArgumentException("An active user state requires an active persistent MOPR user.", nameof(user));
            }

            return new CurrentUserContextState(UserAssignmentStatus.UserActive, user.LoginName, user);
        }

        /// <summary>
        /// Creates a state for an inactive persistent MOPR user.
        /// </summary>
        /// <param name="user">The inactive persistent MOPR user.</param>
        /// <returns>The inactive user state.</returns>
        public static CurrentUserContextState Inactive(CurrentUserIdentity user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user.IsActive)
            {
                throw new ArgumentException("An inactive user state requires an inactive persistent MOPR user.", nameof(user));
            }

            return new CurrentUserContextState(UserAssignmentStatus.UserInactive, user.LoginName, user);
        }

        /// <summary>
        /// Creates a state indicating that no usable operating-system identity is available.
        /// </summary>
        /// <returns>The unavailable operating-system identity state.</returns>
        public static CurrentUserContextState OperatingSystemIdentityUnavailable() => new(UserAssignmentStatus.OperatingSystemIdentityUnavailable, null, null);

        /// <summary>
        /// Creates a state indicating that Persistence is unavailable.
        /// </summary>
        /// <param name="loginName">The resolved operating-system login name, when available.</param>
        /// <returns>The unavailable Persistence state.</returns>
        public static CurrentUserContextState PersistenceUnavailable(string? loginName = null) => new(UserAssignmentStatus.PersistenceUnavailable, loginName, null);

        /// <summary>
        /// Creates a state for an operating-system identity without an assigned MOPR user.
        /// </summary>
        /// <param name="loginName">The resolved operating-system login name.</param>
        /// <returns>The unknown user state.</returns>
        public static CurrentUserContextState UnknownUser(string loginName) => new(UserAssignmentStatus.UserUnknown, RequireLoginName(loginName), null);

        /// <summary>
        /// Creates a state for an assigned user with an invalid persistent identifier.
        /// </summary>
        /// <param name="loginName">The resolved operating-system login name.</param>
        /// <returns>The invalid persistent user identifier state.</returns>
        public static CurrentUserContextState InvalidPersistentUserId(string loginName) => new(UserAssignmentStatus.InvalidPersistentUserId, RequireLoginName(loginName), null);

        private static string? NormalizeOptionalValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string RequireLoginName(string loginName)
        {
            if (string.IsNullOrWhiteSpace(loginName))
            {
                throw new ArgumentException("The operating-system login name must not be empty.", nameof(loginName));
            }

            return loginName.Trim();
        }

        private void Validate()
        {
            if (Status is UserAssignmentStatus.UserActive or UserAssignmentStatus.UserInactive)
            {
                if (User is null)
                {
                    throw new InvalidOperationException("An assigned user state requires a persistent MOPR user.");
                }

                if (!string.Equals(LoginName, User.LoginName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The user context login name must match the persistent MOPR user.");
                }

                return;
            }

            if (User is not null)
            {
                throw new InvalidOperationException("A state without an assigned user must not contain a persistent MOPR user.");
            }

            if (Status is UserAssignmentStatus.UserUnknown or UserAssignmentStatus.InvalidPersistentUserId && string.IsNullOrWhiteSpace(LoginName))
            {
                throw new InvalidOperationException("The user assignment state requires an operating-system login name.");
            }
        }
    }
}