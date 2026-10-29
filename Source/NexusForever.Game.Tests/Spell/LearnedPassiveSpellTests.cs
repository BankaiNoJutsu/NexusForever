using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
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

public class LearnedPassiveSpellTests
{
    private const uint SpellId = 2001, BaseId = 1001;

    [Theory]
    [InlineData("permanent", true)]
    [InlineData("untaught", false)]
    [InlineData("mixed", false)]
    [InlineData("empty", false)]
    [InlineData("timed", false)]
    [InlineData("delayed", false)]
    [InlineData("periodic", false)]
    [InlineData("spell-duration", false)]
    [InlineData("missing-effects", false)]
    [InlineData("missing-spell", false)]
    public void ClassificationOnlyReplaysPermanentTaughtPropertySpells(string scenario, bool expected)
    {
        var f = new Fixture(scenario);
        Assert.Equal(expected, LearnedPassiveSpellRules.IsLearnedPassive(SpellId, f.Tables));
    }

    [Fact]
    public void MissingAndDifferentTableLoadsDoNotPoisonClassification()
    {
        var missing = new Fixture("missing-effects");
        Assert.False(LearnedPassiveSpellRules.IsLearnedPassive(SpellId, missing.Tables));
        var valid = new Fixture();
        Assert.True(LearnedPassiveSpellRules.IsLearnedPassive(SpellId, valid.Tables));
        var untaught = new Fixture("untaught");
        Assert.False(LearnedPassiveSpellRules.IsLearnedPassive(SpellId, untaught.Tables));
        missing.TableProxy.SetProperty(nameof(IGameTableManager.Spell4Effects), valid.Tables.Spell4Effects);
        Assert.True(LearnedPassiveSpellRules.IsLearnedPassive(SpellId, missing.Tables));
    }

    [Fact]
    public void WorldEntryAndReloadRestoreSpeedWithoutStackingOrLosingEffectOwnership()
    {
        var f = new Fixture();
        f.Manager.SendInitialPackets();
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        f.Manager.SendInitialPackets();
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        Assert.False(f.Unit.RemoveSpellProperty(Property.MountSpeedMultiplier, 1));
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);

        var reloaded = new Fixture();
        reloaded.Manager.SendInitialPackets();
        Assert.Equal(1.2f, reloaded.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        Assert.True(reloaded.Unit.RemoveSpellProperty(Property.MountSpeedMultiplier, 1));
        Assert.Equal(1f, reloaded.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
    }

    [Theory]
    [InlineData("untaught")]
    [InlineData("mixed")]
    [InlineData("timed")]
    [InlineData("inactive")]
    [InlineData("unknown")]
    public void ReplayRejectsUnsupportedOrUnownedSpells(string scenario)
    {
        var f = new Fixture(scenario);
        Assert.False(f.Manager.TryApplyLearnedPassive(scenario == "unknown" ? 999u : SpellId));
        Assert.Empty(f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
    }

    [Fact]
    public void PassiveUsesSelfTargetAndExplicitCooldownBypass()
    {
        var f = new Fixture();
        Assert.True(f.Manager.TryApplyLearnedPassive(SpellId));
        var call = Assert.Single(f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        var parameters = Assert.IsAssignableFrom<ISpellParameters>(call.Arguments[1]);
        Assert.Equal(42u, parameters.PrimaryTargetId);
        Assert.False(parameters.UserInitiatedSpellCast);
        Assert.True(parameters.IgnoreGlobalCooldown);
        Assert.False(parameters.CancelActiveTrade);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeadOrOutOfWorldLoginRetriesAfterRecovery(bool outOfWorld)
    {
        var f = new Fixture();
        f.PlayerProxy.SetProperty(outOfWorld ? nameof(IPlayer.InWorld) : nameof(IPlayer.IsAlive), false);
        Assert.False(f.Manager.TryApplyLearnedPassive(SpellId));
        f.Manager.Update(1);
        Assert.Empty(f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        f.PlayerProxy.SetProperty(nameof(IPlayer.InWorld), true);
        f.PlayerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        f.Manager.Update(1);
        Assert.Single(f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
    }

    [Fact]
    public void BlockedPassiveRetriesAtBoundedIntervalsAndStopsAfterSuccess()
    {
        var f = new Fixture();
        f.Result = CastResult.SpellPreRequisites;
        Assert.False(f.Manager.TryApplyLearnedPassive(SpellId));
        f.Manager.Update(0.1);
        f.Manager.Update(0.1);
        Assert.Equal(2, f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)).Count);
        f.Result = CastResult.Ok;
        f.Manager.Update(1);
        f.Manager.Update(5);
        Assert.Equal(3, f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)).Count);
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
    }

    [Fact]
    public void DeactivationRemovesPassiveAndReactivationRestoresIt()
    {
        var f = new Fixture();
        f.Manager.SendInitialPackets();
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        Assert.True(f.Manager.SetSpellActivation(SpellId, false));
        Assert.Equal(1f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        f.Manager.Update(1);
        Assert.Single(f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.True(f.Manager.SetSpellActivation(SpellId, true));
        Assert.Equal(1.2f, f.Unit.GetPropertyValue(Property.MountSpeedMultiplier), 3);
        Assert.Equal(2, f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)).Count);
    }

    [Fact]
    public void DeactivationCancelsBlockedPassiveRetry()
    {
        var f = new Fixture();
        f.Result = CastResult.SpellPreRequisites;
        Assert.False(f.Manager.TryApplyLearnedPassive(SpellId));
        Assert.True(f.Manager.SetSpellActivation(SpellId, false));
        f.Manager.Update(5);
        Assert.Single(f.PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
    }

    [Fact]
    public void LoadedSpellRemainsLoadedAndNewSpellPersistsItsActualTier()
    {
        var f = new Fixture();
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Add(new CharacterModel { Id = 42, Name = "Rider" });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        f.Manager.GetSpell(BaseId).Save(db);
        Assert.Empty(db.ChangeTracker.Entries<CharacterSpellModel>());
        var fresh = new Fixture(loadSpell: false);
        fresh.Manager.AddSpell(BaseId, 2);
        fresh.Manager.GetSpell(BaseId).Save(db);
        db.SaveChanges();
        Assert.Equal((byte)2, db.CharacterSpell.Single().Tier);
        Assert.Empty(db.CharacterEntitlement);
    }

    private sealed class Fixture
    {
        public IGameTableManager Tables { get; }
        public RecordingDispatchProxy<IGameTableManager> TableProxy { get; }
        public RecordingDispatchProxy<IPlayer> PlayerProxy { get; }
        public Game.Entity.SpellManager Manager { get; }
        public TestUnit Unit { get; } = new();
        public CastResult Result { get; set; } = CastResult.Ok;

        public Fixture(string scenario = "permanent", bool loadSpell = true)
        {
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
            TableProxy = tp;
            var entry = new Spell4Entry { Id = SpellId, Spell4BaseIdBaseSpell = BaseId, TierIndex = 1,
                SpellDuration = scenario == "spell-duration" ? 1000u : 0u };
            if (scenario != "missing-spell") tp.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(entry));
            tp.SetProperty(nameof(IGameTableManager.SpellLevel), PlayerBindPointTests.Table<SpellLevelEntry>());
            tp.SetProperty(nameof(IGameTableManager.Class), PlayerBindPointTests.Table(new ClassEntry
            {
                Id = (uint)Class.Warrior, Spell4IdInnateAbilityActive = [], Spell4IdInnateAbilityPassive = [],
                Spell4IdAttackPrimary = [], Spell4IdAttackUnarmed = []
            }));
            var property = new Spell4EffectsEntry
            {
                Id = 2, SpellId = SpellId, EffectType = SpellEffectType.UnitPropertyModifier,
                DataBits00 = (uint)Property.MountSpeedMultiplier, DataBits03 = BitConverter.SingleToUInt32Bits(0.2f),
                DurationTime = scenario == "timed" ? 1000u : 0u,
                DelayTime = scenario == "delayed" ? 100u : 0u,
                TickTime = scenario == "periodic" ? 100u : 0u
            };
            List<Spell4EffectsEntry> effects = [];
            if (scenario != "untaught") effects.Add(new() { Id = 1, SpellId = 999, EffectType = SpellEffectType.AddSpell, DataBits00 = SpellId });
            if (scenario != "empty") effects.Add(property);
            if (scenario == "mixed") effects.Add(new() { Id = 3, SpellId = SpellId, EffectType = SpellEffectType.Damage });
            if (scenario != "missing-effects") tp.SetProperty(nameof(IGameTableManager.Spell4Effects), PlayerBindPointTests.Table(effects.ToArray()));

            var info = RecordingDispatchProxy<ISpellInfo>.Create(out var ip);
            ip.SetProperty(nameof(ISpellInfo.Entry), entry);
            ip.SetProperty(nameof(ISpellInfo.PrerequisiteRunners), new List<PrerequisiteEntry>());
            ip.SetProperty(nameof(ISpellInfo.Thresholds), new List<Spell4ThresholdsEntry>());
            var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var bp);
            bp.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = BaseId });
            bp.SetProperty(nameof(ISpellBaseInfo.SpellType), new Spell4SpellTypesEntry { Id = 5 });
            bp.SetMethodReturn(nameof(ISpellBaseInfo.GetSpellInfo), info);
            ip.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
            var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var gp);
            gp.SetMethodReturn(nameof(IGlobalSpellManager.GetSpellBaseInfo), baseInfo);
            var inventory = RecordingDispatchProxy<IInventory>.Create(out var inv);
            inv.SetMethodReturnFactory("GetEnumerator", () => ((IEnumerable<IBag>)Array.Empty<IBag>()).GetEnumerator());
            inv.SetMethodReturn(nameof(IInventory.SpellCreate), RecordingDispatchProxy<IItem>.Create(out _));
            var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            PlayerProxy = pp;
            pp.SetProperty(nameof(IPlayer.Guid), 42u);
            pp.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            pp.SetProperty(nameof(IPlayer.Class), Class.Warrior);
            pp.SetProperty(nameof(IPlayer.IsLoading), true);
            pp.SetProperty(nameof(IPlayer.InWorld), true);
            pp.SetProperty(nameof(IPlayer.IsAlive), true);
            pp.SetProperty(nameof(IPlayer.Inventory), inventory);
            pp.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
            uint effectId = 0;
            pp.SetMethodHandler(nameof(IPlayer.RemoveSpellProperties), args => Unit.RemoveSpellProperties((uint)args[0]));
            Unit.SetBaseProperty(Property.MountSpeedMultiplier, 1);
            pp.SetMethodHandler(nameof(IPlayer.TryCastSpell), args =>
            {
                if (Result != CastResult.Ok) return Result;
                var parameters = (ISpellParameters)args[1];
                parameters.SpellInfo = info;
                var spell = RecordingDispatchProxy<ISpell>.Create(out var sp);
                sp.SetProperty(nameof(ISpell.Parameters), parameters);
                sp.SetProperty(nameof(ISpell.CastingId), ++effectId);
                var targetInfo = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out var tip);
                tip.SetProperty(nameof(ISpellTargetEffectInfo.Entry), property);
                tip.SetProperty(nameof(ISpellTargetEffectInfo.EffectId), effectId);
                Game.Spell.SpellHandler.HandleEffectPropertyModifier(spell, Unit, targetInfo);
                return CastResult.Ok;
            });
            var model = new CharacterModel { Id = 42 };
            if (loadSpell) model.Spell.Add(new CharacterSpellModel { Id = 42, Spell4BaseId = BaseId, Tier = (byte)(scenario == "inactive" ? 0 : 1) });
            Manager = new Game.Entity.SpellManager(player, model, RecordingDispatchProxy<IPrerequisiteManager>.Create(out _), global, Tables);
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
