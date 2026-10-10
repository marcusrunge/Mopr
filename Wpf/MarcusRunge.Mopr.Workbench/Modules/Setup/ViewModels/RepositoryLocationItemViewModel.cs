namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;

/// <summary>
/// Represents one persisted repository location in the administration view.
/// </summary>
public sealed class RepositoryLocationItemViewModel(int id, string name, string rootPath, bool isDefault, bool isEnabled, byte[] rowVersion)
{
    /// <summary>
    /// Gets the persistent identifier.
    /// </summary>
    public int Id { get; } = id;

    /// <summary>
    /// Gets a value indicating whether this location is the default import target.
    /// </summary>
    public bool IsDefault { get; } = isDefault;

    /// <summary>
    /// Gets a value indicating whether this location accepts new imports.
    /// </summary>
    public bool IsEnabled { get; } = isEnabled;

    /// <summary>
    /// Gets the user-facing name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the absolute repository root path.
    /// </summary>
    public string RootPath { get; } = rootPath;

    /// <summary>
    /// Gets a copy of the optimistic-concurrency token.
    /// </summary>
    public byte[] RowVersion { get; } = [.. rowVersion];
}
