using NexusForever.Database.Character;
using NexusForever.Game.Housing;

namespace NexusForever.Game.Guild
{
    public partial class Community
    {
        private readonly Queue<Residence.Association> associations = new();
        internal Residence.Association CurrentAssociation => associations.TryPeek(out var current) ? current : null;
        internal bool AssociationPrepared => CurrentAssociation?.Prepared == true;
        internal bool AssociationPrerequisitesClear => !PendingCreate && !PaidNamePrepared && !PrivacyPrepared && !NameUnitActive
            && !HasActiveMutationUnit && pendingResidenceChanges.Count == 0 && pendingMemberRemovals.Count == 0 && pendingDisband == null;
        internal bool OwnsAssociation(object token) => token != null && ReferenceEquals(CurrentAssociation, token);
        internal void RequireAssociationSave(object token)
        {
            if (AssociationPrepared && !OwnsAssociation(token)) throw new InvalidOperationException("Community belongs to its retained housing association.");
        }
        internal void QueueAssociation(Residence.Association command)
        {
            if (associations.Any(current => current.Conflicts(command))) throw new ArgumentException("Community plot is already reserved by an accepted placement.");
            associations.Enqueue(command);
        }
        internal void FinishAssociation(Residence.Association command)
        {
            if (!OwnsAssociation(command)) throw new InvalidOperationException("Association cannot release another community request.");
            associations.Dequeue();
        }
        internal void RequireAssociationPlotAssignment()
        {
            if (AssociationPrepared && !CurrentAssociation.Applying) throw new InvalidOperationException("Community plot reservations belong to the prepared association.");
        }
        private void RetainAssociationDeparture(ulong characterId)
        {
            if (!AssociationPrepared || CurrentAssociation.AssociatedChild.OwnerId != characterId) return;
            var child = CurrentAssociation.AssociatedChild;
            QueueResidenceChange(child);
            child.GuildOwnerId = null;
            child.PropertyInfoId = Static.Housing.PropertyInfoId.Residence;
        }
    }
    public abstract partial class GuildBase
    {
        internal GuildMutationPersistence.Snapshot CaptureAssociationGraph(Residence.Association command)
        {
            if (this is not Community community || !community.OwnsAssociation(command)) throw new InvalidOperationException("Association lost its original community graph.");
            return CaptureMutations();
        }
        internal void SaveAssociationGraph(CharacterContext context, Residence.Association command, GuildMutationPersistence.Snapshot target)
        {
            if (this is not Community community || !community.OwnsAssociation(command) || !command.Prepared || mutationPersistence == null)
                throw new InvalidOperationException("Association lost its prepared community graph.");
            namePersistence?.RequireDatabase(context);
            mutationPersistence.Save(context, force: true, frozen: target, reservation: command);
        }
        internal void RequireAssociationDatabase(CharacterContext context, Residence.Association command)
        {
            if (this is not Community community || !community.OwnsAssociation(command) || !command.Prepared || mutationPersistence == null)
                throw new InvalidOperationException("Association lost its original community route.");
            namePersistence?.RequireDatabase(context);
            mutationPersistence.RequireOriginalDatabase(context, command);
        }
    }
}
