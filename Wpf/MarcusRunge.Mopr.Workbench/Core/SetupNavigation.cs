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
    /// Defines the Prism navigation contract of the existing Setup module.
    /// </summary>
    public static class SetupNavigation
    {
        /// <summary>
        /// Gets the navigation-parameter name used to transfer the Setup operating mode.
        /// </summary>
        public const string OperatingModeParameter = nameof(OperatingModeParameter);
    }
}
