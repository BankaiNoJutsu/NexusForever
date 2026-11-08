using System.Numerics;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Challenges;

namespace NexusForever.Game.Tests.Challenges;

// Phase 1 of the time-tiered completion design (Decomp/Analysis/CHALLENGE_TIME_TIERED_COMPLETION_DESIGN_20261108.md): raw millisecond goals for flag-8 rows, completion by
// elapsed time, and finish detection by reaching the indicator location, enabled for Thayd Sprint only. Source/SQLite tests; no retail parity claim.
public partial class ChallengeManagerTests
{
    private const ushort ThaydSprintId = 1002;
    private const uint ThaydSprintIndicatorId = 41555u;
    private const uint TestWorldId = 100u;
    private const uint TimeTieredTestFlag = 8u;
    private static readonly uint[] ThaydSprintGoals = [1u, 60_000u, 70_000u];

    [Fact]
    public void RawGoalsKeepAZeroFirstGoalAndSkipMissingTierIds()
    {
        GameTable<ChallengeTierEntry> tiers = CreateGameTable(
            new ChallengeTierEntry { Id = 1, Count = 0u },
            new ChallengeTierEntry { Id = 2, Count = 30_000u },
            new ChallengeTierEntry { Id = 3, Count = 40_000u });

        Assert.Equal([0u, 30_000u, 40_000u], ChallengeTimeTiers.RawGoals(
            new ChallengeEntry { ChallengeTierId00 = 1, ChallengeTierId01 = 2, ChallengeTierId02 = 3 }, tiers));
        Assert.Equal([0u], ChallengeTimeTiers.RawGoals(new ChallengeEntry { ChallengeTierId00 = 1 }, tiers));
        Assert.Equal([30_000u], ChallengeTimeTiers.RawGoals(new ChallengeEntry { ChallengeTierId01 = 2 }, tiers));
        Assert.Equal([30_000u], ChallengeTimeTiers.RawGoals(new ChallengeEntry { ChallengeTierId00 = 99, ChallengeTierId01 = 2 }, tiers));
        Assert.Empty(ChallengeTimeTiers.RawGoals(new ChallengeEntry(), tiers));
        Assert.Empty(ChallengeTimeTiers.RawGoals(new ChallengeEntry { ChallengeTierId00 = 1 }, null));
    }

    [Fact]
    public void TimeTieredRowPublishesTheUnfilteredMillisecondGoals()
    {
        var (manager, session, _, _) = CreateTimeTieredThaydSprint(goals: [0u, 60_000u, 70_000u]);

        manager.HandleChoice(ThaydSprintId, ChallengeChoice.Activate);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(session).Last().ActiveChallenges);
        Assert.Equal([0u, 60_000u, 70_000u], row.TierGoalCount);
        Assert.Equal(120_000u, row.TimeTotalActive);
        Assert.Equal(0u, row.CurrentTier);
    }

    [Fact]
    public void CountTierRowStillPublishesTheCompactedGoals()
    {
        GameTableManager tables = CreateGameTables(tierOneCount: 5);
        IPlayer player = CreatePlayer(RecipientGuid, out var session, tables);
        player.ChallengeManager.HandleChoice(ChallengeId, ChallengeChoice.Activate);

        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(session).Last().ActiveChallenges);
        Assert.Equal([5u, 0u, 0u], row.TierGoalCount);
    }

    [Theory]
    [InlineData(10d, 3u)]     // gold needs elapsed < 120000 - 70000 = 50000
    [InlineData(55d, 2u)]
    [InlineData(65d, 1u)]
    public void CompletionEarnsTheTiersThatAreStillOpenAtTheElapsedTime(double elapsedSeconds, uint tiers)
    {
        var (manager, session, inventory, quests) = CreateTimeTieredThaydSprint();
        manager.HandleChoice(ThaydSprintId, ChallengeChoice.Activate);
        manager.Update(elapsedSeconds);

        Assert.True(manager.TryCompleteTimeTiered(ThaydSprintId));

        int medal = (int)tiers - 1;
        var results = GetMessages<ServerChallengeResult>(session).ToList();
        Assert.Equal([ChallengeResult.Activate, ChallengeResult.TierAchieved, ChallengeResult.Completed], results.Select(r => r.Result));
        Assert.Equal(medal, results[1].Data);
        Assert.Equal(medal, results[2].Data);
        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(session).Last().ActiveChallenges);
        Assert.False(row.Activated);
        Assert.Equal(tiers, row.CurrentTier);
        Assert.Equal((uint)medal, row.LastRewardTier);
        Assert.Equal(1u, row.CompletionCount);
        Assert.Equal([1u, 60_000u, 70_000u], row.TierGoalCount);
        var item = Assert.Single(inventory.GetInvocations(nameof(IInventory.ItemCreate)));
        Assert.Equal(9011u + (uint)medal, item.Arguments[1]); // reward row flags 1/2/4 hold the item of that medal
        Assert.Single(quests.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void CompletionInTheLastMillisecondCountsAsCompletedWithoutAMedalOrReward()
    {
        var (manager, session, inventory, _) = CreateTimeTieredThaydSprint();
        manager.HandleChoice(ThaydSprintId, ChallengeChoice.Activate);
        manager.Update(119.999d); // elapsed 119999 ms: even the 1 ms goal is lost

        Assert.True(manager.TryCompleteTimeTiered(ThaydSprintId));

        Assert.Equal([ChallengeResult.Activate, ChallengeResult.Completed], GetMessages<ServerChallengeResult>(session).Select(r => r.Result));
        ServerChallengeUpdate.Challenge row = Assert.Single(GetMessages<ServerChallengeUpdate>(session).Last().ActiveChallenges);
        Assert.Equal(0u, row.CurrentTier);
        Assert.Equal(1u, row.CompletionCount);
        Assert.Empty(inventory.GetInvocations(nameof(IInventory.ItemCreate)));
    }

    [Fact]
    public void CompletionIsRejectedWithoutAnObservedLimitOrActiveAttemptAndOnlyHappensOnce()
    {
        // No observed limit (id 1500): a tier from the 300 s default would be wrong.
        GameTableManager tables = CreateTimeTieredTables((1500, ThaydSprintGoals));
        IPlayer player = CreatePlayer(RecipientGuid, out var session, tables);
        player.ChallengeManager.HandleChoice(1500, ChallengeChoice.Activate);
        int before = GetMessages<ServerChallengeResult>(session).Count;
        Assert.False(player.ChallengeManager.TryCompleteTimeTiered(1500));
        Assert.Equal(before, GetMessages<ServerChallengeResult>(session).Count);

        var (manager, thaydSession, _, _) = CreateTimeTieredThaydSprint();
        Assert.False(manager.TryCompleteTimeTiered(ThaydSprintId)); // not active yet
        Assert.Empty(GetMessages<ServerChallengeResult>(thaydSession));
        manager.HandleChoice(ThaydSprintId, ChallengeChoice.Activate);
        Assert.True(manager.TryCompleteTimeTiered(ThaydSprintId));
        int afterFirst = GetMessages<ServerChallengeResult>(thaydSession).Count;
        Assert.False(manager.TryCompleteTimeTiered(ThaydSprintId));
        Assert.Equal(afterFirst, GetMessages<ServerChallengeResult>(thaydSession).Count);
        Assert.Equal(1u, manager.GetCompletionCount(ThaydSprintId));
    }

    [Fact]
    public void CompletionAtTheLimitIsLeftToTheTimerExpiry()
    {
        var (manager, session, _, _) = CreateTimeTieredThaydSprint();
        manager.HandleChoice(ThaydSprintId, ChallengeChoice.Activate);
        manager.Update(120d);

        Assert.False(manager.TryCompleteTimeTiered(ThaydSprintId));
        Assert.Contains(GetMessages<ServerChallengeResult>(session), r => r.Result == ChallengeResult.TimerExpired);
        Assert.Equal(0u, manager.GetCompletionCount(ThaydSprintId));
    }

    [Fact]
    public void CountTierChallengeCannotBeCompletedByTime()
    {
        GameTableManager tables = CreateGameTables(tierOneCount: 5);
        IPlayer player = CreatePlayer(RecipientGuid, out _, tables);
        player.ChallengeManager.HandleChoice(ChallengeId, ChallengeChoice.Activate);

        Assert.False(player.ChallengeManager.TryCompleteTimeTiered(ChallengeId));
    }

    [Fact]
    public void ReachingTheIndicatorFinishesThaydSprint()
    {
        var (manager, session, _, _) = CreateTimeTieredThaydSprint();
        manager.HandleChoice(ThaydSprintId, ChallengeChoice.Activate);
        manager.Update(20d);

        manager.OnPlayerRelocated(TestWorldId, new Vector3(105f, 50f, 200f));

        Assert.Equal(1u, manager.GetCompletionCount(ThaydSprintId));
        Assert.Contains(GetMessages<ServerChallengeResult>(session), r => r.Result == ChallengeResult.Completed);
    }

    [Theory]
    [InlineData(100u, 111f, 50f, 200f)]   // outside the 10 unit radius
    [InlineData(101u, 105f, 50f, 200f)]   // inside the radius but on another world
    [InlineData(100u, 105f, 60f, 200f)]   // above the vertical clamp of 5
    public void OtherPositionsDoNotFinishThaydSprint(uint worldId, float x, float y, float z)
    {
        var (manager, session, _, _) = CreateTimeTieredThaydSprint(maxVerticalDistance: 5f);
        manager.HandleChoice(ThaydSprintId, ChallengeChoice.Activate);

        manager.OnPlayerRelocated(worldId, new Vector3(x, y, z));

        Assert.Equal(0u, manager.GetCompletionCount(ThaydSprintId));
        Assert.True(manager.IsChallengeActivated(ThaydSprintId));
        Assert.DoesNotContain(GetMessages<ServerChallengeResult>(session), r => r.Result == ChallengeResult.Completed);
    }

    [Fact]
    public void ReachingTheIndicatorWithoutAnActiveAttemptDoesNothing()
    {
        var (manager, session, _, _) = CreateTimeTieredThaydSprint();

        manager.OnPlayerRelocated(TestWorldId, new Vector3(105f, 50f, 200f));

        Assert.Empty(GetMessages<ServerChallengeResult>(session));
        Assert.Equal(0u, manager.GetCompletionCount(ThaydSprintId));
    }

    [Fact]
    public void OnlyThaydSprintFinishesByReachingTheIndicatorInThisPhase()
    {
        // 511 Ravenok Racer has an observed limit but needs a mount check; 1500 has no observed limit. Both stay unfinished at their indicator.
        foreach (ushort id in new ushort[] { 511, 1500 })
        {
            GameTableManager tables = CreateTimeTieredTables((id, ThaydSprintGoals));
            IPlayer player = CreatePlayer(RecipientGuid, out var session, tables);
            player.ChallengeManager.HandleChoice(id, ChallengeChoice.Activate);

            player.ChallengeManager.OnPlayerRelocated(TestWorldId, new Vector3(105f, 50f, 200f));

            Assert.True(player.ChallengeManager.IsChallengeActivated(id));
            Assert.DoesNotContain(GetMessages<ServerChallengeResult>(session), r => r.Result == ChallengeResult.Completed);
        }
    }

    private static (IChallengeManager Manager, RecordingDispatchProxy<IGameSession> Session,
        RecordingDispatchProxy<IInventory> Inventory, RecordingDispatchProxy<IQuestManager> Quests) CreateTimeTieredThaydSprint(
            uint[] goals = null, float maxVerticalDistance = 0f)
    {
        GameTableManager tables = CreateTimeTieredTables(maxVerticalDistance, (ThaydSprintId, goals ?? ThaydSprintGoals));
        const uint trackId = 16u;
        tables.Challenge.GetEntry(ThaydSprintId).RewardTrackId = trackId;
        SetAutoProperty(tables, nameof(GameTableManager.RewardTrack), CreateGameTable(new RewardTrackEntry { Id = trackId }));
        SetAutoProperty(tables, nameof(GameTableManager.RewardTrackRewards), CreateGameTable(
            new RewardTrackRewardsEntry { Id = 1u, RewardTrackId = trackId, RewardPointFlags = 1u, RewardChoiceId00 = 9011u, RewardChoiceCount00 = 1u },
            new RewardTrackRewardsEntry { Id = 2u, RewardTrackId = trackId, RewardPointFlags = 2u, RewardChoiceId00 = 9012u, RewardChoiceCount00 = 1u },
            new RewardTrackRewardsEntry { Id = 3u, RewardTrackId = trackId, RewardPointFlags = 4u, RewardChoiceId00 = 9013u, RewardChoiceCount00 = 1u }));
        IPlayer player = CreatePlayer(RecipientGuid, out var session, tables, out var quests);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out var inventoryProxy);
        ((RecordingDispatchProxy<IPlayer>)(object)player).SetProperty(nameof(IPlayer.Inventory), inventory);
        return (player.ChallengeManager, session, inventoryProxy, quests);
    }

    private static GameTableManager CreateTimeTieredTables(params (ushort Id, uint[] Goals)[] challenges)
        => CreateTimeTieredTables(0f, challenges);

    private static GameTableManager CreateTimeTieredTables(float maxVerticalDistance, params (ushort Id, uint[] Goals)[] challenges)
    {
        GameTableManager tables = CreateEmptyGameTables();
        var tiers = new List<ChallengeTierEntry>();
        var entries = new List<ChallengeEntry>();
        uint nextTier = 3001u;
        foreach ((ushort id, uint[] goals) in challenges)
        {
            var entry = new ChallengeEntry
            {
                Id = id,
                ChallengeTypeEnum = (uint)ChallengeType.General,
                ChallengeFlags = TimeTieredTestFlag,
                WorldLocation2IdIndicator = ThaydSprintIndicatorId,
                CompletionCount = 1u
            };
            uint[] ids = new uint[3];
            for (int i = 0; i < goals.Length; i++)
            {
                ids[i] = nextTier++;
                tiers.Add(new ChallengeTierEntry { Id = ids[i], Count = goals[i] });
            }

            entry.ChallengeTierId00 = ids[0];
            entry.ChallengeTierId01 = ids[1];
            entry.ChallengeTierId02 = ids[2];
            entries.Add(entry);
        }

        SetAutoProperty(tables, nameof(GameTableManager.ChallengeTier), CreateGameTable(tiers.ToArray()));
        SetAutoProperty(tables, nameof(GameTableManager.Challenge), CreateGameTable(entries.ToArray()));
        SetAutoProperty(tables, nameof(GameTableManager.WorldLocation2), CreateGameTable(new WorldLocation2Entry
        {
            Id = ThaydSprintIndicatorId, WorldId = TestWorldId, Position0 = 100f, Position1 = 50f, Position2 = 200f, Radius = 10f,
            MaxVerticalDistance = maxVerticalDistance
        }));
        return tables;
    }
}
