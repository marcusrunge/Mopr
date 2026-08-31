using MarcusRunge.Mopr.Workbench.Application.Administration;
using System;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Administration
{
    public sealed class WindowsAdministrativeAuthorizationServiceTests
    {
        [Fact]
        public void IsElevatedAdministrator_WhenEvaluatorAllowsAccess_ReturnsTrue()
        {
            var service = CreateService(isElevatedAdministrator: true);

            Assert.True(service.IsElevatedAdministrator);
        }

        [Fact]
        public void IsElevatedAdministrator_WhenEvaluatorDeniesAccess_ReturnsFalse()
        {
            var service = CreateService(isElevatedAdministrator: false);

            Assert.False(service.IsElevatedAdministrator);
        }

        [Fact]
        public void DemandElevatedAdministrator_WhenEvaluatorAllowsAccess_Completes()
        {
            var service = CreateService(isElevatedAdministrator: true);

            service.DemandElevatedAdministrator();
        }

        [Fact]
        public void DemandElevatedAdministrator_WhenEvaluatorDeniesAccess_ThrowsUnauthorizedAccessException()
        {
            var service = CreateService(isElevatedAdministrator: false);

            var exception = Assert.Throws<UnauthorizedAccessException>(service.DemandElevatedAdministrator);

            Assert.Equal("Machine-wide MOPR configuration requires an elevated local administrator.", exception.Message);
        }

        private static WindowsAdministrativeAuthorizationService CreateService(bool isElevatedAdministrator) => new(new TestWindowsAdministratorRoleEvaluator(isElevatedAdministrator));

        private sealed class TestWindowsAdministratorRoleEvaluator(bool isElevatedAdministrator) : IWindowsAdministratorRoleEvaluator
        {
            public bool IsElevatedAdministrator { get; } = isElevatedAdministrator;
        }
    }
}