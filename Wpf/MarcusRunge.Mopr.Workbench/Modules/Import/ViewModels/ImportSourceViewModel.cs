using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using Prism.Mvvm;

namespace MarcusRunge.Mopr.Workbench.Modules.Import.ViewModels
{
    /// <summary>
    /// Represents the source selected for a DICOM import operation.
    /// </summary>
    public sealed class ImportSourceViewModel : BindableBase
    {
        private string _displayName = Properties.Resources.NoSourceSelected;
        private bool _hasSelection;
        private string _path = string.Empty;
        private DicomImportSourceType _sourceType = DicomImportSourceType.AutoDetect;

        public string DisplayName
        {
            get => _displayName;
            private set => SetProperty(ref _displayName, value);
        }

        public bool HasSelection
        {
            get => _hasSelection;
            private set => SetProperty(ref _hasSelection, value);
        }

        public string Path
        {
            get => _path;
            private set => SetProperty(ref _path, value);
        }

        public DicomImportSourceType SourceType
        {
            get => _sourceType;
            private set => SetProperty(ref _sourceType, value);
        }

        public void Clear()
        {
            Path = string.Empty;
            DisplayName = Properties.Resources.NoSourceSelected;
            SourceType = DicomImportSourceType.AutoDetect;
            HasSelection = false;
        }

        public void Select(string sourcePath, DicomImportSourceType sourceType = DicomImportSourceType.AutoDetect)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

            var normalizedSourcePath = sourcePath.Trim();

            Path = normalizedSourcePath;
            DisplayName = CreateDisplayName(normalizedSourcePath);
            SourceType = sourceType;
            HasSelection = true;
        }

        private static string CreateDisplayName(string sourcePath)
        {
            // Explicit qualification prevents the Path property of this ViewModel from
            // hiding the System.IO.Path type used for filesystem path operations.
            var trimmedPath = sourcePath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            var displayName = System.IO.Path.GetFileName(trimmedPath);

            return string.IsNullOrWhiteSpace(displayName) ? sourcePath : displayName;
        }
    }
}