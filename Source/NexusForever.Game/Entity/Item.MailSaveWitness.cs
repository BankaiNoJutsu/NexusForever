using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Entity
{
    public partial class Item
    {
        internal bool HasMailTakeSourceCustody(ulong recipient, bool allowRecipientOwned)
        {
            lock (persistenceGate)
            {
                // Loading initializes the live location to None; admission must
                // also retain the actual stored page/owner from its baseline.
                return activeSave == null && persistenceBaseline != null && realmBankBaseline == null
                    && location == InventoryLocation.None && persistenceBaseline.Location == (ushort)InventoryLocation.None
                    && characterId == persistenceBaseline.OwnerId
                    && (characterId == null || allowRecipientOwned && characterId == recipient);
            }
        }

        // The mail aggregate observes the Item's enrolled copy, not later actor
        // mutations. It does not acknowledge or replace Item-owned revisions.
        internal MailSaveWitness CaptureMailSaveWitness(CharacterContext context)
        {
            lock (persistenceGate)
            {
                if (activeSave != null && !ReferenceEquals(activeSave.Context, context))
                    throw new InvalidOperationException("Mail Item belongs to another save context.");
                context.ChangeTracker.DetectChanges();
                return new(this, context, activeSave, CopyModel(activeSave == null ? persistenceBaseline : activeSave.LastState), activeSave?.Model);
            }
        }

        internal sealed class MailSaveWitness(Item owner, CharacterContext context, SaveUnit unit, ItemModel target, ItemModel model)
            : SaveWitness(owner, context, unit, target, model, "Mail Item");
    }
}
