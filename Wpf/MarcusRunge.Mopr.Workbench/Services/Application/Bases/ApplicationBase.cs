using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Media;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;
using System.Reflection;
namespace MarcusRunge.Mopr.Workbench.Services.Application.Bases
{
    internal abstract class ApplicationBase(ILogger? logger, IAdministrativeAuthorizationService? administrativeAuthorizationService, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IPersistence? persistence, IRepository? repository) : IApplicationBase, IApplication
    {
        protected IDialogService? _dialogService;
        protected IIdentityService? _identityService;
        protected IImportService? _importService;
        protected IMediaService? _mediaService;
        protected IRepositoryLocationAdministrationService? _repositoryAdministration;
        private readonly Lock _exceptionThrownLock = new(); private Action<Exception>? _exceptionThrown;
        public event Action<Exception> ExceptionThrown { add { lock (_exceptionThrownLock) _exceptionThrown += value; } remove { lock (_exceptionThrownLock) _exceptionThrown -= value; } }
        IAdministrativeAuthorizationService? IApplicationBase.AdministrativeAuthorizationService => administrativeAuthorizationService;
        IAuditIdentityProvider? IApplicationBase.AuditIdentityProvider => (_identityService as IIdentityServiceBase)?.AuditIdentityProvider;
        public IDialogService? DialogService => _dialogService;
        public IIdentityService? IdentityService => _identityService;
        public IImportService? ImportService => _importService;
        ILogger? IApplicationBase.Logger => logger;
        public IMediaService? MediaService => _mediaService;
        IOperatingSystemIdentityProvider? IApplicationBase.OperatingSystemIdentityProvider => operatingSystemIdentityProvider;
        IPersistence? IApplicationBase.Persistence => persistence;
        IRepository? IApplicationBase.Repository => repository;
        public IRepositoryLocationAdministrationService? RepositoryAdministration => _repositoryAdministration;
        void IApplicationBase.OnExceptionThrown(Exception exception) { ArgumentNullException.ThrowIfNull(exception); logger?.LogError(exception, "Exception thrown in {AssemblyName}", Assembly.GetCallingAssembly().GetName().Name); Action<Exception>? handlers; lock (_exceptionThrownLock) handlers = _exceptionThrown; if (handlers is null) return; foreach (Action<Exception> handler in handlers.GetInvocationList().Cast<Action<Exception>>()) { try { handler(exception); } catch (Exception callbackException) { logger?.LogError(callbackException, "Exception thrown by ExceptionThrown event handler."); } } }
    }
}
