namespace MarcusRunge.Mopr.Workbench.Application.Identity
{
    /// <summary>
    /// Provides the current Windows process identity.
    /// </summary>
    internal interface IWindowsIdentityAccessor
    {
        /// <summary>
        /// Gets the login name and security identifier from one Windows identity snapshot.
        /// </summary>
        /// <returns>
        /// The current Windows identity information, or <see langword="null"/> when
        /// no authenticated identity is available.
        /// </returns>
        WindowsIdentityInfo? GetCurrentIdentity();
    }
}