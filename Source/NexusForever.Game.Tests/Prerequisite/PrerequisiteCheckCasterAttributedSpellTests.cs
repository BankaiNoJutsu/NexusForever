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

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_CASTER_ATTRIBUTED_SPELLS_20261107.md): types 50, 60 and 130 walk the first unit's active spell list (unit+0x15c8) and only consider spells whose
// source unit (spell object +0x154) is the second (prerequisite target) unit. 60 (FUN_14046a110) sums the node counts of spells with id ObjectId and compares the sum with Value; 50 (FUN_1404699f0)
// asks whether a spell with id Value exists; 130 (FUN_140469a70) asks the spell group manager whether such a spell is in group ObjectId. Without a second unit: Equal is false, NotEqual true
// (60: other comparisons false), and a missing first unit is false. The server previously ignored who cast a tracked spell.
[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class PrerequisiteCheckCasterAttributedSpellTests
{
    [Fact]
    public void StackCountsOnlyCountCastingsRegisteredToTheCaster()
    {
        var unit = new TestUnit();
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 1u, 100u, 1u, 10u);
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 2u, 100u, 2u, 11u);
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 3u, 100u, 3u, 12u);
        unit.RegisterSpellCaster(10u, 500u);
        unit.RegisterSpellCaster(11u, 500u);
        unit.RegisterSpellCaster(12u, 600u);

        Assert.Equal(2u, unit.GetActiveSpellStackCount(100u, 500u));
        Assert.Equal(1u, unit.GetActiveSpellStackCount(100u, 600u));
        Assert.Equal(0u, unit.GetActiveSpellStackCount(100u, 700u));
        Assert.Equal(0u, unit.GetActiveSpellStackCount(200u, 500u));
        Assert.Equal(3u, unit.GetActiveSpellStackCount(100u));     // the caster-less count still counts every casting
    }

    [Fact]
    public void ACastingWithNoRegisteredCasterBelongsToNobody()
    {
        var unit = new TestUnit();
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 1u, 100u, 1u, 10u);

        Assert.Equal(0u, unit.GetActiveSpellStackCount(100u, 500u));
        Assert.Equal(0u, unit.GetActiveSpellStackCount(100u, 0u));
    }

    [Theory]
    [InlineData(375u, 500u, true)]
    [InlineData(375u, 600u, false)]
    [InlineData(503u, 500u, false)]
    public void SpellGroupsAreFoundOnlyAmongSpellsCastByTheCaster(uint groupId, uint casterGuid, bool expected)
    {
        var unit = new TestUnit();
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 1u, 100u, 1u, 10u);
        unit.RegisterSpellCaster(10u, 500u);

        Assert.Equal(expected, unit.HasActiveSpellInGroup(groupId, casterGuid, Tables()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 2u, true)]
    [InlineData(PrerequisiteComparison.Equal, 3u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 2u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 2u, false)]
    public void SpellTierOnTargetComparesTheStacksCastByTheTarget(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IUnitEntity subject = Subject(stacks: 2u, groupHit: false, out var subjectProxy);
        IUnitEntity target = Unit(guid: 500u);

        Assert.Equal(expected, new PrerequisiteCheckSpellTierOnTarget().MeetsUnit(subject, comparison, value, 100u, new PrerequisiteParameters { Target = target }));
        var call = Assert.Single(subjectProxy.GetInvocations(nameof(IUnitEntity.GetActiveSpellStackCount)));
        Assert.Equal(100u, call.Arguments[0]);
        Assert.Equal(500u, call.Arguments[1]);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, false)]
    public void SpellTierOnTargetWithoutATargetIsOnlyMetByNotEqual(PrerequisiteComparison comparison, bool expected)
    {
        IUnitEntity subject = Subject(stacks: 9u, groupHit: true, out _);

        Assert.Equal(expected, new PrerequisiteCheckSpellTierOnTarget().MeetsUnit(subject, comparison, 0u, 100u, new PrerequisiteParameters()));
    }

    [Fact]
    public void SpellTierOnTargetFailsForANullSubject()
    {
        Assert.False(new PrerequisiteCheckSpellTierOnTarget().MeetsUnit(null, PrerequisiteComparison.NotEqual, 0u, 100u, new PrerequisiteParameters { Target = Unit(500u) }));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 3u, 1u, true)]
    [InlineData(PrerequisiteComparison.Equal, 3u, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 3u, 1u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 3u, 0u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 3u, 1u, false)]
    public void UnderSpellOnTargetAsksWhetherTheSubjectHoldsSpellValueCastByTheTarget(PrerequisiteComparison comparison, uint value, uint stacks, bool expected)
    {
        IUnitEntity subject = Subject(stacks, groupHit: false, out var subjectProxy);

        Assert.Equal(expected, new PrerequisiteCheckUnderSpellOnTarget().MeetsUnit(subject, comparison, value, 0u, new PrerequisiteParameters { Target = Unit(500u) }));
        var call = Assert.Single(subjectProxy.GetInvocations(nameof(IUnitEntity.GetActiveSpellStackCount)));
        Assert.Equal(value, call.Arguments[0]);       // Value is the spell id, not ObjectId
        Assert.Equal(500u, call.Arguments[1]);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    [InlineData(PrerequisiteComparison.LessThan, false)]
    public void UnderSpellOnTargetWithoutATargetIsOnlyMetByNotEqual(PrerequisiteComparison comparison, bool expected)
    {
        IUnitEntity subject = Subject(stacks: 9u, groupHit: true, out _);

        Assert.Equal(expected, new PrerequisiteCheckUnderSpellOnTarget().MeetsUnit(subject, comparison, 3u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, true, false)]
    public void ActiveSpellEffectOnTargetAsksForAGroupSpellCastByTheTarget(PrerequisiteComparison comparison, bool groupHit, bool expected)
    {
        IUnitEntity subject = Subject(stacks: 0u, groupHit, out var subjectProxy);

        Assert.Equal(expected, new PrerequisiteCheckActiveSpellEffectOnTarget().MeetsUnit(subject, comparison, 99u, 375u, new PrerequisiteParameters { Target = Unit(500u) }));
        var call = Assert.Single(subjectProxy.GetInvocations(nameof(IUnitEntity.HasActiveSpellInGroup)));
        Assert.Equal(375u, call.Arguments[0]);        // ObjectId is the group id; Value is not read
        Assert.Equal(500u, call.Arguments[1]);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    public void ActiveSpellEffectOnTargetWithoutATargetIsOnlyMetByNotEqual(PrerequisiteComparison comparison, bool expected)
    {
        IUnitEntity subject = Subject(stacks: 9u, groupHit: true, out _);

        Assert.Equal(expected, new PrerequisiteCheckActiveSpellEffectOnTarget().MeetsUnit(subject, comparison, 0u, 375u, new PrerequisiteParameters()));
        Assert.False(new PrerequisiteCheckActiveSpellEffectOnTarget().MeetsUnit(null, comparison, 0u, 375u, new PrerequisiteParameters { Target = Unit(500u) }));
    }

    private static IUnitEntity Unit(uint guid)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Guid), guid);
        return unit;
    }

    private static IUnitEntity Subject(uint stacks, bool groupHit, out RecordingDispatchProxy<IUnitEntity> proxy)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out proxy);
        proxy.SetMethodReturn(nameof(IUnitEntity.GetActiveSpellStackCount), stacks);
        proxy.SetMethodReturn(nameof(IUnitEntity.HasActiveSpellInGroup), groupHit);
        return unit;
    }

    // Native type 93 (evaluator case 0x5d, inline) compares a dword of each active spell with ObjectId. All 5 retail slots use ObjectId 36 or 38, the BuffDispellable and DebuffDispellable spell classes,
    // and gate Medic "Antidote" follow-ups (Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md). Value is 0 on every retail slot and is not read.
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 38u, true)]
    [InlineData(PrerequisiteComparison.Equal, 36u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 38u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 36u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 38u, false)]
    public void SpellClassOnUnitMatchesTheClassOfATrackedSpell(PrerequisiteComparison comparison, uint objectId, bool expected)
    {
        var unit = new TestUnit();
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 1u, 100u, 1u, 10u);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(new Spell4Entry { Id = 100u, Spell4BaseIdBaseSpell = 7u }));
        proxy.SetProperty(nameof(IGameTableManager.Spell4Base), PlayerBindPointTests.Table(new Spell4BaseEntry { Id = 7u, SpellClass = 38u }));

        var check = new PrerequisiteCheckSpell4EffectCategoryOnUnit(tables);

        // Value is deliberately a different class id: only ObjectId may be read
        Assert.Equal(expected, check.MeetsUnit(unit, comparison, 36u, objectId, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(null, comparison, 0u, objectId, new PrerequisiteParameters()));
    }

    // Native type 103 (FUN_14046a190): an effect node with id ObjectId whose applying unit is the second unit. Retail example: Spell4 29061 "Landmine Proximity" gates its own CCStateSet row 50511
    // with (NotEqual, ObjectId 50511, Value 0) as a target-apply prerequisite. The server approximates the effect row by the spell that owns it.
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 50511u, 500u, true)]         // the caster's own copy is on the unit
    [InlineData(PrerequisiteComparison.NotEqual, 50511u, 500u, false)]
    [InlineData(PrerequisiteComparison.Equal, 50511u, 600u, false)]        // another unit applied it
    [InlineData(PrerequisiteComparison.NotEqual, 50511u, 600u, true)]
    [InlineData(PrerequisiteComparison.Equal, 99999u, 500u, false)]        // unknown effect row
    [InlineData(PrerequisiteComparison.GreaterThan, 50511u, 500u, false)]
    public void ActiveEffectOnTargetOnlyCountsEffectsAppliedByTheSecondUnit(PrerequisiteComparison comparison, uint objectId, uint secondUnitGuid, bool expected)
    {
        var unit = new TestUnit();
        unit.AddSpellModifierProperty(new SpellPropertyModifier(Property.HealingMultiplierIncoming, 1u, 1.1f, 0f, 0f), 1u, 100u, 1u, 10u);
        unit.RegisterSpellCaster(10u, 500u);

        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.Spell4Effects), PlayerBindPointTests.Table(new Spell4EffectsEntry { Id = 50511u, SpellId = 100u }));

        IUnitEntity second = RecordingDispatchProxy<IUnitEntity>.Create(out var secondProxy);
        secondProxy.SetProperty(nameof(IUnitEntity.Guid), secondUnitGuid);

        var check = new PrerequisiteCheckSpellCooldownNodeOnTarget(tables);

        // Value is set to the effect id on purpose: only ObjectId may be read
        Assert.Equal(expected, check.MeetsUnit(unit, comparison, 50511u, objectId, new PrerequisiteParameters { Target = second }));
    }

    [Fact]
    public void ActiveEffectOnTargetWithoutASecondUnitOnlyAnswersNotEqual()
    {
        var unit = new TestUnit();
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        var check = new PrerequisiteCheckSpellCooldownNodeOnTarget(tables);

        Assert.False(check.MeetsUnit(unit, PrerequisiteComparison.Equal, 0u, 50511u, new PrerequisiteParameters()));
        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.NotEqual, 0u, 50511u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.NotEqual, 0u, 50511u, new PrerequisiteParameters()));
    }

    private static IGameTableManager Tables()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(new Spell4Entry { Id = 100u, Spell4GroupListId = 7u }));
        proxy.SetProperty(nameof(IGameTableManager.Spell4GroupList), PlayerBindPointTests.Table(new Spell4GroupListEntry { Id = 7u, SpellGroupId00 = 375u }));
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
