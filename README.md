# MOPR
<p align="left">
  <img src="Wpf/MarcusRunge.Mopr.Workbench/Desktop/Assets/mopr.png" alt="MOPR Logo" width="300">
</p>
**Medical Observation & Projection Renderer**

MOPR is an experimental pipeline for preparing DICOM CT/MRI image series and visualizing volumetric medical imaging data in Unreal Engine.

The repository is split into two main application areas:

- **WPF** — the desktop workbench used to import, process, resample, and export medical imaging volumes.
- **Unreal** — the Unreal Engine viewer used to load prepared volume packages and render them interactively.

MOPR uses a file-based exchange format between both applications. The WPF application prepares the data and exports a MOPR volume package. The Unreal application then loads and visualizes that package.

> **Important:** MOPR is intended for visualization, research, prototyping, and educational purposes only.  
> It is not intended for diagnostic use, treatment planning, clinical reporting, or medical decision-making.

---

## Project Structure

```text
Mopr/
├── .gitignore                                  # Git Ignore Rules
├── LICENSE                                     # Project License
├── README.md                                   # Project Documentation
│
├── Unreal/                                     # Unreal Engine Project Files
│
└── Wpf/
    ├── MarcusRunge.Mopr.Workbench.slnx         # Solution File
    │
    └── MarcusRunge.Mopr.Workbench/
        ├── Contracts/                          # Shared Contracts, Models & Configuration Interfaces
        │   ├── Application/                    # Application Configuration Contracts
        │   ├── Enums/                          # Shared Enumerations
        │   ├── Imaging/                        # Imaging Data Contracts
        │   ├── Models/                         # Domain Models & Data Structures
        │   └── Properties/                     # Localization Resources
        │
        ├── Core/                               # Core MVVM Infrastructure
        │   └── Mvvm/                           # Base ViewModel Implementations
        │
        ├── Desktop/                            # Main WPF Application
        │   ├── Application/                    # Application Configuration & Lifetime
        │   ├── Assets/                         # Application Resources
        │   ├── Properties/                     # Localization Resources
        │   ├── Services/                       # Desktop-Specific Services
        │   ├── ViewModels/                     # Main ViewModels
        │   └── Views/                          # Main Application Views
        │
        ├── Modules/
        │   └── Imaging/                        # Medical Imaging Module
        │       ├── Behaviors/                  # WPF Interaction Behaviors
        │       ├── Infrastructure/             # Imaging Infrastructure Components
        │       ├── Properties/                 # Localization Resources
        │       ├── Services/                   # Module-Specific Services
        │       ├── ViewModels/                 # Imaging ViewModels
        │       └── Views/                      # Imaging Views & Viewports
        │
        ├── Services/
        │   ├── Core/                           # Application Service Layer
        │   │   ├── Contracts/                  # Service Contracts
        │   │   └── Implementations/            # Service Implementations
        │   │
        │   ├── Dicom/                          # DICOM Processing Services
        │   │   ├── Contracts/                  # DICOM Contracts
        │   │   └── Implementations/            # DICOM Implementations
        │   │
        │   ├── Persistence/                    # Entity Framework Persistence Layer
        │   │   ├── Contracts/                  # Repository Contracts
        │   │   ├── Entities/                   # Database Entities
        │   │   ├── Migrations/                 # Entity Framework Migrations
        │   │   └── Implementations/            # Persistence Implementations
        │   │
        │   ├── Repository/                     # DICOM Repository Management
        │   │   ├── Contracts/                  # Repository Contracts
        │   │   └── Implementations/            # Repository Services
        │   │
        │   └── Wpf/                            # WPF-Specific Services
        │       ├── Contracts/                  # UI Service Contracts
        │       └── Implementations/            # UI Service Implementations
        │
        └── Tests/
            ├── MarcusRunge.Mopr.Workbench.Modules.Imaging.Tests
            │   └── ViewModels/                 # Imaging Module Unit Tests
            │
            ├── MarcusRunge.Mopr.Workbench.Services.Persistence.Test
            │                                   # Persistence Integration Tests
            │
            └── MarcusRunge.Mopr.Workbench.Services.Repository.Test
                                                # Repository Integration Tests