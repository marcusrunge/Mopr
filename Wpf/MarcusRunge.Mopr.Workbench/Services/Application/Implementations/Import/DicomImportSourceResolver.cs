using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
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
        private IDicomImportDriveProvider _driveProvider => Base.DicomImportDriveProvider ?? throw new InvalidOperationException("The DICOM import drive provider has not been initialized.");

        private IImportServiceBase Base => _base ?? throw new InvalidOperationException("The DICOM import service has not been initialized.");

        /// <inheritdoc/>
        public Task<DicomImportSourceResolution> ResolveAsync(DicomImportRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(Resolve(request, cancellationToken));
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

        private static bool HasDicomDirectoryIndex(string sourcePath)
        {
            try
            {
                return File.Exists(Path.Combine(sourcePath, DicomDirectoryFileName));
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
            {
                return false;
            }
        }

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

        private static DicomImportSourceResolution ResolveDirectorySource(DicomImportRequest request, string sourcePath, DicomImportSourceType sourceType, bool treatAsReadOnly, CancellationToken cancellationToken, bool driveReady = true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var exists = Directory.Exists(sourcePath);
            var isReady = driveReady && exists;

            return new DicomImportSourceResolution
            {
                ContainsDicomDirectoryIndex = isReady && HasDicomDirectoryIndex(sourcePath),
                EffectiveSourcePath = sourcePath,
                Exists = exists,
                IsReady = isReady,
                IsRepositoryImportSupported = isReady,
                OriginalSourcePath = request.SourcePath,
                SourceType = sourceType,
                TreatAsReadOnly = treatAsReadOnly
            };
        }

        private static DicomImportSourceResolution ResolveIsoImage(DicomImportRequest request, string sourcePath)
        {
            var exists = File.Exists(sourcePath);

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

        private DicomImportDriveInfo? FindContainingDrive(string sourcePath)
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

            return _driveProvider.GetDrives().Where(drive => !string.IsNullOrWhiteSpace(drive.RootPath)).OrderByDescending(drive => drive.RootPath.Length).FirstOrDefault(drive => normalizedPath.StartsWith(NormalizeRootPath(drive.RootPath), StringComparison.OrdinalIgnoreCase));
        }

        private DicomImportSourceResolution Resolve(DicomImportRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SourcePath))
            {
                return CreateUnavailableResolution(request, request.SourceType == DicomImportSourceType.AutoDetect ? DicomImportSourceType.Unknown : request.SourceType);
            }

            var sourcePath = request.SourcePath.Trim();
            cancellationToken.ThrowIfCancellationRequested();

            if (request.SourceType != DicomImportSourceType.AutoDetect)
            {
                return ResolveExplicitSource(request, sourcePath, cancellationToken);
            }

            if (IsIsoImagePath(sourcePath))
            {
                return ResolveIsoImage(request, sourcePath);
            }

            if (IsUncPath(sourcePath))
            {
                return ResolveDirectorySource(request, sourcePath, DicomImportSourceType.NetworkShare, treatAsReadOnly: true, cancellationToken);
            }

            var drive = FindContainingDrive(sourcePath);
            if (drive is null)
            {
                return Directory.Exists(sourcePath) ? ResolveDirectorySource(request, sourcePath, DicomImportSourceType.LocalDirectory, treatAsReadOnly: false, cancellationToken) : CreateUnavailableResolution(request, DicomImportSourceType.Unknown, sourcePath);
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
                return CreateUnavailableResolution(request, sourceType, sourcePath) with
                {
                    Exists = Directory.Exists(sourcePath),
                    IsReady = drive.IsReady
                };
            }

            return ResolveDirectorySource(request, sourcePath, sourceType, IsReadOnlySource(sourceType), cancellationToken, drive.IsReady);
        }

        private DicomImportSourceResolution ResolveExplicitSource(DicomImportRequest request, string sourcePath, CancellationToken cancellationToken)
        {
            if (request.SourceType == DicomImportSourceType.IsoImage)
            {
                return ResolveIsoImage(request, sourcePath);
            }

            if (!IsDirectoryBasedSource(request.SourceType))
            {
                return CreateUnavailableResolution(request, request.SourceType, sourcePath);
            }

            var drive = FindContainingDrive(sourcePath);
            var isReady = drive?.IsReady ?? true;
            return ResolveDirectorySource(request, sourcePath, request.SourceType, IsReadOnlySource(request.SourceType), cancellationToken, isReady);
        }
    }
}