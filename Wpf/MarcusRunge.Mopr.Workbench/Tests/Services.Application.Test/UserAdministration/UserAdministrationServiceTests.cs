using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.UserAdministration;

public sealed class UserAdministrationServiceTests
{
    [Fact]
    public async Task GetAllAsync_WhenUsersExist_ReturnsCurrentUserFirstAndMapsValues()
    {
        var context = new TestContext(currentUserId: 2);
        var inactiveUser = CreateUser(1, "DOMAIN\\inactive", "Inactive", false);
        var currentUser = CreateUser(2, "DOMAIN\\current", "Current", true);
        context.UserRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([inactiveUser, currentUser]);

        var result = await context.Service.GetAllAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Collection(
            result,
            user => { Assert.Equal(2, user.Id); Assert.True(user.IsCurrentUser); Assert.Equal("Current", user.FirstName); },
            user => { Assert.Equal(1, user.Id); Assert.False(user.IsCurrentUser); });
    }

    [Fact]
    public async Task GetAllAsync_WhenCurrentIdentityIsUnavailable_ReturnsUsersWithoutCurrentMarker()
    {
        var context = new TestContext(currentUserId: null);
        context.UserRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([CreateUser(1, "DOMAIN\\user", "User", true)]);

        var result = await context.Service.GetAllAsync(Xunit.TestContext.Current.CancellationToken);

        var user = Assert.Single(result);
        Assert.False(user.IsCurrentUser);
    }

    [Fact]
    public async Task GetAllAsync_WhenUserHasNoRowVersion_ThrowsInvalidOperationException()
    {
        var context = new TestContext();
        var user = CreateUser(1, "DOMAIN\\user", "User", true);
        user.RowVersion = [];
        context.UserRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([user]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.GetAllAsync(Xunit.TestContext.Current.CancellationToken));
    }

    private static User CreateUser(int id, string loginName, string firstName, bool isActive) => new()
    {
        Id = id,
        LoginName = loginName,
        FirstName = firstName,
        LastName = "Test",
        ShortName = firstName,
        IsActive = isActive,
        RowVersion = [1, 2, 3, 4]
    };

    private sealed class TestContext
    {
        internal TestContext(int? currentUserId = 1)
        {
            AuditIdentityProvider.Setup(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(currentUserId);
            Persistence.SetupGet(persistence => persistence.User).Returns(UserRepository.Object);
            Base.SetupGet(value => value.AuditIdentityProvider).Returns(AuditIdentityProvider.Object);
            Base.SetupGet(value => value.Persistence).Returns(Persistence.Object);
            Service = UserAdministrationService.Create(Base.Object, CreationLifetime.Scoped);
        }

        internal Mock<IAuditIdentityProvider> AuditIdentityProvider { get; } = new(MockBehavior.Strict);

        internal Mock<IApplicationBase> Base { get; } = new(MockBehavior.Strict);

        internal Mock<IPersistence> Persistence { get; } = new(MockBehavior.Strict);

        internal IUserAdministrationService Service { get; }

        internal Mock<IUserRepository> UserRepository { get; } = new(MockBehavior.Strict);
    }
}
