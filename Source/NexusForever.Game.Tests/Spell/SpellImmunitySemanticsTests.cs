using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Tests.Spell;

public class SpellImmunitySemanticsTests
{
    [Fact]
    public void AddSpellImmunity_WithConcreteSpellMode_StoresMatchingImmunity()
    {
        var target = new TestUnitEntity();

        target.AddSpellImmunity(1u, 48019u, 7u, 48020u, 0u);

        Assert.True(target.IsImmuneToSpell(48020u));
    }

    [Theory]
    [InlineData(1u, 1316u)]
    [InlineData(2u, 7u)]
    public void AddSpellImmunity_WithUnsupportedMode_DoesNotStoreMatchingImmunity(uint mode, uint payload)
    {
        var target = new TestUnitEntity();

        target.AddSpellImmunity(1u, 48019u, 7u, payload, mode);

        Assert.False(target.IsImmuneToSpell(payload));
    }

    private sealed class TestUnitEntity : UnitEntity
    {
        public override EntityType Type => EntityType.Simple;

        public TestUnitEntity()
            : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new WorldUnitEntityModel();
        }

        protected override float CalculateDefaultProperty(Property property)
        {
            return 0f;
        }
    }
}
