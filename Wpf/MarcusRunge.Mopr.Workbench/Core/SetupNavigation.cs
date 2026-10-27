using Prism.Events;

namespace MarcusRunge.Mopr.Workbench.Core
{
    /// <summary>
    /// Defines the supported operating modes of the existing Setup module.
    /// </summary>
    public enum SetupOperatingMode
    {
        InitialSetup,
        Administration
    }

    /// <summary>
    /// Publishes a requested administration section after Setup navigation has been authorized.
    /// </summary>
    public sealed class AdministrationSectionRequestedEvent : PubSubEvent<string>
    {
    }

    /// <summary>
    /// Defines the Prism navigation contract of the existing Setup module.
    /// </summary>
    public static class SetupNavigation
    {
        /// <summary>
        /// Gets the navigation-parameter name used to transfer the Setup operating mode.
        /// </summary>
        public const string AdministrationSectionParameter = nameof(AdministrationSectionParameter);

        /// <summary>
        /// Gets the administration-section value for the system check.
        /// </summary>
        public const string AdministrationSystemCheckSection = "SystemCheck";

        /// <summary>
        /// Gets the navigation-parameter name used to transfer the Setup operating mode.
        /// </summary>
        public const string OperatingModeParameter = nameof(OperatingModeParameter);
    }
}
