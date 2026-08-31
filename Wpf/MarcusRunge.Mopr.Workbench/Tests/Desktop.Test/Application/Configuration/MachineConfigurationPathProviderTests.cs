using MarcusRunge.Mopr.Workbench.Application.Configuration;
using System;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Configuration
{
    public sealed class MachineConfigurationPathProviderTests
    {
        [Fact]
        public void Constructor_CreatesExpectedMachineWidePaths()
        {
            var provider = new MachineConfigurationPathProvider(@"C:\ProgramData");

            Assert.Equal(@"C:\ProgramData\MOPR\Configuration", provider.ConfigurationDirectoryPath);
            Assert.Equal(@"C:\ProgramData\MOPR\Configuration\application.json", provider.ConfigurationFilePath);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public void Constructor_WithEmptyProgramDataPath_ThrowsArgumentException(string programDataPath) =>
            Assert.Throws<ArgumentException>(() => new MachineConfigurationPathProvider(programDataPath));
    }
}