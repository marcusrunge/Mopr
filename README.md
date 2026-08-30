# MOPR

**Medical Observation & Projection Renderer**

MOPR is an experimental Windows workbench for importing, organizing, validating, viewing, and preparing DICOM CT and MRI data. The current repository focuses on the WPF workbench and its local service architecture. Medical image processing and integrity checks remain local to the workstation.

> **Important**
>
> MOPR is intended for visualization, research, prototyping, and educational use. It is not intended for diagnostic use, treatment planning, clinical reporting, or medical decision-making.

## Current Capabilities

The current implementation includes:

- DICOM grayscale image display
- grouping by `SeriesInstanceUID`
- assignment of image series to multiple viewports
- clearing individual viewports
- window and level presets
- DICOM import and repository services
- Entity Framework Core persistence with SQL Server and in-memory test support
- persistence and repository integrity verification
- MIRAS issue mapping, localized user messages, result classification, and flow-state management
- application-wide single-instance protection
- English and German localization
- isolated test projects for imaging, persistence, repository, MIRAS, Core services, and desktop startup infrastructure

Interactive mouse windowing, the DICOM import user interface, application and database configuration interfaces, and later volumetric or Unreal-related workflows remain separate work packages.

## Architecture

MOPR separates shared contracts, reusable services, application composition, WPF modules, and tests.

### Shared Contracts

`MarcusRunge.Mopr.Workbench.Contracts` targets `.NET Standard 2.1`. It contains technology-independent contracts and data models that must be shared by service projects targeting different .NET versions.

The shared MIRAS contract area contains:

- `IMirasService`
- MIRAS enums
- `MirasIssue`
- `MirasOperationResult`
- `MirasUserMessage`

The project uses a small `IsExternalInit` compatibility type so records and init-only properties can remain available while targeting `.NET Standard 2.1`.

### Core Services

`MarcusRunge.Mopr.Workbench.Services.Core` also targets `.NET Standard 2.1`. It provides application-wide service composition for:

- imaging coordination
- MIRAS application-flow coordination

The MIRAS Core service is divided into:

- `IMirasApplicationService`
- `IMirasFlowService`
- `MirasApplicationService`
- `MirasFlowService`

`MirasFlowService` controls one application-level MIRAS check at a time. It tracks `Idle`, `Running`, `Completed`, `Canceled`, and `Failed`, supports cancellation and repeated runs, retains the most recent regular result, and prevents concurrent duplicate checks.

### MIRAS

MIRAS is the Medical Image Recovery and Assurance System. The `MarcusRunge.Mopr.Workbench.Services.Miras` project targets `.NET 10` because its implementation depends on the `.NET 10` persistence and repository services.

Responsibilities include:

- orchestrating persistence and repository integrity checks
- mapping technical persistence and repository findings to MIRAS issues
- classifying operation results
- producing localized, user-oriented messages
- preserving technical diagnostics separately from user-facing text

MIRAS does not automatically create Unreal objects. Unreal artifacts may only be created later from fully validated DICOM and persistence data.

### Persistence

`MarcusRunge.Mopr.Workbench.Services.Persistence` targets `.NET 10` and contains:

- Entity Framework Core database context and factory
- SQL Server and in-memory provider configuration
- database migrations
- DICOM import persistence
- entity repositories
- persistence-integrity verification
- repository-location records
- studies, series, instances, measurements, users, and Unreal-object persistence

Persistence exposes an observable initialization task so application services can wait for the currently published configuration before accessing dependent data.

### Repository

`MarcusRunge.Mopr.Workbench.Services.Repository` targets `.NET 10` and contains:

- DICOM import coordination
- repository scanning
- file identity and path verification
- repository issue detection
- controlled repair operations
- serialization of repository operations

Repository verification and repair remain separate operations. A MIRAS check does not silently perform a repair.

### Desktop and Modules

The desktop project is the Prism and WPF composition root. It owns:

- application startup and shutdown
- dependency registration
- application configuration
- diagnostics
- application lifetime
- single-instance coordination
- the main window

The imaging module owns its WPF views, view models, interaction behaviors, viewport infrastructure, and module-specific services.

## Project Structure

```text
MOPR/
├── .gitignore
├── LICENSE
├── README.md
└── Wpf/
    ├── Export-MoprSourceCode.ps1
    ├── MarcusRunge.Mopr.Workbench.slnx
    └── MarcusRunge.Mopr.Workbench/
        ├── Contracts/
        │   ├── Application/
        │   │   ├── Configuration/
        │   │   └── Lifetime/
        │   ├── Compatibility/
        │   │   └── IsExternalInit.cs
        │   ├── Enums/
        │   ├── Imaging/
        │   ├── Miras/
        │   │   ├── IMirasService.cs
        │   │   ├── Enums/
        │   │   │   ├── MirasAlertLevel.cs
        │   │   │   ├── MirasFlowState.cs
        │   │   │   ├── MirasIssueState.cs
        │   │   │   ├── MirasIssueType.cs
        │   │   │   ├── MirasOperationStatus.cs
        │   │   │   └── MirasRecommendedAction.cs
        │   │   └── Models/
        │   │       ├── MirasIssue.cs
        │   │       ├── MirasOperationResult.cs
        │   │       └── MirasUserMessage.cs
        │   ├── Models/
        │   │   ├── Geometry/
        │   │   ├── Measurements/
        │   │   └── Unreal/
        │   └── Properties/
        │
        ├── Core/
        │   ├── Mvvm/
        │   └── RegionNames.cs
        │
        ├── Desktop/
        │   ├── Application/
        │   │   ├── Configuration/
        │   │   ├── Diagnostics/
        │   │   ├── Lifetime/
        │   │   └── SingleInstance/
        │   ├── Assets/
        │   ├── Properties/
        │   ├── ViewModels/
        │   ├── Views/
        │   ├── App.xaml
        │   └── App.xaml.cs
        │
        ├── Modules/
        │   └── Imaging/
        │       ├── Behaviors/
        │       ├── Infrastructure/
        │       │   └── Viewports/
        │       ├── Properties/
        │       ├── Services/
        │       ├── ViewModels/
        │       ├── Views/
        │       │   └── Viewports/
        │       └── ImagingModule.cs
        │
        ├── Services/
        │   ├── Core/
        │   │   ├── Bases/
        │   │   │   ├── CoreBase.cs
        │   │   │   ├── ImagingServiceBase.cs
        │   │   │   └── MirasApplicationServiceBase.cs
        │   │   ├── Contracts/
        │   │   │   ├── Imaging/
        │   │   │   ├── Miras/
        │   │   │   │   └── IMirasFlowService.cs
        │   │   │   ├── ICore.cs
        │   │   │   ├── ICoreBase.cs
        │   │   │   ├── IImagingService.cs
        │   │   │   ├── IImagingServiceBase.cs
        │   │   │   ├── IMirasApplicationService.cs
        │   │   │   └── IMirasApplicationServiceBase.cs
        │   │   ├── Implementations/
        │   │   │   ├── Imaging/
        │   │   │   ├── Miras/
        │   │   │   │   └── MirasFlowService.cs
        │   │   │   ├── Core.cs
        │   │   │   ├── ImagingService.cs
        │   │   │   └── MirasApplicationService.cs
        │   │   ├── Properties/
        │   │   └── CoreFactory.cs
        │   │
        │   ├── Dicom/
        │   │   ├── Bases/
        │   │   ├── Contracts/
        │   │   ├── Implementations/
        │   │   ├── Properties/
        │   │   └── DicomFactory.cs
        │   │
        │   ├── Miras/
        │   │   ├── Bases/
        │   │   ├── Contracts/
        │   │   │   ├── IMiras.cs
        │   │   │   └── IMirasBase.cs
        │   │   ├── Implementations/
        │   │   │   ├── Miras.cs
        │   │   │   └── MirasService.cs
        │   │   ├── Properties/
        │   │   └── MirasFactory.cs
        │   │
        │   ├── Persistence/
        │   │   ├── Bases/
        │   │   ├── Configurations/
        │   │   ├── Contexts/
        │   │   ├── Contracts/
        │   │   ├── Entities/
        │   │   ├── Enums/
        │   │   ├── Implementations/
        │   │   ├── Migrations/
        │   │   ├── Models/
        │   │   ├── Properties/
        │   │   ├── Serialization/
        │   │   ├── PersistenceDbContextFactory.cs
        │   │   └── PersistenceFactory.cs
        │   │
        │   ├── Repository/
        │   │   ├── Bases/
        │   │   ├── Contracts/
        │   │   ├── Enums/
        │   │   ├── Implementations/
        │   │   ├── Models/
        │   │   ├── Properties/
        │   │   └── RepositoryFactory.cs
        │   │
        │   └── Wpf/
        │       ├── Bases/
        │       ├── Contracts/
        │       │   ├── Dialog/
        │       │   └── Media/
        │       ├── Implementations/
        │       │   ├── Dialog/
        │       │   └── Media/
        │       ├── Properties/
        │       └── WpfFactory.cs
        │
        └── Tests/
            ├── Desktop.Test/
            ├── Modules.Imaging.Test/
            ├── Services.Core.Test/
            ├── Services.Miras.Test/
            ├── Services.Persistence.Test/
            └── Services.Repository.Test/
```

Generated `bin` and `obj` directories are intentionally omitted.

## Dependency Direction

The principal dependency direction is:

```text
Desktop
├── Core MVVM infrastructure
├── Imaging module
├── Services.Core
├── Services.Miras
├── Services.Persistence
├── Services.Repository
└── Services.Wpf

Services.Core (.NET Standard 2.1)
├── Contracts (.NET Standard 2.1)
└── Services.Dicom (.NET Standard 2.1)

Services.Miras (.NET 10)
├── Contracts (.NET Standard 2.1)
├── Services.Persistence (.NET 10)
└── Services.Repository (.NET 10)

Services.Repository (.NET 10)
├── Contracts (.NET Standard 2.1)
└── Services.Persistence (.NET 10)

Services.Persistence (.NET 10)
└── Contracts (.NET Standard 2.1)
```

The shared `Contracts.Miras.IMirasService` contract allows the `.NET Standard 2.1` Core flow to invoke MIRAS without referencing the `.NET 10` MIRAS implementation project.

## Module and Factory Pattern

Service assemblies expose one module instance through a factory:

```text
CoreFactory        -> ICore
DicomFactory       -> IDicom
MirasFactory       -> IMiras
PersistenceFactory -> IPersistence
RepositoryFactory  -> IRepository
WpfFactory         -> IWpf
```

Each factory retains one module instance per factory. The desktop application controls the overall lifetime through dependency injection.

Core composes two service groups:

```text
ICore
├── IImagingService
└── IMirasApplicationService
    └── IMirasFlowService
```

The MIRAS implementation module remains separate:

```text
IMiras
└── IMirasService
    └── CheckRepositoryAsync(...)
```

## MIRAS Result and Flow Semantics

`MirasFlowState` describes execution of the application-level flow:

```text
Idle
Running
Completed
Canceled
Failed
```

`MirasOperationStatus` describes the regular result returned by a MIRAS check:

```text
Completed
CompletedWithIssues
Blocked
Incomplete
Failed
```

These values intentionally describe different concerns. For example:

```text
MirasFlowState.Completed
MirasOperationStatus.Blocked
```

means that the flow completed normally and MIRAS returned a blocking integrity result.

The flow guarantees:

- one active MIRAS check per Core module
- shared observation of an already running check
- cancellation by caller, user action, or application shutdown
- no synthetic result for cancellation or unexpected exceptions
- clearing `LastResult` when a new run starts
- restart after completion, cancellation, or an unexpected failure
- no restart after final application shutdown
- `INotifyPropertyChanged` notifications for later UI binding

## Localization

MOPR uses English default resources and German satellite resources.

Each localized enum uses:

```csharp
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
```

Each enum value uses:

```csharp
[LocalizedDescription("ResourceKey", typeof(Resources))]
```

Shared MIRAS enum descriptions, operation texts, and status texts are stored in:

```text
Contracts/Properties/Resources.resx
Contracts/Properties/Resources.de.resx
```

MIRAS implementation-specific issue descriptions are stored in:

```text
Services/Miras/Properties/Resources.resx
Services/Miras/Properties/Resources.de.resx
```

Technical identifiers, paths, stack traces, and raw exception details must not be copied into ordinary user-facing messages.

## Build

### Requirements

- Windows 10 or Windows 11
- .NET 10 SDK
- Visual Studio with .NET desktop development support
- SQL Server LocalDB for the default desktop persistence configuration

### Build the solution

```powershell
dotnet build .\MarcusRunge.Mopr.Workbench.slnx --configuration Debug
```

### Run the desktop application

```powershell
dotnet run --project .\MarcusRunge.Mopr.Workbench\Desktop\MarcusRunge.Mopr.Workbench.csproj --configuration Debug
```

## Tests

Run all tests:

```powershell
dotnet test .\MarcusRunge.Mopr.Workbench.slnx --configuration Debug
```

Run individual test projects:

```powershell
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Desktop.Test\MarcusRunge.Mopr.Workbench.Test.csproj --configuration Debug

dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Modules.Imaging.Test\MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.csproj --configuration Debug

dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Services.Core.Test\MarcusRunge.Mopr.Workbench.Services.Core.Test.csproj --configuration Debug

dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Services.Miras.Test\MarcusRunge.Mopr.Workbench.Services.Miras.Test.csproj --configuration Debug

dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Services.Persistence.Test\MarcusRunge.Mopr.Workbench.Services.Persistence.Test.csproj --configuration Debug

dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Services.Repository.Test\MarcusRunge.Mopr.Workbench.Services.Repository.Test.csproj --configuration Debug
```

The test suites cover:

- single-instance application behavior
- imaging view-model behavior
- Core MIRAS flow state, concurrency, cancellation, restart, and shutdown behavior
- MIRAS result mapping and localization
- Persistence integration and integrity verification
- Repository integration, coordination, and repair behavior

## Safety and Privacy

MOPR is designed for local processing. Cloud-based AI or machine-learning services are not part of the intended architecture. DICOM data and derived medical imaging information should remain under the control of the local deployment.

Repository repair is explicit and separate from integrity inspection. MIRAS reports findings and recommended actions but does not silently alter data during a check.

## License

See [LICENSE](LICENSE).
