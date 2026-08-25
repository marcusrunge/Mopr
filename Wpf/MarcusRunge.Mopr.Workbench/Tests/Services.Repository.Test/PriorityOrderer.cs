using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    /// <summary>
    /// Orders the stateful repository integration scenario by the explicit priority
    /// assigned to each test method.
    /// </summary>
    /// <remarks>
    /// xUnit 4 orders collections, classes, methods and test cases on separate levels.
    /// The repository integration scenario builds and verifies shared state across
    /// methods, so its priorities must be applied through ITestMethodOrderer.
    /// </remarks>
    public sealed class PriorityOrderer : ITestMethodOrderer
    {
        public IReadOnlyCollection<TTestMethod?> OrderTestMethods<TTestMethod>(IReadOnlyCollection<TTestMethod?> testMethods) where TTestMethod : notnull, ITestMethod
        {
            ArgumentNullException.ThrowIfNull(testMethods);
            return [.. testMethods.OrderBy(testMethod => GetPriorityOrDefault(GetXunitTestMethod(testMethod))).ThenBy(testMethod => testMethod?.MethodName ?? string.Empty, StringComparer.Ordinal).ThenBy(testMethod => testMethod?.UniqueID ?? string.Empty, StringComparer.Ordinal)];
        }

        private static IXunitTestMethod GetXunitTestMethod(ITestMethod? testMethod)
        {
            // Ein stiller Rückfall auf eine alphabetische Ausführung könnte abhängige
            // Repository-Szenarien vor ihren Voraussetzungen starten. Eine inkompatible
            // Repräsentation soll den Testlauf deshalb sichtbar abbrechen.
            return testMethod as IXunitTestMethod ?? throw new InvalidOperationException($"The test method '{testMethod?.MethodName}' is not represented by {nameof(IXunitTestMethod)}.");
        }

        private static int GetPriorityOrDefault(IXunitTestMethod testMethod)
        {
            var methodPriority = testMethod.Method.GetCustomAttribute<PriorityAttribute>(inherit: true)?.Value;

            if (methodPriority.HasValue)
            {
                return methodPriority.Value;
            }

            var typePriority = testMethod.Method.DeclaringType?.GetCustomAttribute<DefaultPriorityAttribute>(inherit: true)?.Value;

            return typePriority ?? int.MaxValue;
        }
    }
}