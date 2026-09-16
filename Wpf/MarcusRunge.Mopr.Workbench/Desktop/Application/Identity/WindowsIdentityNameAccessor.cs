using System.Security.Principal;

namespace MarcusRunge.Mopr.Workbench.Application.Identity
{
    /// <summary>
    /// Reads the login name from the current Windows process identity.
    /// </summary>
    internal sealed class WindowsIdentityNameAccessor : IWindowsIdentityNameAccessor
    {
        /// <inheritdoc/>
        public string? GetCurrentLoginName()
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.Name;
        }
    }
}