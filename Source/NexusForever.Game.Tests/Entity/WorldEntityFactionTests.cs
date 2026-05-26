using NexusForever.Game.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Tests.Entity;

public class WorldEntityFactionTests
{
    [Fact]
    public void GetDispositionTo_NoneFaction_ReturnsUnknown()
    {
        var entity = new TestWorldEntity();

        Assert.Equal(Disposition.Unknown, entity.GetDispositionTo(Faction.None));
    }

    private sealed class TestWorldEntity : WorldEntity
    {
        public override EntityType Type => EntityType.Simple;

        public TestWorldEntity()
            : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new SimpleEntityModel();
        }
    }
}
