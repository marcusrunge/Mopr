using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    // Concrete IDicomImportService implementation using the CreatableBase lifecycle (sync create + optional async init).
    internal class DicomImportService : CreateableBindableBase<IDicomImportService, DicomImportService, IRepositoryBase>, IDicomImportService
    {
        private IRepositoryBase? _base;

        private IRepositoryBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");

        /// <inheritdoc/>
        public async Task<DicomImportResult> ImportAsync(DicomImportRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            ArgumentException.ThrowIfNullOrWhiteSpace(request.SourcePath);

            DicomImportResult result = new();

            if (!Directory.Exists(request.SourcePath))
            {
                result.FailedFiles++;

                result.Errors.Add($"Source path '{request.SourcePath}' does not exist.");

                return result;
            }

            result.DiscoveredFiles = request.SourceType switch
            {
                ImportSourceType.Directory => Directory.EnumerateFiles(request.SourcePath, "*", SearchOption.AllDirectories).Count(),
                ImportSourceType.CdRom => throw new NotSupportedException(),
                ImportSourceType.Dvd => throw new NotSupportedException(),
                ImportSourceType.UsbDrive => throw new NotSupportedException(),
                ImportSourceType.IsoImage => throw new NotSupportedException(),
                ImportSourceType.NetworkShare => throw new NotSupportedException(),
                ImportSourceType.Unknown => throw new ArgumentException("The import source type must not be Unknown.", nameof(request)),
                _ => throw new NotSupportedException($"Import source type '{request.SourceType}' is currently not supported."),
            };
            await Task.CompletedTask;

            return result;
        }

        protected override void OnCreate(IRepositoryBase @base)
        {
            // What happens here:
            // - This is the synchronous creation hook executed exactly once for the singleton-like instance.
            // - Use this method to perform quick, non-async setup that must happen before the instance is published
            //   to other callers (e.g., assigning references, initializing cheap state, wiring non-async dependencies).
            //
            // Current behavior:
            // - Intentionally empty: ServiceB requires no synchronous initialization at creation time.
            //
            // Notes:
            // - Avoid long-running or blocking work here; that belongs into OnCreateAsync to keep creation fast
            //   and reduce lock hold time during instance publication.
            _base = @base;
        }

        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken) =>
            /*What happens here:
              - This is the asynchronous initialization hook that runs after the instance exists.
              - It is invoked by the base lifecycle to perform potentially expensive/IO work without blocking creation.
              - Returning Task.CompletedTask signals: "no async initialization required" for ServiceB.
              - The provided cancellationToken is not used here because there is nothing to cancel. */
            Task.CompletedTask;

        private static IList<DicomImportFileInfo> CreateFileInfos(string sourcePath) => [.. Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories).Select(file => new DicomImportFileInfo { FileName = Path.GetFileName(file), FilePath = file })];
    }
}