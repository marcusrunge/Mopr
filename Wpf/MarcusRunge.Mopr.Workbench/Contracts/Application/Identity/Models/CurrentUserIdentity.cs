using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models
{
    /// <summary>
    /// Represents the persistent MOPR user assigned to the current operating-system identity.
    /// </summary>
    public sealed record CurrentUserIdentity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUserIdentity"/> class.
        /// </summary>
        /// <param name="userId">The persistent MOPR user identifier.</param>
        /// <param name="loginName">The assigned operating-system login name.</param>
        /// <param name="firstName">The normalized first name.</param>
        /// <param name="lastName">The normalized last name.</param>
        /// <param name="shortName">The normalized short name.</param>
        /// <param name="isActive">Indicates whether the user may perform authenticated MOPR operations.</param>
        public CurrentUserIdentity(int userId, string loginName, string firstName, string lastName, string shortName, bool isActive)
        {
            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId), userId, "The persistent MOPR user identifier must be positive.");
            }

            UserId = userId;
            LoginName = RequireValue(loginName, nameof(loginName));
            FirstName = RequireValue(firstName, nameof(firstName));
            LastName = RequireValue(lastName, nameof(lastName));
            ShortName = RequireValue(shortName, nameof(shortName));
            IsActive = isActive;
        }

        /// <summary>
        /// Gets the display name of the current MOPR user.
        /// </summary>
        public string DisplayName => $"{FirstName} {LastName}";

        /// <summary>
        /// Gets the normalized first name.
        /// </summary>
        public string FirstName { get; }

        /// <summary>
        /// Gets a value indicating whether the user may perform authenticated MOPR operations.
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Gets the normalized last name.
        /// </summary>
        public string LastName { get; }

        /// <summary>
        /// Gets the assigned operating-system login name.
        /// </summary>
        public string LoginName { get; }

        /// <summary>
        /// Gets the normalized short name.
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// Gets the persistent MOPR user identifier.
        /// </summary>
        public int UserId { get; }

        private static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("The value must not be empty.", parameterName);
            }

            return value.Trim();
        }
    }
}