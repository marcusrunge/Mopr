using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    /// <summary>
    /// Identifies one DICOM series and its validated files for atomic imaging loading.
    /// </summary>
    public sealed record ImagingSeriesLoadRequest(string SeriesInstanceUid, IReadOnlyList<string> Files);
}