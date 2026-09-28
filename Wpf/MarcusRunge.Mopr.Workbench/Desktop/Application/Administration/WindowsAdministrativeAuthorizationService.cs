using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using System;

namespace MarcusRunge.Mopr.Workbench.Application.Administration
{
    /// <summary>
    /// Protects privileged MOPR administration operations with effective
    /// Windows administrator authorization.
    /// </summary>
    internal sealed class WindowsAdministrativeAuthorizationService : IAdministrativeAuthorizationService
    {
        private readonly IWindowsAdministratorRoleEvaluator _administratorRoleEvaluator;

        public WindowsAdministrativeAuthorizationService() : this(new WindowsAdministratorRoleEvaluator())
        {
        }

        internal WindowsAdministrativeAuthorizationService(IWindowsAdministratorRoleEvaluator administratorRoleEvaluator) => _administratorRoleEvaluator = administratorRoleEvaluator ?? throw new ArgumentNullException(nameof(administratorRoleEvaluator));

        /// <inheritdoc/>
        public bool IsElevatedAdministrator => _administratorRoleEvaluator.IsElevatedAdministrator;

        /// <inheritdoc/>
        public void DemandElevatedAdministrator()
        {
            if (!IsElevatedAdministrator)
            {
                // Authorization is based on the effective Windows access token.
                // Local and directory-based administrators must therefore run
                // the privileged operation with an elevated process token.
                throw new UnauthorizedAccessException("Privileged MOPR administration requires an elevated local administrator.");
            }
        }
    }
}