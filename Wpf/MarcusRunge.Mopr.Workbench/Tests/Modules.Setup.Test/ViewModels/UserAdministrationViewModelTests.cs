using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.Test.ViewModels;

public sealed class UserAdministrationViewModelTests
{
    private const int CurrentUserId = 2;
    private const int OtherUserId = 3;
    private const int SystemUserId = 1;

    [Fact]
    public async Task Constructor_WhenUsersAreLoaded_SelectsCurrentUserAndAppliesEditorValues()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateUsers());

        await context.WaitUntilLoadedAsync(cancellationToken);

        Assert.Equal(3, context.ViewModel.Users.Count);
        Assert.NotNull(context.ViewModel.SelectedUser);
        Assert.Equal(CurrentUserId, context.ViewModel.SelectedUser.Id);
        Assert.Equal("Current", context.ViewModel.FirstName);
        Assert.Equal(@"DOMAIN\current", context.ViewModel.LoginName);
        Assert.True(context.ViewModel.IsActive);
        Assert.False(context.ViewModel.IsDirty);
    }

    [Fact]
    public async Task Constructor_WhenNoUsersExist_LeavesSelectionEmpty()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext([]);

        await context.WaitUntilLoadedAsync(cancellationToken);

        Assert.Empty(context.ViewModel.Users);
        Assert.Null(context.ViewModel.SelectedUser);
        Assert.False(context.ViewModel.HasSelectedUser);
        Assert.False(context.ViewModel.SaveCommand.CanExecute());
    }

    [Fact]
    public async Task EditorValue_WhenChanged_SetsDirtyStateAndBlocksSelection()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateUsers());

        await context.WaitUntilLoadedAsync(cancellationToken);

        context.ViewModel.FirstName = "Changed";

        Assert.True(context.ViewModel.IsDirty);
        Assert.False(context.ViewModel.CanSelectUser);
        Assert.True(context.ViewModel.DiscardCommand.CanExecute());
        Assert.True(context.ViewModel.SaveCommand.CanExecute());

        var originalSelection = context.ViewModel.SelectedUser;
        context.ViewModel.SelectedUser = context.ViewModel.Users.Single(user => user.Id == OtherUserId);
        Assert.Same(originalSelection, context.ViewModel.SelectedUser);
    }

    [Fact]
    public async Task DiscardCommand_WhenExecuted_RestoresPersistedValues()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateUsers());

        await context.WaitUntilLoadedAsync(cancellationToken);

        context.ViewModel.FirstName = "Changed";
        context.ViewModel.PersonnelNumber = "999";
        context.ViewModel.DiscardCommand.Execute();

        Assert.Equal("Current", context.ViewModel.FirstName);
        Assert.Equal("200", context.ViewModel.PersonnelNumber);
        Assert.False(context.ViewModel.IsDirty);
        Assert.Equal(Resources.UserAdministration_ChangesDiscarded, context.ViewModel.StatusText);
    }

    [Fact]
    public async Task IsActive_WhenCurrentUserIsDeactivated_IgnoresChange()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateUsers());

        await context.WaitUntilLoadedAsync(cancellationToken);

        context.ViewModel.IsActive = false;

        Assert.True(context.ViewModel.IsActive);
        Assert.False(context.ViewModel.IsDirty);
        Assert.False(context.ViewModel.CanChangeActivation);
    }

    [Fact]
    public async Task SystemUser_WhenSelected_DisablesEditorAndSaveCommand()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateUsers());

        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.SelectedUser = context.ViewModel.Users.Single(user => user.Id == SystemUserId);

        Assert.True(context.ViewModel.SelectedUser.IsSystemUser);
        Assert.False(context.ViewModel.IsEditorEnabled);
        Assert.False(context.ViewModel.CanChangeActivation);
        Assert.False(context.ViewModel.SaveCommand.CanExecute());
    }

    [Fact]
    public async Task SaveCommand_WhenUpdateSucceeds_ReloadsAndKeepsUpdatedUserSelected()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var updatedUser = CreateSummary(CurrentUserId, @"DOMAIN\current", isCurrentUser: true, firstName: "Updated", rowVersion: [9, 9, 9, 9]);
        var context = new TestContext(CreateUsers(), [CreateSystemSummary(), updatedUser, CreateOtherSummary()]);
        context.UserAdministration.Setup(service => service.UpdateAsync(It.IsAny<UserAdministrationUpdateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(UserAdministrationUpdateResult.Success(updatedUser));

        await context.WaitUntilLoadedAsync(cancellationToken);

        context.ViewModel.FirstName = "Updated";
        context.ViewModel.SaveCommand.Execute();

        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.UserAdministration_Saved, cancellationToken);

        Assert.NotNull(context.ViewModel.SelectedUser);
        Assert.Equal(CurrentUserId, context.ViewModel.SelectedUser.Id);
        Assert.Equal("Updated", context.ViewModel.FirstName);
        Assert.False(context.ViewModel.IsDirty);
        context.UserAdministration.Verify(service => service.UpdateAsync(It.Is<UserAdministrationUpdateRequest>(request => request.Id == CurrentUserId && request.FirstName == "Updated" && request.RowVersion.SequenceEqual(new byte[] { 1, 2, 3, CurrentUserId })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_WhenUpdateConflicts_ShowsLocalizedStatusAndKeepsChanges()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateUsers());
        context.UserAdministration.Setup(service => service.UpdateAsync(It.IsAny<UserAdministrationUpdateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(UserAdministrationUpdateResult.Conflict());

        await context.WaitUntilLoadedAsync(cancellationToken);

        context.ViewModel.LastName = "Changed";
        context.ViewModel.SaveCommand.Execute();

        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.UserAdministration_Conflict, cancellationToken);

        Assert.True(context.ViewModel.IsDirty);
        Assert.Equal("Changed", context.ViewModel.LastName);
    }

    [Fact]
    public async Task ReloadCommand_WhenExecuted_ReloadsCurrentSelection()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var reloadedCurrentUser = CreateSummary(CurrentUserId, @"DOMAIN\current", isCurrentUser: true, firstName: "Reloaded", rowVersion: [8, 8, 8, 8]);
        var context = new TestContext(CreateUsers(), [CreateSystemSummary(), reloadedCurrentUser, CreateOtherSummary()]);

        await context.WaitUntilLoadedAsync(cancellationToken);

        context.ViewModel.ReloadCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.FirstName == "Reloaded", cancellationToken);

        Assert.Equal(CurrentUserId, context.ViewModel.SelectedUser?.Id);
        Assert.False(context.ViewModel.IsDirty);
    }


    [Fact]
    public async Task NewCommand_WhenExecuted_EntersCreationMode()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateUsers());
        await context.WaitUntilLoadedAsync(cancellationToken);

        context.ViewModel.NewCommand.Execute();

        Assert.True(context.ViewModel.IsCreating);
        Assert.Null(context.ViewModel.SelectedUser);
        Assert.True(context.ViewModel.IsActive);
        Assert.True(context.ViewModel.IsEditorEnabled);
        Assert.False(context.ViewModel.CanSelectUser);
        Assert.True(context.ViewModel.CancelCreateCommand.CanExecute());
        Assert.Equal(Resources.UserAdministration_NewInstructions, context.ViewModel.StatusText);
    }

    [Fact]
    public async Task CancelCreateCommand_WhenExecuted_RestoresCurrentUser()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(CreateUsers());
        await context.WaitUntilLoadedAsync(cancellationToken);

        context.ViewModel.NewCommand.Execute();
        context.ViewModel.FirstName = "Temporary";
        context.ViewModel.CancelCreateCommand.Execute();

        Assert.False(context.ViewModel.IsCreating);
        Assert.Equal(CurrentUserId, context.ViewModel.SelectedUser?.Id);
        Assert.Equal("Current", context.ViewModel.FirstName);
        Assert.False(context.ViewModel.IsDirty);
        Assert.Equal(Resources.UserAdministration_CreationCanceled, context.ViewModel.StatusText);
    }

    [Fact]
    public async Task SaveCommand_WhenCreationSucceeds_ReloadsAndSelectsCreatedUser()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var createdUser = CreateSummary(4, @"DOMAIN\new", isCurrentUser: false, firstName: "New", rowVersion: [9, 8, 7, 6]);
        var context = new TestContext(CreateUsers(), [.. CreateUsers(), createdUser]);
        context.UserAdministration.Setup(service => service.CreateAsync(It.IsAny<UserAdministrationCreateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(UserAdministrationCreateResult.Success(createdUser));
        await context.WaitUntilLoadedAsync(cancellationToken);

        context.ViewModel.NewCommand.Execute();
        context.ViewModel.LoginName = @"DOMAIN\new";
        context.ViewModel.FirstName = "New";
        context.ViewModel.LastName = "User";
        context.ViewModel.ShortName = "NEW";
        context.ViewModel.SaveCommand.Execute();

        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.UserAdministration_Created, cancellationToken);

        Assert.False(context.ViewModel.IsCreating);
        Assert.Equal(4, context.ViewModel.SelectedUser?.Id);
        Assert.Equal("New", context.ViewModel.FirstName);
        Assert.False(context.ViewModel.IsDirty);
        context.UserAdministration.Verify(service => service.CreateAsync(It.Is<UserAdministrationCreateRequest>(request => request.LoginName == @"DOMAIN\new" && request.FirstName == "New" && request.LastName == "User" && request.ShortName == "NEW" && request.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IReadOnlyList<UserAdministrationSummary> CreateUsers() => [CreateSystemSummary(), CreateSummary(CurrentUserId, @"DOMAIN\current", isCurrentUser: true, firstName: "Current"), CreateOtherSummary()];

    private static UserAdministrationSummary CreateSystemSummary() => CreateSummary(SystemUserId, WellKnownUserLoginNames.System, isCurrentUser: false, firstName: "MOPR", lastName: "System", shortName: "SYSTEM", personnelNumber: string.Empty, securityIdentifier: string.Empty);

    private static UserAdministrationSummary CreateOtherSummary() => CreateSummary(OtherUserId, @"DOMAIN\other", isCurrentUser: false, firstName: "Other", personnelNumber: "300");

    private static UserAdministrationSummary CreateSummary(int id, string loginName, bool isCurrentUser, string firstName, string lastName = "User", string shortName = "USR", string personnelNumber = "200", string securityIdentifier = "S-1-5-21-2000", byte[]? rowVersion = null) => new(id, loginName, firstName, string.Empty, lastName, shortName, true, isCurrentUser, string.Empty, string.Empty, personnelNumber, securityIdentifier, rowVersion ?? [1, 2, 3, (byte)id]);

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTime.UtcNow >= timeoutAt) throw new TimeoutException("The expected UserAdministrationViewModel state was not reached.");
            await Task.Delay(10, cancellationToken);
        }
    }

    private sealed class TestContext
    {
        internal TestContext(IReadOnlyList<UserAdministrationSummary> users, IReadOnlyList<UserAdministrationSummary>? reloadedUsers = null)
        {
            LifetimeService.SetupGet(service => service.ApplicationStopping).Returns(CancellationToken.None);
            if (reloadedUsers is null) UserAdministration.Setup(service => service.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);
            else UserAdministration.SetupSequence(service => service.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users).ReturnsAsync(reloadedUsers);
            Application.SetupGet(application => application.UserAdministration).Returns(UserAdministration.Object);
            ViewModel = new UserAdministrationViewModel(Application.Object, LifetimeService.Object);
        }

        internal Mock<IApplication> Application { get; } = new(MockBehavior.Strict);
        internal Mock<ILifetimeService> LifetimeService { get; } = new(MockBehavior.Strict);
        internal Mock<IUserAdministrationService> UserAdministration { get; } = new(MockBehavior.Strict);
        internal UserAdministrationViewModel ViewModel { get; }

        internal async Task WaitUntilLoadedAsync(CancellationToken cancellationToken) => await WaitUntilAsync(() => !ViewModel.IsBusy, cancellationToken);
    }
}
