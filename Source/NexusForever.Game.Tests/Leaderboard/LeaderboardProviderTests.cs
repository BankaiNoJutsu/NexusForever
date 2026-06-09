using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Game.Abstract;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Leaderboard;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Leaderboard;
using NexusForever.WorldServer.Leaderboard;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Leaderboard;

namespace NexusForever.Game.Tests.Leaderboard;

public class LeaderboardProviderTests
{
    [Fact]
    public void EmptyProvider_BuildPveEchoesRequestScopeWithNoPlayers()
    {
        var provider = new EmptyLeaderboardProvider();
        ClientLeaderboardPveRequest request = CreatePveRequest(LeaderboardType.PveDungeon, 99u, 4u);

        ServerLeaderboardPve response = provider.BuildPve(request);

        Assert.Equal(LeaderboardType.PveDungeon, response.Type);
        Assert.Equal(99u, response.MatchingGameMapId);
        Assert.Equal(4u, response.PrimeLevel);
        Assert.Equal(0ul, response.NextUpdateTime);
        Assert.Empty(response.Players);
    }

    [Fact]
    public void EmptyProvider_BuildPvpEchoesRequestTypeWithNoPlayers()
    {
        var provider = new EmptyLeaderboardProvider();
        ClientLeaderboardPvpRequest request = CreatePvpRequest(LeaderboardType.Arena3v3);

        ServerLeaderboardPvp response = provider.BuildPvp(request);

        Assert.Equal(LeaderboardType.Arena3v3, response.Type);
        Assert.Equal(0ul, response.NextUpdateTime);
        Assert.Empty(response.Players);
    }

    [Fact]
    public void PveHandler_EnqueuesProviderResponse()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var response = new ServerLeaderboardPve
        {
            Type              = LeaderboardType.PveExpeditionSolo,
            MatchingGameMapId = 77u,
            PrimeLevel        = 1u
        };
        var provider = new RecordingLeaderboardProvider(pveResponse: response);
        var handler = new ClientLeaderboardPveRequestHandler(NullLogger<ClientLeaderboardPveRequestHandler>.Instance, provider);

        handler.HandleMessage(session, CreatePveRequest(LeaderboardType.PveExpeditionSolo, 77u, 1u));

        Assert.Same(response, GetEncryptedMessages(sessionProxy).Single());
        Assert.Equal(LeaderboardType.PveExpeditionSolo, provider.PveRequest.Type);
        Assert.Equal(77u, provider.PveRequest.MatchingGameMapdId);
        Assert.Equal(1u, provider.PveRequest.PrimeLevel);
    }

    [Fact]
    public void PvpHandler_EnqueuesProviderResponse()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var response = new ServerLeaderboardPvp
        {
            Type = LeaderboardType.BattlegroundMedic
        };
        var provider = new RecordingLeaderboardProvider(pvpResponse: response);
        var handler = new ClientLeaderboardPvpRequestHandler(NullLogger<ClientLeaderboardPvpRequestHandler>.Instance, provider);

        handler.HandleMessage(session, CreatePvpRequest(LeaderboardType.BattlegroundMedic));

        Assert.Same(response, GetEncryptedMessages(sessionProxy).Single());
        Assert.Equal(LeaderboardType.BattlegroundMedic, provider.PvpRequest.Type);
    }

    [Fact]
    public void DatabaseStoreLimitPveRowsPerScope_DoesNotLetBusyScopeHideOtherScopes()
    {
        IEnumerable<LeaderboardPveEntryRecord> entries = Enumerable.Range(0, 101)
            .Select(index => CreatePveRecord((ulong)(1000 + index), LeaderboardType.PveDungeon, 10u, 5u, (uint)(1000 + index), $"Busy {index:000}"))
            .Append(CreatePveRecord(9001ul, LeaderboardType.PveDungeon, 20u, 5u, 50_000u, "Other Scope"));

        IReadOnlyList<LeaderboardPveEntryRecord> limited = DatabaseLeaderboardStore.LimitPveRowsPerScope(entries);

        Assert.Equal(100, limited.Count(entry => entry.MatchingGameMapId == 10u));
        Assert.Contains(limited, entry => entry.MatchingGameMapId == 20u && entry.Name == "Other Scope");
    }

    [Fact]
    public void DatabaseStoreLimitPveRowsPerScope_KeepsBestScorePerCharacterBeforeLimit()
    {
        IEnumerable<LeaderboardPveEntryRecord> entries =
        [
            CreatePveRecord(1000ul, LeaderboardType.PveDungeon, 10u, 5u, 600u, "Runner"),
            CreatePveRecord(1000ul, LeaderboardType.PveDungeon, 10u, 5u, 400u, "Runner"),
            CreatePveRecord(1001ul, LeaderboardType.PveDungeon, 10u, 5u, 500u, "Other")
        ];

        IReadOnlyList<LeaderboardPveEntryRecord> limited = DatabaseLeaderboardStore.LimitPveRowsPerScope(entries);

        Assert.Equal(2, limited.Count);
        LeaderboardPveEntryRecord runner = Assert.Single(limited, entry => entry.CharacterId == 1000ul);
        Assert.Equal(400u, runner.CompletionTime);
    }

    [Fact]
    public void DatabaseStoreLimitPvpRowsPerCategory_DoesNotLetArenaRowsHideBattlegroundRows()
    {
        IEnumerable<LeaderboardPvpEntryRecord> entries = Enumerable.Range(0, 101)
            .Select(index => CreatePvpRecord((ulong)(2000 + index), LeaderboardType.Arena3v3, Class.PvpTeam, (uint)(3000 - index), $"Arena {index:000}"))
            .Append(CreatePvpRecord(9501ul, LeaderboardType.BattlegroundMedic, Class.Medic, 1200u, "Medic Board"));

        IReadOnlyList<LeaderboardPvpEntryRecord> limited = DatabaseLeaderboardStore.LimitPvpRowsPerCategory(entries);

        Assert.Equal(100, limited.Count(entry => entry.Type == LeaderboardType.Arena3v3));
        Assert.Contains(limited, entry => entry.Type == LeaderboardType.BattlegroundMedic && entry.Name == "Medic Board");
    }

    [Fact]
    public void DatabaseStoreLimitPvpRowsPerCategory_KeepsBestRatingPerCharacterBeforeLimit()
    {
        IEnumerable<LeaderboardPvpEntryRecord> entries =
        [
            CreatePvpRecord(2000ul, LeaderboardType.Arena3v3, Class.PvpTeam, 1200u, "Arena Team"),
            CreatePvpRecord(2000ul, LeaderboardType.Arena3v3, Class.PvpTeam, 1500u, "Arena Team"),
            CreatePvpRecord(2001ul, LeaderboardType.Arena3v3, Class.PvpTeam, 1300u, "Other Team")
        ];

        IReadOnlyList<LeaderboardPvpEntryRecord> limited = DatabaseLeaderboardStore.LimitPvpRowsPerCategory(entries);

        Assert.Equal(2, limited.Count);
        LeaderboardPvpEntryRecord team = Assert.Single(limited, entry => entry.CharacterId == 2000ul);
        Assert.Equal(1500u, team.Rating);
    }

    [Fact]
    public void DatabaseStore_WhenDatabaseUnavailable_ReturnsEmptyWithoutCaching()
    {
        IDatabaseManager databaseManager = RecordingDispatchProxy<IDatabaseManager>.Create(out RecordingDispatchProxy<IDatabaseManager> databaseProxy);
        databaseProxy.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), null);
        IRealmContext realmContext = RecordingDispatchProxy<IRealmContext>.Create(out RecordingDispatchProxy<IRealmContext> realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var store = new DatabaseLeaderboardStore(databaseManager, realmContext);

        Assert.Empty(store.GetPveEntries(LeaderboardType.PveDungeon, 10u, 0u));
        Assert.Empty(store.GetPvpEntries(LeaderboardType.Arena3v3));

        Assert.Null(GetPrivateField<IReadOnlyList<LeaderboardPveEntryRecord>>(store, "cachedPve"));
        Assert.Null(GetPrivateField<IReadOnlyList<LeaderboardPvpEntryRecord>>(store, "cachedPvp"));
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private static ClientLeaderboardPveRequest CreatePveRequest(LeaderboardType type, uint matchingGameMapId, uint primeLevel)
    {
        var request = (ClientLeaderboardPveRequest)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientLeaderboardPveRequest));
        typeof(ClientLeaderboardPveRequest).GetProperty(nameof(ClientLeaderboardPveRequest.Type))!.SetValue(request, type);
        typeof(ClientLeaderboardPveRequest).GetProperty(nameof(ClientLeaderboardPveRequest.MatchingGameMapdId))!.SetValue(request, matchingGameMapId);
        typeof(ClientLeaderboardPveRequest).GetProperty(nameof(ClientLeaderboardPveRequest.PrimeLevel))!.SetValue(request, primeLevel);
        return request;
    }

    private static ClientLeaderboardPvpRequest CreatePvpRequest(LeaderboardType type)
    {
        var request = (ClientLeaderboardPvpRequest)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientLeaderboardPvpRequest));
        typeof(ClientLeaderboardPvpRequest).GetProperty(nameof(ClientLeaderboardPvpRequest.Type))!.SetValue(request, type);
        return request;
    }

    private static LeaderboardPveEntryRecord CreatePveRecord(
        ulong characterId,
        LeaderboardType type,
        uint matchingGameMapId,
        uint primeLevel,
        uint completionTime,
        string name)
    {
        return new LeaderboardPveEntryRecord
        {
            CharacterId       = characterId,
            Name              = name,
            Class             = Class.Esper,
            Type              = type,
            MatchingGameMapId = matchingGameMapId,
            PrimeLevel        = primeLevel,
            CompletionTime    = completionTime,
            TeamMembers       = []
        };
    }

    private static LeaderboardPvpEntryRecord CreatePvpRecord(
        ulong characterId,
        LeaderboardType type,
        Class rowClass,
        uint rating,
        string name)
    {
        return new LeaderboardPvpEntryRecord
        {
            CharacterId = characterId,
            Name        = name,
            Class       = rowClass,
            Type        = type,
            Rating      = rating,
            TeamMembers = []
        };
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)field.GetValue(instance);
    }

    private sealed class RecordingLeaderboardProvider(
        ServerLeaderboardPve pveResponse = null,
        ServerLeaderboardPvp pvpResponse = null) : ILeaderboardProvider
    {
        public ClientLeaderboardPveRequest PveRequest { get; private set; }
        public ClientLeaderboardPvpRequest PvpRequest { get; private set; }

        public ServerLeaderboardPve BuildPve(ClientLeaderboardPveRequest request, NexusForever.Game.Abstract.Entity.IPlayer viewer = null)
        {
            PveRequest = request;
            return pveResponse;
        }

        public ServerLeaderboardPvp BuildPvp(ClientLeaderboardPvpRequest request, NexusForever.Game.Abstract.Entity.IPlayer viewer = null)
        {
            PvpRequest = request;
            return pvpResponse;
        }
    }
}
