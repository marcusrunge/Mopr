using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Test.UserAdministration;

public sealed class UserAdministrationServiceTests
{
    private const int CurrentUserId = 17;
    private const int EditedUserId = 23;
    private static readonly byte[] RowVersion = [1, 2, 3, 4];

    [Fact]
    public async Task UpdateAsync_WhenRequestIsValid_UpdatesAndReturnsCurrentValues()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var user = CreateUser();
        var context = new TestContext();
        context.UserRepository.SetupSequence(repository => repository.GetByIdAsync(EditedUserId, cancellationToken)).ReturnsAsync(user).ReturnsAsync(user);
        context.UserRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync([user]);
        context.UserRepository.Setup(repository => repository.UpdateAsync(user, cancellationToken)).Returns(Task.CompletedTask);

        var result = await context.Service.UpdateAsync(CreateRequest(), cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.User);
        Assert.Equal("Updated", result.User.LastName);
        Assert.Equal("S-1-5-21-2000", result.User.SecurityIdentifier);
        context.UserRepository.Verify(repository => repository.UpdateAsync(user, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAuditIdentityIsUnavailable_ReturnsUnauthorized()
    {
        var context = new TestContext(currentUserId: null);
        var result = await context.Service.UpdateAsync(CreateRequest(), Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(UserAdministrationUpdateStatus.Unauthorized, result.Status);
        context.UserRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var context = new TestContext();
        context.UserRepository.Setup(repository => repository.GetByIdAsync(EditedUserId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var result = await context.Service.UpdateAsync(CreateRequest(), Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(UserAdministrationUpdateStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_WhenRowVersionChanged_ReturnsConflict()
    {
        var context = new TestContext();
        context.UserRepository.Setup(repository => repository.GetByIdAsync(EditedUserId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateUser(rowVersion: [9, 9, 9, 9]));
        var result = await context.Service.UpdateAsync(CreateRequest(), Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(UserAdministrationUpdateStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_WhenSystemUserIsSelected_ReturnsSystemUserProtected()
    {
        var context = new TestContext();
        context.UserRepository.Setup(repository => repository.GetByIdAsync(EditedUserId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateUser(loginName: WellKnownUserLoginNames.System));
        var result = await context.Service.UpdateAsync(CreateRequest(), Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(UserAdministrationUpdateStatus.SystemUserProtected, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_WhenCurrentUserIsDeactivated_ReturnsSelfDeactivationNotAllowed()
    {
        var context = new TestContext(currentUserId: EditedUserId);
        context.UserRepository.Setup(repository => repository.GetByIdAsync(EditedUserId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateUser());
        var result = await context.Service.UpdateAsync(CreateRequest(isActive: false), Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(UserAdministrationUpdateStatus.SelfDeactivationNotAllowed, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_WhenSelfModificationIsDisabled_ReturnsSelfModificationNotAllowed()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var context = new TestContext(currentUserId: EditedUserId);
        context.UserRepository.Setup(repository => repository.GetByIdAsync(EditedUserId, cancellationToken)).ReturnsAsync(CreateUser());
        context.ConfigureSelfModification(allowed: false, cancellationToken);

        var result = await context.Service.UpdateAsync(CreateRequest(), cancellationToken);

        Assert.Equal(UserAdministrationUpdateStatus.SelfModificationNotAllowed, result.Status);
    }

    [Theory]
    [InlineData(@"DOMAIN\Edited", "S-1-5-21-9999", "999", UserAdministrationUpdateStatus.DuplicateLoginName)]
    [InlineData(@"DOMAIN\Other", "S-1-5-21-2000", "999", UserAdministrationUpdateStatus.DuplicateSecurityIdentifier)]
    [InlineData(@"DOMAIN\Other", "S-1-5-21-9999", "200", UserAdministrationUpdateStatus.DuplicatePersonnelNumber)]
    public async Task UpdateAsync_WhenUniqueIdentityValueIsAlreadyAssigned_ReturnsExpectedStatus(string duplicateLoginName, string duplicateSid, string duplicatePersonnelNumber, UserAdministrationUpdateStatus expectedStatus)
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var user = CreateUser();
        var duplicate = CreateUser(id: 99, loginName: duplicateLoginName, securityIdentifier: duplicateSid, personnelNumber: duplicatePersonnelNumber);
        var context = new TestContext();
        context.UserRepository.Setup(repository => repository.GetByIdAsync(EditedUserId, cancellationToken)).ReturnsAsync(user);
        context.UserRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync([user, duplicate]);

        var result = await context.Service.UpdateAsync(CreateRequest(), cancellationToken);

        Assert.Equal(expectedStatus, result.Status);
        context.UserRepository.Verify(repository => repository.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenPersistenceReportsConcurrencyConflict_ReturnsConflict()
    {
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var user = CreateUser();
        var context = new TestContext();
        context.UserRepository.Setup(repository => repository.GetByIdAsync(EditedUserId, cancellationToken)).ReturnsAsync(user);
        context.UserRepository.Setup(repository => repository.GetAllAsync(cancellationToken)).ReturnsAsync([user]);
        context.UserRepository.Setup(repository => repository.UpdateAsync(user, cancellationToken)).ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await context.Service.UpdateAsync(CreateRequest(), cancellationToken);

        Assert.Equal(UserAdministrationUpdateStatus.Conflict, result.Status);
    }

    private static UserAdministrationUpdateRequest CreateRequest(bool isActive = true) => new(EditedUserId, @"DOMAIN\Edited", "Edited", null, "Updated", "ED", null, null, "200", "S-1-5-21-2000", isActive, [.. RowVersion]);

    private static User CreateUser(int id = EditedUserId, string loginName = @"DOMAIN\Edited", string securityIdentifier = "S-1-5-21-2000", string personnelNumber = "200", byte[]? rowVersion = null) => new() { Id = id, LoginName = loginName, FirstName = "Edited", LastName = "User", ShortName = "ED", PersonnelNumber = personnelNumber, SecurityIdentifier = securityIdentifier, IsActive = true, RowVersion = rowVersion ?? [.. RowVersion] };

    private sealed class TestContext
    {
        internal TestContext(int? currentUserId = CurrentUserId)
        {
            AuditIdentityProvider.Setup(provider => provider.GetCurrentUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(currentUserId);
            Persistence.SetupGet(persistence => persistence.User).Returns(UserRepository.Object);
            Base.SetupGet(value => value.AuditIdentityProvider).Returns(AuditIdentityProvider.Object);
            Base.SetupGet(value => value.Persistence).Returns(Persistence.Object);
            Base.SetupGet(value => value.MachineConfigurationService).Returns(MachineConfigurationService.Object);
            Service = UserAdministrationService.Create(Base.Object, CreationLifetime.Scoped);
        }

        internal Mock<IAuditIdentityProvider> AuditIdentityProvider { get; } = new(MockBehavior.Strict);
        internal Mock<IApplicationBase> Base { get; } = new(MockBehavior.Strict);
        internal Mock<IMachineConfigurationService> MachineConfigurationService { get; } = new(MockBehavior.Strict);
        internal Mock<IPersistence> Persistence { get; } = new(MockBehavior.Strict);
        internal IUserAdministrationService Service { get; }
        internal Mock<IUserRepository> UserRepository { get; } = new(MockBehavior.Strict);

        internal void ConfigureSelfModification(bool allowed, CancellationToken cancellationToken)
        {
            var configuration = new Mock<IApplicationConfiguration>(MockBehavior.Strict);
            var security = new Mock<ISecurityConfiguration>(MockBehavior.Strict);
            security.SetupGet(value => value.AllowSelfModification).Returns(allowed);
            configuration.SetupGet(value => value.Security).Returns(security.Object);
            MachineConfigurationService.Setup(service => service.LoadAsync(cancellationToken)).ReturnsAsync(configuration.Object);
        }
    }
}
