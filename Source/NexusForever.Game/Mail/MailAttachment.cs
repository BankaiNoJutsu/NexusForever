using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Mail;
using NexusForever.Game.Entity;
using NexusForever.GameTable;
using NexusForever.Network.World.Message.Model.Mail;

namespace NexusForever.Game.Mail
{
    public partial class MailAttachment : IMailAttachment
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="IMailAttachment"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum MailAttachmentSaveMask
        {
            None   = 0x0000,
            Create = 0x0001,
            Delete = 0x0002
        }

        public ulong Id { get; }
        public uint Index { get; }
        public IItem Item { get; }


        /// <summary>
        /// Create a new <see cref="IMailAttachment"/> from an existing <see cref="CharacterMailAttachmentModel"/> model.
        /// </summary>
        /// <param name="model"></param>
        public MailAttachment(
            CharacterMailAttachmentModel model,
            IItemManager itemManager = null,
            IGameTableManager gameTableManager = null)
        {
            Id       = model.Id;
            Index    = model.Index;
            Item     = new Item(model.Item, itemManager, gameTableManager);

            InitializePersistence(model);
        }

        /// <summary>
        /// Create a new <see cref="IMailAttachment"/>.
        /// </summary>
        public MailAttachment(ulong mailId, uint index, IItem item)
        {
            Id       = mailId;
            Index    = index;
            Item     = item;

            InitializePersistence(null);
            persistence.Dirty((int)MailAttachmentSaveMask.Create);
        }

        /// <summary>
        /// Enqueue <see cref="IMailAttachment"/> to be deleted from the database.
        /// </summary>
        public void EnqueueDelete()
        {
            lock (persistence.Gate)
            {
                if (takeReceipt != null) throw new InvalidOperationException("Mail attachment belongs to an unfinished take.");
                deleteRequested = true;
                persistence.Dirty((int)MailAttachmentSaveMask.Delete);
            }
        }

        internal bool CanAcceptCommittedCreation => persistence.Has((int)MailAttachmentSaveMask.Create)
            && !deleteRequested && !persistence.WasAttempted;
        internal void AcceptCommittedCreation(string database)
        {
            lock (persistence.Gate)
            {
                if (!CanAcceptCommittedCreation) throw new InvalidOperationException("Mail attachment lost its new creation owner.");
                persistenceDatabase = database ?? throw new ArgumentNullException(nameof(database));
                InitializePersistence(new CharacterMailAttachmentModel { Id = Id, Index = Index, ItemGuid = Item.Guid });
            }
        }

        public void Save(CharacterContext context) => SavePersistence(context, false);
        internal void Save(CharacterContext context, bool parentDeleted) => SavePersistence(context, parentDeleted);

        public ServerMailAvailable.Attachment Build()
        {
            return new ServerMailAvailable.Attachment
            {
                Item2Id     = Item.Id,
                StackCount  = Item.StackCount
            };
        }
    }
}
