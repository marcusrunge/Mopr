# MOPR
<p align="left">
  <img src="Wpf/MarcusRunge.Mopr.Workbench/Desktop/Assets/mopr.png" alt="MOPR Logo" width="300">
</p>

**Medical Observation & Projection Renderer**

MOPR is an experimental Windows workbench for importing, organizing, validating, viewing, and preparing DICOM CT and MRI data. The repository focuses on a modular WPF workbench, local service architecture, persistent metadata management, repository integrity, and medical-image visualization.

> **Important**
>
> MOPR is intended for visualization, research, prototyping, and educational use. It is not intended for diagnostic use, treatment planning, clinical reporting, or medical decision-making.

## Current Capabilities

The current implementation includes:

- DICOM grayscale image display
- grouping of image instances by `SeriesInstanceUID`
- assignment of image series to multiple viewports
- configurable viewport layouts and orientations
- selection and clearing of individual viewports
- window and level handling
- imaging tools and measurement models
- DICOM metadata, image-frame, image, and import services
- DICOM import and repository coordination
- Entity Framework Core persistence with SQL Server and in-memory test support
- persistence and repository integrity verification
- controlled repository repair operations
- MIRAS issue mapping, result classification, localized messages, and flow-state management
- machine, database, repository, and security configuration contracts
- protected machine-configuration storage
- Windows administrative authorization
- startup diagnostics and startup-route selection
- application-wide single-instance protection
- English and German localization
- isolated test projects for desktop, imaging, Core, MIRAS, persistence, and repository functionality

The DICOM import user interface, extended configuration interfaces, advanced measurements, volumetric reconstruction, segmentation, and Unreal-related workflows remain separate work packages or areas for future development.

## Architecture

MOPR separates shared contracts, reusable services, application composition, WPF modules, persistence, repository operations, and tests.

### Shared Contracts

`MarcusRunge.Mopr.Workbench.Contracts` targets `.NET Standard 2.1`. It contains technology-independent contracts, enumerations, configuration abstractions, and data models shared by projects targeting different .NET versions.

The contract project includes:

- application administration, configuration, and lifetime abstractions
- imaging layouts, tools, viewport state, and study-loading models
- geometry types
- measurement data models
- MIRAS contracts, states, issues, operation results, and user messages
- machine-configuration validation models
- Unreal object and mesh transfer models
- localized English and German resources

A small `IsExternalInit` compatibility type allows records and init-only properties to remain available while targeting `.NET Standard 2.1`.

### Core and MVVM Infrastructure

`MarcusRunge.Mopr.Workbench.Core` contains shared Prism and MVVM infrastructure, including:

- navigation-aware view-model base classes
- region-aware view-model base classes
- navigation confirmation support
- shared navigation names
- shared region names

### Desktop Application

The desktop project is the WPF and Prism composition root. It owns:

- application startup and shutdown
- dependency registration and application composition
- the main window and its view model
- application, database, repository, and security configuration
- machine-specific configuration loading, validation, path handling, and protection
- Windows administrator-role evaluation and administrative authorization
- startup diagnostics
- startup-route selection between setup and the regular workbench
- application lifetime coordination
- single-instance coordination and foreground activation
- application assets and localized resources

### Imaging Module

`MarcusRunge.Mopr.Workbench.Modules.Imaging` owns the image-viewing workspace and its WPF-specific behavior. It includes:

- the imaging workbench
- image viewer and command bar
- series and properties panels
- viewport layout host and viewport tiles
- measurement overlays and interaction state
- focus, mouse-selection, and viewport-interaction behaviors
- viewport image-geometry calculation
- module-specific services and localized resources

### Setup Module

`MarcusRunge.Mopr.Workbench.Modules.Setup` provides a guided, multi-step setup area with:

- repository, database, verification, and completion setup steps
- `SetupModule` and `SetupViewModel`
- dedicated views and code-behind files for each setup step
- shared setup control styles in `Themes/SetupControls.xaml`
- localized English and German resources

### Core Services

`MarcusRunge.Mopr.Workbench.Services.Core` provides application-wide service composition for:

- imaging coordination
- series and viewport selection
- imaging layouts and tools
- viewport state
- window and level changes
- study loading
- MIRAS application-flow coordination

The MIRAS Core flow is divided into:

- `IMirasApplicationService`
- `IMirasFlowService`
- `MirasApplicationService`
- `MirasFlowService`

`MirasFlowService` controls one application-level MIRAS check at a time. It tracks `Idle`, `Running`, `Completed`, `Canceled`, and `Failed`, supports cancellation and repeated runs, retains the most recent regular result, and prevents concurrent duplicate checks.

### DICOM Services

`MarcusRunge.Mopr.Workbench.Services.Dicom` provides:

- DICOM file metadata extraction
- grayscale image handling
- image-frame access
- DICOM import services
- series import results
- metadata and image service contracts
- service composition through `DicomFactory`

### MIRAS

MIRAS is the **Medical Image Recovery and Assurance System**.

`MarcusRunge.Mopr.Workbench.Services.Miras` is responsible for:

- orchestrating persistence and repository integrity checks
- mapping technical findings to MIRAS issues
- classifying operation results
- producing localized, user-oriented messages
- preserving technical diagnostics separately from user-facing text

MIRAS does not silently repair repository data and does not automatically create Unreal objects. Repair operations remain explicit, and Unreal artifacts may only be created by later workflows from validated DICOM and persistence data.

### Persistence

`MarcusRunge.Mopr.Workbench.Services.Persistence` contains:

- Entity Framework Core database context and context factories
- SQL Server and in-memory provider configuration
- database migrations
- DICOM import persistence
- entity repositories
- persistence-integrity verification
- serialization of measurement data
- repository-location records
- studies, series, instances, measurements, users, and Unreal-object persistence

The persistence layer includes auditable entities and dedicated Entity Framework configurations for the stored domain types.

### Repository

`MarcusRunge.Mopr.Workbench.Services.Repository` contains:

- DICOM import coordination
- repository scanning
- file identity and path verification
- repository issue detection
- controlled repair operations
- serialization of repository operations through a coordinator

Repository verification and repair remain separate operations. A MIRAS check reports findings and recommended actions but does not silently alter data.

### WPF Services

`MarcusRunge.Mopr.Workbench.Services.Wpf` contains reusable desktop services for:

- dialogs
- file dialogs
- image-source creation
- media handling
- WPF service composition through `WpfFactory`

## Project Structure

```text
Wpf/
├── Export-MoprSourceCode.ps1
├── MarcusRunge.Mopr.Workbench.slnx
└── MarcusRunge.Mopr.Workbench/
    ├── Contracts/
    │   ├── Application/
    │   │   ├── Administration/
    │   │   ├── Configuration/
    │   │   └── Lifetime/
    │   ├── Compatibility/
    │   ├── Enums/
    │   ├── Imaging/
    │   ├── Miras/
    │   │   ├── Enums/
    │   │   └── Models/
    │   ├── Models/
    │   │   ├── Configuration/
    │   │   ├── Geometry/
    │   │   ├── Measurements/
    │   │   └── Unreal/
    │   └── Properties/
    ├── Core/
    │   └── Mvvm/
    ├── Desktop/
    │   ├── Application/
    │   │   ├── Administration/
    │   │   ├── Configuration/
    │   │   ├── Diagnostics/
    │   │   ├── Lifetime/
    │   │   ├── SingleInstance/
    │   │   └── Startup/
    │   ├── Assets/
    │   ├── Properties/
    │   ├── ViewModels/
    │   └── Views/
    ├── Modules/
    │   ├── Imaging/
    │   │   ├── Behaviors/
    │   │   ├── Infrastructure/
    │   │   │   └── Viewports/
    │   │   ├── Properties/
    │   │   ├── Services/
    │   │   ├── ViewModels/
    │   │   └── Views/
    │   │       └── Viewports/
    │   └── Setup/
    │       ├── Properties/
    │       ├── Themes/
    │       ├── ViewModels/
    │       └── Views/
    ├── Services/
    │   ├── Core/
    │   │   ├── Bases/
    │   │   ├── Contracts/
    │   │   │   ├── Imaging/
    │   │   │   └── Miras/
    │   │   ├── Implementations/
    │   │   │   ├── Imaging/
    │   │   │   └── Miras/
    │   │   └── Properties/
    │   ├── Dicom/
    │   │   ├── Bases/
    │   │   ├── Contracts/
    │   │   ├── Implementations/
    │   │   └── Properties/
    │   ├── Miras/
    │   │   ├── Bases/
    │   │   ├── Contracts/
    │   │   ├── Implementations/
    │   │   └── Properties/
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
    │   │   └── Serialization/
    │   ├── Repository/
    │   │   ├── Bases/
    │   │   ├── Contracts/
    │   │   ├── Enums/
    │   │   ├── Implementations/
    │   │   ├── Models/
    │   │   └── Properties/
    │   └── Wpf/
    │       ├── Bases/
    │       ├── Contracts/
    │       │   ├── Dialog/
    │       │   └── Media/
    │       ├── Implementations/
    │       │   ├── Dialog/
    │       │   └── Media/
    │       └── Properties/
    └── Tests/
        ├── Desktop.Test/
        │   └── Application/
        │       ├── Administration/
        │       ├── Configuration/
        │       └── Startup/
        ├── Modules.Imaging.Test/
        │   └── ViewModels/
        ├── Services.Core.Test/
        ├── Services.Miras.Test/
        ├── Services.Persistence.Test/
        └── Services.Repository.Test/
```

The structure intentionally shows source-controlled project areas only. Build output, IDE state, generated compiler files, local exports, and user-specific project settings are omitted, including `bin`, `obj`, `.vs`, `ref`, `refint`, `*.g.cs`, `*.g.i.cs`, `*.cache`, and `*.csproj.user`.

## Dependency Direction

The principal dependency direction is:

```text
Desktop
├── Contracts
├── Core MVVM infrastructure
├── Modules.Imaging
├── Modules.Setup
├── Services.Core
├── Services.Dicom
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

The shared `Contracts.Miras.IMirasService` contract allows the `.NET Standard 2.1` Core flow to invoke MIRAS without referencing the `.NET 10` MIRAS implementation project directly.

## Module and Factory Pattern

Service assemblies expose a module instance through a factory:

```text
CoreFactory        → ICore
DicomFactory       → IDicom
MirasFactory       → IMiras
PersistenceFactory → IPersistence
RepositoryFactory  → IRepository
WpfFactory         → IWpf
```

Each factory retains one module instance per factory. The desktop application controls the overall lifetime through dependency injection.

Core composes the application-facing service groups:

```text
ICore
├── IImagingService
│   ├── IImagingLayoutService
│   ├── IImagingSelectionService
│   ├── IImagingStudyService
│   ├── IImagingToolService
│   ├── IImagingViewportSelectionService
│   ├── IImagingViewportService
│   └── IImagingWindowLevelService
└── IMirasApplicationService
    └── IMirasFlowService
```

The MIRAS implementation remains separate:

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

This means the application flow completed normally and MIRAS returned a blocking integrity result.

The flow guarantees:

- one active MIRAS check per Core module
- shared observation of an already running check
- cancellation by caller, user action, or application shutdown
- no synthetic result for cancellation or unexpected exceptions
- clearing `LastResult` when a new run starts
- restart after completion, cancellation, or an unexpected failure
- no restart after final application shutdown
- `INotifyPropertyChanged` notifications for UI binding

## Configuration and Administration

The application separates configuration contracts from Windows-specific implementations.

Configuration areas include:

- application configuration
- database configuration
- repository configuration
- security configuration
- machine-configuration storage
- machine-configuration path resolution
- machine-configuration protection
- machine-configuration validation
- repository-location validation before configuration is accepted

Administrative operations are protected through `IAdministrativeAuthorizationService`. The desktop implementation evaluates the current Windows administrator role through `IWindowsAdministratorRoleEvaluator`.

## Localization

MOPR uses English default resources and German satellite resources.

Localized enum values use the established localization infrastructure and resource files. Shared MIRAS enum descriptions, operation texts, and status texts are stored in:

```text
Contracts/Properties/Resources.resx
Contracts/Properties/Resources.de.resx
```

MIRAS implementation-specific issue descriptions are stored in:

```text
Services/Miras/Properties/Resources.resx
Services/Miras/Properties/Resources.de.resx
```

Other projects provide their own English and German resource files where project-specific text is required.

Technical identifiers, filesystem paths, stack traces, and raw exception details must not be copied into ordinary user-facing messages.

## Build

### Requirements

- Windows 10 or Windows 11
- .NET 10 SDK
- Visual Studio with the **.NET desktop development** workload
- SQL Server LocalDB for the default desktop persistence configuration

### Build the Solution

Run from the directory containing `MarcusRunge.Mopr.Workbench.slnx`:

```powershell
dotnet build .\MarcusRunge.Mopr.Workbench.slnx --configuration Debug
```

### Run the Desktop Application

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
- Windows administrative authorization
- application, machine, and repository-location configuration validation
- startup-route selection
- imaging workbench view-model behavior
- Core MIRAS flow state, concurrency, cancellation, restart, and shutdown behavior
- MIRAS result mapping, localization, and edge cases
- persistence integration and integrity verification
- repository integration, repair, and operation coordination

Some integration tests may require local infrastructure or configuration that is not needed by unit tests.

## Safety and Privacy

MOPR is designed for local processing. Cloud-based AI or machine-learning services are not part of the intended architecture.

DICOM data and derived medical-imaging information should remain under the control of the local deployment. Deployments must apply appropriate access controls, storage protection, backup policies, and applicable data-protection requirements.

Repository repair is explicit and separate from integrity inspection. MIRAS reports findings and recommended actions but does not silently alter data during a check.

## License

See [LICENSE](LICENSE).
