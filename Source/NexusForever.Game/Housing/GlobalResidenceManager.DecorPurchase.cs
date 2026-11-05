namespace NexusForever.Game.Housing
{
    public sealed partial class GlobalResidenceManager
    {
        private readonly HashSet<Residence.DecorPurchase> decorPurchases = [];
        private readonly HashSet<ulong> decorPurchaseAttempts = [];
        internal void QueueDecorPurchase(Residence.DecorPurchase command)
        { lock (saveGate) { decorPurchases.Add(command); timeToSave = 0; } }
        internal void ObserveDecorPurchaseAttempt(Residence.DecorPurchase command, Task attempt)
        {
            lock (saveGate) { savesInFlight.Add(command.Id); decorPurchaseAttempts.Add(command.Id); }
            _ = attempt.ContinueWith(_ => { lock (saveGate) { decorPurchaseAttempts.Remove(command.Id); savesInFlight.Remove(command.Id); } }, TaskScheduler.Default);
        }
        private void UpdateDecorPurchases()
        {
            Residence.DecorPurchase[] pending;
            lock (saveGate) pending = decorPurchases.ToArray();
            foreach (var command in pending)
            {
                lock (saveGate) if (command.Finished || !decorPurchases.Contains(command) || !savesInFlight.Add(command.Id)) continue;
                try { command.Advance(); if (command.Finished) lock (saveGate) decorPurchases.Remove(command); }
                catch (Exception error) { log.Error(error, "Housing decor purchase remains retained for world-update retry."); }
                finally { lock (saveGate) if (!decorPurchaseAttempts.Contains(command.Id)) savesInFlight.Remove(command.Id); }
            }
        }
    }
}
