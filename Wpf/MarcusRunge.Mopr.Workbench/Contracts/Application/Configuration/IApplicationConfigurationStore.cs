using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration
{
    /// <summary>
    /// Loads and saves the machine-wide MOPR application configuration.
    /// </summary>
    public interface IApplicationConfigurationStore
    {
        /// <summary>
        /// Gets the machine-wide configuration file path.
        /// </summary>
        string ConfigurationFilePath { get; }

        /// <summary>
        /// Loads the machine-wide application configuration.
        /// </summary>
        /// <param name="cancellationToken">Cancels the read operation.</param>
        /// <returns>
        /// The stored configuration, or a safe incomplete configuration when no
        /// machine-wide configuration exists.
        /// </returns>
        Task<IApplicationConfiguration> LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the supplied machine-wide application configuration.
        /// </summary>
        /// <param name="configuration">The configuration to save.</param>
        /// <param name="cancellationToken">Cancels the write operation.</param>
        Task SaveAsync(IApplicationConfiguration configuration, CancellationToken cancellationToken = default);
    }
}