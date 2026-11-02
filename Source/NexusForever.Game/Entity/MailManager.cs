using System.Numerics;
using System.Linq;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Mail;
using NexusForever.Game.Mail;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Configuration;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Entity
{
    public partial class MailManager : IMailManager
    {
        private readonly IPlayer player;
        private readonly IAssetManager assetManager;
        private readonly ICharacterManager characterManager;
        private readonly ushort realmId;
        private readonly IPlayerManager playerManager;
        private readonly IItemManager itemManager;
        private readonly IGameTableManager gameTableManager;
        private readonly ISharedConfiguration sharedConfiguration;
        private readonly object mailboxGate = new();
        private readonly Queue<IMailItem> outgoingMail = new();
        private readonly List<IMailItem> pendingMail = new();
        private readonly List<IMailItem> expiredMail = new();
        private readonly Dictionary<ulong, IMailItem> availableMail = new();

        // timer to check pending mail ever second
        private readonly UpdateTimer mailTimer = new(TimeSpan.FromSeconds(1));

        /// <summary>
        /// Create a new <see cref="IMailManager"/> from existing <see cref="CharacterModel"/> database model.
        /// </summary>
        public MailManager(
            IPlayer owner,
            CharacterModel model,
            IAssetManager assetManager = null,
            ICharacterManager characterManager = null,
            ushort realmId = 0,
            IPlayerManager playerManager = null,
            IItemManager itemManager = null,
            IGameTableManager gameTableManager = null,
            ISharedConfiguration sharedConfiguration = null,
            Func<CharacterContext> deliveryContexts = null)
        {
            player                = owner;
            this.assetManager     = assetManager;
            this.characterManager = characterManager;
            this.realmId          = realmId;
            this.playerManager    = playerManager;
            this.itemManager      = itemManager;
            this.gameTableManager = gameTableManager;
            this.sharedConfiguration = sharedConfiguration;
            InitializeDelivery(deliveryContexts);
            // Resolving the database identity does not open a connection. Login
            // must honor a retained local sender just like periodic polling.
            using var inboxContext = model.Mail.Count != 0 ? deliveryContexts?.Invoke() : null;
            string inboxDatabase = inboxContext != null ? Item.GetRewardDatabaseKey(inboxContext) : null;
            foreach (CharacterMailModel mailModel in model.Mail)
            {
                MailItem original = null;
                if (inboxDatabase != null && !ResolveOutgoing(inboxDatabase, mailModel, out original)) continue;
                if (inboxDatabase != null && original == null && !BankMailDelivery.Resolve(inboxDatabase, mailModel, out original)) continue;
                if (original != null && !TryAcceptMail(original)) continue;
                knownMail.Add(mailModel.Id);
                var mail = original ?? new MailItem(mailModel, realmId, itemManager, gameTableManager, sharedConfiguration);
                if (IsExpired(mail))
                    ExpireMail(mail);
                else if (mail.IsReadyToDeliver())
                    availableMail.Add(mail.Id, mail);
                else
                    pendingMail.Add(mail);
                if (original != null)
                {
                    original.CreationOwner?.Delivered();
                }
            }
        }

        public void Update(double lastTick)
        {
            UpdateDelivery(lastTick);
            mailTimer.Update(lastTick);
            if (mailTimer.HasElapsed)
            {
                bool sendAvailableMail = false;
                foreach (IMailItem mail in PendingSnapshot())
                {
                    if (!mail.IsReadyToDeliver())
                        continue;

                    lock (mailboxGate)
                    {
                        if (pendingMail.Remove(mail) && availableMail.TryAdd(mail.Id, mail))
                            sendAvailableMail = true;
                    }
                }

                // prevent sending multiple mail packets
                if (sendAvailableMail)
                    SendAvailableMail();

                RemoveExpiredMail();

                mailTimer.Reset();
            }
        }

        public void Save(CharacterContext context)
        {
            var call = BeginMailSave(context);
            try
            {
                SaveMail(context);
                context.OnSaveSettled(_ => EndMailSave(call));
                call.Registered = true;
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }

        private void SaveMail(CharacterContext context)
        {
            IMailItem[] available, pending;
            lock (mailboxGate)
            {
                available = availableMail.Values.ToArray();
                pending = pendingMail.ToArray();
            }
            SaveOutgoing(context);

            foreach (IMailItem mail in available)
            {
                if (mail.PendingDelete)
                    RetainMailDeletion(mail);

                mail.Save(context);
            }

            // Confirmed incoming attachments can retain dirty intent from the
            // sender's captured write even while their delivery time is pending.
            foreach (IMailItem mail in pending.Where(mail => !mail.PendingCreate))
                mail.Save(context);

            IMailItem[] deletions;
            lock (expiredMail) deletions = expiredMail.ToArray();
            foreach (IMailItem mail in deletions)
                if (!StageRetainedDeletion(mail, context)) mail.Save(context);
            context.OnSaveSettled(committed =>
            {
                if (!committed) return;
                lock (expiredMail)
                    foreach (var mail in deletions)
                        if (mail is MailItem concrete ? !concrete.HasUnsettledDeletion : !mail.PendingDelete)
                            expiredMail.Remove(mail);
            });
        }

        /// <summary>
        /// Called by <see cref="IPlayer"/> to send available mail on entering map.
        /// </summary>
        public void SendInitialPackets()
        {
            SendAvailableMail();
        }

        private void SendAvailableMail()
        {
            var mailAvailable = new ServerMailAvailable
            {
                NewMail = true
            };

            foreach (IMailItem mail in AvailableSnapshot().Where(m => !m.PendingDelete))
                mailAvailable.MailList.Add(mail.Build());

            player.Session.EnqueueMessageEncrypted(mailAvailable);
        }

        /// <summary>
        /// Enqueue new incoming <see cref="IMailItem"/> to be processed.
        /// </summary>
        public void EnqueueMail(IMailItem mail)
        {
            lock (mailboxGate)
                if (mail.PendingDelete || knownMail.Contains(mail.Id)) return;
            if (!TryAcceptMail(mail)) return;
            bool send;
            lock (mailboxGate)
            {
                if (mail.PendingDelete || !knownMail.Add(mail.Id)) return;
                send = mail.IsReadyToDeliver();
                if (send) availableMail.Add(mail.Id, mail);
                else pendingMail.Add(mail);
            }

            if (mail is MailItem committed)
            {
                committed.CreationOwner?.Delivered();
            }
            if (send) SendIncomingMail(mail);
        }

        private static bool TryAcceptMail(IMailItem mail)
            => mail is not MailItem { OutgoingDelivery: { } delivery } || delivery.TryAccept();

        /// <summary>
        /// Send mail to another <see cref="IPlayer"/>.
        /// </summary>
        public void SendMail(ClientMailSend mailSend)
        {
            bool inventoryAvailable = InventoryMutationScope.TryAcquire(player.Inventory, out var mutationLease);
            using var heldMutation = mutationLease;

            ICharacter targetCharacter = inventoryAvailable ? GetCharacterManager().GetCharacter(mailSend.Name) : null;

            var items = new List<IItem>();
            ulong totalCost = 0;
            GenericError GetResult()
            {
                if (!inventoryAvailable || player.Inventory is Inventory && !ReferenceEquals(player.MailManager, this)) return GenericError.MailBusy;
                if (targetCharacter == null)
                    return GenericError.MailCannotFindPlayer;

                if (targetCharacter.CharacterId == player.CharacterId)
                    return GenericError.MailCannotMailSelf;

                if (!player.SignatureEnabled
                    && (player.Account.EntitlementManager?.GetEntitlement(EntitlementType.FullSocialParticipation)?.Amount ?? 0u) == 0u)
                    return GenericError.MailCannotMailTrialAccount;

                if (mailSend.CreditsSent > 0ul
                    && !player.SignatureEnabled
                    && (player.Account.EntitlementManager.GetEntitlement(EntitlementType.VIPTier)?.Amount ?? 0u) > 0u)
                    return GenericError.MissingEntitlement;

                if (mailSend.CashOnDeliveryAmount > 0ul && mailSend.CreditsSent > 0ul)
                    return GenericError.MailCanNotHaveCoDAndGift;

                if (mailSend.CashOnDeliveryAmount > 0ul && mailSend.Items.All(i => i == 0ul))
                    return GenericError.MailFailedToCreate;

                if (mailSend.Items.Where(id => id != 0).Distinct().Count() != mailSend.Items.Count(id => id != 0))
                    return GenericError.MailInvalidInventorySlot;

                if (!Enum.IsDefined(mailSend.DeliverySpeed)) return GenericError.MailFailedToCreate;
                if (mailSend.Items.Any(i => i != 0ul))
                {
                    if (!IsTargetMailBoxInRange(mailSend.MailboxUnitId))
                        return GenericError.MailMailBoxOutOfRange;

                    foreach (ulong itemGuid in mailSend.Items.Where(i => i != 0ul))
                    {
                        IItem item = player.Inventory.GetItem(itemGuid);
                        if (item == null)
                            return GenericError.MailInvalidInventorySlot;

                        if (item.Location == InventoryLocation.Equipped)
                            return GenericError.MailInvalidInventorySlot;

                        if (item.Soulbound)
                            return GenericError.MailInvalidInventorySlot;

                        items.Add(item);
                    }
                }

                try { totalCost = checked((ulong)CalculateMailCost(mailSend.DeliverySpeed, items) + mailSend.CreditsSent); }
                catch (OverflowException) { return GenericError.MailInsufficientFunds; }
                if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, totalCost))
                    return GenericError.MailInsufficientFunds;

                return GenericError.Ok;
            }

            GenericError result = GetResult();
            if (result == GenericError.Ok)
            {
                var parameters = new MailParameters
                {
                    RecipientCharacterId = targetCharacter.CharacterId,
                    SenderCharacterId    = player.CharacterId,
                    MessageType          = SenderType.Player,
                    Subject              = mailSend.Subject,
                    Body                 = mailSend.Message,
                    MoneyToGive          = mailSend.CreditsSent,
                    CodAmount            = mailSend.CashOnDeliveryAmount,
                    DeliverySpeed         = mailSend.DeliverySpeed
                };

                if (player.Inventory is Inventory inventory)
                {
                    if (inventory.SendPlayerMail(CreateMail(parameters, items), totalCost, NotifyCommittedMail)) return;
                    result = GenericError.MailBusy;
                }
                else
                {
                    foreach (IItem item in items) player.Inventory.ItemRemove(item);
                    SendMail(parameters, items);
                    player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, totalCost);
                }
            }

            player.Session.EnqueueMessageEncrypted(new ServerMailResult
            {
                Action = MailResultAction.Send,
                MailId = 0,
                Result = result
            });
        }

        /// <summary>
        /// Send mail to self from a creature.
        /// </summary>
        public void SendMail(uint creatureId, DeliverySpeed speed, uint subject, uint body, IEnumerable<uint> itemIds)
        {
            if (gameTableManager.Creature2.GetEntry(creatureId) == null)
                throw new ArgumentException($"Invalid creature {creatureId} for mail sender!");

            if (gameTableManager.LocalizedText.GetEntry(subject) == null)
                throw new ArgumentException($"Invalid localised text {subject} for mail subject!");

            if (gameTableManager.LocalizedText.GetEntry(body) == null)
                throw new ArgumentException($"Invalid localised text {body} for mail body!");

            var parameters = new MailParameters
            {
                MessageType          = SenderType.Creature,
                RecipientCharacterId = player.CharacterId,
                CreatureId           = creatureId,
                SubjectStringId      = subject,
                BodyStringId         = body,
                DeliverySpeed        = speed
            };

            var items = new List<IItem>();
            foreach (uint itemId in itemIds)
            {
                IItemInfo info = itemManager?.GetItemInfo(itemId);
                if (info == null)
                    throw new ArgumentException($"Invalid item {itemId} for mail attachment!");

                var item = new Item(null, info, itemManager: itemManager, gameTableManager: gameTableManager);
                items.Add(item);
            }

            SendMail(parameters, items);
        }

        private void SendMail(MailParameters parameters, IEnumerable<IItem> items)
        {
            var mail = CreateMail(parameters, items);
            lock (outgoingMail) outgoingMail.Enqueue(mail);
        }

        private MailItem CreateMail(MailParameters parameters, IEnumerable<IItem> items)
        {
            var mail = new MailItem(parameters, assetManager, realmId, sharedConfiguration);
            uint index = 0;
            foreach (IItem item in items)
            {
                var attachment = new MailAttachment(mail.Id, index++, item);
                mail.AttachmentAdd(attachment);
            }

            return mail;
        }

        private uint CalculateMailCost(DeliverySpeed time, List<IItem> items)
        {
            GameFormulaEntry GetMailParameters()
            {
                if (items.Count == 0)
                    return gameTableManager.GameFormula.GetEntry(860);

                return time switch
                {
                    DeliverySpeed.Instant => gameTableManager.GameFormula.GetEntry(861),
                    DeliverySpeed.Hour => gameTableManager.GameFormula.GetEntry(862),
                    DeliverySpeed.Day => gameTableManager.GameFormula.GetEntry(863),
                    _ => null
                };
            }

            checked
            {
                GameFormulaEntry parameters = GetMailParameters();
                uint cost = parameters.Dataint0;

                foreach (IItem item in items)
                {
                    cost += parameters.Dataint01;
                    // only instant delivery speed takes item worth into consideration
                    if (parameters.Datafloat01 > 0.0f)
                    {
                        if (item.GetVendorSellCurrency(0) == CurrencyType.Credits)
                            cost += (uint)(item.GetVendorSellAmount(0) * parameters.Datafloat01);
                        if (item.GetVendorSellCurrency(1) == CurrencyType.Credits)
                            cost += (uint)(item.GetVendorSellAmount(1) * parameters.Datafloat01);
                    }
                }

                return cost;
            }
        }

        private ICharacterManager GetCharacterManager()
        {
            return characterManager ?? throw new InvalidOperationException("MailManager requires an ICharacterManager.");
        }

        /// <summary>
        /// Delete a <see cref="IMailItem"/> with supplied id.
        /// </summary>
        public void MailDelete(ulong mailId)
        {
            GenericError result = GenericError.Ok;
            TryGetAvailable(mailId, out IMailItem mailItem);
            if (IsMailSaveInProgress || HasManualAction(mailId)
                || player.Inventory is Inventory && !ManualActionActor())
                result = GenericError.MailBusy;
            else if (mailItem == null)
                result = GenericError.MailDoesNotExist;
            else if (mailItem is MailItem { HasPendingReceipt: true })
                result = GenericError.MailBusy;

            if (result == GenericError.Ok)
            {
                if (player.Inventory is Inventory && mailItem is MailItem concrete)
                    RetainManualAction(concrete, MailResultAction.Delete);
                lock (mailboxGate) availableMail.Remove(mailId);
                mailItem.EnqueueDelete(true);
                RetainMailDeletion(mailItem);
                if (player.Inventory is Inventory && mailItem is MailItem) return;
                NotifyMailUnavailable(mailId);
            }

            player.Session.EnqueueMessageEncrypted(new ServerMailResult
            {
                Action = MailResultAction.Delete,
                MailId = mailId,
                Result = result
            });
        }

        /// <summary>
        /// Mark a <see cref="IMailItem"/> as read with supplied id.
        /// </summary>
        public void MailMarkAsRead(ulong mailId)
        {
            if (!TryGetAvailable(mailId, out IMailItem mailItem))
                return;

            mailItem.MarkAsRead();
        }

        /// <summary>
        /// Pay cash on delivery for a <see cref="IMailItem"/> with supplied id.
        /// </summary>
        public void MailPayCod(ulong mailId)
        {
            bool inventoryAvailable = InventoryMutationScope.TryAcquire(player.Inventory, out var mutationLease);
            using var heldMutation = mutationLease;

            IMailItem mail = null;
            GenericError GetResult()
            {
                if (!inventoryAvailable || IsMailSaveInProgress) return GenericError.MailBusy;
                if (!TryGetAvailable(mailId, out mail))
                    return GenericError.MailDoesNotExist;

                if (!mail.IsCashOnDelivery || mail is MailItem { HasPendingReceipt: true }
                    || player.Inventory is Inventory && !ReferenceEquals(player.MailManager, this))
                    return GenericError.MailBusy;

                if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, mail.CurrencyAmount))
                    return GenericError.MailInsufficientFunds;

                if (mail.HasPaidOrCollectedCurrency)
                    return GenericError.MailBusy;

                return GenericError.Ok;
            }

            GenericError result = GetResult();
            if (result == GenericError.Ok)
            {
                var parameters = new MailParameters
                {
                    RecipientCharacterId = mail.SenderId,
                    SenderCharacterId    = mail.RecipientId,
                    MessageType          = SenderType.Player,
                    Subject              = $"Cash from: {mail.Subject}",
                    MoneyToGive          = mail.CurrencyAmount,
                    DeliverySpeed        = DeliverySpeed.Instant
                };

                if (player.Inventory is Inventory inventory)
                {
                    if (mail is MailItem concrete && inventory.CollectMailCash(concrete, CreateMail(parameters, []),
                        () => ReferenceEquals(player.MailManager, this),
                        () => TryGetAvailable(mailId, out var current) && ReferenceEquals(current, mail), NotifyCommittedMail)) return;
                    result = GenericError.MailBusy;
                }
                else
                {
                    player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, mail.CurrencyAmount);
                    mail.PayOrTakeCash();
                    SendMail(parameters, Enumerable.Empty<Item>());
                }
            }

            player.Session.EnqueueMessageEncrypted(new ServerMailResult
            {
                Action = MailResultAction.PayCashOnDelivery,
                MailId = mailId,
                Result = result
            });
        }

        /// <summary>
        /// Return <see cref="IMailItem"/> to original sender with supplied id.
        /// </summary>
        public void ReturnMail(ulong mailId)
        {
            IMailItem mailItem = null;
            GenericError GetResult()
            {
                if (IsMailSaveInProgress || HasManualAction(mailId)
                    || player.Inventory is Inventory && !ManualActionActor()) return GenericError.MailBusy;
                if (!TryGetAvailable(mailId, out mailItem))
                    return GenericError.MailDoesNotExist;

                if (mailItem is MailItem { HasPendingReceipt: true }) return GenericError.MailBusy;
                if ((mailItem.Flags & MailFlag.NotReturnable) != 0)
                    return GenericError.MailCannotReturn;

                if (!CanReturnToSender(mailItem))
                    return GenericError.MailCannotReturn;

                return GenericError.Ok;
            }

            GenericError result = GetResult();
            if (result == GenericError.Ok)
            {
                if (player.Inventory is Inventory && mailItem is MailItem concrete)
                    RetainManualAction(concrete, MailResultAction.Send);
                mailItem.ReturnMail();

                lock (mailboxGate) availableMail.Remove(mailItem.Id);
                lock (outgoingMail) outgoingMail.Enqueue(mailItem);

                if (player.Inventory is Inventory && mailItem is MailItem) return;
                NotifyMailUnavailable(mailId);
            }

            player.Session.EnqueueMessageEncrypted(new ServerMailResult
            {
                Action = MailResultAction.Send,
                MailId = mailId,
                Result = result
            });
        }

        private static bool CanReturnToSender(IMailItem mailItem)
        {
            return mailItem.SenderId != 0ul
                && (mailItem.SenderType == SenderType.Player || mailItem.SenderType == SenderType.GM);
        }

        /// <summary>
        /// Take attachment from <see cref="IMailItem"/> with supplied id and index.
        /// </summary>
        public void MailTakeAttachment(ulong mailId, uint attachmentIndex, uint unitId)
        {
            bool inventoryAvailable = InventoryMutationScope.TryAcquire(player.Inventory, out var mutationLease);
            using var heldMutation = mutationLease;

            IMailItem mailItem = null;
            IMailAttachment mailAttachment = null;

            GenericError GetResult()
            {
                if (!inventoryAvailable || IsMailSaveInProgress) return GenericError.MailBusy;
                if (!TryGetAvailable(mailId, out mailItem))
                    return GenericError.MailDoesNotExist;

                mailAttachment = mailItem.GetAttachment(attachmentIndex);
                if (mailAttachment == null)
                    return GenericError.MailNoAttachment;

                if (mailItem is MailItem { HasPendingReceipt: true }
                    || player.Inventory is Inventory && !ReferenceEquals(player.MailManager, this)) return GenericError.MailBusy;

                if (mailItem.IsCashOnDelivery && !mailItem.HasPaidOrCollectedCurrency)
                    return GenericError.MailInsufficientFunds;

                if (unitId == 0u || !IsTargetMailBoxInRange(unitId))
                    return GenericError.MailMailBoxOutOfRange;

                if (player.Inventory.IsInventoryFull(InventoryLocation.Inventory))
                    return GenericError.ItemInventoryFull;

                return GenericError.Ok;
            }

            GenericError result = GetResult();
            if (result == GenericError.Ok)
            {
                if (player.Inventory is Inventory inventory)
                {
                    if (mailItem is MailItem concrete && mailAttachment is MailAttachment attachment
                        && inventory.TakeMailAttachment(concrete, attachment, attachmentIndex,
                            () => ReferenceEquals(player.MailManager, this),
                            () => TryGetAvailable(mailId, out var current) && ReferenceEquals(current, mailItem))) return;
                    result = GenericError.MailBusy;
                }
                else
                {
                    mailAttachment.Item.CharacterId = player.CharacterId;
                    player.Inventory.AddItem(mailAttachment.Item, InventoryLocation.Inventory);
                    mailItem.MarkAsNotReturnable();
                    mailItem.AttachmentDelete(mailAttachment, attachmentIndex);
                }
            }

            player.Session.EnqueueMessageEncrypted(new ServerMailTakeAttachment
            {
                MailId = mailId,
                Result = result,
                Index  = attachmentIndex
            });
        }

        /// <summary>
        /// Take cash from <see cref="IMailItem"/> with supplied id.
        /// </summary>
        public void MailTakeCash(ulong mailId, uint unitId)
        {
            bool inventoryAvailable = InventoryMutationScope.TryAcquire(player.Inventory, out var mutationLease);
            using var heldMutation = mutationLease;

            IMailItem mailItem = null;
            GenericError GetResult()
            {
                if (!inventoryAvailable || IsMailSaveInProgress) return GenericError.MailBusy;
                if (!TryGetAvailable(mailId, out mailItem))
                    return GenericError.MailDoesNotExist;

                // probably not the correct error
                if (mailItem.HasPaidOrCollectedCurrency || mailItem.IsCashOnDelivery)
                    return GenericError.MailNoAttachment;

                if (mailItem is MailItem { HasPendingReceipt: true }
                    || player.Inventory is Inventory && !ReferenceEquals(player.MailManager, this)) return GenericError.MailBusy;

                if (unitId == 0u || !IsTargetMailBoxInRange(unitId))
                    return GenericError.MailMailBoxOutOfRange;

                return GenericError.Ok;
            }

            GenericError result = GetResult();
            if (result == GenericError.Ok)
            {
                if (player.Inventory is Inventory inventory)
                {
                    if (mailItem is MailItem concrete && inventory.CollectMailCash(concrete, null,
                        () => ReferenceEquals(player.MailManager, this),
                        () => TryGetAvailable(mailId, out var current) && ReferenceEquals(current, mailItem), null)) return;
                    result = GenericError.MailBusy;
                }
                else
                {
                    player.CurrencyManager.CurrencyAddAmount(mailItem.CurrencyType, mailItem.CurrencyAmount);
                    mailItem.PayOrTakeCash();
                }
            }

            player.Session.EnqueueMessageEncrypted(new ServerMailResult
            {
                Action = MailResultAction.TakeCash,
                MailId = mailId,
                Result = result
            });
        }

        /// <summary>
        /// Checks to see if the targeted <see cref="IMailItem"/> is in range.
        /// </summary>
        private bool IsTargetMailBoxInRange(uint unitId)
        {
            // native client function MailSystemLib.AtMailbox also uses entry 237 for distance check
            GameFormulaEntry entry = gameTableManager.GameFormula.GetEntry(237);
            if (entry == null)
                throw new InvalidOperationException();

            var entity = player.GetVisible<IWorldEntity>(unitId);
            return entity is IMailboxEntity && Vector3.DistanceSquared(player.Position, entity.Position) < entry.Datafloat0 * entry.Datafloat0; // Checking squared distance avoids a slow sqrt operation.
        }

        private void RemoveExpiredMail()
        {
            if (IsMailSaveInProgress) return;
            foreach (IMailItem mail in PendingSnapshot())
            {
                if (mail is MailItem { HasPendingReceipt: true }) continue;
                if (!IsExpired(mail))
                    continue;

                lock (mailboxGate) pendingMail.Remove(mail);
                ExpireMail(mail);
            }

            List<ulong> deprecatedMailIds = new();
            List<MailItem> retainedExpirations = new();
            foreach (IMailItem mail in AvailableSnapshot())
            {
                if (mail.PendingDelete)
                {
                    lock (mailboxGate) availableMail.Remove(mail.Id);
                    RetainMailDeletion(mail);
                    continue;
                }
                if (mail is MailItem { HasPendingReceipt: true }) continue;
                if (!IsExpired(mail))
                    continue;

                lock (mailboxGate) availableMail.Remove(mail.Id);
                ExpireMail(mail);
                if (player.Inventory is Inventory && mail is MailItem concrete)
                {
                    if (ManualActionActor()) retainedExpirations.Add(concrete);
                }
                else deprecatedMailIds.Add(mail.Id);
            }

            if (retainedExpirations.Count > 0) RetainExpiryBatch(retainedExpirations.ToArray());
            if (deprecatedMailIds.Count > 0)
                NotifyMailDeprecated(deprecatedMailIds);
        }

        private void ExpireMail(IMailItem mail)
        {
            mail.EnqueueDelete(true);
            RetainMailDeletion(mail);
        }

        private void RetainMailDeletion(IMailItem mail)
        {
            lock (expiredMail)
                if (!expiredMail.Contains(mail)) expiredMail.Add(mail);
        }

        private static bool IsExpired(IMailItem mail)
        {
            if (mail.ExpiryTime <= 0f)
                return false;

            return DateTime.UtcNow >= GetExpiryUtc(mail);
        }

        private static DateTime GetExpiryUtc(IMailItem mail)
        {
            DateTime createUtc = mail.CreateTime.Kind switch
            {
                DateTimeKind.Utc   => mail.CreateTime,
                DateTimeKind.Local => mail.CreateTime.ToUniversalTime(),
                _                  => DateTime.SpecifyKind(mail.CreateTime, DateTimeKind.Utc)
            };

            return createUtc.AddDays(mail.ExpiryTime);
        }

        private void SendIncomingMail(IMailItem mail)
        {
            player.Session.EnqueueMessageEncrypted(new ServerMailAvailable
            {
                NewMail  = true,
                MailList = { mail.Build() }
            });
        }

        private void NotifyMailUnavailable(ulong mailId)
        {
            player.Session.EnqueueMessageEncrypted(new ServerMailUnavailable
            {
                MailId = mailId
            });
        }

        private void NotifyMailDeprecated(IEnumerable<ulong> mailIds)
        {
            player.Session.EnqueueMessageEncrypted(new ServerMailItemDeprecation
            {
                MailIds = mailIds.ToList()
            });
        }

        /// <summary>
        /// Take all cash and attachments from each <see cref="IMailItem"/> in the supplied list.
        /// </summary>
        public void MailTakeAllFromSelection(IEnumerable<ulong> mailIds, uint unitId)
        {
            if (player.Inventory is Inventory inventory)
            {
                bool available = InventoryMutationScope.TryAcquire(player.Inventory, out var lease);
                using var mutation = lease;
                CollectSelection(inventory, mailIds.Distinct().ToArray(), unitId, available);
                return;
            }
            foreach (ulong mailId in mailIds)
            {
                if (!TryGetAvailable(mailId, out IMailItem mailItem))
                    continue;

                if (!mailItem.HasPaidOrCollectedCurrency && mailItem.CurrencyAmount > 0ul && !mailItem.IsCashOnDelivery)
                    MailTakeCash(mailId, unitId);

                if (mailItem.IsCashOnDelivery && !mailItem.HasPaidOrCollectedCurrency)
                    continue;

                foreach (IMailAttachment attachment in mailItem.ToList())
                    MailTakeAttachment(mailId, attachment.Index, unitId);
            }
        }
    }
}
