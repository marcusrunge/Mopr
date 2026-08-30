namespace MarcusRunge.Mopr.Workbench.Contracts.Models
{
    public sealed class DicomTagInfo(string tag, string name, string value)
    {
        public string DisplayText => $"{Tag} {Name}: {Value}";
        public string Name { get; } = name;
        public string Tag { get; } = tag;
        public string Value { get; } = value;
    }
}