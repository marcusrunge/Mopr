using WorkbenchResources = MarcusRunge.Mopr.Workbench.Properties.Resources;
using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Application.Diagnostics;
using MarcusRunge.Mopr.Workbench.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Application.SingleInstance;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Modules.Imaging;
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
using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MarcusRunge.Mopr.Workbench
{
    public partial class App
    {
        private readonly TaskCompletionSource _shellReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IStartupDiagnostics? _startupDiagnostics;
        private SingleInstanceCoordinator? _singleInstanceCoordinator;

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog) => moduleCatalog.AddModule<ImagingModule>();

        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void OnStartup(StartupEventArgs e)
        {
            _startupDiagnostics = new StartupDiagnostics();
            _singleInstanceCoordinator = new SingleInstanceCoordinator(SingleInstanceOptions.CreateDefault(Process.GetCurrentProcess().SessionId), _startupDiagnostics, new ForegroundPermission());

            try
            {
                var startResult = _singleInstanceCoordinator.TryBecomePrimaryInstance();
                if (startResult == SingleInstanceStartResult.SecondaryInstance)
                {
                    ForwardToPrimaryInstanceAndExitAsync(e.Args).GetAwaiter().GetResult();
                    return;
                }

                // Der Pipe-Server muss vor Prism laufen, damit nahezu gleichzeitige Starts nicht
                // bis zur Container-, Modul-, Shell- oder Persistence-Initialisierung vordringen.
                _singleInstanceCoordinator.StartListening(HandleForwardedRequestAsync);
                base.OnStartup(e);
                _shellReady.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                Shutdown();
            }
            catch (Exception exception)
            {
                _startupDiagnostics.WriteError("Der MOPR-Start ist vor Abschluss der Initialisierung fehlgeschlagen.", exception);
                _shellReady.TrySetException(exception);
                DisposeSingleInstanceCoordinator();
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _shellReady.TrySetCanceled();

                if (Container?.Resolve<IApplicationLifetime>() is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            finally
            {
                DisposeSingleInstanceCoordinator();
                base.OnExit(e);
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _ = Container.Resolve<IPersistence>();

            var subject = Container.Resolve<BehaviorSubject<PersistenceConfiguration>>();
            subject.OnNext(new PersistenceConfiguration
            {
                ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=MoprDb;Integrated Security=True;TrustServerCertificate=True;",
                Mode = PersistenceMode.SqlServer
            });
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IApplicationLifetime, ApplicationLifetime>();

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

            containerRegistry.RegisterSingleton<ICoreFactory>(provider => new CoreFactory(provider.Resolve<IDicom>()));
            containerRegistry.RegisterSingleton<ICore>(provider => provider.Resolve<ICoreFactory>().Create());

            containerRegistry.RegisterSingleton<IPersistenceFactory>(provider => new PersistenceFactory(provider.Resolve<IApplicationLifetime>(), provider.Resolve<IObservable<PersistenceConfiguration>>()));
            containerRegistry.RegisterSingleton<IPersistence>(provider => provider.Resolve<IPersistenceFactory>().Create());

            containerRegistry.RegisterSingleton<IRepositoryFactory>(provider => new RepositoryFactory(provider.Resolve<IApplicationLifetime>(), provider.Resolve<IObservable<IApplicationConfiguration>>(), provider.Resolve<IPersistence>()));
            containerRegistry.RegisterSingleton<IRepository>(provider => provider.Resolve<IRepositoryFactory>().Create());

            containerRegistry.RegisterSingleton<IMirasFactory, MirasFactory>();
            containerRegistry.RegisterSingleton<IMiras>(provider => provider.Resolve<IMirasFactory>().Create());

            containerRegistry.RegisterSingleton<IWpfFactory, WpfFactory>();
            containerRegistry.RegisterSingleton<IWpf>(provider => provider.Resolve<IWpfFactory>().Create());
        }

        private async Task ForwardToPrimaryInstanceAndExitAsync(string[] arguments)
        {
            try
            {
                using var stopping = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await _singleInstanceCoordinator!.ForwardToPrimaryInstanceAsync(arguments, stopping.Token);
            }
            catch (OperationCanceledException)
            {
                _startupDiagnostics!.WriteInformation("Die Weiterleitung an die primäre MOPR-Instanz wurde beendet.");
                ShowForwardingFailedMessage();
            }
            catch (Exception exception)
            {
                _startupDiagnostics!.WriteError("Die Startanforderung konnte nicht an die primäre MOPR-Instanz übertragen werden.", exception);
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

                var arguments = request.Arguments.Length == 0 ? "keine" : string.Join(", ", request.Arguments);

                _startupDiagnostics!.WriteInformation($"Übertragene Startargumente: {arguments}");
            });
        }

        private static void ShowForwardingFailedMessage() => MessageBox.Show(WorkbenchResources.SingleInstanceForwardingFailedMessage, WorkbenchResources.SingleInstanceForwardingFailedTitle, MessageBoxButton.OK, MessageBoxImage.Information);

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