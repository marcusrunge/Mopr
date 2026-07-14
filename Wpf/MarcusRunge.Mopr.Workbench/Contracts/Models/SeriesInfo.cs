namespace MarcusRunge.Mopr.Workbench.Contracts.Models
{
    public sealed class SeriesInfo
    {
        public SeriesInfo(string id, string modality, string name, string description, int imageCount, string? studyId = null, int? seriesNumber = null)
        {
            Id = id;
            StudyId = studyId;
            Modality = modality;
            Name = name;
            Description = description;
            ImageCount = imageCount;
            SeriesNumber = seriesNumber;
        }

        public string Description { get; }
        public string DisplayText => string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} · {Description}";
        public string Id { get; }
        public int ImageCount { get; }
        public string ImageCountDisplayText => $"{ImageCount} Bilder";
        public string Modality { get; }
        public string Name { get; }
        public int? SeriesNumber { get; }
        public string? StudyId { get; }
    }
}