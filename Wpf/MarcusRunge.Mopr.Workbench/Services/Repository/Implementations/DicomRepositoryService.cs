using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    // Concrete IDicomRepositoryService implementation using the CreatableBase lifecycle (sync create + optional async init).
    internal class DicomRepositoryService : CreateableBindableBase<IDicomRepositoryService, DicomRepositoryService, IRepositoryBase>, IDicomRepositoryService
    {
        private IRepositoryBase? _base;

        /// <inheritdoc/>
        public string CreateRelativePath(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid) => Path.Combine(studyInstanceUid, seriesInstanceUid, $"{sopInstanceUid}.dcm");

        /// <inheritdoc/>
        public bool Exists(string relativePath) => File.Exists(GetAbsolutePath(relativePath));

        /// <inheritdoc/>
        public string GetAbsolutePath(string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
            string repositoryPath = _base?.ApplicationConfiguration?.Repository?.DicomRepositoryPath ?? throw new InvalidOperationException("The DICOM repository path has not been configured.");
            return Path.Combine(repositoryPath, relativePath);
        }

        protected override void OnCreate(IRepositoryBase @base) => _base = @base;

        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}