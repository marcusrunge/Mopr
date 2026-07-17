using System;
using System.Collections.Generic;
using System.Text;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    public sealed class DicomRepositoryRepairRequest
    {
        public bool RebuildRepositoryIndex { get; set; }
        public bool RepairMissingFiles { get; set; } = true;
        public bool VerifyFiles { get; set; } = true;
    }
}