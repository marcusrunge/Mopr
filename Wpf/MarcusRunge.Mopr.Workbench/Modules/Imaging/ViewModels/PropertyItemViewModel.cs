namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class PropertyItemViewModel(string name, string value, bool isSection = false)
    {
        public bool IsSection { get; } = isSection;
        public string Name { get; } = name;

        public string Value { get; } = value;
    }
}