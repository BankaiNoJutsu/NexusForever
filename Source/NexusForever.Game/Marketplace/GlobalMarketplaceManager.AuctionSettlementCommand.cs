using System.Data;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        internal sealed record BidCharacter(ulong Id, uint Account);
        internal sealed record BidWallet(ulong Character, uint Account, bool Exists, ulong Before, ulong After, ulong Cap);
        internal sealed record BidItem(ulong Id, ulong? Owner, uint Definition, ushort Location, uint Bag, uint Quantity,
            uint Charges, float Durability, uint Expiration, bool Bound, string Microchips, string Runes)
        {
            internal static BidItem Capture(ItemModel row) => row == null ? null : new(row.Id, row.OwnerId, row.ItemId,
                row.Location, row.BagIndex, row.StackCount, row.Charges, row.Durability, row.ExpirationTimeLeft,
                row.Soulbound, row.MicrochipIds, row.RuneSlots);
            internal ItemModel Model() => new() { Id = Id, OwnerId = Owner, ItemId = Definition, Location = Location,
                BagIndex = Bag, StackCount = Quantity, Charges = Charges, Durability = Durability,
                ExpirationTimeLeft = Expiration, Soulbound = Bound, MicrochipIds = Microchips, RuneSlots = Runes };
        }
        internal sealed record BidOutcome(bool Committed, bool CurrentGraphMatches);
        internal sealed record BidWalletCurrent(ulong Character, uint Account, bool Exists, ulong Amount);
        internal sealed record BidReconciliationView(BidOutcome Outcome, IReadOnlyDictionary<ulong, BidWalletCurrent> Wallets, AuctionRow Auction, BidItem Item,
            bool DeliveryMatches = true);
        internal enum AuctionSettlementKind { Bid, Buyout, WinningExpiry, UnsoldReturn, CancelledReturn }
        internal sealed record AuctionDeliverySettlement(ulong SellerProceeds, BidItem DeliveredItem,
            MailHeaderState SellerMail = null, MailHeaderState DeliveryMail = null,
            AuctionSettlementKind Kind = AuctionSettlementKind.Buyout);

        // Only immutable scalar graphs cross to this executor. Actor reservations,
        // Item/Mail owners and packet callbacks remain on their original queues.
        internal sealed class AuctionSettlementCommand
        {
            private static readonly object ownershipGate = new();
            private static readonly ConditionalWeakTable<CharacterContext, object> contextsOwned = new();
            private readonly Func<CharacterContext> contexts;
            private readonly string database, id, payload;
            private readonly DateTime created;
            private readonly AuctionRow before, target;
            private readonly BidItem item;
            private readonly BidCharacter[] characters;
            private readonly BidWallet[] wallets;
            private readonly MailHeaderState refund;
            private readonly AuctionDeliverySettlement sale;
            private readonly MailHeaderState[] mails;
            private readonly ulong receiver;
            private readonly bool expiry, cancelling, debitsReceiver;
            private int running;
            internal string ReceiptId => id;
            internal string PayloadSHA256 => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

            internal AuctionSettlementCommand(Func<CharacterContext> contexts, string database, string id, DateTime created,
                AuctionRow before, ulong bidder, ulong amount, BidItem item, IEnumerable<BidCharacter> characters,
                IEnumerable<BidWallet> wallets, MailHeaderState refund = null, AuctionDeliverySettlement sale = null)
            {
                this.contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
                this.database = database ?? throw new ArgumentNullException(nameof(database));
                if (!Guid.TryParseExact(id, "N", out var identity) || identity == Guid.Empty || id != identity.ToString("N"))
                    throw new InvalidOperationException("Normal bid requires its retained command identity.");
                this.id = id; this.created = new DateTime(created.Ticks / 10 * 10, created.Kind); this.before = before; this.item = item; this.refund = refund;
                this.characters = characters.OrderBy(row => row.Id).ToArray();
                this.wallets = wallets.OrderBy(row => row.Character).ToArray();
                this.sale = sale;
                receiver = bidder;
                var kind = sale?.Kind ?? AuctionSettlementKind.Bid;
                expiry = kind is AuctionSettlementKind.WinningExpiry or AuctionSettlementKind.UnsoldReturn;
                cancelling = kind == AuctionSettlementKind.CancelledReturn;
                debitsReceiver = !expiry && !cancelling;
                bool returning = kind is AuctionSettlementKind.UnsoldReturn or AuctionSettlementKind.CancelledReturn;
                mails = new[] { refund, sale?.SellerMail, sale?.DeliveryMail }.Where(row => row != null).ToArray();
                if (before == null || item == null || bidder == 0 || before.Id == 0 || !Enum.IsDefined(kind)
                    || sale != null && kind == AuctionSettlementKind.Bid || !ValidRoles(before, bidder, amount, kind)
                    || item.Id != before.Item || item.Owner != null || item.Location != (ushort)InventoryLocation.None
                    || item.Definition != before.Definition || item.Quantity != before.Quantity
                    || this.characters.Length != this.characters.Select(row => row.Id).Distinct().Count()
                    || this.characters.Any(row => row.Id == 0 || row.Account == 0)
                    || this.wallets.Length != this.wallets.Select(row => row.Character).Distinct().Count()
                    || this.wallets.Any(row => !this.characters.Contains(new(row.Character, row.Account))
                        || !row.Exists && row.Before != 0 || row.Cap != 0 && (row.Before > row.Cap || row.After > row.Cap)))
                    throw new InvalidOperationException("Normal bid lost its complete listing/Item/wallet owners.");
                target = sale == null ? before with { Bid = amount, Bidder = bidder } : null;
                ulong debit = !debitsReceiver ? 0 : before.Bidder == bidder ? amount - before.Bid : amount;
                var buyer = this.wallets.SingleOrDefault(row => row.Character == bidder);
                bool outbid = !expiry && before.Bidder != 0 && before.Bidder != bidder && before.Bid != 0;
                var prior = this.wallets.SingleOrDefault(row => row.Character == before.Bidder);
                if (debitsReceiver && (buyer == null || !buyer.Exists) || buyer != null && (buyer.Before < debit || buyer.After != buyer.Before - debit)
                    || !this.characters.Any(row => row.Id == before.Owner) || !this.characters.Any(row => row.Id == bidder)
                    || this.wallets.Length != (buyer != null ? 1 : 0) + (outbid && refund == null ? 1 : 0)
                        + (sale is { SellerProceeds: > 0, SellerMail: null } ? 1 : 0)
                    || outbid && (refund == null
                        ? prior == null || prior.After != Credit(prior.Before, before.Bid, prior.Cap)
                        : refund.Id == 0 || refund.RecipientId != before.Bidder || refund.CurrencyType != (byte)CurrencyType.Credits
                            || refund.CurrencyAmount != before.Bid || refund.HasPaidOrCollectedCurrency != 0
                            || refund.IsCashOnDelivery != 0 || !this.characters.Any(row => row.Id == before.Bidder))
                    || !outbid && refund != null)
                    throw new InvalidOperationException("Normal bid changed its debit or prior-bidder refund.");
                if (sale != null)
                {
                    var seller = returning ? null : this.wallets.SingleOrDefault(row => row.Character == before.Owner);
                    bool mailed = sale.DeliveryMail != null;
                    if (sale.SellerProceeds > amount || returning && sale.SellerProceeds != 0 || !mailed && buyer == null || sale.DeliveredItem == null
                        || sale.DeliveredItem != (item with { Owner = bidder,
                            Location = (ushort)(mailed ? InventoryLocation.None : InventoryLocation.Inventory),
                            Bag = mailed ? 0u : sale.DeliveredItem.Bag })
                        || (sale.SellerProceeds == 0 ? sale.SellerMail != null || seller != null
                            : sale.SellerMail == null ? seller == null || seller.After != Credit(seller.Before, sale.SellerProceeds, seller.Cap)
                            : !CreditMail(sale.SellerMail, before.Owner, sale.SellerProceeds))
                        || mailed && !CreditMail(sale.DeliveryMail, bidder, 0)
                        || mails.Length != mails.Select(row => row.Id).Distinct().Count())
                        throw new InvalidOperationException("Buyout lost its captured proceeds or whole-stack delivery.");
                }
                // Never persist a configured connection string in the receipt.
                payload = JsonSerializer.Serialize(new { Version = 1, Database = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(database))),
                    Created = this.created, Before = before, Target = target, Item = item, Characters = this.characters, Wallets = this.wallets, Refund = refund, Sale = sale });
            }

            private static bool ValidRoles(AuctionRow row, ulong receiver, ulong amount, AuctionSettlementKind kind) => kind switch
            {
                AuctionSettlementKind.Bid => receiver != row.Owner && amount >= row.MinimumBid && amount > row.Bid
                    && (row.Buyout == 0 || amount < row.Buyout),
                AuctionSettlementKind.Buyout => receiver != row.Owner && row.Buyout != 0 && amount == row.Buyout
                    && amount >= row.MinimumBid && amount > row.Bid,
                AuctionSettlementKind.WinningExpiry => row.Bidder == receiver && receiver != row.Owner && row.Bid != 0 && amount == row.Bid,
                AuctionSettlementKind.CancelledReturn => receiver == row.Owner && amount == 0
                    && (row.Bidder == 0 && row.Bid == 0 || row.Bidder != 0 && row.Bidder != row.Owner && row.Bid != 0),
                AuctionSettlementKind.UnsoldReturn => receiver == row.Owner && row.Bidder == 0 && row.Bid == 0 && amount == 0,
                _ => false
            };

            private bool TimeValid => cancelling || (expiry ? before.Expiration <= (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                : before.Expiration > (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            private bool WritesWallet(BidWallet wallet) => !(!debitsReceiver && wallet.Character == receiver && !wallet.Exists && wallet.Before == 0 && wallet.After == 0);

            private static bool CreditMail(MailHeaderState mail, ulong recipient, ulong amount) => mail.Id != 0
                && mail.RecipientId == recipient && mail.CurrencyType == (byte)CurrencyType.Credits && mail.CurrencyAmount == amount
                && mail.HasPaidOrCollectedCurrency == 0 && mail.IsCashOnDelivery == 0;

            private static ulong Credit(ulong value, ulong add, ulong cap) => cap == 0 ? checked(value + add) : value + Math.Min(add, cap - value);

            private CharacterContext NewContext()
            {
                var context = contexts() ?? throw new InvalidOperationException("Normal bid context factory returned null.");
                lock (ownershipGate)
                {
                    if (contextsOwned.TryGetValue(context, out _) || context.AtomicCharacterId.HasValue || context.Database.CurrentTransaction != null
                        || context.ChangeTracker.Entries().Any() || context.IsSaveRejected || context.HasSavepointRollback)
                        throw new InvalidOperationException("Normal bid requires fresh exclusively owned contexts.");
                    contextsOwned.Add(context, new object());
                }
                try { CharacterDatabaseIdentity.Require(context, database); return context; }
                catch { context.Dispose(); throw; }
            }

            internal async Task<BidOutcome> ExecuteAsync()
            {
                if (Interlocked.Exchange(ref running, 1) != 0) throw new InvalidOperationException("Normal bid execution already started.");
                try
                {
                    await using var driver = NewContext();
                    return await driver.Database.CreateExecutionStrategy().ExecuteAsync(Attempt);
                }
                catch
                {
                    // Acquire the same row locks after the original context has
                    // ended. Absence of a receipt is then a terminal noncommit;
                    // inability to probe propagates and keeps the command owned.
                    return await ResolveAsync();
                }
            }

            internal async Task<BidOutcome> ResolveAsync()
            {
                await using var driver = NewContext();
                return await driver.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var context = NewContext();
                    await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    await LockCharacters(context);
                    return await ReadOutcome(context);
                });
            }

            internal async Task<BidReconciliationView> ReadReconciliationAsync()
            {
                await using var driver = NewContext();
                return await driver.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var context = NewContext();
                    await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    await LockCharacters(context);
                    var result = await ReadOutcome(context);
                    Dictionary<ulong, BidWalletCurrent> current = [];
                    foreach (var wallet in wallets)
                    {
                        var owner = await context.Character.AsNoTracking().SingleOrDefaultAsync(row => row.Id == wallet.Character);
                        if (owner == null || owner.DeleteTime != null || owner.AccountId != wallet.Account)
                            throw new InvalidOperationException("Bid reconciliation lost an original wallet's Character/account owner.");
                        var row = await context.CharacterCurrency.AsNoTracking().SingleOrDefaultAsync(row => row.Id == wallet.Character && row.CurrencyId == (byte)CurrencyType.Credits);
                        if (row != null && wallet.Cap != 0 && row.Amount > wallet.Cap)
                            throw new InvalidOperationException("Bid reconciliation cannot adopt a balance above its captured cap.");
                        current.Add(wallet.Character, new(wallet.Character, wallet.Account, row != null, row?.Amount ?? 0));
                    }
                    var auction = await context.MarketplaceAuction.AsNoTracking().SingleOrDefaultAsync(row => row.Id == before.Id);
                    var storedItem = await context.Item.AsNoTracking().SingleOrDefaultAsync(row => row.Id == item.Id);
                    return new BidReconciliationView(result, current, AuctionRow.Capture(auction), BidItem.Capture(storedItem),
                        await MatchesDelivery(context));
                });
            }

            private async Task<BidOutcome> Attempt()
            {
                await using var context = NewContext();
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                await LockCharacters(context);
                var receipt = await context.MarketplaceBidReceipt.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id);
                if (receipt != null) return await ReadOutcome(context);
                if (!await Matches(context, false) || !TimeValid)
                    throw new InvalidOperationException("Normal bid lost its captured complete preimage or expiration.");
                var auction = await context.MarketplaceAuction.SingleAsync(row => row.Id == before.Id);
                if (sale == null) { auction.CurrentBid = target.Bid; auction.TopBidderCharacterId = target.Bidder; }
                else
                {
                    context.MarketplaceAuction.Remove(auction);
                    var delivered = await context.Item.SingleAsync(row => row.Id == item.Id);
                    delivered.OwnerId = sale.DeliveredItem.Owner; delivered.Location = sale.DeliveredItem.Location;
                    delivered.BagIndex = sale.DeliveredItem.Bag;
                }
                ObserveListingWrite(context, auction, sale != null, AuctionRow.Capture,
                    () => AuctionRow.Capture(context.MarketplaceAuction.AsNoTracking().SingleOrDefault(row => row.Id == before.Id)));
                foreach (var wallet in wallets)
                {
                    if (!WritesWallet(wallet)) continue;
                    var row = await context.CharacterCurrency.SingleOrDefaultAsync(row => row.Id == wallet.Character && row.CurrencyId == (byte)CurrencyType.Credits);
                    if (row == null) context.CharacterCurrency.Add(new() { Id = wallet.Character, CurrencyId = (byte)CurrencyType.Credits, Amount = wallet.After });
                    else row.Amount = wallet.After;
                }
                foreach (var mail in mails) context.CharacterMail.Add(mail.Model());
                if (sale?.DeliveryMail != null) context.CharacterMailAttachment.Add(new()
                    { Id = sale.DeliveryMail.Id, Index = 0, ItemGuid = item.Id });
                context.MarketplaceBidReceipt.Add(new() { Id = id, Payload = payload, CreatedUtc = created });
                context.ChangeTracker.DetectChanges();
                var enlisted = context.ChangeTracker.Entries().Select(entry => (Entry: entry, Values: entry.CurrentValues.Clone())).ToArray();
                context.OnSaveValidation(phase =>
                {
                    if (!ReferenceEquals(context.Database.CurrentTransaction, transaction) || context.HasSavepointRollback
                        || !TimeValid
                        || context.ChangeTracker.Entries().Any(entry => !enlisted.Any(row => ReferenceEquals(row.Entry.Entity, entry.Entity)))
                        || enlisted.Any(row => !row.Values.Properties.All(property => Equals(row.Entry.CurrentValues[property.Name], row.Values[property.Name]))))
                        throw new InvalidOperationException("Normal bid changed its sealed transaction graph.");
                    if (phase != CharacterSaveValidationPhase.BeforeFlush
                        && (!Matches(context, true).GetAwaiter().GetResult()
                            || context.MarketplaceBidReceipt.AsNoTracking().SingleOrDefault(row => row.Id == id)?.Payload != payload))
                        throw new InvalidOperationException("Normal bid SQL differs from its complete postimage.");
                });
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new(true, true);
            }

            private async Task LockCharacters(CharacterContext context)
            {
                bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported normal bid provider.");
                foreach (var owner in characters)
                {
                    // Keep locks deterministic across all affected Characters,
                    // including offline recipients and the listing's seller.
                    _ = await (mysql ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={owner.Id} FOR UPDATE")
                        : context.Character.Where(row => row.Id == owner.Id)).AsNoTracking().SingleOrDefaultAsync();
                }
                _ = await (mysql ? context.MarketplaceAuction.FromSqlInterpolated($"SELECT * FROM `marketplace_auction` WHERE id={before.Id} FOR UPDATE")
                    : context.MarketplaceAuction.Where(row => row.Id == before.Id)).AsNoTracking().SingleOrDefaultAsync();
                _ = await (mysql ? context.Item.FromSqlInterpolated($"SELECT * FROM `item` WHERE id={item.Id} FOR UPDATE")
                    : context.Item.Where(row => row.Id == item.Id)).AsNoTracking().SingleOrDefaultAsync();
            }

            private async Task<BidOutcome> ReadOutcome(CharacterContext context)
            {
                var receipt = await context.MarketplaceBidReceipt.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id);
                if (receipt != null && !string.Equals(receipt.Payload, payload, StringComparison.Ordinal))
                    throw new InvalidOperationException("Normal bid receipt belongs to another captured command.");
                return new(receipt != null, await Matches(context, receipt != null));
            }

            private async Task<bool> MatchesDelivery(CharacterContext context)
            {
                if (sale == null) return true;
                if (BidItem.Capture(await context.Item.AsNoTracking().SingleOrDefaultAsync(row => row.Id == item.Id)) != sale.DeliveredItem
                    || await context.RealmBankItem.AnyAsync(row => row.Id == item.Id)
                    || await context.MarketplaceAuction.AnyAsync(row => row.ItemId == item.Id)) return false;
                var attachments = await context.CharacterMailAttachment.AsNoTracking().Where(row => row.ItemGuid == item.Id).ToArrayAsync();
                if (sale.DeliveryMail == null) return attachments.Length == 0 && !await context.Item.AnyAsync(row => row.Id != item.Id
                    && row.OwnerId == sale.DeliveredItem.Owner && row.Location == sale.DeliveredItem.Location && row.BagIndex == sale.DeliveredItem.Bag);
                return MailHeaderState.From(await context.CharacterMail.AsNoTracking().SingleOrDefaultAsync(row => row.Id == sale.DeliveryMail.Id)) == sale.DeliveryMail
                    && attachments.Length == 1 && attachments[0].Id == sale.DeliveryMail.Id && attachments[0].Index == 0
                    && await context.CharacterMailAttachment.CountAsync(row => row.Id == sale.DeliveryMail.Id) == 1;
            }

            private async Task<bool> Matches(CharacterContext context, bool committed)
            {
                foreach (var owner in characters)
                {
                    var row = await context.Character.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner.Id);
                    if (row == null || row.DeleteTime != null || row.AccountId != owner.Account) return false;
                }
                if (AuctionRow.Capture(await context.MarketplaceAuction.AsNoTracking().SingleOrDefaultAsync(row => row.Id == before.Id)) != (committed ? target : before)
                    || BidItem.Capture(await context.Item.AsNoTracking().SingleOrDefaultAsync(row => row.Id == item.Id)) != (committed && sale != null ? sale.DeliveredItem : item)
                    || await context.RealmBankItem.AnyAsync(row => row.Id == item.Id)
                    || await context.CharacterMailAttachment.AnyAsync(row => row.ItemGuid == item.Id
                        && (!committed || sale == null || sale.DeliveryMail == null || row.Id != sale.DeliveryMail.Id || row.Index != 0))
                    || await context.MarketplaceAuction.AnyAsync(row => row.ItemId == item.Id && row.Id != before.Id)) return false;
                if (sale != null && sale.DeliveryMail == null && await context.Item.AnyAsync(row => row.Id != item.Id
                    && row.OwnerId == sale.DeliveredItem.Owner && row.Location == sale.DeliveredItem.Location
                    && row.BagIndex == sale.DeliveredItem.Bag)) return false;
                foreach (var wallet in wallets)
                {
                    var row = await context.CharacterCurrency.AsNoTracking().SingleOrDefaultAsync(row => row.Id == wallet.Character && row.CurrencyId == (byte)CurrencyType.Credits);
                    bool exists = committed ? wallet.Exists || WritesWallet(wallet) : wallet.Exists;
                    if (row == null ? exists : !exists || row.Amount != (committed ? wallet.After : wallet.Before)) return false;
                }
                foreach (var expectedMail in mails)
                {
                    var mail = await context.CharacterMail.AsNoTracking().SingleOrDefaultAsync(row => row.Id == expectedMail.Id);
                    if (MailHeaderState.From(mail) != (committed ? expectedMail : null)) return false;
                    var attachments = await context.CharacterMailAttachment.AsNoTracking().Where(row => row.Id == expectedMail.Id).ToArrayAsync();
                    if (committed && sale?.DeliveryMail?.Id == expectedMail.Id)
                    {
                        if (attachments.Length != 1 || attachments[0].Index != 0 || attachments[0].ItemGuid != item.Id) return false;
                    }
                    else if (attachments.Length != 0) return false;
                }
                return true;
            }
        }
    }
}
