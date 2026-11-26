using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using System.Collections.Generic;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.Services
{
    public sealed class ViewportSynchronizationServiceTests
    {
        [Fact]
        public void Service_Should_Start_Without_Groups()
        {
            var service = new ViewportSynchronizationService();

            Assert.Empty(service.GetGroups());
            Assert.Empty(service.GetRoutes("viewport-a", ViewportSynchronizationFlags.Slice));
        }

        [Fact]
        public void Group_Creation_And_Updates_Should_Preserve_Identity_And_Immutable_Snapshots()
        {
            var service = new ViewportSynchronizationService();
            ViewportSynchronizationGroup created = service.CreateGroup("Initial", ["viewport-b", "viewport-a", "viewport-a"], ViewportSynchronizationFlags.Slice);

            ViewportSynchronizationGroup renamed = service.RenameGroup(created.Id, "Renamed");
            ViewportSynchronizationGroup updatedFlags = service.SetGroupFlags(created.Id, ViewportSynchronizationFlags.Pan);
            ViewportSynchronizationGroup added = service.AddViewport(created.Id, "viewport-c");
            ViewportSynchronizationGroup duplicate = service.AddViewport(created.Id, "viewport-c");

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal("Initial", created.Name);
            Assert.Equal(new[] { "viewport-a", "viewport-b" }, created.ViewportIds);
            Assert.Equal("Renamed", renamed.Name);
            Assert.Equal(ViewportSynchronizationFlags.Slice, renamed.Flags);
            Assert.Equal(ViewportSynchronizationFlags.Pan, updatedFlags.Flags);
            Assert.Equal(new[] { "viewport-a", "viewport-b", "viewport-c" }, added.ViewportIds);
            Assert.Same(added, duplicate);
            Assert.Equal(created.Id, service.GetGroup(created.Id)!.Id);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)created.ViewportIds).Add("viewport-x"));
            Assert.Throws<NotSupportedException>(() => ((IList<ViewportSynchronizationGroup>)service.GetGroups()).Add(created));
        }

        [Fact]
        public void Removing_Viewport_Should_Keep_Group_And_Stop_Routing_When_Only_One_Member_Remains()
        {
            var service = new ViewportSynchronizationService();
            ViewportSynchronizationGroup group = service.CreateGroup("Pair", ["viewport-a", "viewport-b"], ViewportSynchronizationFlags.Zoom);

            ViewportSynchronizationGroup updated = service.RemoveViewport(group.Id, "viewport-b");

            Assert.Equal(group.Id, updated.Id);
            Assert.Equal(new[] { "viewport-a" }, updated.ViewportIds);
            Assert.NotNull(service.GetGroup(group.Id));
            Assert.Empty(service.GetRoutes("viewport-a", ViewportSynchronizationFlags.Zoom));
            Assert.Same(updated, service.RemoveViewport(group.Id, "missing"));
        }

        [Fact]
        public void Removing_Group_Should_Stop_Routing_And_Report_Unknown_Group()
        {
            var service = new ViewportSynchronizationService();
            ViewportSynchronizationGroup group = service.CreateGroup("Pair", ["viewport-a", "viewport-b"], ViewportSynchronizationFlags.Pan);

            Assert.True(service.RemoveGroup(group.Id));
            Assert.False(service.RemoveGroup(group.Id));
            Assert.Null(service.GetGroup(group.Id));
            Assert.Empty(service.GetRoutes("viewport-a", ViewportSynchronizationFlags.Pan));
            Assert.Throws<KeyNotFoundException>(() => service.RenameGroup(group.Id, "Unknown"));
            Assert.Throws<KeyNotFoundException>(() => service.SetGroupFlags(group.Id, ViewportSynchronizationFlags.Zoom));
            Assert.Throws<KeyNotFoundException>(() => service.AddViewport(group.Id, "viewport-c"));
            Assert.Throws<KeyNotFoundException>(() => service.RemoveViewport(group.Id, "viewport-a"));
        }

        [Fact]
        public void None_And_Disabled_Flags_Should_Not_Create_Routes()
        {
            var service = new ViewportSynchronizationService();
            service.CreateGroup("Disabled", ["viewport-a", "viewport-b"], ViewportSynchronizationFlags.None);
            service.CreateGroup("Other category", ["viewport-a", "viewport-c"], ViewportSynchronizationFlags.Pan);

            Assert.Empty(service.GetRoutes("viewport-a", ViewportSynchronizationFlags.None));
            Assert.Empty(service.GetRoutes("viewport-a", ViewportSynchronizationFlags.Zoom));
        }

        [Fact]
        public void Enabled_Flag_Should_Route_From_Source_To_Each_Other_Member()
        {
            var service = new ViewportSynchronizationService();
            ViewportSynchronizationGroup group = service.CreateGroup("Three", ["viewport-c", "viewport-a", "viewport-b"], ViewportSynchronizationFlags.Slice);

            IReadOnlyList<ViewportSynchronizationRoute> routes = service.GetRoutes("viewport-a", ViewportSynchronizationFlags.Slice);

            Assert.Equal(new[] { "viewport-b", "viewport-c" }, routes.Select(route => route.TargetViewportId));
            Assert.All(routes, route =>
            {
                Assert.Equal("viewport-a", route.SourceViewportId);
                Assert.Equal(ViewportSynchronizationFlags.Slice, route.SynchronizationType);
                Assert.Equal(group.Id, route.GroupId);
            });
        }

        [Fact]
        public void Pair_Should_Produce_Exactly_One_Directed_Route_And_Exclude_Source()
        {
            var service = new ViewportSynchronizationService();
            service.CreateGroup("Pair", ["viewport-b", "viewport-a"], ViewportSynchronizationFlags.WindowLevel);

            ViewportSynchronizationRoute route = Assert.Single(service.GetRoutes("viewport-a", ViewportSynchronizationFlags.WindowLevel));

            Assert.Equal("viewport-a", route.SourceViewportId);
            Assert.Equal("viewport-b", route.TargetViewportId);
        }

        [Fact]
        public void Groups_Should_Remain_Isolated_And_Unknown_Source_Should_Have_No_Routes()
        {
            var service = new ViewportSynchronizationService();
            service.CreateGroup("First", ["viewport-a", "viewport-b"], ViewportSynchronizationFlags.Slice);
            service.CreateGroup("Second", ["viewport-c", "viewport-d"], ViewportSynchronizationFlags.Slice);

            Assert.Equal("viewport-b", Assert.Single(service.GetRoutes("viewport-a", ViewportSynchronizationFlags.Slice)).TargetViewportId);
            Assert.Empty(service.GetRoutes("viewport-unknown", ViewportSynchronizationFlags.Slice));
        }

        [Fact]
        public void Shared_Target_Should_Appear_Once_Using_Deterministic_Group_Identity()
        {
            var service = new ViewportSynchronizationService();
            ViewportSynchronizationGroup first = service.CreateGroup("First", ["viewport-a", "viewport-z"], ViewportSynchronizationFlags.Pan);
            ViewportSynchronizationGroup second = service.CreateGroup("Second", ["viewport-a", "viewport-z"], ViewportSynchronizationFlags.Pan);

            ViewportSynchronizationRoute route = Assert.Single(service.GetRoutes("viewport-a", ViewportSynchronizationFlags.Pan));

            Assert.Equal("viewport-z", route.TargetViewportId);
            Assert.Equal(first.Id.CompareTo(second.Id) < 0 ? first.Id : second.Id, route.GroupId);
        }

        [Theory]
        [InlineData(ViewportSynchronizationFlags.Slice)]
        [InlineData(ViewportSynchronizationFlags.WindowLevel)]
        [InlineData(ViewportSynchronizationFlags.Zoom)]
        [InlineData(ViewportSynchronizationFlags.Pan)]
        [InlineData(ViewportSynchronizationFlags.Rotation)]
        [InlineData(ViewportSynchronizationFlags.HorizontalFlip)]
        [InlineData(ViewportSynchronizationFlags.VerticalFlip)]
        public void Every_Supported_Flag_Should_Create_Routes(ViewportSynchronizationFlags flag)
        {
            var service = new ViewportSynchronizationService();
            service.CreateGroup("Enabled", ["viewport-a", "viewport-b"], flag);

            Assert.Equal(flag, Assert.Single(service.GetRoutes("viewport-a", flag)).SynchronizationType);
        }

        [Fact]
        public void Routes_Should_Be_Sorted_Ordinally_And_Viewport_IDs_Are_Case_Sensitive()
        {
            var service = new ViewportSynchronizationService();
            service.CreateGroup("Case-sensitive", ["source", "Target", "target", "alpha"], ViewportSynchronizationFlags.Rotation);

            IReadOnlyList<ViewportSynchronizationRoute> routes = service.GetRoutes("source", ViewportSynchronizationFlags.Rotation);

            Assert.Equal(new[] { "Target", "alpha", "target" }, routes.Select(route => route.TargetViewportId));
            Assert.Equal(2, service.GetGroupsForViewport("Target").Count + service.GetGroupsForViewport("target").Count);
            Assert.Empty(service.GetRoutes("Source", ViewportSynchronizationFlags.Rotation));
        }

        [Fact]
        public void Invalid_Flags_Should_Be_Rejected_For_Groups_And_Routing()
        {
            var service = new ViewportSynchronizationService();
            ViewportSynchronizationGroup group = service.CreateGroup("Valid", ["viewport-a", "viewport-b"]);
            var combined = ViewportSynchronizationFlags.Slice | ViewportSynchronizationFlags.Zoom;
            var unknown = (ViewportSynchronizationFlags)128;

            Assert.Equal(combined, service.CreateGroup("Combined", ["viewport-a"], combined).Flags);
            Assert.Throws<ArgumentOutOfRangeException>(() => service.SetGroupFlags(group.Id, unknown));
            Assert.Empty(service.GetRoutes("viewport-a", ViewportSynchronizationFlags.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.GetRoutes("viewport-a", combined));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.GetRoutes("viewport-a", unknown));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ViewportSynchronizationRoute(group.Id, "viewport-a", "viewport-b", combined));
        }

        [Fact]
        public void Invalid_Group_And_Viewport_Identities_Should_Be_Rejected()
        {
            Assert.Throws<ArgumentException>(() => new ViewportSynchronizationGroup(Guid.Empty, "Group", ["viewport-a"], ViewportSynchronizationFlags.None));
            Assert.Throws<ArgumentException>(() => new ViewportSynchronizationGroup(Guid.NewGuid(), " ", ["viewport-a"], ViewportSynchronizationFlags.None));
            Assert.Throws<ArgumentException>(() => new ViewportSynchronizationGroup(Guid.NewGuid(), "Group", [" ", "viewport-a"], ViewportSynchronizationFlags.None));
            Assert.Throws<ArgumentException>(() => new ViewportSynchronizationRoute(Guid.NewGuid(), "viewport-a", "viewport-a", ViewportSynchronizationFlags.Slice));
            Assert.Throws<ArgumentException>(() => new ViewportSynchronizationRoute(Guid.NewGuid(), "", "viewport-b", ViewportSynchronizationFlags.Slice));
            Assert.Throws<ArgumentException>(() => new ViewportSynchronizationRoute(Guid.NewGuid(), "viewport-a", " ", ViewportSynchronizationFlags.Slice));
        }

        [Fact]
        public void Service_Should_Reject_Invalid_Arguments_And_Return_Empty_For_Unknown_Group_Queries()
        {
            var service = new ViewportSynchronizationService();

            Assert.Null(service.GetGroup(Guid.NewGuid()));
            Assert.Empty(service.GetGroupsForViewport("viewport-a"));
            Assert.Throws<ArgumentException>(() => service.CreateGroup("Group", ["viewport-a", ""], ViewportSynchronizationFlags.None));
            Assert.Throws<ArgumentException>(() => service.CreateGroup("", ["viewport-a"], ViewportSynchronizationFlags.None));
            Assert.Throws<ArgumentException>(() => service.GetGroupsForViewport(" "));
            Assert.Throws<ArgumentException>(() => service.GetRoutes(" ", ViewportSynchronizationFlags.Slice));
            Assert.Throws<ArgumentException>(() => service.GetGroup(Guid.Empty));
            Assert.Throws<ArgumentException>(() => service.RemoveGroup(Guid.Empty));
        }

        [Fact]
        public void Parallel_Group_Queries_And_Updates_Should_Return_Consistent_Snapshots()
        {
            var service = new ViewportSynchronizationService();
            ViewportSynchronizationGroup group = service.CreateGroup("Concurrent", ["source", "initial-target"], ViewportSynchronizationFlags.Slice);

            Parallel.For(0, 128, index =>
            {
                service.AddViewport(group.Id, $"target-{index:D3}");
                Assert.NotEmpty(service.GetGroups());
                Assert.NotEmpty(service.GetRoutes("source", ViewportSynchronizationFlags.Slice));
                Assert.Contains(service.GetGroupsForViewport("source"), item => item.Id == group.Id);
            });

            ViewportSynchronizationGroup result = Assert.Single(service.GetGroups());
            Assert.Equal(130, result.ViewportIds.Count);
            Assert.Equal(129, service.GetRoutes("source", ViewportSynchronizationFlags.Slice).Count);
        }
    }
}
