using NexusForever.Game.Mail;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class MailManager
    {
        private void CollectSelection(Inventory inventory, ulong[] ids, uint unitId, bool inventoryAvailable)
        {
            var requested = new List<Inventory.MailSelectionRequest>();
            var errors = new List<IWritable>();
            foreach (ulong id in ids)
            {
                if (!TryGetAvailable(id, out var item)) continue;
                if (item.IsCashOnDelivery && !item.HasPaidOrCollectedCurrency) continue;
                bool cash = !item.IsCashOnDelivery && !item.HasPaidOrCollectedCurrency && item.CurrencyAmount != 0;
                var attachments = item.ToArray();
                if (!cash && attachments.Length == 0) continue;
                GenericError error = GenericError.Ok;
                if (!inventoryAvailable || IsMailSaveInProgress || !ReferenceEquals(player.MailManager, this)
                    || item is not MailItem { HasPendingReceipt: false } || attachments.Any(link => link is not MailAttachment))
                    error = GenericError.MailBusy;
                else if (unitId == 0 || !IsTargetMailBoxInRange(unitId)) error = GenericError.MailMailBoxOutOfRange;
                if (error != GenericError.Ok)
                {
                    errors.AddRange(MailSelectionReplies.Build(id, attachments.Select(link => link.Index), cash, error));
                    continue;
                }
                requested.Add(new((MailItem)item, attachments.Cast<MailAttachment>().ToArray(), cash));
            }
            if (requested.Count == 0)
            {
                foreach (var error in errors) player.Session.EnqueueMessageEncrypted(error);
                return;
            }
            if (inventory.CollectMailSelection(requested.ToArray(), errors.ToArray(),
                () => ReferenceEquals(player.MailManager, this),
                mail => TryGetAvailable(mail.Id, out var current) && ReferenceEquals(current, mail))) return;
            foreach (var error in errors) player.Session.EnqueueMessageEncrypted(error);
            foreach (var request in requested)
            {
                foreach (var reply in MailSelectionReplies.Build(request.Mail.Id,
                    request.Attachments.Select(link => link.Index), request.CollectCash, GenericError.MailBusy))
                    player.Session.EnqueueMessageEncrypted(reply);
            }
        }
    }
}
