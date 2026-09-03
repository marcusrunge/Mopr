using MarcusRunge.Mopr.Workbench.Application.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Configuration
{
    public sealed class MachineConfigurationProtectionServiceTests
    {
        [Fact]
        public void ProtectData_WithConfigurationData_ReturnsDifferentProtectedData()
        {
            var service = new MachineConfigurationProtectionService();
            var unprotectedData = Encoding.UTF8.GetBytes("""{"ConnectionString":"Server=localhost;User Id=mopr;Password=Secret;"}""");

            var protectedData = service.ProtectData(unprotectedData);

            Assert.NotEmpty(protectedData);
            Assert.False(unprotectedData.AsSpan().SequenceEqual(protectedData));
            Assert.DoesNotContain("Password=Secret", Convert.ToBase64String(protectedData), StringComparison.Ordinal);
        }

        [Fact]
        public void ProtectAndUnprotectData_WithConfigurationData_PreservesOriginalData()
        {
            var service = new MachineConfigurationProtectionService();
            var unprotectedData = Encoding.UTF8.GetBytes("""{"ConnectionString":"Server=localhost;Database=Mopr;User Id=mopr;Password=Secret;"}""");

            var protectedData = service.ProtectData(unprotectedData);
            var roundTripData = service.UnprotectData(protectedData);

            Assert.Equal(unprotectedData, roundTripData);
        }

        [Fact]
        public void ProtectData_WithSameConfigurationData_ProducesIndependentProtectedPayloads()
        {
            var service = new MachineConfigurationProtectionService();
            var unprotectedData = Encoding.UTF8.GetBytes("""{"SetupVersion":1,"IsSetupComplete":true}""");

            var firstProtectedData = service.ProtectData(unprotectedData);
            var secondProtectedData = service.ProtectData(unprotectedData);

            Assert.False(firstProtectedData.AsSpan().SequenceEqual(secondProtectedData));
            Assert.Equal(unprotectedData, service.UnprotectData(firstProtectedData));
            Assert.Equal(unprotectedData, service.UnprotectData(secondProtectedData));
        }

        [Fact]
        public void ProtectData_WithNullData_ThrowsArgumentNullException()
        {
            var service = new MachineConfigurationProtectionService();

            var exception = Assert.Throws<ArgumentNullException>(() => service.ProtectData(null!));

            Assert.Equal("unprotectedData", exception.ParamName);
        }

        [Fact]
        public void ProtectData_WithEmptyData_ThrowsArgumentException()
        {
            var service = new MachineConfigurationProtectionService();

            var exception = Assert.Throws<ArgumentException>(() => service.ProtectData([]));

            Assert.Equal("unprotectedData", exception.ParamName);
        }

        [Fact]
        public void UnprotectData_WithNullData_ThrowsArgumentNullException()
        {
            var service = new MachineConfigurationProtectionService();

            var exception = Assert.Throws<ArgumentNullException>(() => service.UnprotectData(null!));

            Assert.Equal("protectedData", exception.ParamName);
        }

        [Fact]
        public void UnprotectData_WithEmptyData_ThrowsArgumentException()
        {
            var service = new MachineConfigurationProtectionService();

            var exception = Assert.Throws<ArgumentException>(() => service.UnprotectData([]));

            Assert.Equal("protectedData", exception.ParamName);
        }

        [Fact]
        public void UnprotectData_WithInvalidProtectedData_ThrowsCryptographicException()
        {
            var service = new MachineConfigurationProtectionService();
            var invalidProtectedData = Encoding.UTF8.GetBytes("This is not a Windows DPAPI payload.");

            Assert.Throws<CryptographicException>(() => service.UnprotectData(invalidProtectedData));
        }

        [Fact]
        public void UnprotectData_WithTamperedProtectedData_ThrowsCryptographicException()
        {
            var service = new MachineConfigurationProtectionService();
            var unprotectedData = Encoding.UTF8.GetBytes("""{"SetupVersion":1,"IsSetupComplete":true}""");
            var protectedData = service.ProtectData(unprotectedData);

            protectedData[protectedData.Length / 2] ^= 0x5A;

            Assert.Throws<CryptographicException>(() => service.UnprotectData(protectedData));
        }
    }
}