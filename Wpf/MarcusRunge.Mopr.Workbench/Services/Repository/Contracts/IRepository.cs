namespace MarcusRunge.Mopr.Workbench.Services.Repository.Contracts
{
    /// <summary>
    /// Defines the public contract of the assembly.
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// Occurs when an exception is thrown.
        /// </summary>
        event Action<Exception> ExceptionThrown;

        /// <summary>
        /// Gets the import service.
        /// </summary>
        IDicomImportService? ImportService { get; }

        /// <summary>
        /// Gets the repository repair service.
        /// </summary>
        IDicomRepositoryRepairService? RepositoryRepairService { get; }

        /// <summary>
        /// Gets the repository service.
        /// </summary>
        IDicomRepositoryService? RepositoryService { get; }
    }
}