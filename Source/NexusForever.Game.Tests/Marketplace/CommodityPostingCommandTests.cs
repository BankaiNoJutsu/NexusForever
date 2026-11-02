using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Mail;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using static NexusForever.Game.Marketplace.GlobalMarketplaceManager;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Marketplace;

public sealed class CommodityPostingCommandTests
{
    [Theory]
    [InlineData(true, false, false, "commit")]
    [InlineData(false, false, false, "commit")]
    [InlineData(true, false, true, "commit")]
    [InlineData(false, false, true, "commit")]
    [InlineData(true, true, false, "commit")]
    [InlineData(false, true, false, "commit")]
    [InlineData(true, true, true, "commit")]
    [InlineData(false, true, true, "commit")]
    [InlineData(true, false, true, "lost")]
    [InlineData(false, true, true, "lost")]
    [InlineData(true, false, true, "rollback")]
    [InlineData(false, true, true, "rollback")]
    [InlineData(true, false, true, "forged-wallet")]
    [InlineData(true, false, true, "forged-item")]
    [InlineData(true, false, true, "forged-owner")]
    [InlineData(true, false, true, "forged-mail")]
    [InlineData(true, false, true, "forged-material")]
    [InlineData(true, false, true, "forged-empty-mail")]
    public async Task CompletePostingGraphCommitsOrRollsBackWithoutSplittingRoles(bool buy, bool immediate, bool match, string mode)
    {
        using var f = new Fixture(null); f.Seed();
        var now = DateTime.UtcNow;
        var incoming = new CommodityRow(2, 42, 7001, 3, buy ? 12ul : 10ul, buy ? 36ul : 30ul, buy, immediate,
            (ulong)now.ToFileTimeUtc(), (ulong)now.AddHours(1).ToFileTimeUtc());
        var opposite = new CommodityRow(1, 100, 7001, 2, buy ? 10ul : 12ul, buy ? 20ul : 24ul, !buy, false,
            (ulong)now.AddMinutes(-1).ToFileTimeUtc(), incoming.Expiration);
        var before = new BidItem(900, 42, 7001, (ushort)InventoryLocation.Inventory, 0, 10, 0, 1, 0, false, null, null);
        string route;
        using (var seed = f.Context())
        {
            seed.Character.Single().AccountId = 12;
            seed.Character.Add(new() { Id = 100, AccountId = 13, Name = "Counterparty", OriginalName = "Counterparty" });
            seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 120 });
            if (!buy) seed.Item.Add(before.Model());
            if (match) seed.MarketplaceCommodityOrder.Add(opposite.Model());
            await seed.SaveChangesAsync(); route = CharacterDatabaseIdentity.Get(seed);
            // The provider supplies codec defaults for a newly inserted row.
            // Commands capture its stored representation, never the seed input.
            if (!buy) before = BidItem.Capture(await seed.Item.AsNoTracking().SingleAsync(row => row.Id == 900));
        }
        var quote = new CommodityPostingQuote(incoming, match ? [opposite] : [], f.Source.Tables);
        var wallet = new BidWallet(42, 12, true, 120, quote.WalletAfter(42, 120, 1000), 1000);
        List<CommodityPostingItem> items = [];
        List<CommodityPostingMail> mails = [];
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap); ulong mailId = 99;
        ap.SetMethodReturnFactory("get_" + nameof(IAssetManager.NextMailId), () => mailId++);
        uint delivery = quote.Deliveries.GetValueOrDefault(42ul);
        if (buy && delivery != 0) items.Add(new(null, new(1000, 42, 7001, (ushort)InventoryLocation.Inventory, 0, delivery, 0, 1, 0, false, "", ""), 0, delivery));
        if (!buy) items.Add(new(before, before with { Quantity = 10 - quote.Consumption + delivery }, quote.Consumption, delivery));
        if (quote.Deliveries.TryGetValue(100, out uint offline))
        {
            var item = new BidItem(2000, 100, 7001, (ushort)InventoryLocation.None, 0, offline, 0, 1, 0, false, "", "");
            items.Add(new(null, item, 0, offline));
            mails.Add(new(MarketplaceMailDelivery.CreateCommodityReturn(100, f.Source.Tables, assets), [item.Id]));
        }
        foreach (var credit in quote.Credits.Where(row => row.Character == 100))
            mails.Add(new(MarketplaceMailDelivery.CreateNormalBidRefund(100, credit.Amount, f.Source.Tables, assets), []));
        var fault = new CompletionFault(mode == "lost");
        BidCharacter[] owners = match ? [new(42, 12), new(100, 13)] : [new(42, 12)];
        CommodityPostingMaterial[] materials = [];
        switch (mode)
        {
            case "forged-wallet": wallet = wallet with { After = wallet.After + 1 }; break;
            case "forged-item": items[0] = items[0] with { Delivered = items[0].Delivered + 1 }; break;
            case "forged-owner": owners[0] = new(42, 14); break;
            case "forged-mail": mails[0] = mails[0] with { Header = mails[0].Header with { CurrencyAmount = mails[0].Header.CurrencyAmount + 1 } }; break;
            case "forged-material": materials = [new(42, new(1, false, 0, 1, 100))]; break;
            case "forged-empty-mail": mails.Add(new(MarketplaceMailDelivery.CreateCommodityReturn(42, f.Source.Tables, assets), [])); break;
        }
        CommodityPostingCommand Make() => new(() => mode == "commit" ? f.Context() : f.Context(fault), route,
            Guid.NewGuid().ToString("N"), now, quote, owners, [wallet], items, materials, mails, 20);
        if (mode.StartsWith("forged", StringComparison.Ordinal))
        {
            Assert.Throws<InvalidOperationException>(() => Make());
            using var unchanged = f.Context();
            Assert.Equal(120ul, unchanged.CharacterCurrency.Single().Amount); Assert.Single(unchanged.MarketplaceCommodityOrder);
            Assert.Empty(unchanged.CharacterMail); Assert.Empty(unchanged.Item); Assert.Empty(unchanged.MarketplaceBidReceipt); return;
        }
        var command = Make();
        var result = await command.ExecuteAsync();
        Assert.Equal(new BidOutcome(mode != "rollback", true), result);
        Assert.Equal(result, await command.ResolveAsync());
        using var read = f.Context();
        Assert.Equal(mode == "rollback" ? 120ul : wallet.After, read.CharacterCurrency.Single().Amount);
        Assert.Equal(mode == "rollback" ? match ? 1 : 0 : immediate ? 0 : 1, read.MarketplaceCommodityOrder.Count());
        Assert.Equal(mode == "rollback" ? 0 : 1, read.MarketplaceBidReceipt.Count());
        if (mode == "rollback")
        {
            Assert.Empty(read.CharacterMail); Assert.Equal(buy ? 0 : 10, read.Item.ToArray().Sum(row => (int)row.StackCount));
        }
        else
        {
            Assert.Equal(mails.Count, read.CharacterMail.Count());
            Assert.Equal(items.Sum(row => (long)row.After.Quantity), read.Item.ToArray().Sum(row => (long)row.StackCount));
            if (match) Assert.Equal(1u, quote.Fills.Single().Buy.Quantity + quote.Fills.Single().Sell.Quantity);
        }
    }

    private sealed class CompletionFault(bool lost) : DbTransactionInterceptor
    {
        private int fired;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!lost && Interlocked.Exchange(ref fired, 1) == 0) throw new IOException("Posting transaction rolled back.");
            return new(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            if (lost && Interlocked.Exchange(ref fired, 1) == 0) return Task.FromException(new IOException("Posting committed; acknowledgement lost."));
            return Task.CompletedTask;
        }
    }
}
