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

// ChangePhase (0x22) has no client handler, so its meaning is inferred from the 161 Spell4Effects rows (Decomp/Analysis/SPELL_EFFECT_CHANGEPHASE_20261108.md): DataBits00/01 are phase bits
// added to PhasesIPerceive / PhasesThatPerceiveMe (the two dwords of packet 0x0171), DataBits02/03 are bits cleared from the same two words (explicit pairs such as 3/12, 5/10 and 9/6 are
// complementary within four bits, and "16 with 0xFFFFFFEF" is the exclusive-phase form), applied in the order add then clear, and a row with a duration is undone when it expires.
[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class ChangePhaseSpellEffectTests
{
    private const uint Source = 4242u;

    [Theory]
    // add only: OR into the default mask of one
    [InlineData(2u, 2u, 0u, 0u, 3u, 3u)]
    [InlineData(1u, 1u, 0u, 0u, 1u, 1u)]
    [InlineData(5u, 1u, 0u, 0u, 5u, 1u)] // the two words are independent
    [InlineData(0x8001u, 0u, 0u, 0u, 0x8001u, 1u)]
    // add and clear
    [InlineData(3u, 3u, 12u, 12u, 3u, 3u)] // 3 / 12 are complementary within four bits
    [InlineData(9u, 9u, 6u, 6u, 9u, 9u)]
    [InlineData(16u, 16u, 0xFFFFFFEFu, 0xFFFFFFEFu, 16u, 16u)] // exclusive phase: only bit 4 remains
    [InlineData(5u, 5u, 0xFFFFFFFAu, 0xFFFFFFFAu, 5u, 5u)]
    [InlineData(4u, 13u, 0xFFFAu, 0xFFF2u, 5u, 13u)] // 1|4 keeps bits 0 and 2; 1|13 keeps bits 0, 2 and 3
    // clear only
    [InlineData(0u, 0u, 0xFFFFF7FFu, 0xFFFFF7FFu, 0u, 0u)] // default mask 1 has no bit 11 so everything is cleared
    [InlineData(0u, 0u, 1u, 0u, 0u, 1u)] // clearing bit 0 of the perceive word only
    public void AChangeAddsThenClearsPhaseBitsInEachWord(uint addPerceive, uint addPerceiveMe, uint clearPerceive, uint clearPerceiveMe, uint expectedPerceive, uint expectedPerceiveMe)
    {
        PhaseChangeRules.Fold([Change(1u, addPerceive, addPerceiveMe, clearPerceive, clearPerceiveMe)], out uint perceive, out uint perceiveMe);

        Assert.Equal(expectedPerceive, perceive);
        Assert.Equal(expectedPerceiveMe, perceiveMe);
    }

    [Fact]
    public void NoChangesGiveTheLoginDefaultOfOne()
    {
        PhaseChangeRules.Fold([], out uint perceive, out uint perceiveMe);

        Assert.Equal(1u, perceive);
        Assert.Equal(1u, perceiveMe);
    }

    [Fact]
    public void ChangesApplyInInsertionOrder()
    {
        // an exclusive move to bit 4 followed by an add of bit 1 keeps both; the reverse order loses bit 1
        PhaseChange exclusive = Change(1u, 16u, 16u, 0xFFFFFFEFu, 0xFFFFFFEFu);
        PhaseChange add = Change(2u, 2u, 2u, 0u, 0u);

        PhaseChangeRules.Fold([exclusive, add], out uint forward, out _);
        PhaseChangeRules.Fold([add, exclusive], out uint reverse, out _);

        Assert.Equal(18u, forward);
        Assert.Equal(16u, reverse);
    }

    [Fact]
    public void TheHandlerRecordsTheFourWordsOnAPlayerTarget()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        var info = Effect(effectId: 77u, dataBits: (2u, 3u, 0xFFFFFFEFu, 8u));

        SpellHandler.HandleEffectChangePhase(Cast(), player, info);

        PhaseChange change = (PhaseChange)Assert.Single(proxy.GetInvocations(nameof(IPlayer.AddPhaseChange))).Arguments[0];
        Assert.Equal(new PhaseChange(77u, Source, 9u, 2u, 3u, 0xFFFFFFEFu, 8u), change);
    }

    [Fact]
    public void TheHandlerIgnoresATargetThatIsNotAPlayer()
    {
        IUnitEntity creature = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);

        SpellHandler.HandleEffectChangePhase(Cast(), creature, Effect(effectId: 77u, dataBits: (2u, 2u, 0u, 0u)));

        Assert.Empty(proxy.GetInvocations(nameof(IPlayer.AddPhaseChange)));
    }

    [Fact]
    public void ARowWithADurationIsUndoneWhenItExpires()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Guid), 42u);
        proxy.SetMethodReturn(nameof(IPlayer.RemovePhaseChange), true);
        var info = Effect(effectId: 77u, dataBits: (2u, 2u, 0u, 0u), duration: 15000u);
        var effect = SpellEffectInterpreter.Interpret(info.Entry);

        Assert.NotNull(effect.ChangePhase);
        Assert.True(RuntimeSpell.TryGetEffectLifetimeDelay(effect, out uint delay));
        Assert.Equal(15000u, delay);

        var spell = LifetimeSpell(player, info);
        Invoke(spell, "ScheduleEffectLifetime", effect, player, info);
        ((SpellEventManager)Field(spell, "events")).Update(16d);

        Assert.True(info.LifetimeEnded);
        Assert.Equal(77u, Assert.Single(proxy.GetInvocations(nameof(IPlayer.RemovePhaseChange))).Arguments[0]);
    }

    [Fact]
    public void ARowWithoutADurationHasNoLifetime()
    {
        var effect = SpellEffectInterpreter.Interpret(Effect(effectId: 77u, dataBits: (2u, 2u, 0u, 0u)).Entry);

        Assert.False(RuntimeSpell.TryGetEffectLifetimeDelay(effect, out _));
    }

    private static PhaseChange Change(uint effectId, uint addPerceive, uint addPerceiveMe, uint clearPerceive, uint clearPerceiveMe)
        => new(effectId, Source, 9u, addPerceive, addPerceiveMe, clearPerceive, clearPerceiveMe);

    private static SpellTargetInfo.SpellTargetEffectInfo Effect(uint effectId, (uint Add0, uint Add1, uint Clear0, uint Clear1) dataBits, uint duration = 0u)
        => new(effectId, new Spell4EffectsEntry
        {
            Id         = 5924u,
            SpellId    = Source,
            EffectType = SpellEffectType.ChangePhase,
            DataBits00 = dataBits.Add0,
            DataBits01 = dataBits.Add1,
            DataBits02 = dataBits.Clear0,
            DataBits03 = dataBits.Clear1,
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
