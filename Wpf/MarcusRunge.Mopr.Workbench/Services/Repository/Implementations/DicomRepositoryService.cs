using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    // Concrete IDicomRepositoryService implementation using the CreatableBase lifecycle (sync create + optional async init).
    internal class DicomRepositoryService : CreateableBindableBase<IDicomRepositoryService, DicomRepositoryService, IRepositoryBase>, IDicomRepositoryService
    {
        private IRepositoryBase? _base;
        private IRepositoryBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");

        /// <inheritdoc/>
        public DicomRepositoryPathInfo CreatePathInfo(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            string relativePath = CreateRelativePath(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            return new DicomRepositoryPathInfo
            {
                StudyInstanceUid = studyInstanceUid,
                SeriesInstanceUid = seriesInstanceUid,
                SopInstanceUid = sopInstanceUid,
                RelativePath = relativePath,
                AbsolutePath = GetAbsolutePath(relativePath)
            };
        }

        /// <inheritdoc/>
        public string CreateRelativePath(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(studyInstanceUid);
            ArgumentException.ThrowIfNullOrWhiteSpace(seriesInstanceUid);
            ArgumentException.ThrowIfNullOrWhiteSpace(sopInstanceUid);

            return Path.Combine(studyInstanceUid, seriesInstanceUid, $"{sopInstanceUid}.dcm");
        }

        /// <inheritdoc/>
        public bool Exists(string relativePath) => File.Exists(GetAbsolutePath(relativePath));

        /// <inheritdoc/>
        public string GetAbsolutePath(string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
            string repositoryPath = Base.ApplicationConfiguration?.Repository?.DicomRepositoryPath ?? throw new InvalidOperationException("The DICOM repository path has not been configured.");
            return Path.Combine(repositoryPath, relativePath);
        }

        protected override void OnCreate(IRepositoryBase @base)
        {
            _base = @base;
            _ = Base.ApplicationConfiguration?.Repository?.DicomRepositoryPath ?? throw new InvalidOperationException("DICOM repository path is missing.");
        }

        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}