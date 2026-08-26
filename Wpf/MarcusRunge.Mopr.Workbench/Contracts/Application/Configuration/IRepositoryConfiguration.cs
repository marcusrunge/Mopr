namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration
{
    /// <summary>
    /// Defines application-wide repository behavior.
    /// </summary>
    public interface IRepositoryConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether safely resolvable repository-path
        /// issues may be repaired automatically.
        /// </summary>
        bool AutomaticallyRepairPaths { get; }
    }
}