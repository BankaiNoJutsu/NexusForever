using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Matching;
using NexusForever.Game.Matching.Match;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Internal;
using NexusForever.Network.Message;
using NexusForever.Shared;
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

    private static Match CreateMatch(LiveRemovingMatchTeam team)
    {
        IMatchManager matchManager = RecordingDispatchProxy<IMatchManager>.Create(out RecordingDispatchProxy<IMatchManager> matchManagerProxy);
        IMatchingDataManager matchingDataManager = RecordingDispatchProxy<IMatchingDataManager>.Create(out _);
        IFactory<IMatchTeam> matchTeamFactory = RecordingDispatchProxy<IFactory<IMatchTeam>>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out _);
        IInternalMessagePublisher messagePublisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out RecordingDispatchProxy<IInternalMessagePublisher> messagePublisherProxy);
        messagePublisherProxy.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);

        IMatchCharacter matchCharacter = RecordingDispatchProxy<IMatchCharacter>.Create(out _);
        matchManagerProxy.SetMethodReturn(nameof(IMatchManager.GetMatchCharacter), matchCharacter);

        IContentMapInstance map = RecordingDispatchProxy<IContentMapInstance>.Create(out _);
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
            messagePublisher);
        SetAutoProperty(match, nameof(Match.Guid), Guid.NewGuid());
        SetAutoProperty(match, nameof(Match.MatchingMap), matchingMap);
        SetPrivateField(match, "map", map);

        List<IMatchTeam> teams = GetPrivateField<List<IMatchTeam>>(match, "teams");
        teams.Add(team);

        Dictionary<Identity, IMatchTeam> characterTeams = GetPrivateField<Dictionary<Identity, IMatchTeam>>(match, "characterTeams");
        foreach (IMatchTeamMember member in team.GetMembers())
            characterTeams.Add(member.Identity, team);

        return match;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
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

    private sealed class TestMatchTeamMember(Identity identity) : IMatchTeamMember
    {
        public Identity Identity { get; } = identity;
        public bool InMatch => true;
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
