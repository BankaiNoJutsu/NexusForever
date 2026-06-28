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
    private const Faction ColdburrowFaction = (Faction)463u;

    [Fact]
    public void GetDispositionTo_NoneFaction_ReturnsUnknown()
    {
        var entity = new TestWorldEntity();

        Assert.Equal(Disposition.Unknown, entity.GetDispositionTo(Faction.None));
    }

    [Fact]
    public void GetDispositionTo_SamePrimaryFaction_ReturnsFriendly()
    {
        var entity = new TestWorldEntity
        {
            Faction1 = ColdburrowFaction,
            Faction2 = ColdburrowFaction
        };

        Assert.Equal(Disposition.Friendly, entity.GetDispositionTo(ColdburrowFaction));
    }

    [Fact]
    public void GetDispositionTo_SameSecondaryFaction_ReturnsFriendly()
    {
        var entity = new TestWorldEntity
        {
            Faction1 = Faction.Dominion,
            Faction2 = ColdburrowFaction
        };

        Assert.Equal(Disposition.Friendly, entity.GetDispositionTo(ColdburrowFaction, primary: false));
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
