using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Test
{
    public sealed class ImagingSessionChangeTrackerTests
    {
        [Fact]
        public void Created_Should_Register_Change_And_Raise_Event()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var eventCount = 0;
            tracker.UnsavedChangesChanged += (_, _) => eventCount++;
            var identity = CreateIdentity();
            var currentValue = new TestData("Created");

            tracker.MarkChanged(identity, ImagingSessionChangeState.Created, originalValue: null, currentValue);

            var change = Assert.IsType<ImagingSessionChange<TestData>>(Assert.Single(tracker.UnsavedChanges));
            Assert.Equal(ImagingSessionChangeState.Created, change.State);
            Assert.Null(change.OriginalValue);
            Assert.Equal(currentValue, change.CurrentValue);
            Assert.True(change.Version > 0);
            Assert.True(tracker.HasUnsavedChanges);
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void Created_To_Modified_Should_Remain_Created_With_Latest_Value()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var identity = CreateIdentity();
            tracker.MarkChanged(identity, ImagingSessionChangeState.Created, originalValue: null, new TestData("Created"));

            tracker.MarkChanged(identity, ImagingSessionChangeState.Modified, new TestData("Created"), new TestData("Modified"));

            var change = Assert.IsType<ImagingSessionChange<TestData>>(Assert.Single(tracker.UnsavedChanges));
            Assert.Equal(ImagingSessionChangeState.Created, change.State);
            Assert.Null(change.OriginalValue);
            Assert.Equal("Modified", change.CurrentValue!.Value);
        }

        [Fact]
        public void Created_To_Deleted_Should_Remove_Change()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var identity = CreateIdentity();
            var value = new TestData("Created");
            tracker.MarkChanged(identity, ImagingSessionChangeState.Created, originalValue: null, value);

            tracker.MarkChanged(identity, ImagingSessionChangeState.Deleted, value, currentValue: null);

            Assert.False(tracker.HasUnsavedChanges);
            Assert.Empty(tracker.UnsavedChanges);
        }

        [Fact]
        public void Modified_To_Modified_Should_Preserve_Original_And_Use_Latest_Value()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var identity = CreateIdentity();
            var originalValue = new TestData("Persisted");
            tracker.MarkChanged(identity, ImagingSessionChangeState.Modified, originalValue, new TestData("First"));

            tracker.MarkChanged(identity, ImagingSessionChangeState.Modified, new TestData("First"), new TestData("Second"));

            var change = Assert.IsType<ImagingSessionChange<TestData>>(Assert.Single(tracker.UnsavedChanges));
            Assert.Equal(ImagingSessionChangeState.Modified, change.State);
            Assert.Equal(originalValue, change.OriginalValue);
            Assert.Equal("Second", change.CurrentValue!.Value);
        }

        [Fact]
        public void Modified_To_Deleted_Should_Preserve_Original_Value()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var identity = CreateIdentity();
            var originalValue = new TestData("Persisted");
            tracker.MarkChanged(identity, ImagingSessionChangeState.Modified, originalValue, new TestData("Modified"));

            tracker.MarkChanged(identity, ImagingSessionChangeState.Deleted, new TestData("Modified"), currentValue: null);

            var change = Assert.IsType<ImagingSessionChange<TestData>>(Assert.Single(tracker.UnsavedChanges));
            Assert.Equal(ImagingSessionChangeState.Deleted, change.State);
            Assert.Equal(originalValue, change.OriginalValue);
            Assert.Null(change.CurrentValue);
        }

        [Fact]
        public void Deleted_To_Created_Should_Become_Modified_Restoration()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var identity = CreateIdentity();
            var originalValue = new TestData("Persisted");
            tracker.MarkChanged(identity, ImagingSessionChangeState.Deleted, originalValue, currentValue: null);

            tracker.MarkChanged(identity, ImagingSessionChangeState.Created, originalValue: null, new TestData("Restored"));

            var change = Assert.IsType<ImagingSessionChange<TestData>>(Assert.Single(tracker.UnsavedChanges));
            Assert.Equal(ImagingSessionChangeState.Modified, change.State);
            Assert.Equal(originalValue, change.OriginalValue);
            Assert.Equal("Restored", change.CurrentValue!.Value);
        }

        [Fact]
        public void MarkSaved_Should_Remove_Only_Exact_Snapshot_Version()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var identity = CreateIdentity();
            tracker.MarkChanged(identity, ImagingSessionChangeState.Modified, new TestData("Persisted"), new TestData("First"));
            var savedSnapshot = Assert.Single(tracker.UnsavedChanges);
            tracker.MarkChanged(identity, ImagingSessionChangeState.Modified, new TestData("First"), new TestData("Second"));

            var staleSnapshotRemoved = tracker.MarkSaved(identity, savedSnapshot.Version);
            var currentSnapshot = Assert.Single(tracker.UnsavedChanges);
            var currentSnapshotRemoved = tracker.MarkSaved(identity, currentSnapshot.Version);

            Assert.False(staleSnapshotRemoved);
            Assert.True(currentSnapshotRemoved);
            Assert.False(tracker.HasUnsavedChanges);
        }

        [Fact]
        public void Different_Studies_Should_Not_Collide_When_Entity_Ids_Are_Equal()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var entityId = Guid.NewGuid();
            var firstIdentity = CreateIdentity(entityId, "1.2.3");
            var secondIdentity = CreateIdentity(entityId, "4.5.6");

            tracker.MarkChanged(firstIdentity, ImagingSessionChangeState.Created, originalValue: null, new TestData("First"));
            tracker.MarkChanged(secondIdentity, ImagingSessionChangeState.Created, originalValue: null, new TestData("Second"));

            Assert.Equal(2, tracker.UnsavedChanges.Count);
        }

        [Fact]
        public void UnsavedChanges_Should_Return_Stable_Snapshot()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            tracker.MarkChanged(CreateIdentity(), ImagingSessionChangeState.Created, originalValue: null, new TestData("First"));
            IReadOnlyList<IImagingSessionChange> snapshot = tracker.UnsavedChanges;

            tracker.MarkChanged(CreateIdentity(), ImagingSessionChangeState.Created, originalValue: null, new TestData("Second"));

            Assert.Single(snapshot);
            Assert.Equal(2, tracker.UnsavedChanges.Count);
        }

        [Fact]
        public void Clear_Should_Raise_Event_Only_When_Changes_Exist()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var eventCount = 0;
            tracker.UnsavedChangesChanged += (_, _) => eventCount++;
            tracker.MarkChanged(CreateIdentity(), ImagingSessionChangeState.Created, originalValue: null, new TestData("Created"));

            tracker.Clear();
            tracker.Clear();

            Assert.Equal(2, eventCount);
            Assert.False(tracker.HasUnsavedChanges);
        }

        [Fact]
        public void MarkChanged_Should_Reject_Payload_Type_Change_For_Same_Identity()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var identity = CreateIdentity();
            tracker.MarkChanged(identity, ImagingSessionChangeState.Created, originalValue: null, new TestData("Created"));

            Assert.Throws<InvalidOperationException>(() => tracker.MarkChanged(identity, ImagingSessionChangeState.Modified, "Created", "Modified"));
        }

        [Fact]
        public void MarkChanged_Should_Reject_Null_Identity()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();

            Assert.Throws<ArgumentNullException>(() => tracker.MarkChanged<TestData>(null!, ImagingSessionChangeState.Created, originalValue: null, new TestData("Created")));
        }

        private static ImagingSessionChangeIdentity CreateIdentity(Guid? entityId = null, string studyInstanceUid = "1.2.840.113619.2.55.3") => new("Measurement", entityId ?? Guid.NewGuid(), studyInstanceUid, "1.2.840.113619.2.55.3.1", "1.2.840.113619.2.55.3.1.1");

        private static IImagingSessionChangeTracker CreateTracker()
        {
            var imagingServiceBase = new Mock<IImagingServiceBase>();
            return ImagingSessionChangeTracker.Create(imagingServiceBase.Object, CreationLifetime.Scoped) ?? throw new InvalidOperationException("The imaging session change tracker could not be created.");
        }

        private sealed record TestData(string Value);
    }
}
