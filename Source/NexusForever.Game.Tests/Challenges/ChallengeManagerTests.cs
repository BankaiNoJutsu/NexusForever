using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Challenges;
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

public class ChallengeManagerTests
{
    private const ushort ChallengeId = 1001;
    private const uint SharerGuid = 5001u;
    private const uint RecipientGuid = 5002u;

    [Fact]
    public void Activate_SendsActivateResultAndChallengeUpdate()
    {
        GameTableManager gameTables = CreateGameTables();
        IPlayer player = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        IChallengeManager manager = new ChallengeManager(player, gameTables);

        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);

        ServerChallengeResult result = Assert.Single(GetMessages<ServerChallengeResult>(sessionProxy));
        Assert.Equal(ChallengeId, result.ChallengeId);
        Assert.Equal(ChallengeResult.Activate, result.Result);

        ServerChallengeUpdate update = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy));
        ServerChallengeUpdate.Challenge row = Assert.Single(update.ActiveChallenges);
        Assert.Equal((uint)ChallengeId, row.ChallengeId);
        Assert.True(row.Activated);
        Assert.Equal(300_000u, row.TimeActivatedDt);
        Assert.Equal(300_000u, row.TimeTotalActive);
    }

    [Fact]
    public void Activate_WhenChallengeTableMissing_SendsGenericFail()
    {
        GameTableManager gameTables = CreateEmptyGameTables();
        IPlayer player = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        IChallengeManager manager = new ChallengeManager(player, gameTables);

        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);

        ServerChallengeResult result = Assert.Single(GetMessages<ServerChallengeResult>(sessionProxy));
        Assert.Equal(ChallengeId, result.ChallengeId);
        Assert.Equal(ChallengeResult.GenericFail, result.Result);
        Assert.Empty(GetMessages<ServerChallengeUpdate>(sessionProxy));
    }

    [Fact]
    public void Activate_WhenChallengeTierTableMissing_SendsUpdateWithZeroGoal()
    {
        GameTableManager gameTables = CreateGameTablesWithoutChallengeTiers();
        IPlayer player = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        IChallengeManager manager = new ChallengeManager(player, gameTables);

        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);

        ServerChallengeResult result = Assert.Single(GetMessages<ServerChallengeResult>(sessionProxy));
        Assert.Equal(ChallengeResult.Activate, result.Result);

        ServerChallengeUpdate.Challenge row = Assert.Single(Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy)).ActiveChallenges);
        Assert.Equal(0u, row.GoalCount);
        Assert.Equal([0u, 0u, 0u], row.TierGoalCount);
    }

    [Fact]
    public void Activate_ThirdConcurrentChallenge_ReturnsGenericFail()
    {
        const ushort challengeTwoId = 1002;
        const ushort challengeThreeId = 1003;
        GameTableManager gameTables = CreateGameTablesWithMultipleTypes(
            (ChallengeId, ChallengeType.General),
            (challengeTwoId, ChallengeType.Combat),
            (challengeThreeId, ChallengeType.Item));
        IPlayer player = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        IChallengeManager manager = new ChallengeManager(player, gameTables);

        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        manager.HandleChoice(challengeTwoId, ChallengeChoice.Activate);
        manager.HandleChoice(challengeThreeId, ChallengeChoice.Activate);

        ServerChallengeResult failure = GetMessages<ServerChallengeResult>(sessionProxy).Last();
        Assert.Equal(challengeThreeId, failure.ChallengeId);
        Assert.Equal(ChallengeResult.GenericFail, failure.Result);
        Assert.Equal(2, GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges.Count(c => c.Activated));
    }

    [Fact]
    public void Activate_WhenTypeAlreadyActive_SendsTypeAlreadyActive()
    {
        GameTableManager gameTables = CreateGameTables();
        IPlayer player = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        IChallengeManager manager = new ChallengeManager(player, gameTables);

        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);

        ServerChallengeResult failure = GetMessages<ServerChallengeResult>(sessionProxy).Last();
        Assert.Equal(ChallengeResult.TypeAlreadyActive, failure.Result);
    }

    [Fact]
    public void Abandon_ActiveChallenge_SendsAbandonRemoveAndClearsActivation()
    {
        GameTableManager gameTables = CreateGameTables();
        IPlayer player = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables);
        IChallengeManager manager = new ChallengeManager(player, gameTables);

        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        manager.HandleChoice(ChallengeId, ChallengeChoice.Abandon);

        ServerChallengeResult result = GetMessages<ServerChallengeResult>(sessionProxy).Last();
        Assert.Equal(ChallengeResult.AbandonRemove, result.Result);

        ServerChallengeUpdate update = GetMessages<ServerChallengeUpdate>(sessionProxy).Last();
        Assert.False(Assert.Single(update.ActiveChallenges).Activated);
    }

    [Fact]
    public void AcceptShared_ActivatesPendingChallenge()
    {
        GameTableManager gameTables = CreateGameTables();
        IPlayer recipient = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> recipientSession, gameTables, sharedChallengeEnabled: true);
        IChallengeManager recipientManager = new ChallengeManager(recipient, gameTables);

        recipientManager.ReceiveShare(ChallengeId, SharerGuid);
        recipientManager.HandleChoice(ChallengeId, ChallengeChoice.AcceptShared);

        ServerChallengeResult activate = GetMessages<ServerChallengeResult>(recipientSession)
            .Single(result => result.Result == ChallengeResult.Activate);
        Assert.Equal(ChallengeId, activate.ChallengeId);
    }

    [Fact]
    public void DeclineShared_ClearsPendingWithoutActivateResult()
    {
        GameTableManager gameTables = CreateGameTables();
        IPlayer recipient = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> recipientSession, gameTables, sharedChallengeEnabled: true);
        IChallengeManager recipientManager = new ChallengeManager(recipient, gameTables);

        recipientManager.ReceiveShare(ChallengeId, SharerGuid);
        recipientManager.HandleChoice(ChallengeId, ChallengeChoice.DeclineShared);

        Assert.DoesNotContain(
            GetMessages<ServerChallengeResult>(recipientSession),
            result => result.Result == ChallengeResult.Activate);
    }

    [Fact]
    public void PendingShare_TimesOutWithShareTimeoutPacket()
    {
        GameTableManager gameTables = CreateGameTables();
        IPlayer recipient = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> recipientSession, gameTables, sharedChallengeEnabled: true);
        IChallengeManager recipientManager = new ChallengeManager(recipient, gameTables);

        recipientManager.ReceiveShare(ChallengeId, SharerGuid);
        recipientManager.Update(30d);

        ServerChallengeShareTimeout timeout = Assert.Single(GetMessages<ServerChallengeShareTimeout>(recipientSession));
        Assert.Equal(ChallengeId, timeout.ChallengeId);
    }

    [Fact]
    public void TryAdvanceProgress_EmitsTierAchievedThenCompleted()
    {
        GameTableManager gameTables = CreateGameTables(tierOneCount: 1, tierTwoCount: 1);
        IPlayer player = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables, out RecordingDispatchProxy<IQuestManager> questProxy);
        var manager = new ChallengeManager(player, gameTables);

        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        Assert.True(manager.TryAdvanceProgress(ChallengeId));

        ServerChallengeResult tierResult = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.TierAchieved);
        Assert.Equal(0, tierResult.Data);

        Assert.True(manager.TryAdvanceProgress(ChallengeId));

        ServerChallengeResult completed = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Completed);
        Assert.Equal(1, completed.Data);

        RecordingDispatchProxy<IQuestManager>.Invocation questUpdate = Assert.Single(
            questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.CompleteChallenge, questUpdate.Arguments[0]);
        Assert.Equal(0u, questUpdate.Arguments[1]);
        Assert.Equal(1u, questUpdate.Arguments[2]);
    }

    [Fact]
    public void TryAdvanceProgress_ThroughPublicHook_CompletesActiveChallenge()
    {
        GameTableManager gameTables = CreateGameTables(tierOneCount: 1);
        IPlayer player = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables, out RecordingDispatchProxy<IQuestManager> questProxy);
        IChallengeManager manager = player.ChallengeManager;

        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        Assert.True(ChallengeProgressHooks.OnChallengeProgress(player, ChallengeId));

        ServerChallengeResult completed = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Completed);
        Assert.Equal(ChallengeId, completed.ChallengeId);

        RecordingDispatchProxy<IQuestManager>.Invocation questUpdate = Assert.Single(
            questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.CompleteChallenge, questUpdate.Arguments[0]);
    }

    [Fact]
    public void TryAdvanceProgress_WhenChallengeInactive_ReturnsFalseWithoutPackets()
    {
        GameTableManager gameTables = CreateGameTables(tierOneCount: 1);
        IPlayer player = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables, out RecordingDispatchProxy<IQuestManager> questProxy);
        IChallengeManager manager = player.ChallengeManager;

        Assert.False(manager.TryAdvanceProgress(ChallengeId));

        Assert.Empty(GetMessages<ServerChallengeResult>(sessionProxy));
        Assert.Empty(GetMessages<ServerChallengeUpdate>(sessionProxy));
        Assert.Empty(questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void TryAdvanceProgress_LargeProgressClampsAtGoalWithoutWrapping()
    {
        GameTableManager gameTables = CreateGameTables(tierOneCount: uint.MaxValue);
        IPlayer player = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> sessionProxy, gameTables, out RecordingDispatchProxy<IQuestManager> questProxy);
        var manager = new ChallengeManager(player, gameTables);

        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        Assert.True(manager.TryAdvanceProgress(ChallengeId, uint.MaxValue - 1u));
        Assert.True(manager.TryAdvanceProgress(ChallengeId, 10u));

        ServerChallengeResult completed = GetMessages<ServerChallengeResult>(sessionProxy)
            .Single(result => result.Result == ChallengeResult.Completed);
        Assert.Equal(0, completed.Data);

        ServerChallengeUpdate.Challenge finalRow = Assert.Single(GetMessages<ServerChallengeUpdate>(sessionProxy).Last().ActiveChallenges);
        Assert.False(finalRow.Activated);
        Assert.True(finalRow.OnCooldown);
        Assert.Equal(uint.MaxValue, finalRow.CurrentCount);
        Assert.Equal(1_800_000u, finalRow.TimeCooldownDt);
        Assert.Equal(1_800_000u, finalRow.TimeTotalCooldown);

        RecordingDispatchProxy<IQuestManager>.Invocation questUpdate = Assert.Single(
            questProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(QuestObjectiveType.CompleteChallenge, questUpdate.Arguments[0]);
    }

    [Fact]
    public void ShareWithTarget_SendsSharedInvitationToRecipient()
    {
        GameTableManager gameTables = CreateGameTables();
        IPlayer recipient = CreatePlayer(RecipientGuid, out RecordingDispatchProxy<IGameSession> recipientSession, gameTables, sharedChallengeEnabled: true);
        IPlayer sharer = CreatePlayer(SharerGuid, out RecordingDispatchProxy<IGameSession> _, gameTables, targetGuid: RecipientGuid);
        LinkVisibleTarget(sharer, recipient);

        sharer.ChallengeManager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        sharer.ChallengeManager.ShareWithTarget(ChallengeId);

        ServerChallengeShared shared = Assert.Single(GetMessages<ServerChallengeShared>(recipientSession));
        Assert.Equal(ChallengeId, shared.ChallengeId);
        Assert.Equal(SharerGuid, shared.SharerUnitId);
    }

    private static void LinkVisibleTarget(IPlayer sharer, IPlayer recipient)
    {
        RecordingDispatchProxy<IPlayer> sharerProxy = (RecordingDispatchProxy<IPlayer>)(object)sharer;
        sharerProxy.SetMethodReturnFactory(nameof(IPlayer.GetVisible), () => recipient);
    }

    private static GameTableManager CreateGameTablesWithMultipleTypes(params (ushort Id, ChallengeType Type)[] challenges)
    {
        GameTableManager gameTableManager = CreateEmptyGameTables();

        SetAutoProperty(gameTableManager, nameof(GameTableManager.ChallengeTier), CreateGameTable(new ChallengeTierEntry
        {
            Id    = 2001,
            Count = 5u
        }));

        var challengeEntries = challenges.Select(pair => new ChallengeEntry
        {
            Id                      = pair.Id,
            ChallengeTypeEnum       = (uint)pair.Type,
            ChallengeFlags          = CooldownTypeFlag,
            TargetGroupIdRewardPane = 42u,
            ChallengeTierId00       = 2001,
            CompletionCount         = 1u
        }).ToArray();

        SetAutoProperty(gameTableManager, nameof(GameTableManager.Challenge), CreateGameTable(challengeEntries));
        return gameTableManager;
    }

    private static GameTableManager CreateGameTables(uint tierOneCount = 5, uint tierTwoCount = 0)
    {
        GameTableManager gameTableManager = CreateEmptyGameTables();

        var tierEntries = new List<ChallengeTierEntry>
        {
            new()
            {
                Id    = 2001,
                Count = tierOneCount
            }
        };

        if (tierTwoCount > 0)
        {
            tierEntries.Add(new ChallengeTierEntry
            {
                Id    = 2002,
                Count = tierTwoCount
            });
        }

        SetAutoProperty(gameTableManager, nameof(GameTableManager.ChallengeTier), CreateGameTable(tierEntries.ToArray()));

        var challengeEntry = new ChallengeEntry
        {
            Id                      = ChallengeId,
            ChallengeTypeEnum       = (uint)ChallengeType.General,
            ChallengeFlags          = CooldownTypeFlag,
            TargetGroupIdRewardPane = 42u,
            ChallengeTierId00       = 2001,
            CompletionCount         = 1u
        };

        if (tierTwoCount > 0)
            challengeEntry.ChallengeTierId01 = 2002;

        SetAutoProperty(gameTableManager, nameof(GameTableManager.Challenge), CreateGameTable(challengeEntry));

        return gameTableManager;
    }

    private static GameTableManager CreateGameTablesWithoutChallengeTiers()
    {
        GameTableManager gameTableManager = CreateEmptyGameTables();

        SetAutoProperty(gameTableManager, nameof(GameTableManager.Challenge), CreateGameTable(new ChallengeEntry
        {
            Id                      = ChallengeId,
            ChallengeTypeEnum       = (uint)ChallengeType.General,
            ChallengeFlags          = CooldownTypeFlag,
            TargetGroupIdRewardPane = 42u,
            ChallengeTierId00       = 2001,
            CompletionCount         = 1u
        }));

        return gameTableManager;
    }

    private static GameTableManager CreateEmptyGameTables()
    {
        return new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
    }

    private const uint CooldownTypeFlag = 0x10u;

    private static IPlayer CreatePlayer(
        uint guid,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        GameTableManager gameTables,
        out RecordingDispatchProxy<IQuestManager> questProxy,
        bool sharedChallengeEnabled = false,
        uint? targetGuid = null)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questProxy);

        playerProxy.SetProperty(nameof(IPlayer.Guid), guid);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.SharedChallengeEnabled), sharedChallengeEnabled);
        playerProxy.SetProperty(nameof(IPlayer.TargetGuid), targetGuid);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), new ChallengeManager(player, gameTables));

        return player;
    }

    private static IPlayer CreatePlayer(
        uint guid,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        GameTableManager gameTables,
        bool sharedChallengeEnabled = false,
        uint? targetGuid = null)
    {
        return CreatePlayer(guid, out sessionProxy, gameTables, out _, sharedChallengeEnabled, targetGuid);
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
        FieldInfo field = target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(target, value);
    }
}
