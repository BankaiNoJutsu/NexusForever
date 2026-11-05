using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SourceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Pet;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public sealed class PetFlairProgressionTests
{
    private readonly DbContextOptions<CharacterContext> providerOptions;
    public PetFlairProgressionTests() { }
    internal PetFlairProgressionTests(DbContextOptions<CharacterContext> providerOptions) => this.providerOptions = providerOptions;

    [Fact]
    public async Task SpellFlairGrantDoesNotPublishBeforeCharacterConfirmation()
    {
        using var f = await Create(); f.Grant(12);
        Assert.True(f.Pets.HasFlair(12)); Assert.Empty(f.Packets<ServerUnlockPetFlair>());
        using (var before = f.Source.Context()) Assert.Empty(before.CharacterPetFlair);
        await f.Persist(); Assert.Equal((ushort)12, Assert.Single(f.Packets<ServerUnlockPetFlair>()).PetFlairId);
    }

    [Fact]
    public async Task RejectedSpellFlairSaveRetainsItsGrantForRetry()
    {
        using var f = await Create();
        using (var setup = f.Source.Context()) await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
            ? "CREATE TRIGGER reject_pet_flair BEFORE INSERT ON character_pet_flair BEGIN SELECT RAISE(ABORT, 'owned flair failure'); END"
            : "CREATE TRIGGER reject_pet_flair BEFORE INSERT ON character_pet_flair FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned flair failure'");
        f.Grant(12); await Assert.ThrowsAsync<DbUpdateException>(() => f.Persist());
        using (var failed = f.Source.Context())
        {
            Assert.Empty(failed.CharacterPetFlair); await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_pet_flair");
        }
        await f.Persist(); using var stored = f.Source.Context();
        Assert.Equal(12u, Assert.Single(stored.CharacterPetFlair).PetFlairId);
    }

    [Fact]
    public async Task FrozenNormalSaveDoesNotFlushAFlairGrantedDuringAuth()
    {
        using var f = await Create(); f.Grant(12); f.Actor.OnAuth = () => f.Grant(13);
        await f.Actor.Player.SaveDirect(); await f.Actor.Drain();
        using (var first = f.Source.Context()) Assert.Equal(new uint[] { 12 }, first.CharacterPetFlair.Select(row => row.PetFlairId).ToArray());
        f.Actor.OnAuth = null; await f.Actor.Player.SaveDirect(); await f.Actor.Drain();
        using var stored = f.Source.Context(); Assert.Equal(new uint[] { 12, 13 }, stored.CharacterPetFlair.OrderBy(row => row.PetFlairId).Select(row => row.PetFlairId));
    }

    [Fact]
    public async Task LostCommitAcknowledgementWaitsForStoredObservationAndPublishesOnce()
    {
        using var f = await Create(); f.Grant(12);
        await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Source.Context(new LostAcknowledgement()), 42, f.Actor.Player.Save));
        await f.Pump(); Assert.Empty(f.Packets<ServerUnlockPetFlair>());
        using (var committed = f.Source.Context()) Assert.Single(committed.CharacterPetFlair);
        await f.Persist(); await f.Persist(); Assert.Single(f.Packets<ServerUnlockPetFlair>());
    }

    [Fact]
    public async Task InitialFlairBitsHideAcceptedButUncommittedUnlocks()
    {
        using var f = await Create(); f.Grant(12); f.Pets.SendInitialPackets(); await f.Pump();
        Assert.False(HasBit(Assert.Single(f.Packets<ServerPetCustomisationList>()), 5));
        await f.Persist(); f.Pets.SendInitialPackets(); await f.Pump();
        Assert.True(HasBit(f.Packets<ServerPetCustomisationList>().Last(), 5));
    }

    [Fact]
    public async Task LoadingGrantPersistsWithoutCreatingALaterIncrementalNotice()
    {
        using var f = await Create(); f.Actor.Player.IsLoading = true; f.Grant(12); await f.Persist();
        f.Actor.Player.IsLoading = false; f.Pets.SendInitialPackets(); await f.Pump();
        Assert.Empty(f.Packets<ServerUnlockPetFlair>()); Assert.True(HasBit(Assert.Single(f.Packets<ServerPetCustomisationList>()), 5));
    }

    [Theory]
    [InlineData("player")] [InlineData("manager")] [InlineData("epoch")] [InlineData("queue")]
    public async Task ReentrantOwnerReplacementStopsLaterFlairNotices(string change)
    {
        using var f = await Create();
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerUnlockPetFlair { PetFlairId: 12 }) return null;
            if (change == "player") f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            if (change == "manager") f.Actor.Set(nameof(Player.PetCustomisationManager), RecordingDispatchProxy<IPetCustomisationManager>.Create(out _));
            if (change == "epoch") ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.WorldSessionEpoch), 77ul);
            if (change == "queue") f.Messages.SetProperty(nameof(IWorldSession.Events), new EventQueue());
            return null;
        });
        f.Grant(12); f.Grant(13); await f.Persist();
        Assert.Equal((ushort)12, Assert.Single(f.Packets<ServerUnlockPetFlair>()).PetFlairId);
        using var stored = f.Source.Context(); Assert.Equal(2, stored.CharacterPetFlair.Count());
    }

    [Fact]
    public async Task RejectedMutableDeltaRetriesFreshWithoutReplayingAnEarlierUnlock()
    {
        using var f = await Create(); ServerUnlockPetFlair rejected = null; int attempts = 0;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerUnlockPetFlair { PetFlairId: 13 } packet && ++attempts == 1)
            { rejected = packet; packet.PetFlairId = 99; throw new IOException("Owned flair enqueue rejection."); }
            return null;
        });
        f.Grant(12); f.Grant(13); await f.Persist();
        Assert.Equal(2, attempts); Assert.Single(f.Packets<ServerUnlockPetFlair>(), row => row.PetFlairId == 12);
        Assert.NotSame(rejected, f.Packets<ServerUnlockPetFlair>().Last()); Assert.Equal((ushort)13, f.Packets<ServerUnlockPetFlair>().Last().PetFlairId);
        using var stored = f.Source.Context(); Assert.Equal(2, stored.CharacterPetFlair.Count());
    }

    [Fact]
    public async Task RejectedInitialBitsRetryFromTheirImmutableSnapshot()
    {
        using var f = await Create(); f.Grant(12); await f.Persist();
        ServerPetCustomisationList rejected = null; int attempts = 0;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPetCustomisationList packet && ++attempts == 1)
            { rejected = packet; packet.UnlockedFlair.GetBuffer()[0] = 0; throw new IOException("Owned initial flair rejection."); }
            return null;
        });
        f.Pets.SendInitialPackets(); await f.Pump();
        Assert.Equal(2, attempts); Assert.NotSame(rejected, f.Packets<ServerPetCustomisationList>().Last());
        Assert.True(HasBit(f.Packets<ServerPetCustomisationList>().Last(), 5)); Assert.Single(f.Packets<ServerUnlockPetFlair>());
    }

    [Fact]
    public async Task UnknownLoadedFlairRowsSurviveAKnownGrant()
    {
        using var f = await Create([999]); f.Grant(12); await f.Persist();
        using var stored = f.Source.Context(); Assert.Equal(new uint[] { 12, 999 }, stored.CharacterPetFlair.OrderBy(row => row.PetFlairId).Select(row => row.PetFlairId));
        Assert.False(f.Pets.HasFlair(999)); Assert.Single(f.Packets<ServerUnlockPetFlair>());
    }

    [Fact]
    public async Task DuplicateProducerGrantsDoNotCreateRowsOrNoticesTwice()
    {
        using var f = await Create(); f.Grant(12); f.Grant(12); await f.Persist(); f.Grant(12); await f.Persist();
        using var stored = f.Source.Context(); Assert.Single(stored.CharacterPetFlair); Assert.Single(f.Packets<ServerUnlockPetFlair>());
    }

    [Fact]
    public async Task UnrepresentableEffectIdCannotAliasAKnownFlair()
    {
        using var f = await Create();
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Source.Tables).SetProperty(nameof(IGameTableManager.PetFlair),
            PlayerBindPointTests.Table(new PetFlairEntry { Id = 12 }, new PetFlairEntry { Id = 65548 }));
        f.Grant(65548); Assert.False(f.Pets.HasFlair(12)); await f.Persist();
        using var stored = f.Source.Context(); Assert.Empty(stored.CharacterPetFlair); Assert.Empty(f.Packets<ServerUnlockPetFlair>());
    }

    [Fact]
    public async Task LegacyFlairRowRetainsItsDirtyGrantAcrossExplicitRollback()
    {
        using var f = await Create(); var flair = new PetFlair(42, new PetFlairEntry { Id = 12 });
        await using (var context = f.Source.Context()) await using (var transaction = await context.Database.BeginTransactionAsync())
        { flair.Save(context); await context.SaveChangesAsync(); await transaction.RollbackAsync(); }
        using (var retry = f.Source.Context()) { flair.Save(retry); await retry.SaveChangesAsync(); }
        using var stored = f.Source.Context(); Assert.Equal(12u, Assert.Single(stored.CharacterPetFlair).PetFlairId);
    }

    private static bool HasBit(ServerPetCustomisationList packet, int index)
        => (packet.UnlockedFlair.GetBuffer()[index / 8] & (1 << (7 - index % 8))) != 0;

    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned pet flair commit acknowledgement loss."));
    }

    private async Task<Fixture> Create(uint[] loadedFlairs = null)
    {
        var source = new SourceFixture(providerOptions); source.Seed();
        using (var setup = source.Context())
        {
            foreach (uint id in loadedFlairs ?? []) setup.CharacterPetFlair.Add(new() { Id = 42, PetFlairId = id });
            await setup.SaveChangesAsync();
        }
        ((RecordingDispatchProxy<IGameTableManager>)(object)source.Source.Tables).SetProperty(nameof(IGameTableManager.PetFlair),
            PlayerBindPointTests.Table(new PetFlairEntry { Id = 12, UnlockBitIndex = [5, 6] }, new PetFlairEntry { Id = 13, UnlockBitIndex = [7, 8] }));
        var events = new EventQueue(); var session = RecordingDispatchProxy<IWorldSession>.Create(out var connection);
        connection.SetProperty(nameof(IWorldSession.Events), events);
        var actor = new Actor(source, actorSession: session, actorEvents: events); connection.SetProperty(nameof(IWorldSession.Player), actor.Player);
        ((RecordingDispatchProxy<IAccount>)(object)actor.Player.Account).SetProperty(nameof(IAccount.Session), session);
        CharacterModel loaded; using (var context = source.Context()) loaded = context.Character.AsNoTracking().Include(row => row.PetFlair).Single();
        var pets = new PetCustomisationManager(actor.Player, loaded, source.Source.Tables); actor.Set(nameof(Player.PetCustomisationManager), pets);
        var services = new ServiceCollection().AddSingleton(source.Source.Tables).BuildServiceProvider();
        var previous = SpellHandler.InitialiseDependencyResolver(new SpellEffectDependencyResolver(services));
        await actor.Load(); return new(source, actor, pets, connection, services, previous);
    }
    private sealed record Fixture(SourceFixture Source, Actor Actor, PetCustomisationManager Pets, RecordingDispatchProxy<IWorldSession> Messages,
        ServiceProvider Services, ISpellEffectDependencyResolver Previous) : IDisposable
    {
        internal void Grant(uint id)
        {
            var spell = RecordingDispatchProxy<ISpell>.Create(out var proxy); proxy.SetProperty(nameof(ISpell.Caster), Actor.Player);
            var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var bp); bp.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 700 });
            var info = RecordingDispatchProxy<ISpellInfo>.Create(out var ip); ip.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 701 }); ip.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
            proxy.SetProperty(nameof(ISpell.Parameters), new SpellParameters { SpellInfo = info });
            var effect = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out var ep);
            ep.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry { EffectType = SpellEffectType.UnlockPetFlair, DataBits00 = id });
            SpellHandler.HandleEffectUnlockPetFlair(spell, Actor.Player, effect);
        }
        internal IEnumerable<T> Packets<T>() => Messages.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(row => row.Arguments[0]).OfType<T>();
        internal async Task Persist() { await CharacterAtomicSave.Execute(() => Source.Context(), 42, Actor.Player.Save); Actor.Events.Update(.01); await Actor.Drain(); }
        internal async Task Pump() { Actor.Events.Update(.01); await Actor.Drain(); }
        public void Dispose() { SpellHandler.InitialiseDependencyResolver(Previous); Services.Dispose(); Actor.Dispose(); Source.Dispose(); }
    }
}
