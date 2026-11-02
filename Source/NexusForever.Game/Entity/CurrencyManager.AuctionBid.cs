using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Entity
{
    public partial class CurrencyManager
    {
        private AuctionBidCurrency auctionBidCurrency;

        internal string GetAuctionBidDatabase(IPlayer owner)
        {
            AssertLoadedCurrencyOwner();
            if (!ReferenceEquals(owner, player) || loadedOrigin == null || HasPendingContractCurrency || contractCurrencyReload)
                throw new InvalidOperationException("Auction bid requires its qualified unreserved Character wallet.");
            return loadedOrigin.Database;
        }

        // Called only after the original actor's complete ordinary save. The
        // returned scalar wallet is the only part supplied to the SQL executor.
        internal AuctionBidCurrency CaptureAuctionBid(IPlayer owner, ulong amount, bool subtract, Func<bool> ownsActor, bool silentSettlement = false)
        {
            _ = GetAuctionBidDatabase(owner);
            if (!ownsActor()) throw new InvalidOperationException("Auction bid lost its retained actor.");
            var entry = gameTableManager.CurrencyType?.GetEntry((uint)CurrencyType.Credits)
                ?? throw new InvalidOperationException("Auction bid has no canonical Credits definition.");
            bool existed = currencies.TryGetValue(CurrencyType.Credits, out var current);
            var wallet = existed ? current as Currency : CreateCurrencyOwner(entry);
            if (wallet == null || wallet.NeedsPersistenceSave && existed || entry.CapAmount != wallet.Entry.CapAmount)
                throw new InvalidOperationException("Auction bid requires a clean canonical wallet.");
            var row = wallet.CaptureContractCredit(existed, entry.CapAmount);
            try
            {
                ulong after = subtract ? checked(row.Before.Amount - amount) : AddCurrencyAmount(row.Before.Amount, amount, entry.CapAmount);
                var reservation = new AuctionBidCurrency(this, wallet, row, existed, after, subtract, ownsActor, silentSettlement);
                wallet.BeginAuctionOwnership(reservation);
                return auctionBidCurrency = reservation;
            }
            catch { row.Cancel(); throw; }
        }

        internal Action<NexusForever.Database.Character.CharacterContext> CaptureAuctionBidNormalSave(object token, AuctionBidCurrency reservation)
        {
            if (!ReferenceEquals(auctionBidCurrency, reservation) || !reservation.CanSave)
                throw new InvalidOperationException("Auction reconciliation lost its currency capture.");
            AssertLoadedCurrencyOwner();
            var rows = currencies.Values.Select(currency => currency is Currency concrete
                ? concrete.CaptureNormalSave(token, concrete.Id == CurrencyType.Credits ? reservation : null)
                : throw new InvalidOperationException("Auction reconciliation requires concrete currency owners.")).ToArray();
            return context => { AssertLoadedCurrencyContext(context); foreach (var stage in rows) stage(context); };
        }

        internal sealed class AuctionBidCurrency
        {
            private enum Phase { Captured, Applying, Saving, Publishing, Quarantined }
            private sealed record Intent(ulong Amount, bool Subtract, bool Loot);
            private sealed record Line(ulong Before, ulong After, bool Credit, bool Loot, ulong Value);
            private readonly CurrencyManager manager;
            private readonly Currency wallet;
            private readonly Currency.ContractCreditOwner owned;
            private readonly Func<bool> ownsActor;
            private readonly bool existed, subtract;
            private readonly bool silentSettlement;
            private bool registered;
            private readonly List<Intent> later = [];
            private readonly List<Line> notifications = [];
            private Line[] commodityLedger;
            private ulong rollback, commit;
            private Phase phase;
            private int applied;
            internal bool CanSave => phase == Phase.Saving && ReferenceEquals(manager.auctionBidCurrency, this);
            internal GlobalMarketplaceManager.BidWallet Snapshot { get; }

            internal AuctionBidCurrency(CurrencyManager manager, Currency wallet, Currency.ContractCreditOwner owned,
                bool existed, ulong after, bool subtract, Func<bool> ownsActor, bool silentSettlement)
            {
                this.manager = manager; this.wallet = wallet; this.owned = owned; this.existed = existed;
                this.subtract = subtract; this.ownsActor = ownsActor;
                if (silentSettlement && after != owned.Before.Amount) throw new InvalidOperationException("A silent delivery wallet cannot change balance.");
                this.silentSettlement = silentSettlement;
                registered = existed;
                rollback = owned.Before.Amount; commit = after;
                Snapshot = new(manager.player.CharacterId, manager.player.Account.Id, existed, owned.Before.Amount, after, wallet.Entry.CapAmount);
            }

            private void Validate()
            {
                manager.AssertLoadedCurrencyOwner();
                if (!ownsActor() || manager.contractCurrencyReload || phase == Phase.Quarantined
                    || wallet.CharacterId != Snapshot.Character || wallet.Id != CurrencyType.Credits || wallet.Entry.CapAmount != Snapshot.Cap
                    || registered != manager.currencies.TryGetValue(CurrencyType.Credits, out var current)
                    || registered && !ReferenceEquals(current, wallet)
                    || !ReferenceEquals(manager.auctionBidCurrency, this))
                    throw new InvalidOperationException("Auction currency lost its original actor ownership.");
            }
            private ulong Next(ulong before, Intent intent) => intent.Subtract ? checked(before - intent.Amount)
                : AddCurrencyAmount(before, intent.Amount, Snapshot.Cap);

            internal void CaptureCommodityLedger(ulong debit, IEnumerable<ulong> credits)
            {
                Validate();
                if (phase != Phase.Captured || commodityLedger != null)
                    throw new InvalidOperationException("Commodity currency ledger has already been captured.");
                List<Line> lines = [];
                ulong current = Snapshot.Before;
                if (debit != 0)
                {
                    ulong after = checked(current - debit);
                    lines.Add(new(current, after, false, false, Snapshot.After)); current = after;
                }
                foreach (ulong credit in credits)
                {
                    ulong after = AddCurrencyAmount(current, credit, Snapshot.Cap);
                    lines.Add(new(current, after, true, false, Snapshot.After)); current = after;
                }
                if (current != Snapshot.After)
                    throw new InvalidOperationException("Commodity currency effects differ from the captured SQL balance.");
                commodityLedger = lines.ToArray();
            }

            internal void Defer(ulong amount, bool subtract, bool loot)
            {
                Validate();
                if (phase is not (Phase.Captured or Phase.Applying or Phase.Saving)) throw new InvalidOperationException("Auction currency no longer accepts deferred intent.");
                var intent = new Intent(amount, subtract, loot);
                if (phase == Phase.Captured)
                {
                    ulong beforeRollback = Next(rollback, intent), beforeCommit = Next(commit, intent);
                    later.Add(intent); rollback = beforeRollback; commit = beforeCommit;
                }
                else
                {
                    ulong shadow = wallet.Amount;
                    for (int index = applied; index < later.Count; index++) shadow = Next(shadow, later[index]);
                    _ = Next(shadow, intent); later.Add(intent);
                }
            }

            // A confirmed bid/rollback chooses the base. Apply earning effects
            // before the complete ordinary graph is saved; wallet packets wait
            // for that graph's successful completion on this same actor.
            internal void PrepareOrdinaryReconciliation(bool committed, GlobalMarketplaceManager.BidWalletCurrent current = null)
            {
                Validate(); if (phase != Phase.Captured) throw new InvalidOperationException("Auction currency settlement already began.");
                phase = Phase.Applying;
                try
                {
                    if (current != null && (current.Character != Snapshot.Character || current.Account != Snapshot.Account
                        || !current.Exists && current.Amount != 0 || Snapshot.Cap != 0 && current.Amount > Snapshot.Cap))
                        throw new InvalidOperationException("Auction currency changed its authoritative reconciliation owner.");
                    ulong baseline = current?.Amount ?? (committed ? Snapshot.After : Snapshot.Before);
                    bool exists = current?.Exists ?? (committed || existed);
                    if (current != null) owned.AdoptCurrent(exists, baseline);
                    else if (committed) owned.Adopt(baseline); else owned.Cancel();
                    if (exists) { manager.currencies[CurrencyType.Credits] = wallet; registered = true; }
                    else { manager.currencies.Remove(CurrencyType.Credits); registered = false; }
                    if (committed && commodityLedger != null)
                        foreach (var line in commodityLedger) ApplyEffects(line with { Value = baseline });
                    else if (committed && !silentSettlement) ApplyEffects(new(Snapshot.Before, Snapshot.After, !subtract, false, baseline));
                    ApplyLater(); Validate(); phase = Phase.Saving;
                }
                catch { Quarantine(); throw; }
            }

            internal bool PrepareNextOrdinaryReconciliation()
            {
                Validate(); if (phase != Phase.Saving) throw new InvalidOperationException("Auction reconciliation is not waiting for its ordinary save.");
                if (applied == later.Count) return false;
                phase = Phase.Applying;
                try { ApplyLater(); Validate(); phase = Phase.Saving; return true; }
                catch { Quarantine(); throw; }
            }

            private void ApplyLater()
            {
                while (applied < later.Count)
                {
                    var intent = later[applied++]; ulong before = wallet.Amount, after = Next(before, intent);
                    if (!registered) { manager.currencies.Add(CurrencyType.Credits, wallet); registered = true; }
                    wallet.ApplyAuctionAmount(this, after); ApplyEffects(new(before, after, !intent.Subtract, intent.Loot, after));
                }
            }

            private void ApplyEffects(Line line)
            {
                Validate(); notifications.Add(line);
                ulong granted = line.Credit ? line.After - line.Before : 0;
                if (granted == 0) return;
                manager.player.AchievementManager.CheckAchievements(manager.player, AchievementType.CurrencyEarned, (uint)CurrencyType.Credits, count: ToAchievementCount(granted));
                Validate(); CurrencyQuestObjectiveUpdater.OnCurrencyAdded(manager.player, CurrencyType.Credits, granted);
            }

            internal void CompleteOrdinaryReconciliation(bool publish, Func<bool> publicationAllowed = null)
            {
                Validate(); if (phase != Phase.Saving) throw new InvalidOperationException("Auction currency has no reconciled ordinary save.");
                // Mark before delivery. A packet failure must not replay earlier
                // notifications or the already persisted earning effects.
                phase = Phase.Publishing;
                foreach (var line in publish ? notifications : [])
                {
                    Validate();
                    if (publicationAllowed?.Invoke() == false) break;
                    if (line.Loot) manager.player.Session.EnqueueMessageEncrypted(new ServerChannelUpdateLoot
                        { CurrencyId = CurrencyType.Credits, Amount = unchecked(line.After - line.Before) });
                    Validate();
                    if (publicationAllowed?.Invoke() == false) break;
                    manager.player.Session.EnqueueMessageEncrypted(new ServerCombatReward
                        { Stat = (byte)(CurrencyType.Credits - 1), NewValue = line.Value });
                }
                wallet.EndAuctionOwnership(this); manager.auctionBidCurrency = null;
            }

            internal void Quarantine() { phase = Phase.Quarantined; manager.contractCurrencyReload = true; }
        }
    }
}
