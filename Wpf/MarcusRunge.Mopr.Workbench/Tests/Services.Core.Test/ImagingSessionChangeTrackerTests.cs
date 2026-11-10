using MarcusRunge.Base;
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
        public void MarkUnsaved_Should_Register_Change_And_Raise_Event()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var eventCount = 0;
            tracker.UnsavedChangesChanged += (_, _) => eventCount++;
            var change = new ImagingUnsavedChange("Measurement", "Length measurement");

            tracker.MarkUnsaved("MeasurementService", "measurement-1", change);

            Assert.True(tracker.HasUnsavedChanges);
            Assert.Same(change, Assert.Single(tracker.UnsavedChanges));
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void MarkUnsaved_Should_Replace_Existing_Change_Under_Same_Owner_And_Key()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            tracker.MarkUnsaved("MeasurementService", "measurement-1", new ImagingUnsavedChange("Measurement", "Initial value"));

            tracker.MarkUnsaved("MeasurementService", "measurement-1", new ImagingUnsavedChange("Measurement", "Updated value"));

            ImagingUnsavedChange change = Assert.Single(tracker.UnsavedChanges);
            Assert.Equal("Updated value", change.Description);
        }

        [Fact]
        public void Different_Owners_Should_Not_Collide_When_Keys_Are_Equal()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();

            tracker.MarkUnsaved("MeasurementService", "item-1", new ImagingUnsavedChange("Measurement", "Measurement change"));
            tracker.MarkUnsaved("AnnotationService", "item-1", new ImagingUnsavedChange("Annotation", "Annotation change"));

            Assert.Equal(2, tracker.UnsavedChanges.Count);
        }

        [Fact]
        public void MarkSaved_Should_Remove_Only_Requested_Change()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            tracker.MarkUnsaved("MeasurementService", "measurement-1", new ImagingUnsavedChange("Measurement", "First"));
            tracker.MarkUnsaved("MeasurementService", "measurement-2", new ImagingUnsavedChange("Measurement", "Second"));
            var eventCount = 0;
            tracker.UnsavedChangesChanged += (_, _) => eventCount++;

            bool removed = tracker.MarkSaved("MeasurementService", "measurement-1");

            Assert.True(removed);
            Assert.True(tracker.HasUnsavedChanges);
            Assert.Equal("Second", Assert.Single(tracker.UnsavedChanges).Description);
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void MarkSaved_Should_Not_Raise_Event_When_Change_Does_Not_Exist()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var eventCount = 0;
            tracker.UnsavedChangesChanged += (_, _) => eventCount++;

            bool removed = tracker.MarkSaved("MeasurementService", "missing");

            Assert.False(removed);
            Assert.False(tracker.HasUnsavedChanges);
            Assert.Equal(0, eventCount);
        }

        [Fact]
        public void Clear_Should_Remove_All_Changes_And_Raise_One_Event()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            tracker.MarkUnsaved("MeasurementService", "measurement-1", new ImagingUnsavedChange("Measurement", "First"));
            tracker.MarkUnsaved("AnnotationService", "annotation-1", new ImagingUnsavedChange("Annotation", "Second"));
            var eventCount = 0;
            tracker.UnsavedChangesChanged += (_, _) => eventCount++;

            tracker.Clear();

            Assert.False(tracker.HasUnsavedChanges);
            Assert.Empty(tracker.UnsavedChanges);
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void Clear_Should_Not_Raise_Event_When_Tracker_Is_Already_Empty()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            var eventCount = 0;
            tracker.UnsavedChangesChanged += (_, _) => eventCount++;

            tracker.Clear();

            Assert.Equal(0, eventCount);
        }

        [Fact]
        public void UnsavedChanges_Should_Return_Stable_Snapshot()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();
            tracker.MarkUnsaved("MeasurementService", "measurement-1", new ImagingUnsavedChange("Measurement", "First"));
            IReadOnlyList<ImagingUnsavedChange> snapshot = tracker.UnsavedChanges;

            tracker.MarkUnsaved("MeasurementService", "measurement-2", new ImagingUnsavedChange("Measurement", "Second"));

            Assert.Single(snapshot);
            Assert.Equal(2, tracker.UnsavedChanges.Count);
        }

        [Theory]
        [InlineData("", "key")]
        [InlineData("   ", "key")]
        [InlineData("owner", "")]
        [InlineData("owner", "   ")]
        public void MarkUnsaved_Should_Reject_Invalid_Identity(string owner, string key)
        {
            IImagingSessionChangeTracker tracker = CreateTracker();

            Assert.Throws<ArgumentException>(() => tracker.MarkUnsaved(owner, key, new ImagingUnsavedChange("Source", "Description")));
        }

        [Fact]
        public void MarkUnsaved_Should_Reject_Null_Change()
        {
            IImagingSessionChangeTracker tracker = CreateTracker();

            Assert.Throws<ArgumentNullException>(() => tracker.MarkUnsaved("owner", "key", null!));
        }

        private static IImagingSessionChangeTracker CreateTracker()
        {
            var imagingServiceBase = new Mock<IImagingServiceBase>();
            return ImagingSessionChangeTracker.Create(imagingServiceBase.Object, CreationLifetime.Scoped) ?? throw new InvalidOperationException("The imaging session change tracker could not be created.");
        }
    }
}