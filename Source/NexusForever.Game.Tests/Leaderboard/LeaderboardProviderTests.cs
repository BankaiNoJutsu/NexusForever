using Microsoft.Extensions.Logging.Abstractions;
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
