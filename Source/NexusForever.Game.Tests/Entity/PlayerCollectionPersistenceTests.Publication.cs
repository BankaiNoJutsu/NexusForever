using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class PlayerCollectionPersistenceTests
{
    [Fact]
    public async Task CreatureDatacubeNoticeAndInitialListWaitForConfirmedSave()
    {
        using var f = new Fixture(options);
        f.Seed();
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (cubes, _) = Configure(f, actor);
        var entity = new SimpleEntity(RecordingDispatchProxy<IMovementManager>.Create(out _));
        typeof(WorldEntity).GetProperty(nameof(WorldEntity.CreatureEntry), Instance)!
            .GetSetMethod(nonPublic: true)!.Invoke(entity, [new Creature2Entry { Id = 100, DatacubeId = 123 }]);
        entity.OnActivate(actor.Player);

        Assert.Empty(Packets<ServerDatacubeUpdate>(session));
        cubes.SendInitialPackets();
        Assert.Empty(Assert.Single(Packets<ServerDatacubeUpdateList>(session)).DatacubeData);
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        await actor.Drain();
        Assert.Empty(Packets<ServerDatacubeUpdate>(session));
        using (var read = f.Context()) Assert.Empty(read.CharacterDatacube);
        await Persist(f, actor);
        Assert.Empty(Packets<ServerDatacubeUpdate>(session));
        await actor.Drain();
        Assert.Equal((uint)int.MaxValue, Assert.Single(Packets<ServerDatacubeUpdate>(session)).DatacubeData.Progress);
        using var final = f.Context();
        Assert.Equal((uint)int.MaxValue, Assert.Single(final.CharacterDatacube).Progress);
    }

    [Fact]
    public async Task FrozenSavePublishesEarlierBitsAndLaterSavePublishesTheRemainingCollection()
    {
        using var f = new Fixture(options);
        f.Seed();
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (cubes, _) = Configure(f, actor);
        cubes.AddDatacube(123, 1);
        actor.OnAuth = () =>
        {
            cubes.AddDatacube(123, 2);
            cubes.AddDatacubeVolume(123, 4);
            cubes.AddScientistCreatureScan(123);
        };
        await actor.Player.SaveDirect();
        await actor.Drain();
        Assert.Equal(1u, Assert.Single(Packets<ServerDatacubeUpdate>(session)).DatacubeData.Progress);
        Assert.Empty(Packets<ServerDatacubeVolumeUpdate>(session));
        cubes.SendInitialPackets();
        var initial = Assert.Single(Packets<ServerDatacubeUpdateList>(session));
        Assert.Equal(1u, Assert.Single(initial.DatacubeData).Progress);
        Assert.Empty(initial.DatacubeVolumeData);
        using (var first = f.Context()) Assert.Equal(1u, Assert.Single(first.CharacterDatacube).Progress);

        actor.OnAuth = null;
        await actor.Player.SaveDirect();
        await actor.Drain();
        Assert.Equal(new uint[] { 1, 3 }, Packets<ServerDatacubeUpdate>(session).Select(p => p.DatacubeData.Progress));
        Assert.Equal(4u, Assert.Single(Packets<ServerDatacubeVolumeUpdate>(session)).DatacubeVolumeData.Progress);
        cubes.SendInitialPackets();
        var updated = Packets<ServerDatacubeUpdateList>(session).Last();
        Assert.Equal(3u, Assert.Single(updated.DatacubeData).Progress);
        Assert.Equal(4u, Assert.Single(updated.DatacubeVolumeData).Progress);
    }

    [Fact]
    public async Task InitialListKeepsQuietConfirmedProgressWhileAnotherNoticeAwaitsItsTick()
    {
        using var f = new Fixture(options);
        f.Seed();
        using (var seed = f.Context())
        {
            seed.CharacterDatacube.Add(new() { Id = 42, Type = (byte)DatacubeType.Datacube, Datacube = 123, Progress = 1 });
            seed.SaveChanges();
        }
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (cubes, _) = Configure(f, actor);
        cubes.GetDatacube(123, DatacubeType.Datacube).Progress |= 2;
        await Persist(f, actor);
        Assert.Empty(Packets<ServerDatacubeUpdate>(session));

        cubes.AddDatacubeVolume(123, 4);
        await Persist(f, actor);
        cubes.SendInitialPackets();
        var list = Packets<ServerDatacubeUpdateList>(session).Last();
        Assert.Equal(3u, Assert.Single(list.DatacubeData).Progress);
        Assert.Empty(list.DatacubeVolumeData);
        await actor.Drain();
        Assert.Equal(4u, Assert.Single(Packets<ServerDatacubeVolumeUpdate>(session)).DatacubeVolumeData.Progress);
    }

    [Fact]
    public async Task ReentrantInitialSendCannotOvertakeOrRegressCommittedUpdates()
    {
        using var f = new Fixture(options);
        f.Seed();
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (cubes, _) = Configure(f, actor);
        cubes.AddDatacube(123, 1);
        await Persist(f, actor);
        await actor.Drain();
        bool entered = false;
        session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerDatacubeUpdateList || entered) return null;
            entered = true;
            cubes.AddDatacube(123, 2);
            using (var context = f.Context())
            {
                actor.Player.Save(context);
                context.SaveChanges();
            }
            actor.Events.Update(.01);
            Assert.Single(Packets<ServerDatacubeUpdate>(session));
            cubes.SendInitialPackets();
            return null;
        });

        cubes.SendInitialPackets();
        Assert.True(entered);
        await actor.Drain();
        Assert.Equal(new uint[] { 1, 3 }, Packets<ServerDatacubeUpdate>(session).Select(p => p.DatacubeData.Progress));
        Assert.Equal(new uint[] { 1, 3 }, Packets<ServerDatacubeUpdateList>(session).Select(p => Assert.Single(p.DatacubeData).Progress));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfirmedDuplicateActivationRetriesOnlyForTheOriginalPlayer(bool replace)
    {
        using var f = new Fixture(options);
        f.Seed();
        using (var seed = f.Context())
        {
            seed.CharacterDatacube.Add(new() { Id = 42, Type = (byte)DatacubeType.Datacube, Datacube = 123, Progress = 1 });
            seed.SaveChanges();
        }
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (cubes, _) = Configure(f, actor);
        int attempts = 0, successful = 0;
        session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerDatacubeUpdate update) return null;
            Assert.Equal(1u, update.DatacubeData.Progress);
            if (++attempts == 1)
            {
                if (replace) session.SetProperty(nameof(IWorldSession.Player),
                    RecordingDispatchProxy<NexusForever.Game.Abstract.Entity.IPlayer>.Create(out _));
                throw new IOException("Datacube packet failed before enqueue.");
            }
            successful++;
            return null;
        });

        cubes.AddDatacube(123, 1);
        await actor.Drain();
        Assert.Equal(replace ? 1 : 2, attempts);
        Assert.Equal(replace ? 0 : 1, successful);
        using var read = f.Context();
        Assert.Equal(1u, Assert.Single(read.CharacterDatacube).Progress);
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

    private static IEnumerable<T> Packets<T>(RecordingDispatchProxy<IWorldSession> session)
        => session.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<T>();
}
