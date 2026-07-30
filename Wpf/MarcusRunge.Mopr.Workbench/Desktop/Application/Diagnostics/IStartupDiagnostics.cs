using System;

namespace MarcusRunge.Mopr.Workbench.Application.Diagnostics
{
    internal interface IStartupDiagnostics
    {
        void WriteInformation(string message);

        void WriteError(string message, Exception exception);
    }
}