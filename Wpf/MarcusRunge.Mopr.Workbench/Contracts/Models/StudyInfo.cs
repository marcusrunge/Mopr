using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models
{
    public sealed class StudyInfo(string id, string name, string description, DateTime? studyDate = null, string? patientDisplayName = null, string? accessionNumber = null)
    {
        public string? AccessionNumber { get; } = accessionNumber;
        public string Description { get; } = description;
        public string DisplayText => string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} · {Description}";
        public string Id { get; } = id;
        public string Name { get; } = name;
        public string? PatientDisplayName { get; } = patientDisplayName;
        public DateTime? StudyDate { get; } = studyDate;
    }
}