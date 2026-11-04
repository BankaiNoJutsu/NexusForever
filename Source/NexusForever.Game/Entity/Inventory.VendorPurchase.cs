using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        internal void SettleVendorPurchase(Player owner, SupplySatchelManager satchel, IItemInfo item, uint count,
            IReadOnlyDictionary<uint, uint> itemCosts, IReadOnlyDictionary<CurrencyType, ulong> currencyCosts, Func<bool> currentActor)
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
                        using var batch = BeginRealmBankNotifications(true);
                        if (batch == null) return;
                        batch.RequireActor(currentActor);
                        var purchase = new VendorPurchaseSettlement(characterId, database,
                            satchel?.ReserveCommodityReturn(item, count, database));
                        batch.Attach(purchase);
                        var payment = wallet.CaptureBankCurrency(owner, currencyCosts.Where(cost => cost.Value != 0).Select(cost =>
                            new CurrencyManager.BankCurrencyChange(cost.Key, cost.Value, Subtract: true)), batch.CurrentActor, batch.After);
                        batch.Attach(payment);
                        batch.Add(payment.Publish);
                        foreach (var cost in itemCosts.Where(cost => cost.Value != 0))
                            ItemDelete(cost.Key, cost.Value, ItemUpdateReason.Vendor);
                        CreateVendorPurchase(item, count - purchase.MaterialCount, satchel: null);
                        batch.Add(() => purchase.Publish(batch.CurrentActor));
                        batch.Complete();
                    }
                    catch (Exception error)
                    {
                        log.Error(error, "Vendor purchase preparation failed.");
                    }
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
            internal void Publish(Func<bool> allowed) => material?.Publish(allowed);
        }
    }
}
