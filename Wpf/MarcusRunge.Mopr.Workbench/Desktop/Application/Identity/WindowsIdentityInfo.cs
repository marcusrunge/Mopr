namespace MarcusRunge.Mopr.Workbench.Application.Identity
{
    /// <summary>
    /// Contains the Windows account name and security identifier read from one
    /// authenticated process-identity snapshot.
    /// </summary>
    internal sealed record WindowsIdentityInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsIdentityInfo"/> class.
        /// </summary>
        /// <param name="loginName">The Windows login name.</param>
        /// <param name="securityIdentifier">The Windows security identifier.</param>
        internal WindowsIdentityInfo(string? loginName, string? securityIdentifier)
        {
            LoginName = loginName;
            SecurityIdentifier = securityIdentifier;
        }

        /// <summary>
        /// Gets the Windows login name.
        /// </summary>
        internal string? LoginName { get; }

        /// <summary>
        /// Gets the Windows security identifier.
        /// </summary>
        internal string? SecurityIdentifier { get; }
    }
}