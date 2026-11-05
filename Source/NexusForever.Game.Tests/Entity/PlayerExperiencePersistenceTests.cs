using System.Reflection;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using GamePath = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

public sealed class PlayerExperiencePersistenceTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public PlayerExperiencePersistenceTests() { }
    internal PlayerExperiencePersistenceTests(DbContextOptions<CharacterContext> options) => this.options = options;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PathCompletionWaitsForCommitAndResumesOnlyOnTheOriginalSession(bool replaceSession)
    {
        using var f = new Fixture(options);
        f.Seed();
        Seed(f);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var events = new EventQueue();
        sessionProxy.SetProperty(nameof(IWorldSession.Events), events);
        using var actor = new Actor(f, actorSession: session, actorEvents: events);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), actor.Player);
        await actor.Load();
        var (_, path) = Configure(f, actor);
        path.ActivateMissions(9, new Dictionary<ushort, uint> { [35] = 25 });
        var saveRequested = typeof(Player).GetField("forceSave", Instance)!;
        Assert.False((bool)saveRequested.GetValue(actor.Player)!);

        List<ushort> advanced = [];
        List<(uint Mission, bool Complete, uint Count, uint Data)> updates = [];
        sessionProxy.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPathMissionAdvanced start)
                advanced.Add(start.PathMissionId);
            else if (args[0] is ServerPathMissionUpdate update)
            {
                updates.Add((update.Mission.PathMissionId, update.Mission.Completed, update.Mission.ProgressCount, update.Mission.ProgressData));
                if (updates.Count == 1)
                {
                    if (replaceSession)
                        sessionProxy.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                    throw new IOException("Path mission update enqueue failed before delivery.");
                }
            }
            return null;
        });

        Assert.True(path.CompleteActiveMission(35));
        Assert.True((bool)saveRequested.GetValue(actor.Player)!);
        Assert.Empty(advanced);
        Assert.Empty(updates);
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        await actor.Drain();
        Assert.Empty(advanced);
        Assert.Empty(updates);
        using (var read = f.Context())
            Assert.Empty(read.CharacterPathMission);

        await Persist(f, actor, false);
        await actor.Drain();
        Assert.Equal((ushort)35, Assert.Single(advanced));
        Assert.Equal(replaceSession ? 1 : 2, updates.Count);
        Assert.All(updates, packet => Assert.Equal((35u, true, 1u, 0u), packet));
        Assert.False(path.CompleteActiveMission(35));
        using var stored = f.Context();
        Assert.Equal(1, Assert.Single(stored.CharacterPathMission).Completed);
        Assert.Equal(25u, stored.CharacterPath.Single(row => row.Path == (byte)GamePath.Soldier).TotalXp);
    }

    [Fact]
    public async Task AmpPowerPacketsReflectTheCommittedFrozenValue()
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = CreateAmpPowerActor(f, out var session, out var spells);
        await actor.Load();

        spells.AddAmpPower(3);
        Assert.Empty(AmpPowerPackets(session));
        spells.SendInitialPackets();
        Assert.Equal((ushort)0, Assert.Single(AmpPowerPackets(session)).BonusPower);
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        await actor.Drain();
        Assert.Single(AmpPowerPackets(session));

        actor.OnAuth = () => spells.AddAmpPower(2);
        await actor.Player.SaveDirect(); await actor.Drain();
        Assert.Equal(new ushort[] { 0, 3 }, AmpPowerPackets(session).Select(packet => packet.BonusPower));
        Assert.Equal((byte)50, spells.GetActionSet(0).AmpPoints);

        actor.OnAuth = null;
        await actor.Player.SaveDirect(); await actor.Drain();
        Assert.Equal(new ushort[] { 0, 3, 5 }, AmpPowerPackets(session).Select(packet => packet.BonusPower));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommittedAmpPowerEnqueueRetriesOnlyOnTheOriginalSession(bool replace)
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = CreateAmpPowerActor(f, out var session, out var spells);
        await actor.Load();
        int attempts = 0;
        session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerAmpPowerUpdate packet) return null;
            using var read = f.Context();
            Assert.Equal((ushort)3, read.Character.Single().BonusAmpPower);
            Assert.Equal((ushort)3, packet.BonusPower);
            if (++attempts == 1)
            {
                if (replace) session.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                throw new IOException("AMP power enqueue failed before delivery.");
            }
            return null;
        });

        spells.AddAmpPower(3);
        await Persist(f, actor, false);
        await actor.Drain();
        Assert.Equal(replace ? 1 : 2, attempts);
        Assert.Equal(attempts, AmpPowerPackets(session).Count());
    }

    [Fact]
    public async Task InitialAmpPowerRetriesTheLoadedValueWithoutAnotherGrant()
    {
        using var f = new Fixture(options); f.Seed();
        using (var seed = f.Context())
        {
            seed.Character.Single().BonusAmpPower = 4;
            seed.SaveChanges();
        }
        using var actor = CreateAmpPowerActor(f, out var session, out var spells);
        await actor.Load();
        int attempts = 0;
        session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAmpPowerUpdate && ++attempts == 1)
                throw new IOException("Initial AMP power enqueue failed before delivery.");
            return null;
        });

        spells.SendInitialPackets();
        Assert.Equal(1, attempts);
        await actor.Drain();
        Assert.Equal(new ushort[] { 4, 4 }, AmpPowerPackets(session).Select(packet => packet.BonusPower));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ReentrantInitialAmpPowerSyncWaitsForTheInFlightSend()
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = CreateAmpPowerActor(f, out var session, out var spells);
        await actor.Load();
        int attempts = 0, countAfterReentry = 0;
        session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerAmpPowerUpdate && ++attempts == 1)
            {
                spells.AddAmpPower(2);
                using var context = f.Context();
                actor.Player.Save(context);
                context.SaveChanges();
                spells.SendInitialPackets();
                countAfterReentry = AmpPowerPackets(session).Count();
            }
            return null;
        });

        spells.AddAmpPower(3);
        await Persist(f, actor, false);
        await actor.Drain();
        Assert.Equal(1, countAfterReentry);
        Assert.Equal(new ushort[] { 3, 5 }, AmpPowerPackets(session).Select(packet => packet.BonusPower));
        Assert.Equal(2, attempts);
    }

    private static Actor CreateAmpPowerActor(Fixture f, out RecordingDispatchProxy<IWorldSession> sessionProxy, out SpellManager spells)
    {
        var session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        var events = new EventQueue();
        sessionProxy.SetProperty(nameof(IWorldSession.Events), events);
        var actor = new Actor(f, actorSession: session, actorEvents: events);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), actor.Player);
        using var loaded = f.Context();
        spells = new SpellManager(actor.Player, loaded.Character.AsNoTracking().Single());
        actor.Set(nameof(Player.SpellManager), spells);
        return actor;
    }

    private static IEnumerable<ServerAmpPowerUpdate> AmpPowerPackets(RecordingDispatchProxy<IWorldSession> session)
        => session.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerAmpPowerUpdate>();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BonusAmpPowerSurvivesRelogAndRespectsTheFrozenSave(bool rejectFirstSave)
    {
        using var f = new Fixture(options); f.Seed();
        using var actor = new Actor(f); await actor.Load();
        using var initial = f.Context();
        var spells = new SpellManager(actor.Player, initial.Character.AsNoTracking().Single());
        actor.Set(nameof(Player.SpellManager), spells);
        spells.AddAmpPower(3);
        if (rejectFirstSave)
            await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));

        actor.OnAuth = () => spells.AddAmpPower(ushort.MaxValue);
        await actor.Player.SaveDirect(); await actor.Drain();
        AssertStoredAmpBudget(48);

        actor.OnAuth = null;
        await actor.Player.SaveDirect(); await actor.Drain();
        AssertStoredAmpBudget(55);

        void AssertStoredAmpBudget(byte expected)
        {
            using var stored = f.Context();
            var relog = new SpellManager(actor.Player, stored.Character.AsNoTracking().Single());
            for (byte spec = 0; spec < ActionSet.MaxActionSets; spec++)
                Assert.Equal(expected, relog.GetActionSet(spec).AmpPoints);
        }
    }

    [Theory]
    [InlineData("xp")] [InlineData("rest")] [InlineData("path")] [InlineData("mission")]
    public async Task AcceptedExperienceAndMissionProgressSurvivesARealPlayerFlushFailure(string action)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = new Actor(f); await actor.Load(); var (xp, path) = Configure(f, actor);
        if (action == "xp") xp.GrantXp(20);
        if (action == "rest") { Assert.Equal(500u, xp.ModifyRestBonusXp(.5f)); xp.GrantXp(20, ExpReason.KillCreature); }
        if (action == "path") path.AddXp(20);
        if (action == "mission") path.ActivateMissions(9, new Dictionary<ushort, uint> { [35] = 25 });
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        await Persist(f, actor, false);
        using var stored = f.Context(); var character = stored.Character.Single();
        Assert.Equal(action == "xp" ? 120u : action == "rest" ? 130u : 100u, character.TotalXp);
        Assert.Equal(action == "rest" ? 490u : 0u, character.RestBonusXp);
        Assert.Equal(action == "path" ? 20u : 0u, stored.CharacterPath.Single(row => row.Path == (byte)GamePath.Soldier).TotalXp);
        if (action == "mission") { var mission = Assert.Single(stored.CharacterPathMission); Assert.Equal(35, mission.PathMissionId); Assert.Equal(9, mission.PathEpisodeId); Assert.Equal(25u, mission.Xp); }
        Assert.Empty(stored.CharacterEntitlement);
    }

    [Theory]
    [InlineData("dispose")] [InlineData("rollback")] [InlineData("suppressed")] [InlineData("lost-ack")]
    public async Task AnUncertainCombinedExperienceGraphRetriesWithoutLosingOrDoublingProgress(string fault)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, false);
        using var actor = new Actor(f); await actor.Load(); var (xp, path) = Configure(f, actor);
        xp.ModifyRestBonusXp(.5f); xp.GrantXp(20, ExpReason.KillCreature); path.AddXp(20);
        path.ActivateMissions(9, new Dictionary<ushort, uint> { [35] = 25 });
        if (fault == "lost-ack") await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new LostAcknowledgement()), 42, actor.Player.Save));
        else
        {
            await using var context = f.Context(new SuppressedFlush(fault == "suppressed"));
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            actor.Player.Save(context); if (fault != "dispose") await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        await Persist(f, actor, false);
        using var stored = f.Context(); var row = stored.Character.Single();
        Assert.Equal(130u, row.TotalXp); Assert.Equal(490u, row.RestBonusXp);
        Assert.Equal(4, stored.CharacterPath.Count()); Assert.Equal(20u, stored.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).TotalXp);
        Assert.Equal(25u, Assert.Single(stored.CharacterPathMission).Xp); Assert.Empty(stored.CharacterEntitlement);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NormalSaveFreezesExperienceSelectionAndMissionMembershipBeforeAuth(bool existingPaths)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f, existingPaths);
        using var actor = new Actor(f); await actor.Load(); var (xp, path) = Configure(f, actor, existingPaths);
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, new CharacterModel { Id = 42 }); actor.Set(nameof(Player.CurrencyManager), currency);
        actor.OnAuth = () => { xp.GrantXp(existingPaths ? 1000u : 20u); path.AddXp(existingPaths ? 1000u : 20u); path.ActivateMissions(9, new Dictionary<ushort, uint> { [35] = 25 }); path.UnlockPath(GamePath.Scientist); path.ActivatePath(GamePath.Scientist); };
        await actor.Player.SaveDirect(); await actor.Drain();
        using (var read = f.Context())
        {
            Assert.Equal(100u, read.Character.Single().TotalXp); Assert.Equal((uint)GamePath.Soldier, read.Character.Single().ActivePath);
            Assert.Equal(1, read.Character.Single().Level); Assert.Empty(read.CharacterTitle);
            Assert.Equal(0u, read.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).TotalXp); Assert.Empty(read.CharacterPathMission);
        }
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        using var stored = f.Context(); Assert.Equal(existingPaths ? 1100u : 120u, stored.Character.Single().TotalXp); Assert.Equal((uint)GamePath.Scientist, stored.Character.Single().ActivePath);
        Assert.Equal(existingPaths ? 2 : 1, stored.Character.Single().Level);
        Assert.Equal(existingPaths ? 1000u : 20u, stored.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).TotalXp); Assert.Equal(25u, Assert.Single(stored.CharacterPathMission).Xp);
        if (existingPaths) Assert.Equal(22, Assert.Single(stored.CharacterTitle).Title);
    }

    [Fact]
    public async Task ExplicitEfAcceptanceOnlyAcknowledgesTheFlushedValuesAndRetainsLaterEarning()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = new Actor(f); await actor.Load(); var (xp, path) = Configure(f, actor);
        xp.GrantXp(20); path.AddXp(20);
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.Save(context); await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges();
            xp.GrantXp(20); path.AddXp(20); await transaction.CommitAsync();
        }
        using (var read = f.Context()) { Assert.Equal(120u, read.Character.Single().TotalXp); Assert.Equal(20u, read.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).TotalXp); }
        await Persist(f, actor, false); using var stored = f.Context(); Assert.Equal(140u, stored.Character.Single().TotalXp); Assert.Equal(40u, stored.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).TotalXp);
    }

    [Fact]
    public async Task RecoveryRejectsAMixtureOfMatchingCharacterAndPathCandidates()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = new Actor(f); await actor.Load(); var (xp, path) = Configure(f, actor); xp.GrantXp(20); path.AddXp(20);
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        using (var external = f.Context()) await external.Character.ExecuteUpdateAsync(set => set.SetProperty(row => row.TotalXp, 120u));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Persist(f, actor, false));
        using var stored = f.Context(); Assert.Equal(120u, stored.Character.Single().TotalXp); Assert.Equal(0u, stored.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).TotalXp);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForeignSqlOrTrackedProgressCannotBeOverwritten(bool tracked)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = new Actor(f); await actor.Load(); var (xp, path) = Configure(f, actor); xp.GrantXp(20); path.AddXp(20);
        if (!tracked) { using var external = f.Context(); await external.Character.ExecuteUpdateAsync(set => set.SetProperty(row => row.RestBonusXp, 900u)); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        { actor.Player.Save(context); if (tracked) context.Character.Local.Single().TotalXp = 999; }));
        using var stored = f.Context(); Assert.Equal(100u, stored.Character.Single().TotalXp); Assert.Equal(tracked ? 0u : 900u, stored.Character.Single().RestBonusXp);
    }

    [Fact]
    public async Task LevelAndPathRewardsRetainTheirMarkersAndTitleAcrossFailedSaveAndReplay()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = new Actor(f); await actor.Load(); var (xp, path) = Configure(f, actor, true);
        xp.GrantXp(1000); path.AddXp(1000);
        Assert.Equal(2u, actor.Player.Level); Assert.Equal(2, path.Single(value => value.Path == GamePath.Soldier).LevelRewarded);
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); await Persist(f, actor, false);
        using (var read = f.Context()) { Assert.Equal(2, read.Character.Single().Level); Assert.Equal(1100u, read.Character.Single().TotalXp); Assert.Equal(2, read.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).LevelRewarded); Assert.Equal(22, Assert.Single(read.CharacterTitle).Title); }
        using var reloaded = new Actor(f); await reloaded.Load(); var (_, replay) = Configure(f, reloaded, true);
        replay.AddXp(1); await Persist(f, reloaded, false); using var stored = f.Context(); Assert.Single(stored.CharacterTitle); Assert.Equal(1001u, stored.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).TotalXp);
    }

    [Fact]
    public async Task StoredRestedXpSurvivesAnAbsentLastOnlineTimestamp()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using (var seed = f.Context()) { seed.Character.Single().RestBonusXp = 500; seed.SaveChanges(); }
        using var actor = new Actor(f); await actor.Load(); var (xp, _) = Configure(f, actor);
        Assert.Equal(500u, xp.RestBonusXp); xp.GrantXp(20, ExpReason.KillCreature); await Persist(f, actor, false);
        using var stored = f.Context(); Assert.Equal(130u, stored.Character.Single().TotalXp); Assert.Equal(490u, stored.Character.Single().RestBonusXp);
    }

    [Fact]
    public async Task MissionAndEpisodeRewardsPersistWithCompletionAndDoNotReplayAfterReload()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using var actor = new Actor(f); await actor.Load(); var (_, path) = Configure(f, actor, true);
        path.ActivateMissions(9, new Dictionary<ushort, uint> { [35] = 25 }); Assert.True(path.CompleteActiveMission(35));
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); await Persist(f, actor, false);
        using (var read = f.Context())
        {
            Assert.Equal(1, Assert.Single(read.CharacterPathMission).Completed); Assert.Equal(25u, read.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).TotalXp);
            Assert.Equal(new ushort[] { 23, 24 }, read.CharacterTitle.OrderBy(value => value.Title).Select(value => value.Title).ToArray());
        }
        using var reloaded = new Actor(f); await reloaded.Load(); var (_, replay) = Configure(f, reloaded, true);
        Assert.False(replay.CompleteActiveMission(35)); await Persist(f, reloaded, false);
        using var stored = f.Context(); Assert.Equal(2, stored.CharacterTitle.Count()); Assert.Equal(25u, stored.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).TotalXp);
    }

    [Fact]
    public async Task UnknownMissionPayloadAndNoncanonicalFlagsSurviveAnUnrelatedXpWrite()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using (var seed = f.Context())
        {
            seed.CharacterPath.Single(value => value.Path == (byte)GamePath.Scientist).Unlocked = 2;
            seed.CharacterPathMission.Add(new() { Id = 42, PathMissionId = 88, PathEpisodeId = 77, State = 209, Completed = 2, ProgressCount = 99, ProgressData = 501, Xp = 123 }); seed.SaveChanges();
        }
        using var actor = new Actor(f); await actor.Load(); var (xp, _) = Configure(f, actor);
        using (var context = f.Context()) { actor.Player.Save(context); Assert.Empty(context.ChangeTracker.Entries<CharacterPathModel>()); Assert.Empty(context.ChangeTracker.Entries<CharacterPathMissionModel>()); }
        xp.GrantXp(20); await Persist(f, actor, false);
        using var stored = f.Context(); Assert.Equal(2, stored.CharacterPath.Single(value => value.Path == (byte)GamePath.Scientist).Unlocked);
        var mission = Assert.Single(stored.CharacterPathMission); Assert.Equal(209, mission.State); Assert.Equal(2, mission.Completed); Assert.Equal(99u, mission.ProgressCount); Assert.Equal(501u, mission.ProgressData); Assert.Equal(123u, mission.Xp);
    }

    [Fact]
    public async Task AFailedFirstSaveCannotMoveItsAcceptedExperienceToAnotherDatabase()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); var (xp, _) = Configure(f, actor); xp.GrantXp(20);
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        using var foreign = new Fixture(null); foreign.Seed(); Seed(foreign);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Persist(foreign, actor, false));
        await Persist(f, actor, false); using var read = f.Context(); using var other = foreign.Context(); Assert.Equal(120u, read.Character.Single().TotalXp); Assert.Equal(100u, other.Character.Single().TotalXp);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FractionalPathActivationPersistsAndRecoversWithoutChangingTheActorTimestamp(bool lostAcknowledgement)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); var (_, path) = Configure(f, actor);
        path.UnlockPath(GamePath.Scientist); path.ActivatePath(GamePath.Scientist);
        // Pin fractional precision independently of clock resolution, after the
        // real activation producer has accepted selection and unlock changes.
        DateTime accepted = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc); accepted = accepted.AddTicks(1_234_567);
        typeof(Player).GetField("<PathActivatedTime>k__BackingField", Instance)!.SetValue(actor.Player, accepted);
        DateTime expected; using (var projection = f.Context()) expected = projection.Database.ProviderName!.Contains("MySql", StringComparison.Ordinal)
            ? projection.Database.SqlQueryRaw<DateTime>("SELECT CAST({0} AS DATETIME) AS Value", accepted).AsEnumerable().Single() : accepted;
        if (lostAcknowledgement) await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new LostAcknowledgement()), 42, actor.Player.Save));
        else await Persist(f, actor, false);
        await Persist(f, actor, false);
        Assert.Equal(accepted, actor.Player.PathActivatedTime);
        using var stored = f.Context(); var row = stored.Character.Single(); Assert.Equal(expected, row.PathActivatedTimestamp); Assert.Equal((uint)GamePath.Scientist, row.ActivePath);
        using var reloaded = new Actor(f); await reloaded.Load(); Configure(f, reloaded); Assert.Equal(expected, reloaded.Player.PathActivatedTime); Assert.Empty(stored.CharacterEntitlement);
    }

    [Theory]
    [InlineData("spell", false)] [InlineData("scanbot", false)] [InlineData("achievement", false)]
    [InlineData("spell", true)] [InlineData("scanbot", true)] [InlineData("achievement", true)]
    public async Task CoupledPathRewardsSurviveTheSameFailedPlayerSaveAsTheirRewardedMarker(string reward, bool duringAuth)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); var (_, path) = Configure(f, actor, true);
        var tables = (GameTableManager)typeof(PathManager).GetField("gameTableManager", Instance)!.GetValue(path)!;
        using var read = f.Context(); var loaded = read.Character.AsNoTracking().Include(row => row.Spell).Include(row => row.PetCustomisation).Include(row => row.Achievement).Single();
        var row = new PathRewardEntry { Id = 1, ObjectId = PathRewardGrant.GetLevelRewardObjectId(GamePath.Soldier, 2) };
        if (reward == "spell")
        {
            var bags = (Dictionary<InventoryLocation, IBag>)typeof(Inventory).GetField("bags", Instance)!.GetValue(actor.Player.Inventory)!;
            bags.Add(InventoryLocation.Ability, new Bag(InventoryLocation.Ability, 100));
            var entry = new Spell4Entry { Id = 901, Spell4BaseIdBaseSpell = 900, TierIndex = 1 };
            var info = RecordingDispatchProxy<ISpellInfo>.Create(out var infoProxy); infoProxy.SetProperty(nameof(ISpellInfo.Entry), entry);
            var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var baseProxy); baseProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 900 }); baseProxy.SetMethodReturn(nameof(ISpellBaseInfo.GetSpellInfo), info);
            var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var globalProxy); globalProxy.SetMethodReturn(nameof(IGlobalSpellManager.GetSpellBaseInfo), baseInfo);
            actor.Set(nameof(Player.SpellManager), new SpellManager(actor.Player, loaded, globalSpellManager: global, gameTableManager: tables));
            typeof(GameTableManager).GetField("<Spell4>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(entry)); row.Spell4Id = 901;
        }
        if (reward == "scanbot")
        {
            typeof(GameTableManager).GetField("<PathScientistScanBotProfile>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(new PathScientistScanBotProfileEntry { Id = 77 }));
            actor.Set(nameof(Player.PetCustomisationManager), new PetCustomisationManager(actor.Player, loaded, tables)); row.PathScientistScanBotProfileId = 77;
        }
        if (reward == "achievement")
        {
            var info = new AchievementInfo(new AchievementEntry { Id = 500, AchievementTypeId = (uint)AchievementType.PathLevel, RequiredProgress = 2 });
            var global = RecordingDispatchProxy<IGlobalAchievementManager>.Create(out var globalProxy); globalProxy.SetMethodReturn(nameof(IGlobalAchievementManager.GetAchievement), info); globalProxy.SetMethodReturn(nameof(IGlobalAchievementManager.GetCharacterAchievements), new IAchievementInfo[] { info });
            actor.Set(nameof(Player.AchievementManager), new CharacterAchievementManager(actor.Player, loaded, globalAchievementManager: global, gameTableManager: tables));
        }
        typeof(GameTableManager).GetField("<PathReward>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(row));
        if (duringAuth)
        {
            var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, new CharacterModel { Id = 42 }); actor.Set(nameof(Player.CurrencyManager), currency);
            actor.OnAuth = () => path.AddXp(1000); await actor.Player.SaveDirect(); await actor.Drain();
            using (var frozen = f.Context())
            {
                Assert.Equal(1, frozen.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).LevelRewarded);
                Assert.Empty(frozen.CharacterSpell); Assert.Empty(frozen.CharacterPetCustomisation); Assert.Empty(frozen.CharacterAchievement);
            }
            actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        }
        else { path.AddXp(1000); await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); await Persist(f, actor, false); }
        using var stored = f.Context(); Assert.Equal(2, stored.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).LevelRewarded);
        if (reward == "spell") Assert.Equal(900u, Assert.Single(stored.CharacterSpell).Spell4BaseId);
        if (reward == "scanbot") { var pet = Assert.Single(stored.CharacterPetCustomisation); Assert.Equal((byte)PetType.ScanBot, pet.Type); Assert.Equal(77u, pet.ObjectId); }
        if (reward == "achievement") { var achievement = Assert.Single(stored.CharacterAchievement); Assert.Equal(2u, achievement.ProgressState); Assert.NotNull(achievement.DateCompleted); }
        await using var direct = f.Context();
        if (reward == "spell") actor.Player.SpellManager.Save(direct);
        if (reward == "achievement") actor.Player.AchievementManager.Save(direct);
        if (reward == "scanbot") actor.Player.PetCustomisationManager.Save(direct);
        path.Single(value => value.Path == GamePath.Soldier).Save(direct);
        if (reward == "spell") actor.Player.SpellManager.GetSpell(900).Save(direct);
        if (reward == "scanbot") actor.Player.PetCustomisationManager.GetCustomisation(PetType.ScanBot, 77).Save(direct);
        if (reward == "achievement") ((IAchievement)typeof(BaseAchievementManager<CharacterAchievementModel>).GetMethod("GetAchievement", Instance)!.Invoke(actor.Player.AchievementManager, [(ushort)500])!).Save(direct);
        await direct.SaveChangesAsync();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AutoStoredPathMaterialSurvivesFailedSaveAndWaitsForItsFrozenRewardMarker(bool duringAuth)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); var (_, path) = Configure(f, actor, true);
        var tables = (GameTableManager)typeof(PathManager).GetField("gameTableManager", Instance)!.GetValue(path)!;
        typeof(GameTableManager).GetField("<TradeskillMaterial>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(new TradeskillMaterialEntry { Id = 12, Item2IdStatRevolution = 10 }));
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var itemProxy); itemProxy.SetProperty(nameof(IItemInfo.Id), 10u);
        itemProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 10, MaxStackCount = 10 }); itemProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        f.Source.ManagerProxy.SetMethodHandler("GetItemInfo", _ => info);
        using var read = f.Context(); var loaded = read.Character.AsNoTracking().Include(row => row.TradeskillMaterials).Single();
        var satchel = new SupplySatchelManager(actor.Player, loaded, tables); actor.Set(nameof(Player.SupplySatchelManager), satchel);
        f.Source.PlayerProxy.SetProperty(nameof(IPlayer.SupplySatchelManager), satchel); actor.Player.IsLoading = false;
        typeof(GameTableManager).GetField("<PathReward>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(new PathRewardEntry { Id = 1, ObjectId = PathRewardGrant.GetLevelRewardObjectId(GamePath.Soldier, 2), Item2Id = 10, Count = 3 }));
        if (duringAuth)
        {
            var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, new CharacterModel { Id = 42 }); actor.Set(nameof(Player.CurrencyManager), currency);
            actor.OnAuth = () => path.AddXp(1000); await actor.Player.SaveDirect(); await actor.Drain();
            using (var frozen = f.Context()) { Assert.Equal(1, frozen.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).LevelRewarded); Assert.Empty(frozen.CharacterTradeskillMaterial); }
            actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain();
        }
        else { path.AddXp(1000); await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); await Persist(f, actor, false); }
        using var stored = f.Context(); Assert.Equal(2, stored.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).LevelRewarded);
        var material = Assert.Single(stored.CharacterTradeskillMaterial); Assert.Equal(12, material.MaterialId); Assert.Equal(3, material.Amount); Assert.Empty(stored.Item);
    }

    [Theory]
    [InlineData("dispose")] [InlineData("rollback")] [InlineData("suppressed")] [InlineData("lost-ack")]
    public async Task SatchelIntentRecoversUncertainOutcomesAndRetainsEarningAfterExplicitEfAcceptance(string fault)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); var (xp, path) = Configure(f, actor);
        var tables = (GameTableManager)typeof(PathManager).GetField("gameTableManager", Instance)!.GetValue(path)!;
        typeof(GameTableManager).GetField("<TradeskillMaterial>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(new TradeskillMaterialEntry { Id = 12, Item2IdStatRevolution = 10 }));
        using var read = f.Context(); var loaded = read.Character.AsNoTracking().Include(row => row.TradeskillMaterials).Single();
        var satchel = new SupplySatchelManager(actor.Player, loaded, tables); actor.Set(nameof(Player.SupplySatchelManager), satchel);
        Assert.Equal(0u, satchel.AddAmount(12, 1)); xp.GrantXp(20);
        if (fault == "lost-ack") await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new LostAcknowledgement()), 42, actor.Player.Save));
        else
        {
            await using var context = f.Context(new SuppressedFlush(fault == "suppressed")); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            actor.Player.Save(context); if (fault != "dispose") await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        await Persist(f, actor, false); using (var stored = f.Context()) { Assert.Equal(1, Assert.Single(stored.CharacterTradeskillMaterial).Amount); Assert.Equal(120u, stored.Character.Single().TotalXp); }
        satchel.AddAmount(12, 1); xp.GrantXp(20);
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.Save(context); await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges();
            satchel.AddAmount(12, 1); xp.GrantXp(20); await transaction.CommitAsync();
        }
        using (var stored = f.Context()) { Assert.Equal(2, Assert.Single(stored.CharacterTradeskillMaterial).Amount); Assert.Equal(140u, stored.Character.Single().TotalXp); }
        await Persist(f, actor, false); using var final = f.Context(); Assert.Equal(3, Assert.Single(final.CharacterTradeskillMaterial).Amount); Assert.Equal(160u, final.Character.Single().TotalXp);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SatchelRecoveryRejectsForeignSqlAndTrackedAmountChanges(bool tracked)
    {
        using var f = new Fixture(options); f.Seed(); Seed(f);
        using (var seed = f.Context()) { seed.CharacterTradeskillMaterial.Add(new() { Id = 42, MaterialId = 12, Amount = 2 }); seed.SaveChanges(); }
        using var actor = new Actor(f); await actor.Load(); var (_, path) = Configure(f, actor);
        var tables = (GameTableManager)typeof(PathManager).GetField("gameTableManager", Instance)!.GetValue(path)!;
        typeof(GameTableManager).GetField("<TradeskillMaterial>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(new TradeskillMaterialEntry { Id = 12, Item2IdStatRevolution = 10 }));
        using var read = f.Context(); var loaded = read.Character.AsNoTracking().Include(row => row.TradeskillMaterials).Single();
        var satchel = new SupplySatchelManager(actor.Player, loaded, tables); actor.Set(nameof(Player.SupplySatchelManager), satchel); satchel.AddAmount(12, 1);
        if (!tracked) { using var external = f.Context(); await external.CharacterTradeskillMaterial.ExecuteUpdateAsync(set => set.SetProperty(row => row.Amount, (ushort)99)); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        { actor.Player.Save(context); if (tracked) context.CharacterTradeskillMaterial.Local.Single().Amount = 9; }));
        using var stored = f.Context(); Assert.Equal(tracked ? 2 : 99, Assert.Single(stored.CharacterTradeskillMaterial).Amount);
    }

    [Fact]
    public async Task APathItemRewardAcceptedDuringAuthWaitsForTheSaveOfItsRewardedMarker()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); var (_, path) = Configure(f, actor, true);
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var itemProxy); itemProxy.SetProperty(nameof(IItemInfo.Id), 10u);
        itemProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 10, MaxStackCount = 10, MaxCharges = 1 }); itemProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        f.Source.ManagerProxy.SetMethodHandler("GetItemInfo", _ => info);
        var tables = (GameTableManager)typeof(PathManager).GetField("gameTableManager", Instance)!.GetValue(path)!;
        typeof(GameTableManager).GetField("<PathReward>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(new PathRewardEntry { Id = 1, ObjectId = PathRewardGrant.GetLevelRewardObjectId(GamePath.Soldier, 2), Item2Id = 10, Count = 1 }));
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, new CharacterModel { Id = 42 }); actor.Set(nameof(Player.CurrencyManager), currency);
        actor.OnAuth = () => path.AddXp(1000); await actor.Player.SaveDirect(); await actor.Drain();
        using (var frozen = f.Context()) { Assert.Equal(1, frozen.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).LevelRewarded); Assert.Empty(frozen.Item); }
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain(); using var stored = f.Context();
        Assert.Equal(2, stored.CharacterPath.Single(value => value.Path == (byte)GamePath.Soldier).LevelRewarded); var item = Assert.Single(stored.Item); Assert.Equal(10u, item.ItemId); Assert.Equal(1u, item.StackCount);
    }

    [Fact]
    public async Task APreAuthStackEditCommitsItsCapturedQuantityAndRetainsLaterEarning()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var itemProxy); itemProxy.SetProperty(nameof(IItemInfo.Id), 10u);
        itemProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 10, MaxStackCount = 10, MaxCharges = 1 }); itemProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        f.Source.ManagerProxy.SetMethodHandler("GetItemInfo", _ => info);
        actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 10, 1); await Persist(f, actor, false);
        actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 10, 1);
        var currency = new CurrencyManager(f.Source.Tables); currency.Initialise(actor.Player, new CharacterModel { Id = 42 }); actor.Set(nameof(Player.CurrencyManager), currency);
        actor.OnAuth = () => actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 10, 1);
        await actor.Player.SaveDirect(); await actor.Drain(); using (var frozen = f.Context()) Assert.Equal(2u, Assert.Single(frozen.Item).StackCount);
        actor.OnAuth = null; await actor.Player.SaveDirect(); await actor.Drain(); using var stored = f.Context(); Assert.Equal(3u, Assert.Single(stored.Item).StackCount);
    }

    [Fact]
    public async Task AStandaloneFrozenInventorySaveRetainsItsOrdinaryTrackedWriteGuard()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); Configure(f, actor);
        var info = RecordingDispatchProxy<IItemInfo>.Create(out var itemProxy); itemProxy.SetProperty(nameof(IItemInfo.Id), 10u);
        itemProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { Id = 10, MaxStackCount = 10, MaxCharges = 1 }); itemProxy.SetMethodReturn(nameof(IItemInfo.IsStackable), true);
        f.Source.ManagerProxy.SetMethodHandler("GetItemInfo", _ => info); actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 10, 1);
        using var identity = f.Context(); var frozen = ((Inventory)actor.Player.Inventory).CaptureNormalSave(CharacterDatabaseIdentity.Get(identity));
        await using var context = f.Context(); frozen(context); context.Item.Local.Single().StackCount = 9;
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync()); using var stored = f.Context(); Assert.Empty(stored.Item);
    }

    [Fact]
    public async Task CharacterLevelSpellAndAutomaticLasSlotRecoverAndLaterSlotRestorationRemainsPending()
    {
        using var f = new Fixture(options); f.Seed(); Seed(f); using var actor = new Actor(f); await actor.Load(); var (xp, path) = Configure(f, actor, true);
        actor.Set(nameof(Player.Class), Class.Warrior);
        var tables = (GameTableManager)typeof(PathManager).GetField("gameTableManager", Instance)!.GetValue(path)!;
        using var read = f.Context(); var loaded = read.Character.AsNoTracking().Single(); var spells = CreateSpellbook(actor, loaded, tables);
        typeof(GameTableManager).GetField("<SpellLevel>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(new SpellLevelEntry { Id = 1, ClassId = (uint)Class.Warrior, CharacterLevel = 2, Spell4Id = 901 }));
        typeof(GameTableManager).GetField("<Class>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(new ClassEntry { Id = (uint)Class.Warrior, Spell4IdInnateAbilityActive = [], Spell4IdInnateAbilityPassive = [], Spell4IdAttackPrimary = [], Spell4IdAttackUnarmed = [] }));
        typeof(Player).GetField("attributePointAllocations", Instance)!.SetValue(actor.Player, new uint[6]); actor.Player.IsLoading = false;
        xp.GrantXp(1000); Assert.Equal(2u, actor.Player.Level); Assert.NotNull(spells.GetActionSet(0).GetShortcut(UILocation.LAS1));
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true)); await Persist(f, actor, false);
        using (var stored = f.Context()) { Assert.Equal(2, stored.Character.Single().Level); Assert.Equal(900u, Assert.Single(stored.CharacterSpell).Spell4BaseId); Assert.Equal(900u, Assert.Single(stored.CharacterActionSetShortcut).ObjectId); }
        await using (var direct = f.Context()) { spells.GetActionSet(0).Save(direct); spells.GetActionSet(0).GetShortcut(UILocation.LAS1).Save(direct); await direct.SaveChangesAsync(); }
        var bar = spells.GetActionSet(0); bar.RemoveShortcut(UILocation.LAS1);
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            actor.Player.Save(context); await context.SaveChangesAsync(false); context.ChangeTracker.AcceptAllChanges();
            bar.AddShortcut(UILocation.LAS1, ShortcutType.GameCommand, 7, 0); await transaction.CommitAsync();
        }
        using (var stored = f.Context()) Assert.Empty(stored.CharacterActionSetShortcut);
        await Persist(f, actor, false); using var final = f.Context(); var shortcut = Assert.Single(final.CharacterActionSetShortcut); Assert.Equal((byte)ShortcutType.GameCommand, shortcut.ShortcutType); Assert.Equal(7u, shortcut.ObjectId);
    }

    private static SpellManager CreateSpellbook(Actor actor, CharacterModel loaded, GameTableManager tables)
    {
        var bags = (Dictionary<InventoryLocation, IBag>)typeof(Inventory).GetField("bags", Instance)!.GetValue(actor.Player.Inventory)!;
        if (!bags.ContainsKey(InventoryLocation.Ability)) bags.Add(InventoryLocation.Ability, new Bag(InventoryLocation.Ability, 100));
        var entry = new Spell4Entry { Id = 901, Spell4BaseIdBaseSpell = 900, TierIndex = 1 };
        var info = RecordingDispatchProxy<ISpellInfo>.Create(out var infoProxy); infoProxy.SetProperty(nameof(ISpellInfo.Entry), entry);
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var baseProxy); baseProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 900 }); baseProxy.SetMethodReturn(nameof(ISpellBaseInfo.GetSpellInfo), info);
        var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var globalProxy); globalProxy.SetMethodReturn(nameof(IGlobalSpellManager.GetSpellBaseInfo), baseInfo);
        var spells = new SpellManager(actor.Player, loaded, globalSpellManager: global, gameTableManager: tables); actor.Set(nameof(Player.SpellManager), spells);
        typeof(GameTableManager).GetField("<Spell4>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table(entry)); return spells;
    }

    private sealed class SuppressedFlush(bool suppress) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => new(suppress ? InterceptionResult<int>.SuppressWithResult(0) : result);
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned experience acknowledgement loss."));
    }

    private static void Seed(Fixture f, bool paths = true)
    {
        using var context = f.Context(); var model = context.Character.Single();
        model.TotalXp = 100; model.Level = 1; model.RestBonusXp = 0; model.LastOnline = null; model.ActivePath = (uint)GamePath.Soldier;
        for (int i = (int)GamePath.Soldier; paths && i <= (int)GamePath.Explorer; i++)
            context.CharacterPath.Add(new() { Id = 42, Path = (byte)i, Unlocked = (byte)(i == (int)GamePath.Soldier ? 1 : 0), LevelRewarded = (byte)(i == (int)GamePath.Soldier ? 1 : 0) });
        context.SaveChanges();
    }

    private static (XpManager Xp, PathManager Path) Configure(Fixture f, Actor actor, bool rewards = false)
    {
        actor.Player.Account.GetType().GetMethod("SetProperty")!.Invoke(actor.Player.Account, ["RbacManager", RecordingDispatchProxy<IAccountRBACManager>.Create(out _)]);
        typeof(Player).GetField("path", Instance)!.SetValue(actor.Player, GamePath.Soldier);
        CharacterModel loaded; using (var context = f.Context()) loaded = context.Character.AsNoTracking().Include(row => row.Path).Include(row => row.PathMission).Single();
        typeof(Player).GetField("<PathActivatedTime>k__BackingField", Instance)!.SetValue(actor.Player, loaded.PathActivatedTimestamp);
        var tables = (GameTableManager)typeof(XpManagerTests).GetMethod("CreateGameTableManager", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null,
            [new[] { new XpPerLevelEntry { Id = 1, MinXpForLevel = 0 }, new XpPerLevelEntry { Id = 2, MinXpForLevel = 1000 } }])!;
        var xp = new XpManager(actor.Player, loaded, tables); actor.Set(nameof(Player.XpManager), xp);
        var tests = new PathManagerTests(); var build = typeof(PathManagerTests).GetMethod("BuildGameTableProvider", Instance)!;
        var arguments = build.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray();
        arguments[0] = new[] { new PathLevelEntry { Id = 1, PathTypeEnum = (uint)GamePath.Soldier, PathLevel = 1, PathXP = 0 }, new PathLevelEntry { Id = 2, PathTypeEnum = (uint)GamePath.Soldier, PathLevel = 2, PathXP = 1000 } };
        arguments[1] = rewards ? new[] {
            new PathRewardEntry { Id = 1, ObjectId = PathRewardGrant.GetLevelRewardObjectId(GamePath.Soldier, 2), CharacterTitleId = 22 },
            new PathRewardEntry { Id = 2, ObjectId = 35, PathRewardTypeEnum = PathRewardGrant.MissionRewardType, CharacterTitleId = 23 },
            new PathRewardEntry { Id = 3, ObjectId = 9, PathRewardTypeEnum = PathRewardGrant.EpisodeRewardType, CharacterTitleId = 24 } } : Array.Empty<PathRewardEntry>(); build.Invoke(tests, arguments);
        var pathTables = (GameTableManager)typeof(PathManagerTests).GetField("configuredGameTableManager", Instance)!.GetValue(tests)!;
        var path = new PathManager(actor.Player, loaded, gameTableManager: pathTables); actor.Set(nameof(Player.PathManager), path);
        typeof(GridEntity).GetField("visibleEntities", Instance)!.SetValue(actor.Player, new Dictionary<uint, IGridEntity>());
        if (rewards)
        {
            actor.Player.IsLoading = true;
            typeof(Player).GetField("gameTableManager", Instance)!.SetValue(actor.Player, tables);
            typeof(GameTableManager).GetField("<UnitProperty2>k__BackingField", Instance)!.SetValue(tables, PlayerBindPointTests.Table<UnitProperty2Entry>());
            typeof(WorldEntity).GetField("gameTableManagerResolver", Instance)!.SetValue(actor.Player, (Func<IGameTableManager>)(() => tables));
            foreach (var pair in new[] { (typeof(WorldEntity), "properties"), (typeof(Player), "itemProperties"), (typeof(UnitEntity), "spellProperties"), (typeof(UnitEntity), "recalculatingSpellPropertyConversions") })
            { var field = pair.Item1.GetField(pair.Item2, Instance)!; field.SetValue(actor.Player, Activator.CreateInstance(field.FieldType)); }
            var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var characterProxy);
            characterProxy.SetMethodReturn(nameof(ICharacterManager.GetCharacterBaseProperties), Array.Empty<IPropertyModifier>());
            characterProxy.SetMethodReturn(nameof(ICharacterManager.GetCharacterClassBaseProperties), Array.Empty<IPropertyModifier>());
            typeof(Player).GetField("characterManager", Instance)!.SetValue(actor.Player, characters);
            typeof(GameTableManager).GetField("<CharacterTitle>k__BackingField", Instance)!.SetValue(pathTables, PlayerBindPointTests.Table(new CharacterTitleEntry { Id = 22 }, new CharacterTitleEntry { Id = 23 }, new CharacterTitleEntry { Id = 24 }));
            using var read = f.Context(); var titleModel = read.Character.AsNoTracking().Include(row => row.CharacterTitle).Single();
            actor.Set(nameof(Player.TitleManager), new TitleManager(actor.Player, titleModel, pathTables));
            // The actor's real level setter also runs on the reloaded fixture.
            actor.Player.Level = loaded.Level;
        }
        return (xp, path);
    }

    private static Task Persist(Fixture f, Actor actor, bool fail)
        => CharacterAtomicSave.Execute(() => { var context = f.Context(); if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned experience flush fault."); return context; }, 42, actor.Player.Save);
}
