# MOPR

<p align="left">
  <img src="Wpf/MarcusRunge.Mopr.Workbench/Desktop/Assets/mopr.png" alt="MOPR Logo" width="300">
</p>

**Medical Observation & Projection Renderer**

MOPR is an experimental Windows workbench for importing, organizing, validating, viewing, and preparing DICOM CT and MRI data. Its purpose is to provide a modular, locally operated foundation for medical-image workflows while keeping machine configuration, user identity, repository integrity, persistence, and imaging responsibilities clearly separated.

The current solution combines a Prism-based WPF desktop shell with dedicated feature modules, reusable application and domain services, protected machine-wide configuration, local user provisioning and sign-in, persistent DICOM metadata, managed repository operations, and integrity assurance through MIRAS.

> **Important**
>
> MOPR is intended for visualization, research, prototyping, and educational use. It is not intended for diagnostic use, treatment planning, clinical reporting, or medical decision-making.

> **Documentation snapshot:** 2026-11-16  
> This README reflects the current source export, solution structure, completed DICOM import-source work, and verified green test state available on that date.

## Purpose and Context

MOPR is designed as a local-first medical-imaging workbench for environments in which DICOM data, configuration, identities, and derived information must remain under the control of the local deployment.

The architecture follows these principles:

- local processing instead of cloud-dependent medical-image workflows
- explicit separation between UI modules, application orchestration, persistence, repository storage, DICOM processing, and integrity assurance
- guided setup and identity flows for users without specialist IT knowledge
- protected machine-wide configuration and explicit administrative authorization
- auditable operations with a resolved current user or setup identity
- explicit verification and repair instead of silent data modification
- reusable contracts across projects targeting different .NET platforms
- English default resources and German localization owned by the responsible project
- extensibility for later measurements, reconstruction, segmentation, and Unreal-based workflows without coupling them to current integrity or import operations

## Current Capabilities

The current implementation includes:

- DICOM grayscale image display, frame access, metadata extraction, folder scanning, and series loading
- grouping of image instances by `SeriesInstanceUID`
- assignment of image series to multiple viewports
- single, 2 x 2, MPR, and axial/sagittal/coronal viewport layouts
- selection and clearing of individual viewports
- window and level presets plus interactive mouse dragging
- pixel inspection and measurement overlays
- a dedicated import module with local-folder selection, progressive asynchronous drive discovery, direct UNC network-path validation, cancellation, and structured result presentation
- responsive import-source discovery that displays each drive as soon as it is inspected and keeps slow or unavailable network, optical, and removable drives off the WPF dispatcher
- import-source resolution for local folders, removable media, optical media, mounted virtual drives, mapped drives, and UNC network shares
- transparent ISO handling: ISO files are recognized but must currently be mounted in Windows and selected as a virtual drive before import
- application-level DICOM import coordination with current-user audit identity resolution
- repository-level DICOM import with persistence integration, rollback, and file-system compensation
- Entity Framework Core persistence with SQL Server and in-memory test support
- persistence and repository integrity verification
- controlled repository and infrastructure recovery operations
- MIRAS issue mapping, result classification, localized messages, flow-state management, and protected startup integration
- protected machine-wide database, repository, and security configuration
- guided initial setup with database, repository, verification, and completion steps
- an elevated administrative mode for database, DICOM repository, security, and user management
- read-only workstation system checks for database, persistence, and repository/MIRAS state
- repository-location creation, editing, validation, default selection, optimistic concurrency, and protected deletion
- persistent Windows-SID-based user administration with activation and protected system/current-user rules
- operating-system identity discovery, initial provisioning, local sign-in, and current-user context management
- startup routing for machine setup, user provisioning, blocked users, unavailable identity, MIRAS action requirements, and the imaging workbench
- application-wide single-instance protection, request forwarding, and foreground activation
- English default resources and German localization
- isolated test projects for desktop, identity, imaging, import, MIRAS, setup, studies, application services, Core, persistence, and repository functionality

Persistable imaging-session change management for measurements, reports, findings, annotations, segmentations, and later 3D/Unreal artifacts remains a separate work package. Advanced volumetric reconstruction, segmentation, local machine-learning workflows, and Unreal-based artifact generation also remain separate later work packages. Unreal artifacts are not part of repository or MIRAS integrity processing and may only be created from validated DICOM and persistence data.

## Architecture

MOPR separates shared contracts, WPF infrastructure, desktop composition, feature modules, reusable services, persistence, repository operations, integrity assurance, and tests.

### Shared Contracts

`MarcusRunge.Mopr.Workbench.Contracts` targets `.NET Standard 2.1`. It contains technology-independent contracts and data models shared by projects targeting different .NET versions.

The contract project includes:

- application administration, configuration, identity, lifetime, and security abstractions
- current-user, operating-system identity, provisioning, and sign-in models
- imaging layouts, tools, viewport state, and study-loading models
- geometry and measurement models
- machine-configuration validation and setup-completion models
- Unreal object and mesh transfer models
- localized English and German resources

A small `IsExternalInit` compatibility type allows records and init-only properties to remain available while targeting `.NET Standard 2.1`.

### Core WPF and MVVM Infrastructure

`MarcusRunge.Mopr.Workbench.Core` targets `.NET 11 for Windows` and contains shared Prism and WPF infrastructure, including:

- navigation-aware view-model base classes
- region-aware view-model base classes
- navigation confirmation support
- shared navigation names
- shared region names
- application-wide Prism events for completed machine setup and initial user provisioning

### Desktop Application

The desktop project is the WPF and Prism composition root. It owns:

- application startup and shutdown
- dependency registration and module composition
- the main window and its view model
- machine-wide application, database, repository, and security configuration
- configuration loading, validation, path handling, DPAPI protection, and access control
- Windows administrator-role evaluation and administrative authorization
- Windows identity access and operating-system identity mapping
- setup audit identity resolution
- startup diagnostics and machine-level startup-route selection
- user-level startup-route selection
- application lifetime coordination
- single-instance coordination, request forwarding, and foreground activation
- application assets and localized resources

The desktop shell remains the composition root. Feature-specific UI and workflow behavior belongs to Prism modules rather than the desktop project.

### Identity Module

`MarcusRunge.Mopr.Workbench.Modules.Identity` provides the user-facing identity and initial provisioning flow. It includes:

- initial user provisioning
- presentation of blocked-user state
- presentation of unavailable operating-system identity
- Prism registration and navigation
- shared identity control styles
- localized English and German resources

The module consumes identity services from `Services.Application`. It does not directly access Windows identity APIs or persistence repositories.

### Imaging Module

`MarcusRunge.Mopr.Workbench.Modules.Imaging` owns the image-viewing workspace and its WPF-specific behavior. It includes:

- the imaging workbench
- image viewer and command bar
- series and properties panels
- viewport layout host and viewport tiles
- measurement overlays and interaction state
- focus, mouse-selection, pixel-hover, measurement, and viewport-interaction behaviors
- viewport image-geometry calculation
- module-specific services and localized resources

### Import Module

`MarcusRunge.Mopr.Workbench.Modules.Import` provides the guided DICOM import user interface. It includes:

- local-folder selection through the application file-dialog abstraction
- progressive asynchronous enumeration of local, removable, optical, mapped-network, and virtual drives
- immediate display of each inspected drive through an observable collection while slower sources continue loading
- visible search state, refresh cancellation, readiness status, and disabled selection for unavailable media
- direct UNC network-path entry and validation through `IImportService.ResolveSourceAsync(...)`
- import execution, progress reporting, cancellation, and structured result presentation
- explicit distinction between new imports, existing files, other skipped files, invalid input, cancellation, and failed operations
- guidance that ISO images must currently be mounted in Windows and selected as virtual drives
- Prism module registration and localized English and German resources

The module consumes the `IApplication → IImportService` facade. It does not query `DriveInfo`, validate DICOM content, mount ISO images, establish network connections, access persistence, or write repository files directly.

### Studies Module

`MarcusRunge.Mopr.Workbench.Modules.Studies` provides the study and series selection dialog used to reopen imported examinations. It includes:

- study catalog presentation
- series selection within one study
- validation that selected series belong to the same study
- localized English and German resources
- isolated view-model tests

The module consumes application-level study catalog and imported-series loading contracts rather than reading persistence or repository data directly.

### Setup Module

`MarcusRunge.Mopr.Workbench.Modules.Setup` supports two explicit operating modes:

- `InitialSetup` for first-time workstation configuration
- `Administration` for later protected maintenance

The initial setup flow includes:

- database configuration and connection verification
- repository-location selection and validation
- verification and controlled setup completion
- setup compensation when durable changes cannot be completed

The administration area includes:

- database administration with a required successful connection test before saving
- DICOM repository-location administration with validation, default-location invariants, optimistic concurrency, and controlled deletion
- security configuration for self-modification, self-deletion, and visibility of other users
- persistent user administration based on Windows login names and security identifiers
- a read-only system check for database, persistence, and repository/MIRAS state

Administrative entry requires an active MOPR user and an elevated Windows administrator token. Application services repeat authorization checks for privileged operations. Longer-running operations use `CancellationToken`; database and repository operations as well as the system check provide user cancellation where applicable.

The module owns its views, view models, shared setup styles, and English/German resources. View code-behind remains limited to constructors and `InitializeComponent()`. Machine setup and initial user provisioning remain separate responsibilities.

### Application Services

`MarcusRunge.Mopr.Workbench.Services.Application` targets `.NET 11 for Windows` and contains desktop-bound application workflows for:

- dialogs, file dialogs, image sources, and media handling
- current-user context management, user provisioning, and local sign-in
- mapping the current user to a persistent audit identity
- DICOM import orchestration, asynchronous drive discovery, source classification, UNC validation, and structured import-result mapping
- repository-location administration and explicit recovery actions
- persistent user administration
- read-only workstation system checks

The repository-administration service lists, validates, creates, updates, and deletes repository locations while preserving default-location, in-use, authorization, audit, and row-version invariants. The user-administration service protects the technical system identity and the current user while returning structured outcomes for conflicts and validation failures. The system-check service aggregates database, persistence, and repository/MIRAS checks without performing automatic repair.

The assembly exposes its service groups through `ApplicationFactory` and `IApplication`. Presentation modules consume these application-level contracts instead of directly implementing persistence, repository, authorization, or repair logic.

### Core Services

`MarcusRunge.Mopr.Workbench.Services.Core` targets `.NET Standard 2.1` and provides application-wide imaging coordination for:

- series and viewport selection
- imaging layouts and tools
- viewport state
- window and level changes
- study loading

The assembly exposes these services through `CoreFactory`, `ICore`, and `IImagingService`.

### DICOM Services

`MarcusRunge.Mopr.Workbench.Services.Dicom` targets `.NET Standard 2.1` and provides:

- DICOM file metadata extraction
- grayscale image creation
- image-frame access
- DICOM folder scanning and import results
- metadata and image service contracts
- service composition through `DicomFactory`

This assembly owns DICOM parsing and image decoding. It does not own managed repository placement or persistence transactions.

### MIRAS

MIRAS is the **Medical Image Recovery and Assurance System**.

`MarcusRunge.Mopr.Workbench.Services.Miras` targets `.NET 11` and is responsible for:

- orchestrating persistence and repository integrity checks
- mapping technical findings to MIRAS issues
- classifying operation results
- producing localized, user-oriented messages
- preserving technical diagnostics separately from user-facing text
- coordinating one application-level integrity flow at a time
- exposing the current flow state and most recent regular result

MIRAS uses `IFlow` for execution-state coordination and `IOperations` for integrity operations. Flow states and result models are owned by the MIRAS service assembly.

MIRAS does not silently repair repository data and does not automatically create Unreal objects. Repair operations remain explicit, and Unreal artifacts may only be created by later workflows from validated DICOM and persistence data.

### Persistence

`MarcusRunge.Mopr.Workbench.Services.Persistence` targets `.NET 11` and contains:

- Entity Framework Core database contexts and context factories
- SQL Server and in-memory provider configuration
- reactive persistence configuration and asynchronous initialization
- Entity Framework Core migrations and model snapshots
- DICOM import persistence
- entity repositories
- user persistence and activation state
- persistence-integrity verification
- database connection testing
- serialization of measurement data
- repository-location records
- studies, series, instances, measurements, users, and Unreal-object persistence

The persistence layer includes auditable entities and dedicated Entity Framework configurations for the stored domain types.

### Repository

`MarcusRunge.Mopr.Workbench.Services.Repository` targets `.NET 11` and contains:

- managed DICOM import and file placement
- repository scanning across configured locations
- file identity and path verification
- repository issue detection
- controlled repair operations
- compensation and rollback for incomplete imports
- serialization of repository operations through a coordinator

Repository verification and repair remain separate operations. A MIRAS check reports findings and recommended actions but does not silently alter data.

## Project Structure

The following tree summarizes the current source-controlled solution areas. Build output, IDE state, generated compiler files, temporary files, local exports, backup files, and user-specific project settings are intentionally omitted.

```text
Wpf/
├── Clean-BuildArtifacts.ps1
├── Export-MoprSourceCode.ps1
├── MarcusRunge.Mopr.Workbench.slnx
└── MarcusRunge.Mopr.Workbench/
    ├── Contracts/
    │   ├── Application/{Administration,Configuration,Identity,Lifetime,Security}/
    │   ├── Compatibility/
    │   ├── Enums/
    │   ├── Imaging/
    │   ├── Models/{Configuration,Geometry,Measurements,Unreal}/
    │   └── Properties/
    ├── Core/{Events,Mvvm,Themes}/
    ├── Desktop/
    │   ├── Application/{Administration,Configuration,Diagnostics,Identity,Lifetime,SingleInstance,Startup}/
    │   ├── Assets/
    │   ├── Behaviors/
    │   ├── Properties/
    │   ├── ViewModels/
    │   └── Views/
    ├── Modules/
    │   ├── Identity/{Properties,Themes,ViewModels,Views}/
    │   ├── Imaging/{Behaviors,Infrastructure,Properties,Services,ViewModels,Views}/
    │   ├── Import/{Properties,ViewModels,Views}/
    │   ├── Miras/{Properties,ViewModels,Views}/
    │   ├── Setup/{Converters,Properties,Themes,ViewModels,Views}/
    │   └── Studies/{Properties,ViewModels,Views}/
    ├── Services/
    │   ├── Application/{Bases,Contracts,Enums,Implementations,Models,Properties}/
    │   ├── Core/{Bases,Contracts,Implementations,Properties}/
    │   ├── Dicom/{Bases,Contracts,Implementations,Models,Properties}/
    │   ├── Miras/{Bases,Contracts,Enums,Implementations,Models,Properties}/
    │   ├── Persistence/{Bases,Configurations,Contexts,Contracts,Entities,Enums,Implementations,Migrations,Models,Properties,Serialization}/
    │   └── Repository/{Bases,Contracts,Enums,Implementations,Models,Properties}/
    └── Tests/
        ├── Desktop.Test/Application/{Administration,Configuration,Identity,Startup}/
        ├── Modules.Identity.Test/ViewModels/
        ├── Modules.Imaging.Test/ViewModels/
        ├── Modules.Import.Test/ViewModels/
        ├── Modules.Miras.Test/ViewModels/
        ├── Modules.Setup.Test/ViewModels/
        ├── Modules.Studies.Test/ViewModels/
        ├── Services.Application.Test/{Identity,Import,RepositoryAdministration,RepositoryRecovery,Studies,SystemCheck,UserAdministration}/
        ├── Services.Core.Test/
        ├── Services.Miras.Test/
        ├── Services.Persistence.Test/
        ├── Services.Repository.Test/
        └── Test.Shared/Application/
```

The solution currently contains **28 projects**: three root projects, six feature modules, six service projects, and thirteen test/support projects.

Excluded content includes `bin`, `obj`, `.vs`, `TestResults`, `artifacts`, `ref`, `refint`, generated `*.g.cs` and `*.g.i.cs` files, caches, temporary and backup files, `*.csproj.user`, local source exports, and local repair scripts.

## Dependency Direction

The principal dependency direction is:

```text
Desktop (.NET 11 for Windows)
├── Contracts (.NET Standard 2.1)
├── Core (.NET 11 for Windows)
├── Modules.Identity (.NET 11 for Windows)
├── Modules.Imaging (.NET 11 for Windows)
├── Modules.Import (.NET 11 for Windows)
├── Modules.Setup (.NET 11 for Windows)
├── Modules.Studies (.NET 11 for Windows)
├── Services.Application (.NET 11 for Windows)
├── Services.Core (.NET Standard 2.1)
├── Services.Dicom (.NET Standard 2.1)
├── Services.Miras (.NET 11)
├── Services.Persistence (.NET 11)
└── Services.Repository (.NET 11)

Modules.Identity
├── Contracts
├── Core
└── Services.Application

Modules.Imaging
├── Core
├── Services.Application
└── Services.Core

Modules.Import
├── Core
└── Services.Application

Modules.Setup
├── Contracts
├── Core
└── Services.Application

Modules.Studies
├── Core
└── Services.Application

Services.Application
├── Contracts
├── Services.Dicom
├── Services.Miras
├── Services.Persistence
└── Services.Repository

Services.Core
├── Contracts
└── Services.Dicom

Services.Miras
├── Contracts
├── Services.Persistence
└── Services.Repository

Services.Repository
├── Contracts
└── Services.Persistence

Services.Persistence
└── Contracts
```

The `.NET Standard 2.1` boundary keeps shared contracts, DICOM processing, and Core imaging coordination reusable without introducing WPF, SQL Server, or other Windows-specific dependencies.

## Module and Factory Pattern

Service assemblies expose a module instance through a factory:

```text
ApplicationFactory → IApplication
CoreFactory        → ICore
DicomFactory       → IDicom
MirasFactory       → IMiras
PersistenceFactory → IPersistence
RepositoryFactory  → IRepository
```

Each factory retains one module instance per factory. The desktop application controls the overall lifetime through dependency injection.

Application services compose desktop-bound workflows:

```text
IApplication
├── IDialogService
├── IIdentityService
│   ├── ICurrentUserContextManager
│   ├── IUserProvisioningService
│   └── IUserSignInService
├── IImportService
│   ├── IDicomImportDriveProvider
│   ├── IDicomImportService
│   └── ResolveSourceAsync(...)
├── IMediaService
├── IRepositoryLocationAdministrationService
├── IRepositoryRecoveryActionService
├── ISystemCheckService
└── IUserAdministrationService
```

Core composes the imaging service group:

```text
ICore
└── IImagingService
    ├── IImagingLayoutService
    ├── IImagingSelectionService
    ├── IImagingStudyService
    ├── IImagingToolService
    ├── IImagingViewportSelectionService
    ├── IImagingViewportService
    └── IImagingWindowLevelService
```

MIRAS composes integrity execution and flow coordination:

```text
IMiras
├── IFlow
└── IOperations
    └── CheckRepositoryAsync(...)
```

## Startup, Setup, and Identity Flow

Startup routing separates machine readiness from user readiness:

```text
Application start
    ↓ machine configuration and startup diagnostics
Machine setup required?
    ├── yes → Modules.Setup
    └── no  → resolve operating-system identity and application user
                 ├── identity unavailable → IdentityUnavailableView
                 ├── no application user  → UserProvisioningView
                 ├── user blocked         → UserBlockedView
                 └── active user          → regular workbench
```

The machine-setup flow configures and verifies the repository, database, security settings, and completion state. The identity flow then provisions the first application user when required, signs in a valid local user, and establishes the current-user context used by application services and audit identity resolution.

`MachineSetupCompletedEvent` and `InitialUserProvisioningCompletedEvent` allow the relevant modules and the desktop shell to continue routing without coupling feature view models directly to one another.

After regular startup, an active user running with elevated Windows administrator rights can open the same Setup module in `Administration` mode. The mode is passed explicitly through the Prism navigation parameter and is not inferred from setup completion or the selected view.

## Identity and Audit Semantics

MOPR distinguishes between four related concepts:

- the Windows identity reported by the operating system
- the persisted MOPR user record
- the current signed-in MOPR user context
- the audit identity attached to application and repository operations

`IOperatingSystemIdentityProvider` supplies normalized operating-system identity information. `IUserProvisioningService` validates and creates the corresponding MOPR user when permitted. `IUserSignInService` resolves sign-in state, while `ICurrentUserContextManager` owns the active application-user context. `CurrentUserAuditIdentityProvider` maps that context to `IAuditIdentityProvider` for auditable operations.

Setup remains usable before a regular application user exists by using the separate setup audit identity provider.

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

- one active MIRAS check per MIRAS module
- shared observation of an already running check
- cancellation by caller, user action, or application shutdown
- no synthetic result for cancellation or unexpected exceptions
- clearing the previous result when a new run starts
- restart after completion, cancellation, or an unexpected failure
- no restart after final application shutdown
- property-change notifications for UI binding

## DICOM Import Flow

The import path is deliberately separated into distinct responsibilities:

```text
Modules.Import
    ↓ local folder, discovered drive, or validated UNC source
Services.Application
    ↓ source resolution, current-user audit identity, workflow mapping
Services.Repository
    ↓ managed file placement, coordination, compensation
Services.Persistence
    ↓ studies, series, instances, users, and repository metadata
Services.Dicom
    ↓ parsing, validation, metadata, and image decoding
```

`IDicomImportDriveProvider.GetDrivesAsync(...)` exposes a cancellable `IAsyncEnumerable<DicomImportDriveInfo>`. The import view adds each result immediately, so unavailable mapped drives or empty optical media do not freeze the WPF dispatcher. Refreshing, closing the dialog, or starting an import cancels superseded discovery work.

`IImportService.ResolveSourceAsync(...)` is the public application facade for source validation. The internal resolver classifies sources without mounting media, creating network connections, or modifying source content. Direct UNC paths are explicitly checked as `NetworkShare`; the selected path remains `AutoDetect` for the actual import so availability and classification are verified again at execution time.

Supported operational source paths are:

- local directories
- ready removable media and external drives exposed by Windows
- ready CD-ROM and DVD drives
- mapped network drives
- direct UNC network shares
- mounted virtual drives

ISO files are recognized as `IsoImage`, but MOPR does not currently mount them. Users must mount an ISO in Windows and select the resulting virtual drive.

Import results distinguish imported files, already existing files, other skipped files, failed files, and cancellation. A repeat import with zero new files is presented as no new image data rather than as a successful new import.

## Configuration and Administration

The application separates shared configuration contracts from Windows-specific implementations. Machine-wide configuration is stored below `%ProgramData%\MOPR\Configuration` in a versioned envelope protected with Windows DPAPI `LocalMachine` scope and explicit filesystem ACLs.

Configuration areas include:

- application, database, repository, and security configuration
- machine-configuration storage, path resolution, validation, and setup completion
- machine-bound DPAPI protection plus directory and file access control
- targeted database and security updates that preserve all other current configuration sections
- repository-location validation before setup or administrative persistence

Administrative entry requires an active MOPR user and effective elevated Windows administrator rights. `IAdministrativeAuthorizationService` evaluates the effective process token, and privileged application services authorize again at the write boundary.

The administration workspace provides:

- **Database:** load, edit, normalize, test, discard, save, and cancel; only a successfully tested unchanged connection string can be saved
- **DICOM repositories:** list, validate, create, edit, enable, select a default, and delete when invariants allow; physical DICOM files are not deleted by location administration
- **Security:** update self-modification, self-deletion, and user-visibility settings through an isolated machine-configuration update
- **Users:** manage persistent login, Windows SID, names, personnel number, activation state, and optimistic-concurrency token; the system identity and current user are protected
- **System check:** read-only checks of database connectivity, persistence integrity, and repository/MIRAS state; no automatic repair

Machine setup, initial user provisioning, regular sign-in, and later user administration are separate concerns. Audit identity and administrative authorization are also separate: an audit user identifies who performed a change, while elevation determines whether the change is permitted.

## Localization

MOPR uses English default resources and German satellite resources. Each project owns the resources for its user-facing responsibilities.

Examples include:

```text
Contracts/Properties/Resources.resx
Contracts/Properties/Resources.de.resx

Desktop/Properties/Resources.resx
Desktop/Properties/Resources.de.resx

Modules/Identity/Properties/Resources.resx
Modules/Identity/Properties/Resources.de.resx

Modules/Imaging/Properties/Resources.resx
Modules/Imaging/Properties/Resources.de.resx

Modules/Import/Properties/Resources.resx
Modules/Import/Properties/Resources.de.resx

Modules/Setup/Properties/Resources.resx
Modules/Setup/Properties/Resources.de.resx

Services/Application/Properties/Resources.resx
Services/Application/Properties/Resources.de.resx

Services/Miras/Properties/Resources.resx
Services/Miras/Properties/Resources.de.resx
```

Localized enum values use the established `EnumDescriptionTypeConverter` and `LocalizedDescription` infrastructure.

Technical identifiers, filesystem paths, stack traces, and raw exception details must not be copied into ordinary user-facing messages.

## Build

### Requirements

- Windows 10 or Windows 11
- .NET 11 SDK
- Visual Studio with the **.NET desktop development** workload
- SQL Server LocalDB for the default desktop persistence configuration

### Build the Solution

Run from the `Wpf` directory containing `MarcusRunge.Mopr.Workbench.slnx`:

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
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Modules.Identity.Test\MarcusRunge.Mopr.Workbench.Modules.Identity.Test.csproj --configuration Debug
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Modules.Imaging.Test\MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.csproj --configuration Debug
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Modules.Import.Test\MarcusRunge.Mopr.Workbench.Modules.Import.Test.csproj --configuration Debug
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Modules.Miras.Test\MarcusRunge.Mopr.Workbench.Modules.Miras.Test.csproj --configuration Debug
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Modules.Setup.Test\MarcusRunge.Mopr.Workbench.Modules.Setup.Test.csproj --configuration Debug
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Modules.Studies.Test\MarcusRunge.Mopr.Workbench.Modules.Studies.Test.csproj --configuration Debug
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Services.Application.Test\MarcusRunge.Mopr.Workbench.Services.Application.Test.csproj --configuration Debug
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Services.Core.Test\MarcusRunge.Mopr.Workbench.Services.Core.Test.csproj --configuration Debug
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Services.Miras.Test\MarcusRunge.Mopr.Workbench.Services.Miras.Test.csproj --configuration Debug
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Services.Persistence.Test\MarcusRunge.Mopr.Workbench.Services.Persistence.Test.csproj --configuration Debug
dotnet test .\MarcusRunge.Mopr.Workbench\Tests\Services.Repository.Test\MarcusRunge.Mopr.Workbench.Services.Repository.Test.csproj --configuration Debug
```

The test suites cover:

- single-instance application behavior, forwarding, shutdown, and abandoned-marker recovery
- Windows administrative authorization
- application, machine, database, security, and repository-location configuration validation
- protected machine-configuration storage and setup completion with compensation
- operating-system identity and Windows-SID resolution
- machine-level and user-level startup routing
- initial user provisioning, local sign-in, current-user context, and audit identity
- setup and administration view models for database, repository, security, users, and system checks
- repository-administration authorization, conflicts, validation, invariants, and cancellation
- user-administration validation and protection rules
- identity, imaging, import, studies, and MIRAS presentation behavior
- progressive asynchronous drive discovery, ready/unready media selection, direct UNC validation, and application-facade usage
- import-source resolution and application-level DICOM import orchestration
- system-check aggregation and cancellation
- MIRAS flow state, concurrency, cancellation, restart, shutdown, mapping, localization, and edge cases
- persistence integration and integrity verification
- repository import, repair, recovery, compensation, location validation, and operation coordination

Some integration tests may require local infrastructure or configuration that is not needed by unit tests.

## Safety and Privacy

MOPR is designed for local processing. Cloud-based AI or machine-learning services are not part of the intended architecture. Any later AI or machine-learning capability must operate locally and remain a separate validated workflow.

DICOM data, user identities, audit information, and derived medical-imaging information should remain under the control of the local deployment. Deployments must apply appropriate access controls, storage protection, backup policies, and applicable data-protection requirements.

Repository repair is explicit and separate from integrity inspection. MIRAS reports findings and recommended actions but does not silently alter data during a check.

User provisioning and sign-in establish application identity for authorization, routing, and auditing. They do not replace operating-system security, Windows access control, or deployment-specific administrative policies.

## License

See [LICENSE](LICENSE).
