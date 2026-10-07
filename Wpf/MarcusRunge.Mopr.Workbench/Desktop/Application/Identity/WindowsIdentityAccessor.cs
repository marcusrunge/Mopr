using System.Security.Principal;

namespace MarcusRunge.Mopr.Workbench.Application.Identity
{
    /// <summary>
    /// Reads the account name and security identifier from the current Windows process identity.
    /// </summary>
    internal sealed class WindowsIdentityAccessor : IWindowsIdentityAccessor
    {
        /// <inheritdoc/>
        public WindowsIdentityInfo? GetCurrentIdentity()
        {
            using var identity = WindowsIdentity.GetCurrent();

            /*
             * Name and user SID must originate from the same access-token snapshot.
             * Reading both values here prevents an inconsistent identity pair if
             * execution context or impersonation changes between separate calls.
             */
            if (!identity.IsAuthenticated)
            {
                return null;
            }

            var securityIdentifier = identity.User?.Value;

            return string.IsNullOrWhiteSpace(identity.Name) || string.IsNullOrWhiteSpace(securityIdentifier) ? null : new WindowsIdentityInfo(identity.Name, securityIdentifier);
        }
    }
}