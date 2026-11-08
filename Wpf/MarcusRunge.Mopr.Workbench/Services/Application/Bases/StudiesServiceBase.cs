using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Bases
{
    internal class StudiesServiceBase(IApplicationBase? applicationBase) : IStudiesServiceBase, IStudiesService
    {
        protected IImportedSeriesLoadService? _importedSeriesLoader;
        protected IStudyCatalogService? _studyCatalog;
        IApplicationBase? IServiceBase.ApplicationBase => applicationBase;
        public IImportedSeriesLoadService? ImportedSeriesLoader => _importedSeriesLoader;
        public IStudyCatalogService? StudyCatalog => _studyCatalog;
    }
}