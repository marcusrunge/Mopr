namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Administration
{
    /// <summary>
    /// Provides authorization for changes to machine-wide MOPR configuration.
    /// </summary>
    public interface IAdministrativeAuthorizationService
    {
        /// <summary>
        /// Gets a value indicating whether the current process has effective
        /// local administrator rights.
        /// </summary>
        bool IsElevatedAdministrator { get; }

        /// <summary>
        /// Ensures that the current process is authorized to modify machine-wide
        /// MOPR configuration.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">
        /// The current process does not have effective local administrator rights.
        /// </exception>
        void DemandElevatedAdministrator();
    }
}