using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Protects machine-wide MOPR configuration with Windows DPAPI and language-independent Windows SIDs.
    /// </summary>
    internal sealed class MachineConfigurationProtectionService : IMachineConfigurationProtectionService
    {
        private static readonly SecurityIdentifier AdministratorsSid = new(WellKnownSidType.BuiltinAdministratorsSid, null);

        private static readonly SecurityIdentifier AuthenticatedUsersSid = new(WellKnownSidType.AuthenticatedUserSid, null);

        private static readonly byte[] ConfigurationEntropy = Encoding.UTF8.GetBytes("MOPR.Workbench.MachineConfiguration.v1");

        private static readonly SecurityIdentifier SystemSid = new(WellKnownSidType.LocalSystemSid, null);

        /// <inheritdoc/>
        public byte[] ProtectData(byte[] unprotectedData)
        {
            ArgumentNullException.ThrowIfNull(unprotectedData);

            if (unprotectedData.Length == 0)
            {
                throw new ArgumentException("The machine configuration data must not be empty.", nameof(unprotectedData));
            }

            // LocalMachine scope allows every authorized MOPR user on this
            // workstation to load the shared configuration while binding the
            // protected payload to the Windows machine that created it.
            return ProtectedData.Protect(unprotectedData, ConfigurationEntropy, DataProtectionScope.LocalMachine);
        }

        /// <inheritdoc/>
        public void ProtectDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("The configuration directory path must not be empty.", nameof(directoryPath));
            }

            var security = new DirectorySecurity();

            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            security.SetOwner(AdministratorsSid);
            security.AddAccessRule(CreateDirectoryFullControlRule(SystemSid));
            security.AddAccessRule(CreateDirectoryFullControlRule(AdministratorsSid));
            security.AddAccessRule(CreateDirectoryReadRule(AuthenticatedUsersSid));

            new DirectoryInfo(directoryPath).SetAccessControl(security);
        }

        /// <inheritdoc/>
        public void ProtectFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("The configuration file path must not be empty.", nameof(filePath));
            }

            var security = new FileSecurity();

            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            security.SetOwner(AdministratorsSid);

            security.AddAccessRule(new FileSystemAccessRule(SystemSid, FileSystemRights.FullControl, AccessControlType.Allow));

            security.AddAccessRule(new FileSystemAccessRule(AdministratorsSid, FileSystemRights.FullControl, AccessControlType.Allow));

            security.AddAccessRule(new FileSystemAccessRule(AuthenticatedUsersSid, FileSystemRights.ReadAndExecute | FileSystemRights.Read, AccessControlType.Allow));

            new FileInfo(filePath).SetAccessControl(security);
        }

        /// <inheritdoc/>
        public byte[] UnprotectData(byte[] protectedData)
        {
            ArgumentNullException.ThrowIfNull(protectedData);

            if (protectedData.Length == 0)
            {
                throw new ArgumentException("The protected machine configuration data must not be empty.", nameof(protectedData));
            }

            return ProtectedData.Unprotect(protectedData, ConfigurationEntropy, DataProtectionScope.LocalMachine);
        }

        private static FileSystemAccessRule CreateDirectoryFullControlRule(IdentityReference identity) => new(identity, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);

        private static FileSystemAccessRule CreateDirectoryReadRule(IdentityReference identity) => new(identity, FileSystemRights.ReadAndExecute | FileSystemRights.ListDirectory | FileSystemRights.Read, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
    }
}