using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace MarcusRunge.Mopr.Workbench.Application.Diagnostics
{
    internal sealed class StartupDiagnostics : IStartupDiagnostics
    {
        private readonly Lock _synchronization = new();
        private readonly string _logFilePath;

        public StartupDiagnostics()
        {
            var diagnosticsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MOPR", "Diagnostics");

            Directory.CreateDirectory(diagnosticsDirectory);
            _logFilePath = Path.Combine(diagnosticsDirectory, "startup.log");
        }

        public void WriteInformation(string message) => Write("Information", message, null);

        public void WriteError(string message, Exception exception) => Write("Error", message, exception);

        private void Write(string level, string message, Exception? exception)
        {
            var timestamp = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
            var processId = Environment.ProcessId;
            var exceptionText = exception is null ? string.Empty : $"{Environment.NewLine}{exception}";
            var entry = $"{timestamp} [{level}] [Process {processId}] {message}{exceptionText}{Environment.NewLine}";

            // Ein gemeinsamer Lock verhindert ineinanderlaufende Einträge aus UI- und Pipe-Threads.
            lock (_synchronization)
            {
                File.AppendAllText(_logFilePath, entry, Encoding.UTF8);
            }
        }
    }
}