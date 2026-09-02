using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration
{
    /// <summary>
    /// Contains the input required to complete the machine-wide MOPR setup.
    /// </summary>
    public sealed record SetupCompletionRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetupCompletionRequest"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration that shall be completed.</param>
        /// <param name="repositoryPath">The selected DICOM repository path.</param>
        public SetupCompletionRequest(IApplicationConfiguration configuration, string repositoryPath)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            RepositoryPath = repositoryPath ?? throw new ArgumentNullException(nameof(repositoryPath));
        }

        /// <summary>
        /// Gets the application configuration that shall be completed.
        /// </summary>
        public IApplicationConfiguration Configuration { get; }

        /// <summary>
        /// Gets the selected DICOM repository path.
        /// </summary>
        public string RepositoryPath { get; }

        /// <summary>
        /// Validates the request before technical setup completion starts.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The database connection string or repository path is missing.
        /// </exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Configuration.Database.ConnectionString))
            {
                throw new ArgumentException("The database connection string must not be empty.", nameof(Configuration));
            }

            if (string.IsNullOrWhiteSpace(RepositoryPath))
            {
                throw new ArgumentException("The repository path must not be empty.", nameof(RepositoryPath));
            }
        }
    }
}