using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Reputation;
using NexusForever.WorldServer.Network;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class PlayerExplorationPersistenceTests
{
    [Fact]
    public async Task EqualBalanceStillRequiresConfirmationOfBothSignedDeltas()
    {
        using var f = new Fixture(options);
        f.Seed();
        SeedProgress(f);
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (_, reputation) = Configure(f, actor);

        reputation.UpdateReputation(Faction.Exile, 25);
        reputation.UpdateReputation(Faction.Exile, -25);
        Assert.Empty(ReputationPackets(session));
        await Assert.ThrowsAsync<IOException>(() => Persist(f, actor, true));
        await actor.Drain();
        Assert.Empty(ReputationPackets(session));

        await Persist(f, actor);
        Assert.Empty(ReputationPackets(session));
        await actor.Drain();
        Assert.Equal(new[] { 25f, -25f }, ReputationPackets(session).Select(packet => packet.ReputationDelta));
        await Persist(f, actor);
        await actor.Drain();
        Assert.Equal(2, ReputationPackets(session).Count());
        using var read = f.Context();
        Assert.Equal(100f, Assert.Single(read.CharacterReputation).Amount);
    }

    [Fact]
    public async Task FrozenReputationPublishesOnlyItsCapturedSequence()
    {
        using var f = new Fixture(options);
        f.Seed();
        SeedProgress(f);
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (_, reputation) = Configure(f, actor);
        reputation.UpdateReputation(Faction.Exile, 25);
        actor.OnAuth = () => reputation.UpdateReputation(Faction.Exile, -25);

        await actor.Player.SaveDirect();
        await actor.Drain();
        Assert.Equal(25f, Assert.Single(ReputationPackets(session)).ReputationDelta);
        using (var first = f.Context()) Assert.Equal(125f, Assert.Single(first.CharacterReputation).Amount);
        Assert.Equal(100f, reputation.GetReputation(Faction.Exile).Amount);

        actor.OnAuth = null;
        await actor.Player.SaveDirect();
        await actor.Drain();
        Assert.Equal(new[] { 25f, -25f }, ReputationPackets(session).Select(packet => packet.ReputationDelta));
        using var final = f.Context();
        Assert.Equal(100f, Assert.Single(final.CharacterReputation).Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommittedReputationRetryStaysOnItsOriginalSession(bool replace)
    {
        using var f = new Fixture(options);
        f.Seed();
        SeedProgress(f);
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (_, reputation) = Configure(f, actor);
        int attempts = 0;
        session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerReputationUpdate update) return null;
            Assert.Equal(Faction.Exile, update.FactionId);
            Assert.Equal(-25f, update.ReputationDelta);
            if (++attempts == 1)
            {
                if (replace)
                    session.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
                throw new IOException("Reputation packet failed before enqueue.");
            }
            return null;
        });

        reputation.UpdateReputation(Faction.Exile, -25);
        await Persist(f, actor);
        await actor.Drain();
        Assert.Equal(replace ? 1 : 2, attempts);
        using var read = f.Context();
        Assert.Equal(75f, Assert.Single(read.CharacterReputation).Amount);
    }

    [Fact]
    public async Task InitialStandingPrecedesQueuedDeltasEvenDuringReentrantSave()
    {
        using var f = new Fixture(options);
        f.Seed();
        SeedProgress(f);
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (_, reputation) = Configure(f, actor);
        reputation.UpdateReputation(Faction.Exile, 25);
        var observed = new List<(string Kind, float Amount)>();
        void RequestInitial() => reputation.SendInitialStanding(standing =>
        {
            actor.Player.Session.EnqueueMessageEncrypted(new ServerPlayerCreate
                { FactionData = new() { FactionReputations = standing } });
            observed.Add(("dependent", 0f));
        });
        session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPlayerCreate initial)
            {
                observed.Add(("create", Assert.Single(initial.FactionData.FactionReputations).ReputationAmount));
                if (observed.Count == 1)
                {
                    using var context = f.Context();
                    actor.Player.Save(context);
                    context.SaveChanges();
                    actor.Events.Update(.01);
                    Assert.Empty(ReputationPackets(session));
                }
            }
            else if (args[0] is ServerReputationUpdate update) observed.Add(("delta", update.ReputationDelta));
            return null;
        });
        RequestInitial();
        Assert.Equal(new[] { ("create", 100f), ("dependent", 0f) }, observed);
        await actor.Drain();
        RequestInitial();
        Assert.Equal(new[] { ("create", 100f), ("dependent", 0f), ("delta", 25f),
            ("create", 125f), ("dependent", 0f) }, observed);
    }

    [Fact]
    public async Task InitialStandingIncludesQuietCommitBeforeFirstQueuedDelta()
    {
        using var f = new Fixture(options);
        f.Seed();
        SeedProgress(f);
        using var actor = CreatePublicationActor(f, out var session);
        await actor.Load();
        var (_, reputation) = Configure(f, actor);
        reputation.GetReputation(Faction.Exile).Amount = 200;
        await Persist(f, actor);

        reputation.UpdateReputation(Faction.Exile, 25);
        await Persist(f, actor);
        reputation.SendInitialStanding(standing => Assert.Equal(200f, Assert.Single(standing).ReputationAmount));
        Assert.Empty(ReputationPackets(session));

        await actor.Drain();
        Assert.Equal(25f, Assert.Single(ReputationPackets(session)).ReputationDelta);
        reputation.SendInitialStanding(standing => Assert.Equal(225f, Assert.Single(standing).ReputationAmount));
        using var read = f.Context();
        Assert.Equal(225f, Assert.Single(read.CharacterReputation).Amount);
    }

    private static IEnumerable<ServerReputationUpdate> ReputationPackets(RecordingDispatchProxy<IWorldSession> session)
        => session.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerReputationUpdate>();
}
