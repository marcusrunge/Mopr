using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using ContractsResources = MarcusRunge.Mopr.Workbench.Contracts.Properties.Resources;
using MirasResources = MarcusRunge.Mopr.Workbench.Services.Miras.Properties.Resources;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    public sealed class MirasLocalizationTests
    {
        private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en");
        private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de");

        private static readonly Type[] LocalizedEnumTypes =
        [
            typeof(MirasAlertLevel),
            typeof(MirasFlowState),
            typeof(MirasIssueState),
            typeof(MirasIssueType),
            typeof(MirasOperationStatus),
            typeof(MirasRecommendedAction)
        ];

        private static readonly string[] ContractUserMessageResourceKeys =
        [
            "MirasOperation_CheckCompleted_Description",
            "MirasOperation_CheckCompleted_Title",
            "MirasOperation_CheckFailed_Description",
            "MirasOperation_CheckIncomplete_Description",
            "MirasOperation_NoActionRequired",
            "MirasOperation_PersistenceBlocked_Description",
            "MirasOperation_TechnicalFailure_Title",
            "MirasStatus_ActionAvailable",
            "MirasStatus_ActionRequired",
            "MirasStatus_AutomaticallyResolved",
            "MirasStatus_Detected"
        ];

        private static readonly string[] MirasUserMessageResourceKeys =
        [
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
            "MirasIssue_Unknown_Description",
            "MirasIssue_UnreadableFile_Description"
        ];

        [Fact]
        public void ContractResources_ContainAllEnglishUserMessages() =>
            AssertAllResourcesExist(
                ContractsResources.ResourceManager,
                ContractUserMessageResourceKeys,
                EnglishCulture);

        [Fact]
        public void ContractResources_ContainAllGermanUserMessages() =>
            AssertAllResourcesExist(
                ContractsResources.ResourceManager,
                ContractUserMessageResourceKeys,
                GermanCulture);

        [Fact]
        public void MirasResources_ContainAllEnglishUserMessages() =>
            AssertAllResourcesExist(
                MirasResources.ResourceManager,
                MirasUserMessageResourceKeys,
                EnglishCulture);

        [Fact]
        public void MirasResources_ContainAllGermanUserMessages() =>
            AssertAllResourcesExist(
                MirasResources.ResourceManager,
                MirasUserMessageResourceKeys,
                GermanCulture);

        [Fact]
        public void Resources_ContainRequiredGermanActionPhrase()
        {
            Assert.Equal(
                "MIRAS Aktion erforderlich",
                GetRequiredResource(
                    ContractsResources.ResourceManager,
                    "MirasOperation_TechnicalFailure_Title",
                    GermanCulture));

            Assert.Equal(
                "MIRAS Aktion erforderlich",
                GetRequiredResource(
                    ContractsResources.ResourceManager,
                    "MirasStatus_ActionRequired",
                    GermanCulture));

            Assert.Equal(
                "MIRAS Aktion erforderlich",
                GetRequiredResource(
                    ContractsResources.ResourceManager,
                    "MirasIssueState_ActionRequired",
                    GermanCulture));
        }

        [Fact]
        public void Resources_ContainRequiredEnglishActionPhrase()
        {
            Assert.Equal(
                "MIRAS action required",
                GetRequiredResource(
                    ContractsResources.ResourceManager,
                    "MirasOperation_TechnicalFailure_Title",
                    EnglishCulture));

            Assert.Equal(
                "MIRAS action required",
                GetRequiredResource(
                    ContractsResources.ResourceManager,
                    "MirasStatus_ActionRequired",
                    EnglishCulture));

            Assert.Equal(
                "MIRAS action required",
                GetRequiredResource(
                    ContractsResources.ResourceManager,
                    "MirasIssueState_ActionRequired",
                    EnglishCulture));
        }

        [Theory]
        [MemberData(nameof(GetLocalizedEnumTypes))]
        public void Enum_UsesRequiredTypeConverter(Type enumType)
        {
            var attribute = enumType.GetCustomAttribute<TypeConverterAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal(
                typeof(EnumDescriptionTypeConverter).AssemblyQualifiedName,
                attribute.ConverterTypeName);
        }

        [Theory]
        [MemberData(nameof(GetLocalizedEnumValues))]
        public void EnumValue_HasEnglishAndGermanResources(
            Type enumType,
            string enumValue)
        {
            var resourceKey = $"{enumType.Name}_{enumValue}";
            var englishValue = GetRequiredResource(
                ContractsResources.ResourceManager,
                resourceKey,
                EnglishCulture);
            var germanValue = GetRequiredResource(
                ContractsResources.ResourceManager,
                resourceKey,
                GermanCulture);

            Assert.False(string.IsNullOrWhiteSpace(englishValue));
            Assert.False(string.IsNullOrWhiteSpace(germanValue));
        }

        [Theory]
        [MemberData(nameof(GetLocalizedEnumValues))]
        public void EnumValue_UsesLocalizedDescriptionAttribute(
            Type enumType,
            string enumValue)
        {
            var member = Assert.Single(enumType.GetMember(enumValue));
            var attribute = Assert.Single(
                member.GetCustomAttributes(),
                candidate =>
                    candidate.GetType().FullName ==
                    typeof(LocalizedDescriptionAttribute).FullName);

            Assert.NotNull(attribute);
        }

        [Theory]
        [MemberData(nameof(GetContractUserMessageResources))]
        public void ContractUserMessageResource_DoesNotContainTechnicalRawDetails(
            string resourceKey)
        {
            AssertResourceContainsNoTechnicalRawDetails(
                ContractsResources.ResourceManager,
                resourceKey,
                EnglishCulture);
            AssertResourceContainsNoTechnicalRawDetails(
                ContractsResources.ResourceManager,
                resourceKey,
                GermanCulture);
        }

        [Theory]
        [MemberData(nameof(GetMirasUserMessageResources))]
        public void MirasUserMessageResource_DoesNotContainTechnicalRawDetails(
            string resourceKey)
        {
            AssertResourceContainsNoTechnicalRawDetails(
                MirasResources.ResourceManager,
                resourceKey,
                EnglishCulture);
            AssertResourceContainsNoTechnicalRawDetails(
                MirasResources.ResourceManager,
                resourceKey,
                GermanCulture);
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

        public static TheoryData<string> GetContractUserMessageResources() =>
            CreateResourceTheoryData(ContractUserMessageResourceKeys);

        public static TheoryData<string> GetMirasUserMessageResources() =>
            CreateResourceTheoryData(MirasUserMessageResourceKeys);

        private static TheoryData<string> CreateResourceTheoryData(
            IEnumerable<string> resourceKeys)
        {
            var data = new TheoryData<string>();

            foreach (var resourceKey in resourceKeys)
            {
                data.Add(resourceKey);
            }

            return data;
        }

        private static void AssertAllResourcesExist(
            System.Resources.ResourceManager resourceManager,
            IEnumerable<string> resourceKeys,
            CultureInfo culture)
        {
            foreach (var resourceKey in resourceKeys)
            {
                _ = GetRequiredResource(
                    resourceManager,
                    resourceKey,
                    culture);
            }
        }

        private static void AssertResourceContainsNoTechnicalRawDetails(
            System.Resources.ResourceManager resourceManager,
            string resourceKey,
            CultureInfo culture)
        {
            var value = GetRequiredResource(
                resourceManager,
                resourceKey,
                culture);

            Assert.DoesNotContain(
                @"C:\",
                value,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                @"\\",
                value,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".dcm",
                value,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "InstanceId",
                value,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "RepositoryLocationId",
                value,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "SopInstanceUid",
                value,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "SeriesInstanceUid",
                value,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "StudyInstanceUid",
                value,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "StackTrace",
                value,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "Exception:",
                value,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRequiredResource(
            System.Resources.ResourceManager resourceManager,
            string resourceKey,
            CultureInfo culture)
        {
            var value = resourceManager.GetString(resourceKey, culture);

            Assert.False(
                string.IsNullOrWhiteSpace(value),
                $"The resource '{resourceKey}' is missing for culture '{culture.Name}'.");

            return value;
        }
    }
}