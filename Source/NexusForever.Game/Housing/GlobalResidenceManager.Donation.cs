using NexusForever.Shared;

namespace NexusForever.Game.Housing
{
    public sealed partial class GlobalResidenceManager
    {
        private readonly HashSet<Residence.Donation> donations = [];
        internal void QueueDonation(Residence.Donation command)
        {
            lock (saveGate) donations.Add(command);
        }
        private void SaveDonations()
        {
            // HostedService runs residence updates after all map updates. SQL
            // continuations must not enumerate a map's mutable entity dictionary.
            Residence.Donation[] pending;
            lock (saveGate) pending = donations.ToArray();
            foreach (var command in pending)
            {
                if (command.ReadyToPublish)
                {
                    lock (saveGate)
                    {
                        if (command.Finished || !donations.Contains(command)) continue;
                        if (savesInFlight.Contains(command.DonorId) || savesInFlight.Contains(command.RecipientId)) continue;
                        savesInFlight.Add(command.DonorId); savesInFlight.Add(command.RecipientId);
                    }
                    try
                    {
                        command.PublishOnWorldUpdate();
                        lock (saveGate) donations.Remove(command);
                    }
                    catch (Exception error) { log.Error(error, "Committed donation publication remains owned for the next world update."); }
                    finally
                    {
                        lock (saveGate) { savesInFlight.Remove(command.DonorId); savesInFlight.Remove(command.RecipientId); }
                    }
                    continue;
                }
                lock (saveGate)
                {
                    if (command.Finished || command.ReadyToPublish || !donations.Contains(command)) continue;
                    if (savesInFlight.Contains(command.DonorId) || savesInFlight.Contains(command.RecipientId)) continue;
                    savesInFlight.Add(command.DonorId); savesInFlight.Add(command.RecipientId);
                }
                SaveDonation(command).FireAndForgetAsync(error => log.Error(error, "Donation remains owned for retry."));
            }
        }
        private async Task SaveDonation(Residence.Donation command)
        {
            try { await command.Run(GetCharacterDatabase().CreateContext); }
            finally
            {
                lock (saveGate)
                {
                    savesInFlight.Remove(command.DonorId); savesInFlight.Remove(command.RecipientId);
                    if (command.Finished) donations.Remove(command);
                }
            }
        }
    }
}
