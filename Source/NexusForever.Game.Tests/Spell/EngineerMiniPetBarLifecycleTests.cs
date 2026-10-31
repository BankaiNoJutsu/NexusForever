using System.Reflection;
using NexusForever.Database.Character.Model;
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
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.Pet;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Model.Pet;
using Handler = NexusForever.Game.Spell.SpellHandler;

namespace NexusForever.Game.Tests.Spell;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class EngineerMiniPetBarLifecycleTests
{
    [Fact]
    public void ConcreteSpawnPublishesMiniBindingBeforePetEventAndPreservesPrimaryAllBots()
    {
        var f = new PetMiniBarTestFixture(); var pet = f.Pet();
        Track(pet); Handler.OnEngineerCombatBotSummoned(f.Owner, pet);
        Assert.Equal(7, f.Packets.Count);
        Assert.Equal(0u, Assert.IsType<ServerPetSpawned>(f.Packets[0]).PetUnitId);
        var mini = Assert.IsType<ServerActionBarSet>(f.Packets[1]);
        Assert.Equal(ShortcutSet.PetMiniBar0, mini.ShortcutSet); Assert.Equal(499, mini.ActionBarShortcutSetId);
        Assert.Equal(pet.Guid, mini.AssociatedUnitId);
        Assert.Equal(pet.Guid, Assert.IsType<ServerPetSpawned>(f.Packets[2]).PetUnitId);
        Assert.Equal(3, f.Packets.Skip(3).Take(3).OfType<ServerSpellUpdate>().Count());
        var primary = Assert.IsType<ServerActionBarSet>(f.Packets[6]);
        Assert.Equal(ShortcutSet.PrimaryPetBar, primary.ShortcutSet);
        Assert.Equal(299, primary.ActionBarShortcutSetId); Assert.Equal(0u, primary.AssociatedUnitId);
        Handler.OnEngineerCombatBotSummoned(f.Owner, pet); Assert.Equal(7, f.Packets.Count);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NaturalOrDismissedRemovalClearsOnceBeforeDespawnAndPreservesOtherBot(bool dismissed)
    {
        var f = new PetMiniBarTestFixture(); var pet = f.Pet(); var other = f.Pet(101);
        Assert.True(f.Manager.TryBind(pet)); Assert.True(f.Manager.TryBind(other));
        f.Packets.Clear();
        if (dismissed) Assert.True(f.Manager.TryDismiss(ShortcutSet.PetMiniBar0, 0));
        else f.Factory.UntrackSummon(pet);
        Handler.UnregisterEngineerCombatBotActions(f.Owner, pet);
        Assert.Equal(2, f.Packets.Count);
        var clear = Assert.IsType<ServerActionBarSet>(f.Packets[0]);
        Assert.Equal(0, clear.ActionBarShortcutSetId); Assert.Equal(ShortcutSet.PetMiniBar0, clear.ShortcutSet);
        Assert.Equal(pet.Guid, Assert.IsType<ServerPetDespawned>(f.Packets[1]).PetUnitId);
        Assert.Same(other, Assert.Single(f.Factory.GetSummonCreatures(other.CreatureId)));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void CancellationDuringSpawnStopsLaterPetPackets(int cancelAt)
    {
        var f = new PetMiniBarTestFixture(); var pet = f.Pet(); Track(pet);
        f.PacketHook = _ => { if (f.Packets.Count == cancelAt + 1) { f.AdmissionCurrent = false; f.Manager.Clear(false); } };
        Handler.OnEngineerCombatBotSummoned(f.Owner, pet);
        Assert.Equal(cancelAt + 1, f.Packets.Count);
        Assert.False(f.Manager.TryDismiss(ShortcutSet.PetMiniBar0, 0));
    }

    [Theory]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void RecoveryGenerationDuringPrimaryPublicationStopsOldCallback(int transferAt)
    {
        var f = new PetMiniBarTestFixture(); var pet = f.Pet(); Track(pet);
        f.PacketHook = _ => { if (f.Packets.Count == transferAt + 1) f.Generation++; };
        Handler.OnEngineerCombatBotSummoned(f.Owner, pet);
        Assert.Equal(transferAt + 1, f.Packets.Count);
        Assert.False(f.Manager.TryDismiss(ShortcutSet.PetMiniBar0, 0));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void CancellationDuringLastBotRemovalStopsInternalPrimaryPackets(int cancelAt)
    {
        var f = new PetMiniBarTestFixture(); var pet = f.Pet();
        Assert.True(f.Manager.TryBind(pet)); f.Factory.UntrackSummon(pet); f.Packets.Clear();
        f.PacketHook = _ => { if (f.Packets.Count == cancelAt + 1) { f.AdmissionCurrent = false; f.Manager.Clear(false); } };
        Handler.UnregisterEngineerCombatBotActions(f.Owner, pet);
        Assert.Equal(cancelAt + 1, f.Packets.Count);
    }

    [Theory]
    [InlineData(0, false)] [InlineData(0, true)]
    [InlineData(1, false)] [InlineData(1, true)]
    public void DepartingSelectorsAreCleanedBeforeFirstOutboundEvenWhenEntryChanges(int stopAt, bool recovery)
    {
        var (f, spells, resolver) = SeedDeparture(); using var scope = resolver;
        f.PacketHook = packet =>
        {
            Assert.False(spells.TryResolveActivePetActionSpell(51365, out _));
            Assert.False(spells.TryResolveActivePetActionSpell(35123, out _));
            Assert.True(spells.TryResolveActivePetActionSpell(77777, out uint unrelated)); Assert.Equal(88888u, unrelated);
            if (f.Packets.Count == stopAt + 1)
            {
                if (recovery) f.Generation++; else f.AdmissionCurrent = false;
            }
        };
        Handler.UnregisterEngineerCombatBotActions(f.Owner, (IWorldEntity)f.Entities[100]);
        Assert.Equal(stopAt + 1, f.Packets.Count);
        Assert.False(spells.TryResolveActivePetActionSpell(51365, out _));
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void NonnullActionSetTailStopsAfterFamilyUpdateOrCacheClear(bool stopAtCache, bool recovery)
    {
        var (f, spells, resolver) = SeedDeparture(); using var scope = resolver;
        Assert.NotNull(spells.GetActionSet(0));
        f.PacketHook = packet =>
        {
            bool stop = stopAtCache ? packet is ServerActionSetClearCache
                : packet is ServerSpellUpdate { Spell4BaseId: 20884, Activated: false };
            if (stop) { if (recovery) f.Generation++; else f.AdmissionCurrent = false; }
        };
        Handler.UnregisterEngineerCombatBotActions(f.Owner, (IWorldEntity)f.Entities[100]);
        Assert.Equal(stopAtCache ? 9 : 8, f.Packets.Count);
        Assert.DoesNotContain(f.Packets, packet => packet is ServerActionSet);
        if (!stopAtCache) Assert.DoesNotContain(f.Packets, packet => packet is ServerActionSetClearCache);
        Assert.False(spells.TryResolveActivePetActionSpell(51365, out _));
        Assert.True(spells.TryResolveActivePetActionSpell(77777, out _));
    }

    [Fact]
    public void ValidLastBotDepartureRetainsWholePacketOrderAndRestoresNonnullActionSet()
    {
        var (f, spells, resolver) = SeedDeparture(); using var scope = resolver;
        Handler.UnregisterEngineerCombatBotActions(f.Owner, (IWorldEntity)f.Entities[100]);
        Assert.Equal(10, f.Packets.Count);
        Assert.Equal(ShortcutSet.PetMiniBar0, Assert.IsType<ServerActionBarSet>(f.Packets[0]).ShortcutSet);
        Assert.Equal(100u, Assert.IsType<ServerPetDespawned>(f.Packets[1]).PetUnitId);
        Assert.Equal(ShortcutSet.PrimaryPetBar, Assert.IsType<ServerActionBarSet>(f.Packets[2]).ShortcutSet);
        Assert.All(f.Packets.Skip(3).Take(3), packet => Assert.False(Assert.IsType<ServerSpellUpdate>(packet).Activated));
        Assert.Equal(0u, Assert.IsType<ServerPetDespawned>(f.Packets[6]).PetUnitId);
        Assert.Equal(20884u, Assert.IsType<ServerSpellUpdate>(f.Packets[7]).Spell4BaseId);
        Assert.IsType<ServerActionSetClearCache>(f.Packets[8]); Assert.IsType<ServerActionSet>(f.Packets[9]);
        Assert.False(spells.TryResolveActivePetActionSpell(51365, out _));
        Assert.True(spells.TryResolveActivePetActionSpell(77777, out _));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReentrantNewBotKeepsNewSelectorsAndPublication(bool duringCache)
    {
        var (f, spells, resolver) = SeedDeparture(); using var scope = resolver;
        f.PacketHook = packet =>
        {
            if (duringCache ? packet is not ServerActionSetClearCache
                : packet is not ServerActionBarSet { ShortcutSet: ShortcutSet.PetMiniBar0, ActionBarShortcutSetId: 0 }) return;
            f.PacketHook = null;
            Assert.False(spells.TryResolveActivePetActionSpell(51365, out _));
            var replacement = f.Pet(101);
            spells.SetActivePetActionSpell(51365, 35123, 0);
            Track(replacement); Handler.OnEngineerCombatBotSummoned(f.Owner, replacement);
        };
        Handler.UnregisterEngineerCombatBotActions(f.Owner, (IWorldEntity)f.Entities[100]);
        Assert.True(spells.TryResolveActivePetActionSpell(51365, out uint action)); Assert.Equal(35123u, action);
        Assert.True(spells.TryResolveActivePetActionSpell(77777, out _));
        Assert.Equal(299, f.Bars.Last(bar => bar.ShortcutSet == ShortcutSet.PrimaryPetBar).ActionBarShortcutSetId);
        Assert.DoesNotContain(f.Packets, packet => packet is ServerActionSet);
        if (!duringCache)
        {
            Assert.DoesNotContain(f.Packets, packet => packet is ServerActionBarSet { ShortcutSet: ShortcutSet.PrimaryPetBar, ActionBarShortcutSetId: 0 });
            Assert.DoesNotContain(f.Packets, packet => packet is ServerPetDespawned { PetUnitId: 0 });
            Assert.DoesNotContain(f.Packets, packet => packet is ServerSpellUpdate { Activated: false });
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NewDifferentFamilyBotKeepsItsActualProjectedShortcut(bool duringCache)
    {
        var (f, spells, resolver) = SeedDeparture(projectedShortcuts: true); using var scope = resolver;
        f.PacketHook = packet =>
        {
            if (duringCache ? packet is not ServerActionSetClearCache
                : packet is not ServerActionBarSet { ShortcutSet: ShortcutSet.PetMiniBar0, ActionBarShortcutSetId: 0 }) return;
            f.PacketHook = null;
            var bruiser = f.Pet(101, 42685);
            object definition = typeof(Handler).GetMethod("GetEngineerCombatBotDefinition", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [42685u]);
            var summon = ((IGameTableManager)f.Tables).Spell4.GetEntry(42894);
            // Exercise the existing real command registration/projection producer.
            typeof(Handler).GetMethod("RegisterEngineerCombatBotActions", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [f.Owner, bruiser, definition, summon]);
            Handler.OnEngineerCombatBotSummoned(f.Owner, bruiser);
        };
        Handler.UnregisterEngineerCombatBotActions(f.Owner, (IWorldEntity)f.Entities[100]);
        Assert.True(spells.TryResolveActivePetActionSpell(35501, out uint action)); Assert.Equal(52419u, action);
        Assert.False(spells.TryResolveActivePetActionSpell(51365, out _));
        ServerActionSet projected = Assert.Single(f.Packets.OfType<ServerActionSet>());
        Assert.Equal(ShortcutType.Spell, projected.Actions[(int)UILocation.LAS2].ShortcutType);
        Assert.Equal(52419u, projected.Actions[(int)UILocation.LAS2].ObjectId);
        Assert.Equal(299, f.Bars.Last(bar => bar.ShortcutSet == ShortcutSet.PrimaryPetBar).ActionBarShortcutSetId);
    }

    private static (PetMiniBarTestFixture Fixture, SpellManager Spells, IDisposable Resolver) SeedDeparture(bool projectedShortcuts = false)
    {
        var f = new PetMiniBarTestFixture();
        // Actual artillerybot tier1 selector/action rows used by the existing command mapping.
        f.Tables.SetProperty(nameof(IGameTableManager.Spell4), PlayerBindPointTests.Table(
            new Spell4Entry { Id = 51365, Spell4BaseIdBaseSpell = 34051, TierIndex = 1 },
            new Spell4Entry { Id = 35123, Spell4BaseIdBaseSpell = 20884, TierIndex = 1 },
            new Spell4Entry { Id = 42894, Spell4BaseIdBaseSpell = 27082, TierIndex = 1, Spell4IdPetSwitch = 35501 },
            new Spell4Entry { Id = 35501, Spell4BaseIdBaseSpell = 21192, TierIndex = 1 },
            new Spell4Entry { Id = 52419, Spell4BaseIdBaseSpell = 34410, TierIndex = 1 }));
        var tables = (IGameTableManager)f.Tables;
        var previous = Handler.InitialiseDependencyResolver(new MiniBarResolver(tables));
        var model = new CharacterModel();
        if (projectedShortcuts)
        {
            model.ActionSetShortcut.Add(new CharacterActionSetShortcutModel {
                Location = (ushort)UILocation.LAS1, ShortcutType = (byte)ShortcutType.SpellbookItem, ObjectId = 27002, Tier = 1 });
            model.ActionSetShortcut.Add(new CharacterActionSetShortcutModel {
                Location = (ushort)UILocation.LAS2, ShortcutType = (byte)ShortcutType.SpellbookItem, ObjectId = 27082, Tier = 1 });
        }
        var spells = new SpellManager(f.Owner, model, gameTableManager: tables);
        f.Player.SetProperty(nameof(IPlayer.SpellManager), spells);
        spells.SetActivePetActionSpell(51365, 35123, 0);
        spells.SetActivePetActionSpell(77777, 88888, 0); // unrelated selector remains owned elsewhere
        var pet = f.Pet(); Assert.True(f.Manager.TryBind(pet));
        f.Factory.UntrackSummon(pet); f.Packets.Clear();
        return (f, spells, new ResolverScope(previous));
    }

    private sealed class ResolverScope(ISpellEffectDependencyResolver previous) : IDisposable
    {
        public void Dispose() => Handler.InitialiseDependencyResolver(previous);
    }

    private sealed class MiniBarResolver(IGameTableManager tables) : ISpellEffectDependencyResolver
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

    private static void Track(IWorldEntity pet) => typeof(Handler).GetMethod("TrackEngineerCombatBotPetBar",
        BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [pet, 42814u]);
}
