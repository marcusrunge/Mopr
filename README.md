# MOPR
<p align="left">
  <img src="Wpf/MarcusRunge.Mopr.Workbench/MarcusRunge.Mopr.Workbench/Assets/mopr.png" alt="MOPR Logo" width="300">
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
├── Wpf/
│   └── MarcusRunge.Mopr.Workbench/
│       ├── MarcusRunge.Mopr.Workbench/                    # Main WPF Application
│       │   ├── Assets/                                     # Application Resources
│       │   ├── Properties/                                 # Project Properties
│       │   ├── ViewModels/                                 # MVVM ViewModels
│       │   └── Views/                                      # XAML Views
│       │
│       ├── MarcusRunge.Mopr.Workbench.Core/               # Core Functionality
│       │   ├── Mvvm/                                       # MVVM Framework
│       │   └── Services/                                   # Core Services
│       │
│       ├── MarcusRunge.Mopr.Workbench.Contracts/          # Data Contracts & Models
│       │   ├── Imaging/                                    # Imaging Models
│       │   └── Models/                                     # Domain Models
│       │
│       ├── Services/
│       │   ├── MarcusRunge.Mopr.Workbench.Services/       # Service Implementations
│       │   │   └── Imaging/
│       │   └── MarcusRunge.Mopr.Workbench.Services.Interfaces/  # Service Interfaces
│       │       └── Imaging/
│       │
│       ├── Modules/
│       │   ├── MarcusRunge.Mopr.Workbench.Modules.Imaging/     # Imaging Module
│       │   │   ├── Services/
│       │   │   ├── ViewModels/
│       │   │   └── Views/
│       │   └── MarcusRunge.Mopr.Workbench.Modules.ModuleName/  # Template for Additional Modules
│       │
│       └── Tests/
│           ├── MarcusRunge.Mopr.Workbench.Modules.Imaging.Tests/      # Imaging Module Tests
│           │   └── ViewModels/
│           └── MarcusRunge.Mopr.Workbench.Modules.ModuleName.Tests/   # Template for Module Tests
│
└── Unreal/                                                  # Unreal Engine Project Files
