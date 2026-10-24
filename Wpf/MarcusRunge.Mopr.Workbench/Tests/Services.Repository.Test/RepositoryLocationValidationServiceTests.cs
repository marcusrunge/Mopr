using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using Moq;
using System.Reactive.Subjects;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test;

public sealed class RepositoryLocationValidationServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task LocationValidation_WithEmptyPath_ReturnsInvalidResult(string directoryPath)
    {
        using var context = new RepositoryLocationValidationServiceTestContext();
        var result = await context.LocationValidation.ValidateAsync(directoryPath, TestContext.Current.CancellationToken);

        Assert.False(result.Exists);
        Assert.False(result.IsReadable);
        Assert.False(result.IsWritable);
        Assert.False(result.IsValid);
        Assert.Equal(RepositoryLocationValidationFailureKind.InvalidPath, result.FailureKind);
        Assert.Null(result.NormalizedPath);
    }

    [Fact]
    public async Task LocationValidation_WithMissingDirectory_ReturnsNormalizedInvalidResult()
    {
        using var context = new RepositoryLocationValidationServiceTestContext();
        var directoryPath = Path.Combine(Path.GetTempPath(), "MoprRepositoryLocationTests", Guid.NewGuid().ToString("N"));
        var result = await context.LocationValidation.ValidateAsync(directoryPath, TestContext.Current.CancellationToken);

        Assert.False(result.Exists);
        Assert.False(result.IsReadable);
        Assert.False(result.IsWritable);
        Assert.False(result.IsValid);
        Assert.Equal(RepositoryLocationValidationFailureKind.NotFound, result.FailureKind);
        Assert.Equal(Path.GetFullPath(directoryPath), result.NormalizedPath);
    }

    [Fact]
    public async Task LocationValidation_WithAccessibleDirectory_ReturnsValidResultAndLeavesNoValidationFile()
    {
        using var context = new RepositoryLocationValidationServiceTestContext();
        var directoryPath = CreateTemporaryDirectory();

        try
        {
            string[] filesBeforeValidation = Directory.GetFiles(directoryPath);
            var result = await context.LocationValidation.ValidateAsync(directoryPath, TestContext.Current.CancellationToken);
            string[] filesAfterValidation = Directory.GetFiles(directoryPath);

            Assert.True(result.Exists);
            Assert.True(result.IsReadable);
            Assert.True(result.IsWritable);
            Assert.True(result.IsValid);
            Assert.Equal(RepositoryLocationValidationFailureKind.None, result.FailureKind);
            Assert.Equal(Path.GetFullPath(directoryPath), result.NormalizedPath);
            Assert.Equal(filesBeforeValidation, filesAfterValidation);
            Assert.DoesNotContain(filesAfterValidation, path => Path.GetFileName(path).StartsWith(".mopr-write-test-", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task LocationValidation_WithUnavailableNetworkLocation_ReturnsUnavailableResult()
    {
        using var context = new RepositoryLocationValidationServiceTestContext();
        var directoryPath = $@"\\mopr-unavailable-{Guid.NewGuid():N}\repository";
        var result = await context.LocationValidation.ValidateAsync(directoryPath, TestContext.Current.CancellationToken);
        Assert.False(result.Exists);
        Assert.False(result.IsReadable);
        Assert.False(result.IsWritable);
        Assert.False(result.IsValid);
        Assert.Equal(RepositoryLocationValidationFailureKind.Unavailable, result.FailureKind);
        Assert.Equal(Path.GetFullPath(directoryPath), result.NormalizedPath);
    }

    [Fact]
    public async Task LocationValidation_WithCanceledToken_ThrowsOperationCanceledException()
    {
        using var context = new RepositoryLocationValidationServiceTestContext();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.LocationValidation.ValidateAsync(Path.GetTempPath(), cancellation.Token));
    }

    [Fact]
    public void RepositoryFactory_WhenCreated_ExposesStableLocationValidationThroughFacade()
    {
        using var context = new RepositoryLocationValidationServiceTestContext();

        Assert.NotNull(context.Repository.LocationValidation);
        Assert.Same(context.LocationValidation, context.Factory.Create().LocationValidation);
    }

    private static string CreateTemporaryDirectory()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "MoprRepositoryLocationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    private sealed class RepositoryLocationValidationServiceTestContext : IDisposable
    {
        private readonly BehaviorSubject<IApplicationConfiguration> _applicationConfiguration = new(new TestApplicationConfiguration());
        private readonly TestApplicationLifetime _applicationLifetime = new();

        internal RepositoryLocationValidationServiceTestContext()
        {
            Persistence = new Mock<IPersistence>(MockBehavior.Strict);
            Factory = new RepositoryFactory(_applicationLifetime, _applicationConfiguration, Persistence.Object);
            Repository = Factory.Create();
            LocationValidation = Repository.LocationValidation ?? throw new InvalidOperationException("The repository-location validation service has not been initialized.");
        }

        internal RepositoryFactory Factory { get; }

        internal IRepositoryLocationValidationService LocationValidation { get; }

        internal Mock<IPersistence> Persistence { get; }

        internal IRepository Repository { get; }

        public void Dispose()
        {
            _applicationConfiguration.Dispose();
            _applicationLifetime.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
