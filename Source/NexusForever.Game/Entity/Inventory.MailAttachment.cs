using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        internal bool TakeMailAttachment(MailItem mail, MailAttachment attachment, uint index,
            Func<bool> currentManager, Func<bool> currentMail)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || rewardOwnerInvalidated || IsRealmBankPersistenceBlocked
                    || !currentManager() || !currentMail() || mail.RecipientId != characterId
                    || GetItem(attachment.Item.Guid) != null) return false;
                try
                {
                    using var batch = BeginRealmBankNotifications(true);
                    if (batch == null) return false;
                    batch.OnFailure(new ServerMailTakeAttachment { MailId = mail.Id, Index = index, Result = GenericError.DbFailure });
                    batch.RequireActor(currentManager);
                    var receipt = mail.CaptureAttachmentTake(attachment, characterId,
                        () => currentMail() && ReferenceEquals(GetItem(attachment.Item.Guid), attachment.Item));
                    batch.Attach(receipt);
                    batch.RetainPreparation(attachment.Item);
                    attachment.Item.CharacterId = characterId;
                    var bag = GetBag(InventoryLocation.Inventory);
                    uint? slot = bag?.GetFirstAvailableBagIndex();
                    if (!slot.HasValue) throw new InvalidOperationException("Mail attachment lost its destination slot.");
                    AddItem(attachment.Item, InventoryLocation.Inventory, slot.Value);
                    if (!player.IsLoading)
                        batch.Add(new ServerItemAdd
                        {
                            InventoryItem = new InventoryItem
                            {
                                Item = attachment.Item.Build(), Reason = ItemUpdateReason.NoReason
                            }
                        });
                    batch.Add(new ServerMailTakeAttachment { MailId = mail.Id, Index = index, Result = GenericError.Ok });
                    batch.Complete(); return true;
                }
                catch (Exception error)
                {
                    log.Error(error, "Mail attachment preparation failed."); return false;
                }
            }
        }
    }
}
