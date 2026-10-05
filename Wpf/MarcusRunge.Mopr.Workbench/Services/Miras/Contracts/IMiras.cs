namespace MarcusRunge.Mopr.Workbench.Services.Miras.Contracts
{
    /// <summary>
    /// Defines the public contract of the MIRAS assembly.
    /// </summary>
    public interface IMiras
    {
        /// <summary>
        /// Occurs when an exception is thrown.
        /// </summary>
        event Action<Exception> ExceptionThrown;

        /// <summary>
        /// Gets the MIRAS flow, if available.
        /// </summary>
        IFlow? Flow { get; }

        /// <summary>
        /// Gets the MIRAS integrity operations, if available.
        /// </summary>
        IOperations? Operations { get; }

        /// <summary>
        /// Gets the repository-infrastructure recovery service, if available.
        /// </summary>
        IRepositoryInfrastructureRecoveryService? RepositoryInfrastructureRecovery { get; }
    }
}