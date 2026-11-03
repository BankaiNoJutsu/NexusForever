using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.GalacticArchive;
using NexusForever.Shared.Game.Events;
using NLog;
using Row = NexusForever.Game.Entity.ExperiencePersistence.ArchiveState;

namespace NexusForever.Game.Entity
{
    public partial class GalacticArchiveManager
    {
        private static readonly ILogger publicationLog = LogManager.GetCurrentClassLogger();
        private readonly object publicationGate = new();
        private sealed class Publication(Row[] state, Row[] notices, bool initial = false)
        {
            internal readonly Row[] State = state, Notices = notices;
            internal readonly bool Initial = initial;
            internal List<Action> Packets;
            internal int Cursor;
        }
        private readonly List<Row> pendingUpdates = [];
        private readonly Queue<Publication> publications = new();
        private Row[] confirmed, published;
        private IGameSession publicationSession;
        private Identity publicationIdentity;
        private bool publishing, publicationScheduled, initialSending;

        private bool OwnsPublication => player is Player original && publicationIdentity != null
            && original.OwnsArchivePublication(this, publicationIdentity, publicationSession);
        private void InitialisePublication()
        {
            publicationSession = player.Session;
            if (player.Identity != null) publicationIdentity = new() { Id = player.Identity.Id, RealmId = player.Identity.RealmId };
            confirmed = LoadedArchive; published = Known(confirmed);
        }
        private Row[] Known(Row[] rows) => rows.Where(row => articles.ContainsKey(row.Id)).OrderBy(row => row.Id).ToArray();
        private static ServerGalacticArchiveUpdate Build(Row row) => new()
            { ArchiveArticleId = row.Id, UnlockedFlags = row.Unlocked, ViewedFlags = row.Viewed };

        // Public archive operations only add unlock/view bits. A notice can
        // acknowledge exactly those accepted bits once the original graph has
        // confirmed them. Later bits from a frozen save remain pending.
        private Row[] TakeConfirmedUpdates()
        {
            var stored = confirmed.ToDictionary(row => row.Id);
            int count = 0;
            foreach (Row target in pendingUpdates)
            {
                if (!stored.TryGetValue(target.Id, out Row row) || row.Owner != target.Owner
                    || (row.Unlocked & target.Unlocked) != target.Unlocked || (row.Viewed & target.Viewed) != target.Viewed) break;
                count++;
            }
            Row[] ready = pendingUpdates.Take(count).ToArray(); pendingUpdates.RemoveRange(0, count); return ready;
        }
        private void RetainUpdate(Row row)
        {
            lock (publicationGate)
            {
                pendingUpdates.Add(row);
                Row[] ready = TakeConfirmedUpdates();
                if (ready.Length != 0) publications.Enqueue(new(confirmed, ready));
            }
            SchedulePublication();
        }
        internal void AcknowledgePublication(Row[] stored)
        {
            // Metadata only; ExperiencePersistence serializes this with its
            // baseline acknowledgement before another save can enter.
            lock (publicationGate)
            {
                confirmed = stored;
                publications.Enqueue(new(stored, TakeConfirmedUpdates()));
            }
        }
        internal void NotifyPublicationReady() => SchedulePublication();

        private void SendPublishedInitial()
        {
            if (!OwnsPublication) return;
            Row[] current = Known(CaptureArchive());
            Row[] snapshot = null;
            lock (publicationGate)
            {
                // Initial sync also requests newly quiet rule rows once they
                // commit. Ordinary notices may already retain that exact row,
                // either pending SQL or queued for their world update.
                foreach (Row row in current)
                    if (!published.Contains(row) && !pendingUpdates.Contains(row)
                        && !publications.Any(publication => publication.Notices.Contains(row)))
                        pendingUpdates.Add(row);
                Row[] ready = TakeConfirmedUpdates();
                if (ready.Length != 0) publications.Enqueue(new(confirmed, ready));
                if (publishing || initialSending || publications.Count != 0) publications.Enqueue(new(confirmed, [], true));
                else { initialSending = true; snapshot = published; }
            }
            if (snapshot == null) { SchedulePublication(); return; }
            try
            {
                if (!OwnsPublication) return;
                publicationSession.EnqueueMessageEncrypted(new ServerGalacticArchiveRefresh());
                foreach (Row row in snapshot)
                {
                    if (!OwnsPublication) return;
                    publicationSession.EnqueueMessageEncrypted(Build(row));
                }
            }
            finally { lock (publicationGate) initialSending = false; SchedulePublication(); }
        }
        private void SchedulePublication()
        {
            lock (publicationGate)
            {
                if (publicationScheduled || publishing || publications.Count == 0) return;
                publicationScheduled = true;
            }
            try
            {
                var events = publicationSession?.Events;
                if (events == null) { lock (publicationGate) publicationScheduled = false; return; }
                events.EnqueueThreadSafe(new PredicateEvent(() =>
                {
                    if (!OwnsPublication) return true;
                    lock (publicationGate) return !initialSending && player is Player { IsNormalSaveInProgress: false };
                }, PublishCommitted));
            }
            catch (Exception error)
            {
                lock (publicationGate) publicationScheduled = false;
                publicationLog.Error(error, "Committed archive scheduling will retry.");
            }
        }
        private void PublishCommitted()
        {
            lock (publicationGate)
            {
                publicationScheduled = false;
                if (publishing) return;
                publishing = true;
            }
            try
            {
                while (true)
                {
                    if (!OwnsPublication) { lock (publicationGate) publications.Clear(); return; }
                    Publication publication;
                    lock (publicationGate) if (!publications.TryPeek(out publication)) return;
                    publication.Packets ??= CapturePackets(publication);
                    while (true)
                    {
                        if (!OwnsPublication) { lock (publicationGate) publications.Clear(); return; }
                        Action packet;
                        lock (publicationGate)
                        {
                            RequireHead(publication);
                            if (publication.Cursor == publication.Packets.Count) { publications.Dequeue(); break; }
                            packet = publication.Packets[publication.Cursor];
                        }
                        packet(); lock (publicationGate) { RequireHead(publication); publication.Cursor++; }
                    }
                }
            }
            catch (Exception error) { publicationLog.Error(error, "Committed archive publication will retry."); }
            finally { lock (publicationGate) publishing = false; SchedulePublication(); }
        }
        private void RequireHead(Publication publication)
        {
            if (!publications.TryPeek(out var head) || !ReferenceEquals(head, publication)) throw new InvalidOperationException("Archive publication lost its retained queue owner.");
        }
        private List<Action> CapturePackets(Publication publication)
        {
            var actions = new List<Action> { () => { lock (publicationGate) published = Known(publication.State); } };
            if (publication.Initial) actions.Add(() => publicationSession.EnqueueMessageEncrypted(new ServerGalacticArchiveRefresh()));
            foreach (Row row in publication.Initial ? Known(publication.State) : publication.Notices)
                actions.Add(() => publicationSession.EnqueueMessageEncrypted(Build(row)));
            return actions;
        }
    }
}
