using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models
{
    public sealed class StudyInfo
    {
        public StudyInfo(string id, string name, string description, DateTime? studyDate = null, string? patientDisplayName = null, string? accessionNumber = null)
        {
            Id = id;
            Name = name;
            Description = description;
            StudyDate = studyDate;
            PatientDisplayName = patientDisplayName;
            AccessionNumber = accessionNumber;
        }

        public string? AccessionNumber { get; }
        public string Description { get; }
        public string DisplayText => string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} · {Description}";
        public string Id { get; }
        public string Name { get; }
        public string? PatientDisplayName { get; }
        public DateTime? StudyDate { get; }
    }
}