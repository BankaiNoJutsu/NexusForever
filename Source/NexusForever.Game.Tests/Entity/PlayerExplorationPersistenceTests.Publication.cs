using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Map;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class PlayerExplorationPersistenceTests
{
    [Fact]
    public async Task RealRelocationPublishesOnlyTheConfirmedMapAndCompletion()
    {
        using var f = new Fixture(options);
        f.Seed();
        SeedProgress(f);
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (maps, _) = Configure(f, actor);

        maps.OnRelocate(First);
        Assert.Equal(100, maps.GetMapZoneExploredPercent(1));
        Assert.Empty(MapPackets(session));
        maps.SendInitialPackets();
        CheckMask(Assert.Single(MapPackets(session)), first: false, second: true);
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        await actor.Drain();
        Assert.Single(MapPackets(session));
        using (var read = f.Context()) { Assert.Single(read.CharacterZonemapHexgroup); Assert.Empty(read.CharacterTitle); }

        await Persist(f, actor);
        Assert.Single(MapPackets(session));
        await actor.Drain();
        Assert.Equal(2, MapPackets(session).Count());
        CheckMask(MapPackets(session).Last(), first: true, second: true);
        using var final = f.Context();
        Assert.Equal(2, final.CharacterZonemapHexgroup.Count());
        Assert.Equal(22, Assert.Single(final.CharacterTitle).Title);
    }

    [Fact]
    public async Task FrozenMapPublicationDoesNotRevealLaterRelocationBits()
    {
        using var f = new Fixture(options);
        f.Seed();
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (maps, _) = Configure(f, actor);
        maps.OnRelocate(First);
        actor.OnAuth = () => maps.OnRelocate(Second);

        await actor.Player.SaveDirect();
        await actor.Drain();
        CheckMask(Assert.Single(MapPackets(session)), first: true, second: false);
        using (var read = f.Context()) Assert.Equal(123, Assert.Single(read.CharacterZonemapHexgroup).HexGroup);

        actor.OnAuth = null;
        await actor.Player.SaveDirect();
        await actor.Drain();
        Assert.Equal(2, MapPackets(session).Count());
        CheckMask(MapPackets(session).Last(), first: true, second: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedCommittedMapSendRetriesOnlyOnTheOriginalSession(bool replace)
    {
        using var f = new Fixture(options);
        f.Seed();
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (maps, _) = Configure(f, actor, rewards: false);
        int attempts = 0;
        session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerZoneMap packet) return null;
            CheckMask(packet, first: true, second: false);
            if (++attempts == 1)
            {
                if (replace)
                    session.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                throw new IOException("Map packet failed before enqueue.");
            }
            return null;
        });
        maps.OnRelocate(First);
        await Persist(f, actor);
        await actor.Drain();
        Assert.Equal(replace ? 1 : 2, attempts);
        using var read = f.Context();
        Assert.Equal(123, Assert.Single(read.CharacterZonemapHexgroup).HexGroup);
    }

    private static Actor CreatePublicationActor(Fixture f, out RecordingDispatchProxy<IWorldSession> proxy)
    {
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out proxy);
        proxy.SetProperty(nameof(IWorldSession.Events), events);
        var actor = new Actor(f, actorSession: session, actorEvents: events);
        proxy.SetProperty(nameof(IWorldSession.Player), actor.Player);
        return actor;
    }

    private static IEnumerable<ServerZoneMap> MapPackets(RecordingDispatchProxy<IWorldSession> proxy)
        => proxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerZoneMap>();

    private static void CheckMask(ServerZoneMap packet, bool first, bool second)
    {
        Assert.Equal(1u, packet.ZoneMapId);
        var bits = packet.ZoneMapBits.GetBuffer();
        Assert.Equal(first, (bits[212 / 8] & (1 << (212 % 8))) != 0);
        Assert.Equal(second, (bits[630 / 8] & (1 << (630 % 8))) != 0);
    }
}
