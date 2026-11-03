using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract;
using NexusForever.Game.Entity;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Achievement;
using NexusForever.Shared.Game.Events;
using NLog;
using AchievementRow = NexusForever.Game.Entity.ExperiencePersistence.Achievement;
using AchievementPacketRow = NexusForever.Network.World.Message.Model.Achievement.Achievement;
using RealmFirstClaim = NexusForever.Game.Achievement.RealmFirstPersistence.Claim;

namespace NexusForever.Game.Achievement
{
    public sealed partial class CharacterAchievementManager
    {
        private static readonly ILogger publicationLog = LogManager.GetCurrentClassLogger();
        private readonly object publicationGate = new();
        private readonly Dictionary<ushort, AchievementRow> publishedAchievements = [];
        internal RealmFirstPersistence RealmFirst { get; private set; }
        private sealed class Publication(AchievementRow[] rows, RealmFirstClaim[] claims)
        {
            internal readonly AchievementRow[] Rows = rows;
            internal readonly RealmFirstClaim[] Claims = claims;
            internal List<Action> Packets;
            internal int Cursor;
        }
        private readonly Queue<Publication> publications = new();
        private IGameSession publicationSession;
        private Identity publicationIdentity;
        private bool publicationScheduled, publishing;

        private void InitialisePublication(CharacterModel model)
        {
            if (owner is Player) RealmFirst = new(false, model.Id);
            publicationSession = owner.Session;
            if (owner.Identity != null) publicationIdentity = new Identity { Id = owner.Identity.Id, RealmId = owner.Identity.RealmId };
            foreach (CharacterAchievementModel row in model.Achievement)
                publishedAchievements.Add(row.AchievementId, ExperiencePersistence.FromModel(row));
        }
        private bool OwnsPublication => owner is Player original && original.CharacterId == AchievementCharacter
            && publicationIdentity != null && owner.Identity?.Id == publicationIdentity.Id && owner.Identity.RealmId == publicationIdentity.RealmId
            && (playerManager == null || ReferenceEquals(playerManager.GetPlayer(publicationIdentity), owner))
            && ReferenceEquals(original.AchievementManager, this) && ReferenceEquals(owner.Session, publicationSession)
            && publicationSession is IPlayerSession session && ReferenceEquals(session.Player, owner)
            && publicationSession is not NetworkSession { IsDisconnecting: true };
        private static AchievementPacketRow BuildPublished(AchievementRow row) => new()
        {
            AchievementId = row.Id, ProgressState = row.Progress, CreditedChecklistMask = row.Credited,
            DateCompleted = row.Completed.HasValue ? (ulong)DateTime.SpecifyKind(row.Completed.Value, DateTimeKind.Utc).ToFileTimeUtc() : 0UL
        };
        private bool SendPublishedInitial()
        {
            ServerAchievementInit packet;
            lock (publicationGate)
            {
                if (!OwnsPublication) return false;
                packet = new ServerAchievementInit { Achievements = publishedAchievements.Values.OrderBy(row => row.Id).Select(BuildPublished).ToList() };
            }
            if (!OwnsPublication) return false;
            publicationSession.EnqueueMessageEncrypted(packet);
            return OwnsPublication;
        }
        private void RetainRealmFirst(ushort id, string name)
        {
            RealmFirst.Retain(id, name);
        }
        internal void QueuePublication(AchievementRow[] before, AchievementRow[] stored, RealmFirstClaim[] claims)
        {
            lock (publicationGate)
            {
                var rows = stored.Where(row => before.SingleOrDefault(old => old.Id == row.Id) != row).ToArray();
                if (rows.Length == 0 && claims.Length == 0) return;
                publications.Enqueue(new(rows, claims));
            }
        }
        internal void NotifyPublicationReady() => SchedulePublication();
        private void SchedulePublication()
        {
            if (publicationScheduled || publishing || publications.Count == 0 || publicationSession?.Events == null) return;
            publicationScheduled = true;
            publicationSession.Events.EnqueueThreadSafe(new PredicateEvent(
                () => !OwnsPublication || owner is Player { IsNormalSaveInProgress: false }, PublishCommitted));
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
                    Publication publication;
                    lock (publicationGate)
                    {
                        if (!OwnsPublication) { publications.Clear(); return; }
                        if (!publications.TryPeek(out publication)) return;
                    }
                    // Enumerating recipients and enqueueing messages may call
                    // outward code. Never hold the persistence-facing gate here.
                    publication.Packets ??= CapturePackets(publication);
                    while (true)
                    {
                        Action packet;
                        lock (publicationGate)
                        {
                            if (!OwnsPublication) { publications.Clear(); return; }
                            RequireHead(publication);
                            if (publication.Cursor == publication.Packets.Count) { publications.Dequeue(); break; }
                            packet = publication.Packets[publication.Cursor];
                        }
                        packet();
                        lock (publicationGate) { RequireHead(publication); publication.Cursor++; }
                    }
                }
            }
            catch (Exception error) { publicationLog.Error(error, "Committed character achievement publication will retry."); }
            finally { lock (publicationGate) { publishing = false; SchedulePublication(); } }
        }
        private void RequireHead(Publication publication)
        {
            if (!publications.TryPeek(out var current) || !ReferenceEquals(current, publication))
                throw new InvalidOperationException("Achievement publication lost its retained queue owner.");
        }
        private List<Action> CapturePackets(Publication publication)
        {
            var actions = new List<Action>();
            if (publication.Rows.Length != 0) actions.Add(() =>
            {
                publicationSession.EnqueueMessageEncrypted(new ServerAchievementUpdate
                    { Achievements = publication.Rows.Select(BuildPublished).ToList() });
                lock (publicationGate) foreach (var row in publication.Rows) publishedAchievements[row.Id] = row;
            });
            foreach (RealmFirstClaim claim in publication.Claims)
            foreach (IPlayer recipient in playerManager?.ToArray() ?? [])
            {
                IGameSession connection = recipient.Session;
                var identity = recipient.Identity == null ? null : new Identity { Id = recipient.Identity.Id, RealmId = recipient.Identity.RealmId };
                actions.Add(() =>
                {
                    if (identity == null || recipient.Identity?.Id != identity.Id || recipient.Identity.RealmId != identity.RealmId
                        || !ReferenceEquals(playerManager.GetPlayer(identity), recipient)
                        || !ReferenceEquals(recipient.Session, connection) || connection is not IPlayerSession current
                        || !ReferenceEquals(current.Player, recipient) || connection is NetworkSession { IsDisconnecting: true }) return;
                    connection.EnqueueMessageEncrypted(new ServerRealmFirstAchievement
                        { AchievementId = claim.Id, IsGuildAchievement = false, Name = claim.Name });
                });
            }
            return actions;
        }
    }
}
