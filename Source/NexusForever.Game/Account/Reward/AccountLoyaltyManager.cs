using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Inventory;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reward;

namespace NexusForever.Game.Account.Reward
{
    public sealed class AccountLoyaltyManager : IAccountLoyaltyManager
    {
        private readonly object gate = new();
        private readonly HashSet<AuthContext> enlisted = [];
        private readonly IAccount account;
        private readonly AuthDatabase database;
        private readonly IPrerequisiteManager prerequisites;
        private readonly LoyaltyCatalog catalog;
        private LoyaltyState state;
        private IPlayer actor;
        private bool initialPacketsSent;

        public AccountLoyaltyManager(IAccount account, IGameTableManager tables, AuthDatabase database, IPrerequisiteManager prerequisites = null)
        {
            this.account = account ?? throw new ArgumentNullException(nameof(account));
            this.database = database;
            this.prerequisites = prerequisites;
            catalog = new LoyaltyCatalog(tables);
        }

        public void Save(AuthContext context) => Enlist(context, null, null, null);

        private void Enlist(AuthContext context, uint? choose, (uint Row, uint Choice)? claim, Action<bool> requestCompleted)
        {
            lock (gate)
            {
                if (!enlisted.Add(context)) return;
                var currency = account.CurrencyManager as AccountCurrencyManager
                    ?? throw new InvalidOperationException("Loyalty requires the transactional account currency owner.");
                var mutations = currency.CaptureCosmicMutations(context);
                AccountCurrencyManager.CosmicMutation[] appliedMutations = [];
                IPlayer currentActor = actor;
                bool Meets(uint id) => id == 0 || currentActor != null && prerequisites?.Meets(currentActor, id) == true;
                var staged = new List<object>();
                LoyaltyState result = null;
                bool accepted = false;
                try
                {
                    context.EnlistSaveWork(unit =>
                    {
                        appliedMutations = currency.CurrentCapturedMutations(unit, mutations);
                        result = LoyaltyRepository.Stage(unit, account.Id, catalog, appliedMutations, Meets, choose, claim, staged, out accepted);
                    },
                        committed =>
                        {
                            lock (gate)
                            {
                                enlisted.Remove(context);
                                foreach (object entity in staged)
                                {
                                    try { context.Entry(entity).State = EntityState.Detached; }
                                    catch (ObjectDisposedException) { }
                                }
                                if (committed && result != null)
                                {
                                    currency.CompleteCosmicMutations(appliedMutations, result.Wallet, result.Revision);
                                    if (state == null || result.Revision >= state.Revision)
                                    {
                                        bool changed = state == null || state.EarnedPoints != result.EarnedPoints || state.ActiveTrackId != result.ActiveTrackId
                                            || result.Slots.Any(row => state.Slots.SingleOrDefault(old => old.TrackId == row.TrackId && old.Position == row.Position)?.Claimed != row.Claimed);
                                        state = result;
                                        if (account.InventoryManager is AccountInventoryManager inventory)
                                            inventory.AdoptCommittedPendingItems(result.PendingRows);
                                        if (initialPacketsSent && changed) SendUpdates();
                                    }
                                }
                                requestCompleted?.Invoke(committed && accepted);
                            }
                        }, () => appliedMutations.All(operation => operation.Scope?.CanCommit() != false));
                }
                catch
                {
                    enlisted.Remove(context);
                    throw;
                }
            }
        }

        public void SendInitialPackets(IPlayer player)
        {
            if (player == null || player.Account?.Id != account.Id) return;
            actor = player;
            Flush(null, null);
            lock (gate)
            {
                SendReload();
                initialPacketsSent = true;
            }
        }

        public bool ChooseTrack(IPlayer player, uint trackId)
        {
            if (player == null || player.Account?.Id != account.Id) return false;
            actor = player;
            bool accepted = Flush(trackId, null);
            lock (gate) SendReload();
            return accepted;
        }

        public bool ClaimReward(IPlayer player, uint rewardRowId, uint choice)
        {
            if (player == null || player.Account?.Id != account.Id) return false;
            actor = player;
            bool accepted = Flush(null, (rewardRowId, choice));
            lock (gate) SendReload();
            return accepted;
        }

        private bool Flush(uint? choose, (uint Row, uint Choice)? claim)
        {
            if (database == null) throw new InvalidOperationException("Loyalty requires durable Auth persistence.");
            bool accepted = false;
            database.SaveBlocking(context =>
            {
                Enlist(context, choose, claim, value => accepted = value);
                account.Save(context);
            });
            return accepted;
        }

        private RewardTrack BuildTrack(LoyaltyTrackDefinition definition)
        {
            var bits = new NetworkBitArray(32, NetworkBitArray.BitOrder.LeastSignificantBit);
            foreach (var row in state.Slots.Where(row => row.TrackId == definition.Id && row.Claimed)) bits.SetBit(row.Position, true);
            uint points = LoyaltyRepository.Points(state.EarnedPoints, definition);
            return new RewardTrack
            {
                RewardTrackId = definition.Id, RewardPointsEarned = points, ClaimedRewards = bits,
                Unlocked = points == definition.MaximumPoints ? 1u : 0u,
                RewardTrackRewardsIds = state.Slots.Where(row => row.TrackId == definition.Id).OrderBy(row => row.Position).Select(row => row.RewardRowId).ToList(),
                IsActive = definition.Id == state.ActiveTrackId
            };
        }

        private void SendReload()
        {
            if (state != null)
            {
                List<RewardTrack> rows = catalog.Tracks.Select(BuildTrack).ToList();
                if (actor?.RewardTrackPresentation is { } presentation)
                    presentation.PublishAccountTracks(rows);
                else
                    account.Session.EnqueueMessageEncrypted(new ServerRewardTracksReload { RewardsTracks = rows });
            }
        }

        private void SendUpdates()
        {
            IPlayer currentActor = actor;
            IGameSession currentSession = account.Session;
            LoyaltyState currentState = state;
            var presentation = currentActor?.RewardTrackPresentation;
            if (presentation != null && !presentation.PublishAccountTracks(catalog.Tracks.Select(BuildTrack).ToList(), reload: false))
                return;
            foreach (LoyaltyTrackDefinition definition in catalog.Tracks)
            {
                if (!ReferenceEquals(currentState, state) || !ReferenceEquals(currentActor, actor)
                    || !ReferenceEquals(currentSession, account.Session)
                    || currentSession is NetworkSession { IsDisconnecting: true }
                    || presentation?.IsCurrent == false)
                    return;
                RewardTrack row = BuildTrack(definition);
                currentSession.EnqueueMessageEncrypted(new ServerRewardTrackUpdate
                {
                    RewardTrackId = row.RewardTrackId, RewardPointsEarned = row.RewardPointsEarned,
                    ClaimedRewards = row.ClaimedRewards, Unlocked = row.Unlocked, IsActive = row.IsActive
                });
            }
        }
    }
}
