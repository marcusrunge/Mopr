namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    public sealed class PersistenceConfiguration
    {
        public string ConnectionString { get; set; } = string.Empty;
        public PersistenceMode Mode { get; set; }
    }
}