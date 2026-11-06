using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Script.Main.Quests.NorthernWilds;
using NexusForever.Script.Template;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Quests;

public partial class NorthernWildsQuestChainTests
{
    public interface IDepartureSession : IGameSession, IPlayerSession { }

    [Theory]
    [InlineData("session")]
    [InlineData("queue")]
    [InlineData("player")]
    [InlineData("map")]
    [InlineData("character")]
    [InlineData("deleted")]
    [InlineData("reload")]
    public void Q3963_DepartureKeepsItsOriginalOwner(string boundary)
    {
        var f = new DepartureFixture();
        f.Begin();
        var pending = GetQueuedEvent(f.Session.Events);
        if (boundary == "session") f.Player.SetProperty(nameof(IPlayer.Session), CreateSessionWithEvents());
        if (boundary == "queue") f.SessionProxy.SetProperty(nameof(IGameSession.Events), new EventQueue());
        if (boundary == "player") f.SessionProxy.SetProperty(nameof(IPlayerSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        if (boundary == "map") f.Player.SetProperty(nameof(IPlayer.Map), CreateMap(426));
        if (boundary == "character") f.Player.SetProperty(nameof(IPlayer.CharacterId), 43ul);
        if (boundary == "deleted") f.QuestProxy.SetProperty(nameof(IQuest.PendingDelete), true);
        if (boundary == "reload") f.Script.OnLoad(CreateQuest(3963, new Dictionary<ushort, QuestState?>(), out _, out _));
        pending.Execute();
        Assert.Empty(f.Player.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Empty(f.Player.GetInvocations("set_" + nameof(IPlayer.Rotation)));
    }

    [Fact]
    public void Q3963_ReplayedDepartureEventDoesNotTeleportTwice()
    {
        var f = new DepartureFixture(); f.Begin();
        var pending = GetQueuedEvent(f.Session.Events);
        pending.Execute(); pending.Execute();
        Assert.Single(f.Player.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void Q3963_CinematicFailureDoesNotPoisonAnotherValidAttempt()
    {
        var f = new DepartureFixture();
        f.Cinematic.SetMethodHandler(nameof(ICinematicManager.QueueCinematic), _ => throw new IOException("Owned cinematic refusal."));
        Assert.Throws<IOException>(f.Begin);
        f.Cinematic.SetMethodHandler(nameof(ICinematicManager.QueueCinematic), _ => null);
        f.Begin();
        GetQueuedEvent(f.Session.Events).Execute();
        Assert.Single(f.Player.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void Q3963_CinematicCallbackCannotScheduleOnAReplacementQueue()
    {
        var f = new DepartureFixture(); var replacement = CreateSessionWithEvents();
        f.Cinematic.SetMethodHandler(nameof(ICinematicManager.QueueCinematic), _ =>
        { f.Player.SetProperty(nameof(IPlayer.Session), replacement); return null; });
        f.Begin();
        Assert.False(replacement.Events.PendingEvents);
        Assert.False(f.Session.Events.PendingEvents);
    }

    [Fact]
    public void Q3963_ChangedOwnerDuringTeleportCheckDoesNotMoveTheOriginalPlayer()
    {
        var f = new DepartureFixture(); f.Begin();
        f.Player.SetMethodHandler(nameof(IPlayer.CanTeleport), _ =>
        { f.SessionProxy.SetProperty(nameof(IPlayerSession.Player), null); return true; });
        GetQueuedEvent(f.Session.Events).Execute();
        Assert.Empty(f.Player.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Empty(f.Player.GetInvocations("set_" + nameof(IPlayer.Rotation)));
    }

    [Fact]
    public void Q3963_OriginalDepartureKeepsItsDestinationAndRotation()
    {
        var f = new DepartureFixture(); f.Begin(); f.Begin();
        Assert.Single(f.Cinematic.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        GetQueuedEvent(f.Session.Events).Execute();
        var call = Assert.Single(f.Player.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Equal((ushort)51, call.Arguments[0]);
        Assert.Equal(3832.38f, call.Arguments[1]); Assert.Equal(-1001.45f, call.Arguments[2]); Assert.Equal(-4497.14f, call.Arguments[3]);
        Assert.Single(f.Player.GetInvocations("set_" + nameof(IPlayer.Rotation)));
    }

    [Theory]
    [InlineData("epoch")]
    [InlineData("account-session")]
    [InlineData("account")]
    public void Q3963_DepartureKeepsItsOriginalAccount(string boundary)
    {
        var f = new DepartureFixture();
        var account = RecordingDispatchProxy<IAccount>.Create(out var ap);
        ap.SetProperty(nameof(IAccount.Session), f.Session);
        ap.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
        f.Player.SetProperty(nameof(IPlayer.Account), account);
        f.Begin(); var pending = GetQueuedEvent(f.Session.Events);
        if (boundary == "epoch") ap.SetProperty(nameof(IAccount.WorldSessionEpoch), 2ul);
        if (boundary == "account-session") ap.SetProperty(nameof(IAccount.Session), CreateSessionWithEvents());
        if (boundary == "account") f.Player.SetProperty(nameof(IPlayer.Account), RecordingDispatchProxy<IAccount>.Create(out _));
        pending.Execute();
        Assert.Empty(f.Player.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void Q3963_RetiredDepartureDoesNotClearTheNewOwnersPendingTrip()
    {
        var f = new DepartureFixture(); f.Begin(); var old = GetQueuedEvent(f.Session.Events);
        var owner = CreateQuest(3963, new Dictionary<ushort, QuestState?>(), out _, out _, map: CreateMap(426), session: f.Session);
        var player = (RecordingDispatchProxy<IPlayer>)(object)owner.Player;
        player.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        f.SessionProxy.SetProperty(nameof(IPlayerSession.Player), owner.Player);
        f.Script.OnLoad(owner); f.Begin(); old.Execute();
        var queue = (System.Collections.IEnumerable)typeof(EventQueue).GetField("events", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(f.Session.Events)!;
        var current = Assert.Single(queue.Cast<object>().Select(p => (IEvent)p.GetType().GetProperty("Event")!.GetValue(p)!), e => !ReferenceEquals(e, old));
        current.Execute();
        Assert.Empty(f.Player.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Single(player.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void Q3963_ResetStateCancelsQueuedDeparture()
    {
        var f = new DepartureFixture(); f.Begin();
        var pending = GetQueuedEvent(f.Session.Events);
        f.Script.OnQuestStateChange(QuestState.Accepted, QuestState.Achieved);
        pending.Execute();
        Assert.Empty(f.Player.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void Q3963_UnloadingQuestScriptCancelsQueuedDeparture()
    {
        var f = new DepartureFixture(); f.Begin();
        var pending = GetQueuedEvent(f.Session.Events);
        ((IScript)f.Script).OnUnload();
        f.Begin();
        pending.Execute();
        Assert.Empty(f.Player.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    private sealed class DepartureFixture
    {
        public readonly IDepartureSession Session;
        public readonly RecordingDispatchProxy<IDepartureSession> SessionProxy;
        public readonly IQuest Quest;
        public readonly RecordingDispatchProxy<IQuest> QuestProxy;
        public readonly RecordingDispatchProxy<IPlayer> Player;
        public readonly RecordingDispatchProxy<ICinematicManager> Cinematic;
        public readonly Q3963MoreImportantThanRevengeQuestScript Script;
        public DepartureFixture()
        {
            Session = RecordingDispatchProxy<IDepartureSession>.Create(out SessionProxy);
            SessionProxy.SetProperty(nameof(IGameSession.Events), new EventQueue());
            Quest = CreateQuest(3963, new Dictionary<ushort, QuestState?>(), out _, out _, map: CreateMap(426), session: Session);
            QuestProxy = (RecordingDispatchProxy<IQuest>)(object)Quest;
            QuestProxy.SetProperty(nameof(IQuest.State), QuestState.Achieved);
            SessionProxy.SetProperty(nameof(IPlayerSession.Player), Quest.Player);
            Player = (RecordingDispatchProxy<IPlayer>)(object)Quest.Player;
            Player.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
            Cinematic = GetCinematicManagerProxy(Quest);
            Script = new(NullLogger<Q3963MoreImportantThanRevengeQuestScript>.Instance, CreateCinematicFactory(out _, out _),
                CreateGameTableManager(CreateWorldLocationTable(CreateAlgorocDepartureWorldLocation())));
            Script.OnLoad(Quest);
        }
        public void Begin() => Script.OnQuestStateChange(QuestState.Achieved, QuestState.Accepted);
    }
}
