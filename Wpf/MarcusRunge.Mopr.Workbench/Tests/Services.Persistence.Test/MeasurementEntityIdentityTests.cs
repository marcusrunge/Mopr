using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
    public sealed class MeasurementEntityIdentityTests
    {
        [Fact]
        public void Model_Should_Configure_Stable_Measurement_Entity_Id_As_Required_And_Unique()
        {
            var options = new DbContextOptionsBuilder<PersistenceDbContext>().UseInMemoryDatabase($"measurement-model-{Guid.NewGuid():N}").Options;
            using var context = new PersistenceDbContext(options);
            IEntityType entityType = context.Model.FindEntityType(typeof(Measurement)) ?? throw new InvalidOperationException("The Measurement entity is not part of the persistence model.");
            IProperty entityIdProperty = entityType.FindProperty(nameof(Measurement.EntityId)) ?? throw new InvalidOperationException("The Measurement EntityId property is not configured.");
            IIndex entityIdIndex = Assert.Single(entityType.GetIndexes(), index => index.Properties.Count == 1 && index.Properties[0].Name == nameof(Measurement.EntityId));

            Assert.False(entityIdProperty.IsNullable);
            Assert.True(entityIdIndex.IsUnique);
        }

        [Fact]
        public void Measurement_Should_Preserve_Assigned_Stable_Entity_Id()
        {
            var entityId = Guid.NewGuid();
            var measurement = new Measurement { EntityId = entityId };

            Assert.Equal(entityId, measurement.EntityId);
        }
    }
}