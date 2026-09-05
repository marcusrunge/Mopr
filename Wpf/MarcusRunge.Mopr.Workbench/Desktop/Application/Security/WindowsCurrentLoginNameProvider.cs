using System.Security.Principal;

namespace MarcusRunge.Mopr.Workbench.Application.Security
{
    /// <summary>
    /// Resolves the login name from the current Windows identity.
    /// </summary>
    internal sealed class WindowsCurrentLoginNameProvider : ICurrentLoginNameProvider
    {
        /// <inheritdoc/>
        public string? GetCurrentLoginName()
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.Name;
        }
    }
}