using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts
{
    /// <summary>
    /// Internal base contract for exposing services to internal consumers.
    /// </summary>
    internal interface ICoreBase
    {
        /// <summary>
        /// Gets the application lifetime used by long-running Core operations.
        /// </summary>
        ILifetimeService ApplicationLifetime { get; }

        /// <summary>
        /// Gets the DICOM module used for DICOM-related operations within Core.
        /// </summary>
        IDicom? Dicom { get; }

        /// <summary>
        /// Gets the logger used within the Core module.
        /// </summary>
        ILogger? Logger { get; }

        /// <summary>
        /// Reports an exception raised by an internal Core service.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        void OnExceptionThrown(Exception exception);
    }
}