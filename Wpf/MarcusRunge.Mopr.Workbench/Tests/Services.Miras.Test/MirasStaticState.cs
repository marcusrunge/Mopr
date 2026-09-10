using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using System.Reflection;
using MirasFlow = MarcusRunge.Mopr.Workbench.Services.Miras.Implementations.Flow;
using MirasOperations = MarcusRunge.Mopr.Workbench.Services.Miras.Implementations.Operations;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    /// <summary>
    /// Resets the static CreateableBindableBase state used by MIRAS services so
    /// individual tests do not share service instances or disposed dependencies.
    /// </summary>
    internal static class MirasStaticState
    {
        private const string ResetMethodName = "ResetForTests";

        private static readonly MethodInfo FlowResetMethod =
            GetResetMethod(typeof(CreateableBindableBase<IFlow, MirasFlow, IMirasBase>));

        private static readonly MethodInfo OperationsResetMethod =
            GetResetMethod(typeof(CreateableBindableBase<IOperations, MirasOperations, IMirasBase>));

        public static void Reset()
        {
            OperationsResetMethod.Invoke(null, null);
            FlowResetMethod.Invoke(null, null);
        }

        private static MethodInfo GetResetMethod(Type closedBaseType) =>
            closedBaseType.GetMethod(
                ResetMethodName,
                BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"The static reset method '{ResetMethodName}' was not found on '{closedBaseType.FullName}'.");
    }
}