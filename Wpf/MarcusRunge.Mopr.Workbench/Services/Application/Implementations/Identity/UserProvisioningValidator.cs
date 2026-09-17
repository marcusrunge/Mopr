using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    /// <summary>
    /// Validates and normalizes user-editable provisioning values.
    /// </summary>
    internal static class UserProvisioningValidator
    {
        private const int MaximumFirstNameLength = 256;
        private const int MaximumLastNameLength = 256;
        private const int MaximumShortNameLength = 64;

        internal static UserProvisioningValidationResult Validate(UserProvisioningRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var firstName = Normalize(request.FirstName);
            var lastName = Normalize(request.LastName);
            var shortName = Normalize(request.ShortName);
            var issues = new List<UserProvisioningValidationIssue>();

            ValidateRequiredValue(firstName, MaximumFirstNameLength, UserProvisioningValidationIssue.FirstNameRequired, UserProvisioningValidationIssue.FirstNameTooLong, issues);

            ValidateRequiredValue(lastName, MaximumLastNameLength, UserProvisioningValidationIssue.LastNameRequired, UserProvisioningValidationIssue.LastNameTooLong, issues);

            ValidateRequiredValue(shortName, MaximumShortNameLength, UserProvisioningValidationIssue.ShortNameRequired, UserProvisioningValidationIssue.ShortNameTooLong, issues);

            if (!string.IsNullOrEmpty(shortName) && shortName.Any(char.IsControl))
            {
                issues.Add(UserProvisioningValidationIssue.ShortNameInvalid);
            }

            return new UserProvisioningValidationResult(firstName, lastName, shortName, issues);
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // Consecutive whitespace is collapsed so values entered through
            // different input methods receive one deterministic representation.
            return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
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

        internal UserProvisioningValidationResult(
            string firstName,
            string lastName,
            string shortName,
            IEnumerable<UserProvisioningValidationIssue> issues)
        {
            FirstName = firstName;
            LastName = lastName;
            ShortName = shortName;
            _issues = [.. (issues ?? throw new ArgumentNullException(nameof(issues))).Distinct()];
        }

        internal string FirstName { get; }

        internal bool IsValid => _issues.Count == 0;

        internal IReadOnlyList<UserProvisioningValidationIssue> Issues => _issues;

        internal string LastName { get; }

        internal string ShortName { get; }
    }
}