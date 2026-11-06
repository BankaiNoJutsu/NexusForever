using System.Buffers.Binary;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Abilities;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Spell;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public sealed class AmpProcAugmentationTests
{
    private const ushort NodeId = 148;
    private const uint SpellId = 59126, TriggerId = 60979;
    // Exact build16042 raw node148 and effect141788; outer Spell4 fields below
    // are the owning-reader-qualified zero-timing/group2 projection.
    private const string NodeRow = "940000000600000005000000010000000400000000000000f6e600000000000005000000010000005b2407005c240700";
    private const string EffectRow = "dc290200f6e60000010000000e00000000000000000000000000000000000000020000000100000033ee00000000803f09000000e803000001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000803f0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffffffff00000000";

    [Fact]
    public void LoginAppliesTheActualProcOnceWithoutLearningOrGrantingEntitlements()
    {
        using var f = new Fixture();
        Assert.True(AugmentationSpellRules.IsSupported(SpellId, f.Tables));
        var registration = Assert.Single(f.Unit.CreateProcRegistrationSnapshot());
        Assert.Equal(SpellId, registration.Spell4Id); Assert.Equal(TriggerId, registration.TriggerSpell4Id);
        Assert.Equal(1u, registration.TriggerEvent); Assert.Equal(9u, registration.TargetData);
        Assert.Equal(1f, registration.Chance); Assert.Equal(1000u, registration.CooldownMsOrSentinel);
        f.Manager.Initialise(); f.Manager.Update(1); f.Manager.Update(1);
        Assert.Single(f.Casts);
        Assert.Null(f.Manager.GetSpell(38483));
        var parameters = Assert.IsType<AugmentationSpellParameters>(f.Casts[0].Arguments[1]);
        Assert.False(parameters.UserInitiatedSpellCast); Assert.True(parameters.IgnoreGlobalCooldown);
        Assert.False(parameters.CancelActiveTrade);
    }

    [Fact]
    public void InactiveAndSharedSpecsRetainOnlyTheOriginalRegistration()
    {
        using var f = new Fixture(Model(0, (1, NodeId), (2, NodeId)));
        Assert.Empty(f.Casts);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(1));
        var registration = Assert.Single(f.Unit.CaptureSpellProcInstances(SpellId));
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(2));
        Assert.Same(registration, Assert.Single(f.Unit.CaptureSpellProcInstances(SpellId)));
        Assert.Single(f.Casts);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(0));
        Assert.Empty(f.Unit.CreateProcRegistrationSnapshot());
    }

    [Theory]
    [InlineData(AmpRespecType.Full, 0u)]
    [InlineData(AmpRespecType.Section, 5u)]
    [InlineData(AmpRespecType.Single, 148u)]
    public void RespecRemovesOnlyItsOwnedRegistration(AmpRespecType type, uint value)
    {
        using var f = new Fixture();
        Assert.Single(f.Unit.CreateProcRegistrationSnapshot());
        var unrelated = f.Register(999, 99999);
        f.Manager.GetActionSet(0).RemoveAmp(type, value);
        Assert.Same(unrelated, Assert.Single(f.Unit.CaptureSpellProcInstances(99999)));
        Assert.Empty(f.Unit.CaptureSpellProcInstances(SpellId));
    }

    [Fact]
    public void SameSpellReplacementWithReusedEffectAndCastingIdsIsNotOwned()
    {
        using var f = new Fixture();
        var original = Assert.Single(f.Unit.CaptureSpellProcInstances(SpellId));
        Assert.True(f.Unit.RemoveProc(original));
        var replacement = f.Register(original.EffectId, SpellId);
        Assert.NotSame(original, replacement);
        f.Manager.Update(1);
        Assert.Single(f.Casts);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.Same(replacement, Assert.Single(f.Unit.CaptureSpellProcInstances(SpellId)));
    }

    [Fact]
    public void PreExistingSameSpellRegistrationIsNeitherRecastNorClaimed()
    {
        using var f = new Fixture(initialise: false);
        var external = f.Register(777, SpellId);
        f.Manager.Initialise(); f.Manager.Update(1);
        Assert.Empty(f.Casts);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.Same(external, Assert.Single(f.Unit.CaptureSpellProcInstances(SpellId)));
    }

    [Fact]
    public void RegistrationInsertedByACastCallbackIsNotClaimedByTheAmp()
    {
        using var f = new Fixture(initialise: false);
        ISpellProcInstance external = null;
        f.BeforeRegistration = () => external = f.Register(777, SpellId);
        f.Manager.Initialise();
        Assert.Equal(2, f.Unit.CreateProcRegistrationSnapshot().Count);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.Same(external, Assert.Single(f.Unit.CaptureSpellProcInstances(SpellId)));
    }

    [Fact]
    public void RemovedRegistrationRetriesAndAnOldHandleCannotRemoveTheNewOne()
    {
        using var f = new Fixture();
        var old = Assert.Single(f.Unit.CaptureSpellProcInstances(SpellId));
        Assert.True(f.Unit.RemoveProc(old)); f.Manager.Update(1);
        Assert.Equal(2, f.Casts.Count);
        var current = Assert.Single(f.Unit.CaptureSpellProcInstances(SpellId));
        Assert.NotSame(old, current); Assert.False(f.Unit.RemoveProc(old));
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.Empty(f.Unit.CreateProcRegistrationSnapshot());
    }

    [Theory]
    [InlineData("dead")] [InlineData("absent")] [InlineData("rejected")]
    public void UnavailableOrRejectedCastsRetryOnlyWhenReady(string state)
    {
        using var f = new Fixture(initialise: false);
        f.Player.SetProperty(nameof(IPlayer.IsAlive), state != "dead");
        f.Player.SetProperty(nameof(IPlayer.InWorld), state != "absent");
        f.Result = state == "rejected" ? CastResult.SpellCooldown : CastResult.Ok;
        f.Manager.Initialise(); Assert.Empty(f.Unit.CreateProcRegistrationSnapshot());
        f.Player.SetProperty(nameof(IPlayer.IsAlive), true); f.Player.SetProperty(nameof(IPlayer.InWorld), true);
        f.Result = CastResult.Ok; f.Manager.Update(1);
        Assert.Single(f.Unit.CreateProcRegistrationSnapshot());
        Assert.Equal(state == "rejected" ? 2 : 1, f.Casts.Count);
    }

    [Fact]
    public void RespecDuringAcceptedCastCannotLeaveARetiredRegistration()
    {
        using var f = new Fixture(initialise: false);
        f.BeforeRegistration = () => f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        f.Manager.Initialise();
        Assert.Single(f.Casts);
        Assert.Empty(f.Unit.CreateProcRegistrationSnapshot());
        f.Manager.Update(1); Assert.Single(f.Casts);
    }

    [Theory]
    [InlineData("event")] [InlineData("route")] [InlineData("count-tail")]
    [InlineData("spell-filter")] [InlineData("condition-filter")] [InlineData("chance")]
    [InlineData("timed")] [InlineData("target")] [InlineData("mixed")]
    public void UnsupportedProcInputsStayInactive(string scenario)
    {
        using var f = new Fixture(scenario: scenario);
        Assert.False(AugmentationSpellRules.IsSupported(SpellId, f.Tables));
        Assert.Empty(f.Casts); Assert.Empty(f.Unit.CreateProcRegistrationSnapshot());
    }

    [Fact]
    public void SavedNodeReloadsItsRegistrationAndRespecDoesNotPersistEffectsOrEntitlements()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); connection.Open();
        using var db = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated(); db.Add(new CharacterModel { Id = 42, Name = "ProcAmp" }); db.SaveChanges();
        using (var f = new Fixture(Model(0)))
        {
            f.Manager.GetActionSet(0).AddAmp(NodeId);
            Assert.Single(f.Unit.CreateProcRegistrationSnapshot());
            f.Manager.Save(db); db.SaveChanges();
        }
        db.ChangeTracker.Clear();
        var model = db.Character.Include(c => c.ActionSetAmp).AsNoTracking().Single();
        using (var f = new Fixture(model))
        {
            Assert.Single(f.Unit.CreateProcRegistrationSnapshot());
            f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
            Assert.Empty(f.Unit.CreateProcRegistrationSnapshot());
            f.Manager.Save(db); db.SaveChanges();
        }
        Assert.Empty(db.CharacterActionSetAmp); Assert.Empty(db.CharacterSpell); Assert.Empty(db.CharacterEntitlement);
        db.ChangeTracker.Clear();
        using var removed = new Fixture(db.Character.Include(c => c.ActionSetAmp).AsNoTracking().Single());
        Assert.Empty(removed.Casts); Assert.Empty(removed.Unit.CreateProcRegistrationSnapshot());
    }

    private static CharacterModel Model(byte spec, params (byte Spec, ushort Node)[] nodes)
    {
        var model = new CharacterModel { Id = 42, ActiveSpec = spec };
        foreach (var n in nodes) model.ActionSetAmp.Add(new() { Id = 42, SpecIndex = n.Spec, AmpId = n.Node });
        return model;
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly Game.Entity.SpellManager Manager;
        internal readonly AmpTableAugmentationTests.TestUnit Unit = new();
        internal readonly RecordingDispatchProxy<IPlayer> Player;
        internal readonly IGameTableManager Tables;
        internal readonly Spell4EffectsEntry Effect = ReadEffect();
        internal CastResult Result = CastResult.Ok;
        internal Action BeforeRegistration;
        internal IReadOnlyList<RecordingDispatchProxy<IPlayer>.Invocation> Casts => Player.GetInvocations(nameof(IPlayer.TryCastSpell));
        private readonly ServiceProvider services;
        private readonly ISpellEffectDependencyResolver previous;
        private uint nextEffect = 500;

        internal Fixture(CharacterModel model = null, bool initialise = true, string scenario = null)
        {
            switch (scenario)
            {
                case "event": Effect.DataBits00 = 76; break;
                case "route": Effect.DataBits03 = 36; break;
                case "count-tail": Effect.DataBits05 = 2; break;
                case "spell-filter": Effect.DataBits06 = 44178; break;
                case "condition-filter": Effect.DataBits08 = 333; break;
                case "chance": Effect.DataBits02 = BitConverter.SingleToUInt32Bits(float.NaN); break;
                case "timed": Effect.DurationTime = 1000; break;
                case "target": Effect.TargetFlags = (uint)SpellEffectTargetFlags.Target; break;
            }
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out var table);
            var entry = new Spell4Entry { Id = SpellId, Spell4BaseIdBaseSpell = 38483, TierIndex = 1, Spell4StackGroupId = 2, PropertyFlags = 1024 };
            table.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(entry, new Spell4Entry { Id = TriggerId }));
            List<Spell4EffectsEntry> effects = [Effect];
            if (scenario == "mixed") effects.Add(new() { Id = 1, SpellId = SpellId, EffectType = SpellEffectType.Fluff });
            table.SetProperty(nameof(IGameTableManager.Spell4Effects), PlayerBindPointTests.Table(effects.ToArray()));
            table.SetProperty(nameof(IGameTableManager.Spell4StackGroup), PlayerBindPointTests.Table(new Spell4StackGroupEntry { Id = 2, StackTypeEnum = 4 }));
            table.SetProperty(nameof(IGameTableManager.EldanAugmentation), PlayerBindPointTests.Table(ReadNode()));
            table.SetProperty(nameof(IGameTableManager.SpellLevel), PlayerBindPointTests.Table<SpellLevelEntry>());
            table.SetProperty(nameof(IGameTableManager.Class), PlayerBindPointTests.Table(new ClassEntry
            { Id = 1, Spell4IdInnateAbilityActive = [], Spell4IdInnateAbilityPassive = [], Spell4IdAttackPrimary = [], Spell4IdAttackUnarmed = [] }));
            services = new ServiceCollection().AddSingleton(Tables).BuildServiceProvider();
            previous = SpellHandler.InitialiseDependencyResolver(new SpellEffectDependencyResolver(services));
            var owner = RecordingDispatchProxy<IPlayer>.Create(out Player);
            var inventory = RecordingDispatchProxy<IInventory>.Create(out var inv);
            inv.SetMethodReturnFactory("GetEnumerator", () => ((IEnumerable<IBag>)Array.Empty<IBag>()).GetEnumerator());
            Player.SetProperty(nameof(IPlayer.Guid), 42u); Player.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            Player.SetProperty(nameof(IPlayer.Class), Class.Warrior); Player.SetProperty(nameof(IPlayer.Level), 50u);
            Player.SetProperty(nameof(IPlayer.IsAlive), true); Player.SetProperty(nameof(IPlayer.InWorld), true);
            Player.SetProperty(nameof(IPlayer.IsLoading), true); Player.SetProperty(nameof(IPlayer.Inventory), inventory);
            Player.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
            Player.SetMethodHandler(nameof(IPlayer.GetSpellPropertyInstances), a => Unit.GetSpellPropertyInstances((uint)a[0]));
            Player.SetMethodHandler(nameof(IPlayer.CaptureSpellPropertyInstances), a => Unit.CaptureSpellPropertyInstances((uint)a[0]));
            Player.SetMethodHandler(nameof(IPlayer.RemoveSpellPropertyInstance), a => Unit.RemoveSpellPropertyInstance((ISpellPropertyInstance)a[0]));
            Player.SetMethodHandler(nameof(IPlayer.CaptureSpellProcInstances), a => Unit.CaptureSpellProcInstances((uint)a[0]));
            Player.SetMethodHandler(nameof(IPlayer.RemoveProc), a => Unit.RemoveProc((ISpellProcInstance)a[0]));
            Player.SetMethodHandler(nameof(IPlayer.AddProc), a => Unit.AddProc(
                (uint)a[0], (uint)a[1], (uint)a[2], (uint)a[3], (uint)a[4], (float)a[5],
                (uint)a[6], (uint)a[7], (uint)a[8], (uint)a[9], (uint)a[10], (uint)a[11], (uint)a[12]));

            Player.SetMethodHandler(nameof(IPlayer.TryCastSpell), a =>
            {
                if (Result != CastResult.Ok) return Result;
                BeforeRegistration?.Invoke();
                var parameters = (ISpellParameters)a[1];
                var info = RecordingDispatchProxy<ISpellInfo>.Create(out var ip); ip.SetProperty(nameof(ISpellInfo.Entry), entry);
                parameters.SpellInfo = info;
                var spell = RecordingDispatchProxy<ISpell>.Create(out var sp);
                sp.SetProperty(nameof(ISpell.Parameters), parameters); sp.SetProperty(nameof(ISpell.Caster), owner);
                sp.SetProperty(nameof(ISpell.CastingId), 500u);
                SpellHandler.HandleEffectProc(spell, owner, new SpellTargetInfo.SpellTargetEffectInfo(nextEffect++, Effect));
                return CastResult.Ok;
            });
            Manager = new Game.Entity.SpellManager(owner, model ?? Model(0, (0, NodeId)),
                RecordingDispatchProxy<IPrerequisiteManager>.Create(out _), RecordingDispatchProxy<IGlobalSpellManager>.Create(out _), Tables);
            Player.SetProperty(nameof(IPlayer.SpellManager), Manager);
            if (initialise) Manager.Initialise();
        }

        internal ISpellProcInstance Register(uint id, uint spell) => Unit.AddProc(id, spell, 500, 1, TriggerId, 1, 9, 1000, 1, 0, 0, 0, 0);
        public void Dispose() { SpellHandler.InitialiseDependencyResolver(previous); services.Dispose(); }
    }

    private static uint Word(string hex, int i) => BinaryPrimitives.ReadUInt32LittleEndian(Convert.FromHexString(hex).AsSpan(i * 4));
    private static EldanAugmentationEntry ReadNode() => new()
    {
        Id = Word(NodeRow, 0), DisplayRow = Word(NodeRow, 1), DisplayColumn = Word(NodeRow, 2), ClassId = Word(NodeRow, 3),
        PowerCost = Word(NodeRow, 4), EldanAugmentationIdRequired = Word(NodeRow, 5), Spell4IdAugment = Word(NodeRow, 6),
        Item2IdUnlock = Word(NodeRow, 7), EldanAugmentationCategoryId = Word(NodeRow, 8), CategoryTier = Word(NodeRow, 9),
        LocalizedTextIdTitle = Word(NodeRow, 10), LocalizedTextIdTooltip = Word(NodeRow, 11)
    };
    private static Spell4EffectsEntry ReadEffect() => new()
    {
        Id = Word(EffectRow, 0), SpellId = Word(EffectRow, 1), TargetFlags = Word(EffectRow, 2), EffectType = (SpellEffectType)Word(EffectRow, 3),
        DelayTime = Word(EffectRow, 5), TickTime = Word(EffectRow, 6), DurationTime = Word(EffectRow, 7), Flags = Word(EffectRow, 8),
        DataBits00 = Word(EffectRow, 9), DataBits01 = Word(EffectRow, 10), DataBits02 = Word(EffectRow, 11),
        DataBits03 = Word(EffectRow, 12), DataBits04 = Word(EffectRow, 13), DataBits05 = Word(EffectRow, 14),
        DataBits06 = Word(EffectRow, 15), DataBits07 = Word(EffectRow, 16), DataBits08 = Word(EffectRow, 17), DataBits09 = Word(EffectRow, 18)
    };
}
