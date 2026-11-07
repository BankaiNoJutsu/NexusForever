using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_SPELL_STACK_COUNT_20261107.md): type 59 (evaluator case 0x3b, FUN_1404a4f60) returns false for a null unit, otherwise sums the dword at node+0x50 over the
// nodes of the unit's spell list (unit+0x15c8) whose spell id (node+0x38 object, virtual +8) equals ObjectId, and compares that sum with Value through the integer helper FUN_1404a2090. Type 280 (case
// 0x118) requires the same to hold for both units. Retail: 253 of the 260 distinct ObjectIds are Spell4 ids, Value runs 0..60 (tiers stop at 9) and equals the Spell4StackGroup cap for several spells.
// The previous implementation compared the player's ability-book tier of the spell's base spell with Value.
[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class PrerequisiteCheckSpellStackCountTests
{
    [Fact]
    public void EachCastingOfASpellIsOneStack()
    {
        var unit = new TestUnit();
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 1u, 100u, 1u, 10u);
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 2u, 100u, 2u, 11u);   // a different effect entry: re-adding the same spell and entry replaces the state
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierOutgoing, 1u, 1.1f, 0f, 0f), 3u, 100u, 2u, 11u);   // second effect of the same casting
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierOutgoing, 1u, 1.1f, 0f, 0f), 4u, 200u, 1u, 12u);

        Assert.Equal(2u, unit.GetActiveSpellStackCount(100u));
        Assert.Equal(1u, unit.GetActiveSpellStackCount(200u));
        Assert.Equal(0u, unit.GetActiveSpellStackCount(300u));
        Assert.Equal(0u, unit.GetActiveSpellStackCount(0u));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3u, true)]
    [InlineData(PrerequisiteComparison.Equal, 2u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 3u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 3u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 4u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 3u, false)]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    [InlineData(PrerequisiteComparison.Equal, 40u, false)]      // retail Values run to 60; tiers stop at 9
    public void ComparesTheStackCountWithValue(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IUnitEntity unit = Unit(3u);

        Assert.Equal(expected, new PrerequisiteCheckSpellTier().MeetsUnit(unit, comparison, value, 100u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AnAbsentSpellHasZeroStacks()
    {
        IUnitEntity unit = Unit(0u);

        Assert.True(new PrerequisiteCheckSpellTier().MeetsUnit(unit, PrerequisiteComparison.Equal, 0u, 100u, new PrerequisiteParameters()));
        Assert.False(new PrerequisiteCheckSpellTier().MeetsUnit(unit, PrerequisiteComparison.GreaterThanOrEqual, 1u, 100u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ANullUnitFailsEveryComparison()
    {
        var check = new PrerequisiteCheckSpellTier();

        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.Equal, 0u, 100u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.NotEqual, 5u, 100u, new PrerequisiteParameters()));
    }

    [Fact]
    public void PlayerChecksUseTheTargetWhenOneIsSupplied()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodReturn(nameof(IUnitEntity.GetActiveSpellStackCount), 1u);
        IUnitEntity target = Unit(5u);

        var check = new PrerequisiteCheckSpellTier();
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 100u, new PrerequisiteParameters()));
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 5u, 100u, new PrerequisiteParameters { Target = target }));
    }

    [Theory]
    [InlineData(2u, 2u, true)]
    [InlineData(2u, 1u, false)]
    [InlineData(1u, 2u, false)]
    public void CasterAndTargetBothMeetTheStackCount(uint casterStacks, uint targetStacks, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodReturn(nameof(IUnitEntity.GetActiveSpellStackCount), casterStacks);
        IUnitEntity target = Unit(targetStacks);

        var check = new PrerequisiteCheckSpellTierOnCasterAndTarget();
        Assert.Equal(expected, check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 2u, 100u, new PrerequisiteParameters { Target = target }));
    }

    [Fact]
    public void CasterAndTargetFailsWithoutATarget()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodReturn(nameof(IUnitEntity.GetActiveSpellStackCount), 9u);

        Assert.False(new PrerequisiteCheckSpellTierOnCasterAndTarget().Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 1u, 100u, new PrerequisiteParameters()));
    }

    private static IUnitEntity Unit(uint stacks)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetMethodReturn(nameof(IUnitEntity.GetActiveSpellStackCount), stacks);
        return unit;
    }

    private sealed class TestUnit : UnitEntity
    {
        public TestUnit() : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
        }

        public override EntityType Type => EntityType.Simple;
        protected override IEntityModel BuildEntityModel() => new WorldUnitEntityModel();
        protected override float CalculateDefaultProperty(Property property) => 1f;
    }
}
