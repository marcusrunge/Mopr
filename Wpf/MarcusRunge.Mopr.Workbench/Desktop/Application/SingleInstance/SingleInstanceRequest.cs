using System;

namespace MarcusRunge.Mopr.Workbench.Application.SingleInstance
{
    internal sealed record SingleInstanceRequest(string[] Arguments)
    {
        internal static SingleInstanceRequest Create(string[] arguments) => new(arguments ?? Array.Empty<string>());
    }
}