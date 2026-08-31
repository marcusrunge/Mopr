namespace MarcusRunge.Mopr.Workbench.Application.Administration
{
    /// <summary>
    /// Evaluates the effective administrator role of the current Windows process.
    /// </summary>
    internal interface IWindowsAdministratorRoleEvaluator
    {
        /// <summary>
        /// Gets a value indicating whether the current process token has effective
        /// local administrator rights.
        /// </summary>
        bool IsElevatedAdministrator { get; }
    }
}