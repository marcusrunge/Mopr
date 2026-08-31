namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration
{
    /// <summary>
    /// Defines machine-wide MOPR security behavior.
    /// </summary>
    public interface ISecurityConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether users may delete their own records.
        /// </summary>
        bool AllowSelfDeletion { get; }

        /// <summary>
        /// Gets a value indicating whether users may modify their own records.
        /// </summary>
        bool AllowSelfModification { get; }

        /// <summary>
        /// Gets a value indicating whether regular users may see other users.
        /// </summary>
        bool HideOtherUsersFromRegularUsers { get; }
    }
}