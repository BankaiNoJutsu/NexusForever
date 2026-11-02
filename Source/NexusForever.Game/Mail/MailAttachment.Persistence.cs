using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using GameItem = NexusForever.Game.Entity.Item;
using NexusForever.Game.Quest;

namespace NexusForever.Game.Mail
{
    public partial class MailAttachment
    {
        private QuestRowPersistence<CharacterMailAttachmentModel, MailAttachmentState> persistence;
        private bool deleteRequested;
        private string persistenceDatabase;
        internal bool IsDeletionSettled => persistence.IsAbsent;

        private void InitializePersistence(CharacterMailAttachmentModel baseline)
        {
            persistence = new($"mail-attachment:{Id}:{Index}", $"mail:{Id}", MailAttachmentState.From(baseline),
                context => context.CharacterMailAttachment.AsNoTracking().SingleOrDefault(row => row.Id == Id && row.Index == Index),
                MailAttachmentState.From, value => value?.Model(), () => new(Id, Index, Item.Guid));
        }

        private void SavePersistence(CharacterContext context, bool parentDeleted)
        {
            ArgumentNullException.ThrowIfNull(context);
            lock (persistence.Gate)
            {
                try
                {
                    if (takeReceipt != null) throw new InvalidOperationException("Mail attachment belongs to an unfinished take.");
                    string database = GameItem.GetRewardDatabaseKey(context);
                    if (persistenceDatabase != null && persistenceDatabase != database)
                        throw new InvalidOperationException("Mail attachment changed its retained execution database.");
                    persistenceDatabase = database;
                    if (!(deleteRequested || parentDeleted) || !persistence.IsAbsent)
                        persistence.Save(context, deleteRequested || parentDeleted, force: true);
                    Item.Save(context);
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
    }
}
