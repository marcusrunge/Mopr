using MarcusRunge.Mopr.Workbench.Contracts.Application;
using System;
using System.IO;

namespace MarcusRunge.Mopr.Workbench.Application
{
    public sealed class RepositoryConfiguration : IRepositoryConfiguration
    {
        public bool AutomaticallyRepairPaths { get; set; } = true;
        public string DicomRepositoryPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MOPR", "Dicom");

        public bool VerifyRepositoryOnStartup { get; set; } = true;
    }
}