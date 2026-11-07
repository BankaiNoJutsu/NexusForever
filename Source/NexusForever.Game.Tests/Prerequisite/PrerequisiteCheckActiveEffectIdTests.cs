using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_ACTIVE_EFFECT_ID_20261107.md): type 102 (evaluator case 0x66 -> FUN_1404a4fe0) walks the unit's list at unit+0x15d0 and returns whether a node's
// virtual +0x30 equals ObjectId (a Spell4Effects id: 226 of 290 distinct retail ObjectIds are Spell4Effects ids, 34 are Spell4 ids, none is a SpellCoolDown id). A null unit is false for every comparison; Equal returns
// the match, NotEqual its negation, other comparisons false. Value is not read. Retail: 353 slots, NotEqual 249, Equal 104; Value is 0 in 254 of them.
// The enum name SpellCooldownNodeOnUnit is an older guess and the previous implementation looked up a cooldown node by Value.
public class PrerequisiteCheckActiveEffectIdTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 45049u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 45050u, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 45049u, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 45050u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 45049u, 5856u, true)]       // Value is not read
    [InlineData(PrerequisiteComparison.GreaterThan, 45049u, 0u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 45050u, 0u, false)]
    public void ComparesWhetherTheUnitHasTheActiveSpellEffect(PrerequisiteComparison comparison, uint objectId, uint value, bool expected)
    {
        IUnitEntity unit = Unit(activeEffectId: 45049u);

        Assert.Equal(expected, new PrerequisiteCheckSpellCooldownNodeOnUnit().MeetsUnit(unit, comparison, value, objectId, new PrerequisiteParameters()));
    }

    [Fact]
    public void ThePlayerEntryPointEvaluatesTheTargetWhenOneIsSupplied()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodHandler(nameof(IUnitEntity.HasActiveSpellEffectId), _ => false);
        IUnitEntity target = Unit(activeEffectId: 45049u);

        var check = new PrerequisiteCheckSpellCooldownNodeOnUnit();
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 45049u, new PrerequisiteParameters { Target = target }));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 45049u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ANullUnitFailsEvenForNotEqual()
    {
        Assert.False(new PrerequisiteCheckSpellCooldownNodeOnUnit().MeetsUnit(null, PrerequisiteComparison.NotEqual, 0u, 45049u, new PrerequisiteParameters()));
    }

    private static IUnitEntity Unit(uint activeEffectId)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IUnitEntity.HasActiveSpellEffectId), args => (uint)args[0] == activeEffectId);
        return unit;
    }
}
