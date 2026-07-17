using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    public class PriorityOrderer : ITestCaseOrderer
    {
        public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases) where TTestCase : notnull, ITestCase
        {
            ArgumentNullException.ThrowIfNull(testCases);

            var buckets = new SortedDictionary<int, List<TTestCase>>();

            foreach (var testCase in testCases)
            {
                var priority = GetPriorityOrDefault(testCase);

                if (!buckets.TryGetValue(priority, out var list))
                {
                    list = [];
                    buckets.Add(priority, list);
                }

                list.Add(testCase);
            }

            var ordered = new List<TTestCase>(testCases.Count);

            foreach (var (_, list) in buckets)
            {
                ordered.AddRange(list.OrderBy(tc => tc.TestMethodName ?? string.Empty, StringComparer.OrdinalIgnoreCase).ThenBy(tc => tc.TestCaseDisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase).ThenBy(tc => tc.UniqueID ?? string.Empty, StringComparer.OrdinalIgnoreCase));
            }

            return ordered;
        }
        private static int GetPriorityOrDefault(ITestCase testCase)
        {
            var methodInfo = TryGetMethodInfo(testCase);

            if (methodInfo is null)
                return int.MaxValue;

            var methodPriority = methodInfo.GetCustomAttribute<PriorityAttribute>(inherit: true)?.Value;
            if (methodPriority is not null)
                return methodPriority.Value;

            var typePriority = methodInfo.DeclaringType?.GetCustomAttribute<DefaultPriorityAttribute>(inherit: true)?.Value;
            if (typePriority is not null)
                return typePriority.Value;

            return int.MaxValue;
        }

        private static MethodInfo? TryGetMethodInfo(ITestCase testCase)
        {
            try
            {
                return (testCase.TestMethod as IXunitTestMethod)?.Method;
            }
            catch
            {
                return null;
            }
        }
    }
}