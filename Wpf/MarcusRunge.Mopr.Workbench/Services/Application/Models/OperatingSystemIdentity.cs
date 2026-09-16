using System;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Models
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
        public OperatingSystemIdentity(string loginName)
        {
            if (string.IsNullOrWhiteSpace(loginName))
            {
                throw new ArgumentException("The operating-system login name must not be empty.", nameof(loginName));
            }

            LoginName = loginName;
        }

        /// <summary>
        /// Gets the operating-system login name.
        /// </summary>
        public string LoginName { get; }
    }
}
