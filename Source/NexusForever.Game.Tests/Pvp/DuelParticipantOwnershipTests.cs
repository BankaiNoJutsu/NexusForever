using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Pvp;
using NexusForever.Game.Static.Pvp;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pvp;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pvp;

namespace NexusForever.Game.Tests.Pvp;

public partial class DuelParticipantOwnershipTests
{
    [Theory]
    [InlineData("initiate")] [InlineData("accept")] [InlineData("decline")] [InlineData("forfeit")]
    public void ParsedRequestMustComeFromTheParticipantsOwnSession(string request)
    {
        var f = new Fixture();
        if (request != "initiate") f.Initiate();
        else
        {
            f.First.Proxy.SetProperty(nameof(IPlayer.TargetGuid), 202u);
            f.First.Proxy.SetMethodReturn(nameof(IPlayer.GetVisible), f.Second.Player);
        }
        if (request == "forfeit") f.Activate();
        var foreign = RecordingDispatchProxy<IWorldSession>.Create(out var source);
        source.SetProperty(nameof(IWorldSession.Player), request == "initiate" ? f.First.Player : f.Second.Player);
        switch (request)
        {
            case "initiate": new ClientDuelInitiateHandler(NullLogger<ClientDuelInitiateHandler>.Instance, f.Manager).HandleMessage(foreign, Empty<ClientDuelInitiate>()); break;
            case "accept": new ClientDuelAcceptHandler(NullLogger<ClientDuelAcceptHandler>.Instance, f.Manager).HandleMessage(foreign, Empty<ClientDuelAccept>()); break;
            case "decline": new ClientDuelDeclineHandler(NullLogger<ClientDuelDeclineHandler>.Instance, f.Manager).HandleMessage(foreign, Empty<ClientDuelDecline>()); break;
            case "forfeit": new ClientDuelForfeitHandler(NullLogger<ClientDuelForfeitHandler>.Instance, f.Manager).HandleMessage(foreign, Empty<ClientDuelForfeit>()); break;
        }
        Assert.Empty(f.First.Packets<ServerDuelResult>());
        if (request != "forfeit") Assert.Empty(f.First.Packets<ServerDuelCountdown>());
        if (request == "initiate") Assert.Empty(f.First.Packets<ServerDuelChallenge>());
    }

    [Theory]
    [InlineData("accept")] [InlineData("decline")] [InlineData("forfeit")]
    public void ParsedReplacementRequestCannotTakeAnOriginalParticipantsDuel(string request)
    {
        var f = new Fixture(); f.Initiate();
        if (request == "forfeit") f.Activate();
        var replacement = new Participant(202, f.Map);
        if (request == "accept")
            new ClientDuelAcceptHandler(NullLogger<ClientDuelAcceptHandler>.Instance, f.Manager).HandleMessage(replacement.Session, Empty<ClientDuelAccept>());
        else if (request == "decline")
            new ClientDuelDeclineHandler(NullLogger<ClientDuelDeclineHandler>.Instance, f.Manager).HandleMessage(replacement.Session, Empty<ClientDuelDecline>());
        else new ClientDuelForfeitHandler(NullLogger<ClientDuelForfeitHandler>.Instance, f.Manager).HandleMessage(replacement.Session, Empty<ClientDuelForfeit>());
        Assert.Empty(f.First.Packets<ServerDuelResult>()); Assert.Empty(f.Second.Packets<ServerDuelResult>());
        if (request == "accept")
        {
            Assert.Empty(f.First.Packets<ServerDuelCountdown>());
            Assert.Equal(DuelFailureReason.CannotDuelRightNow, Assert.Single(replacement.Packets<ServerDuelFailure>()).Reason);
        }
        if (request != "forfeit") f.Activate();
        Assert.True(f.Manager.AreDueling(f.First.Player, f.Second.Player));
    }

    [Fact]
    public void ReusedUnitIdDoesNotGrantCombatPermissionOrTheDuelingPrerequisite()
    {
        var f = new Fixture(); f.Initiate(); f.Activate();
        var other = new Participant(101, f.Map);
        Assert.False(f.Manager.IsDueling(other.Player));
        Assert.False(f.Manager.AreDueling(other.Player, f.Second.Player));
        Assert.False(f.Manager.AreDueling(f.Second.Player, other.Player));
        Assert.False(f.Manager.AreDueling(f.First.Player, f.First.Player));
        Assert.True(f.Manager.AreDueling(f.First.Player, f.Second.Player));
    }

    [Theory]
    [InlineData("winner")] [InlineData("loser")] [InlineData("self")]
    public void DefeatRequiresBothDistinctOriginalParticipants(string changed)
    {
        var f = new Fixture(); f.Initiate(); f.Activate();
        var replacement = new Participant(changed == "winner" ? 101u : 202u, f.Map);
        var loser = changed == "loser" ? replacement.Player : f.Second.Player;
        var winner = changed == "winner" ? replacement.Player : changed == "self" ? loser : f.First.Player;
        Assert.False(f.Manager.TryFinishDefeat(loser, winner));
        Assert.Empty(f.First.Achievements.Invocations); Assert.Empty(f.Second.Achievements.Invocations);
        Assert.True(f.Manager.AreDueling(f.First.Player, f.Second.Player));
        Assert.True(f.Manager.TryFinishDefeat(f.Second.Player, f.First.Player));
        Assert.Single(f.First.Packets<ServerDuelResult>());
    }

    [Fact]
    public void DisconnectForAReusedIdCannotCancelAnotherPlayersDuel()
    {
        var f = new Fixture(); f.Initiate(); f.Activate();
        f.Manager.OnPlayerDisconnect(new Participant(101, f.Map).Player);
        Assert.True(f.Manager.AreDueling(f.First.Player, f.Second.Player));
        Assert.Empty(f.Second.Packets<ServerDuelResult>());
    }

    [Theory]
    [InlineData("binding")] [InlineData("session")] [InlineData("queue")]
    [InlineData("account")] [InlineData("epoch")] [InlineData("admission")]
    public void CountdownCannotCrossAnOriginalOwnerChange(string changed)
    {
        var f = new Fixture(); f.Initiate();
        Assert.Null(f.Manager.Accept(f.Second.Player));
        f.Second.Change(changed);
        f.Manager.Update(3);
        Assert.Empty(f.First.Packets<ServerDuelStart>());
        Assert.Empty(f.Second.Packets<ServerDuelStart>());
        Assert.Equal(DuelFinishReason.DuelCancelled, Assert.Single(f.First.Packets<ServerDuelResult>()).Reason);
        Assert.False(f.Manager.IsDueling(f.First.Player));
    }

    [Fact]
    public void MovingBothPlayersToAnotherMapDoesNotCarryTheDuelWithThem()
    {
        var f = new Fixture(); f.Initiate(); f.Activate();
        var nextMap = RecordingDispatchProxy<IBaseMap>.Create(out _);
        f.First.Proxy.SetProperty(nameof(IPlayer.Map), nextMap); f.Second.Proxy.SetProperty(nameof(IPlayer.Map), nextMap);
        Assert.False(f.Manager.AreDueling(f.First.Player, f.Second.Player));
        f.Manager.Update(0);
        Assert.False(f.Manager.IsDueling(f.First.Player));
    }

    [Fact]
    public void AReboundSecondRecipientCannotReceiveTheRestOfAChallengeFanout()
    {
        var f = new Fixture();
        var replacement = new Participant(202, f.Map);
        f.First.Wire.SetMethodHandler("EnqueueMessageEncrypted", args =>
        {
            if (args[0] is ServerDuelChallenge) f.Second.Proxy.SetProperty(nameof(IPlayer.Session), replacement.Session);
            return null;
        });
        f.Initiate();
        Assert.Empty(replacement.Packets<ServerDuelChallenge>());
        Assert.Empty(f.Second.Packets<ServerDuelChallenge>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BusyMapOwnershipDefersTheOriginalDefeatWithoutBlockingOrReversingIt(bool staleAfterRelease)
    {
        var f = new Fixture(); f.Initiate(); f.Activate();
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        object gate = f.Second.Player.MapAdmissionSyncRoot;
        Task owner = Task.Run(() => { lock (gate) { entered.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); } });
        Assert.True(entered.Wait(TimeSpan.FromSeconds(3)));
        try
        {
            await Task.Run(() =>
            {
                Assert.True(f.Manager.TryFinishDefeat(f.Second.Player, f.First.Player));
                f.Second.Proxy.SetProperty(nameof(IPlayer.IsAlive), false);
                Assert.False(f.Manager.TryFinishDefeat(f.First.Player, f.Second.Player));
                Assert.False(f.Manager.Forfeit(f.First.Player));
                f.Manager.Update(3);
            }).WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Empty(f.First.Packets<ServerDuelResult>());
            Assert.Empty(f.First.Achievements.Invocations);
            if (staleAfterRelease) f.Second.Change("admission");
        }
        finally { release.Set(); await owner; }
        f.Manager.Update(0);
        var result = Assert.Single(f.First.Packets<ServerDuelResult>());
        Assert.Equal(staleAfterRelease ? DuelFinishReason.DuelCancelled : DuelFinishReason.Defeated, result.Reason);
        Assert.Equal(staleAfterRelease ? 0u : 101u, result.WinnerUnitId);
        Assert.Equal(staleAfterRelease ? 0 : 2, f.First.Achievements.Invocations.Count);
        f.Manager.Update(100);
        Assert.Single(f.First.Packets<ServerDuelResult>());
    }

    [Fact]
    public void ReentrantReplacementDuelSurvivesTheOldUpdateSnapshot()
    {
        var f = new Fixture(); f.Initiate();
        var third = new Participant(303, f.Map); var fourth = new Participant(404, f.Map);
        Assert.Null(f.Manager.Initiate(third.Player, fourth.Player));
        f.First.Wire.SetMethodHandler("EnqueueMessageEncrypted", args =>
        {
            if (args[0] is ServerDuelResult)
            {
                Assert.True(f.Manager.Decline(fourth.Player));
                Assert.Null(f.Manager.Initiate(third.Player, fourth.Player));
            }
            return null;
        });
        f.Manager.Update(31);
        Assert.Single(third.Packets<ServerDuelResult>());
        Assert.Null(f.Manager.Accept(fourth.Player));
        f.Manager.Update(3);
        Assert.True(f.Manager.AreDueling(third.Player, fourth.Player));
    }

    private static T Empty<T>() where T : IReadable, new()
    { var value = new T(); using var reader = new GamePacketReader(new MemoryStream()); value.Read(reader); return value; }

    public interface IAdmittedPlayer : IPlayer, IPlayerMapAdmission { }

    private sealed class Fixture
    {
        internal readonly DuelManager Manager = new();
        internal readonly IBaseMap Map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        internal readonly Participant First, Second;
        internal Fixture() { First = new(101, Map); Second = new(202, Map); }
        internal void Initiate()
        {
            First.Proxy.SetProperty(nameof(IPlayer.TargetGuid), 202u);
            First.Proxy.SetMethodReturn(nameof(IPlayer.GetVisible), Second.Player);
            new ClientDuelInitiateHandler(NullLogger<ClientDuelInitiateHandler>.Instance, Manager)
                .HandleMessage(First.Session, Empty<ClientDuelInitiate>());
            Assert.Single(First.Packets<ServerDuelChallenge>());
        }
        internal void Activate()
        {
            new ClientDuelAcceptHandler(NullLogger<ClientDuelAcceptHandler>.Instance, Manager)
                .HandleMessage(Second.Session, Empty<ClientDuelAccept>());
            Manager.Update(3); Assert.True(Manager.AreDueling(First.Player, Second.Player));
        }
    }

    private sealed class Participant
    {
        internal readonly IAdmittedPlayer Player;
        internal readonly RecordingDispatchProxy<IAdmittedPlayer> Proxy;
        internal readonly IWorldSession Session;
        internal readonly RecordingDispatchProxy<IWorldSession> Wire;
        internal readonly IAccount Account;
        internal readonly RecordingDispatchProxy<IAccount> AccountProxy;
        internal readonly RecordingDispatchProxy<ICharacterAchievementManager> Achievements;
        internal Participant(uint id, IBaseMap map)
        {
            Player = RecordingDispatchProxy<IAdmittedPlayer>.Create(out Proxy);
            Session = RecordingDispatchProxy<IWorldSession>.Create(out Wire);
            Account = RecordingDispatchProxy<IAccount>.Create(out AccountProxy);
            var achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out Achievements);
            Proxy.SetProperty(nameof(IPlayer.Guid), id); Proxy.SetProperty(nameof(IPlayer.Map), map);
            Proxy.SetProperty(nameof(IPlayer.Identity), new Identity { Id = id, RealmId = 1 });
            Proxy.SetProperty(nameof(IPlayer.CharacterId), (ulong)id);
            Proxy.SetProperty(nameof(IPlayer.InWorld), true); Proxy.SetProperty(nameof(IPlayer.IsAlive), true);
            Proxy.SetProperty(nameof(IPlayer.Session), Session); Proxy.SetProperty(nameof(IPlayer.Account), Account);
            Proxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);
            Wire.SetProperty(nameof(IWorldSession.Player), Player); Wire.SetProperty(nameof(IWorldSession.Account), Account);
            Wire.SetProperty(nameof(IWorldSession.Events), new EventQueue());
            AccountProxy.SetProperty(nameof(IAccount.Id), id); AccountProxy.SetProperty(nameof(IAccount.Session), Session);
            AccountProxy.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
            Proxy.SetProperty(nameof(IPlayerMapAdmission.MapAdmissionGeneration), 1ul);
            Proxy.SetProperty(nameof(IPlayerMapAdmission.MapAdmissionSyncRoot), new object());
        }
        internal IEnumerable<T> Packets<T>() => Wire.GetInvocations("EnqueueMessageEncrypted").Select(i => i.Arguments[0]).OfType<T>();
        internal void Change(string part)
        {
            switch (part)
            {
                case "binding": Session.Player = RecordingDispatchProxy<IPlayer>.Create(out _); break;
                case "session": Proxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IWorldSession>.Create(out _)); break;
                case "queue": Wire.SetProperty(nameof(IWorldSession.Events), new EventQueue()); break;
                case "account": Proxy.SetProperty(nameof(IPlayer.Account), RecordingDispatchProxy<IAccount>.Create(out _)); break;
                case "epoch": AccountProxy.SetProperty(nameof(IAccount.WorldSessionEpoch), 2ul); break;
                case "admission": Proxy.SetProperty(nameof(IPlayerMapAdmission.MapAdmissionGeneration), 2ul); break;
            }
        }
    }
}
