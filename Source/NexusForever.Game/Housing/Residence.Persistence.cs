using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Housing;

namespace NexusForever.Game.Housing
{
    public partial class Residence
    {
        internal object MutationGate => saveOwnershipLock;
        private ResidencePersistence persistence;
        private ResidencePersistence.Snapshot loadedPersistence;
        private string loadedDatabase;
        private ulong? boundCommunity;
        private object retiringCommunityOwner;
        internal void RetainCommunityRetirement(Abstract.Guild.ICommunity community)
        {
            lock (MutationGate)
            {
                if (persistence == null) return;
                if (boundCommunity != community.Id || !ReferenceEquals(community.Residence, this) || communitySaveOwner == null)
                    throw new InvalidOperationException("Community retirement lost its original residence reservation.");
                retiringCommunityOwner = communitySaveOwner;
            }
        }
        internal bool OwnsCommunityRetirement(object reservation) => reservation != null
            && ReferenceEquals(reservation, retiringCommunityOwner) && ReferenceEquals(reservation, communitySaveOwner);
        internal bool HasPersistence => persistence != null;
        internal void RequirePersistenceEditable()
        {
            if (paidCreation != null) throw new InvalidOperationException("Paid Community creation has frozen its residence graph.");
            persistence?.RequireEditable();
        }
        internal void RequirePlotEditable(Plot plot)
        {
            if (persistence == null) return;
            RequirePersistenceEditable();
            if (!plots.Any(row => ReferenceEquals(row, plot))) throw new InvalidOperationException("Plot lost its original residence collection.");
        }
        internal void RequireDecorEditable(Decor decor)
        {
            if (persistence == null) return;
            RequirePersistenceEditable();
            if (!decors.TryGetValue(decor.DecorId, out IDecor row) || !ReferenceEquals(row, decor))
                throw new InvalidOperationException("Decor lost its original residence collection.");
            if (donation != null && donation.HoldsDecor(this, decor.DecorId))
                throw new InvalidOperationException("Decor belongs to an unfinished donation.");
        }
        internal void BindPersistence(CharacterContext context, Func<bool> registered)
        {
            lock (MutationGate)
            {
                if (persistence != null) throw new InvalidOperationException("Residence already has its registered save owner.");
                string database = loadedPersistence == null ? CharacterDatabaseIdentity.Get(context)
                    : loadedDatabase ?? throw new InvalidOperationException("Registered residence lost its actual load origin.");
                boundCommunity = Type == Static.Housing.ResidenceType.Community ? GuildOwnerId : null;
                persistence = new(this, database, loadedPersistence, CapturePersistence, AcknowledgePersistence, registered);
                foreach (Plot plot in plots.OfType<Plot>()) plot.BindSaveOwner(this);
            }
        }
        private ResidencePersistence.Header CaptureHeader() => new(OwnerId, GuildOwnerId, (byte)PropertyInfoId, Name, (byte)PrivacyLevel,
            Wallpaper, Roof, Entryway, Door, Ground, Music, Sky, (ushort)Flags, ResourceSharing, GardenSharing);
        private ResidencePersistence.Snapshot CapturePersistence()
        {
            if (plots.Any(row => row is not Plot || row.Id != Id) || plots.Select(row => row.Index).Distinct().Count() != plots.Count)
                throw new InvalidOperationException("Residence lost its original plot identities.");
            foreach (var entry in decors)
                if (entry.Value is not Decor || !ReferenceEquals(entry.Value.Residence, this) || entry.Key != entry.Value.DecorId)
                    throw new InvalidOperationException("Residence lost its original decor identity.");
            return new(CaptureHeader(), plots.OrderBy(row => row.Index).Select(row => ((Plot)row).CapturePersistence()).ToArray(),
                decors.Values.Where(row => !row.PendingDelete).OrderBy(row => row.DecorId).Select(row => ((Decor)row).CapturePersistence()).ToArray(),
                neighbors.Values.OrderBy(row => row.CharacterId).Select(row => new ResidencePersistence.Neighbor(row.CharacterId, row.PermissionLevel)).ToArray());
        }
        private void AcknowledgePersistence(ResidencePersistence.Snapshot target)
        {
            saveMask &= ~ResidenceSaveMask.Create;
            if (CaptureHeader() == target.Header) saveMask = ResidenceSaveMask.None;
            foreach (Plot plot in plots.Cast<Plot>()) plot.AcknowledgePersistence(target.Plots.Single(row => row.Index == plot.Index));
            foreach (var entry in decors.ToArray())
            {
                var saved = target.Decor.SingleOrDefault(row => row.Id == entry.Key);
                if (saved == null)
                {
                    if (entry.Value.PendingDelete) decors.Remove(entry.Key);
                }
                else ((Decor)entry.Value).AcknowledgePersistence(saved);
            }
            foreach (ResidenceNeighbor neighbor in neighbors.Values)
                neighbor.AcknowledgePersistence(target.Neighbors.SingleOrDefault(row => row.Character == neighbor.CharacterId));
            neighborsPendingDelete.RemoveAll(row => !target.Neighbors.Any(saved => saved.Character == row.CharacterId));
        }
        internal bool SavePlot(Plot plot, CharacterContext context)
        {
            if (persistence == null) return false;
            lock (MutationGate)
            {
                try
                {
                    if (!plots.Any(current => ReferenceEquals(current, plot))) throw new InvalidOperationException("Plot lost its original residence collection.");
                    Save(context); return true;
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
        internal bool SaveDecor(Decor decor, CharacterContext context)
        {
            if (persistence == null) return false;
            lock (MutationGate)
            {
                try
                {
                    if (!decors.TryGetValue(decor.DecorId, out IDecor current) || !ReferenceEquals(current, decor))
                        throw new InvalidOperationException("Decor lost its original residence collection.");
                    Save(context); return true;
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
    }
}
