using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Reward;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Tests.Account.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reward;

namespace NexusForever.Game.Tests.Reward;

/// <summary>Actual Loyalty entrypoints, real Auth persistence and serialized composed reloads.</summary>
public sealed class RewardTrackPresentationLoyaltyIntegrationTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"NexusRewardPresentation_{Guid.NewGuid():N}");
    private readonly DatabaseConnectionString connection;
    private readonly IGameTableManager tables;

    public RewardTrackPresentationLoyaltyIntegrationTests()
    {
        Directory.CreateDirectory(directory);
        connection = new() { Provider = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={Path.Combine(directory, "auth.sqlite")};Pooling=False" };
        tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Tracks()));
        proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Rewards()));
        proxy.SetProperty(nameof(IGameTableManager.AccountItem), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.AccountItems()));
        proxy.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = 8 }));
        using var context = Context();
        context.Database.EnsureCreated();
        var model = new AccountModel { Id = 1, Email = "presentation@example.test", S = "", V = "", GameToken = "", SessionKey = "" };
        AccountDefaultEntitlements.EnsureBaseline(model);
        context.Account.Add(model); context.SaveChanges();
    }

    [Fact]
    public void RealInitialPacketsComposeEightDurableLoyaltyRowsWithReadyCharacterContribution()
    {
        Client client = CreateClient();
        client.Presentation.ExpectCharacterTracks();
        Assert.True(client.Presentation.PublishCharacterTracks([CharacterTrack()], reload: false));
        Assert.Empty(client.Reloads);

        client.Loyalty.SendInitialPackets(client.Player);

        AssertComposed(Assert.Single(client.Reloads), 0, 0);
        using var context = Context();
        Assert.Equal(44, context.AccountLoyaltySlot.Count());
        Assert.Equal(9u, context.AccountLoyalty.Single().ActiveTrackId);
        Assert.Empty(context.AccountPendingItem);
        Assert.Equal(new byte[] { 15, 17 }, context.AccountEntitlement.OrderBy(row => row.EntitlementId).Select(row => row.EntitlementId));
    }

    [Fact]
    public void RealChooseTrackPreservesCharacterRowsAndPersistsAccountSelection()
    {
        SeedWallet(4000);
        Client client = CreateClient();
        client.Presentation.PublishCharacterTracks([CharacterTrack()], reload: false);
        client.Loyalty.SendInitialPackets(client.Player);
        client.SessionProxy.Invocations.Clear();

        Assert.True(client.Loyalty.ChooseTrack(client.Player, 9));

        AssertComposed(Assert.Single(client.Reloads), 4000, 0x3F);
        using var context = Context();
        Assert.Equal(9u, context.AccountLoyalty.Single().ActiveTrackId);
        Assert.Equal(6, context.AccountPendingItem.Count());
    }

    [Fact]
    public void RealEarnedClaimReplayKeepsComposedCharacterRowsAndDoesNotDuplicateDelivery()
    {
        SeedWallet(800);
        Client client = CreateClient();
        client.Presentation.PublishCharacterTracks([CharacterTrack()], reload: false);
        client.Loyalty.SendInitialPackets(client.Player);
        client.SessionProxy.Invocations.Clear();

        // Actual native Loyalty metadata auto-claims earned276 during initialization.
        // Its public claim endpoint must still compose a valid idempotent replay.
        Assert.True(client.Loyalty.ClaimReward(client.Player, 276, 0));

        AssertComposed(Assert.Single(client.Reloads), 800, 1);
        using var context = Context();
        Assert.True(context.AccountLoyaltySlot.Single(row => row.TrackId == 9 && row.Position == 0).Claimed);
        Assert.Single(context.AccountPendingItem);
        Assert.Single(context.AccountPendingItemDelivery, row => row.DeliveryKey == "loyalty:1:9:0");
        Assert.DoesNotContain(context.AccountEntitlement, row => row.EntitlementId == 44);
    }

    [Fact]
    public void RealCommittedEarningCachesSendUpdatesWithoutReloadAndNextCharacterReloadUsesFreshAccountState()
    {
        Client client = CreateClient();
        client.Presentation.PublishCharacterTracks([CharacterTrack()], reload: false);
        client.Loyalty.SendInitialPackets(client.Player);
        client.SessionProxy.Invocations.Clear();

        client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 1600);
        using (var context = Context())
        {
            client.Account.Save(context);
            context.SaveChanges();
        }

        Assert.Empty(client.Reloads);
        Assert.Equal(8, client.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerRewardTrackUpdate>().Count());
        Assert.True(client.Presentation.PublishCharacterTracks([CharacterTrack(300)]));
        AssertComposed(Assert.Single(client.Reloads), 1600, 3, characterPoints: 300);
        using var verify = Context();
        Assert.Equal(1600ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Equal(2, verify.AccountPendingItem.Count());
    }

    [Fact]
    public void RealLoyaltyRebindToAlternateActorSessionDoesNotPublishPreviousCharacterContribution()
    {
        Client first = CreateClient();
        first.Presentation.PublishCharacterTracks([CharacterTrack()], reload: false);
        first.Loyalty.SendInitialPackets(first.Player);
        IGameSession replacement = RecordingDispatchProxy<IGameSession>.Create(out var replacementProxy);
        first.AccountProxy.SetProperty(nameof(IAccount.Session), replacement);
        IPlayer alternate = RecordingDispatchProxy<IPlayer>.Create(out var actorProxy);
        actorProxy.SetProperty(nameof(IPlayer.Account), first.Account);
        actorProxy.SetProperty(nameof(IPlayer.Session), replacement);
        actorProxy.SetProperty(nameof(IPlayer.CharacterId), 200ul);
        actorProxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { Id = 200, RealmId = 1 });
        var presentation = new RewardTrackPresentation(alternate);
        actorProxy.SetProperty(nameof(IPlayer.RewardTrackPresentation), presentation);
        presentation.ExpectCharacterTracks();
        presentation.PublishCharacterTracks([], reload: false);
        int oldPacketCount = first.Reloads.Count();

        first.Loyalty.SendInitialPackets(alternate);

        ServerRewardTracksReload packet = Assert.Single(replacementProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerRewardTracksReload>());
        Assert.Equal(8, Decode(packet).Count);
        Assert.DoesNotContain(16u, Decode(packet).Keys);
        Assert.Equal(oldPacketCount, first.Reloads.Count());
        Assert.False(first.Presentation.PublishCharacterTracks([CharacterTrack(999)]));
        using var verify = Context();
        Assert.Empty(verify.AccountPendingItem);
    }

    [Theory]
    [InlineData("session")]
    [InlineData("character")]
    [InlineData("account")]
    [InlineData("logout")]
    [InlineData("invalidate")]
    public void RealSendUpdatesStopsOldSnapshotTailWhenFirstEnqueueReentersActorLifetime(string boundary)
    {
        Client client = CreateClient();
        client.Presentation.PublishCharacterTracks([CharacterTrack()], reload: false);
        client.Loyalty.SendInitialPackets(client.Player);
        client.SessionProxy.Invocations.Clear();
        IGameSession replacementSession = RecordingDispatchProxy<IGameSession>.Create(out var replacementProxy);
        int updateCallbacks = 0;
        client.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerRewardTrackUpdate || ++updateCallbacks != 1) return null;
            switch (boundary)
            {
                case "session": client.AccountProxy.SetProperty(nameof(IAccount.Session), replacementSession); break;
                case "character": client.PlayerProxy.SetProperty(nameof(IPlayer.CharacterId), 999ul); break;
                case "account":
                    IAccount replacementAccount = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
                    accountProxy.SetProperty(nameof(IAccount.Id), 1u);
                    accountProxy.SetProperty(nameof(IAccount.Session), client.Player.Session);
                    client.PlayerProxy.SetProperty(nameof(IPlayer.Account), replacementAccount);
                    break;
                case "logout":
                    ILogoutManager logout = RecordingDispatchProxy<ILogoutManager>.Create(out var logoutProxy);
                    logoutProxy.SetProperty(nameof(ILogoutManager.State), NexusForever.Game.Static.Entity.LogoutState.Logout);
                    client.PlayerProxy.SetProperty(nameof(IPlayer.LogoutManager), logout);
                    break;
                case "invalidate": client.Presentation.Invalidate(); break;
            }
            return null;
        });

        client.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 1600);
        using (var context = Context()) { client.Account.Save(context); context.SaveChanges(); }

        Assert.Equal(1, updateCallbacks);
        Assert.Single(client.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerRewardTrackUpdate>());
        Assert.Empty(replacementProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerRewardTrackUpdate>());
        Assert.Empty(client.Reloads);
        using var verify = Context();
        Assert.Equal(1600ul, verify.AccountLoyalty.Single().EarnedPoints);
        Assert.Equal(2, verify.AccountPendingItem.Count());
    }

    [Fact]
    public void RealChooseTrackReenteredByFirstUpdateStopsOlderStateTailAndReloadsLatestSelection()
    {
        SeedWallet(4000);
        Client client = CreateClient();
        client.Presentation.PublishCharacterTracks([CharacterTrack()], reload: false);
        client.Loyalty.SendInitialPackets(client.Player);
        using (var before = Context()) Assert.Equal(10u, before.AccountLoyalty.Single().ActiveTrackId);
        client.SessionProxy.Invocations.Clear();
        bool reentered = false;
        client.SessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerRewardTrackUpdate && !reentered)
            {
                reentered = true;
                // Both choices use real durable Auth units on the same manager.
                // This changes the state reference while the outer batch is sending.
                Assert.True(client.Loyalty.ChooseTrack(client.Player, 10));
            }
            return null;
        });

        Assert.True(client.Loyalty.ChooseTrack(client.Player, 9));

        Assert.True(reentered);
        object[] packets = client.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).ToArray();
        // One outer update, then eight updates from the newer nested choice.
        // The remaining seven rows from the old outer snapshot must never follow.
        Assert.Equal(9, packets.OfType<ServerRewardTrackUpdate>().Count());
        Assert.Equal(2, client.Reloads.Count());
        int firstReload = Array.FindIndex(packets, packet => packet is ServerRewardTracksReload);
        Assert.True(firstReload >= 0);
        Assert.DoesNotContain(packets.Skip(firstReload + 1), packet => packet is ServerRewardTrackUpdate);
        foreach (ServerRewardTracksReload packet in client.Reloads)
        {
            AssertComposed(packet, 4000, 0x3F);
            Dictionary<uint, DecodedRow> rows = Decode(packet);
            Assert.False(rows[9].Active);
            Assert.True(rows[10].Active);
            Assert.True(rows[16].Active);
        }
        using var after = Context();
        Assert.Equal(10u, after.AccountLoyalty.Single().ActiveTrackId);
        Assert.Equal(6, after.AccountPendingItem.Count());
    }

    private Client CreateClient()
    {
        using var context = Context();
        AccountModel model = context.Account.AsNoTracking().Include(row => row.AccountCurrency)
            .Include(row => row.AccountPendingItem).Include(row => row.AccountInventory)
            .Include(row => row.Loyalty).Include(row => row.LoyaltySlots).Single();
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), 1u);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        var currency = new AccountCurrencyManager(account, model, tables);
        accountProxy.SetProperty(nameof(IAccount.CurrencyManager), currency);
        var inventory = new AccountInventoryManager(account, model, null, gameTableManager: tables);
        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventory);
        var database = new AuthDatabase(); database.Initialise(connection);
        var loyalty = new AccountLoyaltyManager(account, tables, database);
        accountProxy.SetProperty(nameof(IAccount.LoyaltyManager), loyalty);
        accountProxy.SetMethodHandler(nameof(IAccount.Save), args =>
        {
            var unit = (AuthContext)args[0]; currency.Save(unit); inventory.Save(unit); loyalty.Save(unit); return null;
        });
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Account), account);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 100ul);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { Id = 100, RealmId = 1 });
        var presentation = new RewardTrackPresentation(player);
        playerProxy.SetProperty(nameof(IPlayer.RewardTrackPresentation), presentation);
        return new Client(account, accountProxy, player, playerProxy, currency, loyalty, presentation, sessionProxy);
    }

    private static RewardTrack CharacterTrack(uint points = 200)
    {
        var row = new RewardTrack { RewardTrackId = 16, RewardPointsEarned = points, IsActive = true,
            RewardTrackRewardsIds = [320, 321, 324] };
        row.ClaimedRewards.SetBit(0, true);
        return row;
    }

    private static void AssertComposed(ServerRewardTracksReload packet, uint accountPoints, uint accountClaimed, uint characterPoints = 200)
    {
        var decoded = Decode(packet);
        Assert.Equal(9, decoded.Count);
        Assert.Equal(accountPoints, decoded[9].Points);
        Assert.Equal(accountClaimed, decoded[9].Claimed);
        Assert.Equal(new uint[] { 276, 312, 278, 280, 311, 279 }, decoded[9].Rewards);
        Assert.Equal(characterPoints, decoded[16].Points);
        Assert.Equal(1u, decoded[16].Claimed);
        Assert.Equal(new uint[] { 320, 321, 324 }, decoded[16].Rewards);
        Assert.True(decoded[16].Active);
    }

    private static Dictionary<uint, DecodedRow> Decode(ServerRewardTracksReload packet)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream)) { packet.Write(writer); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        uint count = reader.ReadUInt();
        var result = new Dictionary<uint, DecodedRow>();
        for (uint i = 0; i < count; i++)
        {
            uint id = reader.ReadUInt(14), points = reader.ReadUInt(), claimed = reader.ReadUInt(), rowCount = reader.ReadUInt();
            reader.ReadUInt();
            var rewards = new uint[rowCount];
            for (int j = 0; j < rewards.Length; j++) rewards[j] = reader.ReadUInt();
            result.Add(id, new DecodedRow(points, claimed, rewards, reader.ReadBit()));
        }
        return result;
    }

    private AuthContext Context() => new(connection);
    private void SeedWallet(ulong amount)
    {
        using var context = Context();
        context.AccountCurrency.Add(new AccountCurrencyModel { Id = 1, CurrencyId = 8, Amount = amount });
        context.SaveChanges();
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);
    private sealed record DecodedRow(uint Points, uint Claimed, uint[] Rewards, bool Active);
    private sealed record Client(IAccount Account, RecordingDispatchProxy<IAccount> AccountProxy, IPlayer Player, RecordingDispatchProxy<IPlayer> PlayerProxy,
        AccountCurrencyManager Currency, AccountLoyaltyManager Loyalty, RewardTrackPresentation Presentation,
        RecordingDispatchProxy<IGameSession> SessionProxy)
    {
        public IEnumerable<ServerRewardTracksReload> Reloads => SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerRewardTracksReload>();
    }
}
