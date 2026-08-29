using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Miras.Properties;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    public sealed class MirasLocalizationTests
    {
        private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en");
        private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de");

        private static readonly Type[] LocalizedEnumTypes =
        [
            typeof(MirasAlertLevel),
            typeof(MirasIssueState),
            typeof(MirasIssueType),
            typeof(MirasOperationStatus),
            typeof(MirasRecommendedAction)
        ];

        private static readonly string[] UserMessageResourceKeys =
        [
            "MirasOperation_CheckCompleted_Description",
            "MirasOperation_CheckCompleted_Title",
            "MirasOperation_CheckFailed_Description",
            "MirasOperation_CheckIncomplete_Description",
            "MirasOperation_NoActionRequired",
            "MirasOperation_PersistenceBlocked_Description",
            "MirasOperation_TechnicalFailure_Title",

            "MirasIssue_DuplicateFile_Description",
            "MirasIssue_IdentityMismatch_Description",
            "MirasIssue_IncompleteImport_Description",
            "MirasIssue_InvalidDicomFile_Description",
            "MirasIssue_MisplacedFile_Description",
            "MirasIssue_MissingFile_Description",
            "MirasIssue_OrphanedFile_Description",
            "MirasIssue_PersistenceAuditReferenceInvalid_Description",
            "MirasIssue_PersistenceRelationshipConflict_Description",
            "MirasIssue_PersistenceRequiredValueMissing_Description",
            "MirasIssue_PersistenceUnavailable_Description",
            "MirasIssue_PersistenceUniqueValueConflict_Description",
            "MirasIssue_PersistenceValueInvalid_Description",
            "MirasIssue_RelationshipConflict_Description",
            "MirasIssue_RepositoryUnavailable_Description",
            "MirasIssue_RepositoryUnavailable_Title",
            "MirasIssue_Unknown_Description",
            "MirasIssue_UnreadableFile_Description",

            "MirasStatus_ActionAvailable",
            "MirasStatus_ActionRequired",
            "MirasStatus_AutomaticallyResolved",
            "MirasStatus_Detected"
        ];

        [Fact]
        public void Resources_ContainAllEnglishUserMessages()
        {
            AssertAllResourcesExist(UserMessageResourceKeys, EnglishCulture);
        }

        [Fact]
        public void Resources_ContainAllGermanUserMessages()
        {
            AssertAllResourcesExist(UserMessageResourceKeys, GermanCulture);
        }

        [Fact]
        public void Resources_ContainRequiredGermanActionPhrase()
        {
            Assert.Equal(
                "MIRAS Aktion erforderlich",
                GetRequiredResource("MirasOperation_TechnicalFailure_Title", GermanCulture));

            Assert.Equal(
                "MIRAS Aktion erforderlich",
                GetRequiredResource("MirasStatus_ActionRequired", GermanCulture));

            Assert.Equal(
                "MIRAS Aktion erforderlich",
                GetRequiredResource("MirasIssueState_ActionRequired", GermanCulture));
        }

        [Fact]
        public void Resources_ContainRequiredEnglishActionPhrase()
        {
            Assert.Equal(
                "MIRAS action required",
                GetRequiredResource("MirasOperation_TechnicalFailure_Title", EnglishCulture));

            Assert.Equal(
                "MIRAS action required",
                GetRequiredResource("MirasStatus_ActionRequired", EnglishCulture));

            Assert.Equal(
                "MIRAS action required",
                GetRequiredResource("MirasIssueState_ActionRequired", EnglishCulture));
        }

        [Theory]
        [MemberData(nameof(GetLocalizedEnumTypes))]
        public void Enum_UsesRequiredTypeConverter(Type enumType)
        {
            var attribute = enumType.GetCustomAttribute<TypeConverterAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal(typeof(Toolbox.Localization.Core.EnumDescriptionTypeConverter).AssemblyQualifiedName, attribute.ConverterTypeName);
        }

        [Theory]
        [MemberData(nameof(GetLocalizedEnumValues))]
        public void EnumValue_HasEnglishAndGermanResources(Type enumType, string enumValue)
        {
            var resourceKey = $"{enumType.Name}_{enumValue}";

            var englishValue = GetRequiredResource(resourceKey, EnglishCulture);
            var germanValue = GetRequiredResource(resourceKey, GermanCulture);

            Assert.False(string.IsNullOrWhiteSpace(englishValue));
            Assert.False(string.IsNullOrWhiteSpace(germanValue));
        }

        [Theory]
        [MemberData(nameof(GetLocalizedEnumValues))]
        public void EnumValue_UsesLocalizedDescriptionAttribute(Type enumType, string enumValue)
        {
            var member = Assert.Single(enumType.GetMember(enumValue));

            var localizedDescriptionAttribute = Assert.Single(member.GetCustomAttributes(), attribute => attribute.GetType().FullName == "MarcusRunge.Toolbox.Localization.Core.LocalizedDescriptionAttribute");

            Assert.NotNull(localizedDescriptionAttribute);
        }

        [Theory]
        [MemberData(nameof(GetUserMessageResources))]
        public void UserMessageResource_DoesNotContainTechnicalRawDetails(string resourceKey)
        {
            AssertResourceContainsNoTechnicalRawDetails(resourceKey, EnglishCulture);
            AssertResourceContainsNoTechnicalRawDetails(resourceKey, GermanCulture);
        }

        public static TheoryData<Type> GetLocalizedEnumTypes()
        {
            var data = new TheoryData<Type>();

            foreach (var enumType in LocalizedEnumTypes)
            {
                data.Add(enumType);
            }

            return data;
        }

        public static TheoryData<Type, string> GetLocalizedEnumValues()
        {
            var data = new TheoryData<Type, string>();

            foreach (var enumType in LocalizedEnumTypes)
            {
                foreach (var enumValue in Enum.GetNames(enumType))
                {
                    data.Add(enumType, enumValue);
                }
            }

            return data;
        }

        public static TheoryData<string> GetUserMessageResources()
        {
            var data = new TheoryData<string>();

            foreach (var resourceKey in UserMessageResourceKeys)
            {
                data.Add(resourceKey);
            }

            return data;
        }

        private static void AssertAllResourcesExist(IEnumerable<string> resourceKeys, CultureInfo culture)
        {
            foreach (var resourceKey in resourceKeys)
            {
                var value = GetRequiredResource(resourceKey, culture);
                Assert.False(string.IsNullOrWhiteSpace(value));
            }
        }

        private static void AssertResourceContainsNoTechnicalRawDetails(string resourceKey, CultureInfo culture)
        {
            var value = GetRequiredResource(resourceKey, culture);

            Assert.DoesNotContain(@"C:\", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"\\", value, StringComparison.Ordinal);
            Assert.DoesNotContain(".dcm", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("InstanceId", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RepositoryLocationId", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SopInstanceUid", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SeriesInstanceUid", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("StudyInstanceUid", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("StackTrace", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Exception:", value, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRequiredResource(string resourceKey, CultureInfo culture)
        {
            var value = Resources.ResourceManager.GetString(resourceKey, culture);

            Assert.False(string.IsNullOrWhiteSpace(value), $"The resource '{resourceKey}' is missing for culture '{culture.Name}'.");

            return value;
        }
    }
}