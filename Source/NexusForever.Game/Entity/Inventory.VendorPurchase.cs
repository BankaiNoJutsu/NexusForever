using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        internal void SettleVendorPurchase(Player owner, SupplySatchelManager satchel, IItemInfo item, uint count,
            IReadOnlyDictionary<uint, uint> itemCosts, IReadOnlyDictionary<CurrencyType, ulong> currencyCosts,
            IReadOnlyDictionary<AccountCurrencyType, ulong> accountCosts, Func<bool> currentActor)
        {
            if (!currentActor()) return;
            // Settle earlier gameplay as one unit before spending its proceeds
            // or changing one side of a previously split stack.
            owner.Save(Settle);

            void Settle()
            {
                lock (MutationGate)
                {
                    if (!TryAcquireMutationLease(out var lease)) return;
                    using var held = lease;
                    if (!currentActor() || commodityDelivery != null || owner.CurrencyManager is not CurrencyManager wallet
                        || !CanCreateVendorPurchase(item, count, satchel, itemCosts)) return;

                    try
                    {
                        string database = wallet.GetAuctionBidDatabase(owner);
                        using var batch = BeginRealmBankNotifications(true, retryVendorNotices: true);
                        if (batch == null) return;
                        batch.RequireActor(currentActor);
                        var costs = accountCosts.Where(cost => cost.Value != 0)
                            .Select(cost => new VendorAccountPurchase.Cost(cost.Key, cost.Value)).OrderBy(cost => cost.Currency).ToArray();
                        if (costs.Length != 0)
                        {
                            if (owner.Account.CurrencyManager is not AccountCurrencyManager accountWallet)
                                throw new InvalidOperationException("Vendor payment requires its original account wallet manager.");
                            var auth = owner.GetAuthDatabase();
                            var protocol = new VendorAccountPurchase(owner.GetCharacterDatabase().CreateContext, auth.CreateContext, owner.Identity.RealmId);
                            var quote = JsonSerializer.SerializeToUtf8Bytes(new
                            {
                                Item = item.Id, Count = count, Items = itemCosts.OrderBy(cost => cost.Key),
                                Currencies = currencyCosts.OrderBy(cost => cost.Key), Account = costs
                            });
                            var intent = protocol.Create(owner.Account.Id, characterId, item.Id, count,
                                Convert.ToHexString(SHA256.HashData(quote)), costs, owner.Account.WorldSessionEpoch);
                            var account = new VendorAccountSettlement(protocol, intent, auth.CreateContext, accountWallet.CaptureVendorWallets(costs));
                            batch.AttachAccountSettlement(account);
                            batch.Add(() => account.Publish(batch.CurrentActor, batch.Send));
                        }
                        var purchase = new VendorPurchaseSettlement(characterId, database,
                            satchel?.ReserveCommodityReturn(item, count, database));
                        batch.Attach(purchase);
                        var payment = wallet.CaptureBankCurrency(owner, currencyCosts.Where(cost => cost.Value != 0).Select(cost =>
                            new CurrencyManager.BankCurrencyChange(cost.Key, cost.Value, Subtract: true)), batch.CurrentActor, batch.After);
                        batch.Attach(payment);
                        batch.Add(() => payment.Publish(batch.Send));
                        foreach (var cost in itemCosts.Where(cost => cost.Value != 0))
                            ItemDelete(cost.Key, cost.Value, ItemUpdateReason.Vendor);
                        CreateVendorPurchase(item, count - purchase.MaterialCount, satchel: null);
                        batch.Add(() => purchase.Publish(batch.CurrentActor, batch.Send));
                        batch.Complete();
                    }
                    catch (Exception error)
                    {
                        log.Error(error, "Vendor purchase preparation failed.");
                    }
                }
            }
        }

        private sealed class VendorAccountSettlement(VendorAccountPurchase protocol, VendorAccountPurchase.Intent intent,
            Func<AuthContext> auth, AccountCurrencyManager.VendorWallets wallets,
            AccountEntitlementManager.CreddUsageReservation usage = null) : IBankSaveCompanion
        {
            public bool CanExecuteWithoutItems => usage != null;
            internal string ReceiptId => intent.Id;
            internal Func<CharacterContext, Action<CharacterContext>, Task<bool>> Authorization => async (context, enroll) =>
                await protocol.AuthorizeAsync(context, intent, enroll) == VendorAccountAuthorization.Authorized;

            internal async Task ObserveAsync(Task<RealmBankManager.BankOutcome> outcome)
            {
                var resolved = await outcome;
                await protocol.ResolveWalletAsync(intent, resolved.Committed);
                await wallets.ReadAsync(auth);
                if (usage != null) await usage.ReadAsync(auth);
            }

            public void Stage(CharacterContext context) { }
            internal void AdoptWallets()
            {
                wallets.Adopt();
                usage?.Adopt();
            }
            public void Adopt() { }
            public bool ReleaseForOrdinarySave() => true;
            public void Complete() { wallets.Dispose(); usage?.Dispose(); }
            public void Abort() => Complete();
            public void Quarantine() { wallets.Quarantine(); usage?.Dispose(); }
            internal void Publish(Func<bool> currentActor, Action<IWritable> send)
            {
                wallets.Publish(currentActor, send);
                if (usage != null)
                {
                    if (!currentActor())
                        throw new InvalidOperationException("CREDD usage publication lost its original actor.");
                    usage.Publish(send);
                }
            }
        }

        private sealed class VendorPurchaseSettlement(ulong character, string database,
            SupplySatchelManager.CommodityReturnReservation material) : IBankSaveCompanion
        {
            private readonly string receipt = Guid.NewGuid().ToString("N");
            private readonly DateTime created = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).UtcDateTime;
            private string Payload => $"vendor-character-purchase:{character}";
            internal uint MaterialCount => material == null ? 0u : (uint)(material.Write.After - material.Write.Before);
            public bool CanExecuteWithoutItems => true;

            public void Stage(CharacterContext context)
            {
                CharacterDatabaseIdentity.Require(context, database);
                var prior = ReadReceipt();
                if (prior != null && !Matches(prior))
                    throw new InvalidOperationException("Vendor purchase receipt changed its owner.");
                if (prior == null)
                    context.BankReconciliationReceipt.Add(new BankReconciliationReceiptModel { Id = receipt, Payload = Payload, CreatedUtc = created });
                context.OnSaveValidation(phase =>
                {
                    if (phase != CharacterSaveValidationPhase.BeforeFlush && !Matches(ReadReceipt()))
                        throw new InvalidOperationException("Vendor purchase receipt differs from its captured operation.");
                });
                if (material == null) return;

                var write = material.Write;
                var row = context.CharacterTradeskillMaterial.SingleOrDefault(value => value.Id == character && value.MaterialId == write.Id);
                ushort? expected = prior != null ? write.After : write.Exists ? write.Before : null;
                if (row?.Amount != expected)
                    throw new InvalidOperationException("Vendor purchase material changed after capture.");
                if (prior == null)
                {
                    if (row == null)
                        context.CharacterTradeskillMaterial.Add(new CharacterTradeskillMaterialModel { Id = character, MaterialId = write.Id, Amount = write.After });
                    else
                        row.Amount = write.After;
                }
                context.OnSaveValidation(phase =>
                {
                    var stored = context.CharacterTradeskillMaterial.AsNoTracking().SingleOrDefault(value => value.Id == character && value.MaterialId == write.Id);
                    if (stored?.Amount != (phase == CharacterSaveValidationPhase.BeforeFlush ? expected : write.After))
                        throw new InvalidOperationException("Vendor purchase material differs from its captured amount.");
                });

                BankReconciliationReceiptModel ReadReceipt() => context.BankReconciliationReceipt.AsNoTracking().SingleOrDefault(row => row.Id == receipt);
                bool Matches(BankReconciliationReceiptModel row) => row != null && row.Payload == Payload && row.CreatedUtc == created;
            }

            public void Adopt() => material?.Settle(material.Write.After);
            public bool ReleaseForOrdinarySave() { material?.Dispose(); return true; }
            public void Complete() => material?.Dispose();
            public void Abort() => material?.Dispose();
            public void Quarantine() => material?.Dispose();
            internal void Publish(Func<bool> allowed, Action<IWritable> send) => material?.Publish(allowed, send);
        }
    }
}
