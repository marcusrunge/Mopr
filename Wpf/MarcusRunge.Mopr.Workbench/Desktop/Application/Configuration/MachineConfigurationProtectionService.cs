using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Protects machine-wide MOPR configuration with language-independent Windows SIDs.
    /// </summary>
    internal sealed class MachineConfigurationProtectionService : IMachineConfigurationProtectionService
    {
        private static readonly SecurityIdentifier AdministratorsSid = new(WellKnownSidType.BuiltinAdministratorsSid, null);
        private static readonly SecurityIdentifier AuthenticatedUsersSid = new(WellKnownSidType.AuthenticatedUserSid, null);
        private static readonly SecurityIdentifier SystemSid = new(WellKnownSidType.LocalSystemSid, null);

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

        private static FileSystemAccessRule CreateDirectoryFullControlRule(IdentityReference identity) =>
            new(identity, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);

        private static FileSystemAccessRule CreateDirectoryReadRule(IdentityReference identity) =>
            new(identity, FileSystemRights.ReadAndExecute | FileSystemRights.ListDirectory | FileSystemRights.Read, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
    }
}