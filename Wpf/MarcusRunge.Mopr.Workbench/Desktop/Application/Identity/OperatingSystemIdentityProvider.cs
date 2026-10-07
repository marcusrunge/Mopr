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
        private readonly IWindowsIdentityAccessor _identityAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperatingSystemIdentityProvider"/> class.
        /// </summary>
        public OperatingSystemIdentityProvider() : this(new WindowsIdentityAccessor())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperatingSystemIdentityProvider"/> class.
        /// </summary>
        /// <param name="identityAccessor">Provides the current Windows identity snapshot.</param>
        internal OperatingSystemIdentityProvider(IWindowsIdentityAccessor identityAccessor) => _identityAccessor = identityAccessor ?? throw new ArgumentNullException(nameof(identityAccessor));

        /// <inheritdoc/>
        public Task<OperatingSystemIdentity?> GetCurrentIdentityAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var identityInfo = _identityAccessor.GetCurrentIdentity();
            var loginName = identityInfo?.LoginName?.Trim();
            var securityIdentifier = identityInfo?.SecurityIdentifier?.Trim();

            /*
             * Login name and SID originate from the same Windows access-token
             * snapshot. The public identity contract receives both values so
             * downstream services can use the SID as the durable assignment.
             */
            OperatingSystemIdentity? identity = string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(securityIdentifier)
                ? null
                : new OperatingSystemIdentity(loginName, securityIdentifier);

            return Task.FromResult(identity);
        }
    }
}