using MarcusRunge.Mopr.Workbench.Application.Administration;
using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Application.Diagnostics;
using MarcusRunge.Mopr.Workbench.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Application.SingleInstance;
using MarcusRunge.Mopr.Workbench.Application.Startup;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Contracts.Miras;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Imaging;
using MarcusRunge.Mopr.Workbench.Modules.Setup;
using MarcusRunge.Mopr.Workbench.Services.Core;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
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
using WorkbenchResources = MarcusRunge.Mopr.Workbench.Properties.Resources;

namespace MarcusRunge.Mopr.Workbench
{
    public partial class App
    {
        private readonly TaskCompletionSource _shellReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _applicationInitialization;
        private StartupDiagnostics? _startupDiagnostics;
        private SingleInstanceCoordinator? _singleInstanceCoordinator;

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ImagingModule>();
            moduleCatalog.AddModule<SetupModule>();
        }

        protected override Window CreateShell() => Container.Resolve<MainWindow>();

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

        protected override void OnExit(ExitEventArgs e)
        {
            ApplicationLifetime? applicationLifetime = null;

            try
            {
                _shellReady.TrySetCanceled();
                applicationLifetime = Container?.Resolve<IApplicationLifetime>() as ApplicationLifetime;

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

            var applicationStopping = Container.Resolve<IApplicationLifetime>().ApplicationStopping;
            _applicationInitialization = InitializeApplicationAsync(applicationStopping);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IApplicationLifetime, ApplicationLifetime>();

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

            containerRegistry.RegisterSingleton<IDicomFactory, DicomFactory>();
            containerRegistry.RegisterSingleton<IDicom>(provider => provider.Resolve<IDicomFactory>().Create());

            containerRegistry.RegisterSingleton<IPersistenceFactory>(provider => new PersistenceFactory(provider.Resolve<IApplicationLifetime>(), provider.Resolve<IObservable<PersistenceConfiguration>>()));
            containerRegistry.RegisterSingleton<IPersistence>(provider => provider.Resolve<IPersistenceFactory>().Create());

            containerRegistry.RegisterSingleton<IMachineConfigurationService>(provider => new MachineConfigurationService(provider.Resolve<IAdministrativeAuthorizationService>(), provider.Resolve<IApplicationConfigurationStore>(), provider.Resolve<IPersistence>()));
            containerRegistry.RegisterSingleton<IApplicationStartupRouteService, ApplicationStartupRouteService>();

            containerRegistry.RegisterSingleton<IRepositoryFactory>(provider => new RepositoryFactory(provider.Resolve<IApplicationLifetime>(), provider.Resolve<IObservable<IApplicationConfiguration>>(), provider.Resolve<IPersistence>()));
            containerRegistry.RegisterSingleton<IRepository>(provider => provider.Resolve<IRepositoryFactory>().Create());

            containerRegistry.RegisterSingleton<IMirasFactory, MirasFactory>();
            containerRegistry.RegisterSingleton<IMiras>(provider => provider.Resolve<IMirasFactory>().Create());
            containerRegistry.RegisterSingleton<IMirasService>(provider => provider.Resolve<IMiras>().MirasService ?? throw new InvalidOperationException("The MIRAS check service has not been initialized."));

            containerRegistry.RegisterSingleton<ICoreFactory>(provider => new CoreFactory(provider.Resolve<IDicom>(), provider.Resolve<IApplicationLifetime>(), provider.Resolve<IMirasService>()));
            containerRegistry.RegisterSingleton<ICore>(provider => provider.Resolve<ICoreFactory>().Create());

            containerRegistry.RegisterSingleton<IWpfFactory, WpfFactory>();
            containerRegistry.RegisterSingleton<IWpf>(provider => provider.Resolve<IWpfFactory>().Create());
        }

        private async Task InitializeApplicationAsync(CancellationToken cancellationToken)
        {
            try
            {
                var routeService = Container.Resolve<IApplicationStartupRouteService>();
                var navigationTarget = await routeService.GetInitialNavigationTargetAsync(cancellationToken).ConfigureAwait(false);

                if (navigationTarget == NavigationNames.Setup)
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

                // MIRAS must never inspect repository relationships before the
                // configured Persistence provider is fully initialized.
                await persistence.Initialization.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                await NavigateAsync(NavigationNames.Imaging, cancellationToken).ConfigureAwait(false);

                var mirasFlowService = Container.Resolve<ICore>().MirasApplicationService?.MirasFlowService ?? throw new InvalidOperationException("The MIRAS flow service has not been initialized.");
                var result = await mirasFlowService.StartAsync(cancellationToken).ConfigureAwait(false);

                _startupDiagnostics!.WriteInformation($"The initial MIRAS check completed with status '{result.Status}' and inspected {result.ScannedItems} items.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _startupDiagnostics!.WriteInformation("MOPR application initialization was canceled because the application is stopping.");
            }
            catch (Exception exception)
            {
                // A damaged or unreadable machine configuration is routed to Setup
                // after diagnostics capture the technical failure.
                _startupDiagnostics!.WriteError("MOPR application initialization could not be completed.", exception);

                try
                {
                    await NavigateAsync(NavigationNames.Setup, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _startupDiagnostics!.WriteInformation("Navigation to MOPR setup was canceled because the application is stopping.");
                }
                catch (Exception navigationException)
                {
                    _startupDiagnostics!.WriteError("MOPR setup could not be displayed after application initialization failed.", navigationException);
                }
            }
        }

        private async Task NavigateAsync(string navigationTarget, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Dispatcher.InvokeAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Container.Resolve<IRegionManager>().RequestNavigate(RegionNames.ContentRegion, navigationTarget);
            });
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
    }
}