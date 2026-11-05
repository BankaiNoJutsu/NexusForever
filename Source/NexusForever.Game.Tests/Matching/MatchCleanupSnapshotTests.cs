using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Matching;
using NexusForever.Game.Matching.Match;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Internal;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Matching;
using MatchStatus = NexusForever.Game.Static.Matching.MatchStatus;
using Role = NexusForever.Game.Static.Matching.Role;
using StaticMatchTeam = NexusForever.Game.Static.Matching.MatchTeam;

namespace NexusForever.Game.Tests.Matching;

public class MatchCleanupSnapshotTests
{
    [Fact]
    public void MatchCleanup_SnapshotsTeamMembersBeforeLeaveMutatesTeam()
    {
        Identity first = new()
        {
            RealmId = 1,
            Id      = 1000ul
        };
        Identity second = new()
        {
            RealmId = 1,
            Id      = 1001ul
        };
        var team = new LiveRemovingMatchTeam(first, second);
        Match match = CreateMatch(team);

        Exception exception = Record.Exception(match.MatchCleanup);

        Assert.Null(exception);
        Assert.Empty(team.GetMembers());
        Assert.Equal(new[] { first, second }, team.LeftIdentities);
        Assert.Equal(MatchStatus.Finalised, match.Status);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MatchCleanup_ReturnsOnlyActivePlayersStillOnTheMatchMap(bool entered, bool onMap)
    {
        var registry = RecordingDispatchProxy<IPlayerManager>.Create(out var players);
        var player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
        var session = RecordingDispatchProxy<IGameSession>.Create(out var connection);
        var home = RecordingDispatchProxy<IBaseMap>.Create(out var homeMap);
        homeMap.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 12u });
        Identity identity = new() { RealmId = 1, Id = 1000ul };
        actor.SetProperty(nameof(IPlayer.Identity), identity);
        actor.SetProperty(nameof(IPlayer.Session), session);
        actor.SetProperty(nameof(IPlayer.Map), home);
        players.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);
        var member = new MatchTeamMember(registry);
        member.Initialise(identity, Role.DPS);
        var team = new LiveRemovingMatchTeam(member);
        Match match = CreateMatch(team, out var map, out _, registry);
        if (entered)
            member.MatchEnter(match.MatchingMap);
        if (onMap)
            actor.SetProperty(nameof(IPlayer.Map), (IBaseMap)(object)map);

        match.MatchCleanup();

        Assert.Equal(MatchStatus.Finalised, match.Status);
        Assert.Single(team.LeftIdentities);
        Assert.Single(map.GetInvocations(nameof(IContentMapInstance.RemoveMatch)));
        Assert.Equal(onMap ? 1 : 0, actor.GetInvocations(nameof(IPlayer.TeleportTo)).Count);
        Assert.Equal(onMap ? 1 : 0, actor.GetInvocations(nameof(IPlayer.RemoveTemporaryFaction)).Count);
        Assert.Equal(onMap ? 1 : 0, connection.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Count(call => call.Arguments[0] is ServerMatchingMatchExited));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LeaveRequestBeforeMapAdmissionRemovesOnlyTheRequestingMember(bool anotherPending)
    {
        var registry = RecordingDispatchProxy<IPlayerManager>.Create(out var players);
        var player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var connection);
        var home = RecordingDispatchProxy<IBaseMap>.Create(out var homeMap);
        homeMap.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 12u });
        Identity identity = new() { RealmId = 1, Id = 1000ul };
        actor.SetProperty(nameof(IPlayer.Identity), identity);
        actor.SetProperty(nameof(IPlayer.Session), session);
        actor.SetProperty(nameof(IPlayer.Map), home);
        connection.SetProperty(nameof(IWorldSession.Player), player);
        players.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);
        var member = new MatchTeamMember(registry);
        member.Initialise(identity, Role.DPS);
        Identity other = new() { RealmId = 1, Id = 1001ul };
        IMatchTeamMember[] members = anotherPending ? [member, new TestMatchTeamMember(other, inMatch: false)] : [member];
        var team = new LiveRemovingMatchTeam(members);
        Match match = CreateMatch(team, out _, out var manager, registry, attachMap: false);

        new ClientMatchingMatchLeaveHandler(manager).HandleMessage(session, new ClientMatchingMatchLeave());

        Assert.Equal(anotherPending ? MatchStatus.InProgress : MatchStatus.Finalised, match.Status);
        Assert.Equal(identity, Assert.Single(team.LeftIdentities));
        Assert.Null(manager.GetMatchCharacter(identity).Match);
        Assert.Null(match.GetReturnPosition(player));
        if (anotherPending)
            Assert.Same(match, manager.GetMatchCharacter(other).Match);
        Assert.Empty(actor.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Empty(actor.GetInvocations(nameof(IPlayer.RemoveTemporaryFaction)));
    }

    [Fact]
    public void MatchFinishCallbackCanCleanUpWithoutASecondTimerCleanup()
    {
        var team = new LiveRemovingMatchTeam(new Identity { RealmId = 1, Id = 1000ul });
        Match match = CreateMatch(team, out var map, out _);
        SetAutoProperty(match, nameof(Match.Status), MatchStatus.InProgress);
        map.SetMethodHandler(nameof(IContentMapInstance.OnMatchFinish), _ =>
        {
            match.MatchCleanup();
            return null;
        });

        match.MatchFinish();
        match.Update(301);
        match.MatchCleanup();

        Assert.Equal(MatchStatus.Finalised, match.Status);
        Assert.Single(map.GetInvocations(nameof(IContentMapInstance.RemoveMatch)));
        Assert.Single(team.LeftIdentities);
    }

    [Theory]
    [InlineData("cleanup")]
    [InlineData("finish-failure")]
    [InlineData("reattach")]
    public void ContentUnloadCompletesItsOriginalMatchAndPublicEvents(string callback)
    {
        var team = new LiveRemovingMatchTeam(new Identity { RealmId = 1, Id = 1000ul });
        Match match = CreateMatch(team, out _, out _, attachMap: false);
        SetAutoProperty(match, nameof(Match.Status), MatchStatus.InProgress);
        var publicEvents = RecordingDispatchProxy<IPublicEventManager>.Create(out var events);
        var scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out var scripts);
        var scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out var callbacks);
        scripts.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);
        var map = new ContentMapInstance(null, publicEvents, scriptManager,
            mapIOManager: RecordingDispatchProxy<IMapIOManager>.Create(out _),
            entityCacheManager: RecordingDispatchProxy<IEntityCacheManager>.Create(out _));
        map.Initialise(new WorldEntry { Id = 77 }, RecordingDispatchProxy<IMapLock>.Create(out _));
        map.SetMatch(match);

        var replacement = RecordingDispatchProxy<IMatch>.Create(out var replacementMatch);
        var finishFailure = new IOException("Match finish script failed.");
        Exception replacementError = null;
        callbacks.SetMethodHandler(nameof(IScriptCollection.Invoke), _ =>
        {
            if (callback == "finish-failure")
                throw finishFailure;
            if (callback == "cleanup")
                match.MatchCleanup();
            else
            {
                map.RemoveMatch();
                replacementError = Record.Exception(() => map.SetMatch(replacement));
            }
            return null;
        });

        Assert.Same(callback == "finish-failure" ? finishFailure : null, Record.Exception(() => map.Unload()));
        Assert.Equal(MapUnloadStatus.AwaitingGridActions, map.UnloadStatus);
        Assert.Equal(MatchStatus.Finalised, match.Status);
        Assert.Null(map.Match);
        match.Update(301);
        Assert.Single(team.LeftIdentities);
        Assert.Single(events.GetInvocations(nameof(IPublicEventManager.Cleanup)));
        if (callback == "reattach")
            Assert.IsType<InvalidOperationException>(replacementError);
        Assert.Empty(replacementMatch.GetInvocations(nameof(IMatch.SetMap)));
    }

    private static Match CreateMatch(LiveRemovingMatchTeam team) => CreateMatch(team, out _, out _);

    private static Match CreateMatch(LiveRemovingMatchTeam team, out RecordingDispatchProxy<IContentMapInstance> mapProxy,
        out IMatchManager matchManager, IPlayerManager playerManager = null, bool attachMap = true)
    {
        matchManager = RecordingDispatchProxy<IMatchManager>.Create(out RecordingDispatchProxy<IMatchManager> matchManagerProxy);
        IMatchingDataManager matchingDataManager = RecordingDispatchProxy<IMatchingDataManager>.Create(out _);
        IFactory<IMatchTeam> matchTeamFactory = RecordingDispatchProxy<IFactory<IMatchTeam>>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out var tables);
        tables.SetProperty(nameof(IGameTableManager.GameFormula), Transport.PlayerBindPointTests.Table<GameFormulaEntry>());
        playerManager ??= RecordingDispatchProxy<IPlayerManager>.Create(out _);
        IInternalMessagePublisher messagePublisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out RecordingDispatchProxy<IInternalMessagePublisher> messagePublisherProxy);
        IMatchingDeserterManager matchingDeserterManager = RecordingDispatchProxy<IMatchingDeserterManager>.Create(out _);
        messagePublisherProxy.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);

        IContentMapInstance map = RecordingDispatchProxy<IContentMapInstance>.Create(out mapProxy);
        IMatchingMap matchingMap = new MatchingMap
        {
            GameMapEntry = new MatchingGameMapEntry
            {
                Id      = 7u,
                WorldId = 77u
            },
            GameTypeEntry = new MatchingGameTypeEntry
            {
                Id = 9u
            }
        };

        var match = new Match(
            NullLogger<Match>.Instance,
            matchManager,
            matchingDataManager,
            matchTeamFactory,
            gameTableManager,
            playerManager,
            messagePublisher,
            matchingDeserterManager);
        SetAutoProperty(match, nameof(Match.Guid), Guid.NewGuid());
        SetAutoProperty(match, nameof(Match.MatchingMap), matchingMap);
        if (attachMap)
        {
            mapProxy.SetProperty(nameof(IContentMapInstance.Match), match);
            match.SetMap(map);
        }

        List<IMatchTeam> teams = GetPrivateField<List<IMatchTeam>>(match, "teams");
        teams.Add(team);

        Dictionary<Identity, IMatchTeam> characterTeams = GetPrivateField<Dictionary<Identity, IMatchTeam>>(match, "characterTeams");
        var registrations = new Dictionary<Identity, MatchCharacter>();
        foreach (IMatchTeamMember member in team.GetMembers())
        {
            characterTeams.Add(member.Identity, team);
            var registration = new MatchCharacter(NullLogger<MatchCharacter>.Instance);
            registration.Initialise(member.Identity);
            registration.AddMatch(match);
            registrations.Add(member.Identity, registration);
        }
        matchManagerProxy.SetMethodHandler(nameof(IMatchManager.GetMatchCharacter), args => registrations[(Identity)args[0]]);

        return match;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (T)field.GetValue(instance)!;
    }

    private sealed class LiveRemovingMatchTeam : IMatchTeam
    {
        private readonly List<IMatchTeamMember> members;
        private readonly List<Identity> leftIdentities = [];

        public StaticMatchTeam Team => StaticMatchTeam.Red;
        public Faction Faction => Faction.MatchingTeam1;
        public IReadOnlyList<Identity> LeftIdentities => leftIdentities;

        public LiveRemovingMatchTeam(params Identity[] identities)
        {
            members = identities
                .Select(i => new TestMatchTeamMember(i))
                .Cast<IMatchTeamMember>()
                .ToList();
        }

        public LiveRemovingMatchTeam(params IMatchTeamMember[] members) => this.members = members.ToList();

        public void Initialise(IMatch match, StaticMatchTeam team)
        {
        }

        public IMatchTeamMember GetMember(Identity identity)
        {
            return members.SingleOrDefault(m => m.Identity == identity);
        }

        public IEnumerable<IMatchTeamMember> GetMembers()
        {
            return members;
        }

        public void OnLogin(IPlayer player)
        {
        }

        public void MatchJoin(Identity identity, Role roles)
        {
        }

        public void MatchEnter(Identity identity, IMatchingMap matchingMap)
        {
        }

        public void MatchExit(Identity identity, bool teleport)
        {
            GetMember(identity)?.MatchExit(teleport);
        }

        public void MatchLeave(Identity identity)
        {
            IMatchTeamMember member = GetMember(identity);
            if (member == null)
                return;

            members.Remove(member);
            leftIdentities.Add(identity);
        }

        public void MatchTeleport(Identity identity)
        {
        }

        public IMapPosition GetReturnPosition(Identity identity)
        {
            return null;
        }

        public void Broadcast(IWritable message)
        {
        }
    }

    private sealed class TestMatchTeamMember(Identity identity, bool inMatch = true) : IMatchTeamMember
    {
        public Identity Identity { get; } = identity;
        public bool InMatch => inMatch;
        public Role Roles => Role.DPS;
        public IMapPosition ReturnPosition => null;
        public Vector3 ReturnRotation => Vector3.Zero;

        public void Initialise(Identity identity, Role roles)
        {
        }

        public void MatchEnter(IMatchingMap matchingMap)
        {
        }

        public void MatchExit(bool teleport)
        {
        }

        public void TeleportToMatch(IMapEntrance mapEntrance)
        {
        }

        public void TeleportToReturn()
        {
        }

        public void Send(IWritable message)
        {
        }
    }
}
