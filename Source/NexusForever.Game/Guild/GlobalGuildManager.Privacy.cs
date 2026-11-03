using NexusForever.Database.Character;
using NexusForever.Shared;

namespace NexusForever.Game.Guild
{
    public sealed partial class GlobalGuildManager
    {
        private readonly HashSet<Community.PrivacyChange> privacyChanges = [];
        internal void QueuePrivacyChange(Community.PrivacyChange change) { lock (cacheLock) privacyChanges.Add(change); }
        private void UpdatePrivacyChanges()
        {
            Community.PrivacyChange[] snapshot;
            lock (cacheLock) snapshot = privacyChanges.ToArray();
            foreach (var change in snapshot)
            {
                lock (cacheLock)
                {
                    if (!privacyChanges.Contains(change) || change.Finished || !guildSavesInFlight.Add(change.Owner.Id)) continue;
                }
                bool saving = false;
                try
                {
                    if (change.Advance())
                    {
                        CharacterDatabase database = databaseManager?.GetDatabase<CharacterDatabase>()
                            ?? throw new InvalidOperationException("Community privacy requires its original guild save route.");
                        Task task = SaveGuildAsync(database, change.Owner, change.Owner.PendingDelete);
                        saving = true; task.FireAndForgetAsync(error => log.Error(error, "Community privacy retains its original guild graph for retry."));
                    }
                    if (change.Finished) lock (cacheLock) privacyChanges.Remove(change);
                }
                catch (Exception error) { log.Error(error, "Community privacy remains owned for world-update retry."); }
                finally { if (!saving) lock (cacheLock) guildSavesInFlight.Remove(change.Owner.Id); }
            }
        }
    }
}
