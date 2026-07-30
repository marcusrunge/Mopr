using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
    [TestCaseOrderer(typeof(PriorityOrderer))]
    public sealed partial class PersistenceIntegrationTests(PersistenceFixture fixture) : IClassFixture<PersistenceFixture>
    {
        private readonly PersistenceFixture _fixture = fixture;

        [Fact, Priority(1)]
        public void Services_And_Repositories_Should_Be_Available()
        {
            Assert.NotNull(_fixture.Persistence);
            Assert.NotNull(_fixture.Persistence!.DicomImport);
            Assert.NotNull(_fixture.Persistence.Integrity);
            Assert.NotNull(_fixture.Persistence.Study);
            Assert.NotNull(_fixture.Persistence.Series);
            Assert.NotNull(_fixture.Persistence.Instance);
            Assert.NotNull(_fixture.Persistence.Measurement);
            Assert.NotNull(_fixture.Persistence.RepositoryLocation);
            Assert.NotNull(_fixture.Persistence.UnrealObject);
            Assert.NotNull(_fixture.Persistence.User);
        }

        [Fact, Priority(2)]
        public async Task User_Should_Be_Saved_And_Loaded()
        {
            User user = new()
            {
                LoginName = "TestUser"
            };

            await _fixture.Persistence!.User!.AddAsync(user, TestContext.Current.CancellationToken);

            User? loaded = await _fixture.Persistence.User.GetByLoginNameAsync("TestUser", TestContext.Current.CancellationToken);

            Assert.NotNull(loaded);
            Assert.Equal("TestUser", loaded.LoginName);

            _fixture.UserId = loaded.Id;
        }

        [Fact, Priority(3)]
        public async Task Study_Should_Be_Saved_And_Loaded()
        {
            Study study = new()
            {
                StudyInstanceUid = _fixture.StudyInstanceUid,
                CreatedByUserId = _fixture.UserId
            };

            await _fixture.Persistence!.Study!.AddAsync(study, TestContext.Current.CancellationToken);

            Study? loaded = await _fixture.Persistence.Study.GetByStudyInstanceUidAsync(_fixture.StudyInstanceUid, TestContext.Current.CancellationToken);

            Assert.NotNull(loaded);

            _fixture.StudyId = loaded.Id;
        }

        [Fact, Priority(4)]
        public async Task Study_Should_Set_CreatedAtUtc()
        {
            Study study = await GetStudyAsync();

            Assert.NotEqual(default, study.CreatedAtUtc);
        }

        [Fact, Priority(5)]
        public async Task Series_Should_Be_Saved_And_Loaded()
        {
            Series series = new()
            {
                StudyId = _fixture.StudyId,
                SeriesInstanceUid = _fixture.SeriesInstanceUid,
                CreatedByUserId = _fixture.UserId
            };

            await _fixture.Persistence!.Series!.AddAsync(series, TestContext.Current.CancellationToken);

            Series? loaded = await _fixture.Persistence.Series.GetBySeriesInstanceUidAsync(_fixture.SeriesInstanceUid, TestContext.Current.CancellationToken);

            Assert.NotNull(loaded);

            _fixture.SeriesId = loaded.Id;
        }

        [Fact, Priority(6)]
        public async Task RepositoryLocation_Should_Be_Saved_And_Loaded()
        {
            RepositoryLocation repositoryLocation = new()
            {
                Name = "Persistence Test Repository",
                RootPath = _fixture.RepositoryLocationRootPath,
                IsEnabled = true,
                IsDefault = true,
                CreatedByUserId = _fixture.UserId
            };

            await _fixture.Persistence!.RepositoryLocation!.AddAsync(repositoryLocation, TestContext.Current.CancellationToken);

            RepositoryLocation? loaded = await _fixture.Persistence.RepositoryLocation.GetByIdAsync(repositoryLocation.Id, TestContext.Current.CancellationToken);

            Assert.NotNull(loaded);
            Assert.Equal("Persistence Test Repository", loaded.Name);
            Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(_fixture.RepositoryLocationRootPath)), loaded.RootPath);
            Assert.True(loaded.IsEnabled);
            Assert.True(loaded.IsDefault);

            _fixture.RepositoryLocationId = loaded.Id;
        }

        [Fact, Priority(7)]
        public async Task Instance_Should_Be_Saved_And_Loaded()
        {
            Instance instance = new()
            {
                SeriesId = _fixture.SeriesId,
                RepositoryLocationId = _fixture.RepositoryLocationId,
                SopInstanceUid = _fixture.SopInstanceUid,
                CreatedByUserId = _fixture.UserId
            };

            await _fixture.Persistence!.Instance!.AddAsync(instance, TestContext.Current.CancellationToken);

            Instance? loaded = await _fixture.Persistence.Instance.GetBySopInstanceUidAsync(_fixture.SopInstanceUid, TestContext.Current.CancellationToken);

            Assert.NotNull(loaded);
            Assert.Equal(_fixture.RepositoryLocationId, loaded.RepositoryLocationId);

            _fixture.InstanceId = loaded.Id;
        }

        [Fact, Priority(8)]
        public async Task Instance_Should_Be_Found_By_SopInstanceUid()
        {
            Instance? loaded = await _fixture.Persistence!.Instance!.GetBySopInstanceUidAsync(_fixture.SopInstanceUid, TestContext.Current.CancellationToken);

            Assert.NotNull(loaded);
            Assert.Equal(_fixture.SopInstanceUid, loaded.SopInstanceUid);
        }

        [Fact, Priority(9)]
        public async Task Measurement_Should_Be_Saved_And_Loaded()
        {
            Measurement measurement = new()
            {
                InstanceId = _fixture.InstanceId,
                CreatedByUserId = _fixture.UserId,
                MeasurementType = MeasurementType.Length,
                Title = "Test Measurement",
                DataJson = "{}"
            };

            await _fixture.Persistence!.Measurement!.AddAsync(measurement, TestContext.Current.CancellationToken);

            Measurement? loaded = await _fixture.Persistence.Measurement.GetByIdAsync(measurement.Id, TestContext.Current.CancellationToken);

            Assert.NotNull(loaded);

            _fixture.MeasurementId = loaded.Id;
        }

        [Fact, Priority(10)]
        public async Task Measurement_Should_Be_Found_By_InstanceId()
        {
            IList<Measurement> measurements = await _fixture.Persistence!.Measurement!.GetByInstanceIdAsync(_fixture.InstanceId, TestContext.Current.CancellationToken);

            Assert.Single(measurements);
            Assert.Equal(_fixture.MeasurementId, measurements[0].Id);
        }

        [Fact, Priority(11)]
        public async Task Study_Should_Be_Updated()
        {
            Study study = await GetStudyAsync();

            study.Description = "Updated";

            await _fixture.Persistence!.Study!.UpdateAsync(study, TestContext.Current.CancellationToken);

            Study loaded = await GetStudyAsync();

            Assert.Equal("Updated", loaded.Description);
        }

        [Fact, Priority(12)]
        public async Task Study_Should_Set_ModifiedAtUtc()
        {
            Study study = await GetStudyAsync();

            Assert.NotNull(study.ModifiedAtUtc);
        }

        [Fact, Priority(13)]
        public async Task Study_Should_Load_Assigned_Series()
        {
            Study study = await GetStudyAsync();

            Assert.Single(study.Series);
            Assert.Equal(_fixture.SeriesId, study.Series.First().Id);
        }

        [Fact, Priority(14)]
        public async Task Integrity_Should_Report_Clean_Persistence()
        {
            PersistenceIntegrityResult result = await VerifyIntegrityAsync();

            /*
             * The baseline contains one user, one study, one series, one instance,
             * one measurement and one repository location. No Unreal object has
             * been created.
             */
            Assert.Equal(6, result.ScannedEntities);
            Assert.Empty(result.Issues);
            Assert.Empty(result.Errors);
        }

        [Fact, Priority(15)]
        public async Task Integrity_Should_Detect_Missing_Series_Parent()
        {
            Series series = new()
            {
                StudyId = int.MaxValue,
                SeriesInstanceUid = $"MissingStudy_{Guid.NewGuid():N}",
                CreatedByUserId = _fixture.UserId
            };

            try
            {
                await _fixture.Persistence!.Series!.AddAsync(series, TestContext.Current.CancellationToken);

                PersistenceIntegrityResult result = await VerifyIntegrityAsync();

                PersistenceIntegrityIssue issue = Assert.Single(result.Issues, item => item.IssueType == PersistenceIntegrityIssueType.MissingParent && item.EntityType == PersistenceIntegrityEntityType.Series && item.EntityId == series.Id);

                Assert.Equal(nameof(Series.StudyId), issue.PropertyName);
                Assert.Equal(PersistenceIntegrityEntityType.Study, issue.ReferencedEntityType);
                Assert.Equal(int.MaxValue, issue.ReferencedEntityId);
            }
            finally
            {
                /*
                 * The shared fixture must return to its clean baseline even
                 * when an assertion or the verification itself fails.
                 */
                if (series.Id > 0)
                {
                    await _fixture.Persistence!.Series!.DeleteAsync(series, TestContext.Current.CancellationToken);
                }
            }
        }

        [Fact, Priority(16)]
        public async Task Integrity_Should_Detect_Missing_Instance_Parent()
        {
            Instance instance = new()
            {
                SeriesId = int.MaxValue,
                RepositoryLocationId = _fixture.RepositoryLocationId,
                SopInstanceUid = $"MissingSeries_{Guid.NewGuid():N}",
                CreatedByUserId = _fixture.UserId
            };

            try
            {
                await _fixture.Persistence!.Instance!.AddAsync(instance, TestContext.Current.CancellationToken);

                PersistenceIntegrityResult result = await VerifyIntegrityAsync();

                PersistenceIntegrityIssue issue = Assert.Single(result.Issues, item => item.IssueType == PersistenceIntegrityIssueType.MissingParent && item.EntityType == PersistenceIntegrityEntityType.Instance && item.EntityId == instance.Id);

                Assert.Equal(nameof(Instance.SeriesId), issue.PropertyName);
                Assert.Equal(PersistenceIntegrityEntityType.Series, issue.ReferencedEntityType);
                Assert.Equal(int.MaxValue, issue.ReferencedEntityId);
            }
            finally
            {
                if (instance.Id > 0)
                {
                    await _fixture.Persistence!.Instance!.DeleteAsync(instance, TestContext.Current.CancellationToken);
                }
            }
        }

        [Fact, Priority(17)]
        public async Task Integrity_Should_Detect_Missing_Instance_RepositoryLocation()
        {
            Instance instance = new()
            {
                SeriesId = _fixture.SeriesId,
                RepositoryLocationId = int.MaxValue,
                SopInstanceUid = $"MissingRepositoryLocation_{Guid.NewGuid():N}",
                CreatedByUserId = _fixture.UserId
            };

            try
            {
                /*
                 * The medical Series relationship remains valid. Only the physical
                 * repository-location relationship is deliberately invalid.
                 */
                await _fixture.Persistence!.Instance!.AddAsync(instance, TestContext.Current.CancellationToken);

                PersistenceIntegrityResult result = await VerifyIntegrityAsync();
                PersistenceIntegrityIssue issue = Assert.Single(result.Issues, item => item.IssueType == PersistenceIntegrityIssueType.MissingParent && item.EntityType == PersistenceIntegrityEntityType.Instance && item.EntityId == instance.Id && item.PropertyName == nameof(Instance.RepositoryLocationId));

                Assert.Equal(PersistenceIntegrityEntityType.RepositoryLocation, issue.ReferencedEntityType);
                Assert.Equal(int.MaxValue, issue.ReferencedEntityId);
            }
            finally
            {
                if (instance.Id > 0)
                {
                    await _fixture.Persistence!.Instance!.DeleteAsync(instance, TestContext.Current.CancellationToken);
                }
            }
        }

        [Fact, Priority(18)]
        public async Task Integrity_Should_Detect_Missing_Measurement_Parent()
        {
            Measurement measurement = new()
            {
                InstanceId = int.MaxValue,
                CreatedByUserId = _fixture.UserId,
                MeasurementType = MeasurementType.Length,
                Title = "Orphaned measurement",
                DataJson = "{}"
            };

            try
            {
                await _fixture.Persistence!.Measurement!.AddAsync(measurement, TestContext.Current.CancellationToken);

                PersistenceIntegrityResult result = await VerifyIntegrityAsync();

                PersistenceIntegrityIssue issue = Assert.Single(result.Issues, item => item.IssueType == PersistenceIntegrityIssueType.MissingParent && item.EntityType == PersistenceIntegrityEntityType.Measurement && item.EntityId == measurement.Id);

                Assert.Equal(nameof(Measurement.InstanceId), issue.PropertyName);
                Assert.Equal(PersistenceIntegrityEntityType.Instance, issue.ReferencedEntityType);
                Assert.Equal(int.MaxValue, issue.ReferencedEntityId);
            }
            finally
            {
                if (measurement.Id > 0)
                {
                    await _fixture.Persistence!.Measurement!.DeleteAsync(measurement, TestContext.Current.CancellationToken);
                }
            }
        }

        [Fact, Priority(19)]
        public async Task Integrity_Should_Detect_Missing_UnrealObject_Parent()
        {
            UnrealObject unrealObject = new()
            {
                InstanceId = int.MaxValue,
                CreatedByUserId = _fixture.UserId,
                Name = "Integrity test object"
            };

            try
            {
                /*
                 * This controlled descriptor only verifies persistence
                 * integrity. It neither represents nor validates a generated
                 * medical 3D reconstruction.
                 */
                await _fixture.Persistence!.UnrealObject!.AddAsync(unrealObject, TestContext.Current.CancellationToken);

                PersistenceIntegrityResult result = await VerifyIntegrityAsync();

                PersistenceIntegrityIssue issue = Assert.Single(result.Issues, item => item.IssueType == PersistenceIntegrityIssueType.MissingParent && item.EntityType == PersistenceIntegrityEntityType.UnrealObject && item.EntityId == unrealObject.Id);

                Assert.Equal(nameof(UnrealObject.InstanceId), issue.PropertyName);
                Assert.Equal(PersistenceIntegrityEntityType.Instance, issue.ReferencedEntityType);
                Assert.Equal(int.MaxValue, issue.ReferencedEntityId);
            }
            finally
            {
                if (unrealObject.Id > 0)
                {
                    await _fixture.Persistence!.UnrealObject!.DeleteAsync(unrealObject, TestContext.Current.CancellationToken);
                }
            }
        }

        [Fact, Priority(20)]
        public async Task Integrity_Should_Detect_Invalid_Audit_Reference()
        {
            Study study = await GetStudyAsync();
            int originalCreatedByUserId = study.CreatedByUserId;

            try
            {
                /*
                 * The test creates a missing audit relationship without
                 * changing the medical Study-Series-Instance hierarchy.
                 */
                study.CreatedByUserId = int.MaxValue;

                await _fixture.Persistence!.Study!.UpdateAsync(study, TestContext.Current.CancellationToken);

                PersistenceIntegrityResult result = await VerifyIntegrityAsync();

                PersistenceIntegrityIssue issue = Assert.Single(result.Issues, item => item.IssueType == PersistenceIntegrityIssueType.InvalidAuditReference && item.EntityType == PersistenceIntegrityEntityType.Study && item.EntityId == study.Id);

                Assert.Equal(nameof(AuditableEntityBase.CreatedByUserId), issue.PropertyName);
                Assert.Equal(PersistenceIntegrityEntityType.User, issue.ReferencedEntityType);
                Assert.Equal(int.MaxValue, issue.ReferencedEntityId);
            }
            finally
            {
                study.CreatedByUserId = originalCreatedByUserId;

                await _fixture.Persistence!.Study!.UpdateAsync(study, TestContext.Current.CancellationToken);
            }
        }

        [Fact, Priority(21)]
        public async Task Integrity_Should_Detect_Missing_Required_Value()
        {
            User user = await GetUserAsync();

            Assert.False(string.IsNullOrWhiteSpace(user.LoginName));

            string originalLoginName = user.LoginName;

            try
            {
                user.LoginName = string.Empty;

                await _fixture.Persistence!.User!.UpdateAsync(user, TestContext.Current.CancellationToken);

                PersistenceIntegrityResult result = await VerifyIntegrityAsync();

                PersistenceIntegrityIssue issue = Assert.Single(result.Issues, item => item.IssueType == PersistenceIntegrityIssueType.MissingRequiredValue && item.EntityType == PersistenceIntegrityEntityType.User && item.EntityId == user.Id);

                Assert.Equal(nameof(User.LoginName), issue.PropertyName);
            }
            finally
            {
                user.LoginName = originalLoginName;

                await _fixture.Persistence!.User!.UpdateAsync(user, TestContext.Current.CancellationToken);
            }
        }

        [Fact, Priority(22)]
        public async Task Integrity_Should_Detect_Duplicate_Unique_Value()
        {
            Study duplicateStudy = new()
            {
                StudyInstanceUid = _fixture.StudyInstanceUid,
                CreatedByUserId = _fixture.UserId
            };

            try
            {
                /*
                 * The EF Core In-Memory provider does not enforce relational
                 * unique indexes. This allows the test to represent a database
                 * damaged outside the normal productive write path.
                 */
                await _fixture.Persistence!.Study!.AddAsync(duplicateStudy, TestContext.Current.CancellationToken);

                PersistenceIntegrityResult result = await VerifyIntegrityAsync();

                IList<PersistenceIntegrityIssue> issues = [.. result.Issues.Where(item => item.IssueType == PersistenceIntegrityIssueType.DuplicateUniqueValue && item.EntityType == PersistenceIntegrityEntityType.Study && item.Value == _fixture.StudyInstanceUid)];

                Assert.Equal(2, issues.Count);
                Assert.Contains(issues, item => item.EntityId == _fixture.StudyId);
                Assert.Contains(issues, item => item.EntityId == duplicateStudy.Id);
                Assert.All(issues, item => Assert.Equal(nameof(Study.StudyInstanceUid), item.PropertyName));
            }
            finally
            {
                if (duplicateStudy.Id > 0)
                {
                    await _fixture.Persistence!.Study!.DeleteAsync(duplicateStudy, TestContext.Current.CancellationToken);
                }
            }
        }

        [Fact, Priority(23)]
        public async Task Integrity_Should_Remain_Clean_After_Conflict_Tests()
        {
            PersistenceIntegrityResult result = await VerifyIntegrityAsync();

            /*
             * Every deliberately damaged state from the preceding tests must
             * have been removed or restored before the fixture is reused.
             */
            Assert.Equal(6, result.ScannedEntities);
            Assert.Empty(result.Issues);
            Assert.Empty(result.Errors);
        }

        [Fact, Priority(24)]
        public async Task DicomImportPersistence_Should_Create_Study_Series_And_Instance_Atomically()
        {
            DicomImportPersistenceRequest request = CreateDicomImportPersistenceRequest();

            await _fixture.Persistence!.DicomImport!.PersistAsync(request, TestContext.Current.CancellationToken);

            Study? study = await _fixture.Persistence.Study!.GetByStudyInstanceUidAsync(request.StudyInstanceUid, TestContext.Current.CancellationToken);
            Series? series = await _fixture.Persistence.Series!.GetBySeriesInstanceUidAsync(request.SeriesInstanceUid, TestContext.Current.CancellationToken);
            Instance? instance = await _fixture.Persistence.Instance!.GetBySopInstanceUidAsync(request.SopInstanceUid, TestContext.Current.CancellationToken);

            Assert.NotNull(study);
            Assert.NotNull(series);
            Assert.NotNull(instance);
            Assert.Equal(study.Id, series.StudyId);
            Assert.Equal(series.Id, instance.SeriesId);
            Assert.Equal(_fixture.RepositoryLocationId, instance.RepositoryLocationId);
            Assert.Equal(request.RelativeFilePath, instance.RelativeFilePath);
            Assert.Equal(_fixture.UserId, study.CreatedByUserId);
            Assert.Equal(_fixture.UserId, series.CreatedByUserId);
            Assert.Equal(_fixture.UserId, instance.CreatedByUserId);
        }

        [Fact, Priority(25)]
        public async Task DicomImportPersistence_Should_Use_Existing_Study_And_Series()
        {
            DicomImportPersistenceRequest request = CreateDicomImportPersistenceRequest(_fixture.StudyInstanceUid, _fixture.SeriesInstanceUid);

            await _fixture.Persistence!.DicomImport!.PersistAsync(request, TestContext.Current.CancellationToken);

            Study? study = await _fixture.Persistence.Study!.GetByStudyInstanceUidAsync(_fixture.StudyInstanceUid, TestContext.Current.CancellationToken);
            Series? series = await _fixture.Persistence.Series!.GetBySeriesInstanceUidAsync(_fixture.SeriesInstanceUid, TestContext.Current.CancellationToken);
            Instance? instance = await _fixture.Persistence.Instance!.GetBySopInstanceUidAsync(request.SopInstanceUid, TestContext.Current.CancellationToken);

            Assert.NotNull(study);
            Assert.NotNull(series);
            Assert.NotNull(instance);
            Assert.Equal(_fixture.StudyId, study.Id);
            Assert.Equal(_fixture.SeriesId, series.Id);
            Assert.Equal(series.Id, instance.SeriesId);
            Assert.Equal(_fixture.RepositoryLocationId, instance.RepositoryLocationId);
            Assert.Equal(request.RelativeFilePath, instance.RelativeFilePath);
        }

        [Fact, Priority(26)]
        public async Task DicomImportPersistence_Should_Remain_Idempotent_For_Existing_Instance()
        {
            DicomImportPersistenceRequest request = CreateDicomImportPersistenceRequest();

            await _fixture.Persistence!.DicomImport!.PersistAsync(request, TestContext.Current.CancellationToken);

            Instance? initialInstance = await _fixture.Persistence.Instance!.GetBySopInstanceUidAsync(request.SopInstanceUid, TestContext.Current.CancellationToken);

            Assert.NotNull(initialInstance);

            await _fixture.Persistence.DicomImport.PersistAsync(request, TestContext.Current.CancellationToken);

            Instance? persistedInstance = await _fixture.Persistence.Instance.GetBySopInstanceUidAsync(request.SopInstanceUid, TestContext.Current.CancellationToken);

            Assert.NotNull(persistedInstance);
            Assert.Equal(initialInstance.Id, persistedInstance.Id);
            Assert.Equal(initialInstance.SeriesId, persistedInstance.SeriesId);
            Assert.Equal(initialInstance.RepositoryLocationId, persistedInstance.RepositoryLocationId);
            Assert.Equal(request.RelativeFilePath, persistedInstance.RelativeFilePath);
        }

        [Fact, Priority(27)]
        public async Task DicomImportPersistence_Should_Not_Create_Entities_When_User_Is_Missing()
        {
            DicomImportPersistenceRequest request = CreateDicomImportPersistenceRequest(createdByUserId: int.MaxValue);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Persistence!.DicomImport!.PersistAsync(request, TestContext.Current.CancellationToken));

            Assert.Contains(int.MaxValue.ToString(), exception.Message, StringComparison.Ordinal);
            await AssertDicomImportEntitiesDoNotExistAsync(request);
        }

        [Fact, Priority(28)]
        public async Task DicomImportPersistence_Should_Not_Create_Entities_When_RepositoryLocation_Is_Missing()
        {
            DicomImportPersistenceRequest request = CreateDicomImportPersistenceRequest(repositoryLocationId: int.MaxValue);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Persistence!.DicomImport!.PersistAsync(request, TestContext.Current.CancellationToken));

            Assert.Contains(int.MaxValue.ToString(), exception.Message, StringComparison.Ordinal);
            await AssertDicomImportEntitiesDoNotExistAsync(request);
        }

        [Fact, Priority(29)]
        public async Task DicomImportPersistence_Should_Not_Create_Study_When_Series_Belongs_To_Different_Study()
        {
            DicomImportPersistenceRequest request = CreateDicomImportPersistenceRequest(seriesInstanceUid: _fixture.SeriesInstanceUid);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Persistence!.DicomImport!.PersistAsync(request, TestContext.Current.CancellationToken));

            Assert.Contains(request.SeriesInstanceUid, exception.Message, StringComparison.Ordinal);

            Study? study = await _fixture.Persistence!.Study!.GetByStudyInstanceUidAsync(request.StudyInstanceUid, TestContext.Current.CancellationToken);
            Series? series = await _fixture.Persistence.Series!.GetBySeriesInstanceUidAsync(_fixture.SeriesInstanceUid, TestContext.Current.CancellationToken);
            Instance? instance = await _fixture.Persistence.Instance!.GetBySopInstanceUidAsync(request.SopInstanceUid, TestContext.Current.CancellationToken);

            Assert.Null(study);
            Assert.NotNull(series);
            Assert.Equal(_fixture.StudyId, series.StudyId);
            Assert.Null(instance);
        }

        [Fact, Priority(30)]
        public async Task DicomImportPersistence_Should_Reject_Existing_Instance_In_Different_RepositoryLocation()
        {
            RepositoryLocation secondaryRepositoryLocation = new()
            {
                Name = $"Atomic Import Secondary Location {Guid.NewGuid():N}",
                RootPath = Path.Combine(Path.GetTempPath(), "MoprPersistenceTests", Guid.NewGuid().ToString("N")),
                IsEnabled = true,
                IsDefault = false,
                CreatedByUserId = _fixture.UserId
            };

            await _fixture.Persistence!.RepositoryLocation!.AddAsync(secondaryRepositoryLocation, TestContext.Current.CancellationToken);

            DicomImportPersistenceRequest initialRequest = CreateDicomImportPersistenceRequest();

            await _fixture.Persistence.DicomImport!.PersistAsync(initialRequest, TestContext.Current.CancellationToken);

            DicomImportPersistenceRequest conflictingRequest = CreateDicomImportPersistenceRequest(
                initialRequest.StudyInstanceUid,
                initialRequest.SeriesInstanceUid,
                initialRequest.SopInstanceUid,
                repositoryLocationId: secondaryRepositoryLocation.Id);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Persistence.DicomImport.PersistAsync(conflictingRequest, TestContext.Current.CancellationToken));

            Assert.Contains(_fixture.RepositoryLocationId.ToString(), exception.Message, StringComparison.Ordinal);
            Assert.Contains(secondaryRepositoryLocation.Id.ToString(), exception.Message, StringComparison.Ordinal);

            Instance? instance = await _fixture.Persistence.Instance!.GetBySopInstanceUidAsync(initialRequest.SopInstanceUid, TestContext.Current.CancellationToken);

            Assert.NotNull(instance);
            Assert.Equal(_fixture.RepositoryLocationId, instance.RepositoryLocationId);
            Assert.Equal(initialRequest.RelativeFilePath, instance.RelativeFilePath);
        }

        [Fact, Priority(31)]
        public async Task DicomImportPersistence_Should_Propagate_Cancellation_Without_Creating_Entities()
        {
            DicomImportPersistenceRequest request = CreateDicomImportPersistenceRequest();
            using CancellationTokenSource cancellationTokenSource = new();

            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _fixture.Persistence!.DicomImport!.PersistAsync(request, cancellationTokenSource.Token));

            await AssertDicomImportEntitiesDoNotExistAsync(request);
        }

        [Fact, Priority(32)]
        public async Task DicomImportPersistence_Should_Reject_NonCanonical_RelativeFilePath()
        {
            DicomImportPersistenceRequest request = CreateDicomImportPersistenceRequest();
            request.RelativeFilePath = Path.Combine(request.StudyInstanceUid, "DifferentSeries", $"{request.SopInstanceUid}.dcm");

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Persistence!.DicomImport!.PersistAsync(request, TestContext.Current.CancellationToken));

            Assert.Contains("canonical", exception.Message, StringComparison.OrdinalIgnoreCase);
            await AssertDicomImportEntitiesDoNotExistAsync(request);
        }

        [Fact, Priority(33)]
        public async Task DicomImportPersistence_Should_Reject_Traversal_In_Dicom_Uid()
        {
            string seriesInstanceUid = $"Series_{Guid.NewGuid():N}";
            string sopInstanceUid = $"Sop_{Guid.NewGuid():N}";

            DicomImportPersistenceRequest request = new()
            {
                CreatedByUserId = _fixture.UserId,
                RepositoryLocationId = _fixture.RepositoryLocationId,
                StudyInstanceUid = "..",
                SeriesInstanceUid = seriesInstanceUid,
                SopInstanceUid = sopInstanceUid,
                RelativeFilePath = Path.Combine("..", seriesInstanceUid, $"{sopInstanceUid}.dcm")
            };

            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Persistence!.DicomImport!.PersistAsync(request, TestContext.Current.CancellationToken));

            Assert.Contains("path segment", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Null(await _fixture.Persistence!.Series!.GetBySeriesInstanceUidAsync(seriesInstanceUid, TestContext.Current.CancellationToken));
            Assert.Null(await _fixture.Persistence.Instance!.GetBySopInstanceUidAsync(sopInstanceUid, TestContext.Current.CancellationToken));
        }

        [Fact, Priority(34)]
        public async Task DicomImportPersistence_Should_Remain_Consistent_After_Atomicity_Tests()
        {
            PersistenceIntegrityResult result = await VerifyIntegrityAsync();

            /*
             * Successful atomic import scenarios add valid entities to the shared
             * fixture. Failed and cancelled scenarios must not introduce incomplete
             * relationships or invalid audit references.
             */
            Assert.Empty(result.Issues);
            Assert.Empty(result.Errors);
        }
    }
}