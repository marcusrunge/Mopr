namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    /// <summary>
    /// Provides the canonical normalization rules used by identity workflows.
    /// </summary>
    internal static class IdentityValueNormalizer
    {
        /// <summary>
        /// Normalizes a required operating-system login name without changing its identity components.
        /// </summary>
        /// <param name="loginName">The operating-system login name.</param>
        /// <returns>The normalized login name.</returns>
        /// <exception cref="ArgumentException">Thrown when the login name is empty.</exception>
        internal static string NormalizeLoginName(string loginName)
        {
            if (string.IsNullOrWhiteSpace(loginName))
            {
                throw new ArgumentException("The operating-system login name must not be empty.", nameof(loginName));
            }

            // Domain, computer and account components must remain unchanged.
            // Only technically irrelevant surrounding whitespace is removed.
            return loginName.Trim();
        }

        /// <summary>
        /// Normalizes a user-editable name value.
        /// </summary>
        /// <param name="value">The entered name value.</param>
        /// <returns>The normalized value, or an empty string when no usable value was entered.</returns>
        internal static string NormalizeName(string? value) => NormalizeWhitespace(value);

        /// <summary>
        /// Normalizes a user-editable short name.
        /// </summary>
        /// <param name="value">The entered short name.</param>
        /// <returns>The normalized value, or an empty string when no usable value was entered.</returns>
        internal static string NormalizeShortName(string? value) => NormalizeWhitespace(value);

        /// <summary>
        /// Normalizes a required value loaded from Persistence.
        /// </summary>
        /// <param name="value">The persisted value.</param>
        /// <param name="propertyName">The persisted property name used for diagnostics.</param>
        /// <returns>The normalized persisted value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the persisted value is empty.</exception>
        internal static string NormalizeRequiredPersistentValue(string? value, string propertyName)
        {
            var normalizedValue = NormalizeWhitespace(value);

            if (string.IsNullOrEmpty(normalizedValue))
            {
                throw new InvalidOperationException($"The persistent user property '{propertyName}' does not contain a usable value.");
            }

            return normalizedValue;
        }

        private static string NormalizeWhitespace(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}