namespace MarcusRunge.Mopr.Workbench.Application.Security
{
    /// <summary>
    /// Provides the login name of the current operating-system identity.
    /// </summary>
    internal interface ICurrentLoginNameProvider
    {
        /// <summary>
        /// Gets the current operating-system login name.
        /// </summary>
        string? GetCurrentLoginName();
    }
}