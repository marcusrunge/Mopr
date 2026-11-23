using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;
using System.IO;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Import
{
    /// <summary>
    /// Classifies DICOM import sources without mounting media, establishing network connections or modifying source content.
    /// </summary>
    internal sealed class DicomImportSourceResolver : CreateableBindableBase<IDicomImportSourceResolver, DicomImportSourceResolver, IImportServiceBase>, IDicomImportSourceResolver
    {
        private const string DicomDirectoryFileName = "DICOMDIR";

        private IImportServiceBase? _base;
        private IDicomImportDriveProvider DriveProvider => Base.DicomImportDriveProvider ?? throw new InvalidOperationException("The DICOM import drive provider has not been initialized.");
        private IImportServiceBase Base => _base ?? throw new InvalidOperationException("The DICOM import service has not been initialized.");

        /// <inheritdoc/>
        public async Task<DicomImportSourceResolution> ResolveAsync(DicomImportRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request.SourcePath))
            {
                return CreateUnavailableResolution(request, request.SourceType == DicomImportSourceType.AutoDetect ? DicomImportSourceType.Unknown : request.SourceType);
            }

            var sourcePath = request.SourcePath.Trim();

            if (request.SourceType != DicomImportSourceType.AutoDetect)
            {
                return await ResolveExplicitSourceAsync(request, sourcePath, cancellationToken).ConfigureAwait(false);
            }

            if (IsIsoImagePath(sourcePath))
            {
                return await ResolveIsoImageAsync(request, sourcePath, cancellationToken).ConfigureAwait(false);
            }

            if (IsUncPath(sourcePath))
            {
                return await ResolveDirectorySourceAsync(request, sourcePath, DicomImportSourceType.NetworkShare, treatAsReadOnly: true, cancellationToken).ConfigureAwait(false);
            }

            var drive = await FindContainingDriveAsync(sourcePath, cancellationToken).ConfigureAwait(false);

            if (drive is null)
            {
                var directoryExists = await DirectoryExistsAsync(sourcePath, cancellationToken).ConfigureAwait(false);
                return directoryExists
                    ? await ResolveDirectorySourceAsync(request, sourcePath, DicomImportSourceType.LocalDirectory, treatAsReadOnly: false, cancellationToken).ConfigureAwait(false)
                    : CreateUnavailableResolution(request, DicomImportSourceType.Unknown, sourcePath);
            }

            var sourceType = drive.DriveType switch
            {
                DriveType.Network => DicomImportSourceType.MappedNetworkDrive,
                DriveType.CDRom => DicomImportSourceType.CdRom,
                DriveType.Removable => DicomImportSourceType.RemovableDrive,
                DriveType.Fixed => DicomImportSourceType.LocalDirectory,
                DriveType.Ram => DicomImportSourceType.VirtualDrive,
                _ => DicomImportSourceType.Unknown
            };

            if (sourceType == DicomImportSourceType.Unknown)
            {
                var directoryExists = await DirectoryExistsAsync(sourcePath, cancellationToken).ConfigureAwait(false);
                return CreateUnavailableResolution(request, sourceType, sourcePath) with
                {
                    Exists = directoryExists,
                    IsReady = drive.IsReady
                };
            }

            return await ResolveDirectorySourceAsync(request, sourcePath, sourceType, IsReadOnlySource(sourceType), cancellationToken, drive.IsReady).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override void OnCreate(IImportServiceBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IImportServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static DicomImportSourceResolution CreateUnavailableResolution(DicomImportRequest request, DicomImportSourceType sourceType, string effectiveSourcePath = "") => new()
        {
            EffectiveSourcePath = effectiveSourcePath,
            Exists = false,
            IsReady = false,
            IsRepositoryImportSupported = false,
            OriginalSourcePath = request.SourcePath,
            SourceType = sourceType,
            TreatAsReadOnly = IsReadOnlySource(sourceType)
        };

        private static Task<bool> DirectoryExistsAsync(string sourcePath, CancellationToken cancellationToken) => Task.Run(() => Directory.Exists(sourcePath), cancellationToken);

        private static Task<bool> FileExistsAsync(string sourcePath, CancellationToken cancellationToken) => Task.Run(() => File.Exists(sourcePath), cancellationToken);

        private static Task<bool> HasDicomDirectoryIndexAsync(string sourcePath, CancellationToken cancellationToken) => Task.Run(() =>
        {
            try
            {
                return File.Exists(Path.Combine(sourcePath, DicomDirectoryFileName));
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
            {
                return false;
            }
        }, cancellationToken);

        private static bool IsDirectoryBasedSource(DicomImportSourceType sourceType) => sourceType is DicomImportSourceType.LocalDirectory or DicomImportSourceType.UsbDrive or DicomImportSourceType.RemovableDrive or DicomImportSourceType.ExternalDrive or DicomImportSourceType.SdCard or DicomImportSourceType.CdRom or DicomImportSourceType.Dvd or DicomImportSourceType.NetworkShare or DicomImportSourceType.MappedNetworkDrive or DicomImportSourceType.VirtualDrive;

        private static bool IsIsoImagePath(string sourcePath) => string.Equals(Path.GetExtension(sourcePath), ".iso", StringComparison.OrdinalIgnoreCase);

        private static bool IsReadOnlySource(DicomImportSourceType sourceType) => sourceType is DicomImportSourceType.CdRom or DicomImportSourceType.Dvd or DicomImportSourceType.IsoImage or DicomImportSourceType.NetworkShare or DicomImportSourceType.MappedNetworkDrive;

        private static bool IsUncPath(string sourcePath)
        {
            try
            {
                return new Uri(sourcePath).IsUnc;
            }
            catch (UriFormatException)
            {
                return sourcePath.StartsWith(@"\\", StringComparison.Ordinal);
            }
        }

        private static string NormalizeRootPath(string rootPath)
        {
            var normalizedRoot = Path.GetFullPath(rootPath);
            return normalizedRoot.EndsWith(Path.DirectorySeparatorChar) ? normalizedRoot : $"{normalizedRoot}{Path.DirectorySeparatorChar}";
        }

        private static async Task<DicomImportSourceResolution> ResolveDirectorySourceAsync(DicomImportRequest request, string sourcePath, DicomImportSourceType sourceType, bool treatAsReadOnly, CancellationToken cancellationToken, bool driveReady = true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var exists = await DirectoryExistsAsync(sourcePath, cancellationToken).ConfigureAwait(false);
            var isReady = driveReady && exists;
            var containsDicomDirectoryIndex = isReady && await HasDicomDirectoryIndexAsync(sourcePath, cancellationToken).ConfigureAwait(false);

            return new DicomImportSourceResolution
            {
                ContainsDicomDirectoryIndex = containsDicomDirectoryIndex,
                EffectiveSourcePath = sourcePath,
                Exists = exists,
                IsReady = isReady,
                IsRepositoryImportSupported = isReady,
                OriginalSourcePath = request.SourcePath,
                SourceType = sourceType,
                TreatAsReadOnly = treatAsReadOnly
            };
        }

        private static async Task<DicomImportSourceResolution> ResolveIsoImageAsync(DicomImportRequest request, string sourcePath, CancellationToken cancellationToken)
        {
            var exists = await FileExistsAsync(sourcePath, cancellationToken).ConfigureAwait(false);

            return new DicomImportSourceResolution
            {
                EffectiveSourcePath = sourcePath,
                Exists = exists,
                IsReady = false,
                IsRepositoryImportSupported = false,
                OriginalSourcePath = request.SourcePath,
                SourceType = DicomImportSourceType.IsoImage,
                TreatAsReadOnly = true
            };
        }

        private async Task<DicomImportDriveInfo?> FindContainingDriveAsync(string sourcePath, CancellationToken cancellationToken)
        {
            string normalizedPath;

            try
            {
                normalizedPath = Path.GetFullPath(sourcePath);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return null;
            }

            await foreach (var drive in DriveProvider.GetDrivesAsync(cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(drive.RootPath))
                {
                    continue;
                }

                string normalizedRoot;

                try
                {
                    normalizedRoot = NormalizeRootPath(drive.RootPath);
                }
                catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    continue;
                }

                // Windows drive roots do not overlap. Returning the first containing root avoids waiting for
                // unrelated slow network, optical or removable drives after the required drive was identified.
                if (normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return drive;
                }
            }

            return null;
        }

        private async Task<DicomImportSourceResolution> ResolveExplicitSourceAsync(DicomImportRequest request, string sourcePath, CancellationToken cancellationToken)
        {
            if (request.SourceType == DicomImportSourceType.IsoImage)
            {
                return await ResolveIsoImageAsync(request, sourcePath, cancellationToken).ConfigureAwait(false);
            }

            if (!IsDirectoryBasedSource(request.SourceType))
            {
                return CreateUnavailableResolution(request, request.SourceType, sourcePath);
            }

            var drive = await FindContainingDriveAsync(sourcePath, cancellationToken).ConfigureAwait(false);
            var isReady = drive?.IsReady ?? true;
            return await ResolveDirectorySourceAsync(request, sourcePath, request.SourceType, IsReadOnlySource(request.SourceType), cancellationToken, isReady).ConfigureAwait(false);
        }
    }
}