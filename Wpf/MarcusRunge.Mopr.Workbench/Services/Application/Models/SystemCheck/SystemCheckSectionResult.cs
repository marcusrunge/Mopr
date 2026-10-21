using MarcusRunge.Mopr.Workbench.Services.Application.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.SystemCheck;

/// <summary>
/// Describes the user-oriented result of one system-check section.
/// </summary>
/// <param name="Status">The structured section status.</param>
/// <param name="Summary">The localized user-oriented summary.</param>
/// <param name="Details">The localized user-oriented detail messages.</param>
/// <param name="ScannedItems">The number of items examined by the section when available.</param>
public sealed record SystemCheckSectionResult(SystemCheckSectionStatus Status, string Summary, IReadOnlyList<string> Details, int? ScannedItems = null)
{
    /// <summary>
    /// Gets a value indicating whether the section completed without a blocking finding.
    /// </summary>
    public bool IsSuccessful => Status is SystemCheckSectionStatus.Passed or SystemCheckSectionStatus.Notice or SystemCheckSectionStatus.Warning;

    /// <summary>
    /// Creates a section result without detail messages.
    /// </summary>
    public static SystemCheckSectionResult Create(SystemCheckSectionStatus status, string summary, int? scannedItems = null) => new(status, summary, [], scannedItems);
}
