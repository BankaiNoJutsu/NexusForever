using NexusForever.Database.Character;
using NexusForever.Game.Guild;

namespace NexusForever.Game.Housing
{
    public partial class Residence
    {
        private GuildCreationSettlement paidCreation;
        internal void PreparePaidCreation(GuildCreationSettlement command)
        {
            lock (MutationGate)
            {
                if (paidCreation != null || !HasPersistence || !ReferenceEquals(command.CreationResidence, this))
                    throw new InvalidOperationException("Community draft lost its original residence graph.");
                paidCreation = command;
            }
        }
        internal bool OwnsPaidCreationSave(CharacterContext context, object reservation)
            => paidCreation != null && paidCreation.OwnsEnrollment(context) && reservation != null
                && ReferenceEquals(reservation, communitySaveOwner);
        internal void RequirePaidCreationSave(CharacterContext context, object reservation)
        {
            if (paidCreation != null && !OwnsPaidCreationSave(context, reservation))
                throw new InvalidOperationException("Paid Community creation owns its residence save.");
        }
        internal void ReleasePaidCreation(GuildCreationSettlement command)
        {
            lock (MutationGate)
            {
                if (!ReferenceEquals(paidCreation, command)) throw new InvalidOperationException("Community residence changed its creation owner.");
                paidCreation = null;
            }
        }
    }
}
