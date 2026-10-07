using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models
{
    /// <summary>
    /// Represents the authenticated operating-system identity used to resolve a persistent MOPR user.
    /// </summary>
    public sealed record OperatingSystemIdentity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperatingSystemIdentity"/> class.
        /// </summary>
        /// <param name="loginName">The operating-system login name.</param>
        /// <param name="securityIdentifier">The optional Windows security identifier.</param>
        public OperatingSystemIdentity(string loginName, string? securityIdentifier = null)
        {
            if (string.IsNullOrWhiteSpace(loginName))
            {
                throw new ArgumentException("The operating-system login name must not be empty.", nameof(loginName));
            }

            LoginName = loginName.Trim();
            SecurityIdentifier = NormalizeOptionalValue(securityIdentifier);
        }

        /// <summary>
        /// Gets the operating-system login name.
        /// </summary>
        public string LoginName { get; }

        /// <summary>
        /// Gets the optional Windows security identifier.
        /// </summary>
        /// <remarks>
        /// The security identifier is the durable Windows account identity.
        /// The login name remains available for display and migration of existing
        /// MOPR users that do not yet have a persisted security identifier.
        /// </remarks>
        public string? SecurityIdentifier { get; }

        private static string? NormalizeOptionalValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}