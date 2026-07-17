using MarcusRunge.Mopr.Workbench.Application;
using MarcusRunge.Mopr.Workbench.Contracts.Application;
using MarcusRunge.Mopr.Workbench.Modules.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
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
using System.Reactive.Subjects;
using System.Windows;

namespace MarcusRunge.Mopr.Workbench
{
    public partial class App
    {
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog) => moduleCatalog.AddModule<ImagingModule>();

        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (Container.Resolve<IApplicationLifetime>() is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            finally
            {
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
            
            containerRegistry.RegisterSingleton<IRepositoryFactory>(provider => new RepositoryFactory(provider.Resolve<IApplicationLifetime>(), provider.Resolve<IObservable<ApplicationConfiguration>>(), provider.Resolve<IPersistence>()));
            containerRegistry.RegisterSingleton<IRepository>(provider => provider.Resolve<IRepositoryFactory>().Create());

            containerRegistry.RegisterSingleton<IWpfFactory, WpfFactory>();
            containerRegistry.RegisterSingleton<IWpf>(provider => provider.Resolve<IWpfFactory>().Create());
        }
    }
}