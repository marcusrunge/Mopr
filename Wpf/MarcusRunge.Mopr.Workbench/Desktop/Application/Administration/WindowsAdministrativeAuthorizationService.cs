using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration;
using System;

namespace MarcusRunge.Mopr.Workbench.Application.Administration
{
    /// <summary>
    /// Protects machine-wide MOPR configuration changes with effective Windows
    /// administrator authorization.
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
                // The service enforces the security boundary without deciding how
                // the UI requests elevation or explains the requirement to the user.
                throw new UnauthorizedAccessException("Machine-wide MOPR configuration requires an elevated local administrator.");
            }
        }
    }
}