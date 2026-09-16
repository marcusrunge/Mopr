namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity
{
    /// <summary>
    /// Provides the base contract for identity services.
    /// </summary>
    internal interface IIdentityServiceBase : IServiceBase
    {
        /// <summary>
        /// Gets the current-user context owned by the identity-service graph.
        /// </summary>
        internal ICurrentUserContextManager? CurrentUserContextManager { get; }
    }
}