namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts
{
    /// <summary>
    /// Defines the internal Core context available to the MIRAS service.
    /// </summary>
    internal interface IMirasApplicationServiceBase
    {
        /// <summary>
        /// Gets the owning Core module context.
        /// </summary>
        ICoreBase CoreBase { get; }
    }
}