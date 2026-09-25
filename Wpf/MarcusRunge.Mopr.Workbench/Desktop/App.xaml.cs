using MarcusRunge.Mopr.Workbench.Application.Administration;
using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Application.Diagnostics;
using MarcusRunge.Mopr.Workbench.Application.Identity;
using MarcusRunge.Mopr.Workbench.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Application.SingleInstance;
using MarcusRunge.Mopr.Workbench.Application.Startup;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Identity;
using MarcusRunge.Mopr.Workbench.Modules.Imaging;
using MarcusRunge.Mopr.Workbench.Modules.Import;
using MarcusRunge.Mopr.Workbench.Modules.Setup;
using MarcusRunge.Mopr.Workbench.Services.Application;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository;
using MarcusRunge.Mopr.Workbench.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RepositoryContract = MarcusRunge.Mopr.Workbench.Services.Repository.Contracts.IRepository;
using WorkbenchResources = MarcusRunge.Mopr.Workbench.Properties.Resources;

namespace MarcusRunge.Mopr.Workbench
{
    public partial class App
    {
        private readonly TaskCompletionSource _shellReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _applicationInitialization;
        private SingleInstanceCoordinator? _singleInstanceCoordinator;
        private StartupDiagnostics? _startupDiagnostics;

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<IdentityModule>();
            moduleCatalog.AddModule<ImagingModule>();
            moduleCatalog.AddModule<ImportModule>();
            moduleCatalog.AddModule<SetupModule>();
        }

        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void OnExit(ExitEventArgs e)
        {
            LifetimeService? applicationLifetime = null;

            try
            {
                _shellReady.TrySetCanceled();
                applicationLifetime = Container?.Resolve<ILifetimeService>() as LifetimeService;

                // Cancellation is signaled before the initialization task is
                // observed so active Persistence and MIRAS operations can stop.
                applicationLifetime?.Stop();
                ObserveApplicationInitialization();
            }
            finally
            {
                applicationLifetime?.Dispose();
                DisposeSingleInstanceCoordinator();
                base.OnExit(e);
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var applicationStopping = Container.Resolve<ILifetimeService>().ApplicationStopping;
            _applicationInitialization = InitializeApplicationAsync(applicationStopping);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _startupDiagnostics = new StartupDiagnostics();

            if (!TryAcquireSingleInstance())
            {
                Shutdown();
                return;
            }

            try
            {
                if (_singleInstanceCoordinator!.TryBecomePrimaryInstance() == SingleInstanceStartResult.SecondaryInstance)
                {
                    ForwardToPrimaryInstanceAndExitAsync(e.Args).GetAwaiter().GetResult();
                    return;
                }

                // The pipe server starts before Prism so concurrent launches cannot
                // reach container, module, shell, Persistence or MIRAS initialization.
                _singleInstanceCoordinator.StartListening(HandleForwardedRequestAsync);

                base.OnStartup(e);
                _shellReady.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                _shellReady.TrySetCanceled();
                DisposeSingleInstanceCoordinator();
                Shutdown();
            }
            catch (Exception exception)
            {
                HandleProtectedStartupFailure(exception);
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ArgumentNullException.ThrowIfNull(containerRegistry);

            // Application-wide infrastructure is registered first because all subsequent
            // technical modules depend on the shared lifetime and configuration state.
            containerRegistry.RegisterSingleton<ILifetimeService, LifetimeService>();
            containerRegistry.RegisterSingleton<IAdministrativeAuthorizationService, WindowsAdministrativeAuthorizationService>();
            containerRegistry.RegisterSingleton<IMachineConfigurationPathProvider, MachineConfigurationPathProvider>();
            containerRegistry.RegisterSingleton<IMachineConfigurationProtectionService, MachineConfigurationProtectionService>();
            containerRegistry.RegisterSingleton<IApplicationConfigurationStore, ApplicationConfigurationStore>();
            containerRegistry.RegisterSingleton<IRepositoryLocationValidationService, RepositoryLocationValidationService>();

            var persistenceConfigurationSubject = new BehaviorSubject<PersistenceConfiguration>(new PersistenceConfiguration());
            containerRegistry.RegisterInstance(persistenceConfigurationSubject);
            containerRegistry.RegisterInstance<IObservable<PersistenceConfiguration>>(persistenceConfigurationSubject);

            var applicationConfiguration = new ApplicationConfiguration();
            containerRegistry.RegisterInstance<IApplicationConfiguration>(applicationConfiguration);

            var applicationConfigurationSubject = new BehaviorSubject<IApplicationConfiguration>(applicationConfiguration);
            containerRegistry.RegisterInstance(applicationConfigurationSubject);
            containerRegistry.RegisterInstance<IObservable<IApplicationConfiguration>>(applicationConfigurationSubject);

            // The DICOM module owns parsing, metadata extraction and image decoding only.
            containerRegistry.RegisterSingleton<IDicomFactory, DicomFactory>();
            containerRegistry.RegisterSingleton<IDicom>(provider => provider.Resolve<IDicomFactory>().Create());

            // Persistence must be registered before application services that resolve
            // persisted users, repository locations or audit identities.
            containerRegistry.RegisterSingleton<IPersistenceFactory>(provider => new PersistenceFactory(provider.Resolve<ILifetimeService>(), provider.Resolve<IObservable<PersistenceConfiguration>>()));
            containerRegistry.RegisterSingleton<IPersistence>(provider => provider.Resolve<IPersistenceFactory>().Create());

            containerRegistry.RegisterSingleton<IMachineConfigurationService>(provider => new MachineConfigurationService(provider.Resolve<IAdministrativeAuthorizationService>(), provider.Resolve<IApplicationConfigurationStore>(), provider.Resolve<IPersistence>()));
            containerRegistry.RegisterSingleton<ISetupAuditIdentityProvider, SetupAuditIdentityProvider>();
            containerRegistry.RegisterSingleton<ISetupCompletionService, SetupCompletionService>();
            containerRegistry.RegisterSingleton<IApplicationStartupRouteService, ApplicationStartupRouteService>();

            // The repository module owns physical DICOM storage, atomic import,
            // compensation and repository-level integrity operations.
            containerRegistry.RegisterSingleton<IRepositoryFactory>(provider => new RepositoryFactory(provider.Resolve<ILifetimeService>(), provider.Resolve<IObservable<IApplicationConfiguration>>(), provider.Resolve<IPersistence>()));
            containerRegistry.RegisterSingleton<RepositoryContract>(provider => provider.Resolve<IRepositoryFactory>().Create());

            // Runtime security adapters resolve the current Windows identity against
            // an existing persistent MOPR user without implicit user provisioning.
            containerRegistry.RegisterSingleton<IOperatingSystemIdentityProvider, OperatingSystemIdentityProvider>();

            // MIRAS depends on both Persistence and Repository and must therefore be
            // constructed only after both technical modules have been registered.
            containerRegistry.RegisterSingleton<IMirasFactory>(provider => new MirasFactory(provider.Resolve<ILifetimeService>(), provider.Resolve<IPersistence>(), provider.Resolve<RepositoryContract>()));
            containerRegistry.RegisterSingleton<IMiras>(provider => provider.Resolve<IMirasFactory>().Create());
            containerRegistry.RegisterSingleton<IOperations>(provider => provider.Resolve<IMiras>().Operations ?? throw new InvalidOperationException("The MIRAS check service has not been initialized."));

            // Core exposes UI-facing workflows over the initialized technical services.
            containerRegistry.RegisterSingleton<ICoreFactory>(provider => new CoreFactory(provider.Resolve<IDicom>(), provider.Resolve<ILifetimeService>()));
            containerRegistry.RegisterSingleton<ICore>(provider => provider.Resolve<ICoreFactory>().Create());

            // WPF-specific services remain at the outermost application boundary.
            containerRegistry.RegisterSingleton<IApplicationFactory>(provider => new ApplicationFactory(provider.Resolve<IPersistence>(), provider.Resolve<RepositoryContract>(), provider.Resolve<IOperatingSystemIdentityProvider>()));
            containerRegistry.RegisterSingleton<IApplication>(provider => provider.Resolve<IApplicationFactory>().Create());

            // The user startup route service is a WPF-specific implementation that resolves the initial navigation target for the current operating-system user.
            containerRegistry.RegisterSingleton<IUserStartupRouteService, UserStartupRouteService>();
        }

        private static void ShowForwardingFailedMessage() => MessageBox.Show(WorkbenchResources.SingleInstanceForwardingFailedMessage, WorkbenchResources.SingleInstanceForwardingFailedTitle, MessageBoxButton.OK, MessageBoxImage.Information);

        private static void ShowSingleInstanceStartupFailedMessage() => MessageBox.Show(WorkbenchResources.SingleInstanceStartupFailedMessage, WorkbenchResources.SingleInstanceStartupFailedTitle, MessageBoxButton.OK, MessageBoxImage.Error);

        private void DisposeSingleInstanceCoordinator()
        {
            if (_singleInstanceCoordinator is null)
            {
                return;
            }

            _singleInstanceCoordinator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _singleInstanceCoordinator = null;
        }

        private async Task ForwardToPrimaryInstanceAndExitAsync(string[] arguments)
        {
            try
            {
                using var stopping = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                await _singleInstanceCoordinator!.ForwardToPrimaryInstanceAsync(arguments, stopping.Token);
            }
            catch (OperationCanceledException)
            {
                _startupDiagnostics!.WriteInformation("Forwarding the startup request to the primary MOPR instance was canceled or timed out.");

                ShowForwardingFailedMessage();
            }
            catch (Exception exception)
            {
                _startupDiagnostics!.WriteError("The startup request could not be forwarded to the primary MOPR instance.", exception);

                ShowForwardingFailedMessage();
            }
            finally
            {
                DisposeSingleInstanceCoordinator();
                Shutdown();
            }
        }

        private async Task HandleForwardedRequestAsync(SingleInstanceRequest request, CancellationToken cancellationToken)
        {
            await _shellReady.Task.WaitAsync(cancellationToken);

            await Dispatcher.InvokeAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (MainWindow is MainWindow mainWindow)
                {
                    mainWindow.ActivateFromSecondInstance();
                }

                var arguments = request.Arguments.Length == 0 ? "none" : string.Join(", ", request.Arguments);

                _startupDiagnostics!.WriteInformation($"Forwarded startup arguments: {arguments}");
            });
        }

        private void HandleProtectedStartupFailure(Exception exception)
        {
            _startupDiagnostics!.WriteError("MOPR startup failed before protected application initialization completed.", exception);

            _shellReady.TrySetException(exception);
            DisposeSingleInstanceCoordinator();
            ShowSingleInstanceStartupFailedMessage();
            Shutdown();
        }

        private async Task InitializeApplicationAsync(CancellationToken cancellationToken)
        {
            try
            {
                var machineRouteService = Container.Resolve<IApplicationStartupRouteService>();
                var machineNavigationTarget = await machineRouteService.GetInitialNavigationTargetAsync(cancellationToken).ConfigureAwait(false);

                if (machineNavigationTarget == NavigationNames.Setup)
                {
                    await NavigateAsync(NavigationNames.Setup, cancellationToken).ConfigureAwait(false);
                    _startupDiagnostics!.WriteInformation("MOPR setup is required before managed application services can be initialized.");
                    return;
                }

                var machineConfigurationService = Container.Resolve<IMachineConfigurationService>();
                var configuration = await machineConfigurationService.LoadAsync(cancellationToken).ConfigureAwait(false);

                PublishApplicationConfiguration(configuration);
                PublishPersistenceConfiguration(configuration);

                var persistence = Container.Resolve<IPersistence>();

                // Persistent user resolution and MIRAS must never access the configured
                // database before the Persistence provider has initialized successfully.
                await persistence.Initialization.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                var userRouteService = Container.Resolve<IUserStartupRouteService>();
                var userNavigationTarget = await userRouteService.GetNavigationTargetAsync(cancellationToken).ConfigureAwait(false);

                var mirasFlow = Container.Resolve<IMiras>().Flow ?? throw new InvalidOperationException("The MIRAS flow has not been initialized.");
                var mirasResult = await mirasFlow.StartAsync(cancellationToken).ConfigureAwait(false);

                _startupDiagnostics!.WriteInformation($"The initial MIRAS check completed with status '{mirasResult.Status}' and inspected {mirasResult.ScannedItems} items.");

                await NavigateAsync(userNavigationTarget, cancellationToken).ConfigureAwait(false);

                _startupDiagnostics.WriteInformation($"MOPR startup navigation completed with target '{userNavigationTarget}'.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _startupDiagnostics!.WriteInformation("MOPR application initialization was canceled because the application is stopping.");
            }
            catch (Exception exception)
            {
                // A damaged or unreadable machine configuration still belongs to Setup.
                // Failures after machine setup has completed must not silently create or
                // authenticate a user, so they are routed to the safe identity state.
                _startupDiagnostics!.WriteError("MOPR application initialization could not be completed.", exception);

                try
                {
                    var fallbackTarget = await ResolveStartupFailureTargetAsync(cancellationToken).ConfigureAwait(false);
                    await NavigateAsync(fallbackTarget, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _startupDiagnostics.WriteInformation("Startup failure navigation was canceled because the application is stopping.");
                }
                catch (Exception navigationException)
                {
                    _startupDiagnostics.WriteError("The protected startup failure state could not be displayed.", navigationException);
                }
            }
        }

        private async Task NavigateAsync(string navigationTarget, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(navigationTarget))
            {
                throw new ArgumentException("The navigation target must not be empty.", nameof(navigationTarget));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            using var cancellationRegistration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

            await Dispatcher.InvokeAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var regionManager = Container.Resolve<IRegionManager>();

                regionManager.RequestNavigate(RegionNames.ContentRegion, navigationTarget, result =>
                {
                    if (result.Success)
                    {
                        completion.TrySetResult();
                        return;
                    }

                    var exception = result.Exception ?? new InvalidOperationException($"Navigation to '{navigationTarget}' was not completed.");
                    completion.TrySetException(new InvalidOperationException($"Navigation to '{navigationTarget}' in region '{RegionNames.ContentRegion}' failed.", exception));
                });
            });

            await completion.Task.ConfigureAwait(false);
        }

        private void ObserveApplicationInitialization()
        {
            var applicationInitialization = _applicationInitialization;

            if (applicationInitialization is null)
            {
                return;
            }

            try
            {
                applicationInitialization.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // InitializeApplicationAsync normally handles shutdown cancellation.
                // This guard protects shutdown if cancellation occurs before its try block.
            }
            catch (Exception exception)
            {
                // InitializeApplicationAsync normally handles failures internally.
                // This boundary prevents an unexpected observation failure from
                // interrupting application shutdown.
                _startupDiagnostics?.WriteError("The MOPR application initialization task ended unexpectedly.", exception);
            }
            finally
            {
                _applicationInitialization = null;
            }
        }

        private void PublishApplicationConfiguration(IApplicationConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            Container.Resolve<BehaviorSubject<IApplicationConfiguration>>().OnNext(configuration);
        }

        private void PublishPersistenceConfiguration(IApplicationConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            Container.Resolve<BehaviorSubject<PersistenceConfiguration>>().OnNext(new PersistenceConfiguration
            {
                ConnectionString = configuration.Database.ConnectionString,
                Mode = PersistenceMode.SqlServer
            });
        }

        private async Task<string> ResolveStartupFailureTargetAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var routeService = Container.Resolve<IApplicationStartupRouteService>();
                var target = await routeService.GetInitialNavigationTargetAsync(cancellationToken).ConfigureAwait(false);
                return target == NavigationNames.Setup ? NavigationNames.Setup : NavigationNames.IdentityUnavailable;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _startupDiagnostics!.WriteError("The startup failure target could not be determined.", exception);
                return NavigationNames.IdentityUnavailable;
            }
        }

        private bool TryAcquireSingleInstance()
        {
            try
            {
                _singleInstanceCoordinator = new SingleInstanceCoordinator(SingleInstanceOptions.CreateDefault(Process.GetCurrentProcess().SessionId), _startupDiagnostics!, new ForegroundPermission());

                return true;
            }
            catch (Exception exception)
            {
                _startupDiagnostics!.WriteError("The MOPR single-instance coordinator could not be created.", exception);

                ShowSingleInstanceStartupFailedMessage();
                return false;
            }
        }
    }
}