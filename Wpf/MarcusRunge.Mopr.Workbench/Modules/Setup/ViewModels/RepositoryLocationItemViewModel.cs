namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels
{
    /// <summary>
    /// Represents one read-only repository location in the administration view.
    /// </summary>
    public sealed class RepositoryLocationItemViewModel(string name, string rootPath, bool isDefault, bool isEnabled)
    {
        public bool IsDefault { get; } = isDefault;
        public bool IsEnabled { get; } = isEnabled;
        public string Name { get; } = name;
        public string RootPath { get; } = rootPath;
    }
}
