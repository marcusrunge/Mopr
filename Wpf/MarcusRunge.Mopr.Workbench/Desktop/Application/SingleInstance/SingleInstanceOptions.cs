using System;

namespace MarcusRunge.Mopr.Workbench.Application.SingleInstance
{
    internal sealed record SingleInstanceOptions
    {
        public required string MutexName { get; init; }

        public required string PipeName { get; init; }

        public TimeSpan ClientConnectionTimeout { get; init; } = TimeSpan.FromSeconds(5);

        public static SingleInstanceOptions CreateDefault(int sessionId) => new()
        {
            MutexName = @"Global\MOPR.Workbench.SingleInstance",
            PipeName = $"MOPR.Workbench.SingleInstance.Session.{sessionId}"
        };
    }
}