using System.Numerics;
using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Map;

public class BaseMapRelocateTests
{
    [Fact]
    public void RelocateEntity_WithInvalidCurrentPosition_RecoversToDestination()
    {
        var map = new TestBaseMap();
        IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out RecordingDispatchProxy<IGridEntity> entityProxy);
        Vector3 destination = new(10f, 0f, 10f);

        entityProxy.SetProperty(nameof(IGridEntity.Guid), 57u);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        entityProxy.SetProperty(nameof(IGridEntity.Position), new Vector3(float.NaN, float.NaN, float.NaN));
        entityProxy.SetProperty(nameof(IGridEntity.ActivationRange), 0f);

        map.RelocateForTest(entity, destination);

        RecordingDispatchProxy<IGridEntity>.Invocation relocate = Assert.Single(entityProxy.GetInvocations(nameof(IGridEntity.OnRelocate)));
        Assert.Equal(destination, relocate.Arguments[0]);
    }

    private sealed class TestBaseMap : BaseMap
    {
        public TestBaseMap()
            : base(
                RecordingDispatchProxy<IEntityFactory>.Create(out _),
                RecordingDispatchProxy<IPublicEventManager>.Create(out _))
        {
            PropertyInfo entryProperty = typeof(BaseMap).GetProperty(nameof(Entry))!;
            entryProperty.SetValue(this, new WorldEntry { Id = 426u });
        }

        public void RelocateForTest(IGridEntity entity, Vector3 vector)
        {
            RelocateEntity(entity, vector);
        }

        protected override void SpawnGrid(uint gridX, uint gridZ)
        {
        }
    }
}
