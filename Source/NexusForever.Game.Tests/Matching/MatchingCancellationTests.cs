using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Matching;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Group;
using NexusForever.Game.Matching.Queue;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Matching;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public sealed partial class MatchingCancellationTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void PublicLeaveCancelsTheJoinWaitingForItsMatchingTick(bool random, bool all)
    {
        var f = new Fixture(); f.Join(random); f.Leave(all);
        f.Manager.Update(.01);
        Assert.Empty(f.Joined);
        Assert.Empty(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void LogoutCancelsAnUnadmittedJoinEvenBeforeTheRegistryRemovesItsPlayer(bool offline)
    {
        var f = new Fixture(); f.Join(false); f.Manager.OnLogout(f.Leader);
        if (offline) f.Players.Remove(f.Leader.Identity);
        Exception error = Record.Exception(() => f.Manager.Update(.01));
        Assert.Null(error); Assert.Empty(f.Joined);
    }

    [Fact]
    public void ARespondedPartyMemberCanCancelItsPendingRoleCheck()
    {
        var f = new Fixture(); f.JoinParty(); f.Respond(f.LeaderSession);
        var roleCheck = f.Manager.GetMatchingRoleCheck(f.Leader.Identity);
        f.Leave(false);
        Assert.Equal(MatchingRoleCheckStatus.Declined, roleCheck.Status);
        Assert.Null(f.Manager.GetMatchingRoleCheck(f.Leader.Identity));
        Assert.Null(f.Manager.GetMatchingRoleCheck(f.Member.Identity));
        f.Manager.Update(.01); Assert.Empty(f.Joined);
    }

    [Fact]
    public void LogoutAfterAllRolesRespondedCannotThrowOrAdmitTheParty()
    {
        var f = new Fixture(); f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession);
        Exception error = Record.Exception(() => f.Manager.OnLogout(f.Leader));
        Assert.Null(error); f.Manager.Update(.01); Assert.Empty(f.Joined);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void UncancelledPublicJoinReachesTheValidatedQueue(bool random)
    {
        var f = new Fixture(); f.Join(random); f.Manager.Update(.01);
        Assert.Single(f.Joined);
        Assert.Single(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
    }

    [Fact]
    public void NewSoloRequestAfterCancellationCanEnterOnTheSameTick()
    {
        var f = new Fixture(); f.Join(false); f.Leave(false); f.Join(true);
        f.Manager.Update(.01); Assert.Single(f.Joined);
    }

    [Fact]
    public void LeavingAnotherMatchTypeDoesNotCancelTheRequestedQueue()
    {
        var f = new Fixture(); f.Join(false); f.Leave(false, MatchType.Arena);
        f.Manager.Update(.01); Assert.Single(f.Joined);
    }

    [Fact]
    public void LeaveDuringEligibilityValidationStillStopsAdmission()
    {
        var f = new Fixture(); f.BeforeValidation = () => f.Leave(false); f.Join(false);
        f.Manager.Update(.01); Assert.Empty(f.Joined);
    }

    [Fact]
    public void CancelledPartyCanStartANewRoleCheckBeforeTheNextTick()
    {
        var f = new Fixture(); f.JoinParty(); var old = f.Manager.GetMatchingRoleCheck(f.Leader.Identity);
        f.Leave(false); f.JoinParty(); var current = f.Manager.GetMatchingRoleCheck(f.Leader.Identity);
        Assert.NotSame(old, current); f.Respond(f.LeaderSession); f.Respond(f.MemberSession);
        f.Manager.Update(.01); Assert.Single(f.Joined);
        Assert.Null(f.Manager.GetMatchingRoleCheck(f.Leader.Identity));
    }

    [Fact]
    public void CompletedRoleCheckCannotBeExpiredByTheFollowingLongTick()
    {
        var f = new Fixture(); f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession);
        f.Manager.Update(31); Assert.Single(f.Joined);
    }

    [Fact]
    public void PartyLeaveDuringEligibilityValidationStillStopsAdmission()
    {
        var f = new Fixture(); f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession);
        f.BeforeValidation = () => f.Leave(false); f.Manager.Update(.01); Assert.Empty(f.Joined);
    }

    [Fact]
    public void WrongMatchTypeCannotConfirmTheCurrentRoleCheck()
    {
        var f = new Fixture(); f.JoinParty(); f.Respond(f.LeaderSession, MatchType.Arena);
        Assert.All(f.Manager.GetMatchingRoleCheck(f.Leader.Identity).GetMembers(), member => Assert.Null(member.Roles));
        Assert.Equal(MatchingQueueResult.NotConfirmingRole, f.Packets<ServerMatchingQueueResultAnnounce>(f.LeaderSession).Last().Result);
        f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01); Assert.Single(f.Joined);
    }

    [Theory]
    [InlineData(Role.None)] [InlineData(Role.DPS)]
    public void ExplicitDeclineCannotBecomeRoleConsent(Role roles)
    {
        var f = new Fixture(); f.JoinParty(); f.Respond(f.LeaderSession, roles: roles, accepted: false);
        Assert.Null(f.Manager.GetMatchingRoleCheck(f.Leader.Identity));
        f.Manager.Update(.01); Assert.Empty(f.Joined);
    }

    [Fact]
    public void DuplicateRoleResponseIsInertAndDoesNotBreakThePendingParty()
    {
        var f = new Fixture(); f.JoinParty(); f.Respond(f.LeaderSession);
        Assert.Null(Record.Exception(() => f.Respond(f.LeaderSession)));
        f.Respond(f.MemberSession); f.Manager.Update(.01); Assert.Single(f.Joined);
    }

    [Fact]
    public void OldLogoutCannotCancelAReplacementPlayersNewPendingRequest()
    {
        var f = new Fixture(); f.Join(false); var replacement = f.ReplaceLeader(); f.Join(false, replacement);
        f.Manager.OnLogout(f.Leader); f.Manager.Update(.01); Assert.Single(f.Joined);
    }

    [Fact]
    public void OldSessionCannotRespondToAReplacementPlayersRoleCheck()
    {
        var f = new Fixture(); var replacement = f.ReplaceLeader(); f.JoinParty(replacement);
        f.Respond(f.LeaderSession);
        Assert.All(f.Manager.GetMatchingRoleCheck(f.Leader.Identity).GetMembers(), member => Assert.Null(member.Roles));
        f.Manager.OnLogout(f.Leader); Assert.NotNull(f.Manager.GetMatchingRoleCheck(f.Leader.Identity));
        f.Respond(replacement); f.Respond(f.MemberSession); f.Manager.Update(.01); Assert.Single(f.Joined);
    }

    [Fact]
    public void PendingRoleCancellationIsNotPublishedToAReplacementPlayer()
    {
        var f = new Fixture(); f.JoinParty(); var replacement = f.ReplaceLeader();
        f.Leave(false); Assert.Empty(f.Packets<ServerMatchingQueueResultAnnounce>(replacement));
        Assert.Empty(f.Joined);
    }

    [Fact]
    public void OfflineEligibilityRefusesCleanlyWithoutLogoutNotification()
    {
        var f = new Fixture(); f.JoinParty(); f.Respond(f.LeaderSession); f.Respond(f.MemberSession);
        f.Players.Remove(f.Member.Identity);
        Assert.Null(Record.Exception(() => f.Manager.Update(.01))); Assert.Empty(f.Joined);
    }

    [Fact]
    public void AlreadyAdmittedQueueKeepsItsExistingLogoutAndLoginReadback()
    {
        var f = new Fixture(); f.Join(false); f.Manager.Update(.01); f.Manager.OnLogout(f.Leader);
        var replacement = f.ReplaceLeader(); f.Manager.OnLogin(replacement.Player);
        Assert.Single(f.Manager.GetMatchingCharacter(f.Leader.Identity).GetMatchingCharacterQueues());
        Assert.Contains(f.Packets<ServerMatchingQueueStatus>(replacement), status => (status.QueuesJoined.GetBuffer()[(uint)MatchType.Dungeon / 8] & (1 << ((int)MatchType.Dungeon % 8))) != 0);
    }

    [Fact]
    public void CancellationNoticeCanStartANewPartyWithoutOldCleanupRemovingIt()
    {
        var f = new Fixture(); f.JoinParty(); bool replaced = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is ServerMatchingQueueResultAnnounce { Result: MatchingQueueResult.Role } && !replaced)
            { replaced = true; f.JoinParty(); }
        });
        f.Leave(false); Assert.True(replaced);
        Assert.NotNull(f.Manager.GetMatchingRoleCheck(f.Leader.Identity));
        Assert.NotNull(f.Manager.GetMatchingRoleCheck(f.Member.Identity));
        f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01); Assert.Single(f.Joined);
    }

    [Fact]
    public void DuplicatePartyRequestKeepsItsOriginalRoleCheckAndResponses()
    {
        var f = new Fixture(); f.JoinParty(); var original = f.Manager.GetMatchingRoleCheck(f.Leader.Identity);
        f.Respond(f.LeaderSession); Assert.Null(Record.Exception(() => f.JoinParty()));
        Assert.Same(original, f.Manager.GetMatchingRoleCheck(f.Leader.Identity));
        f.Respond(f.MemberSession); f.Manager.Update(.01); Assert.Single(f.Joined);
    }

    [Fact]
    public void FailedEligibilityCannotPublishTheOldRequestToAReplacementPlayer()
    {
        var f = new Fixture(); IWorldSession replacement = null;
        f.BeforeValidation = () =>
        {
            replacement = f.ReplaceLeader();
            ((RecordingDispatchProxy<IPlayer>)(object)replacement.Player).SetProperty(nameof(IPlayer.Level), 0u);
        };
        f.Join(false); f.Manager.Update(.01);
        Assert.NotNull(replacement); Assert.Empty(f.Packets<ServerMatchingQueueResultAnnounce>(replacement));
        Assert.Empty(f.Joined);
    }

    [Fact]
    public void InitialRoleNoticesStopWhenTheFirstRecipientCancels()
    {
        var f = new Fixture(); bool cancelled = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is ServerMatchingRoleCheckStarted && !cancelled) { cancelled = true; f.Leave(false); }
        });
        f.JoinParty(); Assert.True(cancelled);
        Assert.Empty(f.Packets<ServerMatchingRoleCheckStarted>(f.MemberSession));
        Assert.Null(f.Manager.GetMatchingRoleCheck(f.Leader.Identity));
    }

    [Fact]
    public void OldCancellationCannotFollowTheReplacementPartysStartedNotice()
    {
        var f = new Fixture(); f.JoinParty(); bool replaced = false;
        f.OnMessage(f.LeaderSession, message =>
        {
            if (message is ServerMatchingQueueResultAnnounce { Result: MatchingQueueResult.Role } && !replaced)
            { replaced = true; f.JoinParty(); }
        });
        f.Leave(false); Assert.True(replaced);
        Assert.IsType<ServerMatchingRoleCheckStarted>(f.Messages(f.MemberSession)
            .Last(message => message is ServerMatchingRoleCheckStarted or ServerMatchingQueueResultAnnounce));
        f.Respond(f.LeaderSession); f.Respond(f.MemberSession); f.Manager.Update(.01); Assert.Single(f.Joined);
    }

    private sealed class Fixture
    {
        internal readonly MatchingManager Manager;
        internal readonly Dictionary<Identity, IPlayer> Players = [];
        internal readonly List<IMatchingQueueProposal> Joined = [];
        internal readonly IPlayer Leader, Member;
        internal readonly IWorldSession LeaderSession, MemberSession;
        internal Action BeforeValidation;
        internal readonly List<IMatchingQueueGroup> RealGroups = [];
        internal readonly List<IMatchingQueueGroup> Matched = [];

        internal Fixture(bool realBackend = false, bool instant = false, uint teamSize = 5)
        {
            var registry = RecordingDispatchProxy<IPlayerManager>.Create(out var players);
            players.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => Players.GetValueOrDefault((Identity)args[0]));
            var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
            Leader = Player(10, map, out LeaderSession); Member = Player(20, map, out MemberSession);
            mapProxy.SetMethodReturn(nameof(IBaseMap.Search), new[] { Leader, Member });
            var groups = new GroupStateManager();
            groups.UpdateGroup(new GroupLootState { GroupId = 7, Leader = Leader.Identity,
                Members = [new GroupLootMember { Identity = Leader.Identity }, new GroupLootMember { Identity = Member.Identity }] });
            var data = RecordingDispatchProxy<IMatchingDataManager>.Create(out var catalog);
            var matchMap = new NexusForever.Game.Matching.MatchingMap
            {
                GameMapEntry = new MatchingGameMapEntry { Id = 71, WorldId = 123 },
                GameTypeEntry = new MatchingGameTypeEntry { Id = 1, MatchTypeEnum = MatchType.Dungeon, TeamSize = teamSize, MinLevel = 1, MaxLevel = 50 }
            };
            catalog.SetProperty(nameof(IMatchingDataManager.DebugInstantQueue), instant);
            var adventureMap = new NexusForever.Game.Matching.MatchingMap
            {
                GameMapEntry = new MatchingGameMapEntry { Id = 72, WorldId = 124 },
                GameTypeEntry = new MatchingGameTypeEntry { Id = 2, MatchTypeEnum = MatchType.Adventure, TeamSize = teamSize, MinLevel = 1, MaxLevel = 50 }
            };
            catalog.SetMethodHandler(nameof(IMatchingDataManager.GetMatchingMap), args => (uint)args[0] == 72 ? adventureMap : matchMap);
            catalog.SetMethodHandler(nameof(IMatchingDataManager.GetMatchingMaps), args =>
                new[] { (MatchType)args[0] == MatchType.Adventure ? adventureMap : matchMap });
            catalog.SetMethodReturn(nameof(IMatchingDataManager.GetMapEntrance), new NexusForever.Game.Matching.MapEntrance { MapId = 123, Team = 0, Position = default, Rotation = default });
            var times = RecordingDispatchProxy<IMatchingQueueTimeManager>.Create(out _);
            var matches = RecordingDispatchProxy<IMatchCharacterStore>.Create(out var matchStore);
            matchStore.SetMethodReturn(nameof(IMatchCharacterStore.GetMatchCharacter), RecordingDispatchProxy<IMatchCharacter>.Create(out _));
            var penalties = RecordingDispatchProxy<IMatchingDeserterManager>.Create(out var deserters);
            deserters.SetMethodReturn(nameof(IMatchingDeserterManager.CanQueue), true);
            var disable = RecordingDispatchProxy<IDisableManager>.Create(out _);
            var backend = RecordingDispatchProxy<IMatchingQueueManager>.Create(out var queue);
            MatchingQueueValidator validator = null;
            queue.SetMethodHandler(nameof(IMatchingQueueManager.CanQueue), args => { BeforeValidation?.Invoke(); return validator.CanQueue((IMatchingQueueProposal)args[0]); });
            queue.SetMethodHandler(nameof(IMatchingQueueManager.JoinQueue), args =>
            {
                var proposal = (IMatchingQueueProposal)args[0]; Joined.Add(proposal);
                var group = RecordingDispatchProxy<IMatchingQueueGroup>.Create(out var groupProxy);
                groupProxy.SetProperty(nameof(IMatchingQueueGroup.Guid), Guid.NewGuid());
                groupProxy.SetProperty(nameof(IMatchingQueueGroup.MatchType), proposal.MatchType);
                groupProxy.SetMethodHandler(nameof(IMatchingQueueGroup.RemoveMatchingQueueProposal), _ => { proposal.RemoveMatchingQueueGroup(); return null; });
                groupProxy.SetMethodHandler(nameof(IMatchingQueueGroup.DetachMatchingQueueProposal), _ => proposal.DetachMatchingQueueGroup());
                proposal.SetMatchingQueueGroup(group); return null;
            });
            var roles = new MatchingRoleEnforcer(Factory<IMatchingRoleEnforcerResult>(() => new MatchingRoleEnforcerResult()));
            var matchManager = RecordingDispatchProxy<IMatchManager>.Create(out var matched);
            matched.SetMethodHandler(nameof(IMatchManager.CreateMatchProposal), args => { Matched.Add((IMatchingQueueGroup)args[0]); return null; });
            Func<IMatchingQueueManager> createBackend = () => realBackend
                ? new MatchingQueueManager(Factory<IMatchingQueue>(() => new MatchingQueue(
                    NullLogger<MatchingQueue>.Instance,
                    new MatchingQueueMatcher(NullLogger<MatchingQueueMatcher>.Instance,
                        new MatchingQueueGroupMatcher(NullLogger<MatchingQueueGroupMatcher>.Instance, data, roles, registry)),
                    new MatchingMapSelector(data, roles),
                    Factory<IMatchingQueueGroup>(() =>
                    {
                        var group = new MatchingQueueGroup(NullLogger<MatchingQueueGroup>.Instance, data,
                            Factory<IMatchingQueueGroupTeam>(() => new MatchingQueueGroupTeam(NullLogger<MatchingQueueGroupTeam>.Instance)));
                        RealGroups.Add(group); return group;
                    }), matchManager)),
                    Factory<IMatchingQueueValidator>(() => validator))
                : backend;
            Manager = new(NullLogger<MatchingManager>.Instance, data, times, Factory(createBackend),
                Factory<IMatchingQueueProposal>(() => new MatchingQueueProposal(NullLogger<MatchingQueueProposal>.Instance, Manager,
                    times, Factory<IMatchingQueueProposalMember>(() => new MatchingQueueProposalMember(registry)), data)),
                Factory<IMatchingRoleCheck>(() => new MatchingRoleCheck(NullLogger<MatchingRoleCheck>.Instance,
                    Factory<IMatchingRoleCheckMember>(() => new MatchingRoleCheckMember(registry, NullLogger<MatchingRoleCheckMember>.Instance)))),
                Factory<IMatchingCharacter>(() => new MatchingCharacter(NullLogger<MatchingCharacter>.Instance, registry, matches, Manager)),
                groups, registry, matches, penalties);
            validator = new(registry, disable, Manager, data,
                new MatchingRoleEnforcer(Factory<IMatchingRoleEnforcerResult>(() => new MatchingRoleEnforcerResult())), matches, penalties);
            Manager.Initialise();
        }
        private IPlayer Player(ulong id, IBaseMap map, out IWorldSession session)
        {
            var identity = new Identity { RealmId = 1, Id = id };
            var player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
            session = RecordingDispatchProxy<IWorldSession>.Create(out var connection);
            actor.SetProperty(nameof(IPlayer.Identity), identity); actor.SetProperty(nameof(IPlayer.CharacterId), id);
            actor.SetProperty(nameof(IPlayer.Faction1), Faction.Dominion); actor.SetProperty(nameof(IPlayer.Level), 10u);
            actor.SetProperty(nameof(IPlayer.Map), map); actor.SetProperty(nameof(IPlayer.Session), session);
            connection.SetProperty(nameof(IWorldSession.Player), player); Players[identity] = player; return player;
        }
        private static IFactory<T> Factory<T>(Func<T> create) where T : class
        {
            var factory = RecordingDispatchProxy<IFactory<T>>.Create(out var resolver);
            resolver.SetMethodReturnFactory(nameof(IFactory<T>.Resolve), () => create()); return factory;
        }
        internal void Join(bool random, IWorldSession session = null, MatchType type = MatchType.Dungeon)
        {
            session ??= LeaderSession;
            if (random) new ClientMatchingQueueRandomHandler(Manager).HandleMessage(session,
                Read<ClientMatchingQueueRandom>(writer => { writer.Write((uint)MatchType.Dungeon, 5); writer.Write(0u); writer.Write((uint)Role.DPS); }));
            else new ClientMatchingQueueHandler(Manager).HandleMessage(session, Read<ClientMatchingQueue>(writer => JoinBody(writer, type)));
        }
        internal void JoinParty(IWorldSession session = null) => new ClientMatchingQueuePartyHandler(Manager).HandleMessage(session ?? LeaderSession, Read<ClientMatchingQueueParty>(writer => JoinBody(writer)));
        private static void JoinBody(GamePacketWriter writer, MatchType type = MatchType.Dungeon)
        {
            writer.Write((uint)type, 5); writer.Write(1u); writer.Write(type == MatchType.Adventure ? 72u : 71u); writer.Write(0u, 14);
            writer.Write(0u); writer.Write((uint)Role.DPS); writer.Write(0u);
        }
        internal void Leave(bool all, MatchType type = MatchType.Dungeon)
        {
            if (all) new ClientMatchingQueueLeaveAllHandler(Manager).HandleMessage(LeaderSession, Read<ClientMatchingQueueLeaveAll>(_ => { }));
            else new ClientMatchingQueueLeaveHandler(Manager).HandleMessage(LeaderSession, Read<ClientMatchingQueueLeave>(writer => writer.Write((uint)type, 5)));
        }
        internal void Respond(IWorldSession session, MatchType type = MatchType.Dungeon, Role roles = Role.DPS, bool accepted = true) => new ClientMatchingRoleCheckResponseHandler(Manager).HandleMessage(session,
            Read<ClientMatchingRoleCheckResponse>(writer => { writer.Write((uint)type, 5); writer.Write((uint)roles); writer.Write(accepted); }));
        internal void OnMessage(IWorldSession session, Action<object> send) => ((RecordingDispatchProxy<IWorldSession>)(object)session)
            .SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args => { send(args[0]); return null; });
        internal IWorldSession ReplaceLeader() { Player(10, Leader.Map, out var session); return session; }
        internal IEnumerable<object> Messages(IWorldSession session) => ((RecordingDispatchProxy<IWorldSession>)(object)session)
            .GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(row => row.Arguments[0]);
        internal IEnumerable<T> Packets<T>(IWorldSession session) => ((RecordingDispatchProxy<IWorldSession>)(object)session)
            .GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(row => row.Arguments[0]).OfType<T>();
        private static T Read<T>(Action<GamePacketWriter> body) where T : IReadable, new()
        {
            using var output = new MemoryStream(); using (var writer = new GamePacketWriter(output)) { body(writer); writer.FlushBits(); }
            using var input = new MemoryStream(output.ToArray()); using var reader = new GamePacketReader(input);
            var packet = new T(); packet.Read(reader); return packet;
        }
    }
}
