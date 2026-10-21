using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.SystemCheck;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;

/// <summary>
/// Represents one user-oriented system-check section in the administration view.
/// </summary>
public sealed class SystemCheckSectionViewModel
{
    public SystemCheckSectionViewModel(string title, SystemCheckSectionResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(result);
        Title = title;
        Status = result.Status;
        Summary = result.Summary;
        Details = result.Details;
        ScannedItems = result.ScannedItems;
    }

    public IReadOnlyList<string> Details { get; }
    public bool HasDetails => Details.Count > 0;
    public bool HasScannedItems => ScannedItems.HasValue;
    public int? ScannedItems { get; }
    public SystemCheckSectionStatus Status { get; }
    public string StatusText => TypeDescriptor.GetConverter(typeof(SystemCheckSectionStatus)).ConvertToString(context: null, CultureInfo.CurrentUICulture, Status) ?? Status.ToString();
    public string Summary { get; }
    public string Title { get; }
}
