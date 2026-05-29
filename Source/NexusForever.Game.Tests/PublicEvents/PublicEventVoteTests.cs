using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PublicEvent;
using PublicEventTeamId = NexusForever.Game.Static.PublicEvent.PublicEventTeam;

namespace NexusForever.Game.Tests.PublicEvents;

public class PublicEventVoteTests
{
    [Fact]
    public void Initialise_BroadcastsDetailedVoteInitiate()
    {
        VoteHarness harness = CreateHarness([101ul], durationMs: 30000u);

        harness.Vote.Initialise(harness.Team, voteId: 64u, defaultChoice: 1u);

        ServerPublicEventDetailedVoteInitiate initiate = Assert.IsType<ServerPublicEventDetailedVoteInitiate>(Assert.Single(harness.Broadcasts));
        Assert.Equal(9001u, initiate.EventId);
        Assert.Equal(64u, initiate.VoteId);
        Assert.Equal(PublicEventTeamId.PublicTeam, initiate.TeamId);
        Assert.True(initiate.CanPlayerVote);
        Assert.Equal(0u, initiate.ElapsedTimeMs);
        Assert.Empty(initiate.Tallies);
    }

    [Fact]
    public void Choice_BroadcastsTallyAndEndWhenAllMembersVote()
    {
        VoteHarness harness = CreateHarness([101ul, 102ul], durationMs: 30000u);
        harness.Vote.Initialise(harness.Team, voteId: 64u, defaultChoice: 1u);

        harness.Vote.Choice(101ul, 2u);
        harness.Vote.Choice(102ul, 2u);

        Assert.Collection(harness.Broadcasts,
            message => Assert.IsType<ServerPublicEventDetailedVoteInitiate>(message),
            message => AssertVoteTally(message, choice: 2u),
            message => AssertVoteTally(message, choice: 2u),
            message => AssertVoteEnd(message, winner: 2u));
        Assert.True(harness.Vote.IsFinalised);
        Assert.Single(harness.EventProxy.GetInvocations(nameof(IPublicEvent.InvokeScriptCollection)));
    }

    [Fact]
    public void Choice_IgnoresDuplicateInvalidAndUnknownResponses()
    {
        VoteHarness harness = CreateHarness([101ul, 102ul], durationMs: 30000u);
        harness.Vote.Initialise(harness.Team, voteId: 64u, defaultChoice: 1u);

        harness.Vote.Choice(999ul, 2u);
        harness.Vote.Choice(101ul, 99u);
        harness.Vote.Choice(101ul, 2u);
        harness.Vote.Choice(101ul, 3u);
        harness.Vote.Choice(102ul, 3u);
        harness.Vote.Choice(102ul, 2u);

        Assert.Collection(harness.Broadcasts,
            message => Assert.IsType<ServerPublicEventDetailedVoteInitiate>(message),
            message => AssertVoteTally(message, choice: 2u),
            message => AssertVoteTally(message, choice: 3u),
            message => AssertVoteEnd(message, winner: 2u));
    }

    [Fact]
    public void Update_UsesDefaultChoiceWhenVoteTimesOutWithoutResponses()
    {
        VoteHarness harness = CreateHarness([101ul, 102ul], durationMs: 1000u);
        harness.Vote.Initialise(harness.Team, voteId: 64u, defaultChoice: 3u);

        harness.Vote.Update(1.0d);

        Assert.Collection(harness.Broadcasts,
            message => Assert.IsType<ServerPublicEventDetailedVoteInitiate>(message),
            message => AssertVoteEnd(message, winner: 3u));
        Assert.True(harness.Vote.IsFinalised);
    }

    [Fact]
    public void TeamRespondVote_IgnoresMismatchedVoteId()
    {
        TeamHarness harness = CreateTeamHarness([101ul], durationMs: 30000u);
        harness.Team.StartVote(voteId: 64u, defaultChoice: 1u);

        harness.Team.RespondVote(harness.Players[101ul], voteId: 65u, choice: 2u);
        harness.Team.RespondVote(harness.Players[101ul], voteId: 64u, choice: 2u);

        Assert.Collection(harness.Broadcasts,
            message => Assert.IsType<ServerPublicEventDetailedVoteInitiate>(message),
            message => AssertVoteTally(message, choice: 2u),
            message => AssertVoteEnd(message, winner: 2u));
    }

    private static void AssertVoteTally(IWritable message, uint choice)
    {
        ServerPublicEventVoteTally tally = Assert.IsType<ServerPublicEventVoteTally>(message);
        Assert.Equal(9001u, tally.EventId);
        Assert.Equal(64u, tally.VoteId);
        Assert.Equal(choice, tally.Choice);
    }

    private static void AssertVoteEnd(IWritable message, uint winner)
    {
        ServerPublicEventVoteEnd end = Assert.IsType<ServerPublicEventVoteEnd>(message);
        Assert.Equal(9001u, end.EventId);
        Assert.Equal(64u, end.VoteId);
        Assert.Equal(winner, end.Winner);
    }

    private static VoteHarness CreateHarness(ulong[] characterIds, uint durationMs)
    {
        GameTable<PublicEventVoteEntry> voteTable = CreateGameTable(new PublicEventVoteEntry
        {
            Id = 64u,
            LocalizedTextIdOption = [0u, 1001u, 1002u, 1003u, 0u],
            LocalizedTextIdLabel  = [0u, 2001u, 2002u, 2003u, 0u],
            DurationMS = durationMs
        });

        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.PublicEventVote), voteTable);

        var broadcasts = new List<IWritable>();
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Id), 9001u);

        IPublicEventTeam team = RecordingDispatchProxy<IPublicEventTeam>.Create(out RecordingDispatchProxy<IPublicEventTeam> teamProxy);
        teamProxy.SetProperty(nameof(IPublicEventTeam.PublicEvent), publicEvent);
        teamProxy.SetProperty(nameof(IPublicEventTeam.Team), PublicEventTeamId.PublicTeam);
        teamProxy.SetMethodReturn(nameof(IPublicEventTeam.GetMembers), characterIds.Select(CreateMember).ToArray());
        teamProxy.SetMethodHandler(nameof(IPublicEventTeam.Broadcast), args =>
        {
            broadcasts.Add((IWritable)args[0]);
            return null;
        });

        var vote = new PublicEventVote(
            gameTableManager,
            new PublicEventTestSupport.DelegateFactory<IPublicEventVoteResponse>(() => new PublicEventVoteResponse(NullLogger<PublicEventVoteResponse>.Instance)));

        return new VoteHarness(vote, team, eventProxy, broadcasts);
    }

    private static TeamHarness CreateTeamHarness(ulong[] characterIds, uint durationMs)
    {
        GameTable<PublicEventVoteEntry> voteTable = CreateGameTable(new PublicEventVoteEntry
        {
            Id = 64u,
            LocalizedTextIdOption = [0u, 1001u, 1002u, 1003u, 0u],
            LocalizedTextIdLabel  = [0u, 2001u, 2002u, 2003u, 0u],
            DurationMS = durationMs
        });

        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.PublicEventVote), voteTable);

        var broadcasts = new List<IWritable>();
        var players = new Dictionary<ulong, IPlayer>();
        Queue<IPublicEventTeamMember> members = new(characterIds.Select(characterId => CreateRecordingMember(characterId, broadcasts)));

        var team = new NexusForever.Game.PublicEvent.PublicEventTeam(
            NullLogger<NexusForever.Game.PublicEvent.PublicEventTeam>.Instance,
            new PublicEventTestSupport.ThrowingFactory<IPublicEventObjective>(),
            new PublicEventTestSupport.DelegateFactory<IPublicEventTeamMember>(() => members.Dequeue()),
            new PublicEventTestSupport.DelegateFactory<IPublicEventVote>(() => new PublicEventVote(
                gameTableManager,
                new PublicEventTestSupport.DelegateFactory<IPublicEventVoteResponse>(() => new PublicEventVoteResponse(NullLogger<PublicEventVoteResponse>.Instance)))),
            new PublicEventStats());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out RecordingDispatchProxy<IPublicEvent> publicEventProxy);
        publicEventProxy.SetProperty(nameof(IPublicEvent.Id), 9001u);

        var template = new VoteOnlyTemplate(PublicEventTeamId.PublicTeam);
        team.Initialise(publicEvent, template, template.Teams.Single());

        foreach (ulong characterId in characterIds)
        {
            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
            playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
            players.Add(characterId, player);
            team.JoinTeam(player);
        }

        return new TeamHarness(team, players, broadcasts);
    }

    private static IPublicEventTeamMember CreateMember(ulong characterId)
    {
        IPublicEventTeamMember member = RecordingDispatchProxy<IPublicEventTeamMember>.Create(out RecordingDispatchProxy<IPublicEventTeamMember> memberProxy);
        memberProxy.SetProperty(nameof(IPublicEventTeamMember.CharacterId), characterId);
        return member;
    }

    private static IPublicEventTeamMember CreateRecordingMember(ulong characterId, List<IWritable> broadcasts)
    {
        IPublicEventTeamMember member = RecordingDispatchProxy<IPublicEventTeamMember>.Create(out RecordingDispatchProxy<IPublicEventTeamMember> memberProxy);
        memberProxy.SetMethodHandler(nameof(IPublicEventTeamMember.Initialise), args =>
        {
            memberProxy.SetProperty(nameof(IPublicEventTeamMember.CharacterId), characterId);
            return null;
        });
        memberProxy.SetMethodHandler(nameof(IPublicEventTeamMember.Send), args =>
        {
            broadcasts.Add((IWritable)args[0]);
            return null;
        });
        return member;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));

        typeof(GameTable<T>)
            .GetProperty(nameof(GameTable<T>.Entries), BindingFlags.Instance | BindingFlags.Public)
            ?.SetValue(table, entries);

        ulong maxId = entries
            .Select(entry => Convert.ToUInt64(typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)))
            .DefaultIfEmpty(0ul)
            .Max() + 1ul;

        var lookup = Enumerable.Repeat(-1, (int)maxId).ToArray();
        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        for (int i = 0; i < entries.Length; i++)
        {
            ulong id = Convert.ToUInt64(idField.GetValue(entries[i]));
            lookup[id] = i;
        }

        typeof(GameTable<T>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        typeof(GameTable<T>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = maxId });

        return table;
    }

    private sealed record VoteHarness(
        PublicEventVote Vote,
        IPublicEventTeam Team,
        RecordingDispatchProxy<IPublicEvent> EventProxy,
        List<IWritable> Broadcasts);

    private sealed record TeamHarness(
        NexusForever.Game.PublicEvent.PublicEventTeam Team,
        Dictionary<ulong, IPlayer> Players,
        List<IWritable> Broadcasts);

    private sealed class VoteOnlyTemplate(PublicEventTeamId teamId) : IPublicEventTemplate
    {
        public PublicEventEntry Entry { get; } = new()
        {
            Id = 9001
        };

        public Dictionary<uint, PublicEventObjectiveEntry> Objectives { get; } = [];

        public List<PublicEventTeamEntry> Teams { get; } =
        [
            new PublicEventTeamEntry
            {
                Id = teamId
            }
        ];

        public List<PublicEventCustomStatEntry> CustomStats { get; } = [];

        public void Initialise(PublicEventEntry entry) => throw new NotSupportedException();

        public bool HasLiveStats() => false;
    }
}
