using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts
{
    /// <summary>
    /// Defines the public contract of the assembly.
    /// </summary>
    public interface ICore
    {
        /// <summary>
        /// Occurs when an exception is thrown.
        /// </summary>
        event Action<Exception> ExceptionThrown;

        /// <summary>
        /// Gets the IImaging instance exposed by the assembly, if available.
        /// </summary>
        IImagingService? ImagingService { get; }

        /// <summary>
        /// Gets the IMirasApplicationService instance exposed by the assembly, if available.
        /// </summary>
        IMirasApplicationService? MirasApplicationService { get; }
    }
}