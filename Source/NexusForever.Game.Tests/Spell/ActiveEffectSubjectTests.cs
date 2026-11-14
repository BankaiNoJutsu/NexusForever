using System.Reflection;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Spell.Event;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using RuntimeSpell = NexusForever.Game.Spell.Spell;

namespace NexusForever.Game.Tests.Spell;

// Verified native target ownership; Source checks of the existing finite-lifetime model.
// Received-effect discovery and untimed registration remain separate gates. No retail parity claim.
public sealed class ActiveEffectSubjectTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ASpellOnlyReportsTheQueriedTargetsOwnEffect(bool selfFirst, bool queriedRowOnSelf)
    {
        var caster = new TestUnit(1);
        var other = new TestUnit(2);
        RuntimeSpell spell = Create(caster);
        if (selfFirst)
        {
            Apply(spell, caster, 41, queriedRowOnSelf ? 65808u : 44081u);
            Apply(spell, other, 42, queriedRowOnSelf ? 44081u : 65808u);
        }
        else
        {
            Apply(spell, other, 42, queriedRowOnSelf ? 44081u : 65808u);
            Apply(spell, caster, 41, queriedRowOnSelf ? 65808u : 44081u);
        }
        Pending(caster).Add(spell);

        Assert.True(caster.HasActiveSpellEffectId(queriedRowOnSelf ? 65808u : 44081u));
        Assert.False(caster.HasActiveSpellEffectId(queriedRowOnSelf ? 44081u : 65808u));
        Assert.False(caster.HasActiveSpellEffectId(41));
        Assert.False(caster.HasActiveSpellEffectId(33730));
        Events(spell).Update(2);
        Assert.False(caster.HasActiveSpellEffectId(65808));
        Assert.False(caster.HasActiveSpellEffectId(44081));
    }

    [Fact]
    public void ANewObjectWithTheSameGuidDoesNotOwnThePreviousTargetsRecord()
    {
        var caster = new TestUnit(1);
        var previousTarget = new TestUnit(1);
        RuntimeSpell spell = Create(caster);
        Apply(spell, previousTarget, 41, 65808);
        Pending(caster).Add(spell);

        Assert.False(caster.HasActiveSpellEffectId(65808));
    }

    private static void Apply(RuntimeSpell spell, TestUnit target, uint instanceId, uint rowId)
    {
        // Synthetic removable-state fixture; it does not qualify native SetBusy operands.
        var info = new SpellTargetInfo.SpellTargetEffectInfo(instanceId, new Spell4EffectsEntry
        {
            Id = rowId, SpellId = 33730, EffectType = SpellEffectType.SetBusy,
            DataBits00 = 1, DurationTime = 1000
        });
        var targetInfo = new SpellTargetInfo(SpellEffectTargetFlags.Target, target);
        targetInfo.Effects.Add(info);
        Targets(spell).Add(targetInfo);
        SpellHandler.HandleEffectSetBusy(spell, target, info);
        typeof(RuntimeSpell).GetMethod("ScheduleEffectLifetime", BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(SpellEffectInterpretation), typeof(Game.Abstract.Entity.IUnitEntity), typeof(ISpellTargetEffectInfo)])!
            .Invoke(spell, [info.Interpretation, target, info]);
    }

    private static RuntimeSpell Create(TestUnit caster)
    {
        ISpellInfo info = RecordingDispatchProxy<ISpellInfo>.Create(out var p);
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var b);
        b.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 33 });
        p.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 33730 });
        p.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        IGlobalSpellManager manager = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var m);
        m.SetProperty(nameof(IGlobalSpellManager.NextCastingId), caster.Guid);
        IScriptManager scripts = RecordingDispatchProxy<IScriptManager>.Create(out var s);
        IScriptCollection collection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
        s.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), collection);
        return new RuntimeSpell(caster, new SpellParameters { SpellInfo = info }, globalSpellManager: manager, scriptManager: scripts);
    }

    private static List<ISpellTargetInfo> Targets(RuntimeSpell spell) => (List<ISpellTargetInfo>)typeof(RuntimeSpell)
        .GetField("targets", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(spell)!;
    private static ISpellEventManager Events(RuntimeSpell spell) => (ISpellEventManager)typeof(RuntimeSpell)
        .GetField("events", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(spell)!;
    private static List<ISpell> Pending(TestUnit unit) => (List<ISpell>)typeof(UnitEntity)
        .GetField("pendingSpells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(unit)!;

    private sealed class TestUnit : UnitEntity
    {
        public TestUnit(uint guid) : base(RecordingDispatchProxy<IMovementManager>.Create(out _)) => Guid = guid;
        public override EntityType Type => EntityType.Simple;
        protected override IEntityModel BuildEntityModel() => new WorldUnitEntityModel();
        protected override float CalculateDefaultProperty(Property property) => 1f;
    }
}
