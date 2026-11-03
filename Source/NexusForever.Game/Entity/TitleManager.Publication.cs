using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NLog;

namespace NexusForever.Game.Entity
{
    public partial class TitleManager
    {
        private static readonly ILogger publicationLog = LogManager.GetCurrentClassLogger();
        private abstract record Notice;
        private sealed record UpdateTitle(ushort Id, bool Owned, bool Revoked) : Notice;
        private sealed record SelectTitle(ushort Id) : Notice;
        private sealed record ListTitles(TitlePersistence.Snapshot State) : Notice;
        private sealed record PendingNotice(long Version, Notice Notice);
        private sealed class Publication(TitlePersistence.Snapshot state, Notice[] notices)
        {
            internal readonly TitlePersistence.Snapshot State = state;
            internal readonly Notice[] Notices = notices;
            internal List<Action> Packets;
            internal int Cursor;
        }
        private readonly List<PendingNotice> pendingNotices = [];
        private readonly Queue<Publication> publications = new();
        private long publicationVersion;
        private TitlePersistence.Snapshot published;
        private IGameSession publicationSession;
        private Identity publicationIdentity;
        private bool publicationScheduled, publishing;

        internal ushort PublishedActiveTitle { get { lock (persistence.Gate) return published.Active ?? 0; } }
        private bool OwnsPublication => player is Player original && publicationIdentity != null
            && original.OwnsTitlePublication(this, publicationIdentity, publicationSession);

        private void InitialisePublication(CharacterModel model)
        {
            publicationSession = player.Session;
            if (player.Identity != null) publicationIdentity = new() { Id = player.Identity.Id, RealmId = player.Identity.RealmId };
            published = TitlePersistence.FromModel(model);
            // Unknown/revoked static titles were already hidden on load. Keep
            // that compatibility while their raw persistence rows are retained.
            if (model.Title != 0 && (!titles.TryGetValue(model.Title, out var title) || title.Revoked))
                published = published with { Active = 0 };
        }
        private void RetainNotice(Notice notice)
        {
            lock (persistence.Gate) pendingNotices.Add(new(++publicationVersion, notice));
        }
        private void AcknowledgePublication(TitlePersistence.Snapshot state, long version)
        {
            if (player is not Player) return;
            lock (persistence.Gate)
            {
                publications.Enqueue(new(state, pendingNotices.Where(row => row.Version <= version).Select(row => row.Notice).ToArray()));
                pendingNotices.RemoveAll(row => row.Version <= version);
            }
        }
        private ServerTitles BuildPublishedList(TitlePersistence.Snapshot state)
            => new() { Titles = state.Titles.Where(row => titles.ContainsKey(row.Id)).Select(row => new ServerTitles.Title
                { TitleId = row.Id, Revoked = row.Revoked != 0, TimeRemaining = row.Time }).ToList() };

        private void SendPublishedTitles()
        {
            if (!OwnsPublication) return;
            ServerTitles packet;
            lock (persistence.Gate) packet = BuildPublishedList(published);
            if (OwnsPublication) publicationSession.EnqueueMessageEncrypted(packet);
        }
        private void SchedulePublication()
        {
            lock (persistence.Gate)
            {
                if (publicationScheduled || publishing || publications.Count == 0) return;
                publicationScheduled = true;
            }
            try
            {
                var events = publicationSession?.Events;
                if (events == null) { lock (persistence.Gate) publicationScheduled = false; return; }
                events.EnqueueThreadSafe(new PredicateEvent(() => !OwnsPublication || player is Player { IsNormalSaveInProgress: false }, PublishCommitted));
            }
            catch (Exception error)
            {
                lock (persistence.Gate) publicationScheduled = false;
                publicationLog.Error(error, "Committed title publication scheduling will retry.");
            }
        }
        private void PublishCommitted()
        {
            lock (persistence.Gate)
            {
                publicationScheduled = false;
                if (publishing) return;
                publishing = true;
            }
            try
            {
                while (true)
                {
                    Publication publication;
                    if (!OwnsPublication) { lock (persistence.Gate) publications.Clear(); return; }
                    lock (persistence.Gate) if (!publications.TryPeek(out publication)) return;
                    publication.Packets ??= CapturePackets(publication);
                    while (true)
                    {
                        if (!OwnsPublication) { lock (persistence.Gate) publications.Clear(); return; }
                        Action packet;
                        lock (persistence.Gate)
                        {
                            RequireHead(publication);
                            if (publication.Cursor == publication.Packets.Count) { publications.Dequeue(); break; }
                            packet = publication.Packets[publication.Cursor];
                        }
                        packet();
                        lock (persistence.Gate) { RequireHead(publication); publication.Cursor++; }
                    }
                }
            }
            catch (Exception error) { publicationLog.Error(error, "Committed title publication will retry."); }
            finally { lock (persistence.Gate) publishing = false; SchedulePublication(); }
        }
        private void RequireHead(Publication publication)
        {
            if (!publications.TryPeek(out var head) || !ReferenceEquals(head, publication))
                throw new InvalidOperationException("Title publication lost its retained queue owner.");
        }
        private List<Action> CapturePackets(Publication publication)
        {
            var packets = new List<Action> { () => { lock (persistence.Gate) published = publication.State; } };
            foreach (Notice notice in publication.Notices)
                switch (notice)
                {
                    case UpdateTitle update:
                        packets.Add(() => publicationSession.EnqueueMessageEncrypted(new ServerTitleUpdate
                            { TitleId = update.Id, Alreadyowned = update.Owned, Revoked = update.Revoked }));
                        break;
                    case ListTitles list:
                        packets.Add(() =>
                        {
                            ServerTitles packet;
                            lock (persistence.Gate) packet = BuildPublishedList(list.State);
                            publicationSession.EnqueueMessageEncrypted(packet);
                        });
                        break;
                    case SelectTitle selection:
                        var original = (Player)player;
                        object map = original.Map;
                        uint ownerGuid = original.Guid;
                        foreach (IPlayer recipient in original.CaptureTitleRecipients())
                        {
                            IGameSession session = recipient.Session;
                            var identity = recipient.Identity == null ? null : new Identity { Id = recipient.Identity.Id, RealmId = recipient.Identity.RealmId };
                            uint guid = recipient.Guid;
                            packets.Add(() =>
                            {
                                if (identity == null || recipient.Identity?.Id != identity.Id || recipient.Identity.RealmId != identity.RealmId
                                    || !ReferenceEquals(recipient.Session, session) || session is not IPlayerSession current
                                    || !ReferenceEquals(current.Player, recipient) || session is NetworkSession { IsDisconnecting: true }) return;
                                if (!ReferenceEquals(recipient, original) && (!ReferenceEquals(original.Map, map)
                                    || !ReferenceEquals(original.GetVisible<IGridEntity>(guid), recipient))) return;
                                session.EnqueueMessageEncrypted(new ServerTitleSet { Guid = ownerGuid, Title = selection.Id });
                            });
                        }
                        break;
                    default: throw new InvalidOperationException("Unknown retained title publication.");
                }
            return packets;
        }
    }
}
