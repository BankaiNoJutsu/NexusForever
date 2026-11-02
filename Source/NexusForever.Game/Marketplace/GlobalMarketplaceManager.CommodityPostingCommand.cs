using System.Data;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Character;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Marketplace
{
    public sealed partial class GlobalMarketplaceManager
    {
        internal sealed record CommodityPostingItem(BidItem Before, BidItem After, uint Consumed, uint Delivered)
        {
            internal ulong Id => (Before ?? After).Id;
            internal ulong Owner => (Before ?? After).Owner.Value;
        }
        internal sealed record CommodityPostingMaterial(ulong Owner, CommodityMaterialWrite Write);
        internal sealed record CommodityPostingMail(MailHeaderState Header, IReadOnlyList<ulong> Items);
        internal sealed record CommodityPostingView(BidOutcome Outcome, CommodityRow[] Orders,
            IReadOnlyDictionary<ulong, BidWalletCurrent> Wallets, IReadOnlyDictionary<ulong, CommodityReturnView> Inventories);

        // One immutable submit/fill/remainder graph. Original actors prepare and
        // reconcile these rows; this executor holds no Player or live Item.
        internal sealed class CommodityPostingCommand
        {
            private static readonly object ownershipGate = new();
            private static readonly ConditionalWeakTable<CharacterContext, object> ownedContexts = new();
            private readonly Func<CharacterContext> contexts;
            private readonly string database, id, payload;
            private readonly DateTime created;
            private readonly CommodityRow incoming;
            private readonly CommodityPostingOrder[] orders;
            private readonly CommodityPostingCredit[] credits;
            private readonly KeyValuePair<ulong, uint>[] deliveries;
            private readonly BidCharacter[] characters;
            private readonly BidWallet[] wallets;
            private readonly CommodityPostingItem[] items;
            private readonly CommodityPostingMaterial[] materials;
            private readonly CommodityPostingMail[] mails;
            private int running;
            internal string ReceiptId => id;
            internal string PayloadSHA256 => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

            internal CommodityPostingCommand(Func<CharacterContext> contexts, string database, string id, DateTime created,
                CommodityPostingQuote quote, IEnumerable<BidCharacter> characters, IEnumerable<BidWallet> wallets,
                IEnumerable<CommodityPostingItem> items, IEnumerable<CommodityPostingMaterial> materials,
                IEnumerable<CommodityPostingMail> mails, uint maximumStack)
            {
                this.contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
                this.database = database ?? throw new ArgumentNullException(nameof(database));
                this.id = id; this.created = new(created.Ticks / 10 * 10, created.Kind);
                incoming = quote.Incoming; orders = quote.Orders.ToArray(); credits = quote.Credits.ToArray();
                deliveries = quote.Deliveries.OrderBy(row => row.Key).ToArray();
                this.characters = characters.OrderBy(row => row.Id).ToArray();
                this.wallets = wallets.OrderBy(row => row.Character).ToArray();
                this.items = items.OrderBy(row => row.Id).ToArray();
                this.materials = materials.OrderBy(row => row.Owner).ToArray();
                this.mails = mails.Select(row => new CommodityPostingMail(row.Header, row.Items.ToArray())).OrderBy(row => row.Header.Id).ToArray();
                if (!Guid.TryParseExact(id, "N", out var identity) || identity == Guid.Empty || id != identity.ToString("N") || maximumStack == 0
                    || this.characters.Any(row => row.Id == 0 || row.Account == 0) || this.characters.Select(row => row.Id).Distinct().Count() != this.characters.Length
                    || !this.characters.Select(row => row.Id).SequenceEqual(orders.Select(row => (row.Before ?? row.After ?? incoming).Owner).Distinct().Order())
                    || !this.characters.Any(row => row.Id == incoming.Owner) || orders.Select(OrderId).Distinct().Count() != orders.Length
                    || this.items.Select(row => row.Id).Distinct().Count() != this.items.Length
                    || this.wallets.Select(row => row.Character).Distinct().Count() != this.wallets.Length
                    || !this.wallets.Any(row => row.Character == incoming.Owner)
                    || this.materials.Select(row => row.Owner).Distinct().Count() != this.materials.Length
                    || this.mails.Select(row => row.Header.Id).Distinct().Count() != this.mails.Length)
                    throw new InvalidOperationException("Commodity posting lost its unique complete graph.");
                foreach (var owner in this.characters)
                {
                    var wallet = this.wallets.SingleOrDefault(row => row.Character == owner.Id);
                    var paid = credits.Where(row => row.Character == owner.Id).ToArray();
                    if (wallet != null)
                    {
                        ulong after = owner.Id == incoming.Owner && incoming.Buy ? checked(wallet.Before - incoming.Price) : wallet.Before;
                        foreach (var credit in paid) after = Credit(after, credit.Amount, wallet.Cap);
                        if (wallet.Account != owner.Account || !wallet.Exists && wallet.Before != 0 || after != wallet.After
                            || wallet.Cap != 0 && (wallet.Before > wallet.Cap || wallet.After > wallet.Cap))
                            throw new InvalidOperationException("Commodity posting wallet changed its exact debit/fill/refund ledger.");
                    }
                    var cash = this.mails.Where(row => row.Header.RecipientId == owner.Id && row.Header.CurrencyAmount != 0).ToArray();
                    if (wallet != null ? cash.Length != 0 : !cash.Select(row => row.Header.CurrencyAmount).Order().SequenceEqual(paid.Select(row => row.Amount).Order()))
                        throw new InvalidOperationException("Commodity posting lost its exact offline credit mail.");
                    ulong delivered = this.items.Where(row => row.Owner == owner.Id).Aggregate(0ul, (sum, row) => checked(sum + row.Delivered));
                    var material = this.materials.SingleOrDefault(row => row.Owner == owner.Id)?.Write;
                    if (material != null)
                    {
                        if (wallet == null || material.Id >= 512 || !material.Exists && material.Before != 0
                            || material.After <= material.Before || material.After > material.Cap || material.Cap > ushort.MaxValue)
                            throw new InvalidOperationException("Commodity posting changed its captured material capacity.");
                        delivered += (uint)(material.After - material.Before);
                    }
                    if (delivered != quote.Deliveries.GetValueOrDefault(owner.Id))
                        throw new InvalidOperationException("Commodity posting changed a buyer or immediate-return quantity.");
                }
                if (this.wallets.Any(row => !this.characters.Any(owner => owner.Id == row.Character && owner.Account == row.Account))
                    || orders.Any(row => !this.characters.Any(owner => owner.Id == (row.Before ?? row.After ?? incoming).Owner))
                    || credits.Any(row => !this.characters.Any(owner => owner.Id == row.Character))
                    || this.materials.Any(row => !this.characters.Any(owner => owner.Id == row.Owner))
                    || this.items.Aggregate(0ul, (sum, row) => checked(sum + row.Consumed)) != quote.Consumption)
                    throw new InvalidOperationException("Commodity posting changed its captured participant roles.");
                foreach (var item in this.items)
                {
                    var source = item.Before ?? item.After;
                    if (source.Id == 0 || source.Definition != incoming.Definition || !source.Owner.HasValue
                        || !this.characters.Any(owner => owner.Id == source.Owner) || item.Before == null && item.Consumed != 0
                        || item.Consumed != 0 && (source.Owner != incoming.Owner || incoming.Buy)
                        || item.Before != null && (item.Before.Location != (ushort)InventoryLocation.Inventory || item.Consumed > item.Before.Quantity)
                        || item.After == null && (item.Delivered != 0 || item.Consumed != item.Before.Quantity)
                        || item.After != null && (item.After.Quantity == 0 || item.After.Quantity != checked((item.Before?.Quantity ?? 0) - item.Consumed + item.Delivered)
                            || item.Before != null && item.After != (item.Before with { Quantity = item.After.Quantity })
                            || item.After.Location is not ((ushort)InventoryLocation.Inventory) and not ((ushort)InventoryLocation.None)
                            || item.After.Location == (ushort)InventoryLocation.Inventory && item.After.Quantity > maximumStack))
                        throw new InvalidOperationException("Commodity posting changed a captured Item consumption/delivery.");
                    var links = this.mails.Where(row => row.Items.Contains(item.Id)).ToArray();
                    bool mailed = item.After?.Location == (ushort)InventoryLocation.None;
                    if (mailed ? item.Before != null || item.After.Bag != 0 || links.Length != 1 || links[0].Header.RecipientId != item.Owner : links.Length != 0)
                        throw new InvalidOperationException("Commodity posting changed an Item mail custody role.");
                }
                foreach (var mail in this.mails)
                    if (mail.Header.Id == 0 || !this.characters.Any(row => row.Id == mail.Header.RecipientId)
                        || mail.Header.CurrencyType != (byte)CurrencyType.Credits || mail.Header.HasPaidOrCollectedCurrency != 0 || mail.Header.IsCashOnDelivery != 0
                        || mail.Items.Count > 10 || mail.Items.Distinct().Count() != mail.Items.Count
                        || mail.Header.CurrencyAmount == 0 && mail.Items.Count == 0
                        || mail.Header.CurrencyAmount != 0 && mail.Items.Count != 0
                        || mail.Items.Any(key => !this.items.Any(row => row.Id == key && row.After?.Location == (ushort)InventoryLocation.None)))
                        throw new InvalidOperationException("Commodity posting lost a fixed owned mail descriptor.");
                payload = JsonSerializer.Serialize(new { Version = 1, Kind = "CommodityPosting", Database = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(database))),
                    Created = this.created, Incoming = incoming, Orders = orders, Credits = credits, Deliveries = deliveries,
                    Characters = this.characters, Wallets = this.wallets, Items = this.items, Materials = this.materials, Mails = this.mails });
            }

            private ulong OrderId(CommodityPostingOrder row) => (row.Before ?? row.After ?? incoming).Id;
            private static ulong Credit(ulong amount, ulong add, ulong cap) => cap == 0 ? checked(amount + add) : checked(amount + Math.Min(add, cap - amount));
            private static bool WritesWallet(BidWallet row) => row.Exists || row.Before != 0 || row.After != 0;
            private bool TimeValid => incoming.Expiration > (ulong)DateTime.UtcNow.ToFileTimeUtc()
                && orders.All(row => row.Before == null || row.Before.Expiration == 0 || row.Before.Expiration > (ulong)DateTime.UtcNow.ToFileTimeUtc());
            private CharacterContext NewContext()
            {
                var context = contexts() ?? throw new InvalidOperationException("Commodity posting context factory returned null.");
                lock (ownershipGate)
                {
                    if (ownedContexts.TryGetValue(context, out _) || context.AtomicCharacterId.HasValue || context.Database.CurrentTransaction != null
                        || context.ChangeTracker.Entries().Any() || context.IsSaveRejected || context.HasSavepointRollback)
                        throw new InvalidOperationException("Commodity posting requires a fresh exclusively owned context.");
                    ownedContexts.Add(context, new object());
                }
                try { CharacterDatabaseIdentity.Require(context, database); return context; }
                catch { context.Dispose(); throw; }
            }
            internal async Task<BidOutcome> ExecuteAsync()
            {
                if (Interlocked.Exchange(ref running, 1) != 0) throw new InvalidOperationException("Commodity posting already started.");
                try { await using var driver = NewContext(); return await driver.Database.CreateExecutionStrategy().ExecuteAsync(Attempt); }
                catch { return await ResolveAsync(); }
            }
            internal async Task<BidOutcome> ResolveAsync()
            {
                await using var driver = NewContext();
                return await driver.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var context = NewContext();
                    await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    await LockRows(context); return await ReadOutcome(context);
                });
            }
            internal async Task<CommodityPostingView> ReadReconciliationAsync()
            {
                await using var driver = NewContext();
                return await driver.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var context = NewContext();
                    await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    await LockRows(context);
                    if (!await OwnersMatch(context)) throw new InvalidOperationException("Commodity posting lost its original Character/account owners.");
                    var outcome = await ReadOutcome(context);
                    Dictionary<ulong, BidWalletCurrent> currentWallets = [];
                    Dictionary<ulong, CommodityReturnView> inventories = [];
                    foreach (var owner in characters)
                    {
                        var wallet = wallets.SingleOrDefault(row => row.Character == owner.Id);
                        var money = wallet == null ? null : await context.CharacterCurrency.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner.Id && row.CurrencyId == (byte)CurrencyType.Credits);
                        if (money != null && wallet.Cap != 0 && money.Amount > wallet.Cap) throw new InvalidOperationException("Commodity posting current wallet exceeds its captured cap.");
                        BidWalletCurrent current = wallet == null ? null : new(owner.Id, owner.Account, money != null, money?.Amount ?? 0);
                        if (current != null) currentWallets.Add(owner.Id, current);
                        List<BidItem> ownedItems = [];
                        foreach (var item in items.Where(row => row.Owner == owner.Id && row.After != null))
                            ownedItems.Add(BidItem.Capture(await context.Item.AsNoTracking().SingleOrDefaultAsync(row => row.Id == item.Id)));
                        var material = materials.SingleOrDefault(row => row.Owner == owner.Id)?.Write;
                        ushort? amount = material == null ? null : await context.CharacterTradeskillMaterial.AsNoTracking().Where(row => row.Id == owner.Id && row.MaterialId == material.Id).Select(row => (ushort?)row.Amount).SingleOrDefaultAsync();
                        var itemMails = mails.Where(row => row.Header.RecipientId == owner.Id).SelectMany(row => row.Items.Select(key => new KeyValuePair<ulong, MailHeaderState>(key, row.Header))).ToDictionary(row => row.Key, row => row.Value);
                        inventories.Add(owner.Id, new(outcome, null, current, ownedItems.ToArray(), amount, await InventoryMatches(context, owner.Id, outcome.Committed), itemMails));
                    }
                    List<CommodityRow> currentOrders = [];
                    foreach (var order in orders.OrderBy(OrderId))
                        currentOrders.Add(CommodityRow.Capture(await context.MarketplaceCommodityOrder.AsNoTracking().SingleOrDefaultAsync(row => row.Id == OrderId(order))));
                    return new CommodityPostingView(outcome, currentOrders.ToArray(), currentWallets, inventories);
                });
            }
            private async Task<BidOutcome> Attempt()
            {
                await using var context = NewContext();
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                await LockRows(context);
                if (await context.MarketplaceBidReceipt.AnyAsync(row => row.Id == id)) return await ReadOutcome(context);
                if (!TimeValid || !await Matches(context, false)) throw new InvalidOperationException("Commodity posting lost its complete stored preimage.");
                foreach (var order in orders)
                {
                    if (order.Before == null) { if (order.After != null) context.MarketplaceCommodityOrder.Add(order.After.Model()); }
                    else
                    {
                        var row = await context.MarketplaceCommodityOrder.SingleAsync(row => row.Id == order.Before.Id);
                        if (order.After == null) context.MarketplaceCommodityOrder.Remove(row);
                        else context.Entry(row).CurrentValues.SetValues(order.After.Model());
                        ObserveListingWrite(context, row, order.After == null, CommodityRow.Capture,
                            () => CommodityRow.Capture(context.MarketplaceCommodityOrder.AsNoTracking().SingleOrDefault(value => value.Id == order.Before.Id)));
                    }
                }
                foreach (var wallet in wallets.Where(WritesWallet))
                {
                    var row = await context.CharacterCurrency.SingleOrDefaultAsync(row => row.Id == wallet.Character && row.CurrencyId == (byte)CurrencyType.Credits);
                    if (row == null) context.CharacterCurrency.Add(new() { Id = wallet.Character, CurrencyId = (byte)CurrencyType.Credits, Amount = wallet.After });
                    else row.Amount = wallet.After;
                }
                foreach (var item in items)
                {
                    if (item.Before == null) context.Item.Add(item.After.Model());
                    else
                    {
                        var row = await context.Item.SingleAsync(row => row.Id == item.Id);
                        if (item.After == null) context.Item.Remove(row); else row.StackCount = item.After.Quantity;
                    }
                }
                foreach (var material in materials)
                {
                    var row = await context.CharacterTradeskillMaterial.SingleOrDefaultAsync(row => row.Id == material.Owner && row.MaterialId == material.Write.Id);
                    if (row == null) context.CharacterTradeskillMaterial.Add(new() { Id = material.Owner, MaterialId = material.Write.Id, Amount = material.Write.After });
                    else row.Amount = material.Write.After;
                }
                foreach (var mail in mails)
                {
                    context.CharacterMail.Add(mail.Header.Model());
                    for (byte index = 0; index < mail.Items.Count; index++) context.CharacterMailAttachment.Add(new() { Id = mail.Header.Id, Index = index, ItemGuid = mail.Items[index] });
                }
                context.MarketplaceBidReceipt.Add(new() { Id = id, Payload = payload, CreatedUtc = created });
                context.ChangeTracker.DetectChanges();
                var enlisted = context.ChangeTracker.Entries().Select(entry => (Entry: entry, Values: entry.CurrentValues.Clone())).ToArray();
                context.OnSaveValidation(phase =>
                {
                    if (!ReferenceEquals(context.Database.CurrentTransaction, transaction) || context.HasSavepointRollback || !TimeValid
                        || context.ChangeTracker.Entries().Any(entry => !enlisted.Any(row => ReferenceEquals(row.Entry.Entity, entry.Entity)))
                        || enlisted.Any(row => !row.Values.Properties.All(property => Equals(row.Entry.CurrentValues[property.Name], row.Values[property.Name]))))
                        throw new InvalidOperationException("Commodity posting changed its sealed graph.");
                    if (phase != CharacterSaveValidationPhase.BeforeFlush && (!Matches(context, true).GetAwaiter().GetResult()
                        || context.MarketplaceBidReceipt.AsNoTracking().SingleOrDefault(row => row.Id == id)?.Payload != payload))
                        throw new InvalidOperationException("Commodity posting SQL differs from its complete postimage.");
                });
                await context.SaveChangesAsync(); await transaction.CommitAsync(); return new(true, true);
            }
            private async Task LockRows(CharacterContext context)
            {
                bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported commodity posting provider.");
                foreach (var owner in characters)
                    _ = await (mysql ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={owner.Id} FOR UPDATE") : context.Character.Where(row => row.Id == owner.Id)).AsNoTracking().SingleOrDefaultAsync();
                foreach (var order in orders.OrderBy(OrderId))
                    _ = await (mysql ? context.MarketplaceCommodityOrder.FromSqlInterpolated($"SELECT * FROM `marketplace_commodity_order` WHERE id={OrderId(order)} FOR UPDATE") : context.MarketplaceCommodityOrder.Where(row => row.Id == OrderId(order))).AsNoTracking().SingleOrDefaultAsync();
                foreach (var item in items)
                    _ = await (mysql ? context.Item.FromSqlInterpolated($"SELECT * FROM `item` WHERE id={item.Id} FOR UPDATE") : context.Item.Where(row => row.Id == item.Id)).AsNoTracking().SingleOrDefaultAsync();
                foreach (var material in materials)
                    _ = await (mysql ? context.CharacterTradeskillMaterial.FromSqlInterpolated($"SELECT * FROM `character_tradeskill_materials` WHERE id={material.Owner} AND materialId={material.Write.Id} FOR UPDATE")
                        : context.CharacterTradeskillMaterial.Where(row => row.Id == material.Owner && row.MaterialId == material.Write.Id)).AsNoTracking().SingleOrDefaultAsync();
            }
            private async Task<BidOutcome> ReadOutcome(CharacterContext context)
            {
                var receipt = await context.MarketplaceBidReceipt.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id);
                if (receipt != null && receipt.Payload != payload) throw new InvalidOperationException("Commodity posting receipt belongs to another graph.");
                return new(receipt != null, await Matches(context, receipt != null));
            }
            private async Task<bool> OwnersMatch(CharacterContext context)
            {
                foreach (var owner in characters)
                {
                    var row = await context.Character.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner.Id);
                    if (row == null || row.AccountId != owner.Account || row.DeleteTime != null) return false;
                }
                return true;
            }
            private async Task<bool> Matches(CharacterContext context, bool committed)
            {
                if (!await OwnersMatch(context)) return false;
                foreach (var order in orders)
                    if (CommodityRow.Capture(await context.MarketplaceCommodityOrder.AsNoTracking().SingleOrDefaultAsync(row => row.Id == OrderId(order))) != (committed ? order.After : order.Before)) return false;
                foreach (var wallet in wallets)
                {
                    var row = await context.CharacterCurrency.AsNoTracking().SingleOrDefaultAsync(row => row.Id == wallet.Character && row.CurrencyId == (byte)CurrencyType.Credits);
                    bool exists = committed ? WritesWallet(wallet) : wallet.Exists;
                    if (row == null ? exists : !exists || row.Amount != (committed ? wallet.After : wallet.Before)) return false;
                }
                foreach (var owner in characters) if (!await InventoryMatches(context, owner.Id, committed)) return false;
                return true;
            }
            private async Task<bool> InventoryMatches(CharacterContext context, ulong owner, bool committed)
            {
                foreach (var item in items.Where(row => row.Owner == owner))
                {
                    var target = committed ? item.After : item.Before;
                    var link = mails.SingleOrDefault(mail => mail.Items.Contains(item.Id));
                    byte index = link == null ? (byte)0 : checked((byte)Array.IndexOf(link.Items.ToArray(), item.Id));
                    if (BidItem.Capture(await context.Item.AsNoTracking().SingleOrDefaultAsync(row => row.Id == item.Id)) != target
                        || await context.RealmBankItem.AnyAsync(row => row.Id == item.Id) || await context.MarketplaceAuction.AnyAsync(row => row.ItemId == item.Id)
                        || await context.CharacterMailAttachment.AnyAsync(row => row.ItemGuid == item.Id && (!committed || link == null || row.Id != link.Header.Id || row.Index != index))) return false;
                    if (target?.Location == (ushort)InventoryLocation.Inventory && await context.Item.AnyAsync(row => row.Id != item.Id && row.OwnerId == owner && row.Location == target.Location && row.BagIndex == target.Bag)) return false;
                }
                foreach (var material in materials.Where(row => row.Owner == owner))
                {
                    var row = await context.CharacterTradeskillMaterial.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner && row.MaterialId == material.Write.Id);
                    if (row == null ? committed || material.Write.Exists : !committed && !material.Write.Exists || row.Amount != (committed ? material.Write.After : material.Write.Before)) return false;
                }
                foreach (var mail in mails.Where(row => row.Header.RecipientId == owner))
                {
                    if (MailHeaderState.From(await context.CharacterMail.AsNoTracking().SingleOrDefaultAsync(row => row.Id == mail.Header.Id)) != (committed ? mail.Header : null)) return false;
                    var links = await context.CharacterMailAttachment.AsNoTracking().Where(row => row.Id == mail.Header.Id).OrderBy(row => row.Index).ToArrayAsync();
                    if (committed ? links.Length != mail.Items.Count || links.Where((row, index) => row.Index != index || row.ItemGuid != mail.Items[index]).Any() : links.Length != 0) return false;
                }
                return true;
            }
        }
    }
}
