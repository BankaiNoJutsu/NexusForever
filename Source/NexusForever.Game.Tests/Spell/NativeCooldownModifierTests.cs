using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Spell.Event;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Spell.Event;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Script.Template.Collection;
using RuntimeSpell = NexusForever.Game.Spell.Spell;

namespace NexusForever.Game.Tests.Spell;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class NativeCooldownModifierTests
{
    private const uint Primary = 61001, Sibling = 61002, Other = 62001, Source = 70001;

    [Theory]
    [InlineData(0u, 0.5f, 5d)]
    [InlineData(0u, 2f, 10d)]
    [InlineData(0u, 0f, 0d)]
    [InlineData(1u, 2f, 20d)]
    [InlineData(1u, 0.5f, 5d)]
    [InlineData(2u, -1000.9f, 9d)]
    [InlineData(2u, -15000f, 0d)]
    [InlineData(2u, 1000.9f, 11d)]
    [InlineData(3u, 2500.9f, 2.5d)]
    [InlineData(3u, 20000f, 20d)]
    public void NativeModeControlsArithmeticRatherThanMagnitudeSign(uint mode, float magnitude, double expected)
    {
        var f = new Fixture();
        f.Manager.SetSpellCooldown(Other, 10);
        f.Session.Invocations.Clear();
        Assert.True(f.Manager.ApplySpellCooldownModifier(new(1, Source, 9, 1, Other, mode, magnitude), false));
        Assert.Equal(expected, f.Manager.GetSpellCooldown(Other), 3);
        Assert.Empty(f.Packets);
        Assert.Empty(f.Manager.GetSpellCooldownModifiers());
    }

    [Theory]
    // Raw shipped-format words: scope, key, mode, float magnitude. No production writer constructs this fixture.
    [InlineData("00000000420000000000000000000000")]
    [InlineData("0100000049EE00000000000000000000")]
    [InlineData("02000000420000000000000000000000")]
    [InlineData("03000000230000000000000000000000")]
    public void RawRecordMatchesItsOwnIdentitySpaceAndNeverStartsIdleCooldowns(string hex)
    {
        var f = new Fixture();
        f.Manager.SetSpellCooldown(Primary, 10);
        f.Manager.SetSpellCooldown(Other, 9);
        var effect = Effect(hex);
        Game.Spell.SpellHandler.HandleEffectModifySpellCooldown(f.Cast(), f.Player, effect);
        Assert.Equal(0, f.Manager.GetSpellCooldown(Primary));
        Assert.Equal(0, f.Manager.GetSpellCooldown(Sibling));
        Assert.Equal(9, f.Manager.GetSpellCooldown(Other));
        Assert.Empty(f.Manager.GetSpellCooldownModifiers());
        Assert.Equal(0, f.Manager.GetSpellCooldown(99999));
    }

    [Fact]
    public void GroupRearmTouchesEachRunningSharedNodeOnlyOnce()
    {
        var f = new Fixture();
        f.Manager.SetSpellCooldown(Primary, 10);
        f.Manager.SetSpellCooldown(Sibling, 10);
        Assert.True(f.Manager.ApplySpellCooldownModifier(new(1, Source, 9, 2, 66, 1, 0.5f), false));
        Assert.Equal(5, f.Manager.GetSpellCooldown(Primary));
        Assert.Equal(5, f.Manager.GetSpellCooldown(Sibling));
    }

    [Fact]
    public void HeldRecordsFoldInInsertionOrderModeZeroUsesOriginalInputAndWireStaysUnfolded()
    {
        var f = new Fixture();
        Assert.True(f.Manager.ApplySpellCooldownModifier(new(1, Source, 9, 1, Other, 2, -1000), true));
        Assert.True(f.Manager.ApplySpellCooldownModifier(new(2, Source, 10, 1, Other, 0, 0.5f), true));
        Assert.Equal(0, f.Manager.GetSpellCooldown(Other));
        f.Manager.SetSpellCooldown(Other, 10);
        Assert.Equal(5, f.Manager.GetSpellCooldown(Other));
        Assert.Equal(10000u, Assert.Single(f.Packets).Cooldown.TimeRemaining);
        Assert.False(f.Manager.ApplySpellCooldownModifier(new(2, Source, 10, 1, Other, 0, 0.5f), true));
        Assert.Equal(5, f.Manager.GetSpellCooldown(Other));
        Assert.True(f.Manager.RemoveSpellCooldownModifier(2));
        Assert.Equal(5, f.Manager.GetSpellCooldown(Other));
        f.Manager.SetSpellCooldown(Other, 0);
        f.Manager.SetSpellCooldown(Other, 10);
        Assert.Equal(9, f.Manager.GetSpellCooldown(Other));
        Assert.True(f.Manager.RemoveSpellCooldownModifier(1));
    }

    [Fact]
    public void RecastOwnsSeparateConcreteRecordsAndRemovalDoesNotUndoRunningTimers()
    {
        var f = new Fixture();
        f.Manager.ApplySpellCooldownModifier(new(1, Source, 9, 1, Other, 1, 0.5f), true);
        f.Manager.ApplySpellCooldownModifier(new(2, Source, 10, 1, Other, 1, 0.5f), true);
        f.Manager.SetSpellCooldown(Other, 10);
        Assert.Equal(2.5, f.Manager.GetSpellCooldown(Other));
        Assert.True(f.Manager.RemoveSpellCooldownModifier(1));
        Assert.False(f.Manager.RemoveSpellCooldownModifier(1));
        Assert.Equal(2.5, f.Manager.GetSpellCooldown(Other));
        f.Manager.SetSpellCooldown(Other, 0);
        f.Manager.SetSpellCooldown(Other, 10);
        Assert.Equal(5, f.Manager.GetSpellCooldown(Other));
        Assert.Equal(2u, Assert.Single(f.Manager.GetSpellCooldownModifiers()).EffectId);
    }

    [Theory]
    [InlineData(99u, 1u, 1u, 0.5f)]
    [InlineData(1u, 1u, 219u, 0.5f)]
    [InlineData(1u, 0u, 0u, 0.5f)]
    [InlineData(1u, 1u, 0u, float.NaN)]
    [InlineData(1u, 1u, 0u, float.PositiveInfinity)]
    [InlineData(1u, 1u, 1u, -1f)]
    public void UnsupportedRecordsDoNotCreateOrHoldState(uint scope, uint key, uint mode, float amount)
    {
        var f = new Fixture();
        Assert.False(f.Manager.ApplySpellCooldownModifier(new(1, Source, 9, scope, key, mode, amount), true));
        Assert.Empty(f.Manager.GetSpellCooldownModifiers());
        Assert.Empty(f.Packets);
    }

    [Fact]
    public void SharedAndDirectNodesUseDistinctWireTypesKeysAndTableDurations()
    {
        var f = new Fixture(nodeMilliseconds: 20000);
        f.Manager.SetSpellCooldown(35, 7); // concrete Spell4 ID collides numerically with the shared node ID
        f.Manager.SetSpellCooldown(Primary, 10);
        Assert.Equal(7, f.Manager.GetSpellCooldown(35));
        Assert.Equal(20, f.Manager.GetSpellCooldown(Primary));
        Assert.Equal(20, f.Manager.GetSpellCooldown(Sibling));
        var packets = f.Packets.Where(p => p.Cooldown.SpellId == Primary).ToArray();
        Assert.Equal(2, packets.Length);
        Assert.Contains(packets, p => p.Cooldown.Type == 1 && p.Cooldown.TypeId == Primary && p.Cooldown.TimeRemaining == 10000);
        Assert.Contains(packets, p => p.Cooldown.Type == 2 && p.Cooldown.TypeId == 35 && p.Cooldown.TimeRemaining == 20000);
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            packets.Single(p => p.Cooldown.Type == 2).Write(writer);
            writer.FlushBits();
        }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        Assert.Equal(2u, reader.ReadUInt(3));
        Assert.Equal(Primary, reader.ReadUInt(18));
        Assert.Equal(35u, reader.ReadUInt());
        Assert.Equal(20000u, reader.ReadUInt());
        f.Manager.ResetAllSpellCooldowns();
        Assert.Equal(0, f.Manager.GetSpellCooldown(Primary));
        Assert.Equal(0, f.Manager.GetSpellCooldown(Sibling));
    }

    [Theory]
    [InlineData(1u, 0.5f, 5d, 10d)]
    [InlineData(2u, 0.5f, 5d, 10d)]
    [InlineData(7u, 0.5f, 5d, 10d)]
    [InlineData(1u, 0f, 0d, 0d)]
    [InlineData(9u, 0.5f, 10d, 20d)]
    [InlineData(1u, 1f, 10d, 20d)]
    public void Property136ScalingUsesNativeAbilityTagsWithoutChangingTheDefault(uint tag, float scale, double direct, double shared)
    {
        var f = new Fixture(nodeMilliseconds: 20000, tag: tag, scale: scale);
        f.Manager.SetSpellCooldown(Primary, 10);
        Assert.Contains(f.Packets, p => p.Cooldown.Type == 1 && p.Cooldown.TimeRemaining == direct * 1000);
        Assert.Contains(f.Packets, p => p.Cooldown.Type == 2 && p.Cooldown.TimeRemaining == shared * 1000);
        Assert.Equal(shared, f.Manager.GetSpellCooldown(Primary));
    }

    [Fact]
    public void HeldCooldownIdModifierAffectsFutureGcdWithoutRearmingCurrentGcd()
    {
        var f = new Fixture();
        f.Manager.SetGlobalSpellCooldown(121, 2);
        f.Manager.ApplySpellCooldownModifier(new(1, Source, 9, 3, 121, 1, 0.5f), true);
        Assert.Equal(2, f.Manager.GetGlobalSpellCooldown());
        f.Manager.SetGlobalSpellCooldown(121, 0);
        f.Manager.SetGlobalSpellCooldown(121, 2);
        Assert.Equal(1, f.Manager.GetGlobalSpellCooldown());
        Assert.Equal(0, f.Packets.Last().Cooldown.Type);
        Assert.Equal(1000u, f.Packets.Last().Cooldown.TimeRemaining);
    }

    [Theory]
    [InlineData(0u, 1000u, 0u, true)]
    [InlineData(500u, 0u, 0u, true)]
    [InlineData(0u, 0u, 2u, false)]
    public void HandlerLifetimeUsesConcreteRemovalAndCancelOnlyDoesNotExpire(uint delay, uint duration, uint flags, bool expires)
    {
        var f = new Fixture();
        var info = Effect("0100000031F20000010000000000003F", effectId: 19, delay: delay, duration: duration, flags: flags);
        var cast = f.Cast();
        Game.Spell.SpellHandler.HandleEffectModifySpellCooldown(cast, f.Player, info);
        Assert.Equal(19u, Assert.Single(f.Manager.GetSpellCooldownModifiers()).EffectId);
        var effect = SpellEffectInterpreter.Interpret(info.Entry);
        Assert.Equal(expires, RuntimeSpell.TryGetEffectLifetimeDelay(effect, out _));
        var realSpell = LifetimeSpell(f, info);
        Invoke(realSpell, "ScheduleEffectLifetime", effect, f.Player, info);
        ((ISpellEventManager)Field(realSpell, "events")).Update(2);
        Assert.Equal(expires, info.LifetimeEnded);
        Assert.Equal(expires ? 0 : 1, f.Manager.GetSpellCooldownModifiers().Count);
        if (!expires)
        {
            Assert.True(realSpell.TryCancelEffect(f.Player));
            Assert.Empty(f.Manager.GetSpellCooldownModifiers());
        }
    }

    [Fact]
    public void DispelAndForceRemovalStateHookRespectsSourceLimitAndUnrelatedConcreteRecords()
    {
        var f = new Fixture();
        f.Manager.ApplySpellCooldownModifier(new(1, Source, 9, 1, Other, 1, 0.5f), true);
        f.Manager.ApplySpellCooldownModifier(new(2, Source, 10, 1, Other, 1, 0.5f), true);
        f.Manager.ApplySpellCooldownModifier(new(3, Source + 1, 11, 1, Other, 1, 0.5f), true);
        var unit = new Player(RecordingDispatchProxy<IMovementManager>.Create(out _), null, null, null, null, f.Tables, null);
        typeof(Player).GetProperty(nameof(Player.SpellManager))!.SetValue(unit, f.Manager);
        var removed = unit.RemoveTrackedSpellStates(id => id == Source, 1);
        Assert.Equal(new uint[] { 1, 2 }, removed.Select(r => r.EffectId).ToArray());
        Assert.All(removed, r => Assert.Equal(SpellStateRemovalKind.CooldownModifier, r.Kind));
        Assert.Equal(3u, Assert.Single(f.Manager.GetSpellCooldownModifiers()).EffectId);
        Assert.Empty(unit.RemoveTrackedSpellStates(_ => true, 0));
        Assert.Single(unit.RemoveTrackedSpellStates(_ => true, uint.MaxValue));
        Assert.Empty(f.Manager.GetSpellCooldownModifiers());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingDispelAndForceRemoveHandlersRemoveHeldRecordsWithoutLosingTheirDiagnostics(bool forceRemove)
    {
        var f = new Fixture();
        f.Manager.ApplySpellCooldownModifier(new(1, Source, 9, 1, Other, 1, 0.5f), true);
        f.Manager.ApplySpellCooldownModifier(new(2, Source, 10, 1, Other, 1, 0.5f), true);
        f.Manager.ApplySpellCooldownModifier(new(3, Source + 1, 11, 1, Other, 1, 0.5f), true);
        var unit = new Player(RecordingDispatchProxy<IMovementManager>.Create(out _), null, null, null, null, f.Tables, null);
        typeof(Player).GetProperty(nameof(Player.SpellManager))!.SetValue(unit, f.Manager);
        typeof(Player).GetProperty(nameof(Player.Session))!.SetValue(unit, f.Player.Session);
        var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var gp);
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var bp);
        bp.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 9001 });
        bp.SetProperty(nameof(ISpellBaseInfo.IsDispellable), true);
        gp.SetMethodReturn(nameof(IGlobalSpellManager.GetSpellBaseInfo), baseInfo);
        var services = new ServiceCollection();
        services.AddSingleton(f.Tables);
        services.AddSingleton(global);
        using var provider = services.BuildServiceProvider();
        var previous = Game.Spell.SpellHandler.InitialiseDependencyResolver(new SpellEffectDependencyResolver(provider));
        try
        {
            var info = new SpellTargetInfo.SpellTargetEffectInfo(99, new Spell4EffectsEntry
            {
                Id = 99, SpellId = Source, EffectType = forceRemove ? SpellEffectType.SpellForceRemove : SpellEffectType.SpellDispel,
                DataBits00 = forceRemove ? 2u : 1u, DataBits01 = forceRemove ? Source : 0
            });
            if (forceRemove) Game.Spell.SpellHandler.HandleEffectSpellForceRemove(f.Cast(), unit, info);
            else Game.Spell.SpellHandler.HandleEffectSpellDispel(f.Cast(), unit, info);
            Assert.Equal(3u, Assert.Single(f.Manager.GetSpellCooldownModifiers()).EffectId);
            if (!forceRemove)
                Assert.Contains(info.CombatLogs, l => l is CombatLogDispel d && d.SpellRemovedId == Source && d.InstancesRemoved == 2);
        }
        finally { Game.Spell.SpellHandler.InitialiseDependencyResolver(previous); }
    }

    [Fact]
    public void DiagnosticTailWordsDoNotOverrideNativeModeAndZeroMagnitude()
    {
        var f = new Fixture();
        f.Manager.SetSpellCooldown(Other, 10);
        var entry = Effect("0100000031F200000200000000000000").Entry;
        entry.DataBits04 = 1234;
        entry.DataBits05 = 9999;
        var info = new SpellTargetInfo.SpellTargetEffectInfo(1, entry);
        Game.Spell.SpellHandler.HandleEffectModifySpellCooldown(f.Cast(), f.Player, info);
        Assert.Equal(10, f.Manager.GetSpellCooldown(Other));
        Assert.Equal(1234u, info.Interpretation.ModifySpellCooldown.DataBits04);
        Assert.Equal(9999u, info.Interpretation.ModifySpellCooldown.DataBits05);
    }

    private static SpellTargetInfo.SpellTargetEffectInfo Effect(string hex, uint effectId = 1, uint delay = 0, uint duration = 0, uint flags = 0)
    {
        byte[] bytes = Convert.FromHexString(hex);
        return new(effectId, new Spell4EffectsEntry { Id = 173869, SpellId = Source, EffectType = SpellEffectType.ModifySpellCooldown,
            DataBits00 = BinaryPrimitives.ReadUInt32LittleEndian(bytes), DataBits01 = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)),
            DataBits02 = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8)), DataBits03 = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12)),
            DelayTime = delay, DurationTime = duration, Flags = flags });
    }

    private static RuntimeSpell LifetimeSpell(Fixture f, ISpellTargetEffectInfo info)
    {
        var spell = (RuntimeSpell)RuntimeHelpers.GetUninitializedObject(typeof(RuntimeSpell));
        Set(spell, "<Caster>k__BackingField", f.Player);
        Set(spell, "<Parameters>k__BackingField", f.Cast().Parameters);
        Initialise(spell, "lifetimeEvents");
        Set(spell, "events", new SpellEventManager());
        var target = new SpellTargetInfo(SpellEffectTargetFlags.Caster, f.Player);
        target.Effects.Add(info);
        Set(spell, "targets", new List<ISpellTargetInfo> { target });
        Set(spell, "status", SpellStatus.Executing);
        Set(spell, "scriptCollection", RecordingDispatchProxy<IScriptCollection>.Create(out _));
        return spell;
    }

    private sealed class Fixture
    {
        public Game.Entity.SpellManager Manager { get; }
        public IPlayer Player { get; }
        public IGameTableManager Tables { get; }
        public RecordingDispatchProxy<IGameSession> Session { get; }
        public List<ServerCooldown> Packets => Session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(i => i.Arguments[0]).OfType<ServerCooldown>().ToList();
        public Fixture(uint nodeMilliseconds = 0, uint tag = 0, float scale = 1)
        {
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
            tp.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(
                new Spell4Entry { Id = Primary, Spell4BaseIdBaseSpell = 66, Spell4GroupListId = 1, SpellCoolDownId00 = 35, Spell4TagId00 = tag },
                new Spell4Entry { Id = Sibling, Spell4BaseIdBaseSpell = 66, Spell4GroupListId = 1, SpellCoolDownId00 = 35 },
                new Spell4Entry { Id = Other, Spell4BaseIdBaseSpell = 67, Spell4GroupListId = 2 }, new Spell4Entry { Id = 35 },
                new Spell4Entry { Id = Source, Spell4BaseIdBaseSpell = 9001 }, new Spell4Entry { Id = Source + 1, Spell4BaseIdBaseSpell = 9002 }));
            tp.SetProperty(nameof(IGameTableManager.Spell4GroupList), PlayerBindPointTests.Table(new Spell4GroupListEntry { Id = 1, SpellGroupId00 = 66 }, new Spell4GroupListEntry { Id = 2, SpellGroupId00 = 67 }));
            tp.SetProperty(nameof(IGameTableManager.SpellCoolDown), PlayerBindPointTests.Table(new SpellCoolDownEntry { Id = 35, CooldownTime = nodeMilliseconds }));
            Player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            var session = RecordingDispatchProxy<IGameSession>.Create(out var sp);
            Session = sp;
            pp.SetProperty(nameof(IPlayer.Session), session);
            pp.SetProperty(nameof(IPlayer.Guid), 42u);
            pp.SetProperty(nameof(IPlayer.IsLoading), false);
            pp.SetMethodReturn(nameof(IPlayer.GetPropertyValue), scale);
            Manager = (Game.Entity.SpellManager)RuntimeHelpers.GetUninitializedObject(typeof(Game.Entity.SpellManager));
            Initialise(Manager, "activeCooldowns");
            Set(Manager, "gameTableManager", Tables);
            Set(Manager, "player", Player);
            pp.SetProperty(nameof(IPlayer.SpellManager), Manager);
        }

        public ISpell Cast()
        {
            var spell = RecordingDispatchProxy<ISpell>.Create(out var sp);
            var info = RecordingDispatchProxy<ISpellInfo>.Create(out var ip);
            ip.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = Source });
            var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var bp);
            bp.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 9001 });
            ip.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
            sp.SetProperty(nameof(ISpell.Parameters), new SpellParameters { SpellInfo = info });
            sp.SetProperty(nameof(ISpell.CastingId), 9u);
            sp.SetProperty(nameof(ISpell.Caster), Player);
            return spell;
        }
    }

    private static object Field(object o, string name) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(o)!;
    private static void Set(object o, string name, object value) => o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(o, value);
    private static void Initialise(object o, string name) => Set(o, name, Activator.CreateInstance(o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.FieldType)!);
    private static void Invoke(RuntimeSpell spell, string name, SpellEffectInterpretation effect, IUnitEntity target, ISpellTargetEffectInfo info) =>
        typeof(RuntimeSpell).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, [typeof(SpellEffectInterpretation), typeof(IUnitEntity), typeof(ISpellTargetEffectInfo)])!.Invoke(spell, [effect, target, info]);
}
