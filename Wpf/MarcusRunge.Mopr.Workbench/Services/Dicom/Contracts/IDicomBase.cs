using Microsoft.Extensions.Logging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    internal interface IDicomBase
    {
        internal IDicom Dicom { get; }
        internal ILogger? Logger { get; }

        internal void OnExceptionThrown(Exception exception);
    }
}