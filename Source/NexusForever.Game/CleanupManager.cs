using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NLog;

namespace NexusForever.Game
{
    public sealed class CleanupManager : ICleanupManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<ulong, IPlayer> pending = new();
        private readonly HashSet<uint> lockedAccounts = new();
        private readonly object gate = new();
        private readonly Dictionary<uint, object> operations = new();

        /// <summary>
        /// Start tracking supplied <see cref="IPlayer"/> for pending cleanup.
        /// </summary>
        public void AddPlayer(IPlayer player)
        {
            lock (gate)
            {
                pending.Add(player.CharacterId, player);
                lockedAccounts.Add(player.Account.Id);
            }

            log.Trace($"Added player {player.CharacterId}.");
        }

        /// <summary>
        /// Stops tracking supplied <see cref="IPlayer"/> for pending cleanup.
        /// </summary>
        public void RemovePlayer(IPlayer player)
        {
            lock (gate)
            {
                if (!pending.TryGetValue(player.CharacterId, out var original) || !ReferenceEquals(original, player)) return;
                pending.Remove(player.CharacterId);
                if (!pending.Values.Any(other => other.Account.Id == player.Account.Id)) lockedAccounts.Remove(player.Account.Id);
            }

            log.Trace($"Removed player {player.CharacterId}.");
        }

        /// <summary>
        /// Returns if supplied <see cref="IAccount"/> is locked due to pending <see cref="IPlayer"/> cleanup.
        /// </summary>
        public bool IsAccountLocked(IAccount account)
        {
            lock (gate) return lockedAccounts.Contains(account.Id) || operations.ContainsKey(account.Id);
        }

        public bool TryAcquireAccounts(IReadOnlyCollection<uint> accounts, out IDisposable lease)
        {
            uint[] ids = accounts.Distinct().Order().ToArray();
            lock (gate)
            {
                lease = null;
                if (ids.Length == 0 || ids.Any(id => id == 0 || lockedAccounts.Contains(id) || operations.ContainsKey(id))) return false;
                var owner = new object();
                foreach (uint id in ids) operations.Add(id, owner);
                lease = new AccountLease(this, ids, owner); return true;
            }
        }

        private sealed class AccountLease(CleanupManager manager, uint[] ids, object owner) : IDisposable
        {
            public void Dispose()
            {
                lock (manager.gate)
                    foreach (uint id in ids)
                        if (manager.operations.TryGetValue(id, out var current) && ReferenceEquals(current, owner)) manager.operations.Remove(id);
            }
        }
    }
}
