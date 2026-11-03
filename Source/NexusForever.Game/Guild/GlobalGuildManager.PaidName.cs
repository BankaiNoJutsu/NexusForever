namespace NexusForever.Game.Guild
{
    public sealed partial class GlobalGuildManager
    {
        private readonly HashSet<CommunityRenameSettlement> paidNames = [];
        internal void QueuePaidRename(CommunityRenameSettlement command) { lock (cacheLock) paidNames.Add(command); }
        internal void ObservePaidNameAttempt(CommunityRenameSettlement command, Task attempt)
        {
            lock (cacheLock) { guildSavesInFlight.Add(command.GuildId); paidNameAttempts.Add(command.GuildId); }
            _ = attempt.ContinueWith(_ => { lock (cacheLock) { paidNameAttempts.Remove(command.GuildId); guildSavesInFlight.Remove(command.GuildId); } }, TaskScheduler.Default);
        }
        private void UpdatePaidNames()
        {
            CommunityRenameSettlement[] pending;
            lock (cacheLock) pending = paidNames.ToArray();
            foreach (var command in pending)
            {
                lock (cacheLock)
                {
                    if (!paidNames.Contains(command) || command.Finished || guildSavesInFlight.Contains(command.GuildId)) continue;
                    guildSavesInFlight.Add(command.GuildId);
                }
                try { command.Advance(); if (command.Finished) lock (cacheLock) paidNames.Remove(command); }
                catch (Exception error) { log.Error(error, "Paid community name remains owned for world-update retry."); }
                finally
                {
                    // A pending asynchronous attempt retains its own in-flight
                    // registration; its continuation releases it after SQL.
                    lock (cacheLock) if (!paidNameAttempts.Contains(command.GuildId)) guildSavesInFlight.Remove(command.GuildId);
                }
            }
        }
        private readonly HashSet<ulong> paidNameAttempts = [];
    }
}
