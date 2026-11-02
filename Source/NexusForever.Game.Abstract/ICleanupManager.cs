using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Abstract
{
    public interface ICleanupManager
    {
        /// <summary>
        /// Start tracking supplied <see cref="IPlayer"/> for pending cleanup.
        /// </summary>
        void AddPlayer(IPlayer player);

        /// <summary>
        /// Stops tracking supplied <see cref="IPlayer"/> for pending cleanup.
        /// </summary>
        void RemovePlayer(IPlayer player);

        /// <summary>
        /// Returns if supplied <see cref="IAccount"/> is locked due to pending <see cref="IPlayer"/> cleanup.
        /// </summary>
        bool IsAccountLocked(IAccount account);

        /// <summary>Hold admission for all accounts participating in one economic operation.</summary>
        bool TryAcquireAccounts(IReadOnlyCollection<uint> accounts, out IDisposable lease);
    }
}
