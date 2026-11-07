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
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_ACTIVE_SPELL_GROUP_20261107.md): type 129 (evaluator case 0x81) walks the unit's spell list at unit+0x15c8 and asks the spell group manager
// (DAT_140c65b70 virtual +0x38) whether each spell id belongs to group ObjectId; a null unit is false, Equal returns "some spell is in the group", NotEqual its negation, other comparisons false. Value is
// not read. All 146 retail ObjectIds are at most 784 (the largest SpellGroupId in Spell4GroupList; the list ids run to 1579), so ObjectId is a spell group id (one of a list's SpellGroupId00..31 values).
// The previous implementation read Value as a Spell4 id and fell back to ObjectId as a Spell4 id.
[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class PrerequisiteCheckActiveSpellGroupTests
{
    [Theory]
    [InlineData(375u, true)]       // first group of list 7
    [InlineData(480u, true)]       // another group of list 7
    [InlineData(503u, false)]      // a group only list 8 (spell 101, not active) has
    [InlineData(999u, false)]      // a group in no list
    [InlineData(0u, false)]
    public void AUnitHasASpellInAGroupWhenAnActiveSpellsListContainsIt(uint groupId, bool expected)
    {
        var unit = new TestUnit();
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 1u, 100u, 1u, 1u);

        Assert.Equal(expected, unit.HasActiveSpellInGroup(groupId, Tables()));
    }

    [Fact]
    public void ASpellWithoutAGroupListOrAnUnknownSpellIsInNoGroup()
    {
        var unit = new TestUnit();
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 1u, 102u, 1u, 1u);   // Spell4GroupListId 0
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierOutgoing, 1u, 1.1f, 0f, 0f), 2u, 999u, 2u, 2u);   // not a Spell4 row

        Assert.False(unit.HasActiveSpellInGroup(375u, Tables()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 375u, true)]
    [InlineData(PrerequisiteComparison.Equal, 503u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 375u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 503u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 375u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 503u, false)]
    public void ComparesWhetherTheUnitHasAnActiveSpellInTheGroup(PrerequisiteComparison comparison, uint groupId, bool expected)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IUnitEntity.HasActiveSpellInGroup), args => (uint)args[0] == 375u);

        var check = new PrerequisiteCheckActiveSpellEffectOnUnit();
        // Value is not read: a Spell4 id there must not change the result.
        Assert.Equal(expected, check.MeetsUnit(unit, comparison, 100u, groupId, new PrerequisiteParameters()));
        Assert.Equal(expected, check.MeetsUnit(unit, comparison, 0u, groupId, new PrerequisiteParameters()));
    }

    [Fact]
    public void ANullUnitFailsEvenForNotEqual()
    {
        var check = new PrerequisiteCheckActiveSpellEffectOnUnit();

        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.NotEqual, 0u, 375u, new PrerequisiteParameters()));
    }

    private static IGameTableManager Tables()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(
            new Spell4Entry { Id = 100u, Spell4GroupListId = 7u },
            new Spell4Entry { Id = 101u, Spell4GroupListId = 8u },
            new Spell4Entry { Id = 102u, Spell4GroupListId = 0u }));
        proxy.SetProperty(nameof(IGameTableManager.Spell4GroupList), PlayerBindPointTests.Table(
            new Spell4GroupListEntry { Id = 7u, SpellGroupId00 = 375u, SpellGroupId01 = 480u },
            new Spell4GroupListEntry { Id = 8u, SpellGroupId00 = 503u }));
        return tables;
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
