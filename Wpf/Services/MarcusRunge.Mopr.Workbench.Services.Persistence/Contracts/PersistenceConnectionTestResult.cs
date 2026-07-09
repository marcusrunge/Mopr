namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    public sealed class PersistenceConnectionTestResult
    {
        public Exception? Exception { get; init; }
        public bool IsSuccessful { get; init; }

        public string? Message { get; init; }
    }
}