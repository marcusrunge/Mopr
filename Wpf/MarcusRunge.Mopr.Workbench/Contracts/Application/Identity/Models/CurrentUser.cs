using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models
{
    /// <summary>
    /// Represents the persistent MOPR user assigned to the current operating-system identity.
    /// </summary>
    public sealed record CurrentUser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUser"/> class.
        /// </summary>
        /// <param name="id">The persistent user identifier.</param>
        /// <param name="loginName">The assigned operating-system login name.</param>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <param name="shortName">The short name.</param>
        /// <param name="isActive">Indicates whether the user may perform authenticated MOPR operations.</param>
        public CurrentUser(int id, string loginName, string firstName, string lastName, string shortName, bool isActive)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "The persistent user identifier must be positive.");
            }

            Id = id;
            LoginName = NormalizeRequiredValue(loginName, nameof(loginName));
            FirstName = NormalizeRequiredValue(firstName, nameof(firstName));
            LastName = NormalizeRequiredValue(lastName, nameof(lastName));
            ShortName = NormalizeRequiredValue(shortName, nameof(shortName));
            IsActive = isActive;
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName => $"{FirstName} {LastName}";

        /// <summary>
        /// Gets the first name.
        /// </summary>
        public string FirstName { get; }

        /// <summary>
        /// Gets the persistent user identifier.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets a value indicating whether the user may perform authenticated MOPR operations.
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Gets the last name.
        /// </summary>
        public string LastName { get; }

        /// <summary>
        /// Gets the assigned operating-system login name.
        /// </summary>
        public string LoginName { get; }

        /// <summary>
        /// Gets the short name.
        /// </summary>
        public string ShortName { get; }

        private static string NormalizeRequiredValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("The value must not be empty.", parameterName);
            }

            return value.Trim();
        }
    }
}