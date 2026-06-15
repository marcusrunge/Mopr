namespace MarcusRunge.Mopr.Workbench.Contracts.Models
{
    public sealed class DicomTagInfo
    {
        public DicomTagInfo(
            string tag,
            string name,
            string value)
        {
            Tag = tag;
            Name = name;
            Value = value;
        }

        public string DisplayText => $"{Tag} {Name}: {Value}";
        public string Name { get; }
        public string Tag { get; }
        public string Value { get; }
    }
}