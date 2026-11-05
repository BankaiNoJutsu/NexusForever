using NexusForever.Database.Auth;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Static.Account;

namespace NexusForever.Game.Account.Currency
{
    public partial class AccountCurrencyManager
    {
        private readonly object currencyGate = new();
        private readonly Dictionary<string, CosmicMutation> cosmicMutations = [];
        private CurrencyMutationScope currentScope;
        private ulong committedCosmicWallet;
        private ulong committedCosmicRevision;

        internal sealed record CosmicMutation(string Key, ulong Amount, bool Subtract, CurrencyMutationScope Scope,
            ulong? SourceInventoryId = null, string SourceCreationKey = null, bool SourceMayBeUnpersisted = false);

        private void RecordCosmicMutation(ulong amount, bool subtract)
        {
            if (amount == 0 || account.LoyaltyManager == null)
                return;
            lock (currencyGate)
            {
                string key = currentScope?.NextSourceOperationKey() ?? $"cosmic:{Guid.NewGuid():N}";
                CurrencyMutationScope source = currentScope?.SourceOwner;
                cosmicMutations.Add(key, new CosmicMutation(key, amount, subtract, currentScope,
                    source?.SourceInventoryId, source?.SourceCreationKey, source?.SourceMayBeUnpersisted ?? false));
            }
        }

        internal CosmicMutation[] CaptureCosmicMutations(AuthContext context)
        {
            lock (currencyGate)
            {
                if (currencies.GetValueOrDefault(AccountCurrencyType.CosmicReward) is AccountCurrency { IsPersistenceBusy: true })
                {
                    context.RejectSaveUnit();
                    throw new InvalidOperationException("An authoritative vendor observation still owns the Cosmic wallet.");
                }
                return cosmicMutations.Values.Where(operation => operation.Scope == null || operation.Scope.Contexts.Contains(context)).ToArray();
            }
        }

        internal CosmicMutation[] CurrentCapturedMutations(AuthContext context, IReadOnlyList<CosmicMutation> captured)
        {
            lock (currencyGate)
                return captured.Select(operation => cosmicMutations.GetValueOrDefault(operation.Key)).Where(operation => operation != null
                    && (operation.Scope == null || operation.Scope.Contexts.Contains(context))).ToArray();
        }

        internal void CompleteCosmicMutations(IReadOnlyList<CosmicMutation> captured, ulong wallet, ulong revision, bool publish = true)
        {
            lock (currencyGate)
            {
                foreach (CosmicMutation operation in captured)
                {
                    cosmicMutations.Remove(operation.Key);
                    if (operation.Scope != null)
                        operation.Scope.Persisted = true;
                }
                if (revision >= committedCosmicRevision)
                {
                    committedCosmicRevision = revision;
                    committedCosmicWallet = wallet;
                }
                ReconcileCosmicWallet(publish);
            }
        }

        private void ReconcileCosmicWallet(bool publish = true)
        {
            ulong visible = committedCosmicWallet;
            foreach (CosmicMutation operation in cosmicMutations.Values)
                visible = operation.Subtract
                    ? (operation.Amount > visible ? 0 : visible - operation.Amount)
                    : checked(visible + operation.Amount);
            if (!currencies.TryGetValue(AccountCurrencyType.CosmicReward, out IAccountCurrency currency))
            {
                if (visible == 0) return;
                currency = CreateAccountCurrency(AccountCurrencyType.CosmicReward);
            }
            bool changed = currency.Amount != visible;
            ((AccountCurrency)currency).SetJournalAmount(visible);
            if (changed && publish) SendAccountCurrencyUpdate(currency);
        }

        internal void PublishCraftingCosmic(Func<bool> owns)
        {
            lock (currencyGate)
            {
                var session = account.Session;
                if (!owns() || !currencies.TryGetValue(AccountCurrencyType.CosmicReward, out var wallet)) return;
                SendCurrencyGrant(wallet, 0, session);
                if (owns()) session.EnqueueMessageEncrypted(new Network.World.Message.Model.ServerWalletUpdate((uint)AccountCurrencyType.CosmicReward));
            }
        }

        public IAccountCurrencyMutationScope BeginMutationScope() => BeginScope(null, null, null, false);

        public IAccountCurrencyMutationScope BeginSourceConsumption(string sourceKey, ulong inventoryId, bool mayBeUnpersisted = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
            string digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sourceKey)))[..32];
            return BeginScope(digest, sourceKey, inventoryId, mayBeUnpersisted);
        }

        private IAccountCurrencyMutationScope BeginScope(string source, string sourceKey, ulong? inventoryId, bool mayBeUnpersisted)
        {
            if (account.LoyaltyManager == null) return LegacyMutationScope.Instance;
            lock (currencyGate)
            {
                var scope = new CurrencyMutationScope(this, currentScope, source, sourceKey, inventoryId, mayBeUnpersisted);
                currentScope = scope;
                return scope;
            }
        }

        private sealed class LegacyMutationScope : IAccountCurrencyMutationScope
        {
            internal static LegacyMutationScope Instance { get; } = new();
            public void Enlist(AuthContext context) { }
            public void Complete() { }
            public void Dispose() { }
        }

        internal sealed class CurrencyMutationScope(AccountCurrencyManager owner, CurrencyMutationScope parent, string source,
            string sourceKey, ulong? inventoryId, bool mayBeUnpersisted) : IAccountCurrencyMutationScope
        {
            internal HashSet<AuthContext> Contexts { get; } = [];
            internal bool Persisted { get; set; }
            private bool complete;
            private bool disposed;
            private ulong sourceOrdinal;
            internal CurrencyMutationScope SourceOwner => source != null ? this : parent?.SourceOwner;
            internal ulong? SourceInventoryId => inventoryId;
            internal string SourceCreationKey => sourceKey;
            internal bool SourceMayBeUnpersisted => mayBeUnpersisted;

            internal string NextSourceOperationKey() => source != null
                ? $"source:{source}:{sourceOrdinal++}"
                : parent?.NextSourceOperationKey();

            internal bool CanCommit()
            {
                lock (owner.currencyGate) return (complete || !disposed) && (parent?.CanCommit() ?? true);
            }

            public void Enlist(AuthContext context)
            {
                ArgumentNullException.ThrowIfNull(context);
                lock (owner.currencyGate) Contexts.Add(context);
            }

            public void Complete()
            {
                lock (owner.currencyGate)
                {
                    complete = true;
                    foreach (CosmicMutation operation in owner.cosmicMutations.Values.Where(operation => ReferenceEquals(operation.Scope, this)).ToArray())
                        owner.cosmicMutations[operation.Key] = operation with { Scope = parent };
                }
            }

            public void Dispose()
            {
                lock (owner.currencyGate)
                {
                    if (disposed) return;
                    if (!ReferenceEquals(owner.currentScope, this))
                        throw new InvalidOperationException("Currency mutation scopes must finish in nesting order.");
                    disposed = true;
                    owner.currentScope = parent;
                    if (!complete)
                    {
                        foreach (CosmicMutation operation in owner.cosmicMutations.Values.Where(operation => ReferenceEquals(operation.Scope, this)).ToArray())
                            owner.cosmicMutations.Remove(operation.Key);
                        owner.ReconcileCosmicWallet();
                    }
                    Contexts.Clear();
                }
            }
        }
    }
}
