using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.RepositoryRecovery;

public sealed class RepositoryRecoveryActionServiceTests
{
    [Fact]
    public async Task RecreateDirectoryAsync_WhenAdministrativeAuthorizationIsMissing_ReturnsUnauthorized()
    {
        using var context = new RepositoryRecoveryActionServiceTestContext(isElevatedAdministrator: false);
        var result = await context.Service.RecreateDirectoryAsync(1, context.RepositoryPath, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(RepositoryRecoveryActionStatus.Unauthorized, result.Status);
        context.Validation.Verify(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecreateDirectoryAsync_WhenPathIsValid_CreatesDirectoryAndReturnsCompleted()
    {
        using var context = new RepositoryRecoveryActionServiceTestContext();
        context.ConfigureValidPath();
        var result = await context.Service.RecreateDirectoryAsync(1, context.RepositoryPath, Xunit.TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Equal(Path.GetFullPath(context.RepositoryPath), result.NormalizedPath);
        Assert.True(Directory.Exists(context.RepositoryPath));
    }

    [Fact]
    public async Task RelocateAsync_WhenAdministrationReportsConflict_ReturnsConflict()
    {
        using var context = new RepositoryRecoveryActionServiceTestContext();
        context.ConfigureValidPath();
        context.Administration.Setup(service => service.UpdateAsync(It.IsAny<RepositoryLocationUpdateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(RepositoryLocationUpdateResult.Conflict());
        var result = await context.Service.RelocateAsync(1, "Repository", context.RepositoryPath, true, false, [1, 2, 3], Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(RepositoryRecoveryActionStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task RemoveAsync_WhenAdministrationReportsDefaultRepository_ReturnsDefaultRepository()
    {
        using var context = new RepositoryRecoveryActionServiceTestContext();
        context.Administration.Setup(service => service.DeleteAsync(It.IsAny<RepositoryLocationDeleteRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(RepositoryLocationDeleteResult.DefaultRepository());
        var result = await context.Service.RemoveAsync(1, [1, 2, 3], Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(RepositoryRecoveryActionStatus.DefaultRepository, result.Status);
    }

    private sealed class RepositoryRecoveryActionServiceTestContext : IDisposable
    {
        internal RepositoryRecoveryActionServiceTestContext(bool isElevatedAdministrator = true)
        {
            RepositoryPath = Path.Combine(Path.GetTempPath(), "MoprRepositoryRecoveryTests", Guid.NewGuid().ToString("N"));
            Authorization.SetupGet(service => service.IsElevatedAdministrator).Returns(isElevatedAdministrator);
            ApplicationBase.SetupGet(value => value.AdministrativeAuthorizationService).Returns(Authorization.Object);
            ApplicationBase.SetupGet(value => value.RepositoryAdministration).Returns(Administration.Object);
            Repository.SetupGet(value => value.LocationValidation).Returns(Validation.Object);
            ApplicationBase.SetupGet(value => value.Repository).Returns(Repository.Object);
            Service = RepositoryRecoveryActionService.Create(ApplicationBase.Object, CreationLifetime.Scoped);
        }
        internal Mock<IRepositoryLocationAdministrationService> Administration { get; } = new(MockBehavior.Strict);
        internal Mock<IAdministrativeAuthorizationService> Authorization { get; } = new(MockBehavior.Strict);
        internal Mock<IApplicationBase> ApplicationBase { get; } = new(MockBehavior.Strict);
        internal Mock<IRepository> Repository { get; } = new(MockBehavior.Strict);
        internal string RepositoryPath { get; }
        internal IRepositoryRecoveryActionService Service { get; }
        internal Mock<IRepositoryLocationValidationService> Validation { get; } = new(MockBehavior.Strict);
        internal void ConfigureValidPath() => Validation.Setup(service => service.ValidateAsync(RepositoryPath, It.IsAny<CancellationToken>())).ReturnsAsync(new RepositoryLocationValidationResult { Exists = true, IsReadable = true, IsWritable = true, NormalizedPath = Path.GetFullPath(RepositoryPath) });
        public void Dispose() { if (Directory.Exists(RepositoryPath)) Directory.Delete(RepositoryPath, recursive: true); }
    }
}
