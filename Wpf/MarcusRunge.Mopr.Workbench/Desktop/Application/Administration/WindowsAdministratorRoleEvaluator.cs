using System.Security.Principal;

namespace MarcusRunge.Mopr.Workbench.Application.Administration
{
    /// <summary>
    /// Evaluates effective administrator rights from the current Windows process token.
    /// </summary>
    internal sealed class WindowsAdministratorRoleEvaluator : IWindowsAdministratorRoleEvaluator
    {
        /// <inheritdoc/>
        public bool IsElevatedAdministrator
        {
            get
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);

                // The built-in role check uses the language-independent Windows
                // administrator SID and evaluates the effective process token.
                // A filtered UAC token is therefore not accepted as elevated.
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}