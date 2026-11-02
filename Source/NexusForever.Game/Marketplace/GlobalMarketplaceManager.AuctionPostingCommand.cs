using System.Data;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        internal sealed record AuctionPostingView(BidOutcome Outcome, BidWalletCurrent Wallet, AuctionRow Auction, BidItem Item, bool CustodyMatches);
        internal sealed record AuctionPostingExecution(Func<AuctionPostingCommand, Task<BidOutcome>> Execute);
        private readonly AuctionPostingExecution auctionPostingExecution;
        internal GlobalMarketplaceManager(NexusForever.Database.IDatabaseManager databases, NexusForever.GameTable.IGameTableManager tables,
            NexusForever.Game.Abstract.IAssetManager assets, NexusForever.Game.Abstract.Entity.IPlayerManager players,
            NexusForever.Game.Abstract.IItemManager items, NexusForever.Game.Abstract.ICleanupManager cleanup,
            AuctionPostingExecution execution) : this(databases, tables, assets, players, items, cleanup) => auctionPostingExecution = execution;

        // Fresh SQL contexts receive only the captured scalar graph, never a live
        // Item, actor, request, reservation or scoped handler.
        internal sealed class AuctionPostingCommand
        {
            private static readonly object gate = new();
            private static readonly ConditionalWeakTable<CharacterContext, object> owned = new();
            private readonly Func<CharacterContext> contexts;
            private readonly string database, id, payload;
            private readonly DateTime created;
            private readonly AuctionRow target;
            private readonly BidItem before, after;
            private readonly BidWallet wallet;
            private int started;
            internal string ReceiptId => id;
            internal string PayloadSHA256 => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

            internal AuctionPostingCommand(Func<CharacterContext> contexts, string database, string id, DateTime created,
                AuctionRow target, BidItem before, BidWallet wallet)
            {
                this.contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
                this.database = database ?? throw new ArgumentNullException(nameof(database));
                if (!Guid.TryParseExact(id, "N", out var identity) || identity == Guid.Empty || id != identity.ToString("N")
                    || target == null || before == null || wallet == null || target.Id == 0 || target.Owner == 0
                    || target.Owner != before.Owner || target.Item != before.Id || target.Definition != before.Definition
                    || target.Quantity == 0 || target.Quantity != before.Quantity || before.Bound
                    || before.Location != (ushort)InventoryLocation.Inventory || target.MinimumBid == 0
                    || target.Buyout != 0 && target.Buyout < target.MinimumBid || target.Bid != 0 || target.Bidder != 0
                    || wallet.Character != target.Owner || wallet.Account == 0 || wallet.After != wallet.Before
                    || !wallet.Exists && wallet.Before != 0 || wallet.Cap != 0 && wallet.Before > wallet.Cap)
                    throw new InvalidOperationException("Auction posting lost its captured owner, whole Item, wallet or new listing.");
                this.id = id; this.target = target; this.before = before; this.wallet = wallet;
                this.created = new DateTime(created.Ticks / 10 * 10, created.Kind);
                after = before with { Owner = null, Location = (ushort)InventoryLocation.None, Bag = 0 };
                payload = JsonSerializer.Serialize(new { Version = 1, Operation = "AuctionPosting",
                    Database = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(database))),
                    Created = this.created, Listing = target, Before = before, After = after, Wallet = wallet });
            }

            private CharacterContext NewContext()
            {
                var context = contexts() ?? throw new InvalidOperationException("Auction posting context factory returned null.");
                lock (gate)
                {
                    if (owned.TryGetValue(context, out _) || context.AtomicCharacterId.HasValue || context.Database.CurrentTransaction != null
                        || context.ChangeTracker.Entries().Any() || context.IsSaveRejected || context.HasSavepointRollback)
                        throw new InvalidOperationException("Auction posting requires fresh exclusively owned contexts.");
                    owned.Add(context, new object());
                }
                try { CharacterDatabaseIdentity.Require(context, database); return context; }
                catch { context.Dispose(); throw; }
            }

            internal async Task<BidOutcome> ExecuteAsync()
            {
                if (Interlocked.Exchange(ref started, 1) != 0) throw new InvalidOperationException("Auction posting already started.");
                try
                {
                    await using var driver = NewContext();
                    return await driver.Database.CreateExecutionStrategy().ExecuteAsync(Attempt);
                }
                catch { return await ResolveAsync(); }
            }

            internal async Task<BidOutcome> ResolveAsync() => (await ReadReconciliationAsync()).Outcome;

            internal async Task<AuctionPostingView> ReadReconciliationAsync()
            {
                await using var driver = NewContext();
                return await driver.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var context = NewContext();
                    await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    await Lock(context);
                    var outcome = await ReadOutcome(context);
                    var owner = await context.Character.AsNoTracking().SingleOrDefaultAsync(row => row.Id == wallet.Character);
                    if (owner == null || owner.DeleteTime != null || owner.AccountId != wallet.Account)
                        throw new InvalidOperationException("Auction posting reconciliation lost its original Character/account.");
                    var cash = await context.CharacterCurrency.AsNoTracking().SingleOrDefaultAsync(row => row.Id == wallet.Character && row.CurrencyId == (byte)CurrencyType.Credits);
                    if (cash != null && wallet.Cap != 0 && cash.Amount > wallet.Cap)
                        throw new InvalidOperationException("Auction posting reconciliation cannot adopt an over-cap wallet.");
                    return new AuctionPostingView(outcome, new(wallet.Character, wallet.Account, cash != null, cash?.Amount ?? 0),
                        AuctionRow.Capture(await context.MarketplaceAuction.AsNoTracking().SingleOrDefaultAsync(row => row.Id == target.Id)),
                        BidItem.Capture(await context.Item.AsNoTracking().SingleOrDefaultAsync(row => row.Id == before.Id)),
                        await MatchesCustody(context, outcome.Committed));
                });
            }

            private async Task<BidOutcome> Attempt()
            {
                await using var context = NewContext();
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                await Lock(context);
                if (await context.MarketplaceBidReceipt.AnyAsync(row => row.Id == id)) return await ReadOutcome(context);
                if (!await Matches(context, false) || !TimeValid)
                    throw new InvalidOperationException("Auction posting lost its complete preimage or new listing expiry.");
                var item = await context.Item.SingleAsync(row => row.Id == before.Id);
                item.OwnerId = null; item.Location = after.Location; item.BagIndex = after.Bag;
                context.MarketplaceAuction.Add(target.Model());
                context.MarketplaceBidReceipt.Add(new() { Id = id, Payload = payload, CreatedUtc = created });
                context.ChangeTracker.DetectChanges();
                var sealedRows = context.ChangeTracker.Entries().Select(entry => (Entry: entry, Values: entry.CurrentValues.Clone())).ToArray();
                context.OnSaveValidation(phase =>
                {
                    if (!ReferenceEquals(context.Database.CurrentTransaction, transaction) || context.HasSavepointRollback || !TimeValid
                        || context.ChangeTracker.Entries().Any(entry => !sealedRows.Any(row => ReferenceEquals(row.Entry.Entity, entry.Entity)))
                        || sealedRows.Any(row => !row.Values.Properties.All(p => Equals(row.Entry.CurrentValues[p.Name], row.Values[p.Name]))))
                        throw new InvalidOperationException("Auction posting changed its sealed transaction graph.");
                    if (phase != CharacterSaveValidationPhase.BeforeFlush
                        && (!Matches(context, true).GetAwaiter().GetResult()
                            || context.MarketplaceBidReceipt.AsNoTracking().SingleOrDefault(row => row.Id == id)?.Payload != payload))
                        throw new InvalidOperationException("Auction posting SQL differs from its complete postimage.");
                });
                await context.SaveChangesAsync(); await transaction.CommitAsync();
                return new(true, true);
            }

            private bool TimeValid => target.Expiration > (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            private async Task Lock(CharacterContext context)
            {
                bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported auction posting provider.");
                _ = await (mysql ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={wallet.Character} FOR UPDATE")
                    : context.Character.Where(row => row.Id == wallet.Character)).AsNoTracking().SingleOrDefaultAsync();
                _ = await (mysql ? context.MarketplaceAuction.FromSqlInterpolated($"SELECT * FROM `marketplace_auction` WHERE id={target.Id} FOR UPDATE")
                    : context.MarketplaceAuction.Where(row => row.Id == target.Id)).AsNoTracking().SingleOrDefaultAsync();
                _ = await (mysql ? context.Item.FromSqlInterpolated($"SELECT * FROM `item` WHERE id={before.Id} FOR UPDATE")
                    : context.Item.Where(row => row.Id == before.Id)).AsNoTracking().SingleOrDefaultAsync();
            }

            private async Task<BidOutcome> ReadOutcome(CharacterContext context)
            {
                var receipt = await context.MarketplaceBidReceipt.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id);
                if (receipt != null && receipt.Payload != payload) throw new InvalidOperationException("Auction posting receipt belongs to another command.");
                return new(receipt != null, await Matches(context, receipt != null));
            }

            private async Task<bool> Matches(CharacterContext context, bool committed)
            {
                var owner = await context.Character.AsNoTracking().SingleOrDefaultAsync(row => row.Id == wallet.Character);
                var cash = await context.CharacterCurrency.AsNoTracking().SingleOrDefaultAsync(row => row.Id == wallet.Character && row.CurrencyId == (byte)CurrencyType.Credits);
                return owner != null && owner.DeleteTime == null && owner.AccountId == wallet.Account
                    && (cash == null ? !wallet.Exists : wallet.Exists && cash.Amount == wallet.Before)
                    && await MatchesCustody(context, committed);
            }

            private async Task<bool> MatchesCustody(CharacterContext context, bool committed) =>
                    AuctionRow.Capture(await context.MarketplaceAuction.AsNoTracking().SingleOrDefaultAsync(row => row.Id == target.Id)) == (committed ? target : null)
                    && BidItem.Capture(await context.Item.AsNoTracking().SingleOrDefaultAsync(row => row.Id == before.Id)) == (committed ? after : before)
                    && !await context.RealmBankItem.AnyAsync(row => row.Id == before.Id)
                    && !await context.CharacterMailAttachment.AnyAsync(row => row.ItemGuid == before.Id)
                    && !await context.MarketplaceAuction.AnyAsync(row => row.ItemId == before.Id && row.Id != target.Id)
                    && (committed || !await context.Item.AnyAsync(row => row.Id != before.Id && row.OwnerId == before.Owner
                        && row.Location == before.Location && row.BagIndex == before.Bag));
        }
    }
}
