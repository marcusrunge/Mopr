using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Bases;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Studies
{
    internal class StudiesService : StudiesServiceBase
    {
        internal StudiesService(IApplicationBase? applicationBase) : base(applicationBase)
        {
            _studyCatalog = StudyCatalogService.Create(this, CreationLifetime.Scoped);
            _importedSeriesLoader = ImportedSeriesLoadService.Create(this, CreationLifetime.Scoped);
        }

        internal static IStudiesService? Create(IApplicationBase? applicationBase) => applicationBase is null ? null : new StudiesService(applicationBase);
    }
}