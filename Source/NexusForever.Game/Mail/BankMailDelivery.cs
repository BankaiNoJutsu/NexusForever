using Attachment = NexusForever.Game.Mail.MailAttachmentState;
using Header = NexusForever.Game.Mail.MailHeaderState;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.RealmBank;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Mail
{
    // The sender's bank command owns these new rows. No live mail object is
    // published or acknowledged until the complete SQL unit has committed.
    internal sealed class BankMailDelivery : IBankSaveCompanion
    {
        private readonly MailItem mail;
        private readonly Header header;
        private readonly MailAttachment[] attachments;
        private readonly Attachment[] rows;
        private readonly QuestRowPersistence<CharacterMailModel, Header> root;
        private readonly QuestRowPersistence<CharacterMailAttachmentModel, Attachment>[] leaves;
        private bool attempted, ended;
        private bool adopted, quarantined;
        private string database;
        private static readonly object registryGate = new();
        private static readonly Dictionary<(string Database, ulong Mail), WeakReference<BankMailDelivery>> registry = [];
        private Header storedHeader;
        internal uint RecipientAccount { get; private set; }
        public bool CanExecuteWithoutItems => true;

        internal BankMailDelivery(MailItem mail, ulong sender)
        {
            this.mail = mail;
            mail.CreationOwner = this;
            attachments = mail.Cast<MailAttachment>().ToArray();
            if (!mail.PendingCreate || mail.PendingDelete || mail.SenderId != sender
                || attachments.Any(a => a.Item is not Item) || attachments.Select(a => a.Item.Guid).Distinct().Count() != attachments.Length)
                throw new InvalidOperationException("Player mail requires its complete new attachment set.");
            header = new(mail.Id, mail.RecipientId, (byte)mail.SenderType, (byte)mail.ContentType, mail.SenderId,
                mail.Subject, mail.Message, mail.TextEntrySubject, mail.TextEntryMessage, mail.CreatureId, (byte)mail.CurrencyType,
                mail.CurrencyAmount, Convert.ToByte(mail.IsCashOnDelivery), Convert.ToByte(mail.HasPaidOrCollectedCurrency),
                (byte)mail.Flags, (byte)mail.DeliverySpeed, mail.CreateTime);
            rows = attachments.Select(a => new Attachment(a.Id, a.Index, a.Item.Guid)).ToArray();
            string group = $"character:{sender}";
            root = new($"mail:{mail.Id}", group, null,
                c => c.CharacterMail.AsNoTracking().SingleOrDefault(m => m.Id == mail.Id), Header.From,
                h => h.Model(), () => header, c =>
                {
                    Header Normalize(Header h) => h == null ? null : h with { CreateTime = new DateTime(h.CreateTime.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, h.CreateTime.Kind) };
                    bool mysql = c.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) == true;
                    storedHeader = mysql ? Normalize(header) : header;
                    return mysql ? (Normalize, Normalize) : (h => h, h => h);
                });
            leaves = rows.Select(row => new QuestRowPersistence<CharacterMailAttachmentModel, Attachment>(
                $"mail-attachment:{row.Id}:{row.Index}", group, null,
                c => c.CharacterMailAttachment.AsNoTracking().SingleOrDefault(a => a.Id == row.Id && a.Index == row.Index),
                Attachment.From, a => a.Model(), () => row)).ToArray();
        }

        public void Stage(CharacterContext context)
        {
            if (ended) throw new InvalidOperationException("Mail creation has ended.");
            lock (registryGate)
            {
                string key = Item.GetRewardDatabaseKey(context);
                if (database != null && database != key) throw new InvalidOperationException("Mail changed its retained database.");
                database = key;
                if (!quarantined)
                {
                    foreach (var stale in registry.Where(pair => !pair.Value.TryGetTarget(out _)).Select(pair => pair.Key).ToArray()) registry.Remove(stale);
                    if (registry.TryGetValue((database, header.Id), out var prior) && prior.TryGetTarget(out var owner) && !ReferenceEquals(owner, this))
                        throw new InvalidOperationException("Mail identity belongs to another live send.");
                    registry[(database, header.Id)] = new(this);
                }
            }
            var recipient = context.Character.AsNoTracking().SingleOrDefault(c => c.Id == header.RecipientId);
            if (recipient == null || recipient.DeleteTime != null || attempted && RecipientAccount != recipient.AccountId)
                throw new InvalidOperationException("Mail recipient lost its captured account.");
            if (!attempted && (context.CharacterMail.Any(m => m.Id == header.Id) || context.CharacterMailAttachment.Any(a => a.Id == header.Id)))
                throw new InvalidOperationException("Mail allocator reused a stored identity.");
            attempted = true; RecipientAccount = recipient.AccountId;
            root.Save(context, force: true);
            foreach (var leaf in leaves) leaf.Save(context, force: true);
            context.OnSaveValidation(phase =>
            {
                var tracked = context.ChangeTracker.Entries<CharacterMailAttachmentModel>()
                    .Where(e => e.Entity.Id == header.Id && e.State is not (EntityState.Detached or EntityState.Deleted))
                    .Select(e => Attachment.From(e.Entity)).OrderBy(a => a.Index).ToArray();
                if (!rows.SequenceEqual(tracked)) throw new InvalidOperationException("Mail lost its complete tracked attachment set.");
                if (phase != CharacterSaveValidationPhase.BeforeFlush)
                {
                    var sql = context.CharacterMailAttachment.AsNoTracking().Where(a => a.Id == header.Id)
                        .ToArray().Select(Attachment.From).OrderBy(a => a.Index).ToArray();
                    if (!rows.SequenceEqual(sql)) throw new InvalidOperationException("Mail lost its complete SQL attachment set.");
                }
            });
        }

        public void Adopt()
        {
            var proof = root.CaptureReward(h => h, h => h).Candidates();
            if (ended || !attempted || storedHeader == null || proof.Count != 1 || proof[0] != storedHeader || !mail.PendingCreate || mail.PendingDelete
                || !attachments.SequenceEqual(mail.Cast<MailAttachment>())
                || attachments.Any(a => !a.CanAcceptCommittedCreation || a.Item.CharacterId != null
                    || a.Item.Location != InventoryLocation.None || a.Item.PendingDelete))
                throw new InvalidOperationException("Mail creation lost its confirmed live or SQL owner.");
            for (int i = 0; i < leaves.Length; ++i)
            {
                var candidates = leaves[i].CaptureReward(a => a, a => a).Candidates();
                if (candidates.Count != 1 || candidates[0] != rows[i]) throw new InvalidOperationException("Mail attachment is not committed.");
            }
            mail.AcceptCommittedCreation(storedHeader.Model(), database);
            lock (registryGate) adopted = true;
        }
        public void Complete() { lock (registryGate) ended = true; }
        public void Abort() { lock (registryGate) { ended = true; RemoveRegistration(); } }
        // A dead actor can retire while an owned worker attempt still finishes.
        public void Quarantine() { lock (registryGate) { quarantined = true; mail.CreationOwner = null; RemoveRegistration(); } }
        internal void Delivered() { lock (registryGate) { mail.CreationOwner = null; RemoveRegistration(); } }
        private void RemoveRegistration()
        {
            if (database != null && registry.TryGetValue((database, header.Id), out var reference)
                && reference.TryGetTarget(out var owner) && ReferenceEquals(owner, this)) registry.Remove((database, header.Id));
        }
        internal static bool Resolve(string database, CharacterMailModel row, out MailItem original)
        {
            lock (registryGate)
            {
                original = null;
                var key = (database, row.Id);
                if (!registry.TryGetValue(key, out var reference)) return true;
                if (!reference.TryGetTarget(out var owner)) { registry.Remove(key); return true; }
                if (!owner.ended) return false;
                if (owner.adopted && !owner.quarantined && Header.From(row) == owner.storedHeader
                    && owner.rows.SequenceEqual(row.Attachment.Select(Attachment.From).OrderBy(a => a.Index))) original = owner.mail;
                registry.Remove(key);
                return true;
            }
        }
    }
}
