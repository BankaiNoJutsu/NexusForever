using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Challenges;
using NexusForever.Game.Reward;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Reward;
using static NexusForever.Game.Tests.Challenges.ChallengeRewardPersistenceOwnershipTests;
using Fixture = NexusForever.Game.Tests.Challenges.CharacterChallengeRewardRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Challenges;

public sealed class CharacterChallengeRewardRoutingTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public CharacterChallengeRewardRoutingTests() { }
    internal CharacterChallengeRewardRoutingTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)] [InlineData(19u)] [InlineData(20u)]
    [InlineData(21u)] [InlineData(22u)] [InlineData(23u)] [InlineData(24u)] [InlineData(25u)] [InlineData(26u)]
    [InlineData(27u)] [InlineData(28u)] [InlineData(33u)] [InlineData(36u)]
    public async Task EverySupportedCatalogClaimsThroughTheRegisteredPacketOwnerWithBoundInventory(uint track)
    {
        using var h = await Harness.Create(options, track); uint row = h.SelectedRows()[0];
        h.Claim(row, 0); Assert.True(h.Owner.HasPendingRewardOperation); await h.Drain();
        Assert.False(h.Owner.RequiresAuthoritativeReload); Assert.Empty(h.Loyalty.Invocations);
        using var read = h.F.Context(); var claim = Assert.Single(read.CharacterChallengeRewardClaim);
        Assert.Equal(track, claim.TrackId); Assert.Equal(row, claim.RewardRowId); Assert.Equal(0u, claim.Choice);
        Assert.All(read.Item, item => { Assert.True(item.Soulbound); Assert.Equal(42ul, item.OwnerId); Assert.Equal((ushort)InventoryLocation.Inventory, item.Location); });
        Assert.NotEmpty(read.Item); Assert.True(h.Owner.CommittedCharacterRewards.Tracks.Single().Slots[0].Claimed);
        var reload = h.Reloads.Last(); Assert.Equal(new uint[] { 9, track }, reload.RewardsTracks.Select(value => value.RewardTrackId));
        Assert.Equal(9u, h.Reloads.First().RewardsTracks.First().RewardTrackId);
    }

    [Fact]
    public async Task FinalFirstClaimsThenRepeatedStaticRowsUseNewDurableCyclesAndReceiptIdentities()
    {
        using var h = await Harness.Create(options, 16, multiples: 2); uint[] selected = h.SelectedRows();
        foreach (int ordinal in new[] { 2, 0, 1 }) { h.Claim(selected[ordinal], 0); await h.Drain(); }
        var current = Assert.Single(h.Owner.CommittedCharacterRewards.Tracks); Assert.Equal(1ul, current.CycleId); Assert.Equal(400u, current.Points);
        Assert.Equal(selected, current.Slots.Select(slot => slot.RewardRowId)); Assert.All(current.Slots, slot => Assert.False(slot.Claimed));
        h.Claim(selected[0], 0); await h.Drain();
        using var read = h.F.Context(); var claims = read.CharacterChallengeRewardClaim.Where(row => row.Ordinal == 0).AsEnumerable().OrderBy(row => row.CycleId).ToArray();
        Assert.Equal(new ulong[] { 0, 1 }, claims.Select(row => row.CycleId)); Assert.NotEqual(claims[0].ClaimId, claims[1].ClaimId);
        Assert.Equal(4, read.CharacterChallengeRewardClaim.Count()); Assert.Equal(1ul, read.CharacterChallengeRewardTrack.Single().CycleId);
        Assert.Empty(h.Loyalty.Invocations); Assert.False(h.Owner.RequiresAuthoritativeReload);
    }

    [Theory]
    [InlineData("choice")] [InlineData("insufficient")] [InlineData("claimed")] [InlineData("busy")]
    [InlineData("unpublished")] [InlineData("invalidated")] [InlineData("foreign-session")]
    public async Task RejectedClaimsDoNotAllocateItemsOrFallThroughToLoyalty(string reason)
    {
        using var h = await Harness.Create(options, 16, earned: reason != "insufficient", accountReady: reason != "unpublished");
        uint row = h.SelectedRows()[0]; uint choice = reason == "choice" ? uint.MaxValue : 0;
        if (reason == "claimed") { h.Claim(row, 0); await h.Drain(); }
        if (reason == "busy") Assert.True(h.Owner.ChooseRewardTrack(16));
        if (reason == "invalidated") h.Owner.InvalidateRewardOperations();
        if (reason == "foreign-session") h.F.PlayerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        int ids = h.F.ManagerProxy.GetInvocations("get_NextItemId").Count;
        h.Claim(row, choice); if (reason == "busy") await h.Drain();
        Assert.Equal(ids, h.F.ManagerProxy.GetInvocations("get_NextItemId").Count); Assert.Empty(h.Loyalty.Invocations);
        using var read = h.F.Context(); Assert.Equal(reason == "claimed" ? 1 : 0, read.CharacterChallengeRewardClaim.Count());
    }

    [Fact]
    public async Task AnUnselectedCandidateFromTheSameOrdinalCannotReplaceTheDurableRow()
    {
        using var h = await Harness.Create(options, 17);
        var state = Assert.Single(h.Owner.CommittedCharacterRewards.Tracks);
        var definition = h.F.Catalog.GetTrack(17);
        var slot = definition.Slots.First(slot => slot.Candidates.Count > 1);
        uint actual = state.Slots.Single(value => value.Ordinal == slot.Position).RewardRowId;
        uint foreign = slot.Candidates.First(value => value.Id != actual).Id;
        h.Claim(foreign, 0); Assert.False(h.Owner.HasPendingRewardOperation);
        Assert.Empty(h.F.ManagerProxy.GetInvocations("get_NextItemId")); Assert.Empty(h.Loyalty.Invocations);
        using var read = h.F.Context(); Assert.Empty(read.CharacterChallengeRewardClaim);
    }

    [Fact]
    public async Task AForeignRevisionAfterPublicationRequiresReloadWithoutAProvisionalItem()
    {
        using var h = await Harness.Create(options, 16); uint row = h.SelectedRows()[0];
        using (var foreign = h.F.Context()) { foreign.CharacterChallengeRewardTrack.Single().Revision++; foreign.SaveChanges(); }
        h.Claim(row, 0); await h.Drain();
        Assert.True(h.Owner.RequiresAuthoritativeReload); Assert.Single(h.Session.GetInvocations(nameof(IGameSession.ForceDisconnect)));
        Assert.Empty(h.F.ManagerProxy.GetInvocations("get_NextItemId"));
        using var read = h.F.Context(); Assert.Empty(read.Item); Assert.Empty(read.CharacterChallengeRewardClaim);
    }

    [Fact]
    public async Task AClaimWithLostAcknowledgementRetainsItsCycleChoiceAndItemIdentityThroughRecovery()
    {
        bool armed = false; int contexts = 0; var ack = new LostAck();
        using var h = await Harness.Create(options, 16, contextFactory: f =>
        {
            if (!armed) return f.Context();
            if (Interlocked.Increment(ref contexts) == 3) throw new IOException("Routing claim recovery unavailable once.");
            return f.Intercepted(ack);
        });
        uint row = h.SelectedRows()[0]; armed = true; h.Claim(row, 0); await h.Drain();
        Assert.False(h.Owner.RequiresAuthoritativeReload); Assert.Equal(2, h.Reloads.Count());
        using var read = h.F.Context(); Assert.Single(read.CharacterChallengeRewardClaim); Assert.Single(read.Item);
        Assert.Single(h.F.ManagerProxy.GetInvocations("get_NextItemId"));
        h.Claim(row, 0); Assert.False(h.Owner.HasPendingRewardOperation); Assert.Single(read.CharacterChallengeRewardClaim);
    }

    [Fact]
    public async Task AReplacementManagerReloadsClaimedStateAndCannotReplayTheOldDelivery()
    {
        using var h = await Harness.Create(options, 16); uint row = h.SelectedRows()[0]; h.Claim(row, 0); await h.Drain();
        h.Owner.InvalidateRewardOperations(true);
        using var read = h.F.Context(); var model = new CharacterModel { Id = 42 };
        foreach (var challenge in read.CharacterChallenge.AsNoTracking()) model.Challenge.Add(challenge);
        h.Owner = new ChallengeManager(h.F.Player, model, h.F.Tables, h.F.Context, null, (IItemManager)(object)h.F.ManagerProxy);
        h.F.PlayerProxy.SetProperty(nameof(IPlayer.ChallengeManager), h.Owner); h.Owner.SendInitialPackets(); await h.Drain();
        int ids = h.F.ManagerProxy.GetInvocations("get_NextItemId").Count;
        h.Claim(row, 0); Assert.False(h.Owner.HasPendingRewardOperation); Assert.Equal(ids, h.F.ManagerProxy.GetInvocations("get_NextItemId").Count);
        Assert.Single(read.CharacterChallengeRewardClaim); Assert.Single(read.Item); Assert.False(h.Owner.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task ChoosingANewTrackDoesNotRedirectExistingChallengeEarningOrEraseTheAccountContribution()
    {
        using var h = await Harness.Create(options, 16); h.Choose(33); await h.Drain();
        var tracks = h.Owner.CommittedCharacterRewards.Tracks; Assert.Equal(2, tracks.Count);
        Assert.True(tracks.Single(track => track.TrackId == 33).Active); Assert.False(tracks.Single(track => track.TrackId == 16).Active);
        Assert.Equal(300u, tracks.Single(track => track.TrackId == 16).Points); Assert.Equal(0u, tracks.Single(track => track.TrackId == 33).Points);
        Assert.Equal(new uint[] { 9, 16, 33 }, h.Reloads.Last().RewardsTracks.Select(row => row.RewardTrackId));
        Assert.Empty(h.Loyalty.Invocations); Assert.False(h.Owner.RequiresAuthoritativeReload);
    }

    [Theory]
    [InlineData(29u)] [InlineData(30u)] [InlineData(31u)] [InlineData(32u)] [InlineData(34u)] [InlineData(0u)] [InlineData(16383u)]
    public async Task UnsupportedOrUnknownTrackChoicesCannotCreateCycles(uint track)
    {
        using var h = await Harness.Create(options, 16); h.Choose(track);
        Assert.False(h.Owner.HasPendingRewardOperation); Assert.Empty(h.Loyalty.Invocations);
        using var read = h.F.Context(); Assert.Single(read.CharacterChallengeRewardTrack);
    }

    [Fact]
    public async Task CapacityFailureUsesTheExistingInventoryErrorWithoutAllocatingOrChangingTheClaim()
    {
        using var h = await Harness.Create(options, 16); uint row = h.SelectedRows()[0];
        var inventory = RecordingDispatchProxy<IInventory>.Create(out var proxy);
        proxy.SetProperty(nameof(IInventory.SupportsTransactionalSoulboundDelivery), true);
        proxy.SetMethodHandler(nameof(IInventory.TryReserveSoulboundCreation), args => { args[3] = null; args[4] = GenericError.ItemInventoryFull; return false; });
        h.F.PlayerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        h.Claim(row, 0); Assert.False(h.Owner.HasPendingRewardOperation);
        Assert.Equal(GenericError.ItemInventoryFull, Assert.Single(h.F.PlayerProxy.GetInvocations(nameof(IPlayer.SendGenericError))).Arguments[0]);
        Assert.Empty(h.F.ManagerProxy.GetInvocations("get_NextItemId"));
        using var read = h.F.Context(); Assert.Empty(read.CharacterChallengeRewardClaim); Assert.Empty(read.Item);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TrackPrerequisitesAreEvaluatedOnTheActorAndFrozenForTheAcceptedCommand(bool eligible)
    {
        bool meets = eligible; int evaluations = 0;
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IPrerequisiteManager.Meets), _ => { ++evaluations; return meets; });
        using var h = await Harness.Create(options, 16, prerequisites: prerequisites, prerequisiteId: 999);
        uint row = h.SelectedRows()[0]; h.Claim(row, 0); int captured = evaluations; meets = false;
        if (eligible) await h.Drain();
        Assert.Equal(captured, evaluations); Assert.True(evaluations > 0); Assert.False(h.Owner.RequiresAuthoritativeReload);
        using var read = h.F.Context(); Assert.Equal(eligible ? 1 : 0, read.CharacterChallengeRewardClaim.Count());
    }

    [Theory]
    [InlineData(2u)] [InlineData(3u)] [InlineData(1u)] [InlineData(0u)]
    public void RegisteredHandlersDispatchByStaticOwnerWithoutTryingTheOtherOwnerAfterRejection(uint type)
    {
        using var f = new Fixture(options); var tables = f.Tables;
        // Select an actual row of the requested type; unknown uses an absent id.
        var row = tables.RewardTrackRewards.Entries.FirstOrDefault(row => tables.RewardTrack.GetEntry(row.RewardTrackId)?.RewardTrackTypeEnum == type);
        uint track = row?.RewardTrackId ?? 16383, reward = row?.Id ?? 16383;
        var world = RecordingDispatchProxy<IWorldSession>.Create(out var session);
        var account = RecordingDispatchProxy<IAccount>.Create(out var ap); var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
        var loyalty = RecordingDispatchProxy<IAccountLoyaltyManager>.Create(out var lp); var challenge = RecordingDispatchProxy<IChallengeManager>.Create(out var cp);
        var quests = RecordingDispatchProxy<IQuestManager>.Create(out var qp); pp.SetProperty(nameof(IPlayer.QuestManager), quests);
        session.SetProperty(nameof(IWorldSession.Player), player); session.SetProperty(nameof(IWorldSession.Account), account);
        pp.SetProperty(nameof(IPlayer.Session), world); pp.SetProperty(nameof(IPlayer.Account), account); pp.SetProperty(nameof(IPlayer.ChallengeManager), challenge);
        ap.SetProperty(nameof(IAccount.LoyaltyManager), loyalty);
        var registry = Registry(); Dispatch(registry, world, new ClientRewardTrackChooseTrackHandler(tables), 0x07CF, writer => writer.Write(track, 14));
        Dispatch(registry, world, new ClientRewardTrackClaimRewardPointHandler(tables), 0x07CE, writer => { writer.Write(reward, 14); writer.Write(0u); });
        Assert.Equal(type == 2 && row != null ? 1 : 0, lp.GetInvocations(nameof(IAccountLoyaltyManager.ChooseTrack)).Count);
        Assert.Equal(type == 2 && row != null ? 1 : 0, lp.GetInvocations(nameof(IAccountLoyaltyManager.ClaimReward)).Count);
        Assert.Equal(type == 3 && row != null ? 1 : 0, cp.GetInvocations(nameof(IChallengeManager.ChooseRewardTrack)).Count);
        Assert.Equal(type == 3 && row != null ? 1 : 0, cp.GetInvocations(nameof(IChallengeManager.ClaimRewardTrack)).Count);
        Assert.Equal(type is 0 or 1 && row != null ? 1 : 0, qp.GetInvocations(nameof(IQuestManager.ChooseContractRewardTrack)).Count);
    }

    private static MessageManager Registry()
    {
        var registry = new MessageManager(NullLogger<MessageManager>.Instance);
        foreach (Type type in typeof(ClientRewardTrackChooseTrack).Assembly.GetTypes()) registry.RegisterMessage(type);
        registry.RegisterMessageHandler(typeof(ClientRewardTrackChooseTrackHandler)); registry.RegisterMessageHandler(typeof(ClientRewardTrackClaimRewardPointHandler)); return registry;
    }
    private static void Dispatch(MessageManager registry, IWorldSession world, object handler, uint opcode, Action<GamePacketWriter> payload)
    {
        using var bytes = new MemoryStream(); using (var writer = new GamePacketWriter(bytes)) { payload(writer); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(bytes.ToArray()));
        var message = (IReadable)Activator.CreateInstance(registry.GetMessageType((GameMessageOpcode)opcode))!; message.Read(reader);
        registry.GetMessageHandlerDelegate((GameMessageOpcode)opcode)(handler, world, message);
    }

    private sealed class Harness : IDisposable
    {
        public Fixture F;
        public IWorldSession World;
        public RecordingDispatchProxy<IWorldSession> Session;
        public RecordingDispatchProxy<IAccountLoyaltyManager> Loyalty;
        public ChallengeManager Owner;
        public EventQueue Events = new();
        private MessageManager registry;
        public IEnumerable<ServerRewardTracksReload> Reloads => Session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerRewardTracksReload>();
        public uint[] SelectedRows() => Owner.CommittedCharacterRewards.Tracks.First().Slots.OrderBy(slot => slot.Ordinal).Select(slot => slot.RewardRowId).ToArray();
        public static async Task<Harness> Create(DbContextOptions<CharacterContext> options, uint track, bool earned = true, bool accountReady = true,
            uint multiples = 1, IPrerequisiteManager prerequisites = null, uint prerequisiteId = 0, Func<Fixture, CharacterContext> contextFactory = null)
        {
            var h = new Harness { F = new Fixture(options), registry = Registry() }; var f = h.F;
            await f.Begin(track);
            if (earned) await f.Medal(track, 0, new((f.Catalog.GetTrack(track).MaximumPoints * multiples + (multiples > 1 ? 100u : 0)) / 100f, 1));
            PrepareInventory(f); _ = Manager(f);
            using var read = f.Context();
            foreach (uint row in read.CharacterChallengeRewardSlot.AsNoTracking().OrderBy(row => row.Ordinal).Select(row => row.RewardRowId))
            { using var held = f.Reserve(row, 0); Assert.True(held.Cancel()); }
            h.World = RecordingDispatchProxy<IWorldSession>.Create(out h.Session);
            h.Session.SetProperty(nameof(IWorldSession.Player), f.Player); h.Session.SetProperty(nameof(IWorldSession.Account), f.Player.Account); h.Session.SetProperty("Events", h.Events);
            f.PlayerProxy.SetProperty(nameof(IPlayer.Session), h.World);
            ((RecordingDispatchProxy<IAccount>)(object)f.Player.Account).SetProperty(nameof(IAccount.Session), h.World);
            var loyalty = RecordingDispatchProxy<IAccountLoyaltyManager>.Create(out h.Loyalty);
            ((RecordingDispatchProxy<IAccount>)(object)f.Player.Account).SetProperty(nameof(IAccount.LoyaltyManager), loyalty);
            var presentation = new RewardTrackPresentation(f.Player); f.PlayerProxy.SetProperty(nameof(IPlayer.RewardTrackPresentation), presentation);
            if (prerequisiteId != 0) f.Tables.RewardTrack.GetEntry(track).PrerequisiteId = prerequisiteId;
            var model = new CharacterModel { Id = 42 }; foreach (var row in read.CharacterChallenge.AsNoTracking()) model.Challenge.Add(row);
            Func<CharacterContext> contexts = contextFactory == null ? f.Context : () => contextFactory(f);
            h.Owner = new ChallengeManager(f.Player, model, f.Tables, contexts, prerequisites, (IItemManager)(object)f.ManagerProxy);
            f.PlayerProxy.SetProperty(nameof(IPlayer.ChallengeManager), h.Owner); h.Owner.SendInitialPackets();
            if (accountReady) Assert.True(presentation.PublishAccountTracks([new RewardTrack { RewardTrackId = 9, RewardTrackRewardsIds = [276, 312] }]));
            await h.Drain(); return h;
        }
        public void Choose(uint id) => Dispatch(registry, World, new ClientRewardTrackChooseTrackHandler(F.Tables), 0x07CF, writer => writer.Write(id, 14));
        public void Claim(uint row, uint choice) => Dispatch(registry, World, new ClientRewardTrackClaimRewardPointHandler(F.Tables), 0x07CE, writer => { writer.Write(row, 14); writer.Write(choice); });
        public async Task Drain()
        {
            var watch = Stopwatch.StartNew();
            while (Owner.HasPendingRewardOperation)
            {
                Events.Update(.01); Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15), "Reward request did not settle."); await Task.Delay(5);
            }
        }
        public void Dispose() => F.Dispose();
    }

    private sealed class LostAck : DbTransactionInterceptor
    {
        private int commits;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => Interlocked.Increment(ref commits) == 1 ? Task.FromException(new IOException("Routing claim commit acknowledgement fault.")) : Task.CompletedTask;
    }
}
