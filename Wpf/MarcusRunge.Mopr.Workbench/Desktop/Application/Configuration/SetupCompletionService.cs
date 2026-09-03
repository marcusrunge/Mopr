using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Coordinates the technical completion of the machine-wide MOPR setup.
    /// </summary>
    internal sealed class SetupCompletionService(IMachineConfigurationService machineConfigurationService, IRepositoryLocationValidationService repositoryLocationValidationService, IPersistence persistence, ISetupAuditIdentityProvider auditIdentityProvider, BehaviorSubject<PersistenceConfiguration> persistenceConfigurationSubject, BehaviorSubject<IApplicationConfiguration> applicationConfigurationSubject) : ISetupCompletionService
    {
        private const string DefaultRepositoryName = "Default DICOM repository";
        private static readonly TimeSpan RollbackTimeout = TimeSpan.FromSeconds(30);

        private readonly BehaviorSubject<IApplicationConfiguration> _applicationConfigurationSubject = applicationConfigurationSubject ?? throw new ArgumentNullException(nameof(applicationConfigurationSubject));
        private readonly ISetupAuditIdentityProvider _auditIdentityProvider = auditIdentityProvider ?? throw new ArgumentNullException(nameof(auditIdentityProvider));
        private readonly IMachineConfigurationService _machineConfigurationService = machineConfigurationService ?? throw new ArgumentNullException(nameof(machineConfigurationService));
        private readonly IPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        private readonly BehaviorSubject<PersistenceConfiguration> _persistenceConfigurationSubject = persistenceConfigurationSubject ?? throw new ArgumentNullException(nameof(persistenceConfigurationSubject));
        private readonly IRepositoryLocationValidationService _repositoryLocationValidationService = repositoryLocationValidationService ?? throw new ArgumentNullException(nameof(repositoryLocationValidationService));
        private readonly SemaphoreSlim _synchronization = new(1, 1);

        /// <inheritdoc/>
        public async Task<SetupCompletionResult> CompleteAsync(SetupCompletionRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            RepositoryLocationChange? repositoryLocationChange = null;
            var lockAcquired = false;

            try
            {
                request.Validate();

                await _synchronization.WaitAsync(cancellationToken).ConfigureAwait(false);
                lockAcquired = true;

                cancellationToken.ThrowIfCancellationRequested();

                var databaseValid = await _machineConfigurationService
                    .TestDatabaseConnectionAsync(request.Configuration.Database, cancellationToken)
                    .ConfigureAwait(false);

                if (!databaseValid)
                {
                    return SetupCompletionResult.DatabaseValidationFailed();
                }

                var repositoryValidationResult = await _repositoryLocationValidationService.ValidateAsync(request.RepositoryPath, cancellationToken).ConfigureAwait(false);

                if (!repositoryValidationResult.IsValid || string.IsNullOrWhiteSpace(repositoryValidationResult.NormalizedPath))
                {
                    return SetupCompletionResult.RepositoryValidationFailed();
                }

                var normalizedRepositoryPath = repositoryValidationResult.NormalizedPath;

                // Persistence receives the selected database configuration only after
                // the independent connection and repository validations succeeded.
                _persistenceConfigurationSubject.OnNext(new PersistenceConfiguration
                {
                    ConnectionString = request.Configuration.Database.ConnectionString,
                    Mode = PersistenceMode.SqlServer
                });

                await _persistence.Initialization.WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                var repositoryLocationRepository = _persistence.RepositoryLocation ?? throw new InvalidOperationException("The Persistence repository-location contract is not available.");

                var auditUserId = await _auditIdentityProvider.GetOrCreateUserIdAsync(cancellationToken).ConfigureAwait(false);

                repositoryLocationChange = await EnsureDefaultRepositoryLocationAsync(repositoryLocationRepository, normalizedRepositoryPath, auditUserId, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                var completedConfiguration = CreateCompletedConfiguration(request.Configuration);

                // This is the final durable setup step. No setup-completion marker is
                // written before all database and repository prerequisites are ready.
                await _machineConfigurationService.SaveAsync(completedConfiguration, cancellationToken).ConfigureAwait(false);

                // Runtime consumers receive only the configuration that was successfully
                // persisted as the completed machine-wide configuration.
                _applicationConfigurationSubject.OnNext(completedConfiguration);

                return SetupCompletionResult.Completed();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                var rollbackResult = await RollbackAsync(repositoryLocationChange).ConfigureAwait(false);

                return SetupCompletionResult.Canceled(rollbackResult.Attempted, rollbackResult.Successful, rollbackResult.TechnicalDetails);
            }
            catch (Exception exception)
            {
                var rollbackResult = await RollbackAsync(repositoryLocationChange).ConfigureAwait(false);
                var technicalException = rollbackResult.Successful ? exception : new AggregateException("Setup completion and repository-location rollback both failed.", exception, rollbackResult.Exception!);

                return SetupCompletionResult.Failed(technicalException, rollbackResult.Attempted, rollbackResult.Successful);
            }
            finally
            {
                if (lockAcquired)
                {
                    _synchronization.Release();
                }
            }
        }

        private static ApplicationConfiguration CreateCompletedConfiguration(IApplicationConfiguration configuration) => new()
        {
            DatabaseConfiguration = new DatabaseConfiguration
            {
                ConnectionString = configuration.Database.ConnectionString
            },
            IsSetupComplete = true,
            RepositoryConfiguration = new RepositoryConfiguration
            {
                AutomaticallyRepairPaths = configuration.Repository.AutomaticallyRepairPaths
            },
            SecurityConfiguration = new SecurityConfiguration
            {
                AllowSelfDeletion = configuration.Security.AllowSelfDeletion,
                AllowSelfModification = configuration.Security.AllowSelfModification,
                HideOtherUsersFromRegularUsers = configuration.Security.HideOtherUsersFromRegularUsers
            },
            SetupVersion = configuration.SetupVersion
        };

        private static async Task<RepositoryLocationChange> EnsureDefaultRepositoryLocationAsync(IRepositoryLocationRepository repository, string normalizedRepositoryPath, int auditUserId, CancellationToken cancellationToken)
        {
            var selectedLocation = await repository.GetByRootPathAsync(normalizedRepositoryPath, cancellationToken).ConfigureAwait(false);

            var previousDefault = await repository.GetDefaultAsync(cancellationToken).ConfigureAwait(false);

            // Every relevant original state must be captured before AddAsync or
            // UpdateAsync can enforce the single-default invariant and mutate it.
            var previousDefaultSnapshot = previousDefault is not null && (selectedLocation is null || previousDefault.Id != selectedLocation.Id) ? CreateSnapshot(previousDefault) : null;

            if (selectedLocation is null)
            {
                var createdLocation = new RepositoryLocation
                {
                    CreatedByUserId = auditUserId,
                    IsDefault = true,
                    IsEnabled = true,
                    Name = DefaultRepositoryName,
                    RootPath = normalizedRepositoryPath
                };

                await repository.AddAsync(createdLocation, cancellationToken).ConfigureAwait(false);

                return RepositoryLocationChange.Created(repository, createdLocation, previousDefaultSnapshot);
            }

            if (selectedLocation.IsEnabled && selectedLocation.IsDefault)
            {
                return RepositoryLocationChange.Unchanged(repository);
            }

            var selectedLocationSnapshot = CreateSnapshot(selectedLocation) ?? throw new InvalidOperationException("The selected repository location could not be captured for rollback.");

            selectedLocation.IsEnabled = true;
            selectedLocation.IsDefault = true;
            selectedLocation.ModifiedByUserId = auditUserId;

            await repository.UpdateAsync(selectedLocation, cancellationToken).ConfigureAwait(false);

            return RepositoryLocationChange.Updated(repository, selectedLocation, selectedLocationSnapshot, previousDefaultSnapshot);
        }

        private static RepositoryLocationSnapshot? CreateSnapshot(RepositoryLocation? location)
        {
            if (location is null)
            {
                return null;
            }

            return new RepositoryLocationSnapshot
            {
                Entity = location,
                IsDefault = location.IsDefault,
                IsEnabled = location.IsEnabled,
                ModifiedAtUtc = location.ModifiedAtUtc,
                ModifiedByUserId = location.ModifiedByUserId,
                Name = location.Name,
                RootPath = location.RootPath
            };
        }

        private static async Task<RollbackResult> RollbackAsync(RepositoryLocationChange? change)
        {
            if (change is null || !change.RequiresRollback)
            {
                return RollbackResult.NotRequired;
            }

            using var rollbackCancellation = new CancellationTokenSource(RollbackTimeout);

            try
            {
                await change.RollbackAsync(rollbackCancellation.Token).ConfigureAwait(false);

                return RollbackResult.Succeeded;
            }
            catch (Exception exception)
            {
                return RollbackResult.Failed(exception);
            }
        }

        private sealed class RepositoryLocationChange
        {
            private readonly RepositoryLocation? _createdLocation;
            private readonly RepositoryLocationSnapshot? _previousDefaultSnapshot;
            private readonly IRepositoryLocationRepository _repository;
            private readonly RepositoryLocationSnapshot? _updatedLocationSnapshot;

            private RepositoryLocationChange(IRepositoryLocationRepository repository, RepositoryLocation? createdLocation, RepositoryLocationSnapshot? updatedLocationSnapshot, RepositoryLocationSnapshot? previousDefaultSnapshot)
            {
                _repository = repository;
                _createdLocation = createdLocation;
                _updatedLocationSnapshot = updatedLocationSnapshot;
                _previousDefaultSnapshot = previousDefaultSnapshot;
            }

            public bool RequiresRollback => _createdLocation is not null || _updatedLocationSnapshot is not null;

            public static RepositoryLocationChange Created(IRepositoryLocationRepository repository, RepositoryLocation createdLocation, RepositoryLocationSnapshot? previousDefaultSnapshot) => new(repository, createdLocation, null, previousDefaultSnapshot);

            public static RepositoryLocationChange Unchanged(IRepositoryLocationRepository repository) =>
                new(repository, null, null, null);

            public static RepositoryLocationChange Updated(IRepositoryLocationRepository repository, RepositoryLocation updatedLocation, RepositoryLocationSnapshot updatedLocationSnapshot, RepositoryLocationSnapshot? previousDefaultSnapshot)
            {
                if (updatedLocation.Id != updatedLocationSnapshot.Entity.Id)
                {
                    throw new InvalidOperationException("The repository-location rollback snapshot does not match the updated entity.");
                }

                return new RepositoryLocationChange(repository, null, updatedLocationSnapshot, previousDefaultSnapshot);
            }

            public async Task RollbackAsync(CancellationToken cancellationToken)
            {
                if (_createdLocation is not null)
                {
                    await _repository.DeleteAsync(_createdLocation, cancellationToken).ConfigureAwait(false);
                }
                else if (_updatedLocationSnapshot is not null)
                {
                    RestoreSnapshot(_updatedLocationSnapshot);

                    await _repository.UpdateAsync(_updatedLocationSnapshot.Entity, cancellationToken).ConfigureAwait(false);
                }

                // Restoring the previous default last ensures that the repository
                // invariant leaves exactly the original location marked as default.
                if (_previousDefaultSnapshot is not null)
                {
                    RestoreSnapshot(_previousDefaultSnapshot);

                    await _repository.UpdateAsync(_previousDefaultSnapshot.Entity, cancellationToken).ConfigureAwait(false);
                }
            }

            private static void RestoreSnapshot(RepositoryLocationSnapshot snapshot)
            {
                snapshot.Entity.IsDefault = snapshot.IsDefault;
                snapshot.Entity.IsEnabled = snapshot.IsEnabled;
                snapshot.Entity.ModifiedAtUtc = snapshot.ModifiedAtUtc;
                snapshot.Entity.ModifiedByUserId = snapshot.ModifiedByUserId;
                snapshot.Entity.Name = snapshot.Name;
                snapshot.Entity.RootPath = snapshot.RootPath;
            }
        }

        private sealed class RepositoryLocationSnapshot
        {
            public RepositoryLocation Entity { get; init; } = null!;

            public bool IsDefault { get; init; }

            public bool IsEnabled { get; init; }

            public DateTime? ModifiedAtUtc { get; init; }

            public int? ModifiedByUserId { get; init; }

            public string? Name { get; init; }

            public string? RootPath { get; init; }
        }

        private sealed class RollbackResult
        {
            private RollbackResult(bool attempted, bool successful, Exception? exception)
            {
                Attempted = attempted;
                Successful = successful;
                Exception = exception;
            }

            public bool Attempted { get; }

            public Exception? Exception { get; }

            public bool Successful { get; }

            public string TechnicalDetails => Exception?.ToString() ?? string.Empty;

            public static RollbackResult NotRequired { get; } = new(false, true, null);

            public static RollbackResult Succeeded { get; } = new(true, true, null);

            public static RollbackResult Failed(Exception exception) => new(true, false, exception);
        }
    }
}