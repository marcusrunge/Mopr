using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    /// <summary>
    /// Validates and normalizes user-editable provisioning values.
    /// </summary>
    internal static class UserProvisioningValidator
    {
        private const int MaximumAcademicTitleLength = 128;
        private const int MaximumFirstNameLength = 256;
        private const int MaximumLastNameLength = 256;
        private const int MaximumPersonnelNumberLength = 64;
        private const int MaximumShortNameLength = 64;

        /// <summary>
        /// Validates and normalizes the supplied provisioning request.
        /// </summary>
        /// <param name="request">The provisioning request.</param>
        /// <returns>The normalized values and all detected validation issues.</returns>
        internal static UserProvisioningValidationResult Validate(UserProvisioningRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var academicTitle = NormalizeOptionalValue(request.AcademicTitle);
            var firstName = IdentityValueNormalizer.NormalizeName(request.FirstName);
            var lastName = IdentityValueNormalizer.NormalizeName(request.LastName);
            var personnelNumber = NormalizeOptionalValue(request.PersonnelNumber);
            var shortName = IdentityValueNormalizer.NormalizeShortName(request.ShortName);
            var issues = new List<UserProvisioningValidationIssue>();

            ValidateRequiredValue(firstName, MaximumFirstNameLength, UserProvisioningValidationIssue.FirstNameRequired, UserProvisioningValidationIssue.FirstNameTooLong, issues);

            ValidateRequiredValue(lastName, MaximumLastNameLength, UserProvisioningValidationIssue.LastNameRequired, UserProvisioningValidationIssue.LastNameTooLong, issues);

            ValidateRequiredValue(shortName, MaximumShortNameLength, UserProvisioningValidationIssue.ShortNameRequired, UserProvisioningValidationIssue.ShortNameTooLong, issues);

            ValidateOptionalValue(academicTitle, MaximumAcademicTitleLength, UserProvisioningValidationIssue.AcademicTitleTooLong, issues);

            ValidateOptionalValue(personnelNumber, MaximumPersonnelNumberLength, UserProvisioningValidationIssue.PersonnelNumberTooLong, issues);

            if (!string.IsNullOrEmpty(shortName) && shortName.Any(char.IsControl))
            {
                issues.Add(UserProvisioningValidationIssue.ShortNameInvalid);
            }

            return new UserProvisioningValidationResult(academicTitle, firstName, lastName, personnelNumber, shortName, issues);
        }

        private static string? NormalizeOptionalValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            // Optional organizational values are identifiers or display prefixes.
            // Their internal characters remain unchanged while surrounding
            // whitespace is never persisted.
            return value.Trim();
        }

        private static void ValidateOptionalValue(string? value, int maximumLength, UserProvisioningValidationIssue tooLongIssue, ICollection<UserProvisioningValidationIssue> issues)
        {
            if (value is not null && value.Length > maximumLength)
            {
                issues.Add(tooLongIssue);
            }
        }

        private static void ValidateRequiredValue(string value, int maximumLength, UserProvisioningValidationIssue requiredIssue, UserProvisioningValidationIssue tooLongIssue, ICollection<UserProvisioningValidationIssue> issues)
        {
            if (string.IsNullOrEmpty(value))
            {
                issues.Add(requiredIssue);
                return;
            }

            if (value.Length > maximumLength)
            {
                issues.Add(tooLongIssue);
            }
        }
    }

    /// <summary>
    /// Contains normalized provisioning values and their validation issues.
    /// </summary>
    internal sealed class UserProvisioningValidationResult
    {
        private readonly IReadOnlyList<UserProvisioningValidationIssue> _issues;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProvisioningValidationResult"/> class.
        /// </summary>
        internal UserProvisioningValidationResult(string? academicTitle, string firstName, string lastName, string? personnelNumber, string shortName, IEnumerable<UserProvisioningValidationIssue> issues)
        {
            AcademicTitle = academicTitle;
            FirstName = firstName;
            LastName = lastName;
            PersonnelNumber = personnelNumber;
            ShortName = shortName;
            _issues = (issues ?? throw new ArgumentNullException(nameof(issues))).Distinct().ToArray();
        }

        /// <summary>
        /// Gets the normalized optional academic title.
        /// </summary>
        internal string? AcademicTitle { get; }

        /// <summary>
        /// Gets the normalized first name.
        /// </summary>
        internal string FirstName { get; }

        /// <summary>
        /// Gets a value indicating whether no validation issues were detected.
        /// </summary>
        internal bool IsValid => _issues.Count == 0;

        /// <summary>
        /// Gets all detected validation issues.
        /// </summary>
        internal IReadOnlyList<UserProvisioningValidationIssue> Issues => _issues;

        /// <summary>
        /// Gets the normalized last name.
        /// </summary>
        internal string LastName { get; }

        /// <summary>
        /// Gets the normalized optional personnel number.
        /// </summary>
        internal string? PersonnelNumber { get; }

        /// <summary>
        /// Gets the normalized short name.
        /// </summary>
        internal string ShortName { get; }
    }
}