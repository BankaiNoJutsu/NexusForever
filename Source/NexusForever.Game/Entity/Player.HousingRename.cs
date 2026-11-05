using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private HousingNameActor housingNameActor;
        internal bool HousingNameSaveAllowed => housingNameActor?.CanSave == true;
        internal bool HousingNamePublishing => housingNameActor?.Publishing == true;
        internal bool HousingNameCanBegin => housingNameActor == null && bankActor == null && auctionBidActor == null
            && !IsNormalSaveInProgress && !RewardStateRequiresReload && CurrencyManager is CurrencyManager { HasPendingContractCurrency: false };

        internal HousingNameActor BeginHousingName(IHousingFeeOperation command, CurrencyType type, ulong cost)
        {
            if (!HousingNameCanBegin || !TryAcquireInventorySave(out var lease)) return null;
            HousingNameActor actor = null;
            try
            {
                actor = new(this, command, lease); housingNameActor = actor;
                actor.Capture(type, cost); Session.CanProcessIncomingPackets = false; return actor;
            }
            catch
            {
                actor?.CancelBeforeAttempt();
                if (ReferenceEquals(housingNameActor, actor)) housingNameActor = null;
                if (actor == null) lease?.Dispose();
                throw;
            }
        }

        internal sealed class HousingNameActor
        {
            private readonly Player player;
            private readonly IHousingFeeOperation command;
            private readonly IAccount account;
            private readonly IGameSession session;
            private readonly CurrencyManager currencies;
            private readonly IInventory inventory;
            private readonly IQuestManager quests;
            private readonly Abstract.Challenges.IChallengeManager challenges;
            private readonly uint accountId;
            private readonly ulong character;
            private readonly IDisposable inventoryLease;
            private IDisposable frozen;
            private IDisposable publication;
            private CurrencyManager.BankCurrencyReservation wallet;
            private NormalRewardSave snapshot;
            private bool capturing, enrolling, ended, committedEffectsPending, primary = true;
            internal bool Retired, CleanupRequested, DisposeRequested;
            internal bool Publishing { get; private set; }
            internal bool ReadyToPublish { get; private set; }
            internal string Database { get; }
            internal readonly object Token = new();
            internal bool CanSave => (capturing || enrolling) && Owns();
            internal bool Primary => primary;
            internal bool OwnsAuthSnapshot(NormalRewardSave candidate) => candidate != null
                && ReferenceEquals(snapshot, candidate) && Owns() && !Publishing;
            internal bool CanPublish => Owns() && !Retired && session is IPlayerSession original && ReferenceEquals(original.Player, player)
                && session is not NetworkSession { IsDisconnecting: true }
                && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

            internal HousingNameActor(Player player, IHousingFeeOperation command, IDisposable lease)
            {
                this.player = player; this.command = command; inventoryLease = lease;
                account = player.Account; session = player.Session; currencies = player.CurrencyManager as CurrencyManager;
                inventory = player.Inventory; quests = player.QuestManager; challenges = player.ChallengeManager;
                character = player.CharacterId; accountId = account?.Id ?? 0;
                if (currencies == null || character == 0 || accountId == 0 || session == null || !ReferenceEquals(account.Session, session)
                    || quests is not NexusForever.Game.Entity.QuestManager || challenges is not ChallengeManager manager || inventory == null)
                    throw new InvalidOperationException("Paid housing name requires its complete original Character actor.");
                Database = currencies.GetAuctionBidDatabase(player);
                if (manager.NormalSaveIdentity?.ExecutionDatabase != Database)
                    throw new InvalidOperationException("Paid housing name requires its prepared original save route.");
            }
            internal bool Owns() => !ended && ReferenceEquals(player.housingNameActor, this)
                && player.CharacterId == character && ReferenceEquals(player.Account, account) && account.Id == accountId
                && ReferenceEquals(player.Session, session) && ReferenceEquals(account.Session, session)
                && ReferenceEquals(player.CurrencyManager, currencies) && ReferenceEquals(player.Inventory, inventory)
                && ReferenceEquals(player.QuestManager, quests) && ReferenceEquals(player.ChallengeManager, challenges);
            internal void Capture(CurrencyType type, ulong cost)
            {
                frozen = QuestSaveScope.AcquireFrozen(command, Token, Database, character);
                if (cost != 0) wallet = currencies.CaptureFrozenCurrency(player, Token,
                    [new(type, cost, true)], Owns, action => action());
                command.PrepareEffects();
                CaptureSnapshot();
            }
            private void CaptureSnapshot()
            {
                if (!Owns() || snapshot != null) throw new InvalidOperationException("Paid name lost its original frozen snapshot.");
                capturing = true;
                try { snapshot = player.CaptureNormalRewardSave() ?? throw new InvalidOperationException("Paid name requires a complete frozen normal save."); }
                finally { capturing = false; }
            }
            internal QuestManager.NormalSaveSnapshot CaptureQuests(QuestManager manager, string route)
            {
                if (!CanSave || route != Database) throw new InvalidOperationException("Paid name changed its Character freeze.");
                return manager.CaptureNormalSave(route, command, Token);
            }
            internal Action<CharacterContext> CaptureCurrencies(object token)
                => wallet == null ? currencies.CaptureNormalSave(token) : primary ? wallet.CaptureHousingPrimarySave(token) : wallet.CaptureNormalSave(token);
            internal CharacterSaveReconciliation CaptureReceipt() => command.CreateReceipt(primary);
            internal void StageNames(CharacterContext context) { if (primary) command.Stage(context); }

            // Both enrollment transitions execute on the retained session queue.
            // SQL continuations only complete a task; they never apply gameplay
            // or enumerate map/guild recipients.
            internal Task Attempt()
            {
                if (!Owns() || ReadyToPublish) throw new InvalidOperationException("Paid name has no pending original actor save.");
                if (committedEffectsPending)
                {
                    command.ApplyCommittedEffects();
                    committedEffectsPending = false;
                }
                if (snapshot == null) CaptureSnapshot();
                var terminal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Task auth;
                try { auth = player.GetAuthDatabase().Save(context => player.SaveFrozenAuth(context, snapshot)); }
                catch (Exception error) { log.Warn(error, "Paid housing name retains its frozen Character snapshot for Auth retry."); terminal.SetResult(); return terminal.Task; }
                session.Events.EnqueueEvent(new TaskEvent(auth, () =>
                {
                    Task characterSave;
                    try
                    {
                        if (!Owns()) throw new InvalidOperationException("Paid name lost its original actor before Character enrollment.");
                        enrolling = true;
                        try { characterSave = player.SaveCharacterUnit(snapshot); }
                        finally { enrolling = false; }
                    }
                    catch (Exception error) { terminal.TrySetException(error); return; }
                    session.Events.EnqueueEvent(new TaskEvent(characterSave, () =>
                    {
                        try
                        {
                            if (!snapshot.Finish(true)) throw new InvalidOperationException("Paid name did not settle its complete original Character graph.");
                            snapshot = null;
                            if (primary)
                            {
                                wallet?.PrepareHousingOrdinary(); primary = false;
                                committedEffectsPending = true;
                                // Save the adopted wallet and any deferred earning
                                // effects through the same original full actor.
                            }
                            else ReadyToPublish = !(wallet?.PrepareNextOrdinary() == true || currencies.HasPendingPersistenceSave);
                            terminal.TrySetResult();
                        }
                        catch (Exception error) { terminal.TrySetException(error); }
                    }, () =>
                    {
                        try
                        {
                            var proof = characterSave.Exception?.Flatten().InnerExceptions.OfType<CharacterReconciliationRolledBackException>().SingleOrDefault();
                            if (proof == null || !snapshot.RetryHousingRollback(proof))
                                throw (Exception)characterSave.Exception ?? new InvalidOperationException("Paid name has no proven retryable save outcome.");
                            // Keep the exact snapshot and all row owners. The
                            // manager may retry this persistence attempt next tick.
                            terminal.TrySetResult();
                        }
                        catch (Exception error) { terminal.TrySetException(error); }
                    }));
                }, () =>
                {
                    // Character enrollment has not begun. Keep the same frozen
                    // snapshot and retry Auth through its existing row owners.
                    log.Warn(auth.Exception, "Paid housing name retains its frozen Character snapshot for Auth retry.");
                    terminal.TrySetResult();
                }));
                return terminal.Task;
            }
            internal IReadOnlyList<Func<Network.Message.IWritable>> BeginPublication()
            {
                if (!Owns() || !ReadyToPublish || snapshot != null) throw new InvalidOperationException("Paid name has no reconciled publication.");
                publication ??= (inventory as Inventory)?.BeginMarketplacePublication(Owns);
                Publishing = true; return wallet?.CaptureHousingPublication() ?? [];
            }
            internal void RefreshPublicationReadiness()
            {
                if (ReadyToPublish && !Publishing
                    && (wallet?.PrepareNextOrdinary() == true || currencies.HasPendingPersistenceSave))
                    ReadyToPublish = false;
            }
            internal void End()
            {
                if (!Owns() || !ReadyToPublish || snapshot != null) throw new InvalidOperationException("Paid name cannot release unfinished Character ownership.");
                wallet?.FinishHousingPublication(); publication?.Dispose(); frozen.Dispose(); inventoryLease?.Dispose(); ended = true;
                player.housingNameActor = null;
                if (CleanupRequested) player.CleanupCore();
                else if (DisposeRequested) player.Dispose();
                else if (!Retired && session is IPlayerSession original && ReferenceEquals(original.Player, player) && !player.RewardStateRequiresReload)
                    session.CanProcessIncomingPackets = true;
            }
            internal void CancelBeforeAttempt()
            {
                if (snapshot != null) { snapshot.Finish(false); snapshot = null; }
                wallet?.Abort(); frozen?.Dispose(); inventoryLease?.Dispose(); ended = true;
            }
        }
    }
}
