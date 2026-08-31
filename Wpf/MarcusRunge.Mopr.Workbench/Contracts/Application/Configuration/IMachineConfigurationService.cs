using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration
{
    /// <summary>
    /// Provides controlled access to the machine-wide MOPR configuration.
    /// </summary>
    public interface IMachineConfigurationService
    {
        /// <summary>
        /// Gets a value indicating whether the current process may modify the
        /// machine-wide configuration.
        /// </summary>
        bool CanModify { get; }

        /// <summary>
        /// Loads the machine-wide MOPR configuration.
        /// </summary>
        /// <param name="cancellationToken">Cancels the load operation.</param>
        /// <returns>
        /// The stored configuration, or a safe incomplete configuration when the
        /// workstation has not been configured.
        /// </returns>
        Task<IApplicationConfiguration> LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves a structurally valid machine-wide MOPR configuration.
        /// </summary>
        /// <param name="configuration">The configuration to validate and save.</param>
        /// <param name="cancellationToken">Cancels the save operation.</param>
        /// <exception cref="MachineConfigurationValidationException">
        /// The supplied configuration is not valid for machine-wide use.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// The current process does not have effective administrator rights.
        /// </exception>
        Task SaveAsync(IApplicationConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests whether the supplied database configuration can establish a
        /// connection without replacing the active Persistence configuration.
        /// </summary>
        /// <param name="configuration">The database configuration to test.</param>
        /// <param name="cancellationToken">Cancels the connection test.</param>
        /// <returns>
        /// <see langword="true"/> when the database connection was established;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> TestDatabaseConnectionAsync(IDatabaseConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a configuration for completed machine-wide setup.
        /// </summary>
        /// <param name="configuration">The configuration to validate.</param>
        /// <returns>The structural validation result.</returns>
        MachineConfigurationValidationResult ValidateForSetupCompletion(IApplicationConfiguration configuration);
    }
}