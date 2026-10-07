using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
	public sealed partial class PersistenceIntegrationTests
	{
		private const string IntegrationTestSecurityIdentifier = "S-1-5-21-1000000000-2000000000-3000000000-2001";
		private const string UnknownIntegrationTestSecurityIdentifier = "S-1-5-21-9000000000-8000000000-7000000000-9999";

		[Fact, Priority(35)]
		public async Task User_Should_Be_Found_By_SecurityIdentifier()
		{
			var userRepository = _fixture.Persistence?.User ?? throw new InvalidOperationException("The Persistence user repository is not available.");
			var user = await GetUserAsync();
			var originalSecurityIdentifier = user.SecurityIdentifier;

			try
			{
				user.SecurityIdentifier = IntegrationTestSecurityIdentifier;
				await userRepository.UpdateAsync(user, TestContext.Current.CancellationToken);

				var loaded = await userRepository.GetBySecurityIdentifierAsync(IntegrationTestSecurityIdentifier, TestContext.Current.CancellationToken);

				Assert.NotNull(loaded);
				Assert.Equal(_fixture.UserId, loaded.Id);
				Assert.Equal(user.LoginName, loaded.LoginName);
				Assert.Equal(IntegrationTestSecurityIdentifier, loaded.SecurityIdentifier);
			}
			finally
			{
				/*
                 * The ordered integration suite shares one fixture. The original
                 * identity assignment must be restored even when an assertion fails.
                 */
				user.SecurityIdentifier = originalSecurityIdentifier;
				await userRepository.UpdateAsync(user, TestContext.Current.CancellationToken);
			}
		}

		[Fact, Priority(36)]
		public async Task User_Should_Not_Be_Found_By_Unknown_SecurityIdentifier()
		{
			var userRepository = _fixture.Persistence?.User ?? throw new InvalidOperationException("The Persistence user repository is not available.");

			var loaded = await userRepository.GetBySecurityIdentifierAsync(UnknownIntegrationTestSecurityIdentifier, TestContext.Current.CancellationToken);

			Assert.Null(loaded);
		}

		[Theory, Priority(37)]
		[InlineData("")]
		[InlineData(" ")]
		[InlineData("\t")]
		public async Task User_SecurityIdentifier_Lookup_Should_Reject_Empty_Value(string securityIdentifier)
		{
			var userRepository = _fixture.Persistence?.User ?? throw new InvalidOperationException("The Persistence user repository is not available.");

			var exception = await Assert.ThrowsAsync<ArgumentException>(() => userRepository.GetBySecurityIdentifierAsync(securityIdentifier, TestContext.Current.CancellationToken));

			Assert.Equal(nameof(securityIdentifier), exception.ParamName);
			Assert.Contains("security identifier", exception.Message, StringComparison.OrdinalIgnoreCase);
		}

		[Fact, Priority(38)]
		public async Task User_SecurityIdentifier_Lookup_Should_Trim_Surrounding_Whitespace()
		{
			var userRepository = _fixture.Persistence?.User ?? throw new InvalidOperationException("The Persistence user repository is not available.");
			var user = await GetUserAsync();
			var originalSecurityIdentifier = user.SecurityIdentifier;

			try
			{
				user.SecurityIdentifier = IntegrationTestSecurityIdentifier;
				await userRepository.UpdateAsync(user, TestContext.Current.CancellationToken);

				var loaded = await userRepository.GetBySecurityIdentifierAsync($"  {IntegrationTestSecurityIdentifier}  ", TestContext.Current.CancellationToken);

				Assert.NotNull(loaded);
				Assert.Equal(_fixture.UserId, loaded.Id);
				Assert.Equal(IntegrationTestSecurityIdentifier, loaded.SecurityIdentifier);
			}
			finally
			{
				user.SecurityIdentifier = originalSecurityIdentifier;
				await userRepository.UpdateAsync(user, TestContext.Current.CancellationToken);
			}
		}

		[Fact, Priority(39)]
		public async Task Persistence_Should_Remain_Clean_After_User_SecurityIdentifier_Tests()
		{
			var result = await VerifyIntegrityAsync();

			/*
             * Every temporary SID assignment from the preceding tests must have
             * been restored before the shared fixture continues or completes.
             */
			Assert.Empty(result.Issues);
			Assert.Empty(result.Errors);
		}
	}
}