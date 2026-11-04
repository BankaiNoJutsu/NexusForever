using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Abilities;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Tests.Spell;

public class AmpLifecycleTests
{
    private const ushort Node = 11, SharedNode = 12;
    private const uint Augment = 2001, GrantSpell = 2100, GrantBase = 1100;

    [Fact]
    public void ConstructorLoadsActiveSpecBeforeExplicitPostAssignmentGrants()
    {
        var f = new Fixture(Model(1, (0, Node)), initialise: false);
        Assert.Null(f.Manager.GetSpell(GrantBase));
        Assert.False(f.Manager.IsAmpEnabled(Node));
        f.PlayerProxy.SetProperty(nameof(IPlayer.SpellManager), null);
        Assert.Throws<InvalidOperationException>(f.Manager.Initialise);
        Assert.Null(f.Manager.GetSpell(GrantBase));
        f.PlayerProxy.SetProperty(nameof(IPlayer.SpellManager), f.Manager);
        f.Manager.Initialise();
        Assert.Null(f.Manager.GetSpell(GrantBase));
        Assert.Empty(f.CastCalls);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(0));
        Assert.NotNull(f.Manager.GetSpell(GrantBase));
        Assert.Single(f.CastCalls);
        f.Manager.Initialise();
        Assert.Single(f.CastCalls);
        Assert.Null(f.Manager.GetActionSet(0).GetShortcut(ShortcutType.SpellbookItem, GrantBase));
    }

    [Fact]
    public void LoadedActiveSpecGrantsAmpGatedSpellAndKeepsSavedShortcut()
    {
        var model = Model(1, (1, Node));
        model.ActionSetShortcut.Add(new() { Id = 42, SpecIndex = 1, Location = (byte)UILocation.LAS2,
            ShortcutType = (byte)ShortcutType.GameCommand, ObjectId = 77 });
        var f = new Fixture(model);
        Assert.True(f.Manager.IsAmpEnabled(Node));
        Assert.NotNull(f.Manager.GetSpell(GrantBase));
        Assert.Equal(77u, f.Manager.GetActionSet(1).GetShortcut(UILocation.LAS2).ObjectId);
        Assert.Equal(ShortcutType.GameCommand, f.Manager.GetActionSet(1).GetShortcut(UILocation.LAS2).ShortcutType);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 11u, 999u, true)]
    [InlineData(PrerequisiteComparison.Equal, 12u, 11u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 11u, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 12u, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 65547u, 11u, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, 11u, 0u, false)]
    public void EnabledPrerequisiteUsesObjectIdActiveSpecAndOnlyEquality(PrerequisiteComparison comparison,
        uint objectId, uint value, bool expected)
    {
        var f = new Fixture(Model(0, (0, Node), (1, SharedNode)));
        var check = new PrerequisiteCheckAmpEnabled();
        Assert.Equal(expected, check.Meets(f.Player, comparison, value, objectId, new PrerequisiteParameters()));
        Assert.Equal(expected, check.Meets(f.Player, comparison, value, objectId,
            new PrerequisiteParameters { Target = f.Player }));
        Assert.False(check.Meets(f.Player, comparison, value, objectId,
            new PrerequisiteParameters { Target = RecordingDispatchProxy<IUnitEntity>.Create(out _) }));
        Assert.False(check.Meets(null, comparison, value, objectId, null));
    }

    [Fact]
    public void RegisteredPrerequisiteDispatchKeepsEnabledAndGlobalAmpListsSeparate()
    {
        var f = new Fixture(Model(0, (0, Node)));
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
        tp.SetProperty(nameof(IGameTableManager.Prerequisite), PlayerBindPointTests.Table(
            new PrerequisiteEntry { Id = 77, Flags = EvaluationMode.EvaluateAND,
                PrerequisiteTypeId = [(PrerequisiteType)227], PrerequisiteComparisonId = [PrerequisiteComparison.Equal],
                ObjectId = [Node], Value = [SharedNode] },
            new PrerequisiteEntry { Id = 78, Flags = EvaluationMode.EvaluateAND,
                PrerequisiteTypeId = [(PrerequisiteType)231], PrerequisiteComparisonId = [PrerequisiteComparison.Equal],
                ObjectId = [Node], Value = [SharedNode] }));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(tables);
        services.AddGamePrerequisite();
        using var provider = services.BuildServiceProvider();
        Assert.IsType<PrerequisiteCheckAmpEnabled>(provider.GetRequiredKeyedService<IPrerequisiteCheck>((PrerequisiteType)227));
        var prerequisites = provider.GetRequiredService<IPrerequisiteManager>();
        Assert.True(prerequisites.Meets(f.Player, 77));
        Assert.False(prerequisites.Meets(f.Player, 78));
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.False(prerequisites.Meets(f.Player, 77));
    }

    [Theory]
    [InlineData(AmpRespecType.Single, 11u)]
    [InlineData(AmpRespecType.Section, 1u)]
    [InlineData(AmpRespecType.Full, 0u)]
    public void ActiveNodeChangesUpdatePrerequisiteAndEffectsBeforeDatabaseSave(AmpRespecType type, uint value)
    {
        var f = new Fixture(Model(0, (0, Node)));
        f.Manager.GetActionSet(0).RemoveAmp(type, value);
        Assert.False(f.Manager.IsAmpEnabled(Node));
        Assert.Equal(1f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        // Respeccing does not erase legitimate learned class spells.
        Assert.NotNull(f.Manager.GetSpell(GrantBase));
        f.Manager.GetActionSet(0).AddAmp(Node);
        Assert.True(f.Manager.IsAmpEnabled(Node));
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        Assert.Equal(2, f.CastCalls.Count);
    }

    [Fact]
    public void InactiveNodeChangesTakeEffectOnlyWhenItsSpecBecomesActive()
    {
        var f = new Fixture(Model(0));
        f.Manager.GetActionSet(1).AddAmp(Node);
        Assert.False(f.Manager.IsAmpEnabled(Node));
        Assert.Null(f.Manager.GetSpell(GrantBase));
        Assert.Empty(f.CastCalls);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(1));
        Assert.True(f.Manager.IsAmpEnabled(Node));
        Assert.NotNull(f.Manager.GetSpell(GrantBase));
        Assert.Single(f.CastCalls);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(0));
        Assert.False(f.Manager.IsAmpEnabled(Node));
        Assert.Equal(1f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
    }

    [Fact]
    public void SharedAugmentIsDeduplicatedAcrossNodesAndSpecSwitches()
    {
        var f = new Fixture(Model(0, (0, Node), (0, SharedNode), (1, Node)));
        f.Manager.Update(1);
        Assert.Single(f.CastCalls);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Single, Node);
        Assert.Single(f.CastCalls);
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(1));
        Assert.Single(f.CastCalls);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(2));
        Assert.Equal(1f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(0));
        Assert.Equal(2, f.CastCalls.Count);
    }

    [Fact]
    public void PreExistingSameSpellPropertyIsNeitherReplacedNorClaimed()
    {
        var f = new Fixture(Model(0, (0, Node)), initialise: false);
        f.ApplyProperty(91);
        f.Manager.Initialise();
        f.Manager.Update(1);
        Assert.Empty(f.CastCalls);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.Equal((Property.MountSpeedMultiplier, 91u), Assert.Single(f.Unit.GetSpellPropertyInstances(Augment)));
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
    }

    [Fact]
    public void ReplacementAndUnrelatedPropertyInstancesSurviveAmpRemoval()
    {
        var f = new Fixture(Model(0, (0, Node)));
        f.ApplyProperty(92); // same spell/effect entry replaces the AMP-owned concrete instance
        f.ApplyProperty(93, spellId: 9999, amount: 0.3f);
        f.Manager.Update(1);
        Assert.Single(f.CastCalls);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.Equal((Property.MountSpeedMultiplier, 92u), Assert.Single(f.Unit.GetSpellPropertyInstances(Augment)));
        Assert.Equal((Property.MountSpeedMultiplier, 93u), Assert.Single(f.Unit.GetSpellPropertyInstances(9999)));
        Assert.Equal(1.5f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
    }

    [Fact]
    public void ReusedConcreteKeyFromAnotherSpellIsNotRemoved()
    {
        var f = new Fixture(Model(0, (0, Node)));
        f.ApplyProperty(1, spellId: 9999, amount: 0.3f);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.Equal((Property.MountSpeedMultiplier, 1u), Assert.Single(f.Unit.GetSpellPropertyInstances(9999)));
        Assert.Equal(1.3f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameSpellReplacementWithReusedEffectAndCastingIdsSurvivesRespec(bool refreshBeforeRemoval)
    {
        var f = new Fixture(Model(0, (0, Node)));
        uint effectId = Assert.Single(f.Unit.GetSpellPropertyInstances(Augment)).EffectId;
        f.ApplyProperty(effectId, amount: 0.3f);
        if (refreshBeforeRemoval) f.Manager.Update(1);

        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);

        Assert.Single(f.CastCalls);
        Assert.Equal((Property.MountSpeedMultiplier, effectId), Assert.Single(f.Unit.GetSpellPropertyInstances(Augment)));
        Assert.Equal(1.3f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeadAndOutOfWorldLoginRetryWithoutDuplicateModifiers(bool outOfWorld)
    {
        var f = new Fixture(Model(0, (0, Node)), initialise: false);
        f.PlayerProxy.SetProperty(outOfWorld ? nameof(IPlayer.InWorld) : nameof(IPlayer.IsAlive), false);
        f.Manager.Initialise();
        f.Manager.Update(1);
        Assert.Empty(f.CastCalls);
        f.PlayerProxy.SetProperty(nameof(IPlayer.InWorld), true);
        f.PlayerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        f.Manager.Update(1);
        f.Manager.SendInitialPackets();
        f.Manager.Update(1);
        Assert.Single(f.CastCalls);
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        var p = (ISpellParameters)f.CastCalls[0].Arguments[1];
        Assert.Equal(42u, p.PrimaryTargetId);
        Assert.True(p.IgnoreGlobalCooldown);
        Assert.False(p.UserInitiatedSpellCast);
        Assert.False(p.CancelActiveTrade);
    }

    [Fact]
    public void FailedCastRetriesAtBoundedIntervalsAndStopsWhenNodeRemoved()
    {
        var f = new Fixture(Model(0, (0, Node)), initialise: false) { Result = CastResult.SpellPreRequisites };
        f.Manager.Initialise();
        f.Manager.Update(0.1);
        f.Manager.Update(0.1);
        Assert.Equal(2, f.CastCalls.Count);
        f.Result = CastResult.Ok;
        f.Manager.Update(1);
        f.Manager.Update(1);
        Assert.Equal(3, f.CastCalls.Count);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        f.Manager.Update(1);
        Assert.Equal(3, f.CastCalls.Count);
        Assert.Empty(f.Unit.GetSpellPropertyInstances(Augment));
    }

    [Theory]
    [InlineData("mixed")]
    [InlineData("proc")]
    [InlineData("duration")]
    [InlineData("cast")]
    [InlineData("channel")]
    [InlineData("delay")]
    [InlineData("tick")]
    [InlineData("effect-duration")]
    [InlineData("persistence")]
    [InlineData("telegraph")]
    [InlineData("stack-group")]
    [InlineData("missing-effects")]
    [InlineData("missing-spell")]
    public void UnsupportedAugmentFamiliesRemainUncastAndUnlearned(string scenario)
    {
        var f = new Fixture(Model(0, (0, Node)), scenario: scenario);
        f.Manager.Update(1);
        Assert.True(f.Manager.IsAmpEnabled(Node));
        Assert.Empty(f.CastCalls);
        Assert.Null(f.Manager.GetSpell(1001));
        Assert.Empty(f.Unit.GetSpellPropertyInstances(Augment));
    }

    [Fact]
    public void NodePersistenceAndRelogRestoreAndRemoveOnlyActualAmpState()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Add(new CharacterModel { Id = 42, Name = "AmpTester" });
        db.SaveChanges();
        var f = new Fixture(Model(0));
        f.Manager.GetActionSet(0).AddAmp(Node);
        f.Manager.GetActionSet(0).AddAmp(300);
        f.Manager.Save(db);
        db.SaveChanges();
        Assert.Equal(new ushort[] { Node, 300 }, db.CharacterActionSetAmp.OrderBy(a => a.AmpId).Select(a => a.AmpId).ToArray());
        Assert.DoesNotContain(db.CharacterSpell, s => s.Spell4BaseId == 1001);
        Assert.Contains(db.CharacterSpell, s => s.Spell4BaseId == GrantBase);
        db.ChangeTracker.Clear();
        var persisted = db.Character.Include(c => c.ActionSetAmp).Include(c => c.Spell).AsNoTracking().Single();
        var relog = new Fixture(persisted);
        Assert.True(relog.Manager.IsAmpEnabled(Node));
        Assert.True(relog.Manager.IsAmpEnabled(300));
        Assert.NotNull(relog.Manager.GetSpell(GrantBase));
        Assert.Equal(1.2f, relog.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        relog.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        relog.Manager.Save(db);
        db.SaveChanges();
        Assert.Empty(db.CharacterActionSetAmp);
        Assert.Contains(db.CharacterSpell, s => s.Spell4BaseId == GrantBase);
        Assert.Empty(db.CharacterEntitlement);
        db.ChangeTracker.Clear();
        var removed = new Fixture(db.Character.Include(c => c.ActionSetAmp).Include(c => c.Spell).AsNoTracking().Single());
        Assert.False(removed.Manager.IsAmpEnabled(Node));
        Assert.Empty(removed.CastCalls);
        Assert.Equal(1f, removed.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
    }

    private static CharacterModel Model(byte active, params (byte Spec, ushort Node)[] nodes)
    {
        var model = new CharacterModel { Id = 42, ActiveSpec = active };
        foreach (var node in nodes) model.ActionSetAmp.Add(new() { Id = 42, SpecIndex = node.Spec, AmpId = node.Node });
        return model;
    }

    private sealed class Fixture
    {
        public IPlayer Player { get; }
        public RecordingDispatchProxy<IPlayer> PlayerProxy { get; }
        public Game.Entity.SpellManager Manager { get; }
        public TestUnit Unit { get; } = new();
        public CastResult Result { get; set; } = CastResult.Ok;
        public IReadOnlyList<RecordingDispatchProxy<IPlayer>.Invocation> CastCalls => PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell));
        private readonly Spell4EffectsEntry property;
        private readonly ISpellInfo augmentInfo;

        public Fixture(CharacterModel model, bool initialise = true, string scenario = "property")
        {
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
            var augmentEntry = new Spell4Entry { Id = Augment, Spell4BaseIdBaseSpell = 1001, TierIndex = 1,
                SpellDuration = scenario == "duration" ? 100u : 0,
                CastTime = scenario == "cast" ? 100u : 0, ChannelMaxTime = scenario == "channel" ? 100u : 0,
                Spell4StackGroupId = scenario == "stack-group" ? 1u : 0 };
            var grantEntry = new Spell4Entry { Id = GrantSpell, Spell4BaseIdBaseSpell = GrantBase, TierIndex = 1 };
            tp.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table<Spell4Entry>(scenario == "missing-spell" ? [grantEntry] : [augmentEntry, grantEntry]));
            tp.SetProperty(nameof(IGameTableManager.SpellLevel), PlayerBindPointTests.Table(new SpellLevelEntry
            { Id = 1, ClassId = (uint)Class.Warrior, CharacterLevel = 1, Spell4Id = GrantSpell, PrerequisiteId = 77 }));
            tp.SetProperty(nameof(IGameTableManager.Class), PlayerBindPointTests.Table(new ClassEntry
            { Id = (uint)Class.Warrior, Spell4IdInnateAbilityActive = [], Spell4IdInnateAbilityPassive = [], Spell4IdAttackPrimary = [], Spell4IdAttackUnarmed = [] }));
            tp.SetProperty(nameof(IGameTableManager.EldanAugmentation), PlayerBindPointTests.Table(
                new EldanAugmentationEntry { Id = Node, Spell4IdAugment = Augment, PowerCost = 1, EldanAugmentationCategoryId = 1 },
                new EldanAugmentationEntry { Id = SharedNode, Spell4IdAugment = Augment, PowerCost = 1, EldanAugmentationCategoryId = 2 },
                new EldanAugmentationEntry { Id = 300, PowerCost = 1, EldanAugmentationCategoryId = 1 }));
            property = new Spell4EffectsEntry { Id = 2, SpellId = Augment, EffectType = SpellEffectType.UnitPropertyModifier,
                DataBits00 = (uint)Property.MountSpeedMultiplier, DataBits03 = BitConverter.SingleToUInt32Bits(0.2f),
                TargetFlags = (uint)(scenario == "telegraph" ? SpellEffectTargetFlags.Telegraph : SpellEffectTargetFlags.Caster),
                DurationTime = scenario == "effect-duration" ? 100u : 0,
                DelayTime = scenario == "delay" ? 100u : 0, TickTime = scenario == "tick" ? 100u : 0,
                PrerequisiteIdCasterPersistence = scenario == "persistence" ? 77u : 0 };
            List<Spell4EffectsEntry> effects = [property];
            if (scenario == "mixed") effects.Add(new() { Id = 3, SpellId = Augment, EffectType = SpellEffectType.Damage });
            if (scenario == "proc") property.EffectType = SpellEffectType.AddSpell;
            if (scenario != "missing-effects") tp.SetProperty(nameof(IGameTableManager.Spell4Effects), PlayerBindPointTests.Table(effects.ToArray()));
            var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var gp);
            augmentInfo = Info(augmentEntry, out var augmentBase);
            var grantInfo = Info(grantEntry, out var grantBase);
            gp.SetMethodHandler(nameof(IGlobalSpellManager.GetSpellBaseInfo), args => (uint)args[0] == GrantBase ? grantBase : augmentBase);
            var inventory = RecordingDispatchProxy<IInventory>.Create(out var inv);
            inv.SetMethodReturnFactory("GetEnumerator", () => ((IEnumerable<IBag>)Array.Empty<IBag>()).GetEnumerator());
            inv.SetMethodReturn(nameof(IInventory.SpellCreate), RecordingDispatchProxy<IItem>.Create(out _));
            Player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            PlayerProxy = pp;
            pp.SetProperty(nameof(IPlayer.Guid), 42u);
            pp.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            pp.SetProperty(nameof(IPlayer.Class), Class.Warrior);
            pp.SetProperty(nameof(IPlayer.Level), 50u);
            pp.SetProperty(nameof(IPlayer.IsLoading), true);
            pp.SetProperty(nameof(IPlayer.IsAlive), true);
            pp.SetProperty(nameof(IPlayer.InWorld), true);
            pp.SetProperty(nameof(IPlayer.Inventory), inventory);
            pp.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
            pp.SetMethodHandler(nameof(IPlayer.GetSpellPropertyInstances), args => Unit.GetSpellPropertyInstances((uint)args[0]));
            pp.SetMethodHandler(nameof(IPlayer.CaptureSpellPropertyInstances), args => Unit.CaptureSpellPropertyInstances((uint)args[0]));
            pp.SetMethodHandler(nameof(IPlayer.RemoveSpellPropertyInstance), args => Unit.RemoveSpellPropertyInstance((ISpellPropertyInstance)args[0]));
            uint nextEffect = 0;
            pp.SetMethodHandler(nameof(IPlayer.TryCastSpell), args =>
            {
                if (Result == CastResult.Ok) ApplyProperty(++nextEffect);
                return Result;
            });
            var prereqs = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var pr);
            pr.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => (uint)args[1] != 77 ||
                new PrerequisiteCheckAmpEnabled().Meets(Player, PrerequisiteComparison.Equal, 0, Node, null));
            Unit.SetBaseProperty(Property.MountSpeedMultiplier, 1);
            Manager = new Game.Entity.SpellManager(Player, model, prereqs, global, tables);
            pp.SetProperty(nameof(IPlayer.SpellManager), Manager);
            if (initialise) Manager.Initialise();
        }

        public void ApplyProperty(uint effectId, uint spellId = Augment, float amount = 0.2f)
        {
            var parameters = new SpellParameters { SpellInfo = augmentInfo };
            ISpellInfo info = augmentInfo;
            if (spellId != Augment) info = Info(new Spell4Entry { Id = spellId }, out _);
            parameters.SpellInfo = info;
            var spell = RecordingDispatchProxy<ISpell>.Create(out var sp);
            sp.SetProperty(nameof(ISpell.Parameters), parameters);
            sp.SetProperty(nameof(ISpell.CastingId), effectId);
            var target = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out var te);
            var entry = new Spell4EffectsEntry { Id = property.Id, SpellId = spellId, EffectType = SpellEffectType.UnitPropertyModifier,
                DataBits00 = (uint)Property.MountSpeedMultiplier, DataBits03 = BitConverter.SingleToUInt32Bits(amount) };
            te.SetProperty(nameof(ISpellTargetEffectInfo.Entry), entry);
            te.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
            Game.Spell.SpellHandler.HandleEffectPropertyModifier(spell, Unit, target);
        }

        private static ISpellInfo Info(Spell4Entry entry, out ISpellBaseInfo baseInfo)
        {
            var info = RecordingDispatchProxy<ISpellInfo>.Create(out var ip);
            ip.SetProperty(nameof(ISpellInfo.Entry), entry);
            ip.SetProperty(nameof(ISpellInfo.PrerequisiteRunners), new List<PrerequisiteEntry>());
            ip.SetProperty(nameof(ISpellInfo.Thresholds), new List<Spell4ThresholdsEntry>());
            baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var bp);
            bp.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = entry.Spell4BaseIdBaseSpell });
            bp.SetProperty(nameof(ISpellBaseInfo.SpellType), new Spell4SpellTypesEntry { Id = 5 });
            bp.SetMethodReturn(nameof(ISpellBaseInfo.GetSpellInfo), info);
            ip.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
            return info;
        }
    }

    private sealed class TestUnit : UnitEntity
    {
        public TestUnit() : base(RecordingDispatchProxy<IMovementManager>.Create(out _)) { }
        public override EntityType Type => EntityType.Simple;
        protected override IEntityModel BuildEntityModel() => new WorldUnitEntityModel();
        protected override float CalculateDefaultProperty(Property property) => 0;
    }
}
