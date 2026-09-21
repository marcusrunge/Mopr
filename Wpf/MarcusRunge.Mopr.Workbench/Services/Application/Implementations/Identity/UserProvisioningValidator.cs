using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;

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

            var firstName = IdentityValueNormalizer.NormalizeName(request.FirstName);
            var lastName = IdentityValueNormalizer.NormalizeName(request.LastName);
            var shortName = IdentityValueNormalizer.NormalizeShortName(request.ShortName);
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

        internal UserProvisioningValidationResult(string firstName, string lastName, string shortName, IEnumerable<UserProvisioningValidationIssue> issues)
        {
            FirstName = firstName;
            LastName = lastName;
            ShortName = shortName;
            _issues = (issues ?? throw new ArgumentNullException(nameof(issues))).Distinct().ToArray();
        }

        internal string FirstName { get; }

        internal bool IsValid => _issues.Count == 0;

        internal IReadOnlyList<UserProvisioningValidationIssue> Issues => _issues;

        internal string LastName { get; }

        internal string ShortName { get; }
    }
}