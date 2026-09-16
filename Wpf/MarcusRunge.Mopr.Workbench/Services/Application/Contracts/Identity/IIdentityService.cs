namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity
{
    /// <summary>
    /// Defines the interface for an identity service.
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>
        /// Gets the operating-system identity provider.
        /// </summary>
        IOperatingSystemIdentityProvider? OperatingSystemIdentityProvider { get; }
    }
}