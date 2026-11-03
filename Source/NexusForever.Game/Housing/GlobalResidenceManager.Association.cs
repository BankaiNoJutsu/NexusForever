using NexusForever.Shared;

namespace NexusForever.Game.Housing
{
    public sealed partial class GlobalResidenceManager
    {
        private readonly HashSet<Residence.Association> associations = [];
        internal void QueueAssociation(Residence.Association command) { lock (saveGate) associations.Add(command); }
        private void SaveAssociations()
        {
            Residence.Association[] pending; lock (saveGate) pending = associations.ToArray();
            foreach (var command in pending)
            {
                lock (saveGate)
                {
                    if (command.Finished || !associations.Contains(command) || savesInFlight.Contains(command.ChildId) || savesInFlight.Contains(command.ParentId)) continue;
                    savesInFlight.Add(command.ChildId); savesInFlight.Add(command.ParentId);
                }
                if (command.ReadyToPublish)
                {
                    try { command.PublishOnWorldUpdate(); }
                    catch (Exception error) { log.Error(error, "Committed association publication remains owned for retry."); }
                    finally { CompleteAssociationTick(command); }
                }
                else SaveAssociation(command).FireAndForgetAsync(error => log.Error(error, "Association remains owned for retry."));
            }
        }
        private async Task SaveAssociation(Residence.Association command)
        {
            try { await command.Run(GetCharacterDatabase().CreateContext); }
            finally { CompleteAssociationTick(command); }
        }
        private void CompleteAssociationTick(Residence.Association command)
        {
            lock (saveGate)
            {
                savesInFlight.Remove(command.ChildId); savesInFlight.Remove(command.ParentId);
                if (command.Finished) associations.Remove(command);
            }
        }
    }
}
