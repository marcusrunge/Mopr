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
        /// <param name="academicTitle">The optional academic title displayed before the user's name.</param>
        /// <param name="personnelNumber">The optional organization-specific personnel number.</param>
        /// <param name="securityIdentifier">The optional Windows security identifier.</param>
        public CurrentUser(int id, string loginName, string firstName, string lastName, string shortName, bool isActive, string? academicTitle = null, string? personnelNumber = null, string? securityIdentifier = null)
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
            AcademicTitle = NormalizeOptionalValue(academicTitle);
            PersonnelNumber = NormalizeOptionalValue(personnelNumber);
            SecurityIdentifier = NormalizeOptionalValue(securityIdentifier);
        }

        /// <summary>
        /// Gets the optional academic title displayed before the user's name.
        /// </summary>
        public string? AcademicTitle { get; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName => AcademicTitle is null ? $"{FirstName} {LastName}" : $"{AcademicTitle} {FirstName} {LastName}";

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
        /// Gets the optional organization-specific personnel number.
        /// </summary>
        public string? PersonnelNumber { get; }

        /// <summary>
        /// Gets the optional Windows security identifier.
        /// </summary>
        public string? SecurityIdentifier { get; }

        /// <summary>
        /// Gets the short name.
        /// </summary>
        public string ShortName { get; }

        private static string? NormalizeOptionalValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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