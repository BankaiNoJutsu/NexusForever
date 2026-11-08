using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Spell.Event;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Template.Collection;
using RuntimeSpell = NexusForever.Game.Spell.Spell;

namespace NexusForever.Game.Tests.Spell;

// ChangePlane (0x85) has no client handler. The client's only plane state is the second dword of packet 0x0172 (player+0x6758, the first dword is stored but never read), which prerequisite 232
// tests as (word & Value) != 0. The 16 shipped rows have DataBits00 == DataBits01 and no other data words; 14 add bit 2 (value 4) for 3.5 to 8 seconds, which matches the retail prerequisites
// that test plane value 4 (Decomp/Analysis/SPELL_EFFECT_CHANGEPLANE_20261108.md). The inferred rule is: add the bits to the word, undo the row when its duration expires.
[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class ChangePlaneSpellEffectTests
{
    private const uint Source = 37218u;

    [Fact]
    public void NoChangesGiveAnEmptyPlaneMask()
    {
        PlaneChangeRules.Fold([], out uint canSee, out uint canBeSeen);

        Assert.Equal(0u, canSee);
        Assert.Equal(0u, canBeSeen);
    }

    [Theory]
    [InlineData(4u, 4u, 4u, 4u)]
    [InlineData(5u, 5u, 5u, 5u)]
    [InlineData(5u, 1u, 5u, 1u)] // the two words are independent
    public void AChangeAddsItsBitsToEachWord(uint addCanSee, uint addCanBeSeen, uint expectedCanSee, uint expectedCanBeSeen)
    {
        PlaneChangeRules.Fold([new PlaneChange(1u, Source, 9u, addCanSee, addCanBeSeen)], out uint canSee, out uint canBeSeen);

        Assert.Equal(expectedCanSee, canSee);
        Assert.Equal(expectedCanBeSeen, canBeSeen);
    }

    [Fact]
    public void ChangesFromDifferentEffectsCombineAndEachCanBeRemovedAlone()
    {
        PlaneChange void4 = new(1u, Source, 9u, 4u, 4u);
        PlaneChange plane1 = new(2u, 79237u, 10u, 1u, 1u);

        PlaneChangeRules.Fold([void4, plane1], out uint both, out _);
        PlaneChangeRules.Fold([plane1], out uint withoutVoid, out _);

        Assert.Equal(5u, both);
        Assert.Equal(1u, withoutVoid);
    }

    [Fact]
    public void TheHandlerRecordsTheTwoWordsOnAPlayerTarget()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);

        SpellHandler.HandleEffectChangePlane(Cast(), player, Effect(effectId: 77u, bits: (4u, 2u)));

        PlaneChange change = (PlaneChange)Assert.Single(proxy.GetInvocations(nameof(IPlayer.AddPlaneChange))).Arguments[0];
        Assert.Equal(new PlaneChange(77u, Source, 9u, 4u, 2u), change);
    }

    [Fact]
    public void TheHandlerIgnoresATargetThatIsNotAPlayer()
    {
        IUnitEntity creature = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);

        SpellHandler.HandleEffectChangePlane(Cast(), creature, Effect(effectId: 77u, bits: (4u, 4u)));

        Assert.Empty(proxy.GetInvocations(nameof(IPlayer.AddPlaneChange)));
    }

    [Fact]
    public void ARowWithADurationIsUndoneWhenItExpires()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Guid), 42u);
        proxy.SetMethodReturn(nameof(IPlayer.RemovePlaneChange), true);
        var info = Effect(effectId: 77u, bits: (4u, 4u), duration: 5600u);
        var effect = SpellEffectInterpreter.Interpret(info.Entry);

        Assert.NotNull(effect.ChangePlane);
        Assert.True(RuntimeSpell.TryGetEffectLifetimeDelay(effect, out uint delay));
        Assert.Equal(5600u, delay);

        var spell = LifetimeSpell(player, info);
        Invoke(spell, "ScheduleEffectLifetime", effect, player, info);
        ((SpellEventManager)Field(spell, "events")).Update(6d);

        Assert.True(info.LifetimeEnded);
        Assert.Equal(77u, Assert.Single(proxy.GetInvocations(nameof(IPlayer.RemovePlaneChange))).Arguments[0]);
    }

    [Fact]
    public void ARowWithoutADurationHasNoLifetime()
    {
        var effect = SpellEffectInterpreter.Interpret(Effect(effectId: 77u, bits: (5u, 5u)).Entry);

        Assert.False(RuntimeSpell.TryGetEffectLifetimeDelay(effect, out _));
    }

    private static SpellTargetInfo.SpellTargetEffectInfo Effect(uint effectId, (uint CanSee, uint CanBeSeen) bits, uint duration = 0u)
        => new(effectId, new Spell4EffectsEntry
        {
            Id           = 86740u,
            SpellId      = Source,
            EffectType   = SpellEffectType.ChangePlane,
            DataBits00   = bits.CanSee,
            DataBits01   = bits.CanBeSeen,
            DurationTime = duration
        });

    private static ISpell Cast()
    {
        var spell = RecordingDispatchProxy<ISpell>.Create(out var sp);
        var info = RecordingDispatchProxy<ISpellInfo>.Create(out var ip);
        ip.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = Source });
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var bp);
        bp.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 9001 });
        ip.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        sp.SetProperty(nameof(ISpell.Parameters), new SpellParameters { SpellInfo = info });
        sp.SetProperty(nameof(ISpell.CastingId), 9u);
        return spell;
    }

    private static RuntimeSpell LifetimeSpell(IPlayer player, ISpellTargetEffectInfo info)
    {
        var spell = (RuntimeSpell)RuntimeHelpers.GetUninitializedObject(typeof(RuntimeSpell));
        Set(spell, "<Caster>k__BackingField", player);
        Set(spell, "<Parameters>k__BackingField", Cast().Parameters);
        Initialise(spell, "lifetimeEvents");
        Set(spell, "events", new SpellEventManager());
        var target = new SpellTargetInfo(SpellEffectTargetFlags.Caster, player);
        target.Effects.Add(info);
        Set(spell, "targets", new List<ISpellTargetInfo> { target });
        Set(spell, "status", SpellStatus.Executing);
        Set(spell, "scriptCollection", RecordingDispatchProxy<IScriptCollection>.Create(out _));
        return spell;
    }

    private static object Field(object o, string name) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(o)!;
    private static void Set(object o, string name, object value) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(o, value);
    private static void Initialise(object o, string name) => Set(o, name, Activator.CreateInstance(o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.FieldType)!);
    private static void Invoke(RuntimeSpell spell, string name, SpellEffectInterpretation effect, IUnitEntity target, ISpellTargetEffectInfo info) =>
        typeof(RuntimeSpell).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, [typeof(SpellEffectInterpretation), typeof(IUnitEntity), typeof(ISpellTargetEffectInfo)])!.Invoke(spell, [effect, target, info]);
}
