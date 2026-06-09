using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Leaderboard;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Leaderboard;
using NexusForever.WorldServer.Leaderboard;

namespace NexusForever.Game.Tests.Leaderboard;

public class LeaderboardAggregationTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 5, 22, 14, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Provider_BuildPve_ReturnsScopedRankedRowsAndNextUpdateTime()
    {
        var store = new InMemoryLeaderboardStore();
        var provider = new LeaderboardProvider(store, () => FixedUtcNow);
        ClientLeaderboardPveRequest request = CreatePveRequest(LeaderboardType.PveDungeon, 1234u, 5u);

        ServerLeaderboardPve response = provider.BuildPve(request);

        Assert.Equal(LeaderboardType.PveDungeon, response.Type);
        Assert.Equal(1234u, response.MatchingGameMapId);
        Assert.Equal(5u, response.PrimeLevel);
        Assert.Equal(LeaderboardAggregation.ComputeNextHourFileTimeUtc(FixedUtcNow), response.NextUpdateTime);
        Assert.Equal(2, response.Players.Count);
        Assert.Equal("Archive Gold", response.Players[0].Name);
        Assert.Equal(1u, response.Players[0].Rank);
        Assert.Equal(412000u, response.Players[0].CompletionTime);
        Assert.Equal("Prime Runner", response.Players[1].Name);
        Assert.Equal(2u, response.Players[1].Rank);
    }

    [Fact]
    public void Provider_BuildPvp_ReturnsArenaTeamsSortedByRating()
    {
        var store = new InMemoryLeaderboardStore();
        var provider = new LeaderboardProvider(store, () => FixedUtcNow);
        ClientLeaderboardPvpRequest request = CreatePvpRequest(LeaderboardType.Arena3v3);

        ServerLeaderboardPvp response = provider.BuildPvp(request);

        Assert.Equal(LeaderboardType.Arena3v3, response.Type);
        Assert.Equal(LeaderboardAggregation.ComputeNextHourFileTimeUtc(FixedUtcNow), response.NextUpdateTime);
        Assert.Equal(2, response.Players.Count);
        Assert.Equal("Rated Team", response.Players[0].Name);
        Assert.Equal(Class.PvpTeam, response.Players[0].Class);
        Assert.Equal(2100u, response.Players[0].Rating);
        Assert.Equal(1u, response.Players[0].Rank);
        Assert.Equal(3, response.Players[0].TeamMembers.Count);
    }

    [Fact]
    public void Provider_BuildPve_IncludesViewerPersonalPlacementWhenOutsideTopRows()
    {
        var store = new InMemoryLeaderboardStore();
        var provider = new LeaderboardProvider(store, () => FixedUtcNow);
        IPlayer viewer = CreateViewer(9001ul, "Outside Top", Class.Engineer);
        ClientLeaderboardPveRequest request = CreatePveRequest(LeaderboardType.PveDungeon, 1234u, 5u);

        ServerLeaderboardPve response = provider.BuildPve(request, viewer);

        LeaderboardPlayerPve personalRow = response.Players.Single(player => player.Name == "Outside Top");
        Assert.True(personalRow.Rank > (uint)response.Players.Count - 1);
        Assert.Equal(Class.Engineer, personalRow.Class);
        Assert.Single(personalRow.TeamMembers);
    }

    [Fact]
    public void Provider_BuildPve_DeduplicatesCharacterScoresUsingBestCompletion()
    {
        var store = new InMemoryLeaderboardStore(
            [
                CreatePveRecord(1000ul, "Runner", 600u),
                CreatePveRecord(1000ul, "Runner", 400u),
                CreatePveRecord(1001ul, "Other", 500u)
            ],
            []);
        var provider = new LeaderboardProvider(store, () => FixedUtcNow);

        ServerLeaderboardPve response = provider.BuildPve(CreatePveRequest(LeaderboardType.PveDungeon, 1234u, 5u));

        Assert.Equal(2, response.Players.Count);
        Assert.Equal("Runner", response.Players[0].Name);
        Assert.Equal(400u, response.Players[0].CompletionTime);
        Assert.Equal(1u, response.Players[0].Rank);
        Assert.Equal("Other", response.Players[1].Name);
    }

    [Fact]
    public void Provider_BuildPvp_DeduplicatesCharacterScoresUsingBestRating()
    {
        var store = new InMemoryLeaderboardStore(
            [],
            [
                CreatePvpRecord(2000ul, "Arena Team", 1200u),
                CreatePvpRecord(2000ul, "Arena Team", 1500u),
                CreatePvpRecord(2001ul, "Other Team", 1300u)
            ]);
        var provider = new LeaderboardProvider(store, () => FixedUtcNow);

        ServerLeaderboardPvp response = provider.BuildPvp(CreatePvpRequest(LeaderboardType.Arena3v3));

        Assert.Equal(2, response.Players.Count);
        Assert.Equal("Arena Team", response.Players[0].Name);
        Assert.Equal(1500u, response.Players[0].Rating);
        Assert.Equal(1u, response.Players[0].Rank);
        Assert.Equal("Other Team", response.Players[1].Name);
    }

    [Fact]
    public void CategoryRules_BattlegroundFiltersByClass()
    {
        var store = new InMemoryLeaderboardStore();
        var provider = new LeaderboardProvider(store, () => FixedUtcNow);

        ServerLeaderboardPvp response = provider.BuildPvp(CreatePvpRequest(LeaderboardType.BattlegroundMedic));

        Assert.Single(response.Players);
        Assert.Equal("BG Medic", response.Players[0].Name);
        Assert.Equal(Class.Medic, response.Players[0].Class);
    }

    private static IPlayer CreateViewer(ulong characterId, string name, Class playerClass)
    {
        IGuildManager guildManager = RecordingDispatchProxy<IGuildManager>.Create(out RecordingDispatchProxy<IGuildManager> guildProxy);
        guildProxy.SetProperty(nameof(IGuildManager.GuildAffiliation), null);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.Name), name);
        playerProxy.SetProperty(nameof(IPlayer.Class), playerClass);
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);
        return player;
    }

    private static LeaderboardPveEntryRecord CreatePveRecord(ulong characterId, string name, uint completionTime)
    {
        return new LeaderboardPveEntryRecord
        {
            CharacterId       = characterId,
            Name              = name,
            Class             = Class.Esper,
            Type              = LeaderboardType.PveDungeon,
            MatchingGameMapId = 1234u,
            PrimeLevel        = 5u,
            CompletionTime    = completionTime,
            TeamMembers       = []
        };
    }

    private static LeaderboardPvpEntryRecord CreatePvpRecord(ulong characterId, string name, uint rating)
    {
        return new LeaderboardPvpEntryRecord
        {
            CharacterId = characterId,
            Name        = name,
            Class       = Class.PvpTeam,
            Type        = LeaderboardType.Arena3v3,
            Rating      = rating,
            TeamMembers = []
        };
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
}
