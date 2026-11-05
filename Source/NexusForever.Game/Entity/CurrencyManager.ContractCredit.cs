using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Quest;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class CurrencyManager
    {
        private ContractCreditReservation contractCredit;
        private volatile bool contractCurrencyReload;
        private bool publishingContractCredit;
        internal bool HasPendingContractCurrency => contractCredit != null || publishingContractCredit || bankCurrency != null || auctionBidCurrency != null;
        internal bool RequiresContractCurrencyReload => contractCurrencyReload;

        private bool ContractCurrencyBlocked(CurrencyType currency) => contractCurrencyReload || publishingContractCredit || contractCredit?.Type == currency || bankCurrency?.Has(currency) == true
            || auctionBidCurrency != null && currency == CurrencyType.Credits;
        internal void InvalidateContractCredit()
        {
            if (HasPendingContractCurrency) contractCurrencyReload = true;
            contractCredit?.Quarantine();
            bankCurrency?.Quarantine();
            auctionBidCurrency?.Quarantine();
        }
        private void AssertContractCurrencySaveAllowed(CharacterContext context = null)
        {
            if (!HasPendingContractCurrency && !contractCurrencyReload) return;
            var error = new InvalidOperationException("Character currency belongs to a pending or quarantined Contract claim.");
            context?.RejectSave(error); throw error;
        }
        private static ulong AddCurrencyAmount(ulong before, ulong requested, ulong cap)
        {
            if (cap == 0) return checked(before + requested);
            if (before > cap) throw new InvalidOperationException("Currency baseline exceeds its cap.");
            return before + Math.Min(requested, cap - before);
        }
        private bool DeferContractCurrency(CurrencyType currency, ulong amount, bool subtract, bool loot)
        {
            if (contractCurrencyReload) throw new InvalidOperationException("Currency requires authoritative reload.");
            if (auctionBidCurrency != null && currency == CurrencyType.Credits)
            { auctionBidCurrency.Defer(amount, subtract, loot); return true; }
            if (DeferBankCurrency(currency, amount, subtract, loot)) return true;
            if (contractCredit?.Type != currency) return false;
            contractCredit.Defer(amount, subtract, loot); return true;
        }

        internal bool TryReserveContractCredit(ContractRewardAwardCatalog catalog, ContractRewardClaimQuote quote,
            string claimId, string database, out ContractCreditReservation reservation)
        {
            reservation = null;
            if (player == null || contractCurrencyReload || HasPendingContractCurrency || quote?.Award.CharacterCurrency == null
                || quote.CharacterId != player.CharacterId || player.Account?.Id is not > 0 || player.Session == null
                || player.Session is NetworkSession { IsDisconnecting: true } || player.LogoutManager?.State is LogoutState.Logout or LogoutState.Finished
                || string.IsNullOrEmpty(database) || !HasLoadedCurrencyOwner() || loadedOrigin != null && database != loadedOrigin.Database
                || !Guid.TryParseExact(claimId, "N", out var id) || id == Guid.Empty) return false;
            var canonical = ContractRewardClaimQuote.Restore(catalog, quote.Json);
            var award = canonical.Award.CharacterCurrency;
            var entry = gameTableManager.CurrencyType?.GetEntry((uint)award.Type);
            if (entry == null || entry.Id != (uint)award.Type || entry.CapAmount != award.CapAmount) return false;
            bool existed = currencies.TryGetValue(award.Type, out var loaded);
            if (existed && (loaded is not Currency currency || currency.NeedsPersistenceSave || currency.CharacterId != player.CharacterId)) return false;
            var wallet = existed ? (Currency)loaded : CreateCurrencyOwner(entry);
            var owned = wallet.CaptureContractCredit(existed, award.CapAmount);
            try
            {
                var delivery = ContractCurrencyDelivery.Capture(award, owned.Before);
                reservation = new(this, catalog, canonical, claimId, database, wallet, owned, delivery,
                    player.Account, player.Session, existed);
                contractCredit = reservation; return true;
            }
            catch { owned.Cancel(); throw; }
        }

        internal sealed class ContractCreditReservation(CurrencyManager manager, ContractRewardAwardCatalog catalog,
            ContractRewardClaimQuote quote, string claim, string database, Currency wallet,
            Currency.ContractCreditOwner owned, ContractCurrencyDelivery expected, IAccount account, IGameSession session, bool existed)
        {
            private sealed record Mutation(ulong Amount, bool Subtract, bool Loot);
            private readonly List<Mutation> deferred = [];
            private readonly uint accountId = account.Id;
            private readonly IGameSession accountSession = account.Session;
            private readonly EventQueue events = session.Events;
            private ulong rollbackShadow = expected.BeforeAmount, commitShadow = expected.AfterAmount;
            private CharacterChallengeRewardCommand command;
            private IInventoryRewardReservation items;
            private bool registered = existed;
            private bool creditCommitted;
            private int state; //0 prepared,1 executing,2 adopted,3 published/canceled,4 quarantined
            internal CurrencyType Type => quote.Award.CharacterCurrency.Type;
            internal ContractCurrencyBefore Before => owned.Before;

            private void Validate()
            {
                if (!ReferenceEquals(manager.contractCredit, this) || manager.contractCurrencyReload
                    || session is NetworkSession { IsDisconnecting: true } || manager.player.LogoutManager?.State is LogoutState.Logout or LogoutState.Finished
                    || manager.player.CharacterId != quote.CharacterId || !ReferenceEquals(manager.player.Account, account)
                    || account.Id != accountId || !ReferenceEquals(account.Session, accountSession) || !ReferenceEquals(manager.player.Session, session)
                    || !ReferenceEquals(session.Events, events) || session is IPlayerSession bound && !ReferenceEquals(bound.Player, manager.player)
                    || registered != manager.currencies.TryGetValue(Type, out var current) || registered && !ReferenceEquals(current, wallet)
                    || wallet.CharacterId != quote.CharacterId || wallet.Entry.Id != (uint)Type || wallet.Entry.CapAmount != quote.Award.CharacterCurrency.CapAmount)
                    throw new InvalidOperationException("Contract credit lost its original live Character/account/session wallet.");
                if (state < 2) owned.Validate();
            }
            internal void Defer(ulong amount, bool subtract, bool loot)
            {
                Validate();
                if (state > 3 || state == 3 && !manager.publishingContractCredit)
                    throw new InvalidOperationException("Contract credit no longer owns deferred currency.");
                ulong Next(ulong value) => subtract ? checked(value - amount) : AddCurrencyAmount(value, amount, quote.Award.CharacterCurrency.CapAmount);
                ulong rollback = rollbackShadow, commit = commitShadow;
                if (state < 2 || !creditCommitted) rollback = Next(rollback);
                if (state < 2 || creditCommitted) commit = Next(commit);
                deferred.Add(new(amount, subtract, loot)); rollbackShadow = rollback; commitShadow = commit;
            }
            internal void Begin(CharacterChallengeRewardCommand next, IInventoryRewardReservation delivery)
            {
                Validate();
                if (state != 0 || next?.Kind != CharacterChallengeRewardCommandKind.ClaimContract || next.Identity != claim
                    || next.CharacterId != quote.CharacterId || next.ContractAccountId != accountId || next.ContractQuote.Json != quote.Json
                    || next.ContractCurrencyBefore != Before || (quote.Award.Item == null) != (delivery == null))
                    throw new InvalidOperationException("Contract credit changed its exact queued claim command.");
                command = next; items = delivery; state = 1;
            }
            internal bool Adopt(CharacterChallengeRewardExecution result)
            {
                if (state != 1) return false;
                try
                {
                    Validate();
                    if (result?.Outcome != CharacterChallengeRewardExecutionOutcome.Committed || !result.OriginalUnitEnded
                        || !CharacterChallengeRewardExecutor.IsCompletionOf(result, command, database, items)
                        || result.Operation is not ContractClaimStage stage || stage.Claim.ClaimId != claim || stage.Claim.AccountId != accountId
                        || stage.Claim.QuoteJson != quote.Json || stage.Claim.CharacterCurrencyJson != expected.Json)
                        throw new InvalidOperationException("Contract credit has no matching committed execution proof.");
                    stage.Claim.Validate(catalog, quote.CharacterId, quote.Award.TrackId, quote.CycleId, quote.Award.Ordinal, quote.Award.RewardRowId, quote.SelectionJson);
                    owned.Adopt(expected.AfterAmount);
                    if (!registered) { manager.currencies.Add(Type, wallet); registered = true; }
                    creditCommitted = true; state = 2; return true;
                }
                catch { Quarantine(); return false; }
            }
            internal bool Publish()
            {
                if (state != 2) return false;
                try
                {
                    Validate();
                    state = 3; manager.publishingContractCredit = true;
                    session.EnqueueMessageEncrypted(new ServerCombatReward { Stat = (byte)(Type - 1), NewValue = expected.AfterAmount });
                    Validate();
                    if (expected.Granted != 0)
                    {
                        manager.player.AchievementManager.CheckAchievements(manager.player, AchievementType.CurrencyEarned, (uint)Type, count: ToAchievementCount(expected.Granted));
                        Validate();
                        CurrencyQuestObjectiveUpdater.OnCurrencyAdded(manager.player, Type, expected.Granted);
                    }
                    Replay();
                    Validate();
                    manager.contractCredit = null; return true;
                }
                catch { Quarantine(); return false; }
                finally { manager.publishingContractCredit = false; }
            }
            internal bool Cancel()
            {
                if (state != 0) return false;
                try
                {
                    Validate(); owned.Cancel(); state = 3; manager.publishingContractCredit = true;
                    if (!registered && deferred.Any(mutation => !mutation.Subtract))
                    { manager.currencies.Add(Type, wallet); registered = true; }
                    Replay(); manager.contractCredit = null; return true;
                }
                catch { Quarantine(); return false; }
                finally { manager.publishingContractCredit = false; }
            }
            private void Replay()
            {
                for (int index = 0; index < deferred.Count; ++index)
                {
                    var mutation = deferred[index];
                    if (manager.contractCurrencyReload) throw new InvalidOperationException("Deferred currency lost its live owner.");
                    Validate();
                    if (mutation.Subtract) manager.ApplyCurrencySubtract(wallet.Entry, mutation.Amount, mutation.Loot);
                    else manager.ApplyCurrencyAdd(wallet.Entry, mutation.Amount, mutation.Loot);
                }
                deferred.Clear();
            }
            internal void Quarantine()
            {
                state = 4; manager.contractCurrencyReload = true;
                // Unproved SQL ownership remains pinned. Never replay gameplay
                // or network work from a worker/dead-owner continuation.
            }
        }
    }
}
