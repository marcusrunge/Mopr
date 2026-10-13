using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.RepositoryAdministration;

public sealed class RepositoryLocationAdministrationServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenAuditIdentityIsUnavailable_ReturnsUnauthorized()
    {
        var context = new TestContext(auditUserId: null);
        var result = await context.Service.CreateAsync(new("Repository", @"C:\MOPR", true, false), Xunit.TestContext.Current.CancellationToken);
        Assert.True(result.IsUnauthorized);
        context.RepositoryLocation.Verify(repository => repository.AddAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenLocationsExist_ReturnsDefaultFirst()
    {
        var context = new TestContext();
        context.RepositoryLocation.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([CreateLocation(2, "Archive", false), CreateLocation(1, "Default", true)]);
        var result = await context.Service.GetAllAsync(Xunit.TestContext.Current.CancellationToken);
        Assert.Collection(result, location => Assert.True(location.IsDefault), location => Assert.False(location.IsDefault));
    }

    [Fact]
    public async Task DeleteAsync_WhenLocationIsDefault_ReturnsDefaultRepository()
    {
        var context = new TestContext(); var location = CreateLocation(1, "Default", true);
        context.RepositoryLocation.Setup(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(location);
        var result = await context.Service.DeleteAsync(new(1, [.. location.RowVersion]), Xunit.TestContext.Current.CancellationToken);
        Assert.True(result.IsDefaultRepository);
    }

    private static RepositoryLocation CreateLocation(int id, string name, bool isDefault) => new() { Id = id, Name = name, RootPath = $@"C:\{name}", IsEnabled = true, IsDefault = isDefault, RowVersion = [1, 2, 3, 4], CreatedByUserId = 1 };

    private sealed class TestContext
    {
        internal TestContext(int? auditUserId = 17)
        {
            AuditIdentityProvider.Setup(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(auditUserId);
            Persistence.SetupGet(persistence => persistence.RepositoryLocation).Returns(RepositoryLocation.Object);
            Base.SetupGet(value => value.AuditIdentityProvider).Returns(AuditIdentityProvider.Object);
            Base.SetupGet(value => value.Persistence).Returns(Persistence.Object);
            Service = RepositoryLocationAdministrationService.Create(Base.Object, CreationLifetime.Scoped);
        }
        internal Mock<IAuditIdentityProvider> AuditIdentityProvider { get; } = new(MockBehavior.Strict);
        internal Mock<IApplicationBase> Base { get; } = new(MockBehavior.Strict);
        internal Mock<IPersistence> Persistence { get; } = new(MockBehavior.Strict);
        internal Mock<IRepositoryLocationRepository> RepositoryLocation { get; } = new(MockBehavior.Strict);
        internal IRepositoryLocationAdministrationService Service { get; }
    }
}
