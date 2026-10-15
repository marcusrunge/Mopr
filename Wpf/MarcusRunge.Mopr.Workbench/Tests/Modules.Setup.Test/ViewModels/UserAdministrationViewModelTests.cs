using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.Test.ViewModels;

public sealed class UserAdministrationViewModelTests
{
    [Fact]
    public async Task Constructor_WhenUsersAreLoaded_SelectsCurrentUser()
    {
        var context = new TestContext([
            CreateSummary(1, "DOMAIN\\other", false),
            CreateSummary(2, "DOMAIN\\current", true)]);

        await context.WaitUntilLoadedAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(2, context.ViewModel.Users.Count);
        Assert.NotNull(context.ViewModel.SelectedUser);
        Assert.Equal(2, context.ViewModel.SelectedUser.Id);
        Assert.True(context.ViewModel.SelectedUser.IsCurrentUser);
    }

    [Fact]
    public async Task Constructor_WhenNoUsersExist_LeavesSelectionEmpty()
    {
        var context = new TestContext([]);

        await context.WaitUntilLoadedAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Empty(context.ViewModel.Users);
        Assert.Null(context.ViewModel.SelectedUser);
        Assert.False(context.ViewModel.HasSelectedUser);
    }

    private static UserAdministrationSummary CreateSummary(int id, string loginName, bool isCurrentUser) => new(id, loginName, $"First{id}", string.Empty, $"Last{id}", $"U{id}", true, isCurrentUser, string.Empty, string.Empty, string.Empty, $"S-1-5-21-{id}", [1, 2, 3, (byte)id]);

    private sealed class TestContext
    {
        internal TestContext(IReadOnlyList<UserAdministrationSummary> users)
        {
            LifetimeService.SetupGet(service => service.ApplicationStopping).Returns(CancellationToken.None);
            UserAdministration.Setup(service => service.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);
            Application.SetupGet(application => application.UserAdministration).Returns(UserAdministration.Object);
            ViewModel = new UserAdministrationViewModel(Application.Object, LifetimeService.Object);
        }

        internal Mock<IApplication> Application { get; } = new(MockBehavior.Strict);

        internal Mock<ILifetimeService> LifetimeService { get; } = new(MockBehavior.Strict);

        internal Mock<IUserAdministrationService> UserAdministration { get; } = new(MockBehavior.Strict);

        internal UserAdministrationViewModel ViewModel { get; }

        internal async Task WaitUntilLoadedAsync(CancellationToken cancellationToken)
        {
            var timeoutAt = DateTime.UtcNow.AddSeconds(5);
            while (ViewModel.IsBusy)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (DateTime.UtcNow >= timeoutAt) throw new TimeoutException("The expected UserAdministrationViewModel state was not reached.");
                await Task.Delay(10, cancellationToken);
            }
        }
    }
}
