using System.Data;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Quest.Reward
{
    // The existing Character queue holds normal saves and subsequent claims
    // until account delivery, wallet publication and a fresh owned Load settle.
    // Workers only read copied identities/catalogs and the two context factories.
    internal sealed class ContractAccountSync : IDisposable
    {
        private readonly IPlayer player;
        private readonly IAccount account;
        private readonly IGameSession session;
        private readonly object challenges;
        private readonly QuestManager quests;
        private readonly AccountCurrencyManager wallets;
        private readonly uint accountId;
        private readonly ulong characterId, epoch;
        private readonly ushort realm;
        private readonly Func<CharacterContext> characters;
        private readonly Func<AuthContext> auth;
        private readonly ContractRewardAwardCatalog catalog;
        private readonly string[] observed;
        private readonly bool probe;
        private string characterDatabase, authDatabase;
        private EventQueue events;
        private Action ready, failed;
        private ContractAccountIntent[][] groups;
        private AccountCurrencyManager.WalletRefresh refresh;
        private int group, waits, phase; //0 new,1 listing,2 delivering,3 ready,4 retired

        internal ContractAccountSync(IPlayer player, Func<CharacterContext> characters, Func<AuthContext> auth, ContractRewardAwardCatalog catalog,
            bool probe = false, IEnumerable<string> observed = null)
        {
            this.player = player; this.characters = characters; this.auth = auth; this.catalog = catalog;
            this.probe = probe; this.observed = observed?.Distinct(StringComparer.Ordinal).ToArray() ?? [];
            account = player.Account; session = player.Session; challenges = player.ChallengeManager;
            quests = player.QuestManager as QuestManager; wallets = account?.CurrencyManager as AccountCurrencyManager;
            accountId = account?.Id ?? 0; characterId = player.CharacterId; epoch = account?.WorldSessionEpoch ?? 0; realm = player.Identity.RealmId;
        }
        private bool Live() => phase != 4 && accountId != 0 && realm != 0 && characterId != 0 && session != null && wallets != null && quests != null
            && player.CharacterId == characterId && player.Identity.RealmId == realm && ReferenceEquals(player.Account, account)
            && account.Id == accountId && account.WorldSessionEpoch == epoch && ReferenceEquals(account.CurrencyManager, wallets)
            && ReferenceEquals(account.Session, session) && ReferenceEquals(player.Session, session)
            && (events == null || ReferenceEquals(session.Events, events))
            && (session is not IPlayerSession bound || ReferenceEquals(bound.Player, player))
            && ReferenceEquals(player.ChallengeManager, challenges) && ReferenceEquals(player.QuestManager, quests) && !quests.RequiresAuthoritativeReload
            && session is not NetworkSession { IsDisconnecting: true } && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);

        internal CharacterChallengeRewardCommand LoadCommand() => CanPublish()
            ? CharacterChallengeRewardCommand.LoadForAccount(characterId, accountId)
            : throw new InvalidOperationException("Contract account synchronization lost its completed actor.");

        internal void Start(string database, EventQueue queue, Action completed, Action onFailure)
        {
            if (phase != 0 || !Live() || queue == null || !ReferenceEquals(session.Events, queue) || string.IsNullOrEmpty(database))
                throw new InvalidOperationException("Contract account synchronization has no original queue owner.");
            characterDatabase = database; events = queue; ready = completed; failed = onFailure; phase = 1;
            events.EnqueueEvent(new TaskGenericEvent<ContractAccountIntent[][]>(Task.Run(ReadPlan), plan =>
            {
                if (phase == 4) return;
                if (!Live()) { Fail(); return; }
                groups = plan; phase = 2; StartGroup();
            }, Fail));
        }

        private CharacterContext Character()
        {
            var context = characters() ?? throw new InvalidOperationException("Missing Contract source context.");
            try
            {
                if (Item.GetRewardDatabaseKey(context) != characterDatabase || context.Database.CurrentTransaction != null
                    || context.ChangeTracker.Entries().Any() || context.IsSaveRejected || context.HasSavepointRollback)
                    throw new InvalidOperationException("Contract synchronization changed its source database or context owner.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }
        private AuthContext Auth()
        {
            var context = auth() ?? throw new InvalidOperationException("Missing Contract Auth context.");
            try
            {
                string key = AccountCurrencyOperationRepository.DatabaseKey(context);
                string expected = Interlocked.CompareExchange(ref authDatabase, key, null) ?? key;
                if (key != expected || context.Database.CurrentTransaction != null || context.ChangeTracker.Entries().Any())
                    throw new InvalidOperationException("Contract synchronization changed its Auth database or context owner.");
                return context;
            }
            catch { context.Dispose(); throw; }
        }
        private async Task<ContractAccountIntent[][]> ReadPlan()
        {
            // Bind Auth identity before delivery or wallet observation can run.
            await using (var identity = Auth()) { }
            await using var strategy = Character();
            return await strategy.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var context = Character(); await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var query = context.CharacterContractAccountOutbox.AsNoTracking().Where(row => row.AccountId == accountId && row.RealmId == realm);
                if (observed.Length != 0) query = query.Where(row => !EF.Constant(observed).Contains(row.ClaimId));
                var rows = await query.OrderBy(row => row.ClaimId).ToArrayAsync();
                var intents = new List<ContractAccountIntent>();
                foreach (var row in rows)
                {
                    var intent = ContractAccountIntent.Restore(catalog, row.PayloadJson);
                    var quote = ContractRewardClaimQuote.Restore(catalog, intent.QuoteJson);
                    if (intent.AccountId != accountId || intent.RealmId != realm || row.ClaimId != intent.ClaimId || row.CharacterId != intent.CharacterId
                        || row.TrackId != quote.Award.TrackId || row.CycleId != quote.CycleId || row.Ordinal != quote.Award.Ordinal)
                        throw new InvalidOperationException("Contract drain list lost its canonical original account/source.");
                    intents.Add(intent);
                }
                await tx.CommitAsync();
                // Include acknowledged intents: Auth may have committed after
                // the login model was read but before its source ACK became visible.
                return intents.GroupBy(intent => ContractRewardClaimQuote.Restore(catalog, intent.QuoteJson).Award.AccountCurrency.Type)
                    .OrderBy(values => values.Key).Select(values => values.ToArray()).ToArray();
            });
        }
        private void StartGroup()
        {
            if (phase == 4) return;
            try
            {
                if (!Live()) throw new InvalidOperationException("Contract drain lost its live actor.");
                if (group == groups.Length) { phase = 3; ready(); return; }
                var sources = groups[group]; var quote = ContractRewardClaimQuote.Restore(catalog, sources[^1].QuoteJson);
                var award = quote.Award.AccountCurrency;
                if (wallets.IsWalletRefreshBusy(award.Type))
                {
                    if (++waits > 100) throw new InvalidOperationException("Contract wallet save did not settle.");
                    events.EnqueueEvent(new DelayEvent(TimeSpan.FromMilliseconds(50), StartGroup)); return;
                }
                waits = 0;
                var required = new AccountWalletOperation(sources[^1].OperationKey, accountId, award.Type, award.Amount, false, sources[^1].Json);
                var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                refresh = wallets.BeginWalletRefresh(award.Type, Auth, required, barrier.Task);
                var delivery = new ContractAccountDelivery(Character, Auth, catalog, realm);
                // No actor callback is passed into the SQL continuation.
                _ = Deliver(delivery, sources, barrier);
                events.EnqueueEvent(new TaskEvent(refresh.Completion, () =>
                {
                    if (phase == 4) return;
                    if (!Live() || !refresh.TryPublish(Live)) { Fail(); return; }
                    refresh.Dispose(); refresh = null; ++group; StartGroup();
                }, Fail));
            }
            catch { Fail(); }
        }
        private static async Task Deliver(ContractAccountDelivery delivery, ContractAccountIntent[] sources, TaskCompletionSource barrier)
        {
            try
            {
                foreach (var source in sources)
                {
                    ContractAccountDeliveryResult result = null;
                    for (int attempt = 0; attempt < 3; ++attempt)
                    {
                        result = await delivery.DeliverAsync(source.ClaimId);
                        if (result.Outcome is ContractAccountDeliveryOutcome.Delivered or ContractAccountDeliveryOutcome.Conflict) break;
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                    if (result.Outcome != ContractAccountDeliveryOutcome.Delivered)
                        throw new InvalidOperationException("Contract account delivery has no exact durable acknowledgement.", result.Error);
                }
                barrier.TrySetResult();
            }
            catch (Exception error) { barrier.TrySetException(error); }
        }
        internal bool CanPublish() => phase == 3 && Live();
        internal bool SkipLoad => CanPublish() && probe && groups.Length == 0;
        internal IEnumerable<string> ObservedClaims => groups.SelectMany(values => values).Select(value => value.ClaimId);
        internal bool CanAdopt(CharacterChallengeRewardExecution result)
        {
            if (!CanPublish() || result?.State?.ContractTracks == null) return false;
            foreach (var source in groups.SelectMany(values => values).Where(value => value.CharacterId == characterId))
            {
                var quote = ContractRewardClaimQuote.Restore(catalog, source.QuoteJson);
                var track = result.State.ContractTracks.SingleOrDefault(value => value.TrackId == quote.Award.TrackId);
                if (track?.CycleId != quote.CycleId) continue; // retained historical/deleted-source intents still pay their original account
                var claim = track.Slots.SingleOrDefault(value => value.Ordinal == quote.Award.Ordinal)?.Claim;
                if (claim?.ClaimId != source.ClaimId || !claim.AccountDelivered || claim.DeliveryJson != source.Json) return false;
            }
            return true;
        }
        private void Fail()
        {
            if (phase == 4) return;
            Dispose(); failed?.Invoke();
        }
        public void Dispose()
        {
            phase = 4; refresh?.Dispose(); refresh = null;
        }
    }
}
