using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
    /// <summary>
    /// Orders the stateful persistence integration scenario by the explicit priority
    /// assigned to each test method.
    /// </summary>
    /// <remarks>
    /// xUnit 4 orders collections, classes, methods and test cases on separate levels.
    /// The persistence integration scenario builds verified shared state across methods,
    /// so its priorities must be applied through ITestMethodOrderer rather than through
    /// the test-case orderer used by earlier xUnit versions.
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
            if (testMethod is null)
                throw new InvalidOperationException("The test method cannot be null.");

            return testMethod as IXunitTestMethod ?? throw new InvalidOperationException($"The test method '{testMethod?.MethodName}' is not represented by {nameof(IXunitTestMethod)}.");
        }

        private static int GetPriorityOrDefault(IXunitTestMethod testMethod)
        {
            var attribute = testMethod.Method.GetCustomAttribute<PriorityAttribute>(inherit: true);

            if (attribute != null)
            {
                return attribute.Value;
            }

            var typePriority = testMethod.Method.DeclaringType?.GetCustomAttribute<DefaultPriorityAttribute>(inherit: true)?.Value;

            if (typePriority.HasValue)
            {
                return typePriority.Value;
            }

            return int.MaxValue;
        }
    }
}