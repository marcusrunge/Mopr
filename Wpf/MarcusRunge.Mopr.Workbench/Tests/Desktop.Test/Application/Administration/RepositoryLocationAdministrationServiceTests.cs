using MarcusRunge.Mopr.Workbench.Application.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Administration;

public sealed class RepositoryLocationAdministrationServiceTests
{
    private const int CurrentUserId = 17;
    private const int DefaultLocationId = 1;
    private const int SecondaryLocationId = 2;
    private const string DefaultLocationName = "Primary repository";
    private const string DefaultLocationPath = @"C:\MOPR\Repository";
    private const string SecondaryLocationName = "Archive repository";
    private const string SecondaryLocationPath = @"D:\MOPR\Archive";

    private static readonly byte[] DefaultRowVersion = [1, 2, 3, 4];
    private static readonly byte[] SecondaryRowVersion = [5, 6, 7, 8];

    [Fact]
    public async Task GetAllAsync_WhenLocationsExist_ReturnsDefaultFirstAndMapsConcurrencyTokens()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var secondaryLocation = CreateLocation(SecondaryLocationId, SecondaryLocationName, SecondaryLocationPath, true, false, SecondaryRowVersion);
        var defaultLocation = CreateLocation(DefaultLocationId, DefaultLocationName, DefaultLocationPath, true, true, DefaultRowVersion);
        scenario.Repository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync([secondaryLocation, defaultLocation]);

        var result = await scenario.Service.GetAllAsync(cancellationToken);

        Assert.Collection(result,
            location =>
            {
                Assert.Equal(DefaultLocationId, location.Id);
                Assert.Equal(DefaultLocationName, location.Name);
                Assert.Equal(DefaultLocationPath, location.RootPath);
                Assert.True(location.IsEnabled);
                Assert.True(location.IsDefault);
                Assert.Equal(DefaultRowVersion, location.RowVersion);
                Assert.NotSame(DefaultRowVersion, location.RowVersion);
            },
            location =>
            {
                Assert.Equal(SecondaryLocationId, location.Id);
                Assert.Equal(SecondaryLocationName, location.Name);
                Assert.Equal(SecondaryLocationPath, location.RootPath);
                Assert.True(location.IsEnabled);
                Assert.False(location.IsDefault);
                Assert.Equal(SecondaryRowVersion, location.RowVersion);
                Assert.NotSame(SecondaryRowVersion, location.RowVersion);
            });
    }

    [Fact]
    public async Task CreateAsync_WhenCurrentUserIsUnavailable_ReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario(currentUser: null, useDefaultActiveUser: false);
        var request = new RepositoryLocationCreateRequest("New repository", @"E:\MOPR\New", true, false);

        var result = await scenario.Service.CreateAsync(request, cancellationToken);

        Assert.True(result.IsUnauthorized);
        Assert.False(result.IsSuccess);
        scenario.Repository.Verify(repository => repository.AddAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenRootPathAlreadyExists_ReturnsDuplicatePath()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var existingLocation = CreateLocation(SecondaryLocationId, SecondaryLocationName, SecondaryLocationPath, true, false, SecondaryRowVersion);
        scenario.Repository.Setup(repository => repository.GetByRootPathAsync(SecondaryLocationPath, cancellationToken)).ReturnsAsync(existingLocation);
        var request = new RepositoryLocationCreateRequest("Duplicate repository", SecondaryLocationPath, true, false);

        var result = await scenario.Service.CreateAsync(request, cancellationToken);

        Assert.True(result.IsDuplicatePath);
        Assert.False(result.IsSuccess);
        scenario.Repository.Verify(repository => repository.AddAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCreatingDefaultRepository_EnablesAndAuditsPersistedLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var request = new RepositoryLocationCreateRequest("  New repository  ", @"E:\MOPR\New", false, true);
        RepositoryLocation? addedLocation = null;
        scenario.Repository.SetupSequence(repository => repository.GetByRootPathAsync(request.RootPath, cancellationToken)).ReturnsAsync((RepositoryLocation?)null).ReturnsAsync(() => addedLocation);
        scenario.Repository.Setup(repository => repository.AddAsync(It.IsAny<RepositoryLocation>(), cancellationToken)).Callback<RepositoryLocation, CancellationToken>((location, _) =>
        {
            addedLocation = location;
            location.Id = 3;
            location.RowVersion = [9, 10, 11, 12];
        }).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var result = await scenario.Service.CreateAsync(request, cancellationToken);
        var after = DateTime.UtcNow;

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Location);
        Assert.NotNull(addedLocation);
        Assert.Equal(3, result.Location.Id);
        Assert.Equal("New repository", addedLocation.Name);
        Assert.Equal(request.RootPath, addedLocation.RootPath);
        Assert.True(addedLocation.IsEnabled);
        Assert.True(addedLocation.IsDefault);
        Assert.Equal(CurrentUserId, addedLocation.CreatedByUserId);
        Assert.InRange(addedLocation.CreatedAtUtc, before, after);
        Assert.Equal([9, 10, 11, 12], result.Location.RowVersion);
        scenario.Repository.Verify(repository => repository.AddAsync(addedLocation, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenLocationDoesNotExist_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        scenario.Repository.Setup(repository => repository.GetByIdAsync(SecondaryLocationId, cancellationToken)).ReturnsAsync((RepositoryLocation?)null);
        var request = CreateUpdateRequest();

        var result = await scenario.Service.UpdateAsync(request, cancellationToken);

        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_WhenRowVersionChanged_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var location = CreateLocation(SecondaryLocationId, SecondaryLocationName, SecondaryLocationPath, true, false, [8, 8, 8, 8]);
        scenario.Repository.Setup(repository => repository.GetByIdAsync(SecondaryLocationId, cancellationToken)).ReturnsAsync(location);
        var request = CreateUpdateRequest();

        var result = await scenario.Service.UpdateAsync(request, cancellationToken);

        Assert.True(result.IsConflict);
        Assert.False(result.IsSuccess);
        scenario.Repository.Verify(repository => repository.UpdateAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task UpdateAsync_WhenCurrentDefaultWouldBecomeInvalid_ReturnsInvariantViolation(bool requestedIsDefault, bool requestedIsEnabled)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var location = CreateLocation(DefaultLocationId, DefaultLocationName, DefaultLocationPath, true, true, DefaultRowVersion);
        scenario.Repository.Setup(repository => repository.GetByIdAsync(DefaultLocationId, cancellationToken)).ReturnsAsync(location);
        var request = new RepositoryLocationUpdateRequest(DefaultLocationId, DefaultLocationName, DefaultLocationPath, requestedIsEnabled, requestedIsDefault, [.. DefaultRowVersion]);

        var result = await scenario.Service.UpdateAsync(request, cancellationToken);

        Assert.True(result.IsInvariantViolation);
        Assert.False(result.IsSuccess);
        scenario.Repository.Verify(repository => repository.UpdateAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAnotherLocationUsesRootPath_ReturnsDuplicatePath()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var location = CreateLocation(SecondaryLocationId, SecondaryLocationName, SecondaryLocationPath, true, false, SecondaryRowVersion);
        var conflictingLocation = CreateLocation(3, "Conflict", @"E:\MOPR\Conflict", true, false, [9, 9, 9, 9]);
        scenario.Repository.Setup(repository => repository.GetByIdAsync(SecondaryLocationId, cancellationToken)).ReturnsAsync(location);
        scenario.Repository.Setup(repository => repository.GetByRootPathAsync(@"E:\MOPR\Conflict", cancellationToken)).ReturnsAsync(conflictingLocation);
        var request = new RepositoryLocationUpdateRequest(SecondaryLocationId, "Updated archive", @"E:\MOPR\Conflict", true, false, [.. SecondaryRowVersion]);

        var result = await scenario.Service.UpdateAsync(request, cancellationToken);

        Assert.True(result.IsDuplicatePath);
        Assert.False(result.IsSuccess);
        scenario.Repository.Verify(repository => repository.UpdateAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsValid_UpdatesAuditDataAndReturnsReloadedLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var location = CreateLocation(SecondaryLocationId, SecondaryLocationName, SecondaryLocationPath, true, false, SecondaryRowVersion);
        var reloadedLocation = CreateLocation(SecondaryLocationId, "Updated archive", @"E:\MOPR\Archive", true, false, [9, 10, 11, 12]);
        scenario.Repository.SetupSequence(repository => repository.GetByIdAsync(SecondaryLocationId, cancellationToken)).ReturnsAsync(location).ReturnsAsync(reloadedLocation);
        scenario.Repository.Setup(repository => repository.GetByRootPathAsync(reloadedLocation.RootPath!, cancellationToken)).ReturnsAsync((RepositoryLocation?)null);
        scenario.Repository.Setup(repository => repository.UpdateAsync(location, cancellationToken)).Returns(Task.CompletedTask);
        var request = new RepositoryLocationUpdateRequest(SecondaryLocationId, reloadedLocation.Name!, reloadedLocation.RootPath!, true, false, [.. SecondaryRowVersion]);

        var before = DateTime.UtcNow;
        var result = await scenario.Service.UpdateAsync(request, cancellationToken);
        var after = DateTime.UtcNow;

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Location);
        Assert.Equal(reloadedLocation.Name, location.Name);
        Assert.Equal(reloadedLocation.RootPath, location.RootPath);
        Assert.Equal(CurrentUserId, location.ModifiedByUserId);
        Assert.NotNull(location.ModifiedAtUtc);
        Assert.InRange(location.ModifiedAtUtc.Value, before, after);
        Assert.Equal([9, 10, 11, 12], result.Location.RowVersion);
        scenario.Repository.Verify(repository => repository.UpdateAsync(location, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenPersistenceReportsConcurrencyFailure_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var location = CreateLocation(SecondaryLocationId, SecondaryLocationName, SecondaryLocationPath, true, false, SecondaryRowVersion);
        scenario.Repository.Setup(repository => repository.GetByIdAsync(SecondaryLocationId, cancellationToken)).ReturnsAsync(location);
        scenario.Repository.Setup(repository => repository.GetByRootPathAsync(It.IsAny<string>(), cancellationToken)).ReturnsAsync((RepositoryLocation?)null);
        scenario.Repository.Setup(repository => repository.UpdateAsync(location, cancellationToken)).ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await scenario.Service.UpdateAsync(CreateUpdateRequest(), cancellationToken);

        Assert.True(result.IsConflict);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_WhenLocationDoesNotExist_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        scenario.Repository.Setup(repository => repository.GetByIdAsync(SecondaryLocationId, cancellationToken)).ReturnsAsync((RepositoryLocation?)null);

        var result = await scenario.Service.DeleteAsync(CreateDeleteRequest(), cancellationToken);

        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_WhenRowVersionChanged_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var location = CreateLocation(SecondaryLocationId, SecondaryLocationName, SecondaryLocationPath, true, false, [9, 9, 9, 9]);
        scenario.Repository.Setup(repository => repository.GetByIdAsync(SecondaryLocationId, cancellationToken)).ReturnsAsync(location);

        var result = await scenario.Service.DeleteAsync(CreateDeleteRequest(), cancellationToken);

        Assert.True(result.IsConflict);
        Assert.False(result.IsSuccess);
        scenario.Repository.Verify(repository => repository.DeleteAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenLocationIsDefault_ReturnsDefaultRepository()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var location = CreateLocation(DefaultLocationId, DefaultLocationName, DefaultLocationPath, true, true, DefaultRowVersion);
        scenario.Repository.Setup(repository => repository.GetByIdAsync(DefaultLocationId, cancellationToken)).ReturnsAsync(location);
        var request = new RepositoryLocationDeleteRequest(DefaultLocationId, [.. DefaultRowVersion]);

        var result = await scenario.Service.DeleteAsync(request, cancellationToken);

        Assert.True(result.IsDefaultRepository);
        Assert.False(result.IsSuccess);
        scenario.Repository.Verify(repository => repository.DeleteAsync(It.IsAny<RepositoryLocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenPersistenceReportsReferencedData_ReturnsInUse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var location = CreateLocation(SecondaryLocationId, SecondaryLocationName, SecondaryLocationPath, true, false, SecondaryRowVersion);
        scenario.Repository.Setup(repository => repository.GetByIdAsync(SecondaryLocationId, cancellationToken)).ReturnsAsync(location);
        scenario.Repository.Setup(repository => repository.DeleteAsync(location, cancellationToken)).ThrowsAsync(new DbUpdateException());

        var result = await scenario.Service.DeleteAsync(CreateDeleteRequest(), cancellationToken);

        Assert.True(result.IsInUse);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_WhenRequestIsValid_DeletesLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var scenario = new RepositoryLocationAdministrationServiceScenario();
        var location = CreateLocation(SecondaryLocationId, SecondaryLocationName, SecondaryLocationPath, true, false, SecondaryRowVersion);
        scenario.Repository.Setup(repository => repository.GetByIdAsync(SecondaryLocationId, cancellationToken)).ReturnsAsync(location);
        scenario.Repository.Setup(repository => repository.DeleteAsync(location, cancellationToken)).Returns(Task.CompletedTask);

        var result = await scenario.Service.DeleteAsync(CreateDeleteRequest(), cancellationToken);

        Assert.True(result.IsSuccess);
        scenario.Repository.Verify(repository => repository.DeleteAsync(location, cancellationToken), Times.Once);
    }

    private static RepositoryLocationDeleteRequest CreateDeleteRequest() => new(SecondaryLocationId, [.. SecondaryRowVersion]);

    private static RepositoryLocation CreateLocation(int id, string name, string rootPath, bool isEnabled, bool isDefault, byte[] rowVersion) => new() { Id = id, Name = name, RootPath = rootPath, IsEnabled = isEnabled, IsDefault = isDefault, RowVersion = [.. rowVersion], CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = CurrentUserId };

    private static RepositoryLocationUpdateRequest CreateUpdateRequest() => new(SecondaryLocationId, "Updated archive", @"E:\MOPR\Archive", true, false, [.. SecondaryRowVersion]);

    private sealed class RepositoryLocationAdministrationServiceScenario
    {
        public RepositoryLocationAdministrationServiceScenario(CurrentUser? currentUser = null, bool useDefaultActiveUser = true)
        {
            var resolvedUser = useDefaultActiveUser && currentUser is null ? new CurrentUser(CurrentUserId, "DOMAIN\\mru", "Marcus", "Runge", "MR", true) : currentUser;
            CurrentUserContext.Setup(context => context.GetCurrentUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(resolvedUser);
            IdentityService.SetupGet(service => service.CurrentUserContext).Returns(CurrentUserContext.Object);
            Application.SetupGet(application => application.IdentityService).Returns(IdentityService.Object);
            Persistence.SetupGet(persistence => persistence.RepositoryLocation).Returns(Repository.Object);
            Service = new RepositoryLocationAdministrationService(Persistence.Object, Application.Object);
        }

        public Mock<IApplication> Application { get; } = new(MockBehavior.Strict);
        public Mock<ICurrentUserContext> CurrentUserContext { get; } = new(MockBehavior.Strict);
        public Mock<IIdentityService> IdentityService { get; } = new(MockBehavior.Strict);
        public Mock<IPersistence> Persistence { get; } = new(MockBehavior.Strict);
        public Mock<IRepositoryLocationRepository> Repository { get; } = new(MockBehavior.Strict);
        public RepositoryLocationAdministrationService Service { get; }
    }
}
