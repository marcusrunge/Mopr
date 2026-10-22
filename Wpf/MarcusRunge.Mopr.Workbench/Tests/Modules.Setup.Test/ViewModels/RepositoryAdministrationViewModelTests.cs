using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using Moq;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.Test.ViewModels;

public sealed class RepositoryAdministrationViewModelTests
{
    private const int DefaultLocationId = 1;
    private const int SecondaryLocationId = 2;
    private const string DefaultLocationName = "Primary repository";
    private const string DefaultLocationPath = @"C:\MOPR\Repository";
    private const string SecondaryLocationName = "Archive repository";
    private const string SecondaryLocationPath = @"D:\MOPR\Archive";

    private static readonly byte[] DefaultRowVersion = [1, 2, 3, 4];
    private static readonly byte[] SecondaryRowVersion = [5, 6, 7, 8];

    [Fact]
    public async Task Constructor_WhenLocationsAreLoaded_SelectsDefaultLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = new RepositoryAdministrationViewModelTestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        Assert.Equal(2, context.ViewModel.Locations.Count);
        Assert.NotNull(context.ViewModel.SelectedLocation);
        Assert.Equal(DefaultLocationId, context.ViewModel.SelectedLocation.Id);
        Assert.Equal(DefaultLocationName, context.ViewModel.RepositoryName);
        Assert.Equal(DefaultLocationPath, context.ViewModel.RepositoryPath);
        Assert.True(context.ViewModel.IsRepositoryEnabled);
        Assert.True(context.ViewModel.IsDefaultRepository);
        Assert.False(context.ViewModel.IsDirty);
        Assert.False(context.ViewModel.IsCreating);
    }

    [Fact]
    public async Task NewCommand_WhenExecuted_EntersCreationMode()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = new RepositoryAdministrationViewModelTestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.NewCommand.Execute();
        Assert.True(context.ViewModel.IsCreating);
        Assert.Null(context.ViewModel.SelectedLocation);
        Assert.Empty(context.ViewModel.RepositoryName);
        Assert.Empty(context.ViewModel.RepositoryPath);
        Assert.True(context.ViewModel.IsRepositoryEnabled);
        Assert.False(context.ViewModel.IsDefaultRepository);
        Assert.False(context.ViewModel.CanSelectLocation);
        Assert.False(context.ViewModel.RequestDeleteCommand.CanExecute());
        Assert.True(context.ViewModel.CancelCreateCommand.CanExecute());
        Assert.Equal(Resources.RepositoryAdministration_NewInstructions, context.ViewModel.StatusText);
    }

    [Fact]
    public async Task NewCommand_WhenNoLocationsExist_PreparesEnabledDefaultRepository()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = new RepositoryAdministrationViewModelTestContext([]);
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.NewCommand.Execute();
        Assert.True(context.ViewModel.IsCreating);
        Assert.True(context.ViewModel.IsRepositoryEnabled);
        Assert.True(context.ViewModel.IsDefaultRepository);
        Assert.False(context.ViewModel.CanChangeRepositoryEnabled);
    }

    [Fact]
    public async Task CancelCreateCommand_WhenExecuted_RestoresDefaultLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = new RepositoryAdministrationViewModelTestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.NewCommand.Execute();
        context.ViewModel.RepositoryName = "Temporary repository";
        context.ViewModel.RepositoryPath = @"E:\Temporary";
        context.ViewModel.CancelCreateCommand.Execute();
        Assert.False(context.ViewModel.IsCreating);
        Assert.NotNull(context.ViewModel.SelectedLocation);
        Assert.Equal(DefaultLocationId, context.ViewModel.SelectedLocation.Id);
        Assert.Equal(DefaultLocationName, context.ViewModel.RepositoryName);
        Assert.Equal(DefaultLocationPath, context.ViewModel.RepositoryPath);
        Assert.False(context.ViewModel.IsDirty);
        Assert.Equal(Resources.RepositoryAdministration_CreationCanceled, context.ViewModel.StatusText);
    }

    [Fact]
    public async Task RepositoryPath_WhenChanged_InvalidatesPreviousValidation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = new RepositoryAdministrationViewModelTestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.SelectedLocation = context.ViewModel.Locations.Single(location => location.Id == SecondaryLocationId);
        context.ViewModel.RepositoryName = "Updated archive";
        context.ViewModel.RepositoryPath = @"E:\MOPR\Archive";
        Assert.True(context.ViewModel.IsDirty);
        Assert.False(context.ViewModel.SaveCommand.CanExecute());
        context.ViewModel.ValidateCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.RepositoryAdministration_Valid, cancellationToken);
        Assert.True(context.ViewModel.SaveCommand.CanExecute());
        context.ViewModel.RepositoryPath = @"F:\MOPR\Archive";
        Assert.False(context.ViewModel.SaveCommand.CanExecute());
    }

    [Fact]
    public async Task DiscardCommand_WhenExecuted_RestoresPersistedValues()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = new RepositoryAdministrationViewModelTestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        var secondaryLocation = context.ViewModel.Locations.Single(location => location.Id == SecondaryLocationId);
        context.ViewModel.SelectedLocation = secondaryLocation;
        context.ViewModel.RepositoryName = "Changed name";
        context.ViewModel.RepositoryPath = @"E:\Changed";
        Assert.True(context.ViewModel.IsDirty);
        Assert.True(context.ViewModel.DiscardCommand.CanExecute());
        context.ViewModel.DiscardCommand.Execute();
        Assert.Equal(SecondaryLocationName, context.ViewModel.RepositoryName);
        Assert.Equal(SecondaryLocationPath, context.ViewModel.RepositoryPath);
        Assert.True(context.ViewModel.IsRepositoryEnabled);
        Assert.False(context.ViewModel.IsDefaultRepository);
        Assert.False(context.ViewModel.IsDirty);
        Assert.Equal(Resources.RepositoryAdministration_ChangesDiscarded, context.ViewModel.StatusText);
    }

    [Fact]
    public async Task SaveCommand_WhenCreatingValidatedLocation_CreatesAndSelectsLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var createdSummary = CreateSummary(3, "New repository", @"E:\MOPR\New", true, false, [9, 10, 11, 12]);
        IReadOnlyList<RepositoryLocationSummary> reloadedLocations = [.. CreateLocations(), createdSummary];
        var context = new RepositoryAdministrationViewModelTestContext(reloadedLocations: reloadedLocations);
        context.AdministrationService.Setup(service => service.CreateAsync(It.IsAny<RepositoryLocationCreateRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(RepositoryLocationCreateResult.Success(createdSummary));
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.NewCommand.Execute();
        context.ViewModel.RepositoryName = createdSummary.Name;
        context.ViewModel.RepositoryPath = createdSummary.RootPath;
        context.ViewModel.ValidateCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.RepositoryAdministration_Valid, cancellationToken);
        Assert.True(context.ViewModel.SaveCommand.CanExecute());
        context.ViewModel.SaveCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.RepositoryAdministration_Created, cancellationToken);
        Assert.False(context.ViewModel.IsCreating);
        Assert.NotNull(context.ViewModel.SelectedLocation);
        Assert.Equal(createdSummary.Id, context.ViewModel.SelectedLocation.Id);
        Assert.Equal(createdSummary.Name, context.ViewModel.RepositoryName);
        Assert.Equal(createdSummary.RootPath, context.ViewModel.RepositoryPath);
        Assert.False(context.ViewModel.IsDirty);
        context.AdministrationService.Verify(service => service.CreateAsync(It.Is<RepositoryLocationCreateRequest>(request => request.Name == createdSummary.Name && request.RootPath == createdSummary.RootPath && request.IsEnabled && !request.IsDefault), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestDeleteCommand_ForNonDefaultLocation_OpensConfirmation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = new RepositoryAdministrationViewModelTestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.SelectedLocation = context.ViewModel.Locations.Single(location => location.Id == SecondaryLocationId);
        Assert.True(context.ViewModel.RequestDeleteCommand.CanExecute());
        context.ViewModel.RequestDeleteCommand.Execute();
        Assert.True(context.ViewModel.IsDeleteConfirmationVisible);
        Assert.Equal(SecondaryLocationName, context.ViewModel.DeleteConfirmationRepositoryName);
        Assert.True(context.ViewModel.CancelDeleteCommand.CanExecute());
        Assert.True(context.ViewModel.ConfirmDeleteCommand.CanExecute());
    }

    [Fact]
    public async Task RequestDeleteCommand_ForDefaultLocation_CannotExecute()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = new RepositoryAdministrationViewModelTestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        Assert.NotNull(context.ViewModel.SelectedLocation);
        Assert.True(context.ViewModel.SelectedLocation.IsDefault);
        Assert.False(context.ViewModel.CanDeleteSelectedLocation);
        Assert.False(context.ViewModel.RequestDeleteCommand.CanExecute());
    }

    [Fact]
    public async Task CancelDeleteCommand_WhenExecuted_ClosesConfirmationWithoutDeleting()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = new RepositoryAdministrationViewModelTestContext();
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.SelectedLocation = context.ViewModel.Locations.Single(location => location.Id == SecondaryLocationId);
        context.ViewModel.RequestDeleteCommand.Execute();
        context.ViewModel.CancelDeleteCommand.Execute();
        Assert.False(context.ViewModel.IsDeleteConfirmationVisible);
        Assert.Empty(context.ViewModel.StatusText);
        context.AdministrationService.Verify(service => service.DeleteAsync(It.IsAny<RepositoryLocationDeleteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmDeleteCommand_WhenDeletionSucceeds_ReloadsLocations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        IReadOnlyList<RepositoryLocationSummary> reloadedLocations = [CreateDefaultSummary()];
        var context = new RepositoryAdministrationViewModelTestContext(reloadedLocations: reloadedLocations);
        context.AdministrationService.Setup(service => service.DeleteAsync(It.IsAny<RepositoryLocationDeleteRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(RepositoryLocationDeleteResult.Success());
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.SelectedLocation = context.ViewModel.Locations.Single(location => location.Id == SecondaryLocationId);
        context.ViewModel.RequestDeleteCommand.Execute();
        context.ViewModel.ConfirmDeleteCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.RepositoryAdministration_Deleted, cancellationToken);
        Assert.False(context.ViewModel.IsDeleteConfirmationVisible);
        Assert.Single(context.ViewModel.Locations);
        Assert.NotNull(context.ViewModel.SelectedLocation);
        Assert.Equal(DefaultLocationId, context.ViewModel.SelectedLocation.Id);
        context.AdministrationService.Verify(service => service.DeleteAsync(It.Is<RepositoryLocationDeleteRequest>(request => request.Id == SecondaryLocationId && request.RowVersion.SequenceEqual(SecondaryRowVersion)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmDeleteCommand_WhenLocationIsInUse_ShowsLocalizedStatus()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var context = new RepositoryAdministrationViewModelTestContext();
        context.AdministrationService.Setup(service => service.DeleteAsync(It.IsAny<RepositoryLocationDeleteRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(RepositoryLocationDeleteResult.InUse());
        await context.WaitUntilLoadedAsync(cancellationToken);
        context.ViewModel.SelectedLocation = context.ViewModel.Locations.Single(location => location.Id == SecondaryLocationId);
        context.ViewModel.RequestDeleteCommand.Execute();
        context.ViewModel.ConfirmDeleteCommand.Execute();
        await WaitUntilAsync(() => !context.ViewModel.IsBusy && context.ViewModel.StatusText == Resources.RepositoryAdministration_DeleteInUse, cancellationToken);
        Assert.True(context.ViewModel.IsDeleteConfirmationVisible);
        Assert.Equal(Resources.RepositoryAdministration_DeleteInUse, context.ViewModel.StatusText);
    }

    private static RepositoryLocationSummary CreateDefaultSummary() => CreateSummary(DefaultLocationId, DefaultLocationName, DefaultLocationPath, true, true, DefaultRowVersion);

    private static IReadOnlyList<RepositoryLocationSummary> CreateLocations() => [CreateDefaultSummary(), CreateSummary(SecondaryLocationId, SecondaryLocationName, SecondaryLocationPath, true, false, SecondaryRowVersion)];

    private static RepositoryLocationSummary CreateSummary(int id, string name, string rootPath, bool isEnabled, bool isDefault, byte[] rowVersion) => new(id, name, rootPath, isEnabled, isDefault, [.. rowVersion]);

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTime.UtcNow >= timeoutAt) throw new TimeoutException("The expected RepositoryAdministrationViewModel state was not reached.");
            await Task.Delay(10, cancellationToken);
        }
    }

    private sealed class RepositoryAdministrationViewModelTestContext
    {
        public RepositoryAdministrationViewModelTestContext(IReadOnlyList<RepositoryLocationSummary>? locations = null, IReadOnlyList<RepositoryLocationSummary>? reloadedLocations = null)
        {
            Locations = locations ?? CreateLocations();
            LifetimeService.SetupGet(service => service.ApplicationStopping).Returns(CancellationToken.None);

            if (reloadedLocations is null) AdministrationService.Setup(service => service.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Locations);
            else AdministrationService.SetupSequence(service => service.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Locations).ReturnsAsync(reloadedLocations);

            Application.SetupGet(application => application.RepositoryAdministration).Returns(AdministrationService.Object);
            AdministrationService.Setup(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((string path, CancellationToken _) => new RepositoryLocationValidationResult { Exists = true, IsReadable = true, IsWritable = true, NormalizedPath = path });
            ViewModel = new RepositoryAdministrationViewModel(Application.Object, LifetimeService.Object);
        }

        public Mock<IRepositoryLocationAdministrationService> AdministrationService { get; } = new(MockBehavior.Strict);
        public Mock<IApplication> Application { get; } = new(MockBehavior.Strict);
        public Mock<ILifetimeService> LifetimeService { get; } = new(MockBehavior.Strict);
        public IReadOnlyList<RepositoryLocationSummary> Locations { get; }
        public RepositoryAdministrationViewModel ViewModel { get; }
        public async Task WaitUntilLoadedAsync(CancellationToken cancellationToken, int? expectedCount = null) => await WaitUntilAsync(() => !ViewModel.IsBusy && ViewModel.Locations.Count == (expectedCount ?? Locations.Count), cancellationToken);
    }
}
