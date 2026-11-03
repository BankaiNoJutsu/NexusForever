using NexusForever.Shared;

namespace NexusForever.Game.Housing
{
    public sealed partial class GlobalResidenceManager
    {
        private readonly HashSet<Residence.Remodel> remodels = [];
        internal void QueueRemodel(Residence.Remodel command) { lock (saveGate) remodels.Add(command); }
        private void SaveRemodels()
        {
            Residence.Remodel[] pending;
            lock (saveGate) pending = remodels.ToArray();
            foreach (var command in pending)
            {
                lock (saveGate)
                {
                    if (command.Finished || !remodels.Contains(command) || !savesInFlight.Add(command.Id)) continue;
                }
                if (command.ReadyToPublish)
                {
                    try { command.PublishOnWorldUpdate(); }
                    catch (Exception error) { log.Error(error, "Committed remodel publication remains owned for retry."); }
                    finally
                    {
                        lock (saveGate) { savesInFlight.Remove(command.Id); if (command.Finished) remodels.Remove(command); }
                    }
                }
                else SaveRemodel(command).FireAndForgetAsync(error => log.Error(error, "Remodel remains owned for retry."));
            }
        }
        private async Task SaveRemodel(Residence.Remodel command)
        {
            try { if (command.Prepare()) await GetCharacterDatabase().Save(command.Stage); }
            finally { lock (saveGate) { savesInFlight.Remove(command.Id); if (command.Finished) remodels.Remove(command); } }
        }
    }
}
