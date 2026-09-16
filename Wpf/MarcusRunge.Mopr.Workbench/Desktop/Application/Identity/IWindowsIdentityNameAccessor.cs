namespace MarcusRunge.Mopr.Workbench.Application.Identity
{
    /// <summary>
    /// Provides the login name from the current Windows identity.
    /// </summary>
    internal interface IWindowsIdentityNameAccessor
    {
        /// <summary>
        /// Gets the login name associated with the current Windows identity.
        /// </summary>
        /// <returns>The current Windows login name, or <see langword="null"/> when it is unavailable.</returns>
        string? GetCurrentLoginName();
    }
}