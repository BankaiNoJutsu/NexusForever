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
        internal sealed record CommodityItemWrite(BidItem Before, BidItem After);
        internal sealed record CommodityMaterialWrite(ushort Id, bool Exists, ushort Before, ushort After, uint Cap);
        internal sealed record CommodityReturnView(BidOutcome Outcome, CommodityRow Order, BidWalletCurrent Wallet,
            BidItem[] Items, ushort? Material, bool DeliveryMatches, IReadOnlyDictionary<ulong, MailHeaderState> ItemMails = null);

        // The executor owns only immutable scalar preimages/postimages. The
        // original actor reserves and reconciles inventory/satchel delivery.
        internal sealed class CommodityReturnCommand
        {
            private static readonly object ownershipGate = new();
            private static readonly ConditionalWeakTable<CharacterContext, object> contextsOwned = new();
            private readonly Func<CharacterContext> contexts;
            private readonly string database, id, payload;
            private readonly DateTime created;
            private readonly CommodityRow before;
            private readonly BidCharacter owner;
            private readonly BidWallet wallet;
            private readonly CommodityItemWrite[] items;
            private readonly CommodityMaterialWrite material;
            private readonly MailHeaderState mail;
            private readonly bool expiry;
            private int running;
            internal string ReceiptId => id;
            internal string PayloadSHA256 => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

            internal CommodityReturnCommand(Func<CharacterContext> contexts, string database, string id, DateTime created,
                CommodityRow before, BidCharacter owner, bool expiry, BidWallet wallet,
                IEnumerable<CommodityItemWrite> items = null, CommodityMaterialWrite material = null, MailHeaderState mail = null)
            {
                this.contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
                this.database = database ?? throw new ArgumentNullException(nameof(database));
                this.id = id; this.created = new(created.Ticks / 10 * 10, created.Kind);
                this.before = before; this.owner = owner; this.wallet = wallet; this.expiry = expiry;
                this.items = (items ?? []).OrderBy(row => row.After.Id).ToArray();
                this.material = material; this.mail = mail;
                if (!Guid.TryParseExact(id, "N", out var identity) || identity == Guid.Empty || id != identity.ToString("N")
                    || before == null || before.Id == 0 || before.Owner == 0 || before.Definition == 0 || before.Quantity == 0
                    || owner == null || owner.Id != before.Owner || owner.Account == 0 || before.Immediate
                    || wallet != null && (wallet.Character != owner.Id || wallet.Account != owner.Account
                        || !wallet.Exists && wallet.Before != 0 || wallet.Cap != 0 && (wallet.Before > wallet.Cap || wallet.After > wallet.Cap)))
                    throw new InvalidOperationException("Commodity return lost its stored order or original owner.");
                if (before.Buy)
                {
                    if (this.items.Length != 0 || material != null || (wallet == null
                        ? before.Price != 0 && !CreditMail(mail, before.Price) || before.Price == 0 && mail != null
                        : mail != null || wallet.After != Credit(wallet.Before, before.Price, wallet.Cap)))
                        throw new InvalidOperationException("Commodity buy return changed its captured full escrow refund.");
                }
                else
                {
                    if (wallet != null && wallet.Before != wallet.After || this.items.Length == 0 && material == null
                        || this.items.Select(row => row.After.Id).Distinct().Count() != this.items.Length
                        || this.items.Any(row => row.After.Id == 0 || row.After.Owner != owner.Id || row.After.Definition != before.Definition
                            || row.After.Quantity == 0 || row.Before != null && (row.Before.Owner != owner.Id
                                || row.After.Quantity <= row.Before.Quantity || row.After != (row.Before with { Quantity = row.After.Quantity }))))
                        throw new InvalidOperationException("Commodity sell return changed its captured fungible Item creation.");
                    ulong quantity = this.items.Aggregate(0ul, (sum, row) => checked(sum + row.After.Quantity - (row.Before?.Quantity ?? 0)));
                    if (material != null)
                    {
                        if (material.Id >= 512 || !material.Exists && material.Before != 0 || material.After <= material.Before
                            || material.After > material.Cap || material.Cap > ushort.MaxValue)
                            throw new InvalidOperationException("Commodity return changed its captured satchel quantity.");
                        quantity += (uint)(material.After - material.Before);
                    }
                    bool mailed = mail != null;
                    if (quantity != before.Quantity || mailed && (!CreditMail(mail, 0) || material != null
                            || this.items.Length != 1 || this.items[0].Before != null)
                        || !mailed && wallet == null
                        || this.items.Any(row => row.After.Location != (ushort)(mailed ? InventoryLocation.None : InventoryLocation.Inventory)
                            || mailed && row.After.Bag != 0))
                        throw new InvalidOperationException("Commodity return lost complete direct/mail/satchel delivery.");
                }
                payload = JsonSerializer.Serialize(new { Version = 1, Kind = "CommodityReturn", Database = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(database))),
                    Created = this.created, Before = before, Owner = owner, Expiry = expiry, Wallet = wallet, Items = this.items, Material = material, Mail = mail });
            }

            private bool CreditMail(MailHeaderState row, ulong amount) => row != null && row.Id != 0 && row.RecipientId == owner.Id
                && row.CurrencyType == (byte)CurrencyType.Credits && row.CurrencyAmount == amount
                && row.HasPaidOrCollectedCurrency == 0 && row.IsCashOnDelivery == 0;
            private static ulong Credit(ulong value, ulong add, ulong cap) => cap == 0 ? checked(value + add) : value + Math.Min(add, cap - value);
            private bool TimeValid => !expiry || before.Expiration != 0 && before.Expiration <= (ulong)DateTime.UtcNow.ToFileTimeUtc();
            private bool WritesWallet => wallet != null && (wallet.Exists || wallet.Before != 0 || wallet.After != 0);

            private CharacterContext NewContext()
            {
                var context = contexts() ?? throw new InvalidOperationException("Commodity return context factory returned null.");
                lock (ownershipGate)
                {
                    if (contextsOwned.TryGetValue(context, out _) || context.AtomicCharacterId.HasValue || context.Database.CurrentTransaction != null
                        || context.ChangeTracker.Entries().Any() || context.IsSaveRejected || context.HasSavepointRollback)
                        throw new InvalidOperationException("Commodity return requires a fresh exclusively owned context.");
                    contextsOwned.Add(context, new object());
                }
                try { CharacterDatabaseIdentity.Require(context, database); return context; }
                catch { context.Dispose(); throw; }
            }

            internal async Task<BidOutcome> ExecuteAsync()
            {
                if (Interlocked.Exchange(ref running, 1) != 0) throw new InvalidOperationException("Commodity return already started.");
                try
                {
                    await using var driver = NewContext();
                    return await driver.Database.CreateExecutionStrategy().ExecuteAsync(Attempt);
                }
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

            internal async Task<CommodityReturnView> ReadReconciliationAsync()
            {
                await using var driver = NewContext();
                return await driver.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var context = NewContext();
                    await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    await LockRows(context);
                    if (!await OwnerMatches(context)) throw new InvalidOperationException("Commodity return lost its Character/account owner.");
                    var row = wallet == null ? null : await context.CharacterCurrency.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner.Id && row.CurrencyId == (byte)CurrencyType.Credits);
                    if (row != null && wallet.Cap != 0 && row.Amount > wallet.Cap) throw new InvalidOperationException("Commodity wallet exceeds its captured cap.");
                    List<BidItem> stored = [];
                    foreach (var item in items) stored.Add(BidItem.Capture(await context.Item.AsNoTracking().SingleOrDefaultAsync(row => row.Id == item.After.Id)));
                    ushort? amount = material == null ? null : await context.CharacterTradeskillMaterial.AsNoTracking()
                        .Where(row => row.Id == owner.Id && row.MaterialId == material.Id).Select(row => (ushort?)row.Amount).SingleOrDefaultAsync();
                    return new CommodityReturnView(await ReadOutcome(context), CommodityRow.Capture(await context.MarketplaceCommodityOrder.AsNoTracking().SingleOrDefaultAsync(row => row.Id == before.Id)),
                        wallet == null ? null : new(owner.Id, owner.Account, row != null, row?.Amount ?? 0), stored.ToArray(), amount, await DeliveryMatches(context, true));
                });
            }

            private async Task<BidOutcome> Attempt()
            {
                await using var context = NewContext();
                await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                await LockRows(context);
                if (await context.MarketplaceBidReceipt.AnyAsync(row => row.Id == id)) return await ReadOutcome(context);
                if (!TimeValid || !await Matches(context, false)) throw new InvalidOperationException("Commodity return lost its complete stored preimage.");
                var order = await context.MarketplaceCommodityOrder.SingleAsync(row => row.Id == before.Id);
                context.MarketplaceCommodityOrder.Remove(order);
                ObserveListingWrite(context, order, true, CommodityRow.Capture,
                    () => CommodityRow.Capture(context.MarketplaceCommodityOrder.AsNoTracking().SingleOrDefault(row => row.Id == before.Id)));
                if (WritesWallet)
                {
                    var money = await context.CharacterCurrency.SingleOrDefaultAsync(row => row.Id == owner.Id && row.CurrencyId == (byte)CurrencyType.Credits);
                    if (money == null) context.CharacterCurrency.Add(new() { Id = owner.Id, CurrencyId = (byte)CurrencyType.Credits, Amount = wallet.After });
                    else money.Amount = wallet.After;
                }
                foreach (var item in items)
                {
                    if (item.Before == null) context.Item.Add(item.After.Model());
                    else (await context.Item.SingleAsync(row => row.Id == item.Before.Id)).StackCount = item.After.Quantity;
                }
                if (material != null)
                {
                    var current = await context.CharacterTradeskillMaterial.SingleOrDefaultAsync(row => row.Id == owner.Id && row.MaterialId == material.Id);
                    if (current == null) context.CharacterTradeskillMaterial.Add(new() { Id = owner.Id, MaterialId = material.Id, Amount = material.After });
                    else current.Amount = material.After;
                }
                if (mail != null)
                {
                    context.CharacterMail.Add(mail.Model());
                    if (!before.Buy) context.CharacterMailAttachment.Add(new() { Id = mail.Id, Index = 0, ItemGuid = items[0].After.Id });
                }
                context.MarketplaceBidReceipt.Add(new() { Id = id, Payload = payload, CreatedUtc = created });
                context.ChangeTracker.DetectChanges();
                var enlisted = context.ChangeTracker.Entries().Select(entry => (Entry: entry, Values: entry.CurrentValues.Clone())).ToArray();
                context.OnSaveValidation(phase =>
                {
                    if (!ReferenceEquals(context.Database.CurrentTransaction, transaction) || context.HasSavepointRollback || !TimeValid
                        || context.ChangeTracker.Entries().Any(entry => !enlisted.Any(row => ReferenceEquals(row.Entry.Entity, entry.Entity)))
                        || enlisted.Any(row => !row.Values.Properties.All(property => Equals(row.Entry.CurrentValues[property.Name], row.Values[property.Name]))))
                        throw new InvalidOperationException("Commodity return changed its sealed graph.");
                    if (phase != CharacterSaveValidationPhase.BeforeFlush && (!Matches(context, true).GetAwaiter().GetResult()
                        || context.MarketplaceBidReceipt.AsNoTracking().SingleOrDefault(row => row.Id == id)?.Payload != payload))
                        throw new InvalidOperationException("Commodity return SQL differs from its complete postimage.");
                });
                await context.SaveChangesAsync(); await transaction.CommitAsync(); return new(true, true);
            }

            private async Task LockRows(CharacterContext context)
            {
                bool mysql = context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                if (!mysql && !context.Database.IsSqlite()) throw new InvalidOperationException("Unsupported commodity return provider.");
                _ = await (mysql ? context.Character.FromSqlInterpolated($"SELECT * FROM `character` WHERE id={owner.Id} FOR UPDATE")
                    : context.Character.Where(row => row.Id == owner.Id)).AsNoTracking().SingleOrDefaultAsync();
                _ = await (mysql ? context.MarketplaceCommodityOrder.FromSqlInterpolated($"SELECT * FROM `marketplace_commodity_order` WHERE id={before.Id} FOR UPDATE")
                    : context.MarketplaceCommodityOrder.Where(row => row.Id == before.Id)).AsNoTracking().SingleOrDefaultAsync();
                foreach (var item in items)
                    _ = await (mysql ? context.Item.FromSqlInterpolated($"SELECT * FROM `item` WHERE id={item.After.Id} FOR UPDATE")
                        : context.Item.Where(row => row.Id == item.After.Id)).AsNoTracking().SingleOrDefaultAsync();
                if (material != null)
                    _ = await (mysql ? context.CharacterTradeskillMaterial.FromSqlInterpolated($"SELECT * FROM `character_tradeskill_materials` WHERE id={owner.Id} AND materialId={material.Id} FOR UPDATE")
                        : context.CharacterTradeskillMaterial.Where(row => row.Id == owner.Id && row.MaterialId == material.Id)).AsNoTracking().SingleOrDefaultAsync();
            }

            private async Task<BidOutcome> ReadOutcome(CharacterContext context)
            {
                var receipt = await context.MarketplaceBidReceipt.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id);
                if (receipt != null && receipt.Payload != payload) throw new InvalidOperationException("Commodity receipt belongs to another command.");
                return new(receipt != null, await Matches(context, receipt != null));
            }
            private async Task<bool> OwnerMatches(CharacterContext context)
            {
                var row = await context.Character.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner.Id);
                return row != null && row.AccountId == owner.Account && row.DeleteTime == null;
            }
            private async Task<bool> Matches(CharacterContext context, bool committed)
            {
                if (!await OwnerMatches(context) || CommodityRow.Capture(await context.MarketplaceCommodityOrder.AsNoTracking().SingleOrDefaultAsync(row => row.Id == before.Id)) != (committed ? null : before)
                    || !await DeliveryMatches(context, committed)) return false;
                if (wallet != null)
                {
                    var row = await context.CharacterCurrency.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner.Id && row.CurrencyId == (byte)CurrencyType.Credits);
                    bool exists = committed ? wallet.Exists || WritesWallet : wallet.Exists;
                    if (row == null ? exists : !exists || row.Amount != (committed ? wallet.After : wallet.Before)) return false;
                }
                return true;
            }
            private async Task<bool> DeliveryMatches(CharacterContext context, bool committed)
            {
                foreach (var item in items)
                {
                    var target = committed ? item.After : item.Before;
                    if (BidItem.Capture(await context.Item.AsNoTracking().SingleOrDefaultAsync(row => row.Id == item.After.Id)) != target
                        || await context.RealmBankItem.AnyAsync(row => row.Id == item.After.Id)
                        || await context.MarketplaceAuction.AnyAsync(row => row.ItemId == item.After.Id)
                        || await context.CharacterMailAttachment.AnyAsync(row => row.ItemGuid == item.After.Id
                            && (!committed || mail == null || row.Id != mail.Id || row.Index != 0))) return false;
                    if (target?.Location == (ushort)InventoryLocation.Inventory && await context.Item.AnyAsync(row => row.Id != item.After.Id
                        && row.OwnerId == owner.Id && row.Location == target.Location && row.BagIndex == target.Bag)) return false;
                }
                if (material != null)
                {
                    var row = await context.CharacterTradeskillMaterial.AsNoTracking().SingleOrDefaultAsync(row => row.Id == owner.Id && row.MaterialId == material.Id);
                    if (row == null ? committed || material.Exists : !committed && !material.Exists || row.Amount != (committed ? material.After : material.Before)) return false;
                }
                if (mail != null)
                {
                    if (MailHeaderState.From(await context.CharacterMail.AsNoTracking().SingleOrDefaultAsync(row => row.Id == mail.Id)) != (committed ? mail : null)) return false;
                    var links = await context.CharacterMailAttachment.AsNoTracking().Where(row => row.Id == mail.Id).ToArrayAsync();
                    if (committed && !before.Buy ? links.Length != 1 || links[0].Index != 0 || links[0].ItemGuid != items[0].After.Id : links.Length != 0) return false;
                }
                return true;
            }
        }
    }
}
