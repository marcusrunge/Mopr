using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Represents machine-wide MOPR security behavior.
    /// </summary>
    public sealed class SecurityConfiguration : ISecurityConfiguration
    {
        /// <inheritdoc/>
        public bool AllowSelfDeletion { get; set; }

        /// <inheritdoc/>
        public bool AllowSelfModification { get; set; } = true;

        /// <inheritdoc/>
        public bool HideOtherUsersFromRegularUsers { get; set; } = true;
    }
}