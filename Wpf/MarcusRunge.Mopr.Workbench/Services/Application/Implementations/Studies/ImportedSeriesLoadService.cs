using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies;
using System.IO;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Studies
{
    /// <summary>
    /// Resolves imported DICOM series while enforcing repository boundaries and complete physical availability.
    /// </summary>
    internal sealed class ImportedSeriesLoadService : CreateableBindableBase<IImportedSeriesLoadService, ImportedSeriesLoadService, IApplicationBase>, IImportedSeriesLoadService
    {
        private IApplicationBase? _base;
        private IApplicationBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");
        private IPersistence Persistence => Base.Persistence ?? throw new InvalidOperationException("Persistence has not been initialized.");

        /// <inheritdoc/>
        public async Task<ImportedSeriesLoadResult> ResolveAsync(ImportedSeriesLoadRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            if (request.SeriesId <= 0 || string.IsNullOrWhiteSpace(request.SeriesInstanceUid)) return ImportedSeriesLoadResult.Failed(request.SeriesId, request.SeriesInstanceUid, "The selected series identity is incomplete.");

            var repository = Persistence.ImportedSeries ?? throw new InvalidOperationException("The imported-series repository has not been initialized.");
            PersistedImportedSeries? series = await repository.GetAsync(request.SeriesId, request.SeriesInstanceUid, cancellationToken).ConfigureAwait(false);
            if (series is null) return ImportedSeriesLoadResult.Failed(request.SeriesId, request.SeriesInstanceUid, "No persisted series matches both supplied identifiers.");
            if (string.IsNullOrWhiteSpace(series.StudyInstanceUid)) return ImportedSeriesLoadResult.Failed(series.SeriesId, series.SeriesInstanceUid, "The persisted series has no valid parent study identity.");
            if (series.Instances.Count == 0) return ImportedSeriesLoadResult.Failed(series.SeriesId, series.SeriesInstanceUid, "The persisted series contains no DICOM instances.");

            List<string> files = new(series.Instances.Count);
            HashSet<string> uniqueFiles = new(StringComparer.OrdinalIgnoreCase);

            foreach (PersistedImportedSeriesInstance instance in series.Instances)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryResolveFile(instance, out var absolutePath, out var technicalError)) return ImportedSeriesLoadResult.Failed(series.SeriesId, series.SeriesInstanceUid, technicalError);
                if (!uniqueFiles.Add(absolutePath)) return ImportedSeriesLoadResult.Failed(series.SeriesId, series.SeriesInstanceUid, $"Multiple persisted instances resolve to the same repository file '{absolutePath}'.");
                files.Add(absolutePath);
            }

            return ImportedSeriesLoadResult.Succeeded(series.SeriesId, series.SeriesInstanceUid, series.StudyInstanceUid, files);
        }

        /// <inheritdoc/>
        protected override void OnCreate(IApplicationBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IApplicationBase @base, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(@base);
            cancellationToken.ThrowIfCancellationRequested();
            _base = @base;
            return Task.CompletedTask;
        }

        private static bool TryResolveFile(PersistedImportedSeriesInstance instance, out string absolutePath, out string technicalError)
        {
            absolutePath = string.Empty;
            technicalError = string.Empty;
            if (!instance.RepositoryLocationIsEnabled) return Fail($"Repository location '{instance.RepositoryLocationId}' is disabled or unavailable.", out technicalError);
            if (string.IsNullOrWhiteSpace(instance.RepositoryRootPath)) return Fail($"Repository location '{instance.RepositoryLocationId}' has no root path.", out technicalError);
            if (string.IsNullOrWhiteSpace(instance.RelativeFilePath)) return Fail($"DICOM instance '{instance.InstanceId}' has no relative repository path.", out technicalError);

            try
            {
                var repositoryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(instance.RepositoryRootPath));
                var candidatePath = Path.GetFullPath(Path.Combine(repositoryRoot, instance.RelativeFilePath));
                var repositoryBoundary = $"{repositoryRoot}{Path.DirectorySeparatorChar}";
                if (!candidatePath.StartsWith(repositoryBoundary, StringComparison.OrdinalIgnoreCase)) return Fail($"DICOM instance '{instance.InstanceId}' resolves outside repository location '{instance.RepositoryLocationId}'.", out technicalError);
                if (!File.Exists(candidatePath)) return Fail($"Repository file for DICOM instance '{instance.InstanceId}' does not exist.", out technicalError);
                using FileStream stream = new(candidatePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                absolutePath = candidatePath;
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException)
            {
                return Fail($"Repository file for DICOM instance '{instance.InstanceId}' could not be validated: {exception.Message}", out technicalError);
            }
        }

        private static bool Fail(string error, out string technicalError)
        {
            technicalError = error;
            return false;
        }
    }
}