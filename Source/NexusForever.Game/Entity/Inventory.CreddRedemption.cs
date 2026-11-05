using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        public void SettleCreddRedemption(Player owner, ulong amount, Func<bool> currentActor,
            IReadOnlyList<IWritable> success, IReadOnlyList<IWritable> failure)
        {
            if (!currentActor())
                return;
            owner.Save(Settle);

            void Settle()
            {
                lock (MutationGate)
                {
                    if (!currentActor())
                        return;
                    try
                    {
                        if (!TryAcquireMutationLease(out var lease))
                            throw new InvalidOperationException("CREDD redemption overlaps inventory persistence.");
                        using var held = lease;
                        Prepare();
                    }
                    catch (Exception error)
                    {
                        log.Error(error, "CREDD redemption preparation failed.");
                        foreach (var packet in failure)
                        {
                            if (!currentActor())
                                return;
                            owner.Session.EnqueueMessageEncrypted(packet);
                        }
                    }
                }
            }

            void Prepare()
            {
                ulong credit = checked(amount * VendorAccountPurchase.CreddToCreditsRatio);
                if (!ReferenceEquals(owner.Inventory, this) || owner.CurrencyManager is not CurrencyManager credits
                    || !credits.CanCreditFullAmount(CurrencyType.Credits, credit)
                    || owner.Account.CurrencyManager is not AccountCurrencyManager wallet
                    || owner.Account.EntitlementManager is not AccountEntitlementManager entitlements)
                    throw new InvalidOperationException("CREDD redemption lost its original wallet or full credit capacity.");

                using var batch = BeginRealmBankNotifications(true, retryVendorNotices: true)
                    ?? throw new InvalidOperationException("CREDD redemption requires the Character settlement owner.");
                batch.RequireActor(currentActor);
                batch.OnFailure(failure);
                var auth = owner.GetAuthDatabase();
                var protocol = new VendorAccountPurchase(owner.GetCharacterDatabase().CreateContext,
                    auth.CreateContext, owner.Identity.RealmId);
                var usage = entitlements.CaptureCreddUsage();
                VendorAccountSettlement settlement;
                try
                {
                    var intent = protocol.CreateCreddRedemption(owner.Account.Id, owner.CharacterId, amount,
                        usage.Limit, owner.Account.WorldSessionEpoch);
                    settlement = new(protocol, intent, auth.CreateContext, wallet.CaptureVendorWallets(intent.Costs), usage);
                }
                catch
                {
                    usage.Dispose();
                    throw;
                }
                batch.AttachAccountSettlement(settlement);
                var payment = credits.CaptureBankCurrency(owner, [new(CurrencyType.Credits, credit)],
                    batch.CurrentActor, batch.After);
                batch.Attach(payment);
                batch.Add(() => settlement.Publish(batch.CurrentActor, batch.Send));
                batch.Add(() => payment.Publish(batch.Send));
                foreach (var packet in success)
                    batch.Add(packet);
                batch.Complete();
            }
        }
    }
}
