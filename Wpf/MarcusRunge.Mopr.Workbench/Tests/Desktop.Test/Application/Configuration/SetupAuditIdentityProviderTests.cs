using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Configuration
{
    public sealed class SetupAuditIdentityProviderTests
    {
        private const string SystemLoginName = @"MOPR\SYSTEM";

        [Fact]
        public async Task GetOrCreateUserIdAsync_WhenSystemUserExists_ReturnsExistingUserId()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupAuditIdentityProviderTestContext();
            var existingUser = CreateSystemUser(42);

            context.UserRepository.Setup(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationToken)).ReturnsAsync(existingUser);

            var userId = await context.Provider.GetOrCreateUserIdAsync(cancellationToken);

            Assert.Equal(existingUser.Id, userId);

            context.UserRepository.Verify(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationToken), Times.Once);

            context.UserRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetOrCreateUserIdAsync_WhenSystemUserDoesNotExist_CreatesAndReturnsSystemUser()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupAuditIdentityProviderTestContext();
            User? createdUser = null;

            context.UserRepository.Setup(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationToken)).ReturnsAsync((User?)null);

            context.UserRepository.Setup(repository => repository.AddAsync(It.IsAny<User>(), cancellationToken)).Callback<User, CancellationToken>((user, _) =>
            {
                user.Id = 73;
                createdUser = user;
            }).Returns(Task.CompletedTask);

            var userId = await context.Provider.GetOrCreateUserIdAsync(cancellationToken);

            Assert.Equal(73, userId);
            Assert.NotNull(createdUser);
            Assert.Equal("MOPR", createdUser.FirstName);
            Assert.Equal("System", createdUser.LastName);
            Assert.Equal(SystemLoginName, createdUser.LoginName);
            Assert.Equal("SYSTEM", createdUser.ShortName);

            context.UserRepository.Verify(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationToken), Times.Once);

            context.UserRepository.Verify(repository => repository.AddAsync(It.Is<User>(user => user.FirstName == "MOPR" && user.LastName == "System" && user.LoginName == SystemLoginName && user.ShortName == "SYSTEM"), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetOrCreateUserIdAsync_WhenConcurrentCreationWins_ReturnsConcurrentlyCreatedUserId()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupAuditIdentityProviderTestContext();
            var concurrentlyCreatedUser = CreateSystemUser(91);
            var lookupCount = 0;

            context.UserRepository.Setup(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationToken)).ReturnsAsync(() =>
            {
                lookupCount++;
                return lookupCount == 1 ? null : concurrentlyCreatedUser;
            });

            context.UserRepository.Setup(repository => repository.AddAsync(It.IsAny<User>(), cancellationToken)).ThrowsAsync(new InvalidOperationException("The login name already exists."));

            var userId = await context.Provider.GetOrCreateUserIdAsync(cancellationToken);

            Assert.Equal(concurrentlyCreatedUser.Id, userId);

            context.UserRepository.Verify(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationToken), Times.Exactly(2));

            context.UserRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetOrCreateUserIdAsync_WhenCreationAndVerificationFail_PropagatesOriginalCreationException()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupAuditIdentityProviderTestContext();
            var creationException = new InvalidOperationException("The system user could not be created.");
            var lookupCount = 0;

            context.UserRepository.Setup(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationToken)).ReturnsAsync(() =>
            {
                lookupCount++;

                if (lookupCount == 1)
                {
                    return null;
                }

                throw new InvalidOperationException("The verification lookup failed.");
            });

            context.UserRepository.Setup(repository => repository.AddAsync(It.IsAny<User>(), cancellationToken)).ThrowsAsync(creationException);

            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Provider.GetOrCreateUserIdAsync(cancellationToken));

            Assert.Same(creationException, actualException);

            context.UserRepository.Verify(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationToken), Times.Exactly(2));

            context.UserRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetOrCreateUserIdAsync_WhenExistingUserHasInvalidId_ThrowsInvalidOperationException()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupAuditIdentityProviderTestContext();
            var existingUser = CreateSystemUser(0);

            context.UserRepository.Setup(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationToken)).ReturnsAsync(existingUser);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Provider.GetOrCreateUserIdAsync(cancellationToken));

            Assert.Contains("valid persistent identifier", exception.Message, StringComparison.OrdinalIgnoreCase);

            context.UserRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetOrCreateUserIdAsync_WhenCreatedUserHasInvalidId_ThrowsInvalidOperationException()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupAuditIdentityProviderTestContext();

            context.UserRepository.Setup(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationToken)).ReturnsAsync((User?)null);

            context.UserRepository.Setup(repository => repository.AddAsync(It.IsAny<User>(), cancellationToken)).Returns(Task.CompletedTask);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Provider.GetOrCreateUserIdAsync(cancellationToken));

            Assert.Contains("valid persistent identifier", exception.Message, StringComparison.OrdinalIgnoreCase);

            context.UserRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetOrCreateUserIdAsync_WhenUserRepositoryIsUnavailable_ThrowsInvalidOperationException()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var persistence = new Mock<IPersistence>(MockBehavior.Strict);

            persistence.SetupGet(instance => instance.User).Returns((IUserRepository?)null);

            var provider = new SetupAuditIdentityProvider(persistence.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetOrCreateUserIdAsync(cancellationToken));

            Assert.Contains("user repository is not available", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetOrCreateUserIdAsync_WhenCanceledBeforeExecution_PropagatesCancellation()
        {
            var context = new SetupAuditIdentityProviderTestContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            cancellationSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Provider.GetOrCreateUserIdAsync(cancellationSource.Token));

            context.Persistence.VerifyGet(instance => instance.User, Times.Never);

            context.UserRepository.Verify(repository => repository.GetByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            context.UserRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetOrCreateUserIdAsync_WhenLookupIsCanceled_PropagatesCancellation()
        {
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            var context = new SetupAuditIdentityProviderTestContext();

            context.UserRepository.Setup(repository => repository.GetByLoginNameAsync(SystemLoginName, cancellationSource.Token)).Returns(async () =>
            {
                cancellationSource.Cancel();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationSource.Token);
                return null;
            });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Provider.GetOrCreateUserIdAsync(cancellationSource.Token));

            context.UserRepository.Verify(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static User CreateSystemUser(int id) => new()
        {
            Id = id,
            FirstName = "MOPR",
            LastName = "System",
            LoginName = SystemLoginName,
            ShortName = "SYSTEM"
        };

        private sealed class SetupAuditIdentityProviderTestContext
        {
            public SetupAuditIdentityProviderTestContext()
            {
                Persistence.SetupGet(instance => instance.User).Returns(UserRepository.Object);

                Provider = new SetupAuditIdentityProvider(Persistence.Object);
            }

            public Mock<IPersistence> Persistence { get; } = new(MockBehavior.Strict);

            public SetupAuditIdentityProvider Provider { get; }

            public Mock<IUserRepository> UserRepository { get; } = new(MockBehavior.Strict);
        }
    }
}