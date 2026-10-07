using MarcusRunge.Mopr.Workbench.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Identity
{
    public sealed class OperatingSystemIdentityProviderTests
    {
        private const string LoginName = @"DOMAIN\User";
        private const string SecurityIdentifier = "S-1-5-21-1000000000-2000000000-3000000000-1001";

        [Fact]
        public async Task GetCurrentIdentityAsync_WhenWindowsIdentityIsAvailable_ReturnsOperatingSystemIdentity()
        {
            var accessor = new Mock<IWindowsIdentityAccessor>(MockBehavior.Strict);
            accessor.Setup(value => value.GetCurrentIdentity()).Returns(new WindowsIdentityInfo(LoginName, SecurityIdentifier));
            var provider = new OperatingSystemIdentityProvider(accessor.Object);

            var result = await provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.Equal(LoginName, result.LoginName);
            accessor.Verify(value => value.GetCurrentIdentity(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentIdentityAsync_WhenLoginNameContainsOuterWhitespace_ReturnsTrimmedIdentity()
        {
            var accessor = new Mock<IWindowsIdentityAccessor>(MockBehavior.Strict);
            accessor.Setup(value => value.GetCurrentIdentity()).Returns(new WindowsIdentityInfo($"  {LoginName}  ", SecurityIdentifier));
            var provider = new OperatingSystemIdentityProvider(accessor.Object);

            var result = await provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.Equal(LoginName, result.LoginName);
            accessor.Verify(value => value.GetCurrentIdentity(), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public async Task GetCurrentIdentityAsync_WhenLoginNameIsUnavailable_ReturnsNull(string? loginName)
        {
            var accessor = new Mock<IWindowsIdentityAccessor>(MockBehavior.Strict);
            accessor.Setup(value => value.GetCurrentIdentity()).Returns(new WindowsIdentityInfo(loginName, SecurityIdentifier));
            var provider = new OperatingSystemIdentityProvider(accessor.Object);

            var result = await provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken);

            Assert.Null(result);
            accessor.Verify(value => value.GetCurrentIdentity(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentIdentityAsync_WhenWindowsIdentityIsUnavailable_ReturnsNull()
        {
            var accessor = new Mock<IWindowsIdentityAccessor>(MockBehavior.Strict);
            accessor.Setup(value => value.GetCurrentIdentity()).Returns((WindowsIdentityInfo?)null);
            var provider = new OperatingSystemIdentityProvider(accessor.Object);

            var result = await provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken);

            Assert.Null(result);
            accessor.Verify(value => value.GetCurrentIdentity(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentIdentityAsync_WhenCanceled_DoesNotAccessWindowsIdentity()
        {
            var accessor = new Mock<IWindowsIdentityAccessor>(MockBehavior.Strict);
            var provider = new OperatingSystemIdentityProvider(accessor.Object);
            using var cancellation = new CancellationTokenSource();

            await cancellation.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => provider.GetCurrentIdentityAsync(cancellation.Token));

            accessor.Verify(value => value.GetCurrentIdentity(), Times.Never);
        }

        [Fact]
        public async Task GetCurrentIdentityAsync_WhenWindowsIdentityAccessFails_PropagatesException()
        {
            var accessor = new Mock<IWindowsIdentityAccessor>(MockBehavior.Strict);
            accessor.Setup(value => value.GetCurrentIdentity()).Throws<InvalidOperationException>();
            var provider = new OperatingSystemIdentityProvider(accessor.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken));

            accessor.Verify(value => value.GetCurrentIdentity(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentIdentityAsync_WhenSecurityIdentifierIsMissing_ReturnsNull()
        {
            var accessor = new Mock<IWindowsIdentityAccessor>(MockBehavior.Strict);
            accessor.Setup(value => value.GetCurrentIdentity()).Returns(new WindowsIdentityInfo(LoginName, securityIdentifier: null));

            var provider = new OperatingSystemIdentityProvider(accessor.Object);

            var result = await provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken);

            Assert.Null(result);
            accessor.Verify(value => value.GetCurrentIdentity(), Times.Once);
        }
    }
}