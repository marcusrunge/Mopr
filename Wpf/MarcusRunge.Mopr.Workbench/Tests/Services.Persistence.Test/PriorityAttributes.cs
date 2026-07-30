namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class PriorityAttribute(int value) : Attribute
    {
        public int Value { get; } = value;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DefaultPriorityAttribute(int value) : Attribute
    {
        public int Value { get; } = value;
    }
}