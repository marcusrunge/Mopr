namespace MarcusRunge.Mopr.Workbench.Services.Miras.Models
{
    /// <summary>
    /// Contains the values confirmed for a repository-infrastructure recovery operation.
    /// </summary>
    public sealed record RepositoryInfrastructureRecoveryRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryInfrastructureRecoveryRequest"/> class.
        /// </summary>
        /// <param name="repositoryPath">The selected repository root path.</param>
        public RepositoryInfrastructureRecoveryRequest(string? repositoryPath) => RepositoryPath = repositoryPath;

        /// <summary>
        /// Gets the selected repository root path.
        /// </summary>
        public string? RepositoryPath { get; }
    }
}