using System.Buffers.Binary;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Abilities;
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

public class AmpTableAugmentationTests
{
    private const ushort PropertyNode = 770, CooldownNode = 881;
    private const uint PropertySpell = 56845, CooldownSpell = 59417, Affected = 70001, Other = 70002;

    // Build16042 raw DTBL records, read directly from the extracted tables. These
    // fixtures are independent of production table/effect writers and interpretation.
    private const string PropertyNodeRow = "0203000001000000020000000500000001000000000000000dde000000000000030000000000000003ed070004ed0700";
    private const string CooldownNodeRow = "71030000050000000600000005000000040000000000000019e80000000000000400000001000000f5fc0700f6fc0700";
    private const string StackGroupRow = "020000000000000004000000";
    private const string PropertyEffectRow = "c11102000dde0000010000000b000000000000000000000000000000000000000200000066000000030000000000803f0ad7233c000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffffffff00000000";
    private const string CooldownEffectRow = "da2b020019e800000100000049000000000000000000000000000000000000000200000003000000b60000000200000000803bc5000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffffffff00000000";

    [Fact]
    public void ShippedRowsEstablishPassiveGroupAndTwoDistinctOwnedFamilies()
    {
        var f = new Fixture(initialise: false);
        Assert.Equal(770u, f.PropertyAmp.Id);
        Assert.Equal(5u, f.PropertyAmp.ClassId);
        Assert.Equal(PropertySpell, f.PropertyAmp.Spell4IdAugment);
        Assert.Equal(881u, f.CooldownAmp.Id);
        Assert.Equal(CooldownSpell, f.CooldownAmp.Spell4IdAugment);
        Assert.Equal(135617u, f.PropertyEffect.Id);
        Assert.Equal((uint)Property.BaseCritChance, f.PropertyEffect.DataBits00);
        Assert.Equal(0.01f, BitConverter.UInt32BitsToSingle(f.PropertyEffect.DataBits03));
        Assert.Equal(142298u, f.CooldownEffect.Id);
        Assert.Equal(3u, f.CooldownEffect.DataBits00);
        Assert.Equal(182u, f.CooldownEffect.DataBits01);
        Assert.Equal(2u, f.CooldownEffect.DataBits02);
        Assert.Equal(-3000f, BitConverter.UInt32BitsToSingle(f.CooldownEffect.DataBits03));
        Assert.Equal(2u, f.Group.Id);
        Assert.Equal(0u, f.Group.StackCap);
        Assert.Equal(4u, f.Group.StackTypeEnum);
        Assert.True(AugmentationSpellRules.IsSupported(PropertySpell, f.Tables));
        Assert.True(AugmentationSpellRules.IsSupported(CooldownSpell, f.Tables));
    }

    [Fact]
    public void LoginAppliesActualPropertyAndHeldCooldownOnceWithoutLearningThem()
    {
        var f = new Fixture();
        f.Manager.Initialise();
        f.Manager.SendInitialPackets();
        f.Manager.Update(1);
        Assert.Equal(2, f.CastCalls.Count);
        Assert.Equal(0.11f, f.Unit.GetPropertyValue(Property.BaseCritChance), 5);
        var record = Assert.Single(f.Manager.GetSpellCooldownModifiers());
        Assert.Equal(CooldownSpell, record.Spell4Id);
        Assert.Equal(3u, record.Scope);
        Assert.Equal(182u, record.Key);
        Assert.Equal(-3000f, record.Magnitude);
        f.Manager.SetSpellCooldown(Affected, 10);
        f.Manager.SetSpellCooldown(Other, 10);
        Assert.Equal(7, f.Manager.GetSpellCooldown(Affected));
        Assert.Equal(10, f.Manager.GetSpellCooldown(Other));
        Assert.Null(f.Manager.GetSpell(36464));
        Assert.Null(f.Manager.GetSpell(38718));
        foreach (var cast in f.CastCalls)
        {
            var parameters = (ISpellParameters)cast.Arguments[1];
            Assert.False(parameters.UserInitiatedSpellCast);
            Assert.True(parameters.IgnoreGlobalCooldown);
            Assert.False(parameters.CancelActiveTrade);
        }
    }

    [Fact]
    public void InactiveSpecDoesNotApplyAndSharedActiveSpellSurvivesSpecSwitch()
    {
        var f = new Fixture(Model(0, (1, CooldownNode), (2, CooldownNode)));
        Assert.Empty(f.CastCalls);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(1));
        var owned = Assert.Single(f.Manager.GetSpellCooldownModifiers());
        Assert.Single(f.CastCalls);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(2));
        Assert.Same(owned, Assert.Single(f.Manager.GetSpellCooldownModifiers()));
        Assert.Single(f.CastCalls);
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(0));
        Assert.Empty(f.Manager.GetSpellCooldownModifiers());
        Assert.Equal(SpecError.Ok, f.Manager.SetActiveActionSet(1));
        Assert.NotSame(owned, Assert.Single(f.Manager.GetSpellCooldownModifiers()));
        Assert.Equal(2, f.CastCalls.Count);
    }

    [Fact]
    public void RespecRemovesOnlyOwnedEffectsAndDoesNotUndoRunningCooldowns()
    {
        var f = new Fixture();
        var unrelated = new SpellCooldownModifier(90, 99999, 800, 3, 183, 2, -1000);
        Assert.True(f.Manager.ApplySpellCooldownModifier(unrelated, true));
        f.Manager.SetSpellCooldown(Affected, 10);
        Assert.Equal(7, f.Manager.GetSpellCooldown(Affected));
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.Same(unrelated, Assert.Single(f.Manager.GetSpellCooldownModifiers()));
        Assert.Empty(f.Unit.GetSpellPropertyInstances(PropertySpell));
        Assert.Equal(0.1f, f.Unit.GetPropertyValue(Property.BaseCritChance), 5);
        Assert.Equal(7, f.Manager.GetSpellCooldown(Affected));
        f.Manager.SetSpellCooldown(Affected, 0);
        f.Manager.SetSpellCooldown(Affected, 10);
        Assert.Equal(10, f.Manager.GetSpellCooldown(Affected));
    }

    [Fact]
    public void ValueEqualReplacementWithReusedEffectAndCastingIdsIsNotOwned()
    {
        var f = new Fixture(Model(0, (0, CooldownNode)));
        var owned = Assert.Single(f.Manager.GetSpellCooldownModifiers());
        Assert.True(f.Manager.RemoveSpellCooldownModifier(owned.EffectId));
        var replacement = owned with { };
        Assert.Equal(owned, replacement);
        Assert.NotSame(owned, replacement);
        Assert.True(f.Manager.ApplySpellCooldownModifier(replacement, true));
        f.Manager.Update(1);
        Assert.Single(f.CastCalls);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.Same(replacement, Assert.Single(f.Manager.GetSpellCooldownModifiers()));
    }

    [Fact]
    public void PreExistingSameSpellModifierIsNotRecastOrClaimed()
    {
        var f = new Fixture(Model(0, (0, CooldownNode)), initialise: false);
        var external = new SpellCooldownModifier(91, CooldownSpell, 801, 3, 182, 2, -3000);
        Assert.True(f.Manager.ApplySpellCooldownModifier(external, true));
        f.Manager.Initialise();
        f.Manager.Update(1);
        Assert.Empty(f.CastCalls);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        Assert.Same(external, Assert.Single(f.Manager.GetSpellCooldownModifiers()));
    }

    [Fact]
    public void RemovedOwnedRecordRetriesAndNodeRemovalStopsRetry()
    {
        var f = new Fixture(Model(0, (0, CooldownNode)));
        var original = Assert.Single(f.Manager.GetSpellCooldownModifiers());
        f.Manager.RemoveSpellCooldownModifier(original.EffectId);
        f.Manager.Update(0.1);
        Assert.Equal(2, f.CastCalls.Count);
        Assert.NotSame(original, Assert.Single(f.Manager.GetSpellCooldownModifiers()));
        f.Manager.Update(0.1);
        Assert.Equal(2, f.CastCalls.Count);
        f.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        f.Manager.Update(2);
        Assert.Empty(f.Manager.GetSpellCooldownModifiers());
        Assert.Equal(2, f.CastCalls.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeadOrOutOfWorldLoginDefersBothFamilies(bool outOfWorld)
    {
        var f = new Fixture(initialise: false);
        f.PlayerProxy.SetProperty(outOfWorld ? nameof(IPlayer.InWorld) : nameof(IPlayer.IsAlive), false);
        f.Manager.Initialise();
        f.Manager.Update(1);
        Assert.Empty(f.CastCalls);
        f.PlayerProxy.SetProperty(nameof(IPlayer.InWorld), true);
        f.PlayerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        f.Manager.Update(1);
        Assert.Equal(2, f.CastCalls.Count);
        Assert.Single(f.Manager.GetSpellCooldownModifiers());
        Assert.Single(f.Unit.GetSpellPropertyInstances(PropertySpell));
    }

    [Fact]
    public void RejectedCooldownCastRetriesWithoutStartingOrOwningState()
    {
        var f = new Fixture(Model(0, (0, CooldownNode)), initialise: false) { CastResult = CastResult.SpellPreRequisites };
        f.Manager.Initialise();
        f.Manager.Update(0.1);
        Assert.Equal(2, f.CastCalls.Count);
        Assert.Empty(f.Manager.GetSpellCooldownModifiers());
        f.CastResult = CastResult.Ok;
        f.Manager.Update(1);
        Assert.Equal(3, f.CastCalls.Count);
        Assert.Single(f.Manager.GetSpellCooldownModifiers());
    }

    // Retail: Engineer Health Regeneration I (Spell4 57392, row 137082) is one VitalModifier row on the caster with tick and delay 2500 ms and effect flag 2;
    // Stalker Stealth Regen (59394) is one Proxy row with tick 1000 ms, "While in Stealth you regenerate 5 health per second".
    [Theory]
    [InlineData(SpellEffectType.VitalModifier, 2500u, 2500u, 2u, 1u, true)]
    [InlineData(SpellEffectType.Proxy, 1000u, 0u, 2u, 1u, true)]
    [InlineData(SpellEffectType.Proxy, 1000u, 0u, 0u, 1u, false)]     // not marked as living with the spell
    [InlineData(SpellEffectType.Proxy, 500u, 0u, 2u, 4u, true)]       // area rows: the aura follows its caster (Medic Danger Zone, Spell4 58912)
    [InlineData(SpellEffectType.Proxy, 500u, 0u, 2u, 8u, false)]      // a target flag outside caster, target and area
    [InlineData(SpellEffectType.Damage, 1000u, 0u, 2u, 1u, false)]
    [InlineData(SpellEffectType.UnitPropertyModifier, 1000u, 0u, 2u, 1u, false)]    // a property modifier does not tick
    public void PeriodicAmpsThatTickOnTheCasterAreOwnedAsARunningSpell(SpellEffectType type, uint tick, uint delay, uint flags, uint targetFlags, bool expected)
    {
        var f = new Fixture(initialise: false);
        f.PropertyEffect.EffectType = type;
        f.PropertyEffect.TickTime = tick;
        f.PropertyEffect.DelayTime = delay;
        f.PropertyEffect.Flags = flags;
        f.PropertyEffect.TargetFlags = targetFlags;

        Assert.Equal(expected, AugmentationSpellRules.IsSupported(PropertySpell, f.Tables));
        Assert.Equal(expected, AugmentationSpellRules.IsPeriodic(PropertySpell, f.Tables));
        Assert.False(AugmentationSpellRules.IsPeriodic(CooldownSpell, f.Tables));
    }

    [Theory]
    [InlineData(1u, 0u, 4u)]
    [InlineData(2u, 1u, 4u)]
    [InlineData(2u, 0u, 0u)]
    [InlineData(3u, 0u, 4u)]
    public void OtherGroupIdentityCapsAndArbitrationRemainUnsupported(uint id, uint cap, uint type)
    {
        var f = new Fixture(initialise: false);
        f.Group.StackCap = cap;
        f.Group.StackTypeEnum = type;
        f.CooldownEntry.Spell4StackGroupId = id;
        f.PropertyEntry.Spell4StackGroupId = id;
        Assert.False(AugmentationSpellRules.IsSupported(CooldownSpell, f.Tables));
        Assert.False(AugmentationSpellRules.IsSupported(PropertySpell, f.Tables));
        f.Manager.Initialise();
        Assert.Empty(f.CastCalls);
    }

    [Theory]
    [InlineData("mixed")]
    [InlineData("duration")]
    [InlineData("delay")]
    [InlineData("tick")]
    [InlineData("persistence")]
    [InlineData("target")]
    [InlineData("one-shot")]
    [InlineData("scope")]
    [InlineData("mode")]
    [InlineData("magnitude")]
    public void BroaderCooldownEffectsRemainUncast(string scenario)
    {
        var f = new Fixture(Model(0, (0, CooldownNode)), initialise: false, scenario: scenario);
        Assert.False(AugmentationSpellRules.IsSupported(CooldownSpell, f.Tables));
        f.Manager.Initialise();
        Assert.Empty(f.CastCalls);
        Assert.Empty(f.Manager.GetSpellCooldownModifiers());
    }

    [Fact]
    public void MissingPassiveGroupTableFailsClosedWhileLegacyGroupZeroStillWorks()
    {
        var f = new Fixture(initialise: false);
        f.TableProxy.SetProperty(nameof(IGameTableManager.Spell4StackGroup), null);
        Assert.False(AugmentationSpellRules.IsSupported(CooldownSpell, f.Tables));
        Assert.False(AugmentationSpellRules.IsSupported(PropertySpell, f.Tables));
        f.PropertyEntry.Spell4StackGroupId = 0;
        Assert.True(AugmentationSpellRules.IsSupported(PropertySpell, f.Tables));
        f.Manager.Initialise();
        Assert.Single(f.CastCalls);
        Assert.Empty(f.Manager.GetSpellCooldownModifiers());
    }

    [Fact]
    public void ActualNodeSaveAndRelogRebuildEffectsWithoutLearnedSpellsOrEntitlements()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Add(new CharacterModel { Id = 42, Name = "ActualAmpTester" });
        db.SaveChanges();
        var f = new Fixture(Model(0));
        f.Manager.GetActionSet(0).AddAmp(PropertyNode);
        f.Manager.GetActionSet(0).AddAmp(CooldownNode);
        f.Manager.Save(db);
        db.SaveChanges();
        Assert.Equal(new ushort[] { PropertyNode, CooldownNode }, db.CharacterActionSetAmp.OrderBy(a => a.AmpId).Select(a => a.AmpId).ToArray());
        Assert.Empty(db.CharacterSpell);
        Assert.Empty(db.CharacterEntitlement);
        db.ChangeTracker.Clear();
        var saved = db.Character.Include(c => c.ActionSetAmp).AsNoTracking().Single();
        var relog = new Fixture(saved);
        Assert.Equal(2, relog.CastCalls.Count);
        Assert.Single(relog.Manager.GetSpellCooldownModifiers());
        Assert.Single(relog.Unit.GetSpellPropertyInstances(PropertySpell));
        relog.Manager.GetActionSet(0).RemoveAmp(AmpRespecType.Full, 0);
        relog.Manager.Save(db);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var removed = new Fixture(db.Character.Include(c => c.ActionSetAmp).AsNoTracking().Single());
        Assert.Empty(removed.CastCalls);
        Assert.Empty(removed.Manager.GetSpellCooldownModifiers());
    }

    private static CharacterModel Model(byte active, params (byte Spec, ushort Node)[] nodes)
    {
        var model = new CharacterModel { Id = 42, ActiveSpec = active };
        foreach (var node in nodes) model.ActionSetAmp.Add(new() { Id = 42, SpecIndex = node.Spec, AmpId = node.Node });
        return model;
    }

    private sealed class Fixture
    {
        public Game.Entity.SpellManager Manager { get; }
        public IPlayer Player { get; }
        public RecordingDispatchProxy<IPlayer> PlayerProxy { get; }
        public IGameTableManager Tables { get; }
        public RecordingDispatchProxy<IGameTableManager> TableProxy { get; }
        public EldanAugmentationEntry PropertyAmp { get; } = Node(PropertyNodeRow);
        public EldanAugmentationEntry CooldownAmp { get; } = Node(CooldownNodeRow);
        public Spell4EffectsEntry PropertyEffect { get; } = Effect(PropertyEffectRow);
        public Spell4EffectsEntry CooldownEffect { get; } = Effect(CooldownEffectRow);
        public Spell4Entry PropertyEntry { get; } = new() { Id = PropertySpell, Spell4BaseIdBaseSpell = 36464, TierIndex = 1, Spell4StackGroupId = 2, PropertyFlags = 1024 };
        public Spell4Entry CooldownEntry { get; } = new() { Id = CooldownSpell, Spell4BaseIdBaseSpell = 38718, TierIndex = 1, Spell4StackGroupId = 2, PropertyFlags = 1024 };
        public Spell4StackGroupEntry Group { get; } = new() { Id = Word(StackGroupRow, 0), StackCap = Word(StackGroupRow, 1), StackTypeEnum = Word(StackGroupRow, 2) };
        public TestUnit Unit { get; } = new();
        public CastResult CastResult { get; set; } = CastResult.Ok;
        public IReadOnlyList<RecordingDispatchProxy<IPlayer>.Invocation> CastCalls => PlayerProxy.GetInvocations(nameof(IPlayer.TryCastSpell));

        public Fixture(CharacterModel model = null, bool initialise = true, string scenario = null)
        {
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
            TableProxy = tp;
            switch (scenario)
            {
                case "duration": CooldownEffect.DurationTime = 100; break;
                case "delay": CooldownEffect.DelayTime = 100; break;
                case "tick": CooldownEffect.TickTime = 100; break;
                case "persistence": CooldownEffect.PrerequisiteIdCasterPersistence = 77; break;
                case "target": CooldownEffect.TargetFlags = (uint)SpellEffectTargetFlags.Target; break;
                case "one-shot": CooldownEffect.Flags = 0; break;
                case "scope": CooldownEffect.DataBits00 = 4; break;
                case "mode": CooldownEffect.DataBits02 = 4; break;
                case "magnitude": CooldownEffect.DataBits03 = BitConverter.SingleToUInt32Bits(float.NaN); break;
            }
            List<Spell4EffectsEntry> effects = [PropertyEffect, CooldownEffect];
            if (scenario == "mixed") effects.Add(new() { Id = 1, SpellId = CooldownSpell, EffectType = SpellEffectType.UnitPropertyModifier });
            tp.SetProperty(nameof(IGameTableManager.Spell4Effects), PlayerBindPointTests.Table(effects.ToArray()));
            tp.SetProperty(nameof(IGameTableManager.Spell4StackGroup), PlayerBindPointTests.Table(Group));
            tp.SetProperty(nameof(IGameTableManager.EldanAugmentation), PlayerBindPointTests.Table(PropertyAmp, CooldownAmp));
            tp.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(PropertyEntry, CooldownEntry,
                new Spell4Entry { Id = Affected, SpellCoolDownId00 = 182 }, new Spell4Entry { Id = Other, SpellCoolDownId00 = 183 }));
            tp.SetProperty(nameof(IGameTableManager.SpellCoolDown), PlayerBindPointTests.Table(
                new SpellCoolDownEntry { Id = 182, CooldownTime = 10000 }, new SpellCoolDownEntry { Id = 183, CooldownTime = 10000 }));
            tp.SetProperty(nameof(IGameTableManager.SpellLevel), PlayerBindPointTests.Table<SpellLevelEntry>());
            tp.SetProperty(nameof(IGameTableManager.Class), PlayerBindPointTests.Table(new ClassEntry
            { Id = (uint)Class.Stalker, Spell4IdInnateAbilityActive = [], Spell4IdInnateAbilityPassive = [], Spell4IdAttackPrimary = [], Spell4IdAttackUnarmed = [] }));
            var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out _);
            var prereqs = RecordingDispatchProxy<IPrerequisiteManager>.Create(out _);
            var inventory = RecordingDispatchProxy<IInventory>.Create(out var inv);
            inv.SetMethodReturnFactory("GetEnumerator", () => ((IEnumerable<IBag>)Array.Empty<IBag>()).GetEnumerator());
            Player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            PlayerProxy = pp;
            pp.SetProperty(nameof(IPlayer.Guid), 42u);
            pp.SetProperty(nameof(IPlayer.CharacterId), 42ul);
            pp.SetProperty(nameof(IPlayer.Class), Class.Stalker);
            pp.SetProperty(nameof(IPlayer.Level), 50u);
            pp.SetProperty(nameof(IPlayer.IsLoading), true);
            pp.SetProperty(nameof(IPlayer.IsAlive), true);
            pp.SetProperty(nameof(IPlayer.InWorld), true);
            pp.SetProperty(nameof(IPlayer.Inventory), inventory);
            pp.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
            pp.SetMethodHandler(nameof(IPlayer.GetSpellPropertyInstances), args => Unit.GetSpellPropertyInstances((uint)args[0]));
            pp.SetMethodHandler(nameof(IPlayer.CaptureSpellPropertyInstances), args => Unit.CaptureSpellPropertyInstances((uint)args[0]));
            pp.SetMethodHandler(nameof(IPlayer.RemoveSpellPropertyInstance), args => Unit.RemoveSpellPropertyInstance((ISpellPropertyInstance)args[0]));
            Unit.SetBaseProperty(Property.BaseCritChance, 0.1f);
            uint nextEffect = 0;
            pp.SetMethodHandler(nameof(IPlayer.TryCastSpell), args =>
            {
                if (CastResult != CastResult.Ok) return CastResult;
                uint id = (uint)args[0];
                var parameters = (ISpellParameters)args[1];
                var info = RecordingDispatchProxy<ISpellInfo>.Create(out var ip);
                ip.SetProperty(nameof(ISpellInfo.Entry), id == CooldownSpell ? CooldownEntry : PropertyEntry);
                ip.SetProperty(nameof(ISpellInfo.StackGroup), Group);
                parameters.SpellInfo = info;
                var spell = RecordingDispatchProxy<ISpell>.Create(out var sp);
                sp.SetProperty(nameof(ISpell.Parameters), parameters);
                sp.SetProperty(nameof(ISpell.Caster), Player);
                sp.SetProperty(nameof(ISpell.CastingId), ++nextEffect);
                var effect = new SpellTargetInfo.SpellTargetEffectInfo(nextEffect, id == CooldownSpell ? CooldownEffect : PropertyEffect);
                if (id == CooldownSpell) Game.Spell.SpellHandler.HandleEffectModifySpellCooldown(spell, Player, effect);
                else Game.Spell.SpellHandler.HandleEffectPropertyModifier(spell, Unit, effect);
                return CastResult.Ok;
            });
            Manager = new Game.Entity.SpellManager(Player, model ?? Model(0, (0, PropertyNode), (0, CooldownNode)), prereqs, global, Tables);
            pp.SetProperty(nameof(IPlayer.SpellManager), Manager);
            if (initialise) Manager.Initialise();
        }
    }

    private static uint Word(string hex, int index) => BinaryPrimitives.ReadUInt32LittleEndian(Convert.FromHexString(hex).AsSpan(index * 4));

    private static EldanAugmentationEntry Node(string hex) => new()
    {
        Id = Word(hex, 0), DisplayRow = Word(hex, 1), DisplayColumn = Word(hex, 2), ClassId = Word(hex, 3),
        PowerCost = Word(hex, 4), EldanAugmentationIdRequired = Word(hex, 5), Spell4IdAugment = Word(hex, 6),
        Item2IdUnlock = Word(hex, 7), EldanAugmentationCategoryId = Word(hex, 8), CategoryTier = Word(hex, 9),
        LocalizedTextIdTitle = Word(hex, 10), LocalizedTextIdTooltip = Word(hex, 11)
    };

    private static Spell4EffectsEntry Effect(string hex)
    {
        Assert.Equal(168, Convert.FromHexString(hex).Length);
        return new()
        {
            Id = Word(hex, 0), SpellId = Word(hex, 1), TargetFlags = Word(hex, 2), EffectType = (SpellEffectType)Word(hex, 3),
            DamageType = (DamageType)Word(hex, 4), DelayTime = Word(hex, 5), TickTime = Word(hex, 6), DurationTime = Word(hex, 7),
            Flags = Word(hex, 8), DataBits00 = Word(hex, 9), DataBits01 = Word(hex, 10), DataBits02 = Word(hex, 11),
            DataBits03 = Word(hex, 12), DataBits04 = Word(hex, 13), DataBits05 = Word(hex, 14), DataBits06 = Word(hex, 15),
            DataBits07 = Word(hex, 16), DataBits08 = Word(hex, 17), DataBits09 = Word(hex, 18),
            PrerequisiteIdCasterPersistence = Word(hex, 29), PrerequisiteIdTargetPersistence = Word(hex, 30),
            PrerequisiteIdTargetSuspend = Word(hex, 31), ParameterType = [], ParameterValue = [],
            PhaseFlags = Word(hex, 40), OrderIndex = Word(hex, 41)
        };
    }

    public sealed class TestUnit : UnitEntity
    {
        public TestUnit() : base(RecordingDispatchProxy<IMovementManager>.Create(out _)) { }
        public override EntityType Type => EntityType.Simple;
        protected override IEntityModel BuildEntityModel() => new WorldUnitEntityModel();
        protected override float CalculateDefaultProperty(Property property) => 0;
    }
}
