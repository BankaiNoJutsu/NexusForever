using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges;
using NexusForever.Game.Static.Challenges;
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

    private const uint CooldownTypeFlag = 0x10u;

    private static GameTableManager CreateCombatGameTables()
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
            Target                  = TargetCreatureId,
            ChallengeFlags          = CooldownTypeFlag,
            TargetGroupIdRewardPane = 42u,
            ChallengeTierId00       = 2001,
            CompletionCount         = 1u
        }));

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
