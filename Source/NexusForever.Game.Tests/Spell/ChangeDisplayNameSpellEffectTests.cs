using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Spell.Event;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Script.Template.Collection;
using Handler = NexusForever.Game.Spell.SpellHandler;
using RuntimeSpell = NexusForever.Game.Spell.Spell;

namespace NexusForever.Game.Tests.Spell;

// ChangeDisplayName (0x52) has no client handler of its own. Its 268 rows carry a LocalizedText id in DataBits00 that resolves to unit names such as "Dominion Shocktrooper", "Hacked Dominion Turret"
// and "Commandeered Suppressionbot" (Decomp/Analysis/SPELL_EFFECT_CHANGEDISPLAYNAME_20261108.md), and the client does have a packet that renames a unit: 0x0939 ServerUnitNameAndTitleChange (Creature2Id lookup
// or a wide-string name) and the wide-string name change used for scanner units. The inferred rule: the target shows the resolved name; a row with a duration is undone when it expires and the unit shows its
// own name again.
[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class ChangeDisplayNameSpellEffectTests
{
    private const uint Source = 48593u;
    private const uint TextId = 455730u;

    [Fact]
    public void NoOverridesMeansNoCurrentName()
    {
        var stack = new DisplayNameStack();

        Assert.Null(stack.Current);
        Assert.Empty(stack.TemporaryStates);
    }

    [Fact]
    public void TheMostRecentlyAppliedNameWinsAndRemovalFallsBackToTheEarlierOne()
    {
        var stack = new DisplayNameStack();
        stack.Add(1u, Source, 9u, "Captured Dominion Airman", temporary: false);
        stack.Add(2u, Source, 10u, "Hacked Dominion Turret", temporary: true);

        Assert.Equal("Hacked Dominion Turret", stack.Current);
        Assert.True(stack.Remove(2u));
        Assert.Equal("Captured Dominion Airman", stack.Current);
        Assert.False(stack.Remove(2u));
    }

    [Fact]
    public void ARecastOfTheSameEffectReplacesItsName()
    {
        var stack = new DisplayNameStack();
        stack.Add(1u, Source, 9u, "Old", temporary: true);
        stack.Add(1u, Source, 10u, "New", temporary: true);

        Assert.Equal("New", stack.Current);
        Assert.Single(stack.TemporaryStates);
    }

    [Fact]
    public void OnlyTemporaryNamesAreListedForGenericSpellStateRemoval()
    {
        var stack = new DisplayNameStack();
        stack.Add(1u, Source, 9u, "Permanent", temporary: false);
        stack.Add(2u, 5u, 10u, "Timed", temporary: true);

        DisplayNameState state = Assert.Single(stack.TemporaryStates);
        Assert.Equal(2u, state.EffectId);
        Assert.Equal(5u, state.Spell4Id);
        Assert.Equal(10u, state.CastingId);
    }

    [Fact]
    public void AnOverrideIsSentAsAWideStringNameChange()
    {
        IUnitEntity unit = Unit(guid: 77u, creatureId: 123u);

        var packet = Assert.IsType<ServerUnitNameChange>(DisplayNameSync.Build(unit, "Hacked Dominion Turret"));

        Assert.Equal(77u, packet.UnitId);
        Assert.Equal("Hacked Dominion Turret", packet.Name);
    }

    [Fact]
    public void ARestoredCreatureAsksTheClientToLookUpItsCreatureName()
    {
        IUnitEntity unit = Unit(guid: 77u, creatureId: 123u);

        var packet = Assert.IsType<ServerUnitNameAndTitleChange>(DisplayNameSync.Build(unit, null));

        Assert.Equal(77u, packet.UnitId);
        Assert.Equal(123u, packet.Creature2Id);
        Assert.Equal(0u, packet.TitleId);
    }

    [Fact]
    public void ARestoredPlayerGetsItsCharacterNameBack()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Type), EntityType.Player);
        proxy.SetProperty(nameof(IPlayer.Guid), 5u);
        proxy.SetProperty(nameof(IPlayer.Name), "Zephyr");

        var packet = Assert.IsType<ServerUnitNameChange>(DisplayNameSync.Build(player, null));

        Assert.Equal(5u, packet.UnitId);
        Assert.Equal("Zephyr", packet.Name);
    }

    [Theory]
    [InlineData(0u, false)]
    [InlineData(15000u, true)]
    public void TheHandlerResolvesTheEnglishTextAndMarksTimedRowsTemporary(uint duration, bool temporary)
    {
        using var scope = UseTables(new TextTableEntry(Language.English, TextId, "Hacked Dominion Turret"));
        IUnitEntity unit = Unit(guid: 77u, creatureId: 123u, out var proxy);

        Handler.HandleEffectChangeDisplayName(Cast(), unit, Effect(effectId: 31u, textId: TextId, duration: duration));

        var call = Assert.Single(proxy.GetInvocations(nameof(IUnitEntity.AddDisplayName)));
        Assert.Equal(31u, call.Arguments[0]);
        Assert.Equal(Source, call.Arguments[1]);
        Assert.Equal(9u, call.Arguments[2]);
        Assert.Equal("Hacked Dominion Turret", call.Arguments[3]);
        Assert.Equal(temporary, call.Arguments[4]);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(999999u)] // not in the text table
    public void TheHandlerIgnoresAMissingText(uint textId)
    {
        using var scope = UseTables(new TextTableEntry(Language.English, TextId, "Hacked Dominion Turret"));
        IUnitEntity unit = Unit(guid: 77u, creatureId: 123u, out var proxy);

        Handler.HandleEffectChangeDisplayName(Cast(), unit, Effect(effectId: 31u, textId: textId, duration: 0u));

        Assert.Empty(proxy.GetInvocations(nameof(IUnitEntity.AddDisplayName)));
    }

    [Fact]
    public void ATimedRowIsUndoneWhenItExpires()
    {
        using var scope = UseTables(new TextTableEntry(Language.English, TextId, "Hacked Dominion Turret"));
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Guid), 42u);
        proxy.SetMethodReturn(nameof(IUnitEntity.RemoveDisplayName), true);
        var info = Effect(effectId: 31u, textId: TextId, duration: 15000u);
        var effect = SpellEffectInterpreter.Interpret(info.Entry);

        Assert.NotNull(effect.ChangeDisplayName);
        Assert.Equal(TextId, effect.ChangeDisplayName.TextId);
        Assert.True(RuntimeSpell.TryGetEffectLifetimeDelay(effect, out uint delay));
        Assert.Equal(15000u, delay);

        var spell = LifetimeSpell(player, info);
        Invoke(spell, "ScheduleEffectLifetime", effect, player, info);
        ((SpellEventManager)Field(spell, "events")).Update(16d);

        Assert.True(info.LifetimeEnded);
        Assert.Equal(31u, Assert.Single(proxy.GetInvocations(nameof(IUnitEntity.RemoveDisplayName))).Arguments[0]);
    }

    private static IUnitEntity Unit(uint guid, uint creatureId)
        => Unit(guid, creatureId, out _);

    private static IUnitEntity Unit(uint guid, uint creatureId, out RecordingDispatchProxy<IUnitEntity> proxy)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);
        proxy.SetProperty(nameof(IUnitEntity.Guid), guid);
        proxy.SetProperty(nameof(IUnitEntity.CreatureId), creatureId);
        return unit;
    }

    private static SpellTargetInfo.SpellTargetEffectInfo Effect(uint effectId, uint textId, uint duration)
        => new(effectId, new Spell4EffectsEntry
        {
            Id           = 87000u,
            SpellId      = Source,
            EffectType   = SpellEffectType.ChangeDisplayName,
            DataBits00   = textId,
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

    private static IDisposable UseTables(params TextTableEntry[] entries)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
        tp.SetMethodReturn(nameof(IGameTableManager.GetTextTable), new TextTable(entries));
        ISpellEffectDependencyResolver previous = Handler.InitialiseDependencyResolver(new TextResolver(tables));
        return new ResolverScope(previous);
    }

    private sealed class ResolverScope(ISpellEffectDependencyResolver previous) : IDisposable
    {
        public void Dispose() => Handler.InitialiseDependencyResolver(previous);
    }

    private sealed class TextResolver(IGameTableManager tables) : ISpellEffectDependencyResolver
    {
        public IDamageCalculator CreateDamageCalculator() => null;
        public IEntityFactory GetEntityFactory() => null;
        public IForcedMovementGenerator GetForcedMovementGenerator() => null;
        public IAssetManager GetAssetManager() => null;
        public IGlobalAchievementManager GetGlobalAchievementManager() => null;
        public IGlobalResidenceManager GetGlobalResidenceManager() => null;
        public IGlobalLootManager GetGlobalLootManager() => null;
        public IMapLockManager GetMapLockManager() => null;
        public IGlobalSpellManager GetGlobalSpellManager() => null;
        public IGameTableManager GetGameTableManager() => tables;
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
