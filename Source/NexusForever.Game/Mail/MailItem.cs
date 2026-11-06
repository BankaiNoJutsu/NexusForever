using System.Collections;
using Microsoft.EntityFrameworkCore;
using NexusForever.Game.Quest;
using NexusForever.Game.Abstract;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Mail;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Mail;
using NexusForever.GameTable;
using NexusForever.Network.World.Message.Model.Mail;
using NexusForever.Shared.Configuration;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Mail
{
    public partial class MailItem : IMailItem
    {
        private const float DefaultExpiryTimeDays = 30f;

        /// <summary>
        /// Determines which fields need saving for <see cref="IMailItem"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum MailSaveMask
        {
            None                = 0x0000,
            Create              = 0x0001,
            Flags               = 0x0002,
            CurrencyChange      = 0x0004,
            RecipientChange     = 0x0008,
            Delete              = 0x0010
        }

        public ulong Id { get; }

        public ulong RecipientId
        {
            get => recipientId;
            private set
            {
                if (value == recipientId)
                    throw new ArgumentException("Recipient ID must be different than current recipient.");
                recipientId = value;
                persistence.Dirty((int)MailSaveMask.RecipientChange);
            }
        }
        private ulong recipientId;

        public SenderType SenderType { get; }
        public ContentType ContentType { get; }
        public ulong SenderId { get; }
        public uint CreatureId { get; }

        public string Subject { get; private set; } = "";
        public string Message { get; } = "";
        public uint TextEntrySubject { get; }
        public uint TextEntryMessage { get; }

        public CurrencyType CurrencyType { get; }
        public ulong CurrencyAmount { get; }
        public bool IsCashOnDelivery { get; }

        public bool HasPaidOrCollectedCurrency
        {
            get => hasPaidOrCollectedCurrency;
            private set
            {
                hasPaidOrCollectedCurrency = value;
                persistence.Dirty((int)MailSaveMask.CurrencyChange);
            }
        }
        private bool hasPaidOrCollectedCurrency;

        public MailFlag Flags
        {
            get => flags;
            private set
            {
                flags = value;
                persistence.Dirty((int)MailSaveMask.Flags);
            }
        }
        private MailFlag flags;

        public DeliverySpeed DeliverySpeed { get; }
        public DateTime CreateTime { get; }
        public float ExpiryTime => sharedConfiguration?.Get<WorldConfig>()?.MailExpiryDays ?? DefaultExpiryTimeDays;

        public bool PendingCreate => persistence.Has((int)MailSaveMask.Create);

        /// <summary>
        /// Returns if <see cref="IMailItem"/> is enqueued to be deleted from the database.
        /// </summary>
        public bool PendingDelete => deleteRequested;

        private readonly byte storedContentType;
        internal BankMailDelivery CreationOwner { get; set; }
        internal IOutgoingDelivery OutgoingDelivery { get; set; }

        private readonly List<IMailAttachment> mailAttachments = new();
        private readonly HashSet<IMailAttachment> deletedAttachments = new();
        private readonly ushort realmId;
        private readonly ISharedConfiguration sharedConfiguration;

        /// <summary>
        /// Create a new <see cref="IMailItem"/> from an existing <see cref="CharacterMailModel"/>.
        /// </summary>
        public MailItem(
            CharacterMailModel model,
            ushort realmId = 0,
            IItemManager itemManager = null,
            IGameTableManager gameTableManager = null,
            ISharedConfiguration sharedConfiguration = null)
        {
            this.realmId = realmId;
            this.sharedConfiguration = sharedConfiguration;

            Id                         = model.Id;
            recipientId                = model.RecipientId;
            SenderType                 = (SenderType)model.SenderType;
            ContentType                = ResolveContentType(SenderType, model.ContentType);
            SenderId                   = model.SenderId;
            Subject                    = model.Subject;
            Message                    = model.Message;
            TextEntrySubject           = model.TextEntrySubject;
            TextEntryMessage           = model.TextEntryMessage;
            CreatureId                 = model.CreatureId;
            CurrencyType               = (CurrencyType)model.CurrencyType;
            CurrencyAmount             = model.CurrencyAmount;
            IsCashOnDelivery           = Convert.ToBoolean(model.IsCashOnDelivery);
            hasPaidOrCollectedCurrency = Convert.ToBoolean(model.HasPaidOrCollectedCurrency);
            flags                      = (MailFlag)model.Flags;
            DeliverySpeed               = (DeliverySpeed)model.DeliveryTime;
            CreateTime                 = model.CreateTime;

            foreach (CharacterMailAttachmentModel mailAttachment in model.Attachment)
                mailAttachments.Add(new MailAttachment(mailAttachment, itemManager, gameTableManager));

            storedContentType = model.ContentType;
            InitializePersistence(model);
        }

        /// <summary>
        /// Create a new <see cref="IMailItem"/> from supplied <see cref="IMailParameters"/>.
        /// </summary>
        public MailItem(MailParameters parameters, IAssetManager assetManager, ushort realmId = 0, ISharedConfiguration sharedConfiguration = null)
        {
            ArgumentNullException.ThrowIfNull(assetManager);
            this.realmId = realmId;
            this.sharedConfiguration = sharedConfiguration;

            Id          = assetManager.NextMailId;
            recipientId = parameters.RecipientCharacterId;
            SenderType  = parameters.MessageType;
            ContentType = parameters.ContentType != ContentType.PlayerMessage
                ? parameters.ContentType
                : ResolveContentType(SenderType);

            if (SenderType == SenderType.Player || SenderType == SenderType.GM)
                SenderId = parameters.SenderCharacterId;
            else
                CreatureId = parameters.CreatureId;

            if (parameters.SubjectStringId != 0u)
                TextEntrySubject = parameters.SubjectStringId;
            else
                Subject = parameters.Subject;

            if (parameters.BodyStringId != 0u)
                TextEntryMessage = parameters.BodyStringId;
            else
                Message = parameters.Body;

            CurrencyType = CurrencyType.Credits;

            if (parameters.CodAmount > 0ul)
            {
                CurrencyAmount = parameters.CodAmount;
                IsCashOnDelivery = true;
            }
            else
                CurrencyAmount = parameters.MoneyToGive;

            DeliverySpeed = parameters.DeliverySpeed;
            CreateTime   = DateTime.UtcNow;

            storedContentType = (byte)ContentType;
            InitializePersistence(null);
            persistence.Dirty((int)MailSaveMask.Create);
        }

        /// <summary>
        /// Enqueue <see cref="IMailItem"/> to be deleted from the database.
        /// </summary>
        public void EnqueueDelete(bool state)
        {
            lock (persistence.Gate)
            {
                AssertNoCashReceipt();
                deleteRequested = true;
                persistence.Dirty((int)MailSaveMask.Delete);
            }
        }

        internal void AcceptCommittedCreation(CharacterMailModel confirmed, string database)
        {
            lock (persistence.Gate)
            {
                if (!PendingCreate || PendingDelete || persistence.WasAttempted
                    || persistence.Has((int)(MailSaveMask.Flags | MailSaveMask.CurrencyChange | MailSaveMask.RecipientChange))
                    || confirmed?.Id != Id || deletedAttachments.Count != 0
                    || mailAttachments.Any(a => a is not MailAttachment concrete || !concrete.CanAcceptCommittedCreation))
                    throw new InvalidOperationException("Committed mail lost its creation state.");
                foreach (MailAttachment attachment in mailAttachments) attachment.AcceptCommittedCreation(database);
                persistenceDatabase = database ?? throw new ArgumentNullException(nameof(database));
                InitializePersistence(confirmed);
                membershipBaseline = CurrentAttachmentSet();
            }
        }

        public void Save(CharacterContext context) => SavePersistence(context);

        /// <summary>
        /// Returns the specific <see cref="IMailAttachment"/> based on its index.
        /// </summary>
        public IMailAttachment GetAttachment(uint index)
        {
            return index < mailAttachments.Count ? mailAttachments[(int)index] : null;
        }

        /// <summary>
        /// Mark this <see cref="IMailItem"/> as read by the player.
        /// </summary>
        public void MarkAsRead()
        {
            lock (persistence.Gate)
            {
                Flags |= MailFlag.IsRead;
            }
        }

        /// <summary>
        /// Mark this <see cref="IMailItem"/> as paid, or taken the currency attached.
        /// </summary>
        public void PayOrTakeCash()
        {
            lock (persistence.Gate)
            {
                AssertNoCashReceipt();
                HasPaidOrCollectedCurrency = true;
                MarkAsNotReturnable();
            }
        }

        /// <summary>
        /// Mark this <see cref="IMailItem"/> as not returnable.
        /// </summary>
        public void MarkAsNotReturnable()
        {
            lock (persistence.Gate)
            {
                Flags |= MailFlag.NotReturnable;
            }
        }

        /// <summary>
        /// Return this <see cref="IMailItem"/> to sender.
        /// </summary>
        public void ReturnMail()
        {
            lock (persistence.Gate)
            {
                AssertNoCashReceipt();
                if (SenderId == 0ul || SenderType != SenderType.Player && SenderType != SenderType.GM)
                    throw new InvalidOperationException("Mail has no player sender to return to.");

                RecipientId = SenderId;
                Subject = $"Returned: {Subject}";
                MarkAsNotReturnable();
            }
        }

        /// <summary>
        /// Returns whether this item is ready to be delivered based on <see cref="DeliverySpeed"/>.
        /// </summary>
        public bool IsReadyToDeliver()
        {
            if (DeliverySpeed == DeliverySpeed.Instant)
                return true;

            if (DeliverySpeed == DeliverySpeed.Hour)
                return DateTime.UtcNow
                    .Subtract(ToUtc(CreateTime))
                    .TotalHours > 1;

            if (DeliverySpeed == DeliverySpeed.Day)
                return DateTime.UtcNow
                    .Subtract(ToUtc(CreateTime))
                    .TotalDays > 1;

            return false;
        }

        /// <summary>
        /// Add a <see cref="IMailAttachment"/> to this <see cref="IMailItem"/>.
        /// </summary>
        public void AttachmentAdd(IMailAttachment mailAttachment)
        {
            lock (persistence.Gate)
            {
                AssertNoCashReceipt();
                mailAttachments.Add(mailAttachment);
            }
        }

        /// <summary>
        /// Remove a <see cref="IMailAttachment"/> from this <see cref="IMailItem"/>.
        /// </summary>
        public void AttachmentDelete(IMailAttachment mailAttachment, uint index)
        {
            lock (persistence.Gate)
            {
                AssertNoCashReceipt();
                // Validate the caller's slot before any attachment is marked deleted; list positions shift after a removal.
                if (index >= mailAttachments.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Mail attachment index is outside the current attachment list.");
                if (!ReferenceEquals(mailAttachments[(int)index], mailAttachment))
                    throw new ArgumentException("Mail attachment index does not identify the supplied attachment.", nameof(mailAttachment));

                mailAttachment.EnqueueDelete();

                mailAttachments.RemoveAt((int)index);
                deletedAttachments.Add(mailAttachment);
            }
        }

        private static ContentType ResolveContentType(SenderType senderType, byte storedContentType = 0)
        {
            if (storedContentType != 0)
                return (ContentType)storedContentType;

            return senderType switch
            {
                SenderType.ItemAuction     => ContentType.AuctionWon,
                SenderType.CommodityAuction => ContentType.AuctionWon,
                _                          => ContentType.PlayerMessage
            };
        }

        public ServerMailAvailable.Mail Build()
        {
            bool isPlayer = SenderType == SenderType.Player || SenderType == SenderType.GM;

            var serverMailItem = new ServerMailAvailable.Mail
            {
                MailId               = Id,
                SenderType           = SenderType,
                Subject              = Subject,
                Message              = Message,
                SubjectStringId     = TextEntrySubject,
                BodyStringId     = TextEntryMessage,
                FromCreatureId           = !isPlayer ? CreatureId : 0,
                CurrencySentType     = 0,
                CurrencySentAmount   = !IsCashOnDelivery && !HasPaidOrCollectedCurrency ? CurrencyAmount : 0,
                CostOnDeliveryAmount = IsCashOnDelivery && !HasPaidOrCollectedCurrency ? CurrencyAmount : 0,
                ExpiryTimeInDays     = GetRemainingExpiryTimeInDays(),
                ContentType          = ContentType,
                Flags                = Flags,
                Sender               = new NetworkIdentity
                {
                    RealmId = isPlayer ? realmId : (ushort)0,
                    Id      = isPlayer ? SenderId : 0ul
                },
            };

            foreach (IMailAttachment attachment in mailAttachments)
                serverMailItem.Attachments.Add(attachment.Build());

            return serverMailItem;
        }

        private float GetRemainingExpiryTimeInDays()
        {
            float expiryTime = ExpiryTime;
            if (expiryTime <= 0f)
                return 0f;

            DateTime createUtc = ToUtc(CreateTime);
            double remainingDays = createUtc.AddDays(expiryTime).Subtract(DateTime.UtcNow).TotalDays;
            return (float)Math.Max(0d, remainingDays);
        }

        private static DateTime ToUtc(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc   => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _                  => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            };
        }

        public IEnumerator<IMailAttachment> GetEnumerator()
        {
            return mailAttachments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
