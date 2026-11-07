using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;

namespace NexusForever.Game.Tests.Prerequisite;

public sealed class UnitPrerequisiteContextTests
{
    [Theory]
    [InlineData(1, 20u)]
    [InlineData(4, 17u)]
    [InlineData(12, 0u)]
    [InlineData(15, 123u)]
    [InlineData(28, 1u)]
    [InlineData(40, 50u)]   // type 40 compares the health percentage: 45 of 90 is 50 (PREREQUISITE_HEALTH_PERCENT_20261107.md)
    [InlineData(71, 50u)]
    [InlineData(73, 23u)]
    [InlineData(82, (uint)EntityType.NonPlayer)]
    [InlineData(250, 17u)]
    [InlineData(215, 50u)]
    public void SupportedNpcChecksEvaluateSubjectRatherThanUnrelatedTarget(int type, uint required)
    {
        using var fixture = new Fixture(Row(1, (PrerequisiteType)type, required,
            objectId: (uint)(type == 71 ? Vital.Health : Vital.Resource1)));
        ICreatureEntity subject = RecordingDispatchProxy<ICreatureEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Level), 20u);
        proxy.SetProperty(nameof(IUnitEntity.Faction1), (Game.Static.Reputation.Faction)17);
        proxy.SetProperty(nameof(IUnitEntity.InCombat), true);
        proxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        proxy.SetProperty(nameof(IUnitEntity.Health), 45u);
        proxy.SetProperty(nameof(IUnitEntity.MaxHealth), 90u);
        proxy.SetProperty(nameof(IUnitEntity.Shield), 9u);
        proxy.SetProperty(nameof(IUnitEntity.MaxShieldCapacity), 18u);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);
        proxy.SetMethodReturn(nameof(IUnitEntity.HasTrackedSpellState), true);
        proxy.SetMethodHandler(nameof(IUnitEntity.TryGetVitalValue), args => { args[1] = 23f; return true; });
        IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out _);
        var parameters = new PrerequisiteParameters { Target = target };
        Assert.True(fixture.Manager.MeetsUnit(subject, 1, parameters));
        Assert.Same(target, parameters.Target);
    }

    // Type 38 compares the unit's creature id with Value (PREREQUISITE_CREATURE_ITEM_CHALLENGE_20261107.md): the NPC subject carries the id, the unrelated player target does not.
    [Fact]
    public void IsCreatureEvaluatesTheNpcSubjectRatherThanTheUnrelatedTarget()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.IsCreature, 16623u));
        ICreatureEntity subject = RecordingDispatchProxy<ICreatureEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureId), 16623u);
        IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.True(fixture.Manager.MeetsUnit(subject, 1, new PrerequisiteParameters { Target = target }));
        Assert.False(fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = subject }));
    }

    // Type 39 (IsPlayer) returns the flag for Equal and its negation for NotEqual (PREREQUISITE_FLAG_COMPARISON_20261107.md). The NPC subject is not a player while the unrelated target is, so only a
    // subject-based evaluation makes NotEqual meet and Equal fail.
    [Theory]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    [InlineData(PrerequisiteComparison.Equal, false)]
    public void IsPlayerEvaluatesTheNpcSubjectRatherThanTheUnrelatedPlayerTarget(PrerequisiteComparison comparison, bool expected)
    {
        PrerequisiteEntry row = Row(1, PrerequisiteType.IsPlayer, 0u);
        row.PrerequisiteComparisonId = [comparison];
        using var fixture = new Fixture(row);
        ICreatureEntity subject = RecordingDispatchProxy<ICreatureEntity>.Create(out _);
        IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.Equal(expected, fixture.Manager.MeetsUnit(subject, 1, new PrerequisiteParameters { Target = target }));
    }

    [Theory]
    [InlineData(24, 7u)]
    [InlineData(78, (uint)ModeType.Swim)]
    [InlineData(79, 1u)]
    [InlineData(80, 50u)]
    [InlineData(86, 1u)]
    [InlineData(87, 1u)]
    [InlineData(129, 123u)]
    [InlineData(172, 50u)]
    [InlineData(266, 1u)]
    public void ExistingUnitStateProxiesKeepTheirScalarBehaviorOnTheExplicitSubject(int type, uint value)
    {
        using var fixture = new Fixture(Row(1, (PrerequisiteType)type, value));
        IUnitEntity subject = Unit(2, 20, out var proxy);
        var movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMode), ModeType.Swim);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), StateFlags.Jump | StateFlags.Move);
        movementProxy.SetProperty(nameof(IMovementManager.ServerControl), true);
        proxy.SetProperty(nameof(IUnitEntity.MovementManager), movement);
        proxy.SetProperty(nameof(IUnitEntity.PublicEventPhase), 7u);
        proxy.SetProperty(nameof(IUnitEntity.Health), 45u);
        proxy.SetProperty(nameof(IUnitEntity.MaxHealth), 90u);
        proxy.SetProperty(nameof(IUnitEntity.Type), EntityType.Pet);
        proxy.SetMethodReturn(nameof(IUnitEntity.HasTrackedSpellState), true);
        // Type 129 now tests ObjectId as a spell group id on the unit (PREREQUISITE_ACTIVE_SPELL_GROUP_20261107.md); the subject answers it, the unrelated target does not.
        proxy.SetMethodReturn(nameof(IUnitEntity.HasActiveSpellInGroup), true);
        Assert.True(fixture.Manager.MeetsUnit(subject, 1, new PrerequisiteParameters { Target = Unit(3, 10, out _) }));
    }

    [Fact]
    public void OldPlayerTargetApiRetainsTargetProxyWhileUnitApiUsesExplicitSubject()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.InCombat, 1));
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IUnitEntity target = Unit(2, 20, out var targetProxy);
        playerProxy.SetProperty(nameof(IPlayer.InCombat), false);
        targetProxy.SetProperty(nameof(IUnitEntity.InCombat), true);
        var parameters = new PrerequisiteParameters { Target = target };
        Assert.True(fixture.Manager.Meets(player, 1, parameters));
        Assert.False(fixture.Manager.MeetsUnit(player, 1, parameters));
        Assert.True(fixture.Manager.MeetsUnit(target, 1, parameters));
    }

    [Theory]
    [InlineData(EvaluationMode.EvaluateAND, false)]
    [InlineData(EvaluationMode.EvaluateOR, true)]
    public void MixedNpcRowsRetainAndOrSemanticsAndUnsupportedPlayerChecksFailClosed(EvaluationMode mode, bool expected)
    {
        var row = Row(1, PrerequisiteType.Level, 20);
        row.Flags = mode;
        row.PrerequisiteTypeId = [PrerequisiteType.Level, PrerequisiteType.Class];
        row.PrerequisiteComparisonId = [PrerequisiteComparison.Equal, PrerequisiteComparison.Equal];
        row.Value = [20, (uint)Class.Warrior];
        row.ObjectId = [0, 0];
        using var fixture = new Fixture(row);
        Assert.Equal(expected, fixture.Manager.MeetsUnit(Unit(2, 20, out _), 1, null));
    }

    [Fact]
    public void PlayerOnlyDefaultAdapterStillCallsExistingHandlerWithAllParameters()
    {
        IPrerequisiteCheck check = new LegacyCheck();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var parameters = new PrerequisiteParameters();
        Assert.True(check.MeetsUnit(player, PrerequisiteComparison.Equal, 1, 2, parameters));
        Assert.Same(parameters, ((LegacyCheck)check).Parameters);
        Assert.False(check.MeetsUnit(Unit(2, 20, out _), PrerequisiteComparison.NotEqual, 1, 2, parameters));
    }

    [Fact]
    public void ExistingCustomManagerRemainsCompatibleWithNewPlayerEntryPoint()
    {
        IPrerequisiteManager manager = new LegacyManager();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        Assert.True(manager.MeetsUnit(player, 1, new PrerequisiteParameters()));
        Assert.False(manager.MeetsUnit(Unit(2, 20, out _), 1, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedPrerequisitesPreserveItemTargetTaxiAndAccountContext(bool unitApi)
    {
        var nested = Row(1, PrerequisiteType.OtherPrerequisite, 0, 2);
        var leaf = Row(2, PrerequisiteType.EvalContextFloatByObjectId, 0);
        using var fixture = new Fixture([nested, leaf], services =>
            services.AddKeyedSingleton<IPrerequisiteCheck>(PrerequisiteType.EvalContextFloatByObjectId, new ContextCheck()));
        IPlayer subject = RecordingDispatchProxy<IPlayer>.Create(out _);
        var context = new PrerequisiteParameters
        {
            Target = Unit(3, 10, out _), Item = RecordingDispatchProxy<IItem>.Create(out _), TaxiNode = 37,
            AccountItemContext = true
        };
        Assert.True(unitApi ? fixture.Manager.MeetsUnit(subject, 1, context) : fixture.Manager.Meets(subject, 1, context));
        var check = Assert.IsType<ContextCheck>(fixture.Services.GetRequiredKeyedService<IPrerequisiteCheck>(PrerequisiteType.EvalContextFloatByObjectId));
        Assert.Same(context, check.Parameters);
    }

    [Fact]
    public void NestedNpcPrerequisiteRetainsSubjectAndSeparateTarget()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.OtherPrerequisite, 0, 2), Row(2, PrerequisiteType.Level, 20));
        Assert.True(fixture.Manager.MeetsUnit(Unit(2, 20, out _), 1, new PrerequisiteParameters { Target = Unit(3, 10, out _) }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedItemPrerequisiteUsesTheSuppliedItemInsteadOfAnEmptyContext(bool unitApi)
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.OtherPrerequisite, 0, 2), Row(2, PrerequisiteType.Item2Id, 0, 123));
        var subject = Player(1, 10);
        var item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Id), 123u);
        itemProxy.SetProperty(nameof(IItem.Info), info);
        var parameters = new PrerequisiteParameters { Item = item };
        Assert.True(unitApi ? fixture.Manager.MeetsUnit(subject, 1, parameters) : fixture.Manager.Meets(subject, 1, parameters));
        parameters.Item = null;
        Assert.False(unitApi ? fixture.Manager.MeetsUnit(subject, 1, parameters) : fixture.Manager.Meets(subject, 1, parameters));
    }

    [Fact]
    public void MissingSubjectRowHandlerAndInvalidEvaluationModeFailClosed()
    {
        var unknown = Row(1, (PrerequisiteType)999, 0);
        var invalid = Row(2, PrerequisiteType.Level, 20);
        invalid.Flags = (EvaluationMode)255;
        using var fixture = new Fixture(unknown, invalid);
        var subject = Unit(2, 20, out _);
        Assert.False(fixture.Manager.MeetsUnit(null, 1, null));
        Assert.False(fixture.Manager.MeetsUnit(subject, 0, null));
        Assert.False(fixture.Manager.MeetsUnit(subject, 1, null));
        Assert.False(fixture.Manager.MeetsUnit(subject, 2, null));
    }

    [Fact]
    public void CrossTargetSpellAndDistanceAdaptersRetainSubjectAndOtherUnit()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.UnderSpellOnTarget, 123), Row(2, PrerequisiteType.Distance, 5));
        var subject = Unit(2, 20, out var subjectProxy);
        var target = Unit(3, 10, out var targetProxy);
        subjectProxy.SetProperty(nameof(IUnitEntity.Position), Vector3.Zero);
        targetProxy.SetProperty(nameof(IUnitEntity.Position), new Vector3(3, 0, 4));
        targetProxy.SetMethodHandler(nameof(IUnitEntity.HasTrackedSpellState), args => (uint)args[0] == 123);
        var parameters = new PrerequisiteParameters { Target = target };
        Assert.True(fixture.Manager.MeetsUnit(subject, 1, parameters));
        Assert.True(fixture.Manager.MeetsUnit(subject, 2, parameters));
        Assert.False(fixture.Manager.MeetsUnit(target, 1, new PrerequisiteParameters { Target = subject }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CasterAndTargetCastPrerequisitesEvaluateTheirOwnUnit(bool playerCaster)
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.Level, 10), Row(2, PrerequisiteType.Level, 20));
        var caster = playerCaster ? Player(1, 10) : Unit(1, 10, out _);
        var target = Unit(2, 20, out _);
        var spell = fixture.Spell(caster, target, out var info);
        info.SetProperty(nameof(ISpellInfo.CasterCastPrerequisite), Row(1, PrerequisiteType.Level, 10));
        info.SetProperty(nameof(ISpellInfo.TargetCastPrerequisites), Row(2, PrerequisiteType.Level, 20));
        Assert.Equal(CastResult.Ok, Invoke<CastResult>(spell, "CheckPrerequisites"));
        info.SetProperty(nameof(ISpellInfo.TargetCastPrerequisites), Row(1, PrerequisiteType.Level, 10));
        Assert.Equal(CastResult.PrereqTargetCast, Invoke<CastResult>(spell, "CheckPrerequisites"));
    }

    [Fact]
    public void CastTargetGateRejectsMissingExplicitTargetAndUsesCasterForImplicitSelf()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.Level, 10));
        var caster = Unit(1, 10, out _);
        var spell = fixture.Spell(caster, null, out var info);
        info.SetProperty(nameof(ISpellInfo.TargetCastPrerequisites), Row(1, PrerequisiteType.Level, 10));
        spell.Parameters.PrimaryTargetId = 99;
        Assert.Equal(CastResult.PrereqTargetCast, Invoke<CastResult>(spell, "CheckPrerequisites"));
        spell.Parameters.PrimaryTargetId = 0;
        Assert.Equal(CastResult.Ok, Invoke<CastResult>(spell, "CheckPrerequisites"));
    }

    [Fact]
    public void NpcRunnerCanOverrideCasterGateWithoutSkippingTargetGate()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.Level, 999), Row(2, PrerequisiteType.Level, 10));
        var caster = Unit(1, 10, out _);
        var spell = fixture.Spell(caster, Unit(2, 20, out _), out var info);
        info.SetProperty(nameof(ISpellInfo.CasterCastPrerequisite), Row(1, PrerequisiteType.Level, 999));
        info.SetProperty(nameof(ISpellInfo.PrerequisiteRunners), new List<PrerequisiteEntry> { Row(2, PrerequisiteType.Level, 10) });
        Assert.Equal(CastResult.Ok, Invoke<CastResult>(spell, "CheckPrerequisites"));
        info.SetProperty(nameof(ISpellInfo.TargetCastPrerequisites), Row(1, PrerequisiteType.Level, 999));
        Assert.Equal(CastResult.PrereqTargetCast, Invoke<CastResult>(spell, "CheckPrerequisites"));
    }

    [Fact]
    public void ApplyAndPersistenceCheckBothNpcSubjectsWithoutReversingThem()
    {
        using var fixture = new Fixture(Row(1, PrerequisiteType.Level, 10), Row(2, PrerequisiteType.Level, 20));
        var caster = Unit(1, 10, out _);
        var target = Unit(2, 20, out _);
        var spell = fixture.Spell(caster, target, out var info);
        var entry = new Spell4EffectsEntry { Id = 1, PrerequisiteIdCasterApply = 1, PrerequisiteIdTargetApply = 2,
            PrerequisiteIdCasterPersistence = 1, PrerequisiteIdTargetPersistence = 2 };
        var effect = SpellEffectInterpreter.Interpret(entry);
        Assert.True(Invoke<bool>(spell, "MeetsApplyPrerequisites", effect, target));
        Assert.True(Invoke<bool>(spell, "MeetsPersistencePrerequisites", effect, target));
        Assert.False(Invoke<bool>(spell, "MeetsApplyPrerequisites", effect, caster));
        Assert.False(Invoke<bool>(spell, "MeetsPersistencePrerequisites", effect, caster));
        info.SetProperty(nameof(ISpellInfo.TargetPersistencePrerequisites), Row(1, PrerequisiteType.Level, 10));
        Assert.False(Invoke<bool>(spell, "MeetsPersistencePrerequisites", effect, target));
    }

    private static PrerequisiteEntry Row(uint id, PrerequisiteType type, uint value, uint objectId = 0)
        => new() { Id = id, Flags = EvaluationMode.EvaluateAND, PrerequisiteTypeId = [type],
            PrerequisiteComparisonId = [PrerequisiteComparison.Equal], Value = [value], ObjectId = [objectId] };

    private static IUnitEntity Unit(uint guid, uint level, out RecordingDispatchProxy<IUnitEntity> proxy)
    {
        var unit = RecordingDispatchProxy<IUnitEntity>.Create(out proxy);
        proxy.SetProperty(nameof(IUnitEntity.Guid), guid);
        proxy.SetProperty(nameof(IUnitEntity.Level), level);
        return unit;
    }

    private static IPlayer Player(uint guid, uint level)
    {
        var player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Guid), guid);
        proxy.SetProperty(nameof(IPlayer.Level), level);
        return player;
    }

    private static T Invoke<T>(object instance, string method, params object[] arguments)
        => (T)instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, arguments);

    private sealed class Fixture : IDisposable
    {
        public ServiceProvider Services { get; }
        public IPrerequisiteManager Manager => Services.GetRequiredService<IPrerequisiteManager>();
        private IGameTableManager Tables { get; }

        public Fixture(params PrerequisiteEntry[] rows) : this(rows, null) { }

        public Fixture(PrerequisiteEntry[] rows, Action<IServiceCollection> configure)
        {
            var table = (GameTable<PrerequisiteEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<PrerequisiteEntry>));
            typeof(GameTable<PrerequisiteEntry>).GetProperty(nameof(table.Entries))!.SetValue(table, rows);
            uint max = rows.Select(r => r.Id).DefaultIfEmpty().Max();
            typeof(GameTable<PrerequisiteEntry>).GetField("header", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, new GameTableHeader { MaxId = max + 1 });
            int[] lookup = Enumerable.Repeat(-1, (int)max + 1).ToArray();
            for (int i = 0; i < rows.Length; i++) lookup[rows[i].Id] = i;
            typeof(GameTable<PrerequisiteEntry>).GetField("lookup", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(table, lookup);
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
            tableProxy.SetProperty(nameof(IGameTableManager.Prerequisite), table);
            var services = new ServiceCollection().AddLogging().AddSingleton(Tables);
            services.AddGamePrerequisite();
            configure?.Invoke(services);
            Services = services.BuildServiceProvider();
        }

        public Game.Spell.Spell Spell(IUnitEntity caster, IUnitEntity target, out RecordingDispatchProxy<ISpellInfo> infoProxy)
        {
            // Visibility is the production target resolver, not a target stored on the spell test stub.
            if (caster is IPlayer)
                ((RecordingDispatchProxy<IPlayer>)(object)caster).SetMethodReturn(nameof(IPlayer.GetVisible), target);
            else
                ((RecordingDispatchProxy<IUnitEntity>)(object)caster).SetMethodReturn(nameof(IUnitEntity.GetVisible), target);
            var info = RecordingDispatchProxy<ISpellInfo>.Create(out infoProxy);
            infoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 1 });
            infoProxy.SetProperty(nameof(ISpellInfo.PrerequisiteRunners), new List<PrerequisiteEntry>());
            var scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
            var scripts = RecordingDispatchProxy<IScriptManager>.Create(out var scriptsProxy);
            scriptsProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);
            var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out _);
            return new Game.Spell.Spell(caster, new SpellParameters { SpellInfo = info, PrimaryTargetId = target?.Guid ?? 0 }, Manager, global, scripts, Tables);
        }

        public void Dispose() => Services.Dispose();
    }

    private sealed class LegacyCheck : IPrerequisiteCheck
    {
        public IPrerequisiteParameters Parameters { get; private set; }
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        { Parameters = parameters; return true; }
    }

    private sealed class ContextCheck : IPrerequisiteCheck
    {
        public IPrerequisiteParameters Parameters { get; private set; }
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        { Parameters = parameters; return parameters.Item != null && parameters.Target != null && parameters.TaxiNode == 37 && parameters.AccountItemContext; }
    }

    private sealed class LegacyManager : IPrerequisiteManager
    {
        public bool Meets(IPlayer player, uint prerequisiteId) => true;
        public bool Meets(IPlayer player, uint prerequisiteId, IPrerequisiteParameters parameters) => true;
    }
}
