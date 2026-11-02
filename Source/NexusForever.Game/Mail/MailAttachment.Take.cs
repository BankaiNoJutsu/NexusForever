using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Quest;
using GameItem = NexusForever.Game.Entity.Item;

namespace NexusForever.Game.Mail
{
    public partial class MailAttachment
    {
        private TakeReceipt takeReceipt;

        internal TakeReceipt CaptureTake()
        {
            lock (persistence.Gate)
            {
                persistence.AssertRewardOwner();
                if (takeReceipt != null || deleteRequested || persistence.Has((int)MailAttachmentSaveMask.Create))
                    throw new InvalidOperationException("Mail attachment requires its unchanged loaded link.");
                var snapshot = persistence.CaptureNormal(null);
                var acknowledgement = persistence.CaptureReward(value => value, value => value);
                return takeReceipt = new(this, snapshot, acknowledgement);
            }
        }

        internal sealed class TakeReceipt(MailAttachment owner,
            QuestRowPersistence<CharacterMailAttachmentModel, MailAttachmentState>.NormalSnapshot snapshot,
            QuestRewardRow<MailAttachmentState> acknowledgement)
        {
            private bool ended, adopted;
            private void ValidateOwner()
            {
                if (ended || !ReferenceEquals(owner.takeReceipt, this))
                    throw new InvalidOperationException("Mail attachment lost its take receipt.");
            }
            internal void Stage(CharacterContext context)
            {
                lock (owner.persistence.Gate)
                {
                    ValidateOwner();
                    string database = GameItem.GetRewardDatabaseKey(context);
                    if (owner.persistenceDatabase != null && owner.persistenceDatabase != database)
                        throw new InvalidOperationException("Mail attachment take changed database.");
                    owner.persistenceDatabase = database;
                    owner.persistence.Save(context, delete: true, force: true, frozen: snapshot);
                }
            }
            internal void ValidateConfirmed()
            {
                lock (owner.persistence.Gate)
                {
                    ValidateOwner(); var proof = acknowledgement.Candidates();
                    if (adopted || !owner.persistence.WasAttempted || proof.Count != 1 || proof[0] != null)
                        throw new InvalidOperationException("Mail attachment has no confirmed link deletion.");
                }
            }
            internal void Adopt()
            {
                lock (owner.persistence.Gate)
                {
                    ValidateConfirmed();
                    owner.deleteRequested = true;
                    acknowledgement.Adopt(null); adopted = true;
                }
            }
            internal void Complete()
            {
                lock (owner.persistence.Gate)
                {
                    ValidateOwner();
                    if (!adopted) throw new InvalidOperationException("Mail attachment take cannot finish before adoption.");
                    ended = true; owner.takeReceipt = null;
                }
            }
            internal void Abort()
            {
                lock (owner.persistence.Gate) { ValidateOwner(); ended = true; owner.takeReceipt = null; }
            }
            internal void Quarantine()
            {
                lock (owner.persistence.Gate) { ended = true; owner.persistence.RetireRewardOwner(); }
            }
        }
    }
}
