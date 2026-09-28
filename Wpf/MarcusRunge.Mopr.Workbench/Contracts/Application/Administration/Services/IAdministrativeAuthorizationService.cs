namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services
{
    /// <summary>
    /// Provides authorization for privileged MOPR administration operations.
    /// </summary>
    public interface IAdministrativeAuthorizationService
    {
        /// <summary>
        /// Gets a value indicating whether the current process has effective
        /// local administrator rights.
        /// </summary>
        /// <remarks>
        /// This includes local and Active Directory identities whose current
        /// Windows access token has effective local administrator rights.
        /// </remarks>
        bool IsElevatedAdministrator { get; }

        /// <summary>
        /// Ensures that the current process is authorized to perform privileged
        /// MOPR administration operations.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">
        /// The current process does not have effective local administrator rights.
        /// </exception>
        void DemandElevatedAdministrator();
    }
}