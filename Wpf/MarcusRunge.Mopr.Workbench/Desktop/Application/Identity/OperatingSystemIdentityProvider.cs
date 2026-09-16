using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Identity
{
    /// <summary>
    /// Resolves the authenticated identity of the current Windows user.
    /// </summary>
    internal sealed class OperatingSystemIdentityProvider : IOperatingSystemIdentityProvider
    {
        private readonly IWindowsIdentityNameAccessor _identityNameAccessor;

        public OperatingSystemIdentityProvider() : this(new WindowsIdentityNameAccessor())
        {
        }

        internal OperatingSystemIdentityProvider(IWindowsIdentityNameAccessor identityNameAccessor) => _identityNameAccessor = identityNameAccessor ?? throw new ArgumentNullException(nameof(identityNameAccessor));

        /// <inheritdoc/>
        public Task<OperatingSystemIdentity?> GetCurrentIdentityAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var loginName = _identityNameAccessor.GetCurrentLoginName()?.Trim();

            // Windows tokens, SIDs and principal objects remain inside this
            // platform boundary. Only the login name required for the durable
            // MOPR user assignment crosses the public contract.
            OperatingSystemIdentity? identity = string.IsNullOrWhiteSpace(loginName) ? null : new OperatingSystemIdentity(loginName);
            return Task.FromResult(identity);
        }
    }
}