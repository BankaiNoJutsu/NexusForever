using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        internal sealed record MailSelectionRequest(MailItem Mail, MailAttachment[] Attachments, bool CollectCash);

        internal bool CollectMailSelection(MailSelectionRequest[] requested, IWritable[] errors,
            Func<bool> currentManager, Func<MailItem, bool> currentMail)
        {
            lock (MutationGate)
            {
                using var mutation = new InventoryMutationOperation(this);
                if (RequiresAuthoritativeReload || rewardOwnerInvalidated || IsRealmBankPersistenceBlocked
                    || !currentManager() || requested.Any(request => !currentMail(request.Mail)
                        || request.Mail.RecipientId != characterId)) return false;
                try
                {
                    var bag = GetBag(InventoryLocation.Inventory);
                    if (bag?.HasReservedSlots == true) return false;
                    int capacity = bag?.CreateSnapshot().Count(item => item == null) ?? 0;
                    var capacityErrors = new List<IWritable>(errors);
                    var accepted = new List<MailSelectionRequest>();
                    foreach (var request in requested)
                    {
                        var links = request.Attachments.Take(capacity).ToArray(); capacity -= links.Length;
                        foreach (var rejected in request.Attachments.Skip(links.Length))
                            capacityErrors.Add(new ServerMailTakeAttachment { MailId = request.Mail.Id, Index = rejected.Index, Result = GenericError.ItemInventoryFull });
                        if (request.CollectCash || links.Length != 0) accepted.Add(request with { Attachments = links });
                    }
                    if (accepted.Count == 0)
                    {
                        foreach (var error in capacityErrors) player.Session.EnqueueMessageEncrypted(error);
                        return true;
                    }
                    if (accepted.Any(request => request.CollectCash) && player.CurrencyManager is not CurrencyManager) return false;
                    using var batch = BeginRealmBankNotifications(true);
                    if (batch == null) return false;
                    batch.RequireActor(currentManager);
                    var failure = new List<IWritable>();
                    foreach (var request in accepted)
                    {
                        failure.AddRange(MailSelectionReplies.Build(request.Mail.Id,
                            request.Attachments.Select(link => link.Index), request.CollectCash, GenericError.DbFailure));
                        if (request.Attachments.Length == 0)
                            batch.Attach(request.Mail.CaptureCashReceipt(characterId));
                        else
                            batch.Attach(request.Mail.CaptureAttachmentTake(request.Attachments, characterId,
                                () => currentMail(request.Mail) && request.Attachments.All(link => ReferenceEquals(GetItem(link.Item.Guid), link.Item)), request.CollectCash));
                        foreach (var attachment in request.Attachments)
                        {
                            if (GetItem(attachment.Item.Guid) != null)
                                throw new InvalidOperationException("Selected mail Item already belongs to this inventory.");
                            batch.RetainPreparation(attachment.Item);
                            attachment.Item.CharacterId = characterId;
                            uint? slot = bag.GetFirstAvailableBagIndex();
                            if (!slot.HasValue) throw new InvalidOperationException("Selected mail lost its captured destination capacity.");
                            AddItem(attachment.Item, InventoryLocation.Inventory, slot.Value);
                            if (!player.IsLoading)
                                batch.Add(new ServerItemAdd { InventoryItem = new InventoryItem { Item = attachment.Item.Build(), Reason = ItemUpdateReason.NoReason } });
                        }
                    }
                    batch.OnFailure(failure.Concat(capacityErrors));
                    var cash = accepted.Where(request => request.CollectCash).ToArray();
                    if (cash.Length != 0)
                    {
                        var payment = ((CurrencyManager)player.CurrencyManager).CaptureBankCurrency(player,
                            cash.Select(request => new CurrencyManager.BankCurrencyChange(request.Mail.CurrencyType, request.Mail.CurrencyAmount, false)).ToArray(),
                            () => batch.CurrentActor() && cash.All(request => currentMail(request.Mail)), batch.After);
                        batch.Attach(payment); batch.Add(payment.Publish);
                    }
                    foreach (var request in accepted)
                    {
                        foreach (var reply in MailSelectionReplies.Build(request.Mail.Id,
                            request.Attachments.Select(link => link.Index), request.CollectCash, GenericError.Ok)) batch.Add(reply);
                    }
                    foreach (var error in capacityErrors) batch.Add(error);
                    batch.Complete(); return true;
                }
                catch (Exception error)
                {
                    log.Error(error, "Mail selection preparation failed."); return false;
                }
            }
        }
    }
}
