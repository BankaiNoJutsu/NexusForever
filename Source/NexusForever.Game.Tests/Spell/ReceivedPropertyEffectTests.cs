using System.Reflection;
using NexusForever.Game.Abstract.Entity;
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

// Verified native row ID/receiving-unit ownership; Source tests of already accepted property states.
// No new admission, stacking or timer policy; no retail parity claim.
[Collection(MissingGameDataDiagnosticsCollection.Name)]
public sealed class ReceivedPropertyEffectTests
{
    [Theory]
    [InlineData(SpellEffectType.UnitPropertyModifier, false)]
    [InlineData(SpellEffectType.UnitPropertyModifier, true)]
    [InlineData(SpellEffectType.UnitPropertyConversion, false)]
    [InlineData(SpellEffectType.UnitPropertyConversion, true)]
    [InlineData(SpellEffectType.PersonalDmgHealMod, false)]
    [InlineData(SpellEffectType.PersonalDmgHealMod, true)]
    public void ReceiverDiscoversInstalledRowWithoutOwningTheCasting(SpellEffectType kind, bool untimed)
    {
        var caster = new TestUnit();
        var receiver = new TestUnit();
        var unrelated = new TestUnit(); // All have GUID zero; object identity still separates ownership.
        RuntimeSpell spell = Cast(caster);
        var info = Apply(spell, receiver, kind, untimed);
        Schedule(spell, receiver, info);

        Assert.True(receiver.HasActiveSpellEffectId(info.Entry.Id));
        Assert.False(caster.HasActiveSpellEffectId(info.Entry.Id));
        Assert.False(unrelated.HasActiveSpellEffectId(info.Entry.Id));
        Assert.False(receiver.HasActiveSpellEffectId(info.EffectId));
        Assert.False(receiver.HasActiveSpellEffectId(info.Entry.SpellId));
        Assert.Equal(!untimed, Events(spell).HasPendingEvent);
        Assert.True(receiver.RemoveSpellPropertyInstance(Assert.Single(receiver.CaptureSpellPropertyInstances(88057))));
        Assert.False(receiver.HasActiveSpellEffectId(info.Entry.Id));
    }

    [Theory]
    [InlineData(SpellEffectType.UnitPropertyModifier)]
    [InlineData(SpellEffectType.UnitPropertyConversion)]
    [InlineData(SpellEffectType.PersonalDmgHealMod)]
    public void ExplicitRemovalCannotBeResurrectedByTheOldTimer(SpellEffectType kind)
    {
        var receiver = new TestUnit();
        RuntimeSpell spell = Cast(receiver);
        var info = Apply(spell, receiver, kind, false);
        Schedule(spell, receiver, info);
        ((List<ISpell>)typeof(UnitEntity).GetField("pendingSpells", Fields)!.GetValue(receiver)!).Add(spell);

        Assert.True(receiver.HasActiveSpellEffectId(info.Entry.Id));
        Assert.Single(receiver.RemoveTrackedSpellStates(id => id == 88057, uint.MaxValue));
        Assert.True(Events(spell).HasPendingEvent);
        Assert.False(receiver.HasActiveSpellEffectId(info.Entry.Id));
        Events(spell).Update(2);
        Assert.False(receiver.HasActiveSpellEffectId(info.Entry.Id));
    }

    [Fact]
    public void FiniteExpiryRetiresTheReceivedPropertyIdentity()
    {
        var receiver = new TestUnit();
        RuntimeSpell spell = Cast(new TestUnit());
        var info = Apply(spell, receiver, SpellEffectType.UnitPropertyModifier, false);
        Schedule(spell, receiver, info);

        Assert.True(receiver.HasActiveSpellEffectId(info.Entry.Id));
        Events(spell).Update(2);
        Assert.True(info.LifetimeEnded);
        Assert.False(receiver.HasActiveSpellEffectId(info.Entry.Id));
    }

    [Fact]
    public void UntimedPersonalInstancesRetireByExactOwnerWithoutErasingAnotherCaster()
    {
        var a = new TestUnit();
        var b = new TestUnit();
        var receiver = new TestUnit();
        var first = Apply(Cast(a), receiver, SpellEffectType.PersonalDmgHealMod, true, 41);
        var other = Apply(Cast(b), receiver, SpellEffectType.PersonalDmgHealMod, true, 42);
        var replacement = Apply(Cast(a), receiver, SpellEffectType.PersonalDmgHealMod, true, 41);

        Assert.True(receiver.HasActiveSpellEffectId(first.Entry.Id));
        Assert.False(receiver.RemoveSpellPropertyInstance(first.PersonalHealingInstance));
        Assert.True(receiver.RemoveSpellPropertyInstance(replacement.PersonalHealingInstance));
        Assert.True(receiver.HasActiveSpellEffectId(first.Entry.Id));
        Assert.True(receiver.RemoveSpellPropertyInstance(other.PersonalHealingInstance));
        Assert.False(receiver.HasActiveSpellEffectId(first.Entry.Id));
    }

    [Theory]
    [InlineData(SpellEffectType.UnitPropertyConversion)]
    [InlineData(SpellEffectType.PersonalDmgHealMod)]
    public void RejectedProducerShapeDoesNotInstallIdentity(SpellEffectType kind)
    {
        var receiver = new TestUnit();
        RuntimeSpell spell = Cast(new TestUnit());
        var info = Entry(kind, true, 41);
        if (kind == SpellEffectType.UnitPropertyConversion)
            info.Entry.DataBits01 = info.Entry.DataBits00; // Existing self-conversion rejection.
        else
            info.Entry.DataBits01 = 2; // Existing unsupported personal-healing mode.
        info = new SpellTargetInfo.SpellTargetEffectInfo(info.EffectId, info.Entry); // Rebuild the cached interpretation after changing the fixture.
        Dispatch(spell, receiver, info);

        Assert.Empty(receiver.CaptureSpellPropertyInstances(88057));
        Assert.False(receiver.HasActiveSpellEffectId(info.Entry.Id));
        Assert.False(receiver.HasActiveSpellEffectId(0));
    }

    private const BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;
    private static SpellTargetInfo.SpellTargetEffectInfo Apply(RuntimeSpell spell, TestUnit receiver, SpellEffectType kind, bool untimed, uint instanceId = 41)
    {
        var info = Entry(kind, untimed, instanceId);
        var target = new SpellTargetInfo(SpellEffectTargetFlags.Target, receiver);
        target.Effects.Add(info);
        ((List<ISpellTargetInfo>)typeof(RuntimeSpell).GetField("targets", Fields)!.GetValue(spell)!).Add(target);
        Dispatch(spell, receiver, info);
        Assert.Single(receiver.CaptureSpellPropertyInstances(88057).Where(i => i.EffectId == instanceId));
        return info;
    }

    // Synthetic ordinary/conversion rows plus the existing Fusion Probe personal shape.
    private static SpellTargetInfo.SpellTargetEffectInfo Entry(SpellEffectType kind, bool untimed, uint instanceId)
    {
        var row = new Spell4EffectsEntry { Id = 231375, SpellId = 88057, EffectType = kind, Flags = untimed ? 2u : 0u, DurationTime = untimed ? 0u : 1000u };
        row.DataBits00 = kind == SpellEffectType.PersonalDmgHealMod ? 12u : (uint)Property.BaseHealth;
        row.DataBits01 = kind == SpellEffectType.UnitPropertyConversion ? (uint)Property.Armor : 1u;
        row.DataBits02 = BitConverter.SingleToUInt32Bits(1.2f);
        return new(instanceId, row);
    }

    private static void Dispatch(RuntimeSpell spell, TestUnit target, ISpellTargetEffectInfo info)
    {
        switch (info.Entry.EffectType)
        {
            case SpellEffectType.UnitPropertyModifier: SpellHandler.HandleEffectPropertyModifier(spell, target, info); break;
            case SpellEffectType.UnitPropertyConversion: SpellHandler.HandleEffectUnitPropertyConversion(spell, target, info); break;
            default: SpellHandler.HandleEffectPersonalDmgHealMod(spell, target, info); break;
        }
    }

    private static RuntimeSpell Cast(TestUnit caster)
    {
        ISpellInfo info = RecordingDispatchProxy<ISpellInfo>.Create(out var p);
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var b);
        b.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 33 });
        p.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 88057 });
        p.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        IGlobalSpellManager manager = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var m);
        m.SetProperty(nameof(IGlobalSpellManager.NextCastingId), 7u);
        IScriptManager scripts = RecordingDispatchProxy<IScriptManager>.Create(out var s);
        s.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), RecordingDispatchProxy<IScriptCollection>.Create(out _));
        return new RuntimeSpell(caster, new SpellParameters { SpellInfo = info }, globalSpellManager: manager, scriptManager: scripts);
    }

    private static void Schedule(RuntimeSpell spell, IUnitEntity target, ISpellTargetEffectInfo info) =>
        typeof(RuntimeSpell).GetMethod("ScheduleEffectLifetime", Fields, [typeof(SpellEffectInterpretation), typeof(IUnitEntity), typeof(ISpellTargetEffectInfo)])!
            .Invoke(spell, [SpellEffectInterpreter.Interpret(info.Entry), target, info]);
    private static ISpellEventManager Events(RuntimeSpell spell) => (ISpellEventManager)typeof(RuntimeSpell).GetField("events", Fields)!.GetValue(spell)!;

    private sealed class TestUnit : UnitEntity
    {
        public TestUnit() : base(RecordingDispatchProxy<IMovementManager>.Create(out _)) { }
        public override EntityType Type => EntityType.Simple;
        protected override IEntityModel BuildEntityModel() => new WorldUnitEntityModel();
        protected override float CalculateDefaultProperty(Property property) => 1f;
    }
}
