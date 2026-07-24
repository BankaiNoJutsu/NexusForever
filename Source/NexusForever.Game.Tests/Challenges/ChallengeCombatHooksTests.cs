using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Challenges;

namespace NexusForever.Game.Tests.Challenges;

public class ChallengeCombatHooksTests
{
    private const ushort CombatChallengeId = 1001;
    private const uint TargetCreatureId = 4242u;
    private const uint AutoActivateOnProgressFlag = 0x20u;

    [Fact]
    public void OnCreatureKilled_AdvancesMatchingCombatChallenge()
    {
        GameTableManager gameTables = CreateCombatGameTables();
        IPlayer player = CreatePlayer(9001u, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        player.ChallengeManager.HandleChoice(CombatChallengeId, ChallengeChoice.Activate);

        ChallengeCombatHooks.OnCreatureKilled(player, TargetCreatureId);

        ServerChallengeResult result = GetMessages<ServerChallengeResult>(sessionProxy).Last();
        Assert.True(result.Result is ChallengeResult.TierAchieved or ChallengeResult.Completed);
    }

    [Fact]
    public void OnCreatureKilled_AdvancesCombatChallengeForDirectTargetGroupMember()
    {
        const uint targetGroupId = 1852u;
        GameTableManager gameTables = CreateCombatGameTables(
            target: targetGroupId,
            new TargetGroupEntry
            {
                Id          = targetGroupId,
                Type        = (uint)TargetGroupType.CreatureIdGroup,
                DataEntries = [TargetCreatureId, 13549u, 12213u, 0u, 0u, 0u, 0u]
            });
        IPlayer player = CreatePlayer(9001u, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        player.ChallengeManager.HandleChoice(CombatChallengeId, ChallengeChoice.Activate);

        ChallengeCombatHooks.OnCreatureKilled(player, TargetCreatureId);

        ServerChallengeResult result = GetMessages<ServerChallengeResult>(sessionProxy).Last();
        Assert.True(result.Result is ChallengeResult.TierAchieved or ChallengeResult.Completed);
    }

    [Fact]
    public void OnCreatureKilled_AdvancesCombatChallengeForNestedTargetGroupMember()
    {
        const uint parentTargetGroupId = 8851u;
        const uint childTargetGroupId = 2309u;
        GameTableManager gameTables = CreateCombatGameTables(
            target: parentTargetGroupId,
            new TargetGroupEntry
            {
                Id          = parentTargetGroupId,
                Type        = (uint)TargetGroupType.OtherTargetGroupCreatures,
                DataEntries = [childTargetGroupId, 0u, 0u, 0u, 0u, 0u, 0u]
            },
            new TargetGroupEntry
            {
                Id          = childTargetGroupId,
                Type        = (uint)TargetGroupType.CreatureIdListGroup,
                DataEntries = [TargetCreatureId, 11965u, 11580u, 0u, 0u, 0u, 0u]
            });
        IPlayer player = CreatePlayer(9001u, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        player.ChallengeManager.HandleChoice(CombatChallengeId, ChallengeChoice.Activate);

        ChallengeCombatHooks.OnCreatureKilled(player, TargetCreatureId);

        ServerChallengeResult result = GetMessages<ServerChallengeResult>(sessionProxy).Last();
        Assert.True(result.Result is ChallengeResult.TierAchieved or ChallengeResult.Completed);
    }

    [Fact]
    public void OnCreatureKilled_IgnoresNonMatchingTargetGroupMember()
    {
        const uint targetGroupId = 1852u;
        GameTableManager gameTables = CreateCombatGameTables(
            target: targetGroupId,
            new TargetGroupEntry
            {
                Id          = targetGroupId,
                Type        = (uint)TargetGroupType.CreatureIdGroup,
                DataEntries = [12212u, 13549u, 12213u, 0u, 0u, 0u, 0u]
            });
        IPlayer player = CreatePlayer(9001u, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        player.ChallengeManager.HandleChoice(CombatChallengeId, ChallengeChoice.Activate);

        ChallengeCombatHooks.OnCreatureKilled(player, TargetCreatureId);

        Assert.DoesNotContain(
            GetMessages<ServerChallengeResult>(sessionProxy),
            result => result.Result is ChallengeResult.TierAchieved or ChallengeResult.Completed);
    }

    [Fact]
    public void OnCreatureKilled_AutoActivatesFlaggedCombatChallengeAndCreditsKill()
    {
        GameTableManager gameTables = CreateCombatGameTablesWithFlags(AutoActivateOnProgressFlag);
        IPlayer player = CreatePlayer(9001u, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);

        ChallengeCombatHooks.OnCreatureKilled(player, TargetCreatureId);

        ServerChallengeResult activate = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Activate);
        Assert.Equal(CombatChallengeId, activate.ChallengeId);

        ServerChallengeResult completed = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Completed);
        Assert.Equal(CombatChallengeId, completed.ChallengeId);
    }

    [Fact]
    public void OnCreatureKilled_DoesNotAutoActivateUnflaggedCombatChallenge()
    {
        GameTableManager gameTables = CreateCombatGameTablesWithFlags(0u);
        IPlayer player = CreatePlayer(9001u, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);

        ChallengeCombatHooks.OnCreatureKilled(player, TargetCreatureId);

        Assert.Empty(GetMessages<ServerChallengeResult>(sessionProxy));
        Assert.Empty(GetMessages<ServerChallengeUpdate>(sessionProxy));
    }

    [Fact]
    public void Update_DuringActiveTimer_DoesNotResendUnchangedChallengeState()
    {
        GameTableManager gameTables = CreateCombatGameTables();
        IPlayer player = CreatePlayer(9001u, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        player.ChallengeManager.HandleChoice(CombatChallengeId, ChallengeChoice.Activate);
        int updateCount = GetMessages<ServerChallengeUpdate>(sessionProxy).Count;

        player.ChallengeManager.Update(1d);

        Assert.Equal(updateCount, GetMessages<ServerChallengeUpdate>(sessionProxy).Count);
    }

    [Fact]
    public void Update_WhenCooldownExpires_SendsUpdatedChallengeState()
    {
        GameTableManager gameTables = CreateCombatGameTables();
        IPlayer player = CreatePlayer(9001u, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        player.ChallengeManager.HandleChoice(CombatChallengeId, ChallengeChoice.Activate);
        ChallengeCombatHooks.OnCreatureKilled(player, TargetCreatureId);
        int updateCount = GetMessages<ServerChallengeUpdate>(sessionProxy).Count;

        player.ChallengeManager.Update(1800d);

        IReadOnlyList<ServerChallengeUpdate> updates = GetMessages<ServerChallengeUpdate>(sessionProxy);
        Assert.Equal(updateCount + 1, updates.Count);
        ServerChallengeUpdate.Challenge row = Assert.Single(updates.Last().ActiveChallenges);
        Assert.False(row.OnCooldown);
        Assert.Equal(0u, row.TimeCooldownDt);
    }

    private const uint CooldownTypeFlag = 0x10u;

    private static GameTableManager CreateCombatGameTables(uint target = TargetCreatureId, params TargetGroupEntry[] targetGroups)
    {
        return CreateCombatGameTablesWithFlags(CooldownTypeFlag, target, targetGroups);
    }

    private static GameTableManager CreateCombatGameTablesWithFlags(uint challengeFlags, uint target = TargetCreatureId, params TargetGroupEntry[] targetGroups)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.ChallengeTier), CreateGameTable(new ChallengeTierEntry
        {
            Id    = 2001,
            Count = 1u
        }));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.Challenge), CreateGameTable(new ChallengeEntry
        {
            Id                      = CombatChallengeId,
            ChallengeTypeEnum       = (uint)ChallengeType.Combat,
            Target                  = target,
            ChallengeFlags          = challengeFlags,
            TargetGroupIdRewardPane = 42u,
            ChallengeTierId00       = 2001,
            CompletionCount         = 1u
        }));

        SetAutoProperty(gameTableManager, nameof(GameTableManager.TargetGroup), CreateGameTable(targetGroups));
        return gameTableManager;
    }

    private static IPlayer CreatePlayer(
        uint guid,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        GameTableManager gameTables)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out _);

        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), new ChallengeManager(player, gameTables));

        return player;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<T>()
            .ToList();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static void SetAutoProperty(object target, string propertyName, object value)
    {
        FieldInfo backingField = target.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(target, value);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(target, value);
    }
}
