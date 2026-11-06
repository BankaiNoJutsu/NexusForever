using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Cinematic;
using NexusForever.Game.Cinematic.Cinematics;
using NexusForever.Game.Entity;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Static.Cinematic;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Cinematic;
using NexusForever.Script.Template;
using NexusForever.Shared;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Entity;

public sealed class CinematicCompletionTests
{
    [Fact]
    public void PublicEndedWithoutPlaybackAndAfterIdleCompletionIsInert()
    {
        var f = new Fixture();
        Assert.Null(Record.Exception(f.End)); Assert.Empty(f.Completed);
        f.Manager.QueueCinematic(new Clip(11)); f.End();
        Assert.Null(Record.Exception(f.End)); Assert.Equal(new uint[] { 11 }, f.Completed);
    }

    [Fact]
    public void ExplicitAdvanceCannotReplaceAnActivePlayback()
    {
        var f = new Fixture(); var first = new Clip(11); var second = new Clip(12);
        f.Manager.QueueCinematic(first); f.Manager.QueueCinematic(second); f.Manager.PlayQueuedCinematic();
        Assert.Equal(0, second.Starts); f.End(); Assert.Equal(1, second.Starts);
        Assert.Equal(new uint[] { 11 }, f.Completed); f.End(); Assert.Equal(new uint[] { 11, 12 }, f.Completed);
    }

    [Fact]
    public void CompletionIsRetiredBeforeAnEventRecursivelyReportsEnded()
    {
        var f = new Fixture(); var next = new Clip(12); bool nested = false;
        f.Callback = _ => { if (!nested) { nested = true; f.End(); } };
        f.Manager.QueueCinematic(new Clip(11)); f.Manager.QueueCinematic(next); f.End();
        Assert.Equal(new uint[] { 11 }, f.Completed); Assert.Equal(1, next.Starts);
        f.Callback = null; f.End(); Assert.Equal(new uint[] { 11, 12 }, f.Completed);
    }

    [Fact]
    public void CompletionFromTheOldMapCannotProgressTheNewMapsEvent()
    {
        var f = new Fixture(); f.Manager.QueueCinematic(new Clip(11)); f.MoveToNewMap(); f.End();
        Assert.Empty(f.Completed);
    }

    [Fact]
    public void OldMapQueueIsSkippedBeforeNewMapPlaybackStarts()
    {
        var f = new Fixture(); var old = new Clip(12); var current = new Clip(13);
        f.Manager.QueueCinematic(new Clip(11)); f.Manager.QueueCinematic(old);
        f.MoveToNewMap(); f.Manager.QueueCinematic(current); f.End();
        Assert.Equal(0, old.Starts); Assert.Equal(1, current.Starts); Assert.Empty(f.Completed);
        f.End(); Assert.Equal(new uint[] { 13 }, f.Completed);
    }

    [Fact]
    public void EventReceivesTheIdThatActuallyStartedPlayback()
    {
        var f = new Fixture(); var clip = new Clip(11); f.Manager.QueueCinematic(clip);
        clip.CinematicId = 99; f.End(); Assert.Equal(new uint[] { 11 }, f.Completed);
    }

    [Fact]
    public void EventCanJoinAnotherEventDuringItsCompletionWithoutReplayingIt()
    {
        var f = new Fixture(); int later = 0;
        f.Callback = _ => f.AddEvent(2, _ => ++later);
        f.Manager.QueueCinematic(new Clip(11)); Assert.Null(Record.Exception(f.End));
        Assert.Equal(0, later); Assert.Equal(new uint[] { 11 }, f.Completed);
    }

    [Fact]
    public void QueuedPlaybackCannotMigrateIntoAReplacementSession()
    {
        var f = new Fixture(); var old = new Clip(12); var current = new Clip(13);
        f.Manager.QueueCinematic(new Clip(11)); f.Manager.QueueCinematic(old);
        f.ReplaceSession(); f.End(); f.Manager.QueueCinematic(current);
        Assert.Equal(0, old.Starts); Assert.Equal(1, current.Starts);
    }

    [Fact]
    public void CompletionCallbackCanQueueTheNextConcretePlayback()
    {
        var f = new Fixture(); var next = new Clip(12);
        f.Callback = _ => f.Manager.QueueCinematic(next);
        f.Manager.QueueCinematic(new Clip(11)); f.End();
        Assert.Equal(1, next.Starts); f.Callback = null; f.End();
        Assert.Equal(new uint[] { 11, 12 }, f.Completed);
        Assert.Equal(new ushort[] { 11, 12 }, f.StartedIds());
    }

    [Fact]
    public void OtherClientStatesDoNotAdvancePlayback()
    {
        var f = new Fixture(); var next = new Clip(12);
        f.Manager.QueueCinematic(new Clip(11)); f.Manager.QueueCinematic(next);
        foreach (var state in new[] { CinematicState.Initalising, CinematicState.Playing, CinematicState.Finishing, CinematicState.Cancelling, (CinematicState)255 }) f.State(state);
        Assert.Equal(0, next.Starts); Assert.Empty(f.Completed);
        f.End(); Assert.Equal(1, next.Starts); Assert.Equal(new uint[] { 11 }, f.Completed);
    }

    [Fact]
    public void CompletionCallbackCanStartQueuedPlaybackWithoutAnOldAdvanceReplacingIt()
    {
        var f = new Fixture(); var next = new Clip(12); var later = new Clip(13);
        f.Manager.QueueCinematic(new Clip(11)); f.Manager.QueueCinematic(next); f.Manager.QueueCinematic(later);
        f.Callback = _ => f.Manager.PlayQueuedCinematic();
        f.End(); Assert.Equal(1, next.Starts); Assert.Equal(0, later.Starts);
        f.Callback = null; f.End(); Assert.Equal(1, later.Starts);
        Assert.Equal(new uint[] { 11, 12 }, f.Completed);
    }

    [Fact]
    public void ThrowingEventCallbackDoesNotLeaveOldPlaybackActiveOrBlockTheQueue()
    {
        var f = new Fixture(); var next = new Clip(12);
        f.Manager.QueueCinematic(new Clip(11)); f.Manager.QueueCinematic(next);
        f.Callback = _ => throw new InvalidOperationException("Owned event callback failure");
        Assert.Throws<InvalidOperationException>(f.End); Assert.Equal(1, next.Starts);
        f.Callback = null; f.End(); Assert.Equal(new uint[] { 11, 12 }, f.Completed);
    }

    [Fact]
    public void ThrowingPlaybackDoesNotKeepTheManagerBusy()
    {
        var f = new Fixture(); var next = new Clip(12);
        Assert.Throws<InvalidOperationException>(() => f.Manager.QueueCinematic(new Clip(11)
            { OnSetup = () => throw new InvalidOperationException("Owned setup failure") }));
        f.Manager.QueueCinematic(next); Assert.Equal(1, next.Starts);
        f.End(); Assert.Equal(new uint[] { 12 }, f.Completed);
    }

    [Fact]
    public void SetupContextChangeReleasesTheUnpublishedFrameAndStartsValidQueuedPlayback()
    {
        var f = new Fixture(); var next = new Clip(12);
        f.Manager.QueueCinematic(new Clip(11) { OnSetup = () =>
        {
            f.MoveToNewMap(); f.Manager.QueueCinematic(next);
        } });
        Assert.Equal(1, next.Starts); Assert.Equal(new ushort[] { 12 }, f.StartedIds());
        Assert.Empty(f.Completed); f.End(); Assert.Equal(new uint[] { 12 }, f.Completed);
    }

    [Fact]
    public void EventFanoutDoesNotInvokeAnOldMembershipAfterReplacement()
    {
        var f = new Fixture(); int oldCalls = 0, newCalls = 0;
        var old = f.AddEvent(2, _ => ++oldCalls); bool replaced = false;
        f.Callback = _ =>
        {
            if (replaced) return;
            replaced = true; f.RemoveEvent(old); f.AddEvent(2, _ => ++newCalls);
        };
        f.Manager.QueueCinematic(new Clip(11)); f.End(); Assert.Equal(0, oldCalls); Assert.Equal(0, newCalls);
        f.Manager.QueueCinematic(new Clip(12)); f.End(); Assert.Equal(0, oldCalls); Assert.Equal(1, newCalls);
    }

    [Fact]
    public void EventFanoutStopsWhenTheFirstScriptChangesMap()
    {
        var f = new Fixture(); int oldTail = 0;
        f.AddEvent(2, _ => ++oldTail); f.Callback = _ => f.MoveToNewMap();
        f.Manager.QueueCinematic(new Clip(11)); f.End();
        Assert.Equal(0, oldTail); Assert.Equal(new uint[] { 11 }, f.Completed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ChangedEpochOrEventQueueCannotAdoptOldQueuedPlayback(bool replaceQueue)
    {
        var f = new Fixture(); var old = new Clip(12); var next = new Clip(13);
        f.Manager.QueueCinematic(new Clip(11)); f.Manager.QueueCinematic(old);
        f.ChangeActorContext(replaceQueue); f.End(); f.Manager.QueueCinematic(next);
        Assert.Empty(f.Completed); Assert.Equal(0, old.Starts); Assert.Equal(1, next.Starts);
    }

    [Fact]
    public void PublicCompletionAfterSessionPlayerWasRemovedIsInert()
    {
        var f = new Fixture(); f.Manager.QueueCinematic(new Clip(11));
        ((RecordingDispatchProxy<IWorldSession>)(object)f.Session).SetProperty(nameof(IWorldSession.Player), null);
        Assert.Null(Record.Exception(f.End)); Assert.Empty(f.Completed);
    }

    [Fact]
    public void FirstNotifyCannotChangeThePublishedCinematicIdentity()
    {
        var f = new Fixture(); var clip = new Clip(11);
        f.OnMessage(message => { if (message is ServerCinematicNotify { CinematicId: 11 }) clip.CinematicId = 99; });
        f.Manager.QueueCinematic(clip); f.End(); Assert.Equal(new uint[] { 11 }, f.Completed);
    }

    [Fact]
    public void ReentrantCompletionAtFirstNotifyStopsTheOldConcretePacketTail()
    {
        var f = new Fixture(); var next = new Clip(12); bool advanced = false;
        f.OnMessage(message =>
        {
            if (message is not ServerCinematicNotify { CinematicId: 11 } || advanced) return;
            advanced = true; f.Manager.QueueCinematic(next); f.End();
        });
        f.Manager.QueueCinematic(new Clip(11)); Assert.True(advanced); Assert.Equal(1, next.Starts);
        Assert.DoesNotContain(f.Messages().OfType<ServerCinematicText>(), packet => packet.LocalizedTextId == 1100);
        Assert.Contains(f.Messages().OfType<ServerCinematicText>(), packet => packet.LocalizedTextId == 1200);
        f.End(); Assert.Equal(new uint[] { 11, 12 }, f.Completed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EachScriptInTheSameEventMustStillOwnCompletion(bool removeMembership)
    {
        var f = new Fixture(); int revokedCalls = 0; IPublicEvent value = null;
        value = f.AddEvent(2, _ => { if (removeMembership) f.RemoveEvent(value); else f.MoveToNewMap(); }, _ => ++revokedCalls);
        f.Manager.QueueCinematic(new Clip(11)); f.End(); Assert.Equal(0, revokedCalls);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ProvidedCinematicActorAndDerivedPlayPacketsRemainOwned(bool endAtFirstActor)
    {
        var f = new Fixture(); var next = new Clip(12); bool advanced = false;
        f.Actor.SetMethodReturnFactory(nameof(IPlayer.GetVisibleCreature), () => Array.Empty<WorldEntity>());
        f.OnMessage(message =>
        {
            if (!endAtFirstActor || message is not ServerCinematicActorAdd || advanced) return;
            advanced = true; f.Manager.QueueCinematic(next); f.End();
        });
        f.Manager.QueueCinematic(new NorthernWildsOnCreate());

        if (endAtFirstActor)
        {
            Assert.True(advanced); Assert.Equal(1, next.Starts);
            Assert.Empty(f.Messages().OfType<ServerCinematicActorAngle>());
            Assert.Empty(f.Messages().OfType<ServerCinematicTransitionDurationSet>());
            Assert.DoesNotContain(f.Messages().OfType<ServerCinematicText>(), packet => packet.LocalizedTextId != 1200);
            Assert.Contains(f.Messages().OfType<ServerCinematicText>(), packet => packet.LocalizedTextId == 1200);
            f.End(); Assert.Equal(new uint[] { 35, 12 }, f.Completed);
        }
        else
        {
            Assert.Equal(2, f.Messages().OfType<ServerCinematicActorAngle>().Count());
            Assert.Single(f.Messages().OfType<ServerCinematicTransitionDurationSet>());
            Assert.Equal(new ushort[] { 35 }, f.StartedIds());
            f.End(); Assert.Equal(new uint[] { 35 }, f.Completed);
        }
    }

    private sealed class Clip(ushort id) : CinematicBase
    {
        internal int Starts;
        internal Action OnSetup;
        protected override void Setup() { ++Starts; CinematicId = id; InitialFlags = CinematicFlags.NotifyServer; Duration = 1; Texts[0] = (uint)id * 100; OnSetup?.Invoke(); }
    }

    private sealed class Fixture
    {
        internal readonly IPlayer Player;
        internal readonly RecordingDispatchProxy<IPlayer> Actor;
        internal readonly CinematicManager Manager;
        internal IWorldSession Session;
        internal readonly List<uint> Completed = [];
        internal Action<uint> Callback;
        private readonly IAccount account;
        private readonly RecordingDispatchProxy<IAccount> accountProxy;
        private PublicEventManager events;
        private uint mapId;

        internal Fixture()
        {
            Player = RecordingDispatchProxy<IPlayer>.Create(out var actor); Actor = actor;
            actor.SetProperty(nameof(IPlayer.CharacterId), 42ul); actor.SetProperty(nameof(IPlayer.Guid), 42u);
            account = RecordingDispatchProxy<IAccount>.Create(out accountProxy);
            accountProxy.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul);
            actor.SetProperty(nameof(IPlayer.Account), account);
            ReplaceSession(); MoveToNewMap();
            Manager = new CinematicManager(Player); actor.SetProperty(nameof(IPlayer.CinematicManager), Manager);
        }
        internal void ReplaceSession()
        {
            Session = RecordingDispatchProxy<IWorldSession>.Create(out var session);
            session.SetProperty(nameof(IWorldSession.Player), Player); session.SetProperty(nameof(IWorldSession.Events), new EventQueue());
            Actor.SetProperty(nameof(IPlayer.Session), Session); accountProxy.SetProperty(nameof(IAccount.Session), Session);
        }
        internal void MoveToNewMap()
        {
            var map = RecordingDispatchProxy<IBaseMap>.Create(out var proxy);
            proxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = ++mapId });
            events = new PublicEventManager(NullLogger<PublicEventManager>.Instance,
                RecordingDispatchProxy<IPublicEventTemplateManager>.Create(out _), RecordingDispatchProxy<IPublicEventFactory>.Create(out _),
                Factory<IPublicEventCharacter>(() => new PublicEventCharacter(NullLogger<PublicEventCharacter>.Instance)));
            proxy.SetProperty(nameof(IBaseMap.PublicEventManager), events); Actor.SetProperty(nameof(IPlayer.Map), map);
            events.Initialise(map); events.OnAddToMap(Player); AddEvent(1, id => { Completed.Add(id); Callback?.Invoke(id); });
        }
        internal IPublicEvent AddEvent(uint id, Action<uint> callback, Action<uint> second = null)
        {
            var scripts = new List<IPublicEventScript>();
            foreach (var action in new[] { callback, second }.Where(action => action != null))
            {
                var script = RecordingDispatchProxy<IPublicEventScript>.Create(out var hook);
                hook.SetMethodHandler(nameof(IPublicEventScript.OnCinematicFinish), args => { action((uint)args[1]); return null; });
                scripts.Add(script);
            }
            var value = RecordingDispatchProxy<IPublicEvent>.Create(out var proxy);
            proxy.SetProperty(nameof(IPublicEvent.Id), id);
            proxy.SetMethodHandler(nameof(IPublicEvent.InvokeScriptCollection), args => { foreach (var script in scripts) ((Action<IPublicEventScript>)args[0])(script); return null; });
            events.AddEvent(42, value); return value;
        }
        internal void OnMessage(Action<object> action) => ((RecordingDispatchProxy<IWorldSession>)(object)Session)
            .SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args => { action(args[0]); return null; });
        internal IEnumerable<object> Messages() => ((RecordingDispatchProxy<IWorldSession>)(object)Session)
            .GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(row => row.Arguments[0]);
        internal void RemoveEvent(IPublicEvent value) => events.RemoveEvent(42, value);
        internal void ChangeActorContext(bool queue)
        {
            if (queue) ((RecordingDispatchProxy<IWorldSession>)(object)Session).SetProperty(nameof(IWorldSession.Events), new EventQueue());
            else accountProxy.SetProperty(nameof(IAccount.WorldSessionEpoch), 2ul);
        }
        internal void End() => State(CinematicState.Ended);
        internal void State(CinematicState state)
        {
            using var output = new MemoryStream(); using (var writer = new GamePacketWriter(output)) { writer.Write((uint)state, 8); writer.Write(false); writer.FlushBits(); }
            using var input = new MemoryStream(output.ToArray()); using var reader = new GamePacketReader(input);
            var message = new ClientCinematicState(); message.Read(reader);
            new ClientCinematicStateHandler().HandleMessage(Session, message);
        }
        internal IEnumerable<ushort> StartedIds() => ((RecordingDispatchProxy<IWorldSession>)(object)Session)
            .GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(row => row.Arguments[0])
            .OfType<ServerCinematicNotify>().Where(packet => packet.CinematicId != 0).Select(packet => packet.CinematicId);
        private static IFactory<T> Factory<T>(Func<T> create) where T : class
        {
            var factory = RecordingDispatchProxy<IFactory<T>>.Create(out var proxy);
            proxy.SetMethodReturnFactory(nameof(IFactory<T>.Resolve), () => create()); return factory;
        }
    }
}
