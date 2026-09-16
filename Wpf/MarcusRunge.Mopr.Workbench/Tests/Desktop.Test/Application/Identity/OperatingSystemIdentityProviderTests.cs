using MarcusRunge.Mopr.Workbench.Application.Identity;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Identity
{
    public sealed class OperatingSystemIdentityProviderTests
    {
        [Fact]
        public async Task GetCurrentIdentityAsync_WhenLoginNameIsAvailable_ReturnsOperatingSystemIdentity()
        {
            var accessor = new Mock<IWindowsIdentityNameAccessor>(MockBehavior.Strict);
            accessor.Setup(x => x.GetCurrentLoginName()).Returns(@"DOMAIN\User");
            var provider = new OperatingSystemIdentityProvider(accessor.Object);

            OperatingSystemIdentity? result = await provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.Equal(@"DOMAIN\User", result.LoginName);
            accessor.Verify(x => x.GetCurrentLoginName(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentIdentityAsync_WhenLoginNameContainsOuterWhitespace_ReturnsTrimmedIdentity()
        {
            var accessor = new Mock<IWindowsIdentityNameAccessor>(MockBehavior.Strict);
            accessor.Setup(x => x.GetCurrentLoginName()).Returns(@"  DOMAIN\User  ");
            var provider = new OperatingSystemIdentityProvider(accessor.Object);

            OperatingSystemIdentity? result = await provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.Equal(@"DOMAIN\User", result.LoginName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public async Task GetCurrentIdentityAsync_WhenLoginNameIsUnavailable_ReturnsNull(string? loginName)
        {
            var accessor = new Mock<IWindowsIdentityNameAccessor>(MockBehavior.Strict);
            accessor.Setup(x => x.GetCurrentLoginName()).Returns(loginName);
            var provider = new OperatingSystemIdentityProvider(accessor.Object);

            OperatingSystemIdentity? result = await provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCurrentIdentityAsync_WhenCanceled_DoesNotAccessWindowsIdentity()
        {
            var accessor = new Mock<IWindowsIdentityNameAccessor>(MockBehavior.Strict);
            var provider = new OperatingSystemIdentityProvider(accessor.Object);
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => provider.GetCurrentIdentityAsync(cancellation.Token));

            accessor.Verify(x => x.GetCurrentLoginName(), Times.Never);
        }

        [Fact]
        public async Task GetCurrentIdentityAsync_WhenWindowsIdentityAccessFails_PropagatesException()
        {
            var accessor = new Mock<IWindowsIdentityNameAccessor>(MockBehavior.Strict);
            accessor.Setup(x => x.GetCurrentLoginName()).Throws<InvalidOperationException>();
            var provider = new OperatingSystemIdentityProvider(accessor.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetCurrentIdentityAsync(TestContext.Current.CancellationToken));
        }
    }
}