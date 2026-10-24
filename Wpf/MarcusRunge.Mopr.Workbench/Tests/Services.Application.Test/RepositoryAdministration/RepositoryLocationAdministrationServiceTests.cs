using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.RepositoryAdministration;

public sealed class RepositoryLocationAdministrationServiceTests
{
    [Fact]
    public async Task ValidateAsync_WhenPathIsValid_DelegatesNormalizedPathAndReturnsResult()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext();
        var expected = new RepositoryLocationValidationResult { Exists = true, IsReadable = true, IsWritable = true, NormalizedPath = @"C:\MOPR" };

        context.ValidationService.Setup(service => service.ValidateAsync(@"C:\MOPR", cancellationToken)).ReturnsAsync(expected);

        var result = await context.Service.ValidateAsync("  C:\\MOPR  ", cancellationToken);

        Assert.Same(expected, result);
        context.AdministrativeAuthorizationService.Verify(service => service.DemandElevatedAdministrator(), Times.Once);
        context.ValidationService.Verify(service => service.ValidateAsync(@"C:\MOPR", cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WhenAdministratorAuthorizationFails_ThrowsUnauthorizedAccessException()
    {
        var context = new TestContext(isAuthorized: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => context.Service.ValidateAsync(@"C:\MOPR", Xunit.TestContext.Current.CancellationToken));

        context.ValidationService.Verify(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenPathIsMissing_ThrowsArgumentException(string rootPath)
    {
        var context = new TestContext();

        await Assert.ThrowsAsync<ArgumentException>(() => context.Service.ValidateAsync(rootPath, Xunit.TestContext.Current.CancellationToken));

        context.AdministrativeAuthorizationService.Verify(service => service.DemandElevatedAdministrator(), Times.Once);
        context.ValidationService.Verify(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_WhenCanceled_PropagatesCancellationWithoutAuthorizingOrCallingRepository()
    {
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(Xunit.TestContext.Current.CancellationToken);
        await cancellationSource.CancelAsync();

        var context = new TestContext();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.ValidateAsync(@"C:\MOPR", cancellationSource.Token));

        context.AdministrativeAuthorizationService.Verify(service => service.DemandElevatedAdministrator(), Times.Never);
        context.ValidationService.Verify(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenAdministratorAuthorizationFails_ReturnsUnauthorized()
    {
        var context = new TestContext(isAuthorized: false);

        var result = await context.Service.CreateAsync(new("Repository", @"C:\MOPR", true, false), Xunit.TestContext.Current.CancellationToken);

        Assert.True(result.IsUnauthorized);
        context.AuditIdentityProvider.Verify(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.RepositoryLocation.Verify(repository => repository.AddAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenAuditIdentityIsUnavailable_ReturnsUnauthorized()
    {
        var context = new TestContext(auditUserId: null);

        var result = await context.Service.CreateAsync(new("Repository", @"C:\MOPR", true, false), Xunit.TestContext.Current.CancellationToken);

        Assert.True(result.IsUnauthorized);
        context.AdministrativeAuthorizationService.Verify(service => service.DemandElevatedAdministrator(), Times.Once);
        context.RepositoryLocation.Verify(repository => repository.AddAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAdministratorAuthorizationFails_ReturnsUnauthorized()
    {
        var context = new TestContext(isAuthorized: false);

        var result = await context.Service.UpdateAsync(new(1, "Repository", @"C:\MOPR", true, false, [1, 2, 3, 4]), Xunit.TestContext.Current.CancellationToken);

        Assert.True(result.IsUnauthorized);
        context.AuditIdentityProvider.Verify(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.RepositoryLocation.Verify(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenAdministratorAuthorizationFails_ReturnsUnauthorized()
    {
        var context = new TestContext(isAuthorized: false);

        var result = await context.Service.DeleteAsync(new(1, [1, 2, 3, 4]), Xunit.TestContext.Current.CancellationToken);

        Assert.True(result.IsUnauthorized);
        context.AuditIdentityProvider.Verify(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>()), Times.Never);
        context.RepositoryLocation.Verify(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenAdministratorAuthorizationFails_ThrowsUnauthorizedAccessException()
    {
        var context = new TestContext(isAuthorized: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => context.Service.GetAllAsync(Xunit.TestContext.Current.CancellationToken));

        context.RepositoryLocation.Verify(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenLocationsExist_ReturnsDefaultFirst()
    {
        var context = new TestContext();

        context.RepositoryLocation.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([CreateLocation(2, "Archive", false), CreateLocation(1, "Default", true)]);

        var result = await context.Service.GetAllAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Collection(result, location => Assert.True(location.IsDefault), location => Assert.False(location.IsDefault));
        context.AdministrativeAuthorizationService.Verify(service => service.DemandElevatedAdministrator(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenLocationIsDefault_ReturnsDefaultRepository()
    {
        var context = new TestContext();
        var location = CreateLocation(1, "Default", true);

        context.RepositoryLocation.Setup(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(location);

        var result = await context.Service.DeleteAsync(new(1, [.. location.RowVersion]), Xunit.TestContext.Current.CancellationToken);

        Assert.True(result.IsDefaultRepository);
        context.AdministrativeAuthorizationService.Verify(service => service.DemandElevatedAdministrator(), Times.Once);
    }

    private static RepositoryLocation CreateLocation(int id, string name, bool isDefault) => new() { Id = id, Name = name, RootPath = $@"C:\{name}", IsEnabled = true, IsDefault = isDefault, RowVersion = [1, 2, 3, 4], CreatedByUserId = 1 };

    private sealed class TestContext
    {
        internal TestContext(int? auditUserId = 17, bool isAuthorized = true)
        {
            if (isAuthorized)
            {
                AdministrativeAuthorizationService.Setup(service => service.DemandElevatedAdministrator());
            }
            else
            {
                AdministrativeAuthorizationService.Setup(service => service.DemandElevatedAdministrator()).Throws(new UnauthorizedAccessException("Administrative authorization is required."));
            }

            AuditIdentityProvider.Setup(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(auditUserId);
            Persistence.SetupGet(persistence => persistence.RepositoryLocation).Returns(RepositoryLocation.Object);
            Base.SetupGet(value => value.AdministrativeAuthorizationService).Returns(AdministrativeAuthorizationService.Object);
            Base.SetupGet(value => value.AuditIdentityProvider).Returns(AuditIdentityProvider.Object);
            Base.SetupGet(value => value.Persistence).Returns(Persistence.Object);
            Base.SetupGet(value => value.Repository).Returns(Repository.Object);
            Repository.SetupGet(value => value.LocationValidation).Returns(ValidationService.Object);

            Service = RepositoryLocationAdministrationService.Create(Base.Object, CreationLifetime.Scoped);
        }

        internal Mock<IAdministrativeAuthorizationService> AdministrativeAuthorizationService { get; } = new(MockBehavior.Strict);

        internal Mock<IAuditIdentityProvider> AuditIdentityProvider { get; } = new(MockBehavior.Strict);

        internal Mock<IApplicationBase> Base { get; } = new(MockBehavior.Strict);

        internal Mock<IPersistence> Persistence { get; } = new(MockBehavior.Strict);

        internal Mock<IRepositoryLocationRepository> RepositoryLocation { get; } = new(MockBehavior.Strict);

        internal Mock<MarcusRunge.Mopr.Workbench.Services.Repository.Contracts.IRepository> Repository { get; } = new(MockBehavior.Strict);

        internal Mock<IRepositoryLocationValidationService> ValidationService { get; } = new(MockBehavior.Strict);

        internal IRepositoryLocationAdministrationService Service { get; }
    }
}