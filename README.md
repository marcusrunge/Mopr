# MOPR

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
mopr/
├── README.md
├── LICENSE
├── .gitignore
│
├── wpf/
│   ├── Mopr.Workbench.sln
│   ├── Mopr.Workbench/
│   ├── Mopr.Core/
│   ├── Mopr.Dicom/
│   ├── Mopr.Volume/
│   └── Mopr.Export/
│
├── unreal/
│   └── MoprViewer/
│       ├── MoprViewer.uproject
│       ├── Source/
│       ├── Content/
│       └── Plugins/
│           └── MoprRuntime/
│
├── shared/
│   ├── schema/
│   └── docs/
│
├── data/
│   ├── exports/
│   ├── samples/
│   └── cache/
│
└── tools/
