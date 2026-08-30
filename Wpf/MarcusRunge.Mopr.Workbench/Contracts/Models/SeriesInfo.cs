namespace MarcusRunge.Mopr.Workbench.Contracts.Models
{
    public sealed class SeriesInfo(string id, string modality, string name, string description, int imageCount, string? studyId = null, int? seriesNumber = null)
    {
        public string Description { get; } = description;
        public string DisplayText => string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} · {Description}";
        public string Id { get; } = id;
        public int ImageCount { get; } = imageCount;
        public string ImageCountDisplayText => $"{ImageCount} Bilder";
        public string Modality { get; } = modality;
        public string Name { get; } = name;
        public int? SeriesNumber { get; } = seriesNumber;
        public string? StudyId { get; } = studyId;
    }
}