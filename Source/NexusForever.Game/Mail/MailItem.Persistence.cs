using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Quest;
using NexusForever.Game.Entity;

namespace NexusForever.Game.Mail
{
    public partial class MailItem
    {
        private QuestRowPersistence<CharacterMailModel, MailHeaderState> persistence;
        private bool deleteRequested;
        private DateTime storageCreateTime;
        private string persistenceDatabase;
        internal bool HasUnsettledDeletion => deleteRequested && !persistence.IsAbsent;

        private MailHeaderState CaptureHeader() => new(Id, RecipientId, (byte)SenderType, storedContentType, SenderId,
            Subject, Message, TextEntrySubject, TextEntryMessage, CreatureId, (byte)CurrencyType, CurrencyAmount,
            Convert.ToByte(IsCashOnDelivery), Convert.ToByte(HasPaidOrCollectedCurrency), (byte)Flags,
            (byte)DeliverySpeed, storageCreateTime);

        internal MailHeaderState CaptureMarketplaceCreation()
        {
            var header = CaptureHeader();
            // Whole seconds are exactly representable by both current provider
            // mappings. Allocate/capture once before any provider attempt.
            return header with { CreateTime = new DateTime(header.CreateTime.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc) };
        }

        private void InitializePersistence(CharacterMailModel baseline)
        {
            storageCreateTime = baseline?.CreateTime ?? CreateTime;
            membershipBaseline = baseline?.Attachment.Select(MailAttachmentState.From).OrderBy(a => a.Index).ToArray() ?? [];
            persistence = new($"mail:{Id}", $"mail:{Id}", MailHeaderState.From(baseline),
                context => context.CharacterMail.AsNoTracking().SingleOrDefault(row => row.Id == Id),
                MailHeaderState.From, value => value?.Model(), CaptureHeader, DateEncoding);
        }

        private (Func<MailHeaderState, MailHeaderState> Stage, Func<MailHeaderState, MailHeaderState> Ack) DateEncoding(CharacterContext context)
        {
            if (context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) != true)
                return (value => value, value => value);
            var dates = new Dictionary<DateTime, DateTime>();
            MailHeaderState Encode(MailHeaderState value, bool allowRead)
            {
                if (value == null) return null;
                if (!dates.TryGetValue(value.CreateTime, out var stored))
                {
                    if (!allowRead) return value;
                    stored = context.Database.SqlQueryRaw<DateTime>("SELECT CAST({0} AS DATETIME) AS Value", value.CreateTime).AsEnumerable().Single();
                    dates.Add(value.CreateTime, stored);
                }
                // Retain the chosen SQL timestamp through fresh-context retries.
                // A bank-confirmed header already supplies its exact stored value.
                if (allowRead) storageCreateTime = stored;
                return value with { CreateTime = stored };
            }
            return (value => Encode(value, true), value => Encode(value, false));
        }

        private void SavePersistence(CharacterContext context, bool force = false)
        {
            ArgumentNullException.ThrowIfNull(context);
            lock (persistence.Gate)
            {
                try
                {
                    AssertNoCashReceipt();
                    if (CreationOwner != null && PendingCreate)
                        throw new InvalidOperationException("Mail creation still belongs to its captured player-send transaction.");
                    if (deleteRequested && persistence.IsAbsent) return;
                    string database = Item.GetRewardDatabaseKey(context);
                    if (persistenceDatabase != null && persistenceDatabase != database)
                        throw new InvalidOperationException("Mail changed its retained execution database.");
                    persistenceDatabase = database;
                    persistence.Save(context, deleteRequested, force: force || mailAttachments.Count != 0 || deletedAttachments.Count != 0);
                    if (!force && !persistence.NeedsSave && !deleteRequested && mailAttachments.Count == 0 && deletedAttachments.Count == 0) return;
                    BeginMembership(context);
                    foreach (var attachment in mailAttachments)
                        if (attachment is MailAttachment concrete) concrete.Save(context, deleteRequested); else attachment.Save(context);
                    foreach (var attachment in deletedAttachments)
                        if (attachment is MailAttachment concrete) concrete.Save(context, deleteRequested); else attachment.Save(context);
                    StageMembership(context);
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
    }
}
